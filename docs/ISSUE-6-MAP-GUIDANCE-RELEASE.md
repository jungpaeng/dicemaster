# 지도 임무·후속 기회 안내 — v1.0.7-alpha.3

[사용자 안내](../README.md) · [분석 설계](ISSUE-6-MAP-GUIDANCE-ANALYSIS.md) · [상점 직접 효과](ISSUE-6-SHOP-DESCRIPTION.md) · [패드](GAMEPAD.md)

상태: **[prerelease 공개](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.3)·전체 자동 검사·고정 소스 재빌드·공개 ZIP 검증 완료 / 새 Windows 확인 대기**. main에서만 작업하며 별도 branch·worktree를 생성하지 않는다. 공개 alpha.1·alpha.2와 Latest 정식 v1.0.6을 보존한다. 이번 새 main 소스로 후보를 만들며 제외했던 과거 작업을 복원하지 않는다.

## 달라지는 동작

- 선택한 지도 장소의 고정 패널 하나에 대기 임무·현재 단계 후보·확인된 1회성을 구분한다. 일반 활동·고정 이야기·1회성 미확인은 기본 기회 수에서 제외하고 기타 연결 정보에 보존한다.
- 기존 대기 사전과 현재 조건 충족을 구분한다. 현재 키가 전진해도 기존 대기 주 임무 단계를 유지하고 동일 사전·같은 개수의 예약 값 교체도 다시 읽는다. 대기 사실과 현재 조건, 이번 클릭의 주/부 임무 우선순위를 별도로 설명한다.
- 장소 무관 임무는 로그의 별도 묶음이다. 관찰된 현재 지도 세션이 살아 있으면 장소 미선택에서도 조회하며 선택 장소의 행동력·활성 제한을 적용하지 않는다. 즉시 자동 실행을 보장하지 않는다.
- 메뉴 선택·리터럴 후속·Position 반환 변수를 통해 연결된 결과를 읽기 전용으로 탐색한다. 등록 덮어쓰기·scope·제어 흐름·중복 경로·순환·한도를 처리하고 경로가 불분명하면 확인 필요를 남긴다.
- 지도 요약과 개별 로그 행은 갱신 하나의 MapGuideSnapshot을 공유한다. 자동 패널은 이름·종류·상태, 조건·효과·경로는 선택한 로그 행의 상세에서 읽는다. 지도 전체의 배치·hover UI는 추가하지 않는다.
- 공개 alpha.2 이후 main의 상점 직접 효과 수정도 포함한다. 사용 효과와 책·비급의 다음 학습 효과를 원래 설명 아래에 표시하고 기존 상세 조회·설명 복원을 유지한다.

## 읽기 전용·지원 경계

시작점은 현재 `_positionData._defaultScript`다. 클릭 후에만 갱신되는 `_scriptName`으로 대체하지 않는다. 임무 관리자의 기존 필드만 읽고 지연 singleton/Unity 검색, 게임 조건 판정·임무 갱신·이벤트 추첨·세이브·구매·사용·학습을 호출하지 않는다. 게임의 클릭 처리 중 일어나는 플래그 변경도 미리 실행하지 않는다.

기본 활동 진입 전에 `_conditionFlags`가 있으면 게임은 조건의 참/거짓 양쪽에서 플래그를 바꾸거나 조건 체인에서 난수를 사용할 수 있다. 빈 배열로 확인된 경우 외에는 활동·도달 이벤트에 `진입 시 상태 변경 가능 · 경로 확인 필요`를 표시하고 현재 기회 수에서 제외한다. 현재 조건·1회성 증명은 별도로 보존한다. 그 전에 실행되는 대기 주/부 임무와 장소 무관 임무에는 이 활동 전용 경계를 적용하지 않는다.

대기 임무는 현재 조건이 달라져도 기존 대기로 보존하고 조건 충족 후보를 대기 등록으로 승격하지 않는다. 배치 주 임무의 행동력 예외·같은 장소 첫 부 임무 우선·장소 무관의 별도 경로와 제공 게임의 특정/구간/주기형 기간 의미를 따른다. 논리 임무 ID를 번역명에서 복원하지 않는다.

1회성은 기존의 엄격한 증명을 유지한다. 실제 API IL 감사에서 say의 치환/리스너 콜백, stage의 상태/가상 호출/완료 콜백, GetPortrait의 필드 변경, runwait의 주사위 코루틴 실행을 확인했다. 이름만으로 무해 API를 허용하거나 Issues를 버리지 않는다. 후속 메뉴 수집이 넓어져도 모든 실제 이벤트의 1회성을 판별하는 것은 아니다. 일반 선택지는 원래 활성 조건 참조가 없어 기본 판별 한계가 유지된다.

1회성 미확인은 반복 가능이 아니고 표식도 미독·영구 유일·발생 보장이 아니다. 메뉴·이동·선행 상태 변경 뒤의 조건은 현재 스냅샷만으로 확정하지 않는다. 자료 누락·준비·실패·한도 도달을 완전한 0건과 구분한다.

## 캐시·설정·조작

정상 0.25초·실패 5초·숨김/OFF 조회 0·정상 주기 Lua 재분석 없음 계약을 유지한다. 최초 경로 준비와 임무 색인을 나누어 진행하며 숨김/OFF에는 준비도 멈춘다. 선택 A→B→A·지도/관리 자료·Lua 문맥·저장/로드·장면·설정 경계에 이전 게시 권한을 폐기한다. OnGUI는 게시된 요약·배치만 읽는다. 실제 Windows 프레임/할당량은 별도 확인한다.

경로는 깊이 8·스크립트 32·문맥 128·종단 256·누적 작업 상한을 갖는다. 한 의미 명령의 큰 복사를 막기 위해 바인딩 48·scope 12·환경 128셀·분기/선택 16개·조건식 128토큰·전체 분기 복사/조건 1,024셀·테이블 64셀도 제한한다. 작업 수는 문자/토큰/명령의 논리 단위이며 CPU 시간이나 helper 내부의 모든 연산 횟수를 뜻하지 않는다. 상한 도달은 부분 확인이다.

임무 정의는 조회당 64 작업씩, 새 결과 효과·1회성은 조회당 새 루트 스크립트 하나씩 준비한다. 기존 효과 파서 자체와 그 내부의 제한된 후속 분석은 동기 실행을 유지한다. 경로 파서 전체와 모든 게임 비용을 프레임별 시간 예산으로 바꾼 것은 아니며 이 단일 결과 분석 비용도 실제 Windows 측정 대상이다.

네 bool 키와 기본 true를 유지한다. EnableMapGuide는 지도 임무 안내와 상세, EnableOneShotHints는 지도·선택지·팝업의 1회성, EnableBranchNotices는 실제 알림/20건, EnableShopEffectInfo는 상점 정보를 제어한다. 개별 즉시 적용·저장·OFF 정리·ON 과거 재생 없음, 상위 EnableOptionInfo 시작 시 적용과 설정 읽기 실패 격리를 유지한다. cfg의 지도 설명만 새 의미에 맞추며 기존 cfg를 덮어쓸 필요가 없다.

Tab/L3·R3 로그 열기 → 해제·중립 → Y 도구 → 지도 안내 조회에서 개별 항목과 상세를 읽는다. ↑↓/왼쪽 스틱·오른쪽 스틱·B/Esc·LB/RB를 유지하고 새 입력 체계를 추가하지 않는다. 새 행에 실행 Target·예약 ticket을 주지 않으며 #5 팝업·네 탭·명시적 예약·주사위와 키보드·마우스·터치·패드 입력 소유권을 보존한다.

## 자동 검사와 후보 파일

전체 소스 후보 빌드: 2026-10-21 03:22:18 KST · SDK 8.0.426 · **20개 검사 프로그램 PASS**. 빌드 전후 239개 입력·100개 소스/리소스·15개 읽기 전용 외부 참조를 대조했다. 전체 모드 컴파일 오류 0개, 기존 경고 7개다. 후보 검증기 Python 회귀 20개도 통과했다.

| 파일 | 크기 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 753664바이트 | `2176640a594c0343721ce33b708290ace25eacafe75b9e3542ec57badb2e5f2c` |
| DiceMaster.cfg | 12592바이트 | `61d654d992520b169377713e1d2e8484b533b38fe47348dcb4d486377e52cd37` |
| DiceMaster-v1.0.7-alpha.3.zip | 319341바이트 | `6c8844e1123dd243199c6f3ac0b9d1b1ee5de2a011cb071126f7b304debab935` |

ZIP은 정확히 config/DiceMaster.cfg·plugins/DiceMaster/DiceMaster.dll 두 파일이다. CRC·내부 바이트·네 bool 기본 true·게임 참조 DLL 미포함을 확인했다. 업데이트는 cfg를 덮어쓰지 않는다.

| 검사 | 실제 결과 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed. / PASS: 4265 managed behavior assertions and 1278 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2152 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 113 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5289 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2116 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |
| MapInformationTests | PASS: 176 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| MapRouteTests | PASS: MapRouteTests 5117 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed. |
| MapGuideRouteIntegrationTests | PASS: MapGuideRouteIntegrationTests 81 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed. |
| ShopInformationTests | PASS: ShopInformationTests 742 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 1611 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |

MapRouteTests는 번체 2,198개 본문의 색인/본문 해시와 실제 room/door/pharmacy 구문 표본을 확인했다. 풀 정의는 명시 대역이며 실제 이벤트 수·현재 게임 도달·Windows 실행을 검증한 것은 아니다. 실제 후보 DLL의 경로 IL·등록 덮어쓰기·빈 메뉴·미완결 주석·준비 예산도 PreviewIntegrationTests에서 실행했다. Runtime/Overlay의 같은 스냅샷 연결과 금지 게임 체인 직접 호출 부재는 후보 IL로 확인했다.

독립 검토는 잘못된 Met 재현·조건 플래그 진입 경계·큰 분기 복사·숨김/설정·단일 패널·읽기 전용 행을 검사했다. 작업 상한의 관리 실험을 실제 Unity 프레임 수치로 해석하지 않는다.

로컬 영수증은 artifacts/issue-6/map-guidance-candidate/full-final/의 build-manifest.json·build.log와 상위 independent-final-review/verification.json·one-shot-audit/다. 고정 커밋 재빌드·공개 배포 결과는 아래 별도 기록으로 보충한다. 분석 단계 자료·첫 full 후보의 결과와 최종 파일을 혼합하지 않는다.

## 공개 prerelease 게시와 검증

2026-10-21 03:26:15 KST에 **[v1.0.7-alpha.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.3)**을 게시했다. **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.3/DiceMaster-v1.0.7-alpha.3.zip)** · 빌드 소스/주석 태그 `9a9b6176e038595d55d72ef152a7df0910fb3a4e`. 원래 main에 구현 커밋과 태그를 atomic push했으며 상점 직접 효과 커밋 5e951cf도 포함한다. 별도 작업 branch/worktree는 만들지 않았다.

고정 커밋만 추출한 새 소스 스냅샷에는 기존 관리 모드 DLL이 0개였다. verify-preview-release.py --scope prd6로 다시 20개 검사 프로그램을 실행했고 검토 후보와 SDK·239개 입력·100개 소스/리소스·15개 외부 참조·전체 검사 결과·DLL/cfg/ZIP 바이트가 일치했다. Python 후보 검증기 회귀 20개도 통과했다.

Release `418727216`·asset `660123096`, draft=false·prerelease=true·make_latest=false다. 익명 공개 API·페이지·ZIP 다운로드에서 CRC·정확히 두 파일·내부 바이트·SHA-256을 확인했다. 기존 44개 Release·45개 태그·자산과 Latest 정식 v1.0.6을 보존했다. 이슈 #2 closed·#4 closed·#5 open·#6 open 상태도 유지했다.

영수증은 로컬 artifacts/releases/v1.0.7-alpha.3/의 release-verification.json·build-manifest.json·build.log·reviewed-candidate-comparison.json·publication-preflight-summary.json·github-publication.json·anonymous-verification.json·release-body.md에 보존한다. release-verification의 published=false는 게시 전 검증 시점이며 원본을 바꾸지 않는다. 게시 결과 보충은 별도 main 문서 커밋이며 위 빌드 소스·태그·설치 파일을 변경하지 않는다.

## 설치와 실제 Windows 확인

위에 연결한 이 버전의 설치 ZIP을 사용한다. 게임을 종료하고 기존 DLL/cfg를 백업한 뒤 **DLL만 교체해 cfg를 보존**한다. 최초 설치만 config·plugins 두 파일을 넣는다. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복구는 이미 진행·저장한 게임 상태를 되돌리지 않는다.

아래는 모두 **미실행·사용자 확인 대기**다.

- [ ] 설치 DLL 해시, 게임·Windows·BepInEx 버전, 해상도·글자 크기, 패드 모델·Steam Input 설정 기록.
- [ ] 선택 장소의 패널 하나와 대기/단계 후보/1회성 수·문자 상태 확인. 선택 변경·해제·로그/팝업·지도 종료에 오래된 패널이 남지 않음.
- [ ] 같은 게임 버전·세이브·시점에서 실제 대기 임무·현재 조건·우선순위 비교. 조건 충족 후보를 대기로 오해하지 않음.
- [ ] hover 없이 지도 로그의 개별 행·기타 정보·장소 무관 묶음 조회. 장소 미선택, 이동/선택 후 경로·확인 필요 표시 확인.
- [ ] 최초 준비·누락·실패·이전 값·한도와 빈 목록 구분. 후보가 많은 지도에서 최초/반복 조회 비용·할당량과 측정 방법 기록.
- [ ] 네 ON/OFF·재시작·상위 OFF·ON과 기존 설정 보존. OFF의 표식·자료가 남거나 과거 분기가 재생되지 않음.
- [ ] 분기 5초·20건·저장/로드/장면/OFF 초기화, 상점 직접 사용·학습 효과/설명 복원, #5 팝업·네 탭·예약·주사위 회귀.
- [ ] A/B 유지·로그/팝업 전환·Alt-Tab·패드 재연결의 해제/중립과 소유권, 키보드·마우스·터치 회귀. 조회로 RNG·카운터·예약·돈·재고·아이템·능력치·세이브가 바뀌지 않음.

자동 검사·공개 파일 검증과 실제 Windows·Unity·물리 Xbox/XInput 실행은 별도 근거다. 새 후보 확인 전에는 PRD #6 전체 완료·이슈 종료로 처리하지 않는다.
