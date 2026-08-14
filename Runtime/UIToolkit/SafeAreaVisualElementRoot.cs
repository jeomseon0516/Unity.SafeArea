using UnityEngine;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.SafeArea.UIToolkit
{
    /// <summary>
    /// <see cref="SafeArea.SafeAreaRoot"/>의 UI Toolkit 대응. RectTransform의 anchor 대신
    /// <see cref="Position.Absolute"/> + <c>left</c>/<c>right</c>/<c>top</c>/<c>bottom</c> 인셋으로
    /// 지정한 <see cref="VisualElement"/>의 박스 자체를 안전 영역 크기로 줄입니다.
    /// <see cref="SafeAreaVisualElementPadding"/>과 달리 배경도 함께 줄어듭니다(padding은 배경을
    /// 그대로 두고 내부 콘텐츠만 밀어 넣습니다 — 웹의 <c>env(safe-area-inset-*)</c> padding
    /// 관례와 동일). 화면 전체를 안전 영역에 맞춰 실제로 줄여야 하면 이 컴포넌트를, 배경은 그대로
    /// 두고 콘텐츠만 안쪽으로 밀면 되면 <see cref="SafeAreaVisualElementPadding"/>을 씁니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SafeAreaVisualElementRoot : MonoBehaviour
    {
        [SerializeField] private string targetElementName;
        [SerializeField] private bool useLeft = true;
        [SerializeField] private bool useRight = true;
        [SerializeField] private bool useTop = true;
        [SerializeField] private bool useBottom = true;

        private UIDocument _document;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;
            ApplyToTarget(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplyToTarget(safeArea);
        }

        private void ApplyToTarget(in Rect safeArea)
        {
            var target = ResolveTarget();
            if (target == null)
                return;

            SafeAreaUtility.GetInsets(safeArea, SafeAreaUtility.GetScreenSize(),
                out float left, out float right, out float top, out float bottom);

            ApplyInsets(target, left, right, top, bottom, useLeft, useRight, useTop, useBottom);
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
        /// 순수 적용 로직. <see cref="UIDocument"/>/panel 없이도 단위 테스트가 가능합니다.
        /// </summary>
        internal static void ApplyInsets(
            VisualElement target,
            float insetLeft, float insetRight, float insetTop, float insetBottom,
            bool useLeft, bool useRight, bool useTop, bool useBottom)
        {
            target.style.position = Position.Absolute;
            target.style.left = useLeft ? insetLeft : 0f;
            target.style.right = useRight ? insetRight : 0f;
            target.style.top = useTop ? insetTop : 0f;
            target.style.bottom = useBottom ? insetBottom : 0f;
        }
    }
}
