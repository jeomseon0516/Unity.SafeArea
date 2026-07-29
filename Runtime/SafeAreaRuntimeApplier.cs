// Assets/Jeomseon/SafeArea/Runtime/SafeAreaRuntimeApplier.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// 런타임에만 Canvas들을 SafeAreaRoot로 감싸는 자동 패처.
    /// (에디터에서 씬 구조를 영구 수정하지 않음)
    /// </summary>
    public static class SafeAreaRuntimeApplier
    {
        // TODO(architecture): 모든 Canvas를 전역 검색해 계층을 자동 변경하는 대신,
        // ScriptableObject 설정이나 명시적 부트스트랩으로 적용 대상을 선택할 수 있게 개선합니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToAllCanvases();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToAllCanvases();
        }

        /// <summary>
        /// 현재 로드된 모든 Canvas에 SafeAreaRoot를 붙인다.
        /// </summary>
        public static void ApplyToAllCanvases()
        {
#if UNITY_2023_1_OR_NEWER
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var canvases = Object.FindObjectsOfType<Canvas>(true);
#endif
            foreach (var canvas in canvases)
            {
                SafeAreaPatchCore.EnsureSafeAreaRoot(canvas);
            }
        }
    }
}
