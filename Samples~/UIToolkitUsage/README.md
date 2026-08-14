# Safe Area UI Toolkit 예제

uGUI(`RectTransform`/`LayoutGroup`) 기반 [Basic Usage](../BasicUsage) 예제와 달리, 이 예제는 UI
Toolkit(`UIDocument`/`VisualElement`)에서 `SafeAreaVisualElementRoot`/`SafeAreaVisualElementPadding`을
쓰는 법을 보여줍니다.

## Scene 재생성 (선택)

`UIDocument`/`PanelSettings`는 Unity native 모듈 타입이라 `.unity`를 손으로 작성하면 classID를
잘못 추측해 컴포넌트가 깨질 위험이 있습니다. 대신 Unity 메뉴로 생성합니다.

패키지에는 Unity 6000.5.7f1이 생성한 `PanelSettings.asset`과
`SafeAreaUIToolkitSample.unity`가 포함됩니다. 다시 생성하려면 두 파일과 각 `.meta`를 삭제한 뒤:

1. `Jeomseon/Safe Area/Setup UI Toolkit Sample` 메뉴를 실행합니다.
2. 이 폴더에 두 자산이 다시 생성되는지 확인합니다.

이미 생성돼 있다면(예: 패키지에 커밋된 상태로 받았다면) 이 메뉴는 아무것도 하지 않고 건너뜁니다.

## Scene 구성

`SafeAreaUIToolkitSample.uxml` 기준, `safe-area-panel`과 `header`는 형제(둘 다 UXML 최상위
직속)입니다 — uGUI 예제의 `Canvas` 아래 `Safe Area Panel`/`Header`가 형제인 구조와 동일합니다.

- **safe-area-panel**(초록) — `SafeAreaVisualElementRoot`(`useLeft`/`useRight`/`useTop`/
  `useBottom` 전부 켬)가 붙어 있습니다. `position: Absolute` + `left`/`right`/`top`/`bottom`
  인셋으로 **박스 자체(배경 포함)를 안전 영역 크기로 실제로 줄입니다** — uGUI 예제의
  `SafeAreaRoot`(anchor 기반)와 동일한 역할입니다.
- **header**(파랑) — `useTop`만 켠 `SafeAreaVisualElementPadding`이 붙어 있습니다. USS로 화면
  상단에 항상 고정(`position: Absolute; top: 0;`)되고, **내부 padding.top만** 안전 영역
  인셋만큼 늘어납니다(배경 자체는 줄어들지 않고 화면 최상단까지 깔림 — 웹의
  `env(safe-area-inset-top)` padding 관례와 동일). uGUI 예제의 `Header`(top-stretch anchor +
  `SafeAreaPadding`)와 동일한 역할입니다. `VisualElement`는 내용에 맞춰 높이가 자동으로 늘어나
  uGUI에서 겪었던 "고정 높이라 배경이 안 늘어나는" 문제 자체가 없습니다.

두 컴포넌트는 서로 다른 타입(`SafeAreaVisualElementRoot`/`SafeAreaVisualElementPadding`)이고
각각 다른 GameObject 위 컴포넌트로 붙어 `targetElementName`으로 대상 `VisualElement`를 가리킵니다.

**`Root`와 `Padding`의 차이**: `Root`는 박스 자체(배경 포함)를 안전 영역 크기로 줄이고,
`Padding`은 배경은 그대로 두고 내부 콘텐츠만 안쪽으로 밀어 넣습니다. 화면 전체를 안전 영역에
맞춰 실제로 줄여야 하면 `Root`를, 배경은 노치 아래까지 깔리고 콘텐츠만 피하면 되면(예: 헤더
바처럼 배경이 상단까지 이어져야 자연스러운 경우) `Padding`을 씁니다.

## 확인 절차

1. `SafeAreaUIToolkitSample.unity`를 열어 Game View에서 노치 있는 디바이스를 선택하거나 Device
   Simulator를 켠 뒤 Play Mode로 진입합니다.
2. `safe-area-panel`(초록)이 화면 가장자리에서 안전 영역만큼 실제로 줄어드는지(배경이 노치/펀치홀
   영역을 침범하지 않는지) 확인합니다.
3. `header`(파랑)는 배경이 화면 최상단(노치 영역 포함)까지 깔리고, 그 안의 Label 텍스트만 안전
   영역 아래로 밀려 있는지 확인합니다(Root와 다른 시각적 결과인 것이 정상입니다).
4. Play Mode 중에 Device Simulator에서 디바이스를 바꿔가며 `header`/`safe-area-panel`이 즉시
   재적용되는지 확인합니다(런타임에는 `SafeAreaWatcher`가 매 프레임 `Screen.safeArea` 변화를
   감지해 이벤트로 알려줍니다). 참고로 이 두 컴포넌트는 `[ExecuteAlways]`인 uGUI
   `SafeAreaRoot`/`SafeAreaPadding`과 달리 `OnValidate`가 없어 **Edit Mode에서는 컴포넌트가
   활성화되는 시점(Scene 로드 등)에만 한 번 적용**됩니다 — Edit Mode에서 디바이스를 바꿔도 즉시
   갱신되지 않는 것이 정상입니다.
