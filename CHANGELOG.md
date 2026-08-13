# 변경 기록

## [0.2.0] - 2026-08-13

- **(Breaking)** Runtime/Editor 네임스페이스를 패키지 규칙에 맞춰
  `Jeomseon.Unity.SafeArea`와 `Jeomseon.Unity.SafeArea.Editor`로 변경했습니다. 이전
  `Jeomseon.SafeArea`/`Jeomseon.SafeAreaEditor` 호환 별칭은 제공하지 않습니다.

## [0.1.6] - 2026-08-11

- 워크스페이스 명명 규칙에 맞춰 `SafeAreaPadding`·`SafeAreaRoot`의 `[SerializeField] private`
  필드를 `_camelCase`에서 `camelCase`로 정리하고 기존 이름을 `[FormerlySerializedAs]`로
  보존했습니다. 공개 API 변경은 없으며 기존 Scene·Prefab의 직렬화된 값은 그대로 유지됩니다.

## [0.1.5] - 2026-08-10

- `SafeAreaRuntimeApplier.ApplyToAllCanvases()`가 Unity 6000.5에서 obsolete된
  `Object.FindObjectsByType<Canvas>(FindObjectsInactive, FindObjectsSortMode)` 오버로드를
  사용하던 것을 `FindObjectsByType<Canvas>(FindObjectsInactive.Include)`로 교체했습니다.
  공개 API 변경은 없습니다.

## [0.1.3] - 2026-07-29

- Samples 어셈블리의 `rootNamespace`를 샘플 namespace에 맞게 정리했습니다.

## [0.1.2] - 2026-07-29

- SafeAreaUtility 테스트 메서드의 한글 식별자를 영문 이름으로 변경했습니다.

## [0.1.1] - 2026-07-29

- Safe Area 인셋 계산을 확인하는 `Basic Usage` 샘플을 추가했습니다.

## [Unreleased]

- **(Breaking)** `SafeAreaUtility.EditorOverrideEnabled`/`EditorSafeArea`를 제거했습니다. XML 문서에는
  `SafeAreaPreviewWindow`가 이 값을 사용한다고 되어 있었으나 실제로는 어디에서도 참조되지 않는 죽은
  API였습니다(Preview는 `SafeAreaRoot.ApplyPreview`로 값을 직접 주입하는 별도 경로를 씁니다).
- **(Breaking)** Canvas를 SafeArea 패치 대상에서 제외하는 방식을 태그(`IgnoreSafeAreaCanvas`) 기반에서
  `SafeAreaIgnore` 마커 컴포넌트 부착 방식으로 교체했습니다. 태그 방식은 프로젝트 Tag Manager에 해당
  태그를 미리 등록해야 동작했는데 패키지가 이를 제공하지 않아 사실상 opt-out이 불가능했습니다.
- **(Breaking)** `SafeAreaRuntimeApplier`가 씬 로드 시 모든 Canvas를 무조건 자동 패치하던 동작을 새
  `SafeAreaSettings.AutoPatchRuntimeCanvases`(기본값 `false`, 옵트인)로 게이팅했습니다. 기존처럼 자동
  패치를 쓰려면 `Assets/Resources/SafeAreaSettings.asset`을 만들고 옵션을 켜야 합니다.
  `SafeAreaRuntimeApplier.ApplyToAllCanvases()`/`SafeAreaScenePatcher`를 통한 명시적 호출은 이 설정과
  무관하게 항상 동작합니다.
- `SafeAreaSettings` ScriptableObject(`Jeomseon/Safe Area/Safe Area Settings` 메뉴로 생성)를 추가해 root
  이름, World Space Canvas 스킵 여부, 런타임 자동 패치 사용 여부를 설정할 수 있게 했습니다.
  `SafeAreaPatchCore.EnsureSafeAreaRoot`가 이 정책을 주입받도록 리팩터링되어, Runtime 자동패치·Editor
  영구패치(`SafeAreaScenePatcher`)·Preview 임시패치(`SafeAreaPreviewWindow`)가 동일한 정책을 공유하면서도
  필요하면 서로 다르게 구성할 수 있습니다.
- `SafeAreaPreviewWindow`에 남아있던 디버그용 `Debug.Log("Enable!")` 호출을 제거했습니다.
- TODO: Unity Device Simulator와 자체 프리뷰 창의 역할을 비교하고 통합 여부를 결정합니다.
- TODO: 안전 영역 갱신 이벤트의 정적 수명과 Enter Play Mode Options 호환성을 검토합니다.
- TODO: 자식 재배치 정책(현재는 항상 Canvas의 모든 자식을 이동)을 `SafeAreaSettings`로 추가 노출하고,
  Custom Inspector에서 적용 대상 preview와 씬 변경 전 Undo를 지원합니다.

## [0.1.0] - 2026-07-29

- JeomseonScriptPack의 SafeArea 모듈을 독립 UPM 패키지로 분리했습니다.


## [0.1.4] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.
