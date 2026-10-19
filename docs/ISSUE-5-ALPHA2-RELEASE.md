# DiceMaster v1.0.6-alpha.2 — A 확인 통일·클릭 처리 수정

alpha.1에서 나뉘어 있던 이벤트 팝업의 일반 진행과 조건 무시를 **A/Enter 확인 하나로 통일**하고, 로그 탭·목록·팝업 버튼의 클릭 처리 오류를 수정한다. 트랙패드와 마우스로 인식되는 모니터 터치에서 보고된 증상에 대응하는 시험판이다.

**게시 준비 중 · 로컬 후보 검증 완료 · 고정 커밋 재빌드·공개 게시 대기**. 예정 자산은 `DiceMaster-v1.0.6-alpha.2.zip`이다. [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5로 유지한다. 새 DLL의 실제 Windows 게임·트랙패드·터치·Xbox/XInput 확인은 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기하며 이슈 #5는 종료하지 않는다.

## alpha.1 이후 변경

| 선택한 후보 | A/Enter의 표시와 동작 |
| --- | --- |
| 정상 발동 가능 | `이 이벤트로 진행 [A/Enter]`으로 일반 진행 |
| 조건 미충족·최종 가중치 0·판정 미확인 | `조건 무시하고 이번만 진행 [A/Enter]`으로 명시 조건 무시 |
| 대상·결과 읽기 실패·오래된 값·소속 불일치 | 실행 차단 |
| 무작위 행 | 원래 게임의 조건·가중치·난수 판정으로 진행 |

**Shift+Enter도 같은 확인**이며 X의 조건 무시 단축키는 제거한다. B/Escape와 무작위 버튼은 원래 판정으로 진행한다. 표시된 일반 진행이 확정 직전 불가능해지면 차단하고 새로 표시된 진행 방식을 확인하게 하며 조건 무시로 자동 전환하지 않는다. 조건 무시는 이번 발동 자격만 우회한다. 능력치·호감도·스토리 플래그를 미리 바꾸거나 지정 이벤트 내부의 모든 이야기 조건을 통과시키는 기능은 아니다.

로그의 탭·목록·도구와 팝업 행·진행 버튼은 클릭으로 조작한다. 공통 GUI가 버튼을 처리하기 전에 실제 영역을 확보하도록 바꿔, Unity가 누름 이벤트를 소비한 뒤 임시 영역을 읽고 선택을 거부하던 경로를 수정한다. 팝업에서 누른 다음 프레임의 초기화가 누름 기록을 지우던 문제도 수정한다.

같은 유효 문맥에서 포인터로 전환하거나 게임 입력 모드가 바뀌는 동안 누름·해제는 보존한다. 입력 모드가 바뀌면 이미 대기 중인 확정은 폐기하고 새 확인을 받아야 한다. 초점·문맥·대상·연결·소유자 등 안전 경계에서는 누름을 취소한다. 게임 선택지 A→팝업, 팝업→게임·대화·주사위 전환의 전체 버튼 해제·양쪽 스틱/트리거 중립 보호는 유지한다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 게시 후 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.2.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

새 설정 키·기본값 이행은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용하며 큰 로그 도구의 **이벤트 선택 팝업 사용: ON/OFF**에서 저장한다. OFF는 미확정 대기를 원래 판정으로 돌려보낸다. v1.0.5로 복구하면 이벤트 사전 예약 방식으로, alpha.1로 복구하면 팝업 A/X 분리 조작으로 돌아간다.

ZIP에는 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브 파일을 배포하지 않는다.

## 검증·알려진 제한

전체 소스 후보와 기존 모드 DLL·Git·빌드 출력을 제외한 격리 소스 재빌드가 각각 14개 검사 프로그램을 통과했고 DLL·cfg·ZIP 바이트가 일치했다. 아래는 이번 후보의 결과이며 고정 커밋 재빌드·공개 다운로드 결과는 게시 단계에 보충한다.

| 확인 | 이번 결과 |
| --- | --- |
| 전체 소스 후보 빌드·자동 검사 | 14개 프로그램 통과 · 격리 소스 재빌드의 DLL/ZIP 바이트 일치 · 입력 166개/소스·리소스 69개 |
| Unity와 같은 버튼 누름 이벤트 소비·프레임 분리 누름/해제 회귀 | 공통 UI 2,654개 통과 · 기존 렌더러와 소비된 이벤트의 1px 모서리 대조 검사에서 결함 재현 |
| A/Enter의 정상·명시 조건 무시·읽기 실패 차단과 자격 변경 회귀 | 팝업 503개 통과 · 표시한 일반 확인이 조건 무시로 자동 전환되지 않음 |
| 포인터 전환/모드 변화 보존과 안전 경계 취소·대기 중 확정 폐기 | 실제 컨트롤러 소스와 팝업 검사 통과 · 배포 DLL 연결 83개 통과 |
| 고정 커밋 `--scope prd5` 재빌드·검토 DLL/cfg/ZIP 일치 | 기록 전 |
| 공개 태그·prerelease·Latest 보존·다운로드 CRC/두 파일/바이트·해시 | 기록 전 |
| 실제 Windows 게임·트랙패드·마우스로 인식되는 터치·별도 터치 입력·Xbox/XInput | 미실행 · 사용자 확인 대기 |

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API의 경계를 확인한다. 실제 장치의 성공 여부·화면 배치·Unity 글꼴·Windows 성능 확인과 구분한다. 트랙패드 클릭·마우스 에뮬레이션 터치가 같은 마우스 경로를 쓰더라도, Unity에 별도 터치 이벤트로 전달되는 입력은 별도로 확인해야 한다.

팝업 지원 범위는 alpha.1과 같다. 실제 manager의 직접 리터럴 점 `Position` 호출이 비메인 Lua 코루틴에서 실행되는 경로를 지원하며 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출 등은 원래 판정으로 진행한다. 큰 로그의 이벤트는 조회 전용이며 미션 예약·해제와 주사위 기능은 유지한다. 기본 `EnableGamepadLogOpen=true`의 게임 초점·로그 닫힘 구간에서는 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 옵션을 끄고 남은 입력을 해제한다.

큰 로그는 현재 스크립트에서 발견한 여러 판정 묶음을 보여주지만 팝업은 지금 실행된 한 판정의 후보를 보여준다. 두 목록의 수가 항상 같지는 않다. 이번 변경은 후보 수를 합치거나 실행 경로를 확장하지 않는다. 기존 Tab 진입의 첫 패드 입력 문제와 Steam Input 구성별 동작은 실제 환경에서 확인해야 한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md).

<details>
<summary>이번 전체 검사 결과</summary>

| 프로그램 | 실제 결과 |
| --- | --- |
| `LogRefreshTests` | PASS: 17 assertions against installed DLL method bodies and boundary fixtures. |
| `LiveLogEngineTests` | PASS: 16 refresh engine scenarios, 70 assertions. |
| `LifecycleTests` | Lifecycle assertions passed: 4023 |
| `LiveLogIntegrationTests` | PASS: 340 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| `RealGameApiTests` | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.<br>PASS: 3946 managed behavior assertions and 1058 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| `GamepadTests` | PASS: 262831 assertions. |
| `SourceBehaviorTests` | PASS: 3511 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| `PreviewIntegrationTests` | PASS: 1453 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| `LogUiModelTests` | PASS: 390 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| `LogUiWiringTests` | PASS: 83 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 2654 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 86 assertions across 17 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 503 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

빌드 SDK 8.0.426, 모드 컴파일 오류 0개·기존 경고 7개. 재빌드 검증기 자체의 회귀 검사 14개도 통과했다. 기존 모드 DLL을 정상 빌드 입력으로 읽지 않는다.

</details>

## 파일 확인·게시 기록

| 항목 | 이번 식별값 |
| --- | --- |
| 고정 소스 커밋·태그 | 기록 전 |
| `DiceMaster-v1.0.6-alpha.2.zip` 크기·SHA-256 | 258654바이트 · `7736cf3be9a6e986059d81af3e4fb9344ee4bb0ef9da1dfc95e5223034b2ac48` |
| `DiceMaster.dll` 크기·SHA-256 | 613376바이트 · `da49e4c2b874849a9ecdb39e9e41981a90e0857819da2a91bc0c42c25bdb7c6f` |
| `config/DiceMaster.cfg` 크기·SHA-256 | 11459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |
| 게시 시각·Release/asset ID | 기록 전 |

게시에는 `codex/prd5-prerelease` 작업 브랜치와 새 태그를 사용하고 기본 main을 직접 push하지 않는다. `prerelease=true`·`make_latest=false`와 Latest v1.0.5·기존 Release/태그/자산 보존을 확인한다. 영수증은 로컬 `artifacts/releases/v1.0.6-alpha.2/`에 보존하며 공개 링크는 게시 후 확인한 절대 URL로 기록한다. 소스 태그와 게시 결과를 보충하는 문서 커밋은 구분한다.
