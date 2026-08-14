using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// LayoutGroup의 padding에 Safe Area 인셋을 더해주는 컴포넌트.
    /// - 에디터/런타임 모두 동작(ExecuteAlways)
    /// - Top/Bottom/Left/Right 방향별로 적용 여부 선택 가능.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LayoutGroup))]
    public sealed class SafeAreaPadding : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_useLeft")] private bool useLeft;
        [SerializeField, FormerlySerializedAs("_useRight")] private bool useRight;
        [SerializeField, FormerlySerializedAs("_useTop")] private bool useTop = true;
        [SerializeField, FormerlySerializedAs("_useBottom")] private bool useBottom;

        private LayoutGroup _layoutGroup;
        private RectOffset _originalPadding;
        private RectOffset _lastAppliedPadding;
        private Canvas _canvas;
        private bool _initializedOriginal;

        private void Awake()
        {
            _layoutGroup = GetComponent<LayoutGroup>();
            CacheOriginalPadding();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void CacheOriginalPadding()
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<LayoutGroup>();

            if (_layoutGroup == null)
                return;

            var current = _layoutGroup.padding;

            if (!_initializedOriginal)
            {
                _originalPadding = new RectOffset(current.left, current.right, current.top, current.bottom);
                _initializedOriginal = true;
                return;
            }

            // 마지막으로 이 컴포넌트가 적용한 값과 현재 값이 다르면, 그 사이에 사용자가 Inspector에서
            // padding을 직접 편집한 것입니다. 편집된 만큼(delta)을 원본 기준선에도 반영해 다음 적용 시
            // 사용자의 편집이 되돌아가지 않도록 합니다.
            if (_lastAppliedPadding != null && !PaddingEquals(current, _lastAppliedPadding))
            {
                _originalPadding = new RectOffset(
                    _originalPadding.left + (current.left - _lastAppliedPadding.left),
                    _originalPadding.right + (current.right - _lastAppliedPadding.right),
                    _originalPadding.top + (current.top - _lastAppliedPadding.top),
                    _originalPadding.bottom + (current.bottom - _lastAppliedPadding.bottom)
                );
            }
        }

        private static bool PaddingEquals(RectOffset a, RectOffset b)
        {
            return a.left == b.left && a.right == b.right && a.top == b.top && a.bottom == b.bottom;
        }

        private void OnEnable()
        {
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;
            CacheOriginalPadding();
            OnSafeAreaChanged(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            _layoutGroup = GetComponent<LayoutGroup>();
            _canvas = GetComponentInParent<Canvas>();
            CacheOriginalPadding();

            OnSafeAreaChanged(SafeAreaUtility.GetSafeArea());
        }
#endif

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplyPadding(safeArea, SafeAreaUtility.GetScreenSize());
        }

#if UNITY_EDITOR
        /// <summary>
        /// Safe Area Preview Window 전용 진입점. PreviewScene에 복제된 인스턴스에서만 호출되며,
        /// <see cref="SafeAreaWatcher"/> 이벤트를 거치지 않고 전달받은 safeArea/screenSize를 그대로
        /// 적용한다. 원본 Scene의 SafeAreaPadding은 이 메서드가 호출되지 않으므로 Preview 조작에
        /// 영향받지 않는다.
        /// </summary>
        internal void ApplyPreview(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            ApplyPadding(safeArea, screenSize);
        }
#endif

        private void ApplyPadding(Rect safeArea, Vector2 screenSize)
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<LayoutGroup>();

            if (!_initializedOriginal)
                CacheOriginalPadding();

            SafeAreaUtility.GetInsets(safeArea, screenSize,
                out float left, out float right, out float top, out float bottom);

            float scaleFactor = 1f;
            if (_canvas != null)
            {
                var scaler = _canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaleFactor = scaler.scaleFactor;
                }
            }

            int padLeft = _originalPadding.left;
            int padRight = _originalPadding.right;
            int padTop = _originalPadding.top;
            int padBottom = _originalPadding.bottom;

            if (useLeft)
                padLeft = _originalPadding.left + Mathf.RoundToInt(left / scaleFactor);
            if (useRight)
                padRight = _originalPadding.right + Mathf.RoundToInt(right / scaleFactor);
            if (useTop)
                padTop = _originalPadding.top + Mathf.RoundToInt(top / scaleFactor);
            if (useBottom)
                padBottom = _originalPadding.bottom + Mathf.RoundToInt(bottom / scaleFactor);

            // SafeAreaWatcher.ForceUpdate()는 값이 실제로 바뀌었는지 확인하지 않고 매번
            // SafeAreaChanged를 방송한다(에디터에서 SafeAreaPreviewWindow가 마우스 움직임 등으로
            // 잦아진 EditorApplication.update tick마다 호출하는 경우 포함). 계산 결과가 이미 적용된
            // 값과 같으면 LayoutRebuilder를 또 돌릴 이유가 없다 — 특히 ContentSizeFitter가 붙은
            // 대상에서 불필요한 반복 relayout이 깜빡임으로 보이는 걸 막는다.
            if (_lastAppliedPadding != null &&
                _lastAppliedPadding.left == padLeft && _lastAppliedPadding.right == padRight &&
                _lastAppliedPadding.top == padTop && _lastAppliedPadding.bottom == padBottom)
            {
                return;
            }

            _layoutGroup.padding.left = padLeft;
            _layoutGroup.padding.right = padRight;
            _layoutGroup.padding.top = padTop;
            _layoutGroup.padding.bottom = padBottom;
            _lastAppliedPadding = new RectOffset(padLeft, padRight, padTop, padBottom);

            LayoutRebuilder.MarkLayoutForRebuild(_layoutGroup.transform as RectTransform);
        }
    }
}
