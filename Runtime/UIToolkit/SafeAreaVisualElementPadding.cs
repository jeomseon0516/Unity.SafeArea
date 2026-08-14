using UnityEngine;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.SafeArea.UIToolkit
{
    /// <summary>
    /// <see cref="SafeArea.SafeAreaPadding"/>의 UI Toolkit 대응. 지정한 <see cref="VisualElement"/>의
    /// padding에 안전 영역 인셋을 더해 콘텐츠만 안쪽으로 밀어 넣습니다(배경은 그대로 유지 — 웹의
    /// <c>env(safe-area-inset-*)</c> padding 관례와 동일). 박스 자체(배경 포함)를 안전 영역 크기로
    /// 실제로 줄여야 하면 <see cref="SafeAreaVisualElementRoot"/>를 씁니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SafeAreaVisualElementPadding : MonoBehaviour
    {
        [SerializeField] private string targetElementName;
        [SerializeField] private bool useLeft;
        [SerializeField] private bool useRight;
        [SerializeField] private bool useTop = true;
        [SerializeField] private bool useBottom;
        [SerializeField] private float basePaddingLeft;
        [SerializeField] private float basePaddingRight;
        [SerializeField] private float basePaddingTop;
        [SerializeField] private float basePaddingBottom;

        private UIDocument _document;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;
            ApplyPadding(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplyPadding(safeArea);
        }

        private void ApplyPadding(in Rect safeArea)
        {
            var target = ResolveTarget();
            if (target == null)
                return;

            SafeAreaUtility.GetInsets(safeArea, SafeAreaUtility.GetScreenSize(),
                out float left, out float right, out float top, out float bottom);

            ApplyInsets(target, left, right, top, bottom,
                useLeft, useRight, useTop, useBottom,
                basePaddingLeft, basePaddingRight, basePaddingTop, basePaddingBottom);
        }

        private VisualElement ResolveTarget()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();

            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
                return null;

            return string.IsNullOrEmpty(targetElementName) ? root : root.Q(targetElementName);
        }

        /// <summary>
        /// 순수 적용 로직. <see cref="UIDocument"/>/panel 없이도 단위 테스트가 가능하도록
        /// <see cref="VisualElement"/>와 이미 계산된 inset 값만 받습니다.
        /// </summary>
        internal static void ApplyInsets(
            VisualElement target,
            float insetLeft, float insetRight, float insetTop, float insetBottom,
            bool useLeft, bool useRight, bool useTop, bool useBottom,
            float basePaddingLeft, float basePaddingRight, float basePaddingTop, float basePaddingBottom)
        {
            target.style.paddingLeft = useLeft ? basePaddingLeft + insetLeft : basePaddingLeft;
            target.style.paddingRight = useRight ? basePaddingRight + insetRight : basePaddingRight;
            target.style.paddingTop = useTop ? basePaddingTop + insetTop : basePaddingTop;
            target.style.paddingBottom = useBottom ? basePaddingBottom + insetBottom : basePaddingBottom;
        }
    }
}
