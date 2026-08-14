using UnityEngine;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// RectTransform을 Safe Area에 맞게 자동으로 맞춰주는 컴포넌트. 런타임/에디터 모두
    /// <see cref="SafeAreaWatcher.SafeAreaChanged"/> 이벤트를 구독해서 갱신한다. Safe Area Preview
    /// Window(Editor)는 원본 Scene의 이 인스턴스를 절대 건드리지 않고, PreviewScene에 복제한
    /// 인스턴스에서만 <see cref="ApplyPreview"/>를 호출해 격리된 미리보기를 만든다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_applyLeft")] private bool applyLeft = true;
        [SerializeField, FormerlySerializedAs("_applyRight")] private bool applyRight = true;
        [SerializeField, FormerlySerializedAs("_applyTop")] private bool applyTop = true;
        [SerializeField, FormerlySerializedAs("_applyBottom")] private bool applyBottom = true;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;

            // 현재 SafeArea 기준으로 한 번 즉시 적용 (런타임 기준)
            ApplySafeArea(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplySafeArea(safeArea);
        }

        /// <summary>
        /// 런타임/일반용: SafeAreaUtility의 ScreenSize 기준으로 적용.
        /// </summary>
        private void ApplySafeArea(Rect safeArea)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            Vector2 screenSize = SafeAreaUtility.GetScreenSize();

#if UNITY_EDITOR
            // 에디터에서 GameView가 없어서 0,0일 수 있는 상황 방어
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                var canvas = _rectTransform.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var rect = canvas.pixelRect;
                    if (rect.width > 0f && rect.height > 0f)
                    {
                        screenSize = rect.size;
                    }
                }
            }
#endif

            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            ApplyInternal(safeArea, screenSize);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Safe Area Preview Window 전용 진입점. PreviewScene에 복제된 인스턴스에서만 호출되며,
        /// <see cref="SafeAreaWatcher"/> 이벤트를 거치지 않고 전달받은 safeArea/screenSize를 그대로
        /// 적용한다. 원본 Scene의 SafeAreaRoot는 이 메서드가 호출되지 않으므로 Preview 조작에
        /// 영향받지 않는다.
        /// </summary>
        internal void ApplyPreview(Rect safeArea, Vector2 screenSize)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            ApplyInternal(safeArea, screenSize);
        }
#endif

        private void ApplyInternal(Rect safeArea, Vector2 screenSize)
        {
            SafeAreaUtility.GetInsets(safeArea, screenSize,
                out float left, out float right, out float top, out float bottom);

            float xMin = 0f;
            float xMax = screenSize.x;
            float yMin = 0f;
            float yMax = screenSize.y;

            if (applyLeft)
                xMin = left;
            if (applyRight)
                xMax = screenSize.x - right;
            if (applyBottom)
                yMin = bottom;
            if (applyTop)
                yMax = screenSize.y - top;

            Rect target = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            Vector2 anchorMin = new Vector2(
                target.xMin / screenSize.x,
                target.yMin / screenSize.y
            );
            Vector2 anchorMax = new Vector2(
                target.xMax / screenSize.x,
                target.yMax / screenSize.y
            );

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
