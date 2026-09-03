# Jeomseon Unity Safe Area

모바일 화면의 안전 영역을 uGUI/UI Toolkit 레이아웃에 적용하고 에디터에서 미리 볼 수 있게 하는 UPM
패키지입니다.

## 설치

요구 버전: Unity 6000.6.0f1 이상, uGUI 2.6.0 이상

OpenUPM 등록 전에는 Package Manager의 **Add package from git URL**에서 다음 주소를 사용합니다.

```text
https://github.com/jeomseon0516/Unity.SafeArea.git#v0.1.2
```

## 구성

### uGUI

- uGUI 2.6의 공식 `UnityEngine.UI.SafeArea`: `RectTransform` 앵커에 안전 영역 적용. 이 패키지는
  동일 기능을 다시 구현하지 않습니다.
- `SafeAreaPadding`: `LayoutGroup.padding`에 안전 영역 여백 추가
- `SafeAreaSettings`: root 이름, World Space Canvas 스킵 여부, 런타임 자동 패치 사용 여부를 정하는
  설정 에셋(`Jeomseon/Safe Area/Safe Area Settings` 메뉴로 생성, `Assets/Resources/SafeAreaSettings`
  경로에 두면 인식됨). 없으면 기본값(런타임 자동 패치 꺼짐)으로 동작합니다.
- `SafeAreaIgnore`: 이 컴포넌트가 붙은 Canvas는 런타임 자동·수동 패치에서 제외됩니다. Editor
  Patcher에서는 `Runtime Ignore` 체크박스로 추가·제거하며, 명시적인 SafeArea 구성과 독립적으로
  관리합니다. 같은 GameObject에 Canvas가 없으면 전용 Inspector가 Validator 경고를 표시합니다.
- `SafeAreaRuntimeApplier`: `SafeAreaSettings.AutoPatchRuntimeCanvases`가 켜져 있을 때만 씬 로드마다
  모든 Canvas를 자동 패치합니다(기본값 꺼짐, 옵트인). `ApplyToAllCanvases()`로 언제든 수동 호출도
  가능합니다.
- `SafeAreaPatcherWindow`(`Jeomseon/Safe Area/Patch Active Scene`): 활성 Scene의 실제 Hierarchy를
  펼치고 접을 수 있는 TreeView로 보여줍니다. Canvas별 `Safe Area`와 `Runtime Ignore` 상태, Validator
  사유를 확인한 뒤 `Apply Changes`를 눌러 두 컴포넌트의 추가·제거를 한 번에 적용합니다.
- `SafeAreaPreviewWindow`(`Jeomseon/Safe Area/Preview Window`): 열려 있는 Scene의 Canvas를 원본과
  격리된 PreviewScene에 복제해 자체 Camera/RenderTexture로 렌더링합니다. 원본 Scene의 SafeArea
  컴포넌트는 전혀 건드리지 않으므로, 이 창을 열어도 원본 Scene 상태는 항상 안전합니다. 값은 Unity
  내장 Device Simulator(`Window/General/Device Simulator`)의 `Screen.safeArea`를 기본으로 읽어오며,
  Override 토글로 임의의 값을 직접 입력해 확인할 수도 있습니다.

### UI Toolkit

- `SafeAreaVisualElementRoot`(`Jeomseon.Unity.SafeArea.UIToolkit`, 공식 uGUI `SafeArea`의 UI Toolkit
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
uGUI 공식 API는 `RectTransform`이 필요해 UI Toolkit에는 적용되지 않으므로, UI Toolkit
프로젝트는 `SafeAreaVisualElementPadding`을 사용해야 합니다. 사용 예제는 `Samples~/UIToolkitUsage`
참고(uGUI 예제는 `Samples~/BasicUsage`).

### 0.3.x 마이그레이션

`Jeomseon.Unity.SafeArea.SafeAreaRoot`를 제거하고 같은 GameObject에 uGUI 2.6의
`UnityEngine.UI.SafeArea`를 추가합니다. 기존 `applyLeft`/`applyRight`/`applyTop`/`applyBottom`은 공식
컴포넌트의 `Edges`에 대응합니다. `SafeAreaRuntimeApplier`와 Scene Patcher는 공식 컴포넌트를 자동으로
생성합니다.
