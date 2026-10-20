# 이야기 경로의 진행 상태 — v1.0.7-alpha.8 후보

원래 작업 폴더의 `main`에서 인물 대화의 진행 상태를 첫 메뉴 조건과 분리하는 수정 후보를 구현했다. 전체 소스 빌드와 21개 검사 프로그램·250개 빌드 입력·103개 소스/리소스·15개 참조, 실제 DLL 연결·설치 ZIP 검증을 통과했다. **고정 커밋 재빌드·prerelease 게시·공개 파일 대조는 대기다.** 현재 공개 시험판은 [alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.7), 최신 정식은 v1.0.6이다. 새 DLL의 실제 Windows·물리 입력·가독성·성능 확인은 대기이며 PRD #6은 open이다.

[구현 계약](ISSUE-6-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [공개 alpha.7 기록](ISSUE-6-ALPHA7-RELEASE.md) · [패드](GAMEPAD.md)

## 바뀌는 판정과 문구

alpha.7은 첫 선택 조건의 현재값을 초록색으로 표시했다. 그러나 첫 메뉴를 선택할 수 있다는 사실과 그 뒤 특정 인물 이야기를 진행할 수 있다는 사실은 다르다. 실제 `center_001`의 단련 선택에는 별도 조건이 없어도, 이후 플래그 변경·`Position("Center_Free_001")`와 `Switch("Center_001_001")` 분기가 있다. 이 경로는 불명인 선행 처리와 탐색 상한 때문에 `진행 가능성 있음`으로 남을 수 있다. 해당 Switch의 실제 조건 설정이나 특정 만남의 불가 조건을 모두 판독했다고 주장하지 않으며, 별도 플래그를 이 조건으로 추정하지 않는다.

새 후보의 인물 대화 상태는 그 이야기까지 이어지는 경로를 대상으로 한다.

| 상태 | 의미와 표시 |
| --- | --- |
| `진행 가능` | 이야기 경로의 조건을 안전하게 확인한 양성. 상태 문구만 초록색 |
| `지금 진행 불가` | 모든 관련 경로의 미충족을 안전하게 입증한 경우. 중립색. 알려진 장소·행동 제한은 `지금 진행 불가 · 이유` |
| `진행 가능성 있음` | 상태 변경·풀 판정·미지원/동적·준비/실패/생략 자료 등으로 확정할 수 없는 경우. 중립색 |

`진행 가능성 있음`은 발생 확률을 계산했거나 실제 발생을 보장한다는 뜻이 아니다. 일부 조건의 true로 미확정 후속을 덮지 않고, 일부 false나 생략된 경로만으로 전체 불가를 단정하지 않는다. 현재 선택 화면에서는 원본 메뉴 위치가 겹치거나 연결이 준비 중·누락 상태이거나 선행 상태 변경이 있으면 가능성 상태를 유지한다. 인물 표시 자료만 일부 미확정이고 이야기 도달 조건은 안전하게 확인한 경로는 양성이 될 수 있다.

렌더러는 `진행 가능`·`발생 조건 충족`·`진행 조건 충족`·`선택 후 진행 가능`의 정확한 양성 상태 문구를 초록색으로 표시한다. 이름·제목·불가·가능성 문구는 중립색이며 갱신 실패의 이전 값을 새 양성으로 표시하지 않는다. 색과 함께 상태 문구를 제공한다.

## 보존하는 계약

플레이어 이외의 명시된 화자에 근거한 인물 대화 필터와 기본 휴식/독백 제외, 휴식 뒤 특수 연결 탐색, 기존 엄격 1회성·임무를 유지한다. 경로를 읽기 위해 Lua·게임 조건·RNG를 실행하거나 선행 효과를 적용하지 않는다. 세이브·능력치·아이템·예약·원래 게임 반환값을 바꾸지 않는다.

기존 설정 키·기본값·패드/키보드/마우스/터치와 입력 소유권·해제/중립을 유지한다. 정의 캐시·준비/현재 조회 상한, 정상 0.25초·실패 5초·숨김/OFF 조회 중단·정상 주기 Lua 재분석 없음 계약을 따른다. 자동 화면에 전체 효과 분석을 추가하지 않는다. 실제 Windows 성능은 별도 확인한다.

## 자동 검사·파일·게시 상태

SDK `8.0.426`, 생성 시각 `2026-10-20T21:03:29.872609+00:00`의 전체 후보에서 21개 검사 프로그램·250개 빌드 입력·103개 소스/리소스·15개 참조를 확인했다. 영수증은 `artifacts/issue-6/story-conditions-candidate/full-final/build-manifest.json`에 보관한다. Python 배포 검증기 회귀 20개도 별도로 통과했다.

| 검사 프로그램 | 이번 실행의 성공 기록 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.<br>PASS: 4265 managed behavior assertions and 1280 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2163 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 119 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5310 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2124 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |
| MapInformationTests | PASS: 208 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| MapRouteTests | PASS: MapRouteTests 5421 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed. |
| MapGuideRouteIntegrationTests | PASS: MapGuideRouteIntegrationTests 406 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed. |
| ShopInformationTests | PASS: ShopInformationTests 967 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 2583 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |
| ChoiceStoryTests | PASS: ChoiceStoryTests 1261 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution. |

첫 선택 무조건/내부 이야기 조건 미충족, 관련 플래그 쓰기·무관한 쓰기·불명 콜백·풀 이후 무조건 대화, 복수 경로 OR·누락·상한, 메뉴 위치·선행 변경, 장소 제한 우선 표시와 녹색의 정확한 상태 구분을 확인했다. SHA를 확인한 실제 연무장 스크립트와 별도의 합성 Switch 설정에서도 첫 선택만으로 양성이 되는 회귀를 차단했다. 합성 설정은 실제 게임의 만남 조건을 입증하지 않는다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 794624 | `da97db93c5a84c7dba5f6531e5d4f4c0f800cd90a00cfa0c2fc12295eb8c26a1` |
| DiceMaster.cfg | 12912 | `442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a` |
| DiceMaster-source-candidate.zip | 337694 | `460fc96c6990ec2e91b327b37cc07bfb0bc8b940d778f21b617f4c0cb6dd7443` |

ZIP은 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 정확히 두 파일이며 CRC·내부 바이트·해시를 대조했다. 실제 DLL 메서드와 제공 게임 API 연결 검사는 관리 코드 경계의 검증이며 게임 실행은 아니다. 고정 소스 재빌드·소스 커밋/태그·게시와 공개 다운로드 대조는 대기다. 새 DLL의 실제 Windows·Unity 화면·물리 입력·가독성·성능 확인은 별도로 남긴다.

## 설치와 실제 Windows 확인 — 대기

설치 후보가 확정되면 게임 종료 후 DLL/cfg를 백업하고 **DLL만 교체해 기존 cfg를 보존**한다. 문제가 있으면 종료 후 백업 DLL과 필요한 cfg 또는 정식 v1.0.6으로 복구한다. DLL 교체로 이미 저장한 게임 진행이 되돌아가지는 않는다.

[새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)의 alpha.8 항목에서 실제 조건 양성·입증된 불가·불명 문구, 연무장 같은 첫 선택/후속 분기, 장소·메뉴 수명, 기존 인물/1회성/임무와 물리 입력·화면·성능을 확인한다. 환경·설치 DLL 해시·관찰 근거를 남기기 전에는 Windows 통과나 #6 전체 완료로 처리하지 않는다.
