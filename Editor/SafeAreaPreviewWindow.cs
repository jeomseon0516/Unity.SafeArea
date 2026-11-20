// Assets/Jeomseon/SafeArea/Editor/SafeAreaPreviewWindow.cs
#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Jeomseon.Unity.SafeArea;

namespace Jeomseon.Unity.SafeArea.Editor
{
    /// <summary>
    /// Device Simulator에 값 전파를 위임하는 방식은(<c>SafeAreaWatcher.ForceUpdate()</c>로 실제
    /// Scene의 컴포넌트를 직접 갱신) 검토했지만 채택하지 않았다 — "Preview는 원본 Scene을 절대
    /// 건드리지 않아야 한다"는 원칙과 충돌한다. Undo/SetDirty를 안 걸어도, 사용자가 다른 이유로
    /// Scene을 dirty시키고 저장하면 Preview가 실제 컴포넌트에 써넣은 값까지 같이 저장될 위험이
    /// 있다. 그래서 원본과 완전히 격리된 PreviewScene(복제 Canvas + Camera + RenderTexture)에만
    /// 적용하고, 렌더링 결과만 이 창에 그려서 보여준다.
    /// </summary>
    public class SafeAreaPreviewWindow : EditorWindow
    {
        private bool _overrideEnabled = false; // 기본은 OFF
        private Rect _overrideSafeArea;
        private Vector2 _overrideScreenSize = new Vector2(1080, 1920);

        // ----- 시뮬레이터/Screen에서 읽어온 값 -----
        // 항상 OnEditorUpdate에서 최신 값으로 유지 (가벼운 갱신)
        private Vector2 _simScreenSize;
        private Rect _simSafeArea;

        // 변경 감지용 이전 값
        private Vector2 _lastSimScreenSize;
        private Rect _lastSimSafeArea;

        // 마지막 RebuildAll 시점에 실제로 프리뷰에 반영된 값 (stale 감지용)
        private Vector2 _builtScreenSize;
        private Rect _builtSafeArea;

        // ----- Preview용 씬/카메라/RT -----
        private Scene _previewScene;
        private Camera _previewCamera;
        private RenderTexture _rt;
        private bool _showSafeAreaOverlay = true;

        // 디버그용
        private int _srcCanvasCount;
        private int _previewCanvasCount;

        // ----- UI Toolkit 요소 (CreateGUI에서 생성) -----
        private Label _inputValueLabel;
        private Label _sourceCanvasesValueLabel;
        private Label _previewCanvasesValueLabel;
        private Label _canvasesInSceneLabel;
        private Label _cameraEnabledLabel;
        private Label _cameraActiveLabel;
        private Vector2Field _appliedScreenField;
        private RectField _appliedSafeAreaField;
        private Label _insetsValueLabel;
        private Vector2Field _simulatorScreenField;
        private RectField _simulatorSafeAreaField;
        private Toggle _overrideToggle;
        private Vector2Field _overrideScreenField;
        private RectField _overrideSafeAreaField;
        private HelpBox _staleHelpBox;
        private VisualElement _previewArea;
        private VisualElement _previewFrame;
        private Image _previewImage;
        private VisualElement _overlayRoot;
        private readonly VisualElement[] _unsafeAreas = new VisualElement[4];
        private readonly Label[] _unsafeAreaLabels = new Label[4];
        private VisualElement _safeAreaBorder;
        private VisualElement _safeAreaBadge;
        private Label _safeAreaBadgeLabel;

        // =====================================================================
        //  Menu
        // =====================================================================

        private const float ControlPanelWidth = 340f;
        private const float LabelFieldLabelWidth = 150f;
        private const int OverlayFontSize = 9;
        private static readonly Color UnsafeAreaOverlayColor = new(0.85f, 0.12f, 0.08f, 0.22f);
        private static readonly Color SafeAreaBorderColor = new(0.2f, 1f, 0.35f, 1f);
        private static readonly Color SafeAreaBadgeColor = new(0.02f, 0.08f, 0.03f, 0.88f);

        [MenuItem("Jeomseon/Safe Area/Preview Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SafeAreaPreviewWindow>("Safe Area Preview");
            window.minSize = new Vector2(720, 360);
        }

        // =====================================================================
        //  Life Cycle
        // =====================================================================

        private void OnEnable()
        {
            // 시뮬레이터 값 초기화
            RefreshSimulatorValues();
            _lastSimScreenSize = _simScreenSize;
            _lastSimSafeArea = _simSafeArea;

            // Override 초기값은 "현재 시뮬레이터 상태"로 맞춰놓기
            _overrideScreenSize = _simScreenSize;
            _overrideSafeArea = _simSafeArea;

            CreatePreviewScene();

            // GameView 변경 감지용 (하지만 무거운 작업은 여기서 안 함)
            EditorApplication.update += OnEditorUpdate;

            // PreviewScene은 도메인 리로드(재컴파일) 시 Unity가 자체적으로 강제 정리한다. OnDisable만
            // 믿으면 그 정리 타이밍과 경합해 "Releasing render texture that is set as
            // Camera.targetTexture!" 경고가 날 수 있어, 리로드 직전에 우리가 먼저 정리한다.
            AssemblyReloadEvents.beforeAssemblyReload += DestroyPreviewScene;

            // 첫 진입 시 한 번만 전체 리빌드
            RebuildAll();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= DestroyPreviewScene;
            DestroyPreviewScene();
        }

        /// <summary>
        /// 매 프레임 호출되지만, 여기서는 "값 갱신 + Repaint"만 한다.
        /// 무거운 Rebuild/Render 작업은 절대 여기서 하지 않는다.
        /// </summary>
        private void OnEditorUpdate()
        {
            // 항상 시뮬레이터(Screen) 값만 추적
            Vector2 currentScreenSize = Handles.GetMainGameViewSize();
            Rect currentSafeArea = Screen.safeArea;

            bool screenSizeChanged =
                Vector2.Distance(currentScreenSize, _lastSimScreenSize) > 0.1f;
            bool safeAreaChanged =
                Mathf.Abs(currentSafeArea.x - _lastSimSafeArea.x) > 0.1f ||
                Mathf.Abs(currentSafeArea.y - _lastSimSafeArea.y) > 0.1f ||
                Mathf.Abs(currentSafeArea.width - _lastSimSafeArea.width) > 0.1f ||
                Mathf.Abs(currentSafeArea.height - _lastSimSafeArea.height) > 0.1f;

            if (screenSizeChanged || safeAreaChanged)
            {
                _simScreenSize = currentScreenSize;
                _simSafeArea = currentSafeArea;
                _lastSimScreenSize = currentScreenSize;
                _lastSimSafeArea = currentSafeArea;

                // Override가 꺼져 있으면, 필드에도 최신값을 보여주고 싶다면 동기화
                if (!_overrideEnabled)
                {
                    _overrideScreenSize = _simScreenSize;
                    _overrideSafeArea = _simSafeArea;
                }

                // 값만 갱신하고, 렌더/리빌드는 하지 않는다.
                // 사용자가 Apply 버튼을 누르면 그때 한 번만 리빌드.
                RefreshView();
            }
        }

        // =====================================================================
        //  GUI
        // =====================================================================

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var title = new Label("Safe Area Preview (PreviewScene)");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(title);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.marginTop = 6f;
            root.Add(body);

            // ----- 왼쪽: 컨트롤 패널 -----
            var controlPanel = new VisualElement();
            controlPanel.style.width = ControlPanelWidth;
            controlPanel.style.flexShrink = 0f;
            body.Add(controlPanel);

            controlPanel.Add(CreateBoldLabel("Preview Status"));
            _inputValueLabel = AddLabelField(controlPanel, "Input");
            _sourceCanvasesValueLabel = AddLabelField(controlPanel, "Source Canvases");
            _previewCanvasesValueLabel = AddLabelField(controlPanel, "Preview Canvases");
            _canvasesInSceneLabel = new Label();
            controlPanel.Add(_canvasesInSceneLabel);
            _cameraEnabledLabel = new Label();
            controlPanel.Add(_cameraEnabledLabel);
            _cameraActiveLabel = new Label();
            controlPanel.Add(_cameraActiveLabel);

            var appliedHeader = CreateBoldLabel("Applied Safe Area");
            appliedHeader.style.marginTop = 6f;
            controlPanel.Add(appliedHeader);

            // IMGUI에서는 값을 반환받지 않아 편집해도 즉시 되돌아가던 필드라, 변경 시 현재 값으로 되돌린다.
            _appliedScreenField = new Vector2Field("Screen (px)");
            _appliedScreenField.RegisterValueChangedCallback(_ =>
                _appliedScreenField.SetValueWithoutNotify(GetEffectiveScreenSize()));
            controlPanel.Add(_appliedScreenField);
            _appliedSafeAreaField = new RectField("Safe Area (px)");
            _appliedSafeAreaField.RegisterValueChangedCallback(_ =>
                _appliedSafeAreaField.SetValueWithoutNotify(GetEffectiveSafeArea()));
            controlPanel.Add(_appliedSafeAreaField);
            _insetsValueLabel = AddLabelField(controlPanel, "Insets (L / R / T / B)");

            var overlayToggle = new Toggle("Show Safe Area Overlay") { value = _showSafeAreaOverlay };
            overlayToggle.RegisterValueChangedCallback(evt =>
            {
                _showSafeAreaOverlay = evt.newValue;
                RefreshView();
            });
            controlPanel.Add(overlayToggle);

            // 시뮬레이터(현재 GameView) 기준 값 디스플레이 (읽기 전용)
            _simulatorScreenField = new Vector2Field("Simulator Screen (px)");
            _simulatorScreenField.style.marginTop = 6f;
            _simulatorScreenField.SetEnabled(false);
            controlPanel.Add(_simulatorScreenField);
            _simulatorSafeAreaField = new RectField("Simulator SafeArea (px)");
            _simulatorSafeAreaField.SetEnabled(false);
            controlPanel.Add(_simulatorSafeAreaField);

            // ----- Override 토글 -----
            _overrideToggle = new Toggle("Override Safe Area") { value = _overrideEnabled };
            _overrideToggle.style.marginTop = 6f;
            _overrideToggle.RegisterValueChangedCallback(evt =>
            {
                _overrideEnabled = evt.newValue;

                // Override를 껐을 때는, 필드를 시뮬레이터 상태로 맞춰두면 UX가 더 직관적
                if (!_overrideEnabled)
                {
                    _overrideScreenSize = _simScreenSize;
                    _overrideSafeArea = _simSafeArea;
                }

                // Override 상태가 바뀌면, 현재 설정에 맞춰 한 번만 전체 리빌드
                RebuildAll();
            });
            controlPanel.Add(_overrideToggle);

            // ----- Override 값 편집 (Override ON일 때만 수정 가능) -----
            _overrideScreenField = new Vector2Field("Override Screen Size (px)");
            _overrideScreenField.style.marginTop = 6f;
            _overrideScreenField.RegisterValueChangedCallback(evt =>
            {
                _overrideScreenSize = evt.newValue;
                RefreshView();
            });
            controlPanel.Add(_overrideScreenField);
            _overrideSafeAreaField = new RectField("Override Safe Area (px)");
            _overrideSafeAreaField.RegisterValueChangedCallback(evt =>
            {
                _overrideSafeArea = evt.newValue;
                RefreshView();
            });
            controlPanel.Add(_overrideSafeAreaField);

            _staleHelpBox = new HelpBox(
                "Device Simulator values changed. Press Apply & Rebuild Preview to refresh the render.",
                HelpBoxMessageType.Info);
            controlPanel.Add(_staleHelpBox);

            // Override ON이면 사용자가 입력한 값을, OFF이면 시뮬레이터 값을 사용하여 한 번만 전체 리빌드
            controlPanel.Add(new Button(RebuildAll) { text = "Apply & Rebuild Preview" });

            // ----- 오른쪽: 프리뷰 렌더 (남은 가로 공간 전체) -----
            _previewArea = new VisualElement();
            _previewArea.style.flexGrow = 1f;
            _previewArea.style.overflow = Overflow.Hidden;
            _previewArea.RegisterCallback<GeometryChangedEvent>(_ => UpdatePreviewLayout());
            body.Add(_previewArea);

            _previewFrame = new VisualElement { pickingMode = PickingMode.Ignore };
            _previewFrame.style.position = Position.Absolute;
            // 카메라 배경색과 같은 검정 배경 위에 RenderTexture를 그린다.
            _previewFrame.style.backgroundColor = Color.black;
            _previewArea.Add(_previewFrame);

            _previewImage = new Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            SetAbsoluteFill(_previewImage);
            _previewFrame.Add(_previewImage);

            _overlayRoot = new VisualElement { pickingMode = PickingMode.Ignore };
            SetAbsoluteFill(_overlayRoot);
            _previewFrame.Add(_overlayRoot);

            for (var i = 0; i < _unsafeAreas.Length; i++)
            {
                var unsafeArea = CreateOverlayBox(UnsafeAreaOverlayColor);
                var label = CreateOverlayLabel(TextAnchor.MiddleCenter);
                label.style.whiteSpace = WhiteSpace.Normal;
                SetAbsoluteFill(label);
                unsafeArea.Add(label);
                _overlayRoot.Add(unsafeArea);
                _unsafeAreas[i] = unsafeArea;
                _unsafeAreaLabels[i] = label;
            }

            _safeAreaBorder = new VisualElement { pickingMode = PickingMode.Ignore };
            _safeAreaBorder.style.position = Position.Absolute;
            const float borderWidth = 2f;
            _safeAreaBorder.style.borderLeftWidth = borderWidth;
            _safeAreaBorder.style.borderRightWidth = borderWidth;
            _safeAreaBorder.style.borderTopWidth = borderWidth;
            _safeAreaBorder.style.borderBottomWidth = borderWidth;
            _safeAreaBorder.style.borderLeftColor = SafeAreaBorderColor;
            _safeAreaBorder.style.borderRightColor = SafeAreaBorderColor;
            _safeAreaBorder.style.borderTopColor = SafeAreaBorderColor;
            _safeAreaBorder.style.borderBottomColor = SafeAreaBorderColor;
            _overlayRoot.Add(_safeAreaBorder);

            _safeAreaBadge = CreateOverlayBox(SafeAreaBadgeColor);
            _safeAreaBadge.style.overflow = Overflow.Hidden;
            _safeAreaBadgeLabel = CreateOverlayLabel(TextAnchor.UpperLeft);
            _safeAreaBadgeLabel.style.position = Position.Absolute;
            _safeAreaBadgeLabel.style.left = 6f;
            _safeAreaBadgeLabel.style.right = 6f;
            _safeAreaBadgeLabel.style.top = 2f;
            _safeAreaBadgeLabel.style.bottom = 2f;
            _safeAreaBadge.Add(_safeAreaBadgeLabel);
            _overlayRoot.Add(_safeAreaBadge);

            RefreshView();
        }

        /// <summary>
        /// 현재 상태 값을 UI 요소에 반영하고, 프리뷰를 다시 렌더링한다(IMGUI의 Repaint 시점에 해당).
        /// CreateGUI 이전(OnEnable 중)에는 아무 것도 하지 않는다.
        /// </summary>
        private void RefreshView()
        {
            if (_previewArea == null)
                return;

            _inputValueLabel.text = _overrideEnabled ? "Override" : "Simulator";
            _sourceCanvasesValueLabel.text = _srcCanvasCount.ToString();
            _previewCanvasesValueLabel.text = _previewCanvasCount.ToString();

            var previewSceneValid = _previewScene.IsValid();
            _canvasesInSceneLabel.style.display = previewSceneValid ? DisplayStyle.Flex : DisplayStyle.None;
            if (previewSceneValid)
            {
                int canvasInScene = _previewScene.GetRootGameObjects()
                    .Sum(r => r.GetComponentsInChildren<Canvas>(true).Length);
                _canvasesInSceneLabel.text = $"Canvases in PreviewScene: {canvasInScene}";
            }

            var hasCamera = _previewCamera != null;
            _cameraEnabledLabel.style.display = hasCamera ? DisplayStyle.Flex : DisplayStyle.None;
            _cameraActiveLabel.style.display = hasCamera ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasCamera)
            {
                _cameraEnabledLabel.text = $"Camera enabled: {_previewCamera.enabled}";
                _cameraActiveLabel.text = $"Camera active: {_previewCamera.gameObject.activeInHierarchy}";
            }

            var effectiveScreenSize = GetEffectiveScreenSize();
            var effectiveSafeArea = GetEffectiveSafeArea();
            var insets = GetSafeAreaInsets(effectiveScreenSize, effectiveSafeArea);
            _appliedScreenField.SetValueWithoutNotify(effectiveScreenSize);
            _appliedSafeAreaField.SetValueWithoutNotify(effectiveSafeArea);
            _insetsValueLabel.text = $"{insets.x:0} / {insets.y:0} / {insets.z:0} / {insets.w:0}";

            _simulatorScreenField.SetValueWithoutNotify(_simScreenSize);
            _simulatorSafeAreaField.SetValueWithoutNotify(_simSafeArea);

            _overrideToggle.SetValueWithoutNotify(_overrideEnabled);
            _overrideScreenField.SetValueWithoutNotify(_overrideScreenSize);
            _overrideSafeAreaField.SetValueWithoutNotify(_overrideSafeArea);
            _overrideScreenField.SetEnabled(_overrideEnabled);
            _overrideSafeAreaField.SetEnabled(_overrideEnabled);

            _staleHelpBox.style.display = !_overrideEnabled && PreviewIsStale()
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            RenderPreview();
            UpdatePreviewLayout();
        }

        private static Label CreateBoldLabel(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        /// <summary>IMGUI <c>EditorGUILayout.LabelField(label, value)</c>와 같은 "라벨 + 값" 한 줄.</summary>
        private static Label AddLabelField(VisualElement parent, string labelText)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var label = new Label(labelText);
            label.style.width = LabelFieldLabelWidth;
            label.style.flexShrink = 0f;
            row.Add(label);

            var value = new Label();
            value.style.flexGrow = 1f;
            row.Add(value);

            parent.Add(row);
            return value;
        }

        private static void SetAbsoluteFill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
        }

        private static VisualElement CreateOverlayBox(Color color)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.backgroundColor = color;
            return box;
        }

        private static Label CreateOverlayLabel(TextAnchor alignment)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.color = Color.white;
            label.style.fontSize = OverlayFontSize;
            label.style.unityTextAlign = alignment;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.marginTop = 0f;
            label.style.marginBottom = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            label.style.paddingTop = 0f;
            label.style.paddingBottom = 0f;
            return label;
        }

        // =====================================================================
        //  헬퍼: 값 계산 / 전체 리빌드
        // =====================================================================

        /// <summary>
        /// 시뮬레이터(Screen) 기준 값을 읽어서 _simScreenSize / _simSafeArea에 저장.
        /// </summary>
        private void RefreshSimulatorValues()
        {
            _simScreenSize = Handles.GetMainGameViewSize();
            _simSafeArea = Screen.safeArea;
        }

        /// <summary>
        /// 실제 프리뷰에 사용할 ScreenSize.
        /// Override ON → 사용자가 입력한 값
        /// Override OFF → 시뮬레이터(Screen) 값
        /// </summary>
        private Vector2 GetEffectiveScreenSize()
        {
            return _overrideEnabled ? _overrideScreenSize : _simScreenSize;
        }

        /// <summary>
        /// 실제 프리뷰에 사용할 SafeArea.
        /// Override ON → 사용자가 입력한 값
        /// Override OFF → 시뮬레이터(Screen) 값
        /// </summary>
        private Rect GetEffectiveSafeArea()
        {
            return _overrideEnabled ? _overrideSafeArea : _simSafeArea;
        }

        /// <summary>
        /// 프리뷰 전체 재구성:
        /// 1) 캔버스 복제
        /// 2) 카메라 설정
        /// 3) SafeAreaRoot에 SafeArea 적용
        /// </summary>
        private void RebuildAll()
        {
            CreatePreviewScene();
            RebuildPreviewFromActiveScene();
            UpdateCameraSettings();
            ApplyPreviewToScene();
            Canvas.ForceUpdateCanvases();
            _builtScreenSize = GetEffectiveScreenSize();
            _builtSafeArea = GetEffectiveSafeArea();
            RefreshView();
        }

        private bool PreviewIsStale()
        {
            var screen = GetEffectiveScreenSize();
            var area = GetEffectiveSafeArea();
            return Vector2.Distance(screen, _builtScreenSize) > 0.1f ||
                   Mathf.Abs(area.x - _builtSafeArea.x) > 0.1f ||
                   Mathf.Abs(area.y - _builtSafeArea.y) > 0.1f ||
                   Mathf.Abs(area.width - _builtSafeArea.width) > 0.1f ||
                   Mathf.Abs(area.height - _builtSafeArea.height) > 0.1f;
        }

        // =====================================================================
        //  PreviewScene 구축/해제
        // =====================================================================

        private void CreatePreviewScene()
        {
            if (_previewScene.IsValid())
                return;

            _previewScene = EditorSceneManager.NewPreviewScene();

            var camGo = new GameObject("SafeAreaPreviewCamera");
            _previewCamera = camGo.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = Color.black;
            _previewCamera.orthographic = true;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 100f;
            _previewCamera.cullingMask = ~0;
            _previewCamera.enabled = true;
            _previewCamera.cameraType = CameraType.Game;

            SceneManager.MoveGameObjectToScene(camGo, _previewScene);

            ulong sceneMask = EditorSceneManager.GetSceneCullingMask(_previewScene);
            _previewCamera.overrideSceneCullingMask = sceneMask;

            UpdateCameraSettings();
        }

        private void DestroyPreviewScene()
        {
            if (_previewCamera != null && _previewCamera.targetTexture != null)
                _previewCamera.targetTexture = null;

            if (_rt != null)
            {
                _rt.Release();
                DestroyImmediate(_rt);
                _rt = null;
            }

            if (_previewScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_previewScene);
            }
        }

        // =====================================================================
        //  Camera / RenderTexture / Preview Draw
        // =====================================================================

        /// <summary>
        /// 카메라를 "논리 해상도"에 맞게 설정.
        /// 1유닛 = 1픽셀, 수직 범위: -H/2 ~ +H/2
        /// </summary>
        private void UpdateCameraSettings()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = GetEffectiveScreenSize();

            if (screenSize.y <= 0) screenSize.y = 1;
            if (screenSize.x <= 0) screenSize.x = screenSize.y;

            _previewCamera.orthographicSize = screenSize.y * 0.5f;
            _previewCamera.aspect = screenSize.x / screenSize.y;
            _previewCamera.transform.position = new Vector3(0, 0, -10);
            _previewCamera.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 현재 유효 화면 크기로 RenderTexture를 준비하고 PreviewScene 카메라를 한 번 렌더링한다.
        /// </summary>
        private void RenderPreview()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = GetEffectiveScreenSize();
            int renderWidth = Mathf.Max(1, (int)screenSize.x);
            int renderHeight = Mathf.Max(1, (int)screenSize.y);

            // RenderTexture 준비
            if (_rt == null || _rt.width != renderWidth || _rt.height != renderHeight)
            {
                if (_previewCamera.targetTexture == _rt)
                    _previewCamera.targetTexture = null;

                if (_rt != null)
                {
                    _rt.Release();
                    DestroyImmediate(_rt);
                    _rt = null;
                }

                _rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
                _rt.Create();
            }

            if (_rt != null)
            {
                Canvas.ForceUpdateCanvases();

                _previewCamera.targetTexture = _rt;
                _previewCamera.pixelRect = new Rect(0, 0, renderWidth, renderHeight);
                _previewCamera.Render();
            }

            if (_previewImage != null)
            {
                _previewImage.image = _rt;
                _previewImage.MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// 프리뷰 영역(오른쪽 남은 공간 전체) 안에 화면 비율을 유지한 프리뷰 사각형을 배치하고 오버레이를 갱신한다.
        /// </summary>
        private void UpdatePreviewLayout()
        {
            if (_previewArea == null)
                return;

            Vector2 screenSize = GetEffectiveScreenSize();
            Rect layoutRect = _previewArea.contentRect;

            if (_previewCamera == null || _rt == null ||
                float.IsNaN(layoutRect.width) || float.IsNaN(layoutRect.height) ||
                layoutRect.width <= 1f || layoutRect.height <= 1f ||
                screenSize.x <= 0f || screenSize.y <= 0f)
            {
                _previewFrame.style.display = DisplayStyle.None;
                return;
            }

            float targetAspect = screenSize.x / screenSize.y;
            float windowAspect = layoutRect.width / layoutRect.height;

            Rect previewRect;

            if (windowAspect > targetAspect)
            {
                // 창이 더 납작 → 높이에 맞추고 좌우 여백
                float height = layoutRect.height;
                float width = height * targetAspect;
                float x = layoutRect.x + (layoutRect.width - width) * 0.5f;
                float y = layoutRect.y;
                previewRect = new Rect(x, y, width, height);
            }
            else
            {
                // 창이 더 세로 → 너비에 맞추고 상하 여백
                float width = layoutRect.width;
                float height = width / targetAspect;
                float x = layoutRect.x;
                float y = layoutRect.y + (layoutRect.height - height) * 0.5f;
                previewRect = new Rect(x, y, width, height);
            }

            _previewFrame.style.display = DisplayStyle.Flex;
            SetElementRect(_previewFrame, previewRect);

            _overlayRoot.style.display = _showSafeAreaOverlay ? DisplayStyle.Flex : DisplayStyle.None;
            if (_showSafeAreaOverlay)
                UpdateSafeAreaOverlay(new Rect(0f, 0f, previewRect.width, previewRect.height), screenSize, GetEffectiveSafeArea());
        }

        private static void SetElementRect(VisualElement element, Rect rect)
        {
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
        }

        private static Vector4 GetSafeAreaInsets(Vector2 screenSize, Rect safeArea)
        {
            var left = Mathf.Max(0f, safeArea.xMin);
            var right = Mathf.Max(0f, screenSize.x - safeArea.xMax);
            var top = Mathf.Max(0f, screenSize.y - safeArea.yMax);
            var bottom = Mathf.Max(0f, safeArea.yMin);
            return new Vector4(left, right, top, bottom);
        }

        internal static Rect CalculatePreviewSafeAreaRect(Rect previewRect, Vector2 screenSize, Rect safeArea)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return previewRect;

            var xMin = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
            var xMax = Mathf.Clamp(safeArea.xMax, xMin, screenSize.x);
            var yMin = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
            var yMax = Mathf.Clamp(safeArea.yMax, yMin, screenSize.y);

            return new Rect(
                previewRect.x + previewRect.width * xMin / screenSize.x,
                previewRect.y + previewRect.height * (1f - yMax / screenSize.y),
                previewRect.width * (xMax - xMin) / screenSize.x,
                previewRect.height * (yMax - yMin) / screenSize.y);
        }

        /// <summary>
        /// 프리뷰 사각형(프레임 로컬 좌표, 좌상단 원점) 기준으로 unsafe 영역·safe area 테두리·배지를 배치한다.
        /// </summary>
        private void UpdateSafeAreaOverlay(Rect previewRect, Vector2 screenSize, Rect safeArea)
        {
            var safeRect = CalculatePreviewSafeAreaRect(previewRect, screenSize, safeArea);
            var insets = GetSafeAreaInsets(screenSize, safeArea);

            UpdateUnsafeArea(
                0,
                new Rect(previewRect.x, previewRect.y, previewRect.width, safeRect.y - previewRect.y),
                $"UNSAFE TOP  {insets.z:0}px");
            UpdateUnsafeArea(
                1,
                new Rect(previewRect.x, safeRect.yMax, previewRect.width, previewRect.yMax - safeRect.yMax),
                $"UNSAFE BOTTOM  {insets.w:0}px");
            UpdateUnsafeArea(
                2,
                new Rect(previewRect.x, safeRect.y, safeRect.x - previewRect.x, safeRect.height),
                $"UNSAFE LEFT\n{insets.x:0}px");
            UpdateUnsafeArea(
                3,
                new Rect(safeRect.xMax, safeRect.y, previewRect.xMax - safeRect.xMax, safeRect.height),
                $"UNSAFE RIGHT\n{insets.y:0}px");

            SetElementRect(_safeAreaBorder, safeRect);

            var badgeRect = new Rect(safeRect.x + 8f, safeRect.y + 8f, 230f, 36f);
            badgeRect.width = Mathf.Min(badgeRect.width, Mathf.Max(0f, safeRect.width - 16f));
            if (badgeRect.width <= 1f)
            {
                _safeAreaBadge.style.display = DisplayStyle.None;
                return;
            }

            _safeAreaBadge.style.display = DisplayStyle.Flex;
            SetElementRect(_safeAreaBadge, badgeRect);
            _safeAreaBadgeLabel.text =
                $"SAFE AREA\n{safeArea.x:0}, {safeArea.y:0}, {safeArea.width:0}, {safeArea.height:0} px";
        }

        private void UpdateUnsafeArea(int index, Rect rect, string label)
        {
            var area = _unsafeAreas[index];
            if (rect.width <= 0f || rect.height <= 0f)
            {
                area.style.display = DisplayStyle.None;
                return;
            }

            area.style.display = DisplayStyle.Flex;
            SetElementRect(area, rect);

            var areaLabel = _unsafeAreaLabels[index];
            var showLabel = rect.width >= 54f && rect.height >= 16f;
            areaLabel.style.display = showLabel ? DisplayStyle.Flex : DisplayStyle.None;
            areaLabel.text = label;
        }

        // =====================================================================
        //  Canvas 복제 / SafeArea 적용
        // =====================================================================

        /// <summary>
        /// 현재 Active Scene의 Canvas들을 PreviewScene으로 복제한다.
        /// </summary>
        private void RebuildPreviewFromActiveScene()
        {
            if (!_previewScene.IsValid())
                CreatePreviewScene();

            // 카메라만 남기고 나머지 정리
            foreach (var root in _previewScene.GetRootGameObjects())
            {
                if (root.name != "SafeAreaPreviewCamera")
                    Object.DestroyImmediate(root);
            }

            _srcCanvasCount = 0;
            _previewCanvasCount = 0;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            var settings = SafeAreaSettings.Resolve();
            var roots = activeScene.GetRootGameObjects();

            foreach (var root in roots)
            {
                var canvases = root.GetComponentsInChildren<Canvas>(true);
                _srcCanvasCount += canvases.Length;

                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.WorldSpace)
                        continue; // 3D UI는 제외

                    var clone = Object.Instantiate(canvas.gameObject);
                    clone.name = canvas.gameObject.name + " (Preview)";
                    clone.SetActive(true);

                    SceneManager.MoveGameObjectToScene(clone, _previewScene);

                    if (clone.TryGetComponent<Canvas>(out var cloneCanvas))
                    {
                        SetupCanvasForPreview(cloneCanvas);
                        SafeAreaPatchCore.EnsureSafeAreaContainer(cloneCanvas, settings);
                        _previewCanvasCount++;
                    }
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// PreviewScene에 맞게 Canvas를 세팅한다.
        /// (SafeAreaRoot가 실제 SafeArea 적용을 담당하므로, Canvas는 전체 화면 기준)
        /// </summary>
        private void SetupCanvasForPreview(Canvas canvas)
        {
            if (canvas == null || _previewCamera == null)
                return;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                canvas.renderMode = RenderMode.ScreenSpaceCamera;

            canvas.worldCamera = _previewCamera;
            canvas.planeDistance = 1f;

            var rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 screenSize = GetEffectiveScreenSize();

                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = screenSize;
                rectTransform.localPosition = Vector3.zero;
            }

            canvas.sortingOrder = 0;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                SetLayerRecursively(canvas.gameObject, uiLayer);

            if (!canvas.gameObject.activeInHierarchy)
                canvas.gameObject.SetActive(true);
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 현재 설정된 safeArea / screenSize를 PreviewScene 안의 공식 uGUI SafeArea와
        /// SafeAreaPadding에만 적용한다.
        /// 원본 씬은 건드리지 않는다.
        /// </summary>
        private void ApplyPreviewToScene()
        {
            if (!_previewScene.IsValid())
                return;

            Rect safeArea = GetEffectiveSafeArea();
            Vector2 screenSize = GetEffectiveScreenSize();

            var roots = _previewScene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var safeAreaComponents = root.GetComponentsInChildren<UnityEngine.UI.SafeArea>(true);
                foreach (var component in safeAreaComponents)
                {
                    // Skip SafeArea components nested inside another SafeArea container:
                    // the outer one already remapped the hierarchy to safe-area anchors,
                    // so applying the screen safe area here again would double-inset.
                    var parent = component.transform.parent;
                    if (parent != null && parent.GetComponentInParent<UnityEngine.UI.SafeArea>(true) != null)
                        continue;

                    ApplyPreview(component, safeArea, screenSize);
                }

                var safeAreaPaddings = root.GetComponentsInChildren<SafeAreaPadding>(true);
                foreach (var sp in safeAreaPaddings)
                {
                    sp.ApplyPreview(safeArea, screenSize);
                }
            }
        }

        private static void ApplyPreview(UnityEngine.UI.SafeArea component, Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            var edges = component.GetReferenceOrientationMappedDirection(component.Edges);
            var min = safeArea.min;
            var max = safeArea.max;

            if ((edges & UnityEngine.UI.SafeArea.SafeAreaMode.Left) == 0)
                min.x = 0f;
            if ((edges & UnityEngine.UI.SafeArea.SafeAreaMode.Right) == 0)
                max.x = screenSize.x;
            if ((edges & UnityEngine.UI.SafeArea.SafeAreaMode.Bottom) == 0)
                min.y = 0f;
            if ((edges & UnityEngine.UI.SafeArea.SafeAreaMode.Top) == 0)
                max.y = screenSize.y;

            var alignment = component.Alignment;
            // Use the orientation of the previewed screen (Override / Simulator input),
            // not the Editor's real Screen.orientation.
            var previewLandscape = screenSize.x > screenSize.y;
            var alignmentFlipped = previewLandscape != IsLandscape(component.ReferenceOrientation);
            var horizontal = alignmentFlipped
                ? UnityEngine.UI.SafeArea.AlignmentMode.CenterVertically
                : UnityEngine.UI.SafeArea.AlignmentMode.CenterHorizontally;
            var vertical = alignmentFlipped
                ? UnityEngine.UI.SafeArea.AlignmentMode.CenterHorizontally
                : UnityEngine.UI.SafeArea.AlignmentMode.CenterVertically;

            if ((alignment & horizontal) != 0)
            {
                var inset = Mathf.Max(min.x, screenSize.x - max.x);
                min.x = inset;
                max.x = screenSize.x - inset;
            }

            if ((alignment & vertical) != 0)
            {
                var inset = Mathf.Max(min.y, screenSize.y - max.y);
                min.y = inset;
                max.y = screenSize.y - inset;
            }

            var rectTransform = (RectTransform)component.transform;
            rectTransform.anchorMin = new Vector2(min.x / screenSize.x, min.y / screenSize.y);
            rectTransform.anchorMax = new Vector2(max.x / screenSize.x, max.y / screenSize.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static bool IsLandscape(ScreenOrientation orientation)
        {
            return orientation is ScreenOrientation.LandscapeLeft or ScreenOrientation.LandscapeRight;
        }
    }
}
#endif
