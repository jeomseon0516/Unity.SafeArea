// Assets/Jeomseon/SafeArea/Runtime/SafeAreaRuntimeApplier.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// 런타임에만 Canvas들을 SafeAreaRoot로 감싸는 자동 패처.
    /// (에디터에서 씬 구조를 영구 수정하지 않음)
    /// </summary>
    public static class SafeAreaRuntimeApplier
    {
        /// <summary>
        /// SafeAreaSettings.AutoPatchRuntimeCanvases가 true인 프로젝트에서만 씬 로드마다
        /// 자동으로 모든 Canvas를 패치한다(기본값 false, 옵트인). 꺼져 있으면 아무것도 구독하지
        /// 않으며, ApplyToAllCanvases()를 통한 명시적 수동 호출은 이 설정과 무관하게 항상 동작한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!SafeAreaSettings.Resolve().AutoPatchRuntimeCanvases)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToAllCanvases();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToAllCanvases();
        }

        /// <summary>
        /// 현재 로드된 모든 Canvas에 SafeAreaRoot를 붙인다. SafeAreaSettings.AutoPatchRuntimeCanvases
        /// 설정과 무관하게 항상 동작하는 명시적 진입점이다.
        /// </summary>
        public static void ApplyToAllCanvases()
        {
            var settings = SafeAreaSettings.Resolve();
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var canvas in canvases)
            {
                SafeAreaPatchCore.EnsureSafeAreaRoot(canvas, settings);
            }
        }
    }
}
