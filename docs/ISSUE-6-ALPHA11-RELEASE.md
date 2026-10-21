# 반복 활동과 이야기 안내 구분 — v1.0.7-alpha.11 prerelease

<!-- alpha11-current-status -->
2026-10-21 14:48:43 KST에 소스·주석 태그 [`f3ee0af`](https://github.com/jungpaeng/dicemaster/commit/f3ee0afa0b68c2c9d10c9c8774e80db2d694490d)의 [alpha.11](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.11)을 게시했다. 반복 활동의 고정 반응과 개별 이야기를 구조로 구분하고 준비가 끝난 빈 지도 오버레이를 숨긴다. 전체·고정 커밋 재빌드 21개 프로그램·254개 입력·104개 소스/리소스·15개 참조·성공 기록·DLL/cfg/ZIP 동일성과 익명 공개 파일 대조를 통과했다. 기존 52개 Release·53개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·성능 확인과 #6 전체 완료는 대기다.** [설치·파일·확인표](ISSUE-6-ALPHA11-RELEASE.md).
<!-- /alpha11-current-status -->

[사용자 안내](../README.md) · [구현 계약](ISSUE-6-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [패드](GAMEPAD.md) · [이전 alpha.10](ISSUE-6-ALPHA10-RELEASE.md)

## 사용자에게 달라지는 점

사사형 메뉴의 개별 대화를 모두 본 뒤 반복 대련만 남아도, 대련 시작·결과의 고정 대사를 별도 이야기로 합쳐 지도에 남기지 않도록 구분한다. 메뉴 안의 각 이야기 조건을 먼저 확인하고 안내할 이야기만 지도에서 인물별로 합친다. 대련 메뉴나 게임의 실행 가능 여부는 바꾸지 않는다.

인물명·플래그·체크포인트·번역 문구의 제외 목록을 추가하지 않는다. 안전하게 구조가 확인되는 반복 활동과 그 고정 반응을 공통 규칙으로 구분한다. 아직 남은 개별 대화와 활동 안의 특별 조건·중첩 이야기·지원하지 않는 구조는 후보로 보존한다. 모든 반복 활동을 자동으로 완벽하게 분류한다는 의미는 아니다.

준비가 끝난 뒤 자동 안내할 항목이 없으면 지도 패널 전체를 숨긴다. 실제 경로·효과·임무 자료를 나누어 준비하는 동안만 `이 장소의 안내를 확인하는 중`을 표시한다. 영구적인 분석 제한·읽기 실패·게임 데이터 대기는 확인 중으로 합치지 않고 기존 로그 상세에 보존한다. 표시가 없음을 전체 게임의 기회 없음·반복 가능으로 해석하지 않는다.

## 지원 범위와 읽기 전용

이번에 고정 활동으로 증명하는 범위는 고정 대사 뒤 리터럴 Switch 하나가 평평한 두 갈래로 나뉘어, 한쪽은 정해진 자원을 차감하고 전투/핀볼로 진행하며 다른 쪽은 직접 거절 종료하는 닫힌 구조다. 그 활동에 연결된 평평한 두 결과의 자원 보상/손실·고정 대사·종료도 활동 반응으로 구분한다. 상점·음식 등 임의 활동 API 전체를 지원했다고 확대하지 않는다.

추가 특별 조건·중첩 선택·동적/미지원 호출·생략/상한이 있으면 활동으로 확정하지 않는다. 부모 선택 주제의 조건을 앞선 쓰기가 소모할 수 있으면 해당 대사를 단순 활동으로 제외하지 않는다. 실제 직렬화 조건 그래프의 의존 플래그를 읽고 쓰기 목록과 대조하며, 체크포인트 키와 플래그 이름이 같다고 가정하지 않는다. 동일한 상수의 SetFlag가 현재 값을 유지할 때만 영향 없음으로 볼 수 있고, 앞선 대화 초기화의 대상이거나 초기화 목록이 미확정이면 이 면제를 적용하지 않는다.

대화 선택지 초기화는 게임이 이미 로드한 Lua 환경에서 실제 관리자의 직렬화 플래그 목록을 읽어 영향만 마스킹한다. 원래 초기화·판정·RNG를 실행하지 않으며 현재 플래그 값을 저장하지 않는다. 목록을 확보하지 못하면 플래그 의존 비교는 보수적으로 미확정으로 두고, 독립적인 확정 false는 유지한다. 세이브·조건 횟수·능력치·아이템·게임 선택지를 변경하지 않는다.

기존 설정 네 개와 기본값·상위 설정, 상태 색·현재 선택·조건/효과 로그 상세를 유지한다. 분류 준비는 기존 한도 안에서 나누어 캐시하고 정상 0.25초/실패 5초·숨김/OFF 조회 중단·정상 주기 Lua 재분석 없음 계약을 유지한다. 키보드·마우스·터치와 Windows Xbox/XInput·입력 소유권·해제/중립·기존 이벤트 선택·로그 네 탭·미션 예약·주사위는 유지한다.

## 자동 검사·소스·파일

작업 시작 기준 main은 `5cc0b5d99d2349d1af5c76030bda03813dcad4f2`이다. 최종 고정 소스는 `f3ee0afa0b68c2c9d10c9c8774e80db2d694490d`이며 검사·파일·게시 값은 아래와 같다. 이전 후보의 검사 수·해시·Windows 확인을 이 후보에 승계하지 않는다.

SDK `8.0.426`, `2026-10-21T05:45:55.934895+00:00`의 전체 소스 후보에서 21개 프로그램·254개 입력·104개 소스/리소스·15개 참조·실제 DLL 연결·제공 게임 API 경계·설치 ZIP 검증을 통과했다. Python 배포 검증기 회귀 20개도 별도로 통과했다. 소스 `f3ee0afa0b68c2c9d10c9c8774e80db2d694490d`를 기존 관리 모드 DLL 0개인 격리 소스에서 같은 SDK·21개 프로그램·254개 입력·104개 소스/리소스·15개 참조로 재빌드해 전체 성공 기록·DLL/cfg/ZIP 바이트를 대조했다. [익명 공개 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.11/DiceMaster-v1.0.7-alpha.11.zip)도 같다. Release `419201423`·asset `661499876`, `draft=false`·`prerelease=true`·`make_latest=false`다. main·주석 태그를 atomic push했다. 게시 보충 문서는 별도 main 커밋이며 빌드 입력·태그·자산은 바꾸지 않는다.

| 검사 프로그램 | 이번 실행의 성공 기록 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.<br>PASS: 4265 managed behavior assertions and 1280 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2227 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 119 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5310 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2124 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |
| MapInformationTests | PASS: 440 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| MapRouteTests | PASS: MapRouteTests 5514 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed. |
| MapGuideRouteIntegrationTests | PASS: MapGuideRouteIntegrationTests 2976 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed. |
| ShopInformationTests | PASS: ShopInformationTests 967 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 3189 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |
| ChoiceStoryTests | PASS: ChoiceStoryTests 2098 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution. |

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 824320 | `5cafcb9575310dc745221bb4b19c0968e26462dd5ce2809975641dcbca1f560e` |
| DiceMaster.cfg | 12912 | `442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a` |
| DiceMaster-v1.0.7-alpha.11.zip | 348841 | `46b46dfe4f063750b71289c27d672ff8971297187c7e5892424bcec3f508b3ef` |

ZIP은 config·DLL 정확히 두 파일이며 CRC·내부 바이트·해시를 대조했다. 영수증은 `artifacts/issue-6/story-topics-candidate/full-final/build-manifest.json`에 보존한다.

로컬 전체 후보 영수증은 `artifacts/issue-6/story-topics-candidate/full-final/`, 고정 커밋 재빌드·게시 영수증은 `artifacts/releases/v1.0.7-alpha.11/`에 보관한다. 이 로컬 경로는 공개 다운로드 링크가 아니다. 관리 코드·모의 게임 경계·실제 DLL 연결 검사는 Windows 게임 실행·물리 입력·Unity 렌더링·프레임 측정을 대신하지 않는다.

## 설치와 실제 Windows 확인

설치 자산은 [DiceMaster-v1.0.7-alpha.11.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.11/DiceMaster-v1.0.7-alpha.11.zip)이다. 최신 정식은 [v1.0.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6)이다. BepInEx가 포함된 활협전용 모드 베이스가 필요하다. 업데이트는 게임 종료 후 DLL·cfg를 백업하고 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체해 cfg를 보존**한다. 최초 설치는 ZIP의 두 파일을 기존 BepInEx 아래에 넣는다. 문제가 있으면 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 저장한 게임 진행을 되돌리지 않는다.

실제 Windows·물리 Xbox/XInput·화면/가독성·성능 확인은 모두 미실행이다. [새 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha11-추가-확인--모두-미실행)에서 반복 대련만 남은 사사형·다른 인물의 같은 구조·남은 특별 대화·빈 패널·로그 상세·패드 복귀·지도 지연을 확인한다. 설치한 버전·소스·DLL 해시·환경·확인 일시와 실제 결과를 기록한다. v1.0.6의 #5 착수 조건 확인과 이전 후보에 대한 사용자 보고는 이번 DLL의 통과 근거가 아니다. **#6 전체 완료와 이슈 종료는 처리하지 않는다.**
