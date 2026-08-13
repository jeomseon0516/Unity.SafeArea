# Jeomseon Unity Safe Area

모바일 화면의 안전 영역을 uGUI 레이아웃에 적용하고 에디터에서 미리 볼 수 있게 하는 UPM 패키지입니다.

## 설치

OpenUPM 등록 전에는 Package Manager의 **Add package from git URL**에서 다음 주소를 사용합니다.

```text
https://github.com/jeomseon0516/Unity.SafeArea.git#v0.1.2
```

## 구성

- `SafeAreaRoot`: `RectTransform` 앵커에 안전 영역 적용
- `SafeAreaPadding`: `LayoutGroup.padding`에 안전 영역 여백 추가
- `SafeAreaSettings`: root 이름, World Space Canvas 스킵 여부, 런타임 자동 패치 사용 여부를 정하는
  설정 에셋(`Jeomseon/Safe Area/Safe Area Settings` 메뉴로 생성, `Assets/Resources/SafeAreaSettings`
  경로에 두면 인식됨). 없으면 기본값(런타임 자동 패치 꺼짐)으로 동작합니다.
- `SafeAreaIgnore`: 이 컴포넌트가 붙은 Canvas는 SafeArea 패치 대상에서 제외됩니다.
- `SafeAreaRuntimeApplier`: `SafeAreaSettings.AutoPatchRuntimeCanvases`가 켜져 있을 때만 씬 로드마다
  모든 Canvas를 자동 패치합니다(기본값 꺼짐, 옵트인). `ApplyToAllCanvases()`로 언제든 수동 호출도
  가능합니다.
- `SafeAreaPreviewWindow`: 에디터 프리뷰

Unity의 `Screen.safeArea`를 기반으로 동작합니다. Device Simulator가 제공하는 화면 시뮬레이션과 역할이 겹치는 프리뷰 부분은 향후 통합 여부를 검토합니다.
