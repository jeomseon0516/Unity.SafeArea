using UnityEngine;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// 이 컴포넌트가 붙은 Canvas는 SafeAreaPatchCore의 자동/영구/프리뷰 패치 대상에서 제외된다.
    /// 프로젝트 Tag 등록이 필요했던 이전 태그 기반 제외 방식을 대체한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaIgnore : MonoBehaviour
    {
    }
}
