# 변경 기록

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

- TODO: Unity Device Simulator와 자체 프리뷰 창의 역할을 비교하고 통합 여부를 결정합니다.
- TODO: 전체 Canvas 자동 검색·구조 변경을 명시적 설정 기반 부트스트랩으로 교체할지 검토합니다.
- TODO: 안전 영역 갱신 이벤트의 정적 수명과 Enter Play Mode Options 호환성을 검토합니다.

## [0.1.0] - 2026-07-29

- JeomseonScriptPack의 SafeArea 모듈을 독립 UPM 패키지로 분리했습니다.


## [0.1.4] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.
