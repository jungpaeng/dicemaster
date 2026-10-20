# 조건·결과 한국어 표시 연결 — v1.0.6-alpha.17

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.17/DiceMaster-v1.0.6-alpha.17.zip) · [최신 정식판](https://github.com/jungpaeng/dicemaster/releases/latest) · [사용자 안내](../README.md)

## 변경

alpha.16에서 같은 Switch 조건을 로그에서는 일부 번역하고 팝업에서는 중국어·에셋 이름으로 표시하던 경로를 보완한다. 플래그의 설명, 호감도 대상, 책·잡물·귀중품·기능·개발 항목·도감과 날짜를 자료 종류에 맞는 이름으로 표시한다. 조건 이름과 참조 표현, 이벤트 활성·가중치 조건, 결과 요약·상세에 기존 번역 자료를 연결한다.

`Door_001_001`의 우소매·욱죽 당문 체류, 욱죽 호감도, 기간 같은 조건이 대상이다. `Book숫자_중국어이름`의 알려진 표시 이름은 ID 보호 전에 해석하며 미등록 ID와 원본 식별자는 보존한다. 없는 번역을 추측해 만들어 내지는 않는다.

조건·결과는 이름·문자열만 번역한다. 기존 `UiTextMapper.ApplyTranslations`의 GetFlag/GetDevelop 표현식 평가를 새 표시 경로로 가져오지 않는다. 이름 해석에는 이미 받은 참조의 필드·감사한 locale 키와 유한한 사전 조회를 사용한다. 난수 범위·미확정, AND/OR·비교식, 엄격 판정·회색, 선택·예약과 실행 권한은 유지한다.

이름 조회는 아이템/플레이어 singleton 준비를 전제하지 않고 locale 키를 바로 조회한다. 미확정 이름을 포함한 결과 설명은 5초 뒤 제한된 기존 예산 안에서 다시 읽는다. 외부 이름 매핑도 최대 5초 간격으로 추가 로드를 시도하며 내장 번역의 우선권을 유지한다. 파일 자체가 없거나 번역이 없는 경우에는 식별자를 유지할 수 있다.

결과는 번역 후 색상 정리·160자 요약·팝업 말줄임·높이 계산을 수행한다. 버튼·포커스·패드·터치·하단 텍스트 안내와 cfg 및 기본값은 바꾸지 않는다.

## 설치와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다. 게임을 종료하고 DLL/cfg를 백업한 뒤 업데이트는 ZIP의 `plugins/DiceMaster/DiceMaster.dll`만 교체해 cfg를 보존한다. 최초 설치는 ZIP의 두 파일을 기존 BepInEx 아래에 넣는다. GitHub 자동 Source code 압축 파일은 설치용이 아니다.

문제 시 게임 종료 후 백업한 alpha.16 DLL과 필요한 cfg를 복원한다. 복원하면 이번 이름·번역 연결 보완은 사라진다. DLL 복원은 이미 저장한 게임 상태를 되돌리지 않는다.

## 검증

2026-10-20 22:47 KST, .NET SDK 8.0.426 전체 소스 빌드에서 14개 검사 프로그램을 통과했다. 198개 빌드 입력·81개 소스/리소스를 대조했다. 고정 커밋 재빌드·패키지 대조는 게시 전에 확인한다. 실제 번역 리소스·제품 조건 표시기 IL을 연결한 원문 입력 검사, GetFlag 미실행·순환 보호·원본 ID 보존·효과색·알려진 Book 이름 매핑·locale 준비 후 회복을 포함한다. 기존 RNG·카운터 비변경과 UI/입력·예약·Lua 회귀 검사도 수행한다.

이 DLL의 실제 Windows·Unity 화면·물리 패드·터치 확인은 대기다. 기존 버전의 사용자 확인을 이 DLL에 승계하지 않는다. 이슈 #5 완료를 주장하지 않는다.

## 자동 검사·파일 식별값

| 검사 | 결과 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed. / PASS: 4265 managed behavior assertions and 1278 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3540 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2101 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 112 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5232 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2069 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 654848 | `a8efd430371d75731539754bf1fef60f9af3203fd0655778de30218bb68659f2` |
| DiceMaster-v1.0.6-alpha.17.zip | 275235 | `07dc10aa1cb180e82764fe451549f5656956d90312f899e79990e645d4a38e9b` |
| DiceMaster.cfg | 11761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

ZIP은 cfg와 DLL 두 파일로 구성되며 게임 DLL·소스는 포함하지 않는다. 고정 커밋의 깨끗한 재빌드와 공개 다운로드의 일치는 게시 후 기록한다. 배포 영수증은 로컬 `artifacts/releases/v1.0.6-alpha.17/`에 보관한다.

