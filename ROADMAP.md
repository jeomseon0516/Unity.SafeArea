# Safe Area 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 정적 이벤트 수명 안정화**
   - Domain Reload 비활성화와 Play Mode 재진입 시 구독·초기화 상태를 재설정합니다.
2. **P1-01 — 명시적인 적용 대상 구성**
   - 모든 Canvas 검색과 자동 계층 변경 대신 컴포넌트 또는 설정 에셋으로 대상을 지정합니다.
3. **P1-02 — SafeArea Project Settings**
   - root 이름, 제외 tag, World Space와 자식 재배치 정책을 설정 에셋과 Inspector로 제공합니다.
4. **P2-01 — 화면 변화 감지 최적화**
   - 해상도, 방향, safe area가 실제로 바뀐 경우에만 레이아웃을 갱신합니다.
5. **P3-01 — Device Simulator와 중복되는 Preview 정리**
   - 추가 프리셋 가치가 없으면 자체 창을 축소하고 Simulator 연동 문서를 제공합니다.
