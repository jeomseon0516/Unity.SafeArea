# Jeomseon Unity Safe Area

모바일 화면의 안전 영역을 uGUI/UI Toolkit 레이아웃에 적용하고 에디터에서 미리 볼 수 있게 하는 UPM
패키지입니다.

## 설치

OpenUPM 등록 전에는 Package Manager의 **Add package from git URL**에서 다음 주소를 사용합니다.

```text
https://github.com/jeomseon0516/Unity.SafeArea.git#v0.1.2
```

## 구성

### uGUI

- `SafeAreaRoot`: `RectTransform` 앵커에 안전 영역 적용
- `SafeAreaPadding`: `LayoutGroup.padding`에 안전 영역 여백 추가
- `SafeAreaSettings`: root 이름, World Space Canvas 스킵 여부, 런타임 자동 패치 사용 여부를 정하는
  설정 에셋(`Jeomseon/Safe Area/Safe Area Settings` 메뉴로 생성, `Assets/Resources/SafeAreaSettings`
  경로에 두면 인식됨). 없으면 기본값(런타임 자동 패치 꺼짐)으로 동작합니다.
- `SafeAreaIgnore`: 이 컴포넌트가 붙은 Canvas는 SafeArea 패치 대상에서 제외됩니다.
- `SafeAreaRuntimeApplier`: `SafeAreaSettings.AutoPatchRuntimeCanvases`가 켜져 있을 때만 씬 로드마다
  모든 Canvas를 자동 패치합니다(기본값 꺼짐, 옵트인). `ApplyToAllCanvases()`로 언제든 수동 호출도
  가능합니다.
- `SafeAreaPreviewWindow`(`Jeomseon/Safe Area/Preview Window`): 열려 있는 Scene의 Canvas를 원본과
  격리된 PreviewScene에 복제해 자체 Camera/RenderTexture로 렌더링합니다. 원본 Scene의 SafeArea
  컴포넌트는 전혀 건드리지 않으므로, 이 창을 열어도 원본 Scene 상태는 항상 안전합니다. 값은 Unity
  내장 Device Simulator(`Window/General/Device Simulator`)의 `Screen.safeArea`를 기본으로 읽어오며,
  Override 토글로 임의의 값을 직접 입력해 확인할 수도 있습니다.

### UI Toolkit

- `SafeAreaVisualElementRoot`(`Jeomseon.Unity.SafeArea.UIToolkit`, `SafeAreaRoot`의 UI Toolkit
  대응): `UIDocument`가 붙은 GameObject에 부착합니다. `RectTransform` anchor 대신
  `Position.Absolute` + `left`/`right`/`top`/`bottom` 인셋으로 지정한 `VisualElement`(비우면
  `rootVisualElement`)의 박스 자체(배경 포함)를 안전 영역 크기로 실제로 줄입니다.
- `SafeAreaVisualElementPadding`(`SafeAreaPadding`의 UI Toolkit 대응): 지정한
  `VisualElement`의 padding에 안전 영역 인셋을 더합니다(배경은 그대로 두고 내부 콘텐츠만 안쪽으로
  밀어 넣음 — 웹의 `env(safe-area-inset-*)` padding 관례와 동일). 베이스 padding
  (`basePaddingLeft/Right/Top/Bottom`)을 직접 필드로 받아 안전 영역 인셋과 합산합니다.

두 컴포넌트의 선택 기준: 화면 전체(배경 포함)를 안전 영역에 맞춰 실제로 줄여야 하면 `Root`를,
배경은 노치 아래까지 깔리고 콘텐츠만 피하면 되는 경우(예: 상단까지 이어지는 헤더 바)라면
`Padding`을 씁니다.

두 계열 모두 같은 `SafeAreaUtility`/`SafeAreaWatcher`(Runtime, `Screen.safeArea` 기반)를 공유합니다.
uGUI용 API는 `RectTransform`/`LayoutGroup`이 필요해 UI Toolkit에는 적용되지 않으므로, UI Toolkit
프로젝트는 `SafeAreaVisualElementPadding`을 사용해야 합니다. 사용 예제는 `Samples~/UIToolkitUsage`
참고(uGUI 예제는 `Samples~/BasicUsage`).
