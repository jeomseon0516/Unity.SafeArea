using UnityEngine;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.SafeArea.UIToolkit
{
    /// <summary>
    /// UI Toolkit(<see cref="VisualElement"/>) 대응. <see cref="SafeArea.SafeAreaRoot"/>/
    /// <see cref="SafeArea.SafeAreaPadding"/>는 <c>RectTransform</c>/<c>LayoutGroup</c> 기반이라
    /// uGUI 전용이며 <see cref="UIDocument"/>에는 적용되지 않아 별도로 제공합니다. VisualElement는
    /// anchor 개념이 없어(flex 기반 레이아웃) padding 하나로 "화면 전체를 안쪽으로 밀기"(모든 방향
    /// 사용)와 "가장자리 일부만 밀기"(예: 상단 헤더)를 둘 다 표현합니다.
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

        private void ApplyPadding(Rect safeArea)
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
