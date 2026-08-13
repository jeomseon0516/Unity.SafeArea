using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Jeomseon.Unity.SafeArea;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaPaddingTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SafeAreaPaddingTestTarget", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);

            SafeAreaWatcher.ResetStaticStateForNewSession();
        }

        [Test]
        public void ManualPaddingEditAfterActivation_IsPreservedOnNextApply()
        {
            var layout = _go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);

            var safeAreaPadding = _go.AddComponent<SafeAreaPadding>();

            var appliedTop = layout.padding.top;

            // 사용자가 Inspector에서 LayoutGroup.padding.top을 직접 편집했다고 가정합니다.
            layout.padding = new RectOffset(
                layout.padding.left,
                layout.padding.right,
                appliedTop + 50,
                layout.padding.bottom);

            safeAreaPadding.SendMessage("OnValidate");

            Assert.AreEqual(appliedTop + 50, layout.padding.top,
                "사용자가 직접 수정한 padding 값이 다음 적용에서 되돌아가면 안 됨 " +
                "(_originalPadding이 최초 1회만 캐싱되던 버그의 회귀 테스트)");
        }

        [Test]
        public void SafeAreaChangeAfterManualEdit_AddsInsetOnTopOfEditedBaseline()
        {
            var layout = _go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);

            var safeAreaPadding = _go.AddComponent<SafeAreaPadding>();
            var appliedTopBeforeEdit = layout.padding.top;

            layout.padding = new RectOffset(
                layout.padding.left,
                layout.padding.right,
                appliedTopBeforeEdit + 50,
                layout.padding.bottom);
            safeAreaPadding.SendMessage("OnValidate");

            var appliedTopAfterEdit = layout.padding.top;

            // 편집 이후 SafeArea 값이 다시 브로드캐스트돼도(값 자체는 테스트 환경에서 동일하더라도)
            // 새 기준선(appliedTopAfterEdit 이전의 원본)이 유지된 채로 재적용되어야 합니다.
            SafeAreaWatcher.ForceUpdate();

            Assert.AreEqual(appliedTopAfterEdit, layout.padding.top,
                "직전에 사용자가 편집한 기준값 위에 동일한 inset이 다시 적용되어야 하며, " +
                "편집 이전 값으로 되돌아가면 안 됨");
        }
    }
}
