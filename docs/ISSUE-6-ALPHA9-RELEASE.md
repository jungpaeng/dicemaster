# 지도 오버레이의 불가 항목 제외 — v1.0.7-alpha.9 prerelease

원래 작업 폴더의 `main`에서 지도 오버레이의 진행 불가 항목을 제외했다. **2026-10-21 06:25:46 KST에 [alpha.9](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.9)를 게시하고 익명 공개 ZIP을 대조했다.** 전체·고정 소스 재빌드의 21개 검사 성공 기록과 DLL/cfg/ZIP 바이트가 같다. 최신 정식은 v1.0.6이며 새 파일의 실제 Windows·물리 입력·가독성·성능 확인과 PRD #6 전체 완료는 대기다.

[구현 계약](ISSUE-6-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [공개 alpha.8 기록](ISSUE-6-ALPHA8-RELEASE.md) · [패드](GAMEPAD.md)

## 바뀌는 지도 표시

지도 자동 안내를 만들 때 내부 판정이 불가인 `Rank == 1` 항목을 모두 제외한다. 이름이나 상태 문자열을 검색해서 숨기지 않으며 기존 조건 판정 자체를 바꾸지 않는다. `진행 가능` 등 양성과 `진행 가능성 있음`은 유지한다. 필터 후 남은 항목만 최대 세 행과 `외 N개`에 계산한다.

제외된 항목의 상세 `Entries`·조건·불가 사유는 **로그 → 도구 → 지도 안내**에 그대로 남는다. 현재 선택 화면 오버레이·원래 게임 선택지는 변경하지 않는다. 정보 조회에서 실제 선택이나 게임 진행을 실행하지 않는다.

정상 빈 안내는 **`현재 표시할 인물 대화·임무·1회성 없음`**이다. 현재 설정·자동 표시 필터 안에서 남은 안내가 없다는 뜻이며 전체 게임의 기회 없음·반복 가능을 증명하지 않는다. 자료 준비 중 상태는 기존 별도 문구로 구분한다.

기존 인물 대화 필터·엄격 1회성·임무·색·설정 키/기본값과 패드·키보드·마우스·터치·입력 소유권·해제/중립을 유지한다. 읽기 전용·정상 0.25초/실패 5초·숨김/OFF 조회 중단·정상 주기 Lua 재분석 없음과 기존 캐시 계약도 유지한다. 필터로 판정·RNG·조건 횟수·예약·세이브·능력치·아이템을 바꾸지 않는다.

## 검사·후보 파일·사용자 확인 상태

SDK `8.0.426`, 생성 시각 `2026-10-20T21:23:29.116136+00:00`의 전체 후보에서 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조를 확인했다. 영수증은 `artifacts/issue-6/map-overlay-filter-candidate/full-final/build-manifest.json`에 보관한다. Python 배포 검증기 회귀 20개도 별도로 통과했다.

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
| MapInformationTests | PASS: 217 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| MapRouteTests | PASS: MapRouteTests 5421 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed. |
| MapGuideRouteIntegrationTests | PASS: MapGuideRouteIntegrationTests 415 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed. |
| ShopInformationTests | PASS: ShopInformationTests 967 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 2583 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |
| ChoiceStoryTests | PASS: ChoiceStoryTests 1261 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution. |

진행 불가 인물 대화·임무·1회성의 자동 제외와 상세 보존, 가능/불명 유지, 필터 후 세 행·나머지 수, 불가만 남은 목록·1회성 전용 설정을 기존 provider 경계에서 검증했다. 조건·순서·입력은 바꾸지 않고 캐시된 판정 결과로 표시 목록만 선별한다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 794624 | `b11b1af675cc656e87602e9fbedb98e28669497102708603028aa13813810362` |
| DiceMaster.cfg | 12912 | `442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a` |
| DiceMaster-v1.0.7-alpha.9.zip | 337698 | `fce934ca725b2eb9edfaeea7a69606c8a44d0ac820eeff5204a71b22eead4096` |

ZIP은 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 정확히 두 파일이며 CRC·내부 바이트·해시를 대조했다. 실제 DLL·제공 게임 API 연결 검사는 관리 코드 경계의 검증이다. 소스·주석 태그 [`1a782f2`](https://github.com/jungpaeng/dicemaster/commit/1a782f206f1aa7ab9f9e490125fbe916aabbad66)를 기존 관리 모드 DLL 0개인 격리 소스에서 SDK `8.0.426`·동일한 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조로 재빌드해 전체 성공 기록·DLL/cfg/ZIP 바이트를 대조했다. [공개 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.9/DiceMaster-v1.0.7-alpha.9.zip)도 같다. Release `418896174`·asset `660530763`, `draft=false`·`prerelease=true`·`make_latest=false`이며 기존 50개 Release·51개 태그·자산·Latest v1.0.6·이슈 상태를 보존했다. main과 주석 태그를 atomic push했다. 게시 보충 문서는 별도 main 커밋이며 빌드 입력·태그·설치 파일은 바꾸지 않는다. 새 DLL의 실제 Windows·Unity 화면·물리 입력·성능 확인은 별도다.

사용자의 **“아까 그 문제가 해결된 건 확인했어.”**은 alpha.8 내부 조건 안내 문제에 대한 부분 확인이다. 두 인물의 개별 만남·다른 기능·실제 Windows 전체 시험이 확인된 것으로 확대하지 않는다. 새 지도 필터·파일의 Windows 통과도 이 답변으로 대신하지 않는다.

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.9/DiceMaster-v1.0.7-alpha.9.zip)을 사용한다. 게임 종료 후 DLL/cfg를 백업하고 **DLL만 교체해 기존 cfg를 보존**한다. 문제가 있으면 종료 후 백업 DLL과 필요한 cfg 또는 정식 v1.0.6으로 복구한다. DLL 복원은 이미 저장한 게임 진행을 되돌리지 않는다.

## 실제 Windows 확인 — 모두 미실행

- [ ] 양성·불가·가능성 혼합 목록에서 지도 자동 패널은 불가 항목을 제외하고 로그 상세에는 해당 조건·사유를 유지한다.
- [ ] 남은 항목만 순서대로 최대 세 행·`외 N개`에 계산하며 불가 항목 때문에 뒤의 표시 대상이 빠지지 않는다.
- [ ] 자료 상태 안내가 없는 전체 불가 목록은 정상 빈 안내 문구를 사용하고 실제 기회 없음으로 오해하게 하지 않는다.
- [ ] 현재 선택 화면·원래 게임 선택지·색·설정·입력과 장소/메뉴 수명·로그 상세/복귀·OFF/ON을 유지한다.
- [ ] 실제 지도 프레임/할당량과 정상/실패/숨김 조회·warm 캐시 재사용·읽기 전용을 확인한다.

환경·설치 DLL 해시·관찰 결과를 [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)에 남기기 전에는 새 후보의 Windows 통과나 #6 전체 완료로 처리하지 않는다.
