// Assets/Jeomseon/SafeArea/Runtime/SafeAreaWatcher.cs
using UnityEngine;
using System;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// Static watcher for safe area / screen size changes.
    /// - 런타임: Application.onBeforeRender에서 자동 체크
    /// - 에디터: SafeAreaPreviewWindow 등에서 ForceUpdate()를 호출해 수동 트리거
    /// 
    /// 이 클래스는 카메라를 직접 건드리지 않고,
    /// SafeAreaChanged 이벤트만 브로드캐스트한다.
    /// 구독자는 SafeAreaRoot, SafeAreaPadding 등.
    /// </summary>
    public static class SafeAreaWatcher
    {
        /// <summary>
        /// SafeArea 또는 화면 크기가 변경되었을 때 호출되는 이벤트입니다.
        /// Rect 인자는 새 SafeArea 값(Screen 좌표 기준, px)입니다.
        /// </summary>
        public static event Action<Rect> SafeAreaChanged;

        private static bool _initialized;
        private static Rect _lastSafeArea;
        private static Vector2 _lastScreenSize;

        /// <summary>
        /// Domain Reload를 끈 Enter Play Mode에서도 매 Play 세션 진입 시 무조건 실행됩니다(static 필드
        /// 초기화와 달리 <see cref="RuntimeInitializeOnLoadMethod"/> 메서드는 Domain Reload 여부와
        /// 무관하게 항상 재실행됨). 이전 세션에서 남은 구독자·초기화 플래그·캐시값을 전부 지워
        /// <see cref="InitOnPlay"/>가 이번 세션 기준으로 다시 초기화하도록 보장합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStaticStateForNewSession()
        {
            Application.onBeforeRender -= CheckForChanges;
            SafeAreaChanged = null;
            _initialized = false;
            _lastSafeArea = default;
            _lastScreenSize = default;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnPlay()
        {
            InitInternal();
        }

        /// <summary>
        /// 외부에서 명시적으로 초기화가 필요할 때 호출 가능.
        /// (보통은 RuntimeInitializeOnLoadMethod로 충분함)
        /// </summary>
        public static void Init()
        {
            InitInternal();
        }

        private static void InitInternal()
        {
            if (_initialized)
                return;

            _initialized = true;

            _lastSafeArea = SafeAreaUtility.GetSafeArea();
            _lastScreenSize = SafeAreaUtility.GetScreenSize();

            // 런타임에서 매 프레임 직전에 호출되는 훅
            Application.onBeforeRender -= CheckForChanges;
            Application.onBeforeRender += CheckForChanges;

            // 초기 상태도 한 번 브로드캐스트 해주고 싶다면 아래 주석 해제
            // SafeAreaChanged?.Invoke(_lastSafeArea);
        }

        private static void CheckForChanges()
        {
            var safe = SafeAreaUtility.GetSafeArea();
            var size = SafeAreaUtility.GetScreenSize();

            if (safe != _lastSafeArea || size != _lastScreenSize)
            {
                _lastSafeArea = safe;
                _lastScreenSize = size;

                SafeAreaChanged?.Invoke(_lastSafeArea);
            }
        }

        /// <summary>
        /// 외부(에디터 툴 등)에서 강제로 "현재 SafeArea 상태"를 브로드캐스트하고 싶을 때 사용.
        /// 에디터에서도 안전하게 호출 가능.
        /// </summary>
        public static void ForceUpdate()
        {
            InitInternal(); // onBeforeRender 등록까지 보장

            _lastSafeArea = SafeAreaUtility.GetSafeArea();
            _lastScreenSize = SafeAreaUtility.GetScreenSize();

            SafeAreaChanged?.Invoke(_lastSafeArea);
        }
    }
}
