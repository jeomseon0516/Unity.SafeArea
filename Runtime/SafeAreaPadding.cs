using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            ApplyPadding(safeArea);
        }

        private void ApplyPadding(Rect safeArea)
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<LayoutGroup>();

            if (!_initializedOriginal)
                CacheOriginalPadding();

            Vector2 screenSize = SafeAreaUtility.GetScreenSize();
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(_layoutGroup, "Apply Safe Area Padding");
#endif

            _layoutGroup.padding.left = padLeft;
            _layoutGroup.padding.right = padRight;
            _layoutGroup.padding.top = padTop;
            _layoutGroup.padding.bottom = padBottom;
            _lastAppliedPadding = new RectOffset(padLeft, padRight, padTop, padBottom);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(_layoutGroup);
#endif

            LayoutRebuilder.MarkLayoutForRebuild(_layoutGroup.transform as RectTransform);
        }
    }
}
