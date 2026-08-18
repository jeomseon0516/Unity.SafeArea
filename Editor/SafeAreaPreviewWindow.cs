// Assets/Jeomseon/SafeArea/Editor/SafeAreaPreviewWindow.cs
#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        // ----- Preview용 씬/카메라/RT -----
        private Scene _previewScene;
        private Camera _previewCamera;
        private RenderTexture _rt;

        // 디버그용
        private int _srcCanvasCount;
        private int _previewCanvasCount;

        // =====================================================================
        //  Menu
        // =====================================================================

        private const float ControlPanelWidth = 340f;

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
                Repaint();
            }
        }

        // =====================================================================
        //  GUI
        // =====================================================================

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Safe Area Preview (PreviewScene)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 리빌드는 모든 레이아웃 Scope가 닫힌 뒤 마지막에 한 번만 수행한다(같은 프레임에 여러 번
            // 리빌드하지 않기 위한 플래그일 뿐 — HorizontalScope/VerticalScope의 Dispose가 Begin/End를
            // 항상 짝지어 호출해주므로 그룹 균형 자체는 걱정할 필요 없음).
            bool needsRebuild = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                // ----- 왼쪽: 컨트롤 패널 -----
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(ControlPanelWidth)))
                {
                    EditorGUILayout.LabelField($"Source canvases  : {_srcCanvasCount}");
                    EditorGUILayout.LabelField($"Preview canvases : {_previewCanvasCount}");

                    if (_previewScene.IsValid())
                    {
                        int canvasInScene = _previewScene.GetRootGameObjects()
                            .Sum(r => r.GetComponentsInChildren<Canvas>(true).Length);
                        EditorGUILayout.LabelField($"Canvases in PreviewScene: {canvasInScene}");
                    }

                    if (_previewCamera != null)
                    {
                        EditorGUILayout.LabelField($"Camera enabled: {_previewCamera.enabled}");
                        EditorGUILayout.LabelField($"Camera active: {_previewCamera.gameObject.activeInHierarchy}");
                    }

                    EditorGUILayout.Space();

                    // 시뮬레이터(현재 GameView) 기준 값 디스플레이 (읽기 전용)
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Vector2Field("Simulator Screen (px)", _simScreenSize);
                        EditorGUILayout.RectField("Simulator SafeArea (px)", _simSafeArea);
                    }

                    EditorGUILayout.Space();

                    // ----- Override 토글 -----
                    bool prevOverride = _overrideEnabled;
                    _overrideEnabled = EditorGUILayout.Toggle("Override Safe Area", _overrideEnabled);

                    if (prevOverride != _overrideEnabled)
                    {
                        // Override를 껐을 때는, 필드를 시뮬레이터 상태로 맞춰두면 UX가 더 직관적
                        if (!_overrideEnabled)
                        {
                            _overrideScreenSize = _simScreenSize;
                            _overrideSafeArea = _simSafeArea;
                        }

                        // Override 상태가 바뀌면, 현재 설정에 맞춰 한 번만 전체 리빌드
                        needsRebuild = true;
                    }
                    else
                    {
                        EditorGUILayout.Space();

                        // ----- Override 값 편집 (Override ON일 때만 수정 가능) -----
                        using (new EditorGUI.DisabledScope(!_overrideEnabled))
                        {
                            _overrideScreenSize = EditorGUILayout.Vector2Field("Override Screen Size (px)", _overrideScreenSize);
                            _overrideSafeArea = EditorGUILayout.RectField("Override Safe Area (px)", _overrideSafeArea);
                        }

                        if (GUILayout.Button("Apply & Rebuild Preview"))
                        {
                            // Override ON이면 사용자가 입력한 값을, OFF이면 시뮬레이터 값을 사용하여
                            // 한 번만 전체 리빌드
                            needsRebuild = true;
                        }
                    }
                }

                // ----- 오른쪽: 프리뷰 렌더 (남은 가로 공간 전체) -----
                using (new EditorGUILayout.VerticalScope())
                {
                    if (!needsRebuild)
                        DrawPreview();
                }
            }

            if (needsRebuild)
                RebuildAll();
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
            Repaint();
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
            _previewCamera.clearFlags = CameraClearFlags.Skybox;   // 요청대로 Skybox
            _previewCamera.backgroundColor = Color.gray;
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

        private void DrawPreview()
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

            // 지금까지 그린 UI 아래의 남은 영역 전체를 프리뷰로 사용
            Rect layoutRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            if (layoutRect.width <= 1f || layoutRect.height <= 1f || _rt == null)
                return;

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

            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(previewRect, _rt, ScaleMode.StretchToFill, false);
            }
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
                        SafeAreaPatchCore.EnsureSafeAreaRoot(cloneCanvas, settings);
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
        /// 현재 설정된 safeArea / screenSize를 PreviewScene 안의 SafeAreaRoot들에게만 적용.
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
                var safeAreaRoots = root.GetComponentsInChildren<SafeAreaRoot>(true);
                foreach (var sr in safeAreaRoots)
                {
                    sr.ApplyPreview(safeArea, screenSize);
                }

                var safeAreaPaddings = root.GetComponentsInChildren<SafeAreaPadding>(true);
                foreach (var sp in safeAreaPaddings)
                {
                    sp.ApplyPreview(safeArea, screenSize);
                }
            }
        }
    }
}
#endif
