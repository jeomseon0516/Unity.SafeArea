using UnityEngine;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// SafeAreaPatchCore가 Canvas를 패치할 때 따르는 정책.
    /// "Resources/SafeAreaSettings" 에셋으로 프로젝트별 오버라이드가 가능하며, 없으면 기본값을 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SafeAreaSettings", menuName = "Jeomseon/Safe Area/Safe Area Settings")]
    public sealed class SafeAreaSettings : ScriptableObject
    {
        public const string DefaultRootName = "SafeAreaRoot";
        private const string ResourcesPath = "SafeAreaSettings";

        [SerializeField] private string rootName = DefaultRootName;
        [SerializeField] private bool skipWorldSpaceCanvases = true;
        [SerializeField] private bool autoPatchRuntimeCanvases;

        /// <summary>
        /// 패치 시 생성되는 SafeAreaRoot GameObject의 이름.
        /// </summary>
        public string RootName => string.IsNullOrEmpty(rootName) ? DefaultRootName : rootName;

        /// <summary>
        /// true면 RenderMode.WorldSpace Canvas는 패치 대상에서 제외한다.
        /// </summary>
        public bool SkipWorldSpaceCanvases => skipWorldSpaceCanvases;

        /// <summary>
        /// true면 SafeAreaRuntimeApplier가 씬 로드 시 모든 Canvas를 자동으로 패치한다.
        /// 기본값은 false(옵트인)이며, 명시적 패치(SafeAreaScenePatcher, ApplyToAllCanvases 수동 호출)는
        /// 이 값과 무관하게 항상 동작한다.
        /// </summary>
        public bool AutoPatchRuntimeCanvases => autoPatchRuntimeCanvases;

        /// <summary>
        /// "Resources/SafeAreaSettings" 에셋을 찾아 반환한다. 없으면 기본값으로 된 인스턴스를 새로 만들어 반환한다.
        /// 결과를 static으로 캐싱하지 않는다 — 패치는 씬 로드/메뉴 실행처럼 드물게만 일어나므로,
        /// 정적 캐시로 인한 Domain Reload 수명 문제를 새로 만들 필요가 없다.
        /// </summary>
        public static SafeAreaSettings Resolve()
        {
            var settings = Resources.Load<SafeAreaSettings>(ResourcesPath);
            return settings != null ? settings : CreateInstance<SafeAreaSettings>();
        }
    }
}
