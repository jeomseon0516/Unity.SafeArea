# Safe Area 기본 예제

`SafeAreaBasicUsageSample.unity`를 열어 확인합니다.

## Scene 구성

- **Canvas**
  - **Safe Area Panel** — `SafeAreaRoot` 부착(초록색 반투명). 화면 전체를 채우다가 Safe Area 인셋에
    맞춰 anchor가 줄어듭니다. `SafeAreaScenePatcher`/`SafeAreaRuntimeApplier`가 자동으로 새
    `SafeAreaRoot`를 만들지 않고 이미 있는 이 컴포넌트를 그대로 사용한다는 것도 함께 확인할 수
    있습니다(명시적 배치 우선).
  - **Header** — `VerticalLayoutGroup` + `SafeAreaPadding`(`useTop`만 켜짐, 파란색 반투명) 부착.
    상단 Safe Area 인셋만큼 padding.top이 늘어납니다. 자식 **Label**은 헤더 문구를 표시합니다.
- **Ignored Canvas** — `SafeAreaIgnore` 컴포넌트가 Canvas 자체에 부착돼 있어 패치 대상에서
  제외됩니다. 자식 **Full Screen Marker**(빨간색 반투명)가 Safe Area와 무관하게 항상 화면 전체를
  채우는 것으로 제외가 정상 동작하는지 확인합니다.
- **Safe Area Debug Print** — 기존 `SafeAreaSample` 컴포넌트. 컨텍스트 메뉴로 현재 Safe Area
  인셋 계산 결과를 Console에 출력합니다.
- **EventSystem** — 표준 UGUI 이벤트 시스템(이 씬 자체에는 클릭 상호작용이 없지만 관례상 포함).

## 확인 절차

1. Game View에서 시뮬레이션할 디바이스(노치/펀치홀 있는 기종)를 선택하거나 Device Simulator를 켠 뒤
   Play Mode로 진입합니다.
   - `SafeAreaSettings` 에셋이 프로젝트에 없어도(기본값 fallback) Safe Area Panel과 Header가 정상
     동작해야 합니다.
2. `Safe Area Panel`(초록색)이 Safe Area 인셋만큼 안쪽으로 줄어드는지, `Header`(파란색)의 위쪽
   padding이 늘어나는지 확인합니다.
3. `Ignored Canvas`의 `Full Screen Marker`(빨간색)는 Safe Area와 무관하게 항상 화면 전체를 채우는지
   확인합니다.
4. Edit Mode에서 `Header`를 선택하고 Inspector의 `Vertical Layout Group > Padding > Top` 값을 직접
   바꿔봅니다. 값이 유지되고, 이후 `SafeAreaPreviewWindow`나 디바이스 시뮬레이터로 Safe Area 값을
   다시 바꿨을 때 방금 입력한 값 위에 인셋이 더해지는지 확인합니다(전에는 최초 캐싱값으로 되돌아가는
   버그가 있었습니다). `Ctrl+Z`로 Undo가 되는지도 함께 확인합니다.
5. `Jeomseon/Safe Area/Patch Active Scene` 메뉴를 실행합니다. `Canvas`는 이미 `SafeAreaRoot`가 있어
   변화가 없어야 하고, `Ignored Canvas`는 `SafeAreaIgnore` 때문에 전혀 건드려지지 않아야 합니다.
6. `Jeomseon/Safe Area/Preview Window`(또는 해당 메뉴)를 열어 Safe Area 프리뷰가 `Safe Area Panel`에
   정상 반영되는지 확인합니다.
7. `Safe Area Debug Print`의 컨텍스트 메뉴 `Safe Area 출력`을 실행해 Console에서 인셋 값을 확인합니다.
8. (선택) `Assets/Resources/`에 `Jeomseon/Safe Area/Safe Area Settings` 메뉴로 `SafeAreaSettings`
   에셋을 만들고 `Auto Patch Runtime Canvases`를 켠 뒤 Play Mode에 재진입해, 런타임 자동 패치
   옵트인이 정상 동작하는지 확인합니다.
