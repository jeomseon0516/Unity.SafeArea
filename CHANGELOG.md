# 변경 기록

## [Unreleased]

## [0.3.0] - 2026-08-17

- **(버그 수정)** `SafeAreaPreviewWindow`에서 Override 값을 바꾸고 "Apply & Rebuild Preview"를
  눌러도 `SafeAreaPadding`(예: Header)이 반영되지 않던 문제를 고쳤습니다. `SafeAreaRoot`에만
  Preview 전용 진입점(`ApplyPreview`)이 있고 `SafeAreaPadding`에는 없어서, PreviewScene에 복제된
  `SafeAreaPadding`이 Window가 전달하는 값을 받을 방법이 없었습니다(`SafeAreaWatcher` 이벤트도
  안 타므로 자기 자신의 `OnEnable`이 읽은 실제 `Screen.safeArea` 값에 머물러 있었음).
  `SafeAreaPadding`에도 같은 모양의 `ApplyPreview(Rect, Vector2)`를 추가하고
  `SafeAreaPreviewWindow.ApplyPreviewToScene()`이 `SafeAreaRoot`와 함께 호출하도록 했습니다.

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
- **(P0-01)** `SafeAreaWatcher`에 `RuntimeInitializeLoadType.SubsystemRegistration` 단계의
  `ResetStaticStateForNewSession()`을 추가해, Domain Reload를 끈 Enter Play Mode에서도 매 세션마다
  이전 실행의 정적 이벤트 구독자·초기화 플래그·캐시값이 남지 않도록 했습니다.
- **(버그 수정)** `SafeAreaPadding.ApplyPadding`에 추가했던 `Undo.RecordObject`/`EditorUtility.SetDirty`
  호출을 다시 제거했습니다. 이 호출이 사용자의 명시적 편집뿐 아니라 `OnEnable`/`OnValidate`의 자동
  재계산(씬 열기, 재컴파일, Play→Edit 전환 등)마다 매번 실행돼, Game View에 노치 디바이스가 선택돼
  있으면 Play Mode를 거치지 않아도 그 순간의 Safe Area 값이 씬에 dirty 처리되어 저장 시 영구
  반영되는 문제가 있었습니다(Header의 Label이 예기치 않은 위치로 저장되는 형태로 발견). 같은
  ExecuteAlways 방식인 `SafeAreaRoot`는 애초에 이 호출을 하지 않아 문제가 없었으므로, 그 컨벤션에
  맞춰 자동 재계산 경로는 항상 비영속(preview성) 적용으로 되돌렸습니다. Inspector에서 사용자가
  직접 필드를 편집하는 경우의 Undo/Dirty는 Unity의 기본 SerializedProperty 처리로 이미 커버됩니다.
  **2026-08-17 사용자 Unity 재검증 완료**(Inspector 직접 편집 + Undo 정상 동작).
- **(P1-03)** UI Toolkit 지원 `SafeAreaVisualElementPadding`(`Jeomseon.Unity.SafeArea.UIToolkit`)을
  추가했습니다. 기존 `SafeAreaRoot`/`SafeAreaPadding`은 `RectTransform`/`LayoutGroup` 기반 uGUI
  전용으로 계속 유지하며, `UIDocument`를 쓰는 프로젝트는 새 컴포넌트를 사용합니다.
- **(P1-03)** `SafeAreaVisualElementRoot`(`SafeAreaRoot`의 UI Toolkit 대응)를 추가했습니다.
  `Position.Absolute` + `left`/`right`/`top`/`bottom` 인셋으로 지정한 `VisualElement`의 박스
  자체(배경 포함)를 안전 영역 크기로 실제로 줄입니다. `SafeAreaVisualElementPadding`(배경은 유지,
  내부 콘텐츠만 안쪽으로 밀기)과 역할이 분리돼, uGUI의 `SafeAreaRoot`/`SafeAreaPadding` 짝과
  동일하게 UI Toolkit에서도 두 가지 선택지를 제공합니다.
- **(P1-03)** UI Toolkit Sample(`Samples~/UIToolkitUsage`)을 추가했습니다. `UIDocument`/
  `PanelSettings`는 native 모듈 타입이라 `.unity`를 손으로 작성하는 대신, `Jeomseon/Safe Area/Setup
  UI Toolkit Sample` 메뉴로 Unity가 직접 생성하도록 했습니다(`Jeomseon.Unity.Localization`의 Sample
  Setup 스크립트와 같은 패턴). `com.unity.modules.uielements` 의존성을 `package.json`에 추가했습니다.
  `safe-area-panel`(`SafeAreaVisualElementRoot`)과 `header`(`SafeAreaVisualElementPadding`)를
  uGUI Basic Usage 예제와 동일하게 형제 구조로 구성했습니다.
- **(버그 수정)** 위 Setup 메뉴로 생성한 Scene의 `UIDocument`에 `panelSettings`가 비어 있는(Inspector
  경고: "assign a PanelSettings asset") 문제가 있었습니다. 재현 조건과 무관하게(캐시 상태·재임포트
  여부와 무관하게) 매번 재현되는 것을 보고 디버그 로그로 추적한 결과, 진짜 원인은 호출 순서였습니다
  — `CreatePanelSettings()`로 새 `PanelSettings`를 먼저 만들고 그다음에
  `EditorSceneManager.NewScene(..., NewSceneMode.Single)`을 호출했는데, 그 사이 아직 아무
  오브젝트에도 물려있지 않은(C# 지역 변수만 참조하는) 새 `PanelSettings`가 `NewScene`의 "사용되지
  않는 에셋 언로드" 과정에서 함께 파괴돼(fake-null) 그 뒤 `document.panelSettings`에 대입해도 이미
  죽은 참조였습니다. `NewScene` 호출을 `PanelSettings` 생성보다 먼저 하도록 순서를 바꿔 해결했습니다.
  (중간에 `UIDocument.Reset()` 타이밍 문제, `AssetDatabase` 캐시 불일치 등으로 잘못 짚고 방어
  코드를 추가했다 디버그 로그로 반증돼 제거한 이력이 있습니다 — 최종 코드는 순서 수정 하나로
  단순합니다.)
- **(버그 수정)** Basic Usage Sample Scene에서 관례상 포함했던 `EventSystem`(`StandaloneInputModule`
  사용)을 제거했습니다. Player Settings의 Active Input Handling이 "Input System Package(New)"
  단독으로 설정된 프로젝트에서 Scene 진입 시 `StandaloneInputModule`이 레거시 `UnityEngine.Input`을
  읽으려 해 `InvalidOperationException`이 발생했습니다. 이 Scene은 원래 클릭 상호작용이 없어
  `EventSystem` 자체가 불필요했으므로, Active Input Handling 설정에 따라 어떤 Input Module을 써야
  할지 분기하는 대신 컴포넌트를 통째로 제거했습니다.
- **(버그 수정)** Basic Usage Sample Scene의 `Header`가 고정 `sizeDelta.y = 80`이고
  `VerticalLayoutGroup.m_ChildControlHeight = 0`이라, 런타임에 `SafeAreaPadding`이 노치 인셋만큼
  `padding.top`을 키워도 `Header` 자신의 배경(파란색)은 자라지 않고 고정 80px에 머무는 채 노치
  영역을 그대로 침범하고, `Label`만 padding을 따라 컨테이너 밖(초록 Safe Area Panel 쪽)까지
  밀려나 보이는 문제가 있었습니다. `Header`에 `ContentSizeFitter`(Vertical Fit: Preferred Size)를
  추가해 배경 높이가 `padding.top`(안전 영역 인셋)에 맞춰 항상 함께 자라도록 했습니다.
- TODO: 안전 영역 갱신 이벤트의 정적 수명과 Enter Play Mode Options 호환성을 검토합니다.
- TODO: 자식 재배치 정책(현재는 항상 Canvas의 모든 자식을 이동)을 `SafeAreaSettings`로 추가 노출하고,
  Custom Inspector에서 적용 대상 preview와 씬 변경 전 Undo를 지원합니다.
- TODO(P2-01): 해상도/방향/safe area가 실제로 바뀐 경우에만 레이아웃을 갱신하도록 최적화합니다.
- TODO(P3-02, 백로그): UI Toolkit용 Preview Window(`SafeAreaVisualElementRoot`/
  `SafeAreaVisualElementPadding` 대응)를 추가합니다.

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

## [0.1.0] - 2026-07-29

- JeomseonScriptPack의 SafeArea 모듈을 독립 UPM 패키지로 분리했습니다.


## [0.1.4] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.
