using System.Collections;
using Jeomseon.Unity.SafeArea.UIToolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaVisualElementLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator Root_MissingPanelSettings_DoesNotThrowDuringEnableDisable()
        {
            var gameObject = new GameObject(
                "SafeAreaVisualElementRootTestTarget",
                typeof(UIDocument));
            SafeAreaVisualElementRoot component =
                gameObject.AddComponent<SafeAreaVisualElementRoot>();

            yield return null;

            component.enabled = false;
            yield return null;
            component.enabled = true;
            yield return null;

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Padding_MissingPanelSettings_DoesNotThrowDuringEnableDisable()
        {
            var gameObject = new GameObject(
                "SafeAreaVisualElementPaddingTestTarget",
                typeof(UIDocument));
            SafeAreaVisualElementPadding component =
                gameObject.AddComponent<SafeAreaVisualElementPadding>();

            yield return null;

            component.enabled = false;
            yield return null;
            component.enabled = true;
            yield return null;

            Object.Destroy(gameObject);
            yield return null;
        }
    }
}
