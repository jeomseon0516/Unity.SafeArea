# Safe Area 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 정적 이벤트 수명 안정화**
   - Domain Reload 비활성화와 Play Mode 재진입 시 구독·초기화 상태를 재설정합니다.
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
   - 남은 작업: 자식 재배치 정책(현재는 항상 Canvas의 모든 자식을 이동)을 설정으로 노출하고, Custom
     Inspector에서 적용 대상 preview·Undo를 지원합니다.
4. **P2-01 — 화면 변화 감지 최적화**
   - 해상도, 방향, safe area가 실제로 바뀐 경우에만 레이아웃을 갱신합니다.
5. **P3-01 — Device Simulator와 중복되는 Preview 정리**
   - 추가 프리셋 가치가 없으면 자체 창을 축소하고 Simulator 연동 문서를 제공합니다.
