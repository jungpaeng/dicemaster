# 현재 필수 조건의 false 보존 — v1.0.7-alpha.10 prerelease

<!-- alpha10-current-status -->
2026-10-21 07:05:30 KST에 소스·주석 태그 [`05c4880`](https://github.com/jungpaeng/dicemaster/commit/05c4880ff11e0e8a79ea545495b04c6c28772bed)의 [alpha.10](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.10)을 게시했다. 랜덤 AND의 확정 false와 현재 필수 선택 조건의 미충족을 보존하여 불가능한 대화의 자동 지도를 정리했다. 전체·고정 커밋 재빌드 21개 프로그램·252개 입력·103개 소스/리소스·15개 참조·성공 기록·DLL/cfg/ZIP 동일성과 익명 공개 파일 대조를 통과했다. 기존 51개 Release·52개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·성능 확인과 #6 전체 완료는 대기다.**
<!-- /alpha10-current-status -->

[구현 계약](ISSUE-6-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [이전 alpha.9](ISSUE-6-ALPHA9-RELEASE.md) · [패드](GAMEPAD.md)

## 바뀌는 동작

`랜덤값 비교 AND 우소매가 당문에 있음 AND 날짜 조건`에서 인물 부재가 확정 false이면 전체 AND는 false다. RNG는 실행하지 않고 미확정으로 두어도 다른 필수 조건의 false는 보존한다. 비교 순서를 바꾸거나 Lua의 별도 체크포인트를 AND로 연결해도 같은 결론이다. OR의 다른 참 대안과 Switch의 선행 대안은 함께 고려한다.

남제자방 사사형·여제자방 소사매의 원래 선택지가 꺼졌는데 지도에만 남던 경우, 현재 루트 메뉴의 필수 guard를 새로 읽어 자동 표시에서 제외한다. NPC의 첫 대화 이력만 보고 모든 대화를 지우지 않으며 일반 대화와 확인 못 한 대안은 유지한다. 첫 guard가 참이라는 이유로 전체 이야기의 `진행 가능`을 만들지 않는다.

자동 지도 목록은 alpha.9의 불가 항목 제외를 그대로 사용한다. 제외한 항목의 조건·사유는 로그 → 도구 → 지도 안내에서 조회한다. 이는 현재값에 따른 표시 제외이며 진입 뒤/미래 실행 불가의 증명이 아니다.

## 읽기 전용과 보호 범위

게임 Condition/Switch/Execute·RNG·선택·세이브를 호출하지 않는다. 비교 단위로 지원하지 않는 조건·예외를 Unknown으로 격리하고 확정된 AND false·OR true만 합성한다. Switch는 가능한 결과 인덱스를 최대 257개로 제한하며 사용 횟수 의존은 Unknown이다.

루트의 정확한 연속 화면 준비 다섯 문장만 현재값 투영 대상으로 구분한다. 그 화면/Fungus block 전체의 상태 불변을 증명한 것은 아니다. 전체 이야기의 양성 판정은 기존 보수 규칙을 유지한다. 선행 플래그 쓰기는 읽는 시점에 해당 의존성을 마스킹하고, 임의 콜백·재바인딩·동적 코드·생략 경로·상한은 가능한 대안을 남긴다. 현재 조건이 false인 모든 대안을 확인하지 못하면 가능성으로 유지할 수 있다.

경로가 많으면 모든 관찰 경로의 공통 AND 필요조건을 정의 인덱스에서 압축한다. 관찰한 우회 경로가 하나라도 있으면 그 조건을 공통으로 삼지 않는다. fresh 등록 직후의 정확한 terminal 후속 실행은 비어 있는 모든 남은 scope를 확인한 뒤 등록된 대상을 계속 추적한다. 실행 호출의 상태 불변을 선언하지 않으며 자식 경로의 불확실성과 누락 보호는 유지한다.

정상 0.25초/실패 5초·숨김/OFF 조회 중단·정의 캐시·정상 주기 Lua 재분석 없음과 네 설정·기본값을 유지한다. 지도 그룹 16개/경로 예산 32개 안에서 필요조건 투영을 추가하고 구조 생략 prefix도 최대 32개로 제한한다. 물리 성능 측정은 별도다. 패드와 기존 입력 소유권·해제/중립·이벤트 선택·로그 네 탭·미션 예약·주사위는 변경하지 않는다.

## 자동 검증

SDK `8.0.426`, `2026-10-20T22:02:42.050376+00:00`의 전체 소스 후보에서 21개 프로그램·252개 입력·103개 소스/리소스·15개 참조·실제 DLL 연결·제공 게임 API 경계·설치 ZIP 검증을 통과했다. Python 배포 검증기 회귀 20개도 별도로 통과했다. 소스 `05c4880ff11e0e8a79ea545495b04c6c28772bed`를 기존 관리 모드 DLL 0개인 격리 소스에서 같은 SDK·21개 프로그램·252개 입력·103개 소스/리소스·15개 참조로 재빌드해 전체 성공 기록·DLL/cfg/ZIP 바이트를 대조했다. [익명 공개 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.10/DiceMaster-v1.0.7-alpha.10.zip)도 같다. Release `418927336`·asset `660620010`, `draft=false`·`prerelease=true`·`make_latest=false`다. main·주석 태그를 atomic push했다. 게시 보충 문서는 별도 main 커밋이며 빌드 입력·태그·자산은 바꾸지 않는다.

| 검사 프로그램 | 이번 실행의 성공 기록 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.<br>PASS: 4265 managed behavior assertions and 1280 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2182 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 119 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5310 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2124 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |
| MapInformationTests | PASS: 217 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| MapRouteTests | PASS: MapRouteTests 5490 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed. |
| MapGuideRouteIntegrationTests | PASS: MapGuideRouteIntegrationTests 1291 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed. |
| ShopInformationTests | PASS: ShopInformationTests 967 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 2816 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |
| ChoiceStoryTests | PASS: ChoiceStoryTests 2007 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution. |

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 803328 | `6ab3d39f3a1730f85a86bb8de3b277487d205128dbe0571e6549249036482d12` |
| DiceMaster.cfg | 12912 | `442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a` |
| DiceMaster-v1.0.7-alpha.10.zip | 340957 | `b6df91798112d5d22f01f2fadd38c6bd4f154023d485213d877be7bbe9df5bd4` |

ZIP은 config·DLL 정확히 두 파일이며 CRC·내부 바이트·해시를 대조했다. 영수증은 `artifacts/issue-6/map-current-blockers-candidate/full-final/build-manifest.json`에 보존한다.

SHA를 확인한 실제 원문의 메뉴·후속 경로에 명시적 합성 조건/flag를 연결해 첫 대화 전후·캐시 재사용·확정 false와 unknown 대안을 검사한다. 실제 직렬화 조건 설정·사용자 세이브·Windows 게임을 실행한 결과는 아니다.

## 설치·복구와 실제 Windows 확인

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.10/DiceMaster-v1.0.7-alpha.10.zip)의 소스·해시는 위 기록을 따른다. Windows용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 ZIP의 `plugins/DiceMaster/DiceMaster.dll`만 교체해 기존 cfg를 보존한다. 최초 설치는 config·plugins를 BepInEx 아래에 넣는다. GitHub Source code 압축은 설치용이 아니다.

문제가 있으면 종료 후 백업 DLL/cfg 또는 정식 v1.0.6으로 복구한다. DLL 복원은 이미 저장한 게임 진행을 되돌리지 않는다. [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)의 alpha.10 항목은 모두 미실행이다. 새 파일의 사용자 확인 전에는 전체 완료나 이슈 종료로 처리하지 않는다.
