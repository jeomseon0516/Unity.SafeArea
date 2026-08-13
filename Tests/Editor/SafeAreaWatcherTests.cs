using NUnit.Framework;
using UnityEngine;
using Jeomseon.Unity.SafeArea;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaWatcherTests
    {
        [TearDown]
        public void TearDown()
        {
            SafeAreaWatcher.ResetStaticStateForNewSession();
        }

        [Test]
        public void ResetStaticStateForNewSession_DropsSubscribersFromPreviousSession()
        {
            var invocationCount = 0;
            SafeAreaWatcher.SafeAreaChanged += _ => invocationCount++;

            SafeAreaWatcher.ForceUpdate();
            Assert.AreEqual(1, invocationCount,
                "sanity check: subscriber should receive the broadcast before reset");

            SafeAreaWatcher.ResetStaticStateForNewSession();
            SafeAreaWatcher.ForceUpdate();

            Assert.AreEqual(1, invocationCount,
                "a subscriber registered before the reset must not be invoked after it " +
                "(Domain Reload 비활성화 시 이전 Play 세션의 죽은 구독자가 남아있으면 안 됨)");
        }

        [Test]
        public void ResetStaticStateForNewSession_AllowsReinitializationInNewSession()
        {
            SafeAreaWatcher.ForceUpdate();
            SafeAreaWatcher.ResetStaticStateForNewSession();

            var invocationCount = 0;
            SafeAreaWatcher.SafeAreaChanged += _ => invocationCount++;

            SafeAreaWatcher.ForceUpdate();

            Assert.AreEqual(1, invocationCount,
                "a subscriber registered in the new session must still receive broadcasts " +
                "(reset이 초기화 자체를 영구히 막아서는 안 됨)");
        }

        [Test]
        public void ForceUpdate_RefreshesCachedSafeAreaAndScreenSizeAfterReset()
        {
            SafeAreaWatcher.ForceUpdate();
            SafeAreaWatcher.ResetStaticStateForNewSession();

            Rect? received = null;
            SafeAreaWatcher.SafeAreaChanged += rect => received = rect;

            SafeAreaWatcher.ForceUpdate();

            Assert.IsTrue(received.HasValue,
                "재초기화 이후에도 ForceUpdate가 현재 SafeArea 값을 정상적으로 브로드캐스트해야 함");
        }
    }
}
