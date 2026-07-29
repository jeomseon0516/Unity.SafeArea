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
- `SafeAreaRuntimeApplier`: 런타임 Canvas 자동 구성
- `SafeAreaPreviewWindow`: 에디터 프리뷰

Unity의 `Screen.safeArea`를 기반으로 동작합니다. Device Simulator가 제공하는 화면 시뮬레이션과 역할이 겹치는 프리뷰 부분은 향후 통합 여부를 검토합니다.
