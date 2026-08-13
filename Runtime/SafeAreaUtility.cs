// Assets/Jeomseon/SafeArea/Runtime/SafeAreaUtility.cs
using UnityEngine;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// Centralized access to Safe Area and screen size.
    /// - 런타임: Screen.safeArea / Screen.width / Screen.height 사용
    /// - 에디터 프리뷰: SafeAreaPreviewWindow가 SafeAreaRoot.ApplyPreview로 직접 값을 주입
    /// </summary>
    public static class SafeAreaUtility
    {
        /// <summary>
        /// 현재 Safe Area를 반환한다.
        /// </summary>
        public static Rect GetSafeArea()
        {
            return Screen.safeArea;
        }

        /// <summary>
        /// 현재 화면 크기(px)를 반환한다.
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>
        /// SafeArea와 ScreenSize를 기반으로
        /// 각 방향별 인셋(left, right, top, bottom)을 계산한다. (px 단위)
        /// </summary>
        public static void GetInsets(in Rect safeArea, in Vector2 screenSize,
            out float left, out float right, out float top, out float bottom)
        {
            left = safeArea.xMin;
            right = Mathf.Max(0f, screenSize.x - safeArea.xMax);
            bottom = safeArea.yMin;
            top = Mathf.Max(0f, screenSize.y - safeArea.yMax);
        }
    }
}
