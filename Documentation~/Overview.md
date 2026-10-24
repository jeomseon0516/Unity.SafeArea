# Jeomseon Unity Safe Area

[English](Overview.en.md) · [설치](../README.md)

## 목적

모바일 안전 영역 여백 적용과 Scene 패치·격리 Preview 도구를 제공합니다. uGUI root 크기 조정은 공식 SafeArea를 사용합니다.

## 사용 흐름

uGUI는 공식 SafeArea 또는 SafeAreaPadding 선택. UI Toolkit은 UIDocument에 Root 또는 Padding adapter 연결. Runtime 자동 패치는 Settings에서 명시적으로 활성화하고 Editor Patcher는 Apply Changes로 반영합니다.

## 계약과 제한

Root는 배경 포함 박스를 줄이고 Padding은 내부 콘텐츠만 이동합니다. SafeAreaIgnore는 runtime 패치 제외입니다. Preview는 원본 Scene이 아닌 복제본을 렌더링합니다. 임의 Canvas를 자동 변경하는 기본 정책은 꺼져 있습니다.

## 패키지 요구 사항

- Package ID: `com.jeomseon.unity.safe-area`
- Version: `0.4.0`
- Unity: `6000.6` / `0f1`
- `com.unity.ugui`: `2.6.0`
- `com.unity.modules.ui`: `1.0.0`
- `com.unity.modules.uielements`: `1.0.0`

## 샘플과 검증

Package Manager > Samples에서 다음 예제를 Import하세요. 포함된 Scene 또는 Editor 도구를 열고 샘플 README의 Setup 절차를 먼저 수행합니다.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [SafeAreaBasicUsageSample](../Samples~/BasicUsage/SafeAreaBasicUsageSample.unity)
- [UI Toolkit Usage](../Samples~/UIToolkitUsage/README.md)
  - [SafeAreaUIToolkitSample](../Samples~/UIToolkitUsage/SafeAreaUIToolkitSample.unity)

[테스트 코드](../Tests)

소비 프로젝트 manifest의 `testables`에 패키지 ID를 추가하고 Unity Test Runner의 관련 어셈블리를 실행합니다. 위 링크는 테스트 위치이며 이번 문서 수정으로 새 테스트 실행을 주장하지 않습니다.

## 코드 지도

- [SafeAreaIgnore](../Runtime/SafeAreaIgnore.cs)
- [SafeAreaPadding](../Runtime/SafeAreaPadding.cs)
- [SafeAreaRuntimeApplier](../Runtime/SafeAreaRuntimeApplier.cs)
- [SafeAreaSettings](../Runtime/SafeAreaSettings.cs)
- [SafeAreaUtility](../Runtime/SafeAreaUtility.cs)
- [SafeAreaWatcher](../Runtime/SafeAreaWatcher.cs)
- [SafeAreaVisualElementPadding](../Runtime/UIToolkit/SafeAreaVisualElementPadding.cs)
- [SafeAreaVisualElementRoot](../Runtime/UIToolkit/SafeAreaVisualElementRoot.cs)
- [SafeAreaPatcherWindow](../Editor/SafeAreaPatcherWindow.cs)
- [SafeAreaPreviewWindow](../Editor/SafeAreaPreviewWindow.cs)
- [SafeAreaScenePatcher](../Editor/SafeAreaScenePatcher.cs)
