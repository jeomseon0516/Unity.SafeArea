# Safe Area 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 정적 이벤트 수명 안정화 (완료, 2026-08-13, Unity 검증 대기)**
   - `SafeAreaWatcher`에 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`
     단계의 `ResetStaticStateForNewSession()`을 추가했습니다. Domain Reload를 끈 Enter Play Mode에서도
     이 메서드는 매 Play 세션 진입 시 무조건 재실행되므로(static 필드 초기화와 달리
     `RuntimeInitializeOnLoadMethod`는 Domain Reload 여부와 무관하게 항상 실행됨), `_initialized`
     플래그·`SafeAreaChanged` 구독자·캐시된 safe area/screen size를 전부 초기화해 이후 `BeforeSceneLoad`
     단계의 `InitOnPlay()`가 이번 세션 기준으로 다시 초기화하도록 보장합니다. 회귀 테스트
     `Tests/Editor/SafeAreaWatcherTests.cs` 추가.
2. **P1-01 — 명시적인 적용 대상 구성 (부분 완료, 2026-08-13)**
   - `SafeAreaRuntimeApplier`의 전체 Canvas 자동 패치를 `SafeAreaSettings.AutoPatchRuntimeCanvases`
     (기본값 `false`, 옵트인)로 게이팅했습니다. 명시적 호출(`ApplyToAllCanvases()`,
     `SafeAreaScenePatcher`)은 이 설정과 무관하게 항상 동작합니다.
   - 남은 작업: Custom Inspector에서 적용 대상을 미리 확인하고 씬 변경 전 preview·Undo를 지원하는
     것은 아직입니다(P1-02 잔여 항목과 함께 처리).
3. **P1-02 — SafeArea Project Settings (부분 완료, 2026-08-13)**
   - `SafeAreaSettings` ScriptableObject(`Jeomseon/Safe Area/Safe Area Settings` 메뉴, `Resources`
     에셋으로 오버라이드)로 root 이름과 World Space Canvas 스킵 여부를 제공합니다. 제외 대상은 태그
     대신 `SafeAreaIgnore` 마커 컴포넌트로 지정합니다(프로젝트 Tag 등록이 필요 없음).
   - `SafeAreaPadding._originalPadding`이 최초 1회만 캐싱되어 사용자가 Inspector에서
     `LayoutGroup.padding`을 직접 편집해도 다음 SafeArea 갱신 시 되돌아가던 버그를 수정했습니다.
     마지막으로 이 컴포넌트가 적용한 값(`_lastAppliedPadding`)과 현재 값의 차이(delta)를 원본
     기준선에 반영하는 방식으로 재설계했습니다. `ApplyPadding`에 누락돼 있던
     `Undo.RecordObject`/`EditorUtility.SetDirty` 호출도 추가했습니다(에디터에서 Play 중이 아닐
     때만). 회귀 테스트 `Tests/Editor/SafeAreaPaddingTests.cs` 추가.
   - 남은 작업: 자식 재배치 정책(현재는 항상 Canvas의 모든 자식을 이동)을 설정으로 노출하고, Custom
     Inspector에서 적용 대상 preview·Undo를 지원합니다.
4. **P1-03 — UI Toolkit 지원 (완료, 2026-08-13, Unity 검증 대기)**
   - 기존 `SafeAreaRoot`/`SafeAreaPadding`은 `RectTransform`/`LayoutGroup` 기반이라 uGUI 전용이며
     `UIDocument`(UI Toolkit)에는 적용되지 않습니다. 사용자 지시로 uGUI API는 그대로 legacy로 남기고,
     UI Toolkit용 `SafeAreaVisualElementPadding`(`Runtime/UIToolkit/`)을 추가했습니다. `UIDocument`의
     지정 `VisualElement`(비우면 `rootVisualElement`) padding에 안전 영역 인셋을 더합니다. VisualElement는
     anchor 개념이 없어(flex 레이아웃) 컴포넌트 하나로 "전체 화면 인셋"과 "가장자리 일부 인셋"을 모두
     표현합니다. USS로 읽어들인 값을 다시 캐싱하는 대신(패널 부착 전 `resolvedStyle` 타이밍 위험 회피),
     베이스 padding을 컴포넌트 필드로 직접 받는 방식을 택했습니다. 순수 적용 로직(`ApplyInsets`)을
     `internal static`으로 분리해 `UIDocument`/Panel 없이도 단위 테스트가 가능합니다.
   - 회귀 테스트 `Tests/Editor/SafeAreaVisualElementPaddingTests.cs` 추가, `dotnet build` Runtime/Tests
     오류 0개 확인. **UI Toolkit 부분은 아직 실제 Unity Test Runner로 실행 확인은 못 했습니다**(같은
     세션에서 P0-01/P1-02는 이 데스크톱에 열려 있던 Unity Editor의 `TestResults.xml`로 실제 PASS를
     확인했지만, 이 작업 시점에는 로그가 최신화되지 않아 재확인 못함 — 다음 세션에서 우선 확인 필요).
   - Sample Scene에는 아직 UI Toolkit 데모를 추가하지 않았습니다(PanelSettings/UXML/USS까지 손으로
     작성하는 추가 리스크를 피하기 위해 이번 범위에서 제외). 다음 단계로 남겨둡니다.
5. **P2-01 — 화면 변화 감지 최적화**
   - 해상도, 방향, safe area가 실제로 바뀐 경우에만 레이아웃을 갱신합니다.
6. **P3-01 — Device Simulator와 중복되는 Preview 정리**
   - 추가 프리셋 가치가 없으면 자체 창을 축소하고 Simulator 연동 문서를 제공합니다.
