using NUnit.Framework;
using UnityEngine;
using Jeomseon.Unity.SafeArea;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaUtilityTests
    {
        [Test]
        public void GetInsets_CalculatesInsetsFromScreenAndSafeArea()
        {
            SafeAreaUtility.GetInsets(
                new Rect(10f, 20f, 970f, 1920f),
                new Vector2(1000f, 2000f),
                out float left,
                out float right,
                out float top,
                out float bottom);

            Assert.AreEqual(10f, left);
            Assert.AreEqual(20f, right);
            Assert.AreEqual(60f, top);
            Assert.AreEqual(20f, bottom);
        }
    }
}
