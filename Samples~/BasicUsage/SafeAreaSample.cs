using Jeomseon.SafeArea;
using UnityEngine;

namespace Jeomseon.Samples.SafeArea
{
    public sealed class SafeAreaSample : MonoBehaviour
    {
        [ContextMenu("Safe Area 출력")]
        private void Print()
        {
            Rect safeArea = SafeAreaUtility.GetSafeArea();
            Vector2 screenSize = SafeAreaUtility.GetScreenSize();
            SafeAreaUtility.GetInsets(
                safeArea, screenSize,
                out float left, out float right, out float top, out float bottom);

            Debug.Log($"Safe Area Insets L:{left} R:{right} T:{top} B:{bottom}");
        }
    }
}
