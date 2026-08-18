# Safe Area 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 테스트 모드 정리 (2026-08-18, Unity 검증 대기)

- UI Toolkit 컴포넌트의 `OnEnable`/`OnDisable` 예외 안전성 테스트를 EditMode에서 제거하고 실제
  프레임과 Unity 메시지를 사용하는 PlayMode 테스트로 이전했습니다.

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
   - `SafeAreaPadding`은 기존처럼 `LayoutGroup.padding`을 기준값으로 사용합니다. PreviewScene은
     원본 Scene의 컴포넌트를 변경하지 않으므로 Preview 기능 때문에 별도 Base Padding API를
     추가하지 않습니다. 회귀 테스트 `Tests/Editor/SafeAreaPaddingTests.cs`를 유지합니다.
   - **버그 발견·수정(2026-08-14)**: 위 수정에서 `ApplyPadding`에 함께 추가했던
     `Undo.RecordObject`/`EditorUtility.SetDirty` 호출이 사용자가 Sample Scene에서 Play Mode 종료 후
     Header의 Label 위치가 의도치 않게 저장되는 버그를 유발했습니다. 이 호출이 사용자의 명시적 편집
     때뿐 아니라 `OnEnable`/`OnValidate`의 모든 자동 재계산(씬 열기, 재컴파일, Play↔Edit 전환 등)마다
     실행돼, Game View에 선택된 디바이스의 Safe Area 값을 매번 dirty 처리했기 때문입니다. 같은
     ExecuteAlways 방식인 `SafeAreaRoot`는 원래 이 호출을 하지 않는다는 걸 확인하고, 그 컨벤션대로
     자동 재계산 경로를 다시 비영속(preview성)으로 되돌렸습니다.
   - 남은 작업: 자식 재배치 정책(현재는 항상 Canvas의 모든 자식을 이동)을 설정으로 노출하고, Custom
     Inspector에서 적용 대상 preview·Undo를 지원합니다.
4. **P1-03 — UI Toolkit 지원 (완료, 2026-08-14, Unity 검증 완료)**
   - 기존 `SafeAreaRoot`/`SafeAreaPadding`은 `RectTransform`/`LayoutGroup` 기반이라 uGUI 전용이며
     `UIDocument`(UI Toolkit)에는 적용되지 않습니다. 사용자 지시로 uGUI API는 그대로 legacy로 남기고,
     UI Toolkit용 `SafeAreaVisualElementPadding`(`Runtime/UIToolkit/`)을 추가했습니다. `UIDocument`의
     지정 `VisualElement`(비우면 `rootVisualElement`) padding에 안전 영역 인셋을 더합니다. VisualElement는
     anchor 개념이 없어(flex 레이아웃) 컴포넌트 하나로 "전체 화면 인셋"과 "가장자리 일부 인셋"을 모두
     표현합니다. USS로 읽어들인 값을 다시 캐싱하는 대신(패널 부착 전 `resolvedStyle` 타이밍 위험 회피),
     베이스 padding을 컴포넌트 필드로 직접 받는 방식을 택했습니다. 순수 적용 로직(`ApplyInsets`)을
     `internal static`으로 분리해 `UIDocument`/Panel 없이도 단위 테스트가 가능합니다.
   - 회귀 테스트 `Tests/Editor/SafeAreaVisualElementPaddingTests.cs` 추가. 처음 작성 시
     `target.resolvedStyle`로 assertion했는데, 테스트가 만든 `VisualElement`가 Panel에 붙어있지 않아
     레이아웃 리졸브가 한 번도 안 돌아 항상 0을 반환하는 버그가 있었습니다(구현이 아니라 테스트
     쪽 버그) — `target.style`(인라인 값)로 assertion을 바꿔 수정, 4개 전체 Unity Test Runner PASS
     사용자가 확인 완료.
   - **`SafeAreaVisualElementRoot` 추가(2026-08-14)**: `SafeAreaVisualElementPadding`만으로는
     "배경 자체를 안전 영역 크기로 줄이는" uGUI `SafeAreaRoot`의 역할이 UI Toolkit 쪽에 없다는 걸
     사용자가 지적해 추가했습니다. `Position.Absolute` + `left`/`right`/`top`/`bottom` 인셋으로
     박스 자체(배경 포함)를 줄입니다(`Padding`은 배경은 유지하고 내부 콘텐츠만 밈 — 웹의
     `env(safe-area-inset-*)` padding 관례와 동일, 대체가 아니라 서로 다른 용도). 회귀 테스트
     `Tests/Editor/SafeAreaVisualElementRootTests.cs` 추가.
   - **Sample Scene 추가 완료(2026-08-14, `Samples~/UIToolkitUsage`)**: `UIDocument`/`PanelSettings`는
     native 모듈 타입이라 `.unity`를 손으로 작성하면 classID를 잘못 추측해 Missing Script로 조용히
     깨질 위험이 있어(참조할 기존 예시가 워크스페이스에 전혀 없었음), 대신
     `Jeomseon/Safe Area/Setup UI Toolkit Sample` 메뉴로 Unity가 직접 생성하도록 했습니다
     (`Jeomseon.Unity.Localization`의 Sample Setup 스크립트와 같은 패턴). UXML/USS는 손으로
     작성했습니다(plain text라 위험이 낮음, `ilspycmd`로 `PanelSettings`/`UIDocument`의 정확한 public
     API를 직접 디컴파일해 확인 후 작성). `safe-area-panel`(`SafeAreaVisualElementRoot`, 전 방향)과
     `header`(`SafeAreaVisualElementPadding`, top만)를 형제 구조로 구성해 uGUI 예제의 Safe Area
     Panel/Header와 대응시켰습니다. `com.unity.modules.uielements` 의존성을 `package.json`에 추가.
   - **버그 발견·수정(2026-08-14)**: Setup 메뉴로 생성한 Scene의 `UIDocument`에 `panelSettings`가
     비어 있는 문제가 있었습니다. 진짜 원인은 `CreatePanelSettings()`로 만든 새 `PanelSettings`가
     아직 아무 오브젝트에도 안 붙어있는 상태에서 그 뒤에 `EditorSceneManager.NewScene(...,
     Single)`을 호출해, "사용되지 않는 에셋 언로드" 과정에서 함께 파괴된 것이었습니다(디버그 로그로
     실측 확인, 중간에 `UIDocument.Reset()` 타이밍·`AssetDatabase` 캐시 불일치로 잘못 짚었던 방어
     코드는 전부 제거). `NewScene` 호출을 `PanelSettings` 생성보다 먼저 하도록 순서만 바꿔
     해결했습니다. 사용자가 Unity에서 정상 동작 확인 완료.
   - **Unity 검증 완료(2026-08-14)**: Unity 6000.5.7f1에서 새 형제 구조 Scene을 열고 Punch Hole
     Left 디바이스로 Play Mode 동작을 확인했습니다. `SafeAreaVisualElementRoot`의 초록 패널과
     `SafeAreaVisualElementPadding`의 파란 Header가 각각 안전 영역을 반영했고, SafeArea EditMode
     테스트 13개가 모두 통과했습니다. Unity가 생성한 `PanelSettings.asset`/Scene도 Sample에
     포함했습니다.
   - **Setup 메뉴 제거(2026-08-18)**: `Jeomseon/Safe Area/Setup UI Toolkit Sample` 메뉴는 이미 생성된
     `PanelSettings.asset`/Scene이 커밋돼 있으면 항상 "이미 있음" 로그만 남기고 아무 것도 하지 않는
     no-op이었습니다. `Jeomseon.Unity.Shaders`에서 같은 패턴을 폐기한 것과 동일한 이유로 Setup
     스크립트와 전용 Editor asmdef를 통째로 제거했습니다 — 커밋된 `PanelSettings.asset`/
     `SafeAreaUIToolkitSample.unity`가 Unity 자체 직렬화 결과 그대로이므로 재생성 경로가 없어도
     안전합니다. README의 "Scene 재생성" 안내도 함께 제거.
   - **Basic Usage와 시각적 1:1 대응(2026-08-18)**: 사용자가 스크린샷으로 `header`(파랑)와
     `safe-area-panel`(초록)의 텍스트가 겹쳐 보인다고 지적했습니다. 원인은 `safe-area-panel`에만
     있던 `body-label`(uGUI Basic Usage에는 대응 요소가 없음)이 header의 반투명 배경 아래로
     내려오는 영역과 겹쳤기 때문이었습니다. uGUI Basic Usage Scene을 YAML로 직접 대조해, uGUI의
     `Safe Area Panel`은 콘텐츠 없이 배경색만 있다는 걸 확인하고 `body-label`을 제거해 구조를
     맞췄습니다. `header-label` 텍스트도 `"Safe Area Header"`로 맞췄습니다(uGUI Label 실제 텍스트는
     `"Safe Area Header (SafeAreaPadding, useTop)"`이지만 고정 `400x60` 박스의 Vertical Overflow:
     Truncate 때문에 두 번째 줄이 항상 잘려 화면엔 `"Safe Area Header"`만 보임 — 실제 렌더링 기준으로
     맞춤), 글자 크기를 uGUI와 동일한 `28px`로, `SafeAreaVisualElementPadding`의
     `basePaddingTop`/`basePaddingBottom`을 uGUI Header `VerticalLayoutGroup`의 padding(Top 0,
     Bottom 0) 기준에 맞춰 `8`→`0`으로 조정(좌우 `16`은 이미 일치)했습니다.
   - **Gamma/Linear 색상 불일치 수정(2026-08-18)**: 텍스트/구조를 맞춘 뒤에도 사용자가 "색상이 다르다"고
     지적했습니다. 두 Scene 다 Camera가 없어 배경 차이는 아니었고, `ProjectSettings.asset`의
     `m_ActiveColorSpace: 1`(Linear)을 확인한 뒤 uGUI `Canvas`의 `m_VertexColorAlwaysGammaSpace: 1`
     (항상 Gamma 블렌딩)과 UI Toolkit `PanelSettings`의 `forceGammaRendering: 0`(Linear 블렌딩)이
     서로 다르다는 걸 발견했습니다. 같은 RGBA 값이라도 블렌딩 공간이 다르면 다르게 보이는 게 원인 —
     `PanelSettings.asset`의 `forceGammaRendering`을 `1`로 켜서 해결했습니다.
   - **Camera 부재로 인한 프레임 누적(2026-08-18, 사용자 Unity 검증 완료)**: 색상 수정 후에도
     사용자가 header가 "이전 화면이 계속 캐시되어 겹쳐 보이는" 것처럼 색이 진해진다고 재보고했습니다.
     Scene에 Camera가 하나도 없다는 걸 재확인했고, Camera가 없으면 매 프레임 확실한 클리어가
     보장되지 않아 UI Toolkit 오버레이가 이전 프레임 결과 위에 반투명 색을 계속 덧그렸을 가능성이
     있다고 판단해 Clear Flags: Solid Color인 `Main Camera`를 Scene에 추가했습니다. **사용자가
     Unity에서 재현 확인 — 프레임 누적 현상이 사라짐을 확인했습니다.**
   - **uGUI Scene 되돌림(2026-08-18)**: 위 수정 직후 두 샘플의 배경색이 달라 보인다는 지적을 받아
     uGUI Basic Usage Scene에도 같은 배경색의 Camera를 추가했었으나, **사용자가 전제 자체가 틀렸다고
     정정**했습니다 — uGUI는 이미 안정화 완료된 패키지이고, `Safe Area Panel`/`Header`가 Image
     컴포넌트로 화면을 실제로 덮는 불투명에 가까운 UI라 애초에 Camera 배경에 영향받지 않는 구조인
     반면, UI Toolkit 쪽에는 그런 배경을 실제로 채우는 UI가 없어서 Camera 배경색을 그대로 따라가는
     것이라는 지적입니다. uGUI Basic Usage Scene의 Camera 추가는 **완전히 되돌렸고**(패키지 저장소는
     `git checkout --`로, TestProject 임포트본은 사용자가 이미 열어둔 상태라 그 Camera 삽입분만
     수동으로 제거), **uGUI Basic Usage는 더 이상 건드리지 않습니다.** 남은 배경색 문제가 있다면
     UI Toolkit 쪽(Scene의 `Main Camera` 배경값 또는 Panel 자체의 배경 커버리지)에서만 계속
     조정합니다.
   - **`safe-area-panel` 크기 0 버그(2026-08-18)**: uGUI Camera를 되돌린 뒤 사용자가 "header 빼고
     전부 Camera 배경색(검정)으로 보인다"고 재보고했습니다 — `safe-area-panel`의 초록 배경 자체가
     전혀 그려지지 않고 있었습니다. `body-label` 제거로 이 요소가 완전히 빈 `VisualElement`가 된
     뒤로, `position: Absolute` + `left`/`right`/`top`/`bottom`만 런타임에 설정하고 `width`/`height`를
     USS에 명시하지 않아 레이아웃 엔진이 자식 없는 이 요소의 크기를 0으로 계산했던 것으로 보입니다.
     프레임 누적 버그가 있던 동안에는 이전(라벨이 있던 시절) 렌더링 결과가 잔상으로 남아 있어 실제로
     배경이 안 그려지고 있다는 게 가려져 있었습니다. USS에 `width: 100%; height: 100%;`를 기본값으로
     명시해 해결(런타임 insets가 이후 이 값을 안전 영역 크기로 덮어씀). **사용자가 Unity에서 재확인 —
     배경이 정상적으로 다시 그려지고 프레임 누적 현상도 없음을 확인했습니다.**
   - **색상/글자 크기 미세 차이는 대응 범위 밖으로 확정(2026-08-18)**: 위 수정 후에도 uGUI와 UI
     Toolkit의 초록/파랑 색상이 미묘하게 다르고(스크린샷 대조 결과 UI Toolkit 쪽이 더 탁하게 보임 —
     원인 후보로 URP Camera 렌더 경로의 색공간 변환을 의심했으나 `DefaultVolumeProfile`엔 Tonemapping
     등 컴포넌트가 없어(`components: []`) 미확정) Header 글자 크기도 근소하게 다릅니다(uGUI는 내장
     legacy `Arial`, UI Toolkit Label은 기본 폰트라 폰트 시스템 자체가 달라 `28px` 지정에도 실제 줄
     높이가 정확히 같지 않음). **사용자가 "SafeArea 인셋 대응 기능만 정상이면 충분하다"고 확정해 이
     미세한 시각적 차이는 더 이상 쫓지 않기로 했습니다.** 완전 픽셀 단위 매칭이 필요해지면 다음
     세션에서 Frame Debugger로 실제 블렌딩 경로를 직접 확인한 뒤 접근할 것(소스 분석만으로는 이
     세션에서 한계에 도달함).
5. **P2-01 — 화면 변화 감지 최적화**
   - 해상도, 방향, safe area가 실제로 바뀐 경우에만 레이아웃을 갱신합니다.
6. **P3-01 — PreviewScene 유지 (복원, Unity 검증 완료 2026-08-14)**
   - Device Simulator 연동으로 실제 Edit Mode Scene에 값을 전파하는 방식은 Simulator Repaint의
     Screen shim과 `ExecuteAlways` 레이아웃 변경이 결합돼 Padding 왕복·누적 문제를 만들었을 뿐 아니라,
     원본 Scene의 실제 컴포넌트를 직접 수정해 "Preview는 원본에 안전해야 한다"는 원칙 자체를
     깼습니다. 원본 Scene과 격리된 기존 PreviewScene + 복제 Canvas + Camera/RenderTexture 방식을
     복원했습니다. `SafeAreaRoot.ApplyPreview`도 PreviewScene 전용 internal API로 복원했습니다.
   - **(버그 수정)** `SafeAreaRoot`에만 `ApplyPreview`가 있고 `SafeAreaPadding`(Header)에는 없어서,
     복원 직후 Override 값을 바꿔도 Header가 반영되지 않는 문제가 있었습니다.
     `SafeAreaPadding.ApplyPreview(Rect, Vector2)`를 추가하고
     `SafeAreaPreviewWindow.ApplyPreviewToScene()`이 `SafeAreaRoot`와 함께 호출하도록 고쳤습니다.
   - Device Simulator는 독립적인 Safe Area 경계 확인 도구로만 사용하며 PreviewWindow 자동
     동기화에는 사용하지 않습니다.
   - Unity에서 Preview Window 렌더링, 원본 Scene 불변, Play Mode/ContentSizeFitter 리그레션까지
     전부 재확인 완료(2026-08-14, 사용자 확인).
7. **P3-02 — UI Toolkit Preview Window (백로그, 미착수)**
   - 현재 Preview Window는 uGUI(`SafeAreaRoot`/`SafeAreaPadding`) 전용이고, UI Toolkit
     (`SafeAreaVisualElementRoot`/`SafeAreaVisualElementPadding`)에는 대응하는 Preview 기능이
     없습니다. uGUI와 UI Toolkit 화면이 다르게 구성되는 경우 1:1로 비교 확인할 수 없다는 지적이
     있어 백로그에 기록합니다.
   - 구현 방식은 uGUI와 달라야 합니다 — uGUI는 Canvas GameObject를 `Instantiate()`로 복제하지만,
     UI Toolkit은 `VisualTreeAsset.CloneTree()`로 UXML 원본에서 새 트리를 만들고, `PanelSettings`에
     `targetTexture`(Unity가 이미 지원하는 RenderTexture 출력 옵션)를 지정해 렌더링하는 구조가 될
     것으로 예상합니다. 원본 Scene의 `UIDocument`는 전혀 건드리지 않는다는 점은 uGUI 방식과
     동일하게 유지합니다.
   - 렌더링 파이프라인이 uGUI(Camera+RenderTexture)와 UI Toolkit(PanelSettings.targetTexture)로
     완전히 갈리므로, 하나의 창에 두 렌더러를 모두 넣을지(탭 전환) 별도 창으로 분리할지는 착수
     시점에 결정합니다. 설계만 기록된 상태이며 구현은 시작하지 않았습니다.
