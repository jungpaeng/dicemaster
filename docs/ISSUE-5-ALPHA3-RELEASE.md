# DiceMaster v1.0.6-alpha.3 — 전체 로그 터치·팝업 행 재클릭 진행

alpha.2에서 남아 있던 전체 로그의 터치 처리 경로를 보완하고, 팝업에서 선택된 행을 다시 클릭·터치하면 바로 진행하도록 바꾼다. 마우스·터치 사용자는 Enter를 누르지 않고 행 또는 진행 버튼으로 확정할 수 있다.

**로컬 후보 검증 완료 · 고정 커밋 재빌드·공개 게시 대기.** 아래 검사·해시는 이번 후보에서 확인한 값이며, 고정 소스·공개 게시 결과는 완료 뒤 보충한다. 현재 [공개 alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.2)에는 전체 로그 터치 실패가 보고됐으며, [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5다. 새 alpha.3 DLL의 실제 Windows·장치 확인은 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다.

## alpha.2 이후 변경

### 전체 로그의 클릭·터치 처리

로그의 탭·목록·도구 버튼과 이벤트 검색 칸은 실제 누름·해제 이벤트에서만 포인터 입력을 기록한다. 검색 칸도 입력 처리 전에 영역을 확보해 Unity가 이벤트를 소비한 뒤 임시 영역을 읽던 경로를 제거한다. 검색 칸 클릭·터치로 초점을 둔 뒤 물리 키보드로 입력하며 화면 검색 자판은 추가하지 않는다. `rawType`에 입력 종류가 남아 있더라도 실제 이벤트가 `Layout`·`Repaint`·`Ignore`인 호출에서는 새 누름을 만들거나 기존 누름을 해제하지 않는다. 버튼이 입력을 소비한 `Used` 상태에서는 원래 입력 종류를 정리에만 사용한다.

별도 `TouchDown`·`TouchMove`·`TouchUp`은 해당 버튼 호출 동안 마우스 단계로 전달해 Unity의 버튼 영역·스크롤 잘림·누름 소유권·같은 버튼에서 해제 검사를 사용한다. 마우스로 인식되는 터치는 기존 마우스 경로를 사용한다. 다른 행·창 밖 해제, 스크롤 뒤 원래 영역과의 불일치, 문맥·대상 교체와 초점 상실은 실행을 취소한다.

alpha.2에서 버튼 영역을 이벤트 소비 전에 확보한 수정은 유지한다. 이번에는 남아 있던 입력 단계 해석과 별도 터치 전달을 보완한다. 실제 Windows에서 전체 로그의 native 창이 어떤 이벤트 순서로 받았는지는 관찰하지 않았으므로, 사용자 증상의 원인을 특정 Unity 창 필터 하나로 확정하지 않는다.

### 팝업의 선택 행 재클릭·터치 진행

| 입력 | 동작 |
| --- | --- |
| 다른 후보 행을 처음 클릭·터치 | 행을 선택하고 조건 설명·진행 방식을 확인한다. 이 클릭으로 실행하지 않는다. |
| 선택된 행을 새로 누르고 같은 행에서 해제 | 표시된 진행 방식으로 한 번 확정한다. 정상 후보는 일반 진행, 미충족·가중치 0·판정 미확인 후보는 명시 조건 무시다. |
| 선택된 무작위 행 재클릭·터치 | 원래 게임의 조건·가중치·난수 판정으로 진행한다. |
| 진행 버튼 클릭·터치 또는 A / Enter / Shift+Enter | 표시된 진행 방식으로 확정한다. |
| B / Escape 또는 무작위 버튼 | 원래 무작위 판정으로 진행한다. |

선택된 행을 누를 때 판정 토큰·대상 객체·결과·일반/조건 무시 방식을 함께 기록한다. 해제 전 그 값이 바뀌거나 일반 진행이 불가능해지면 실행을 거부한다. 같은 ID의 다른 대상·결과에 이전 입력을 적용하거나 일반 진행을 조건 무시로 자동 전환하지 않는다. 새로 표시된 상태를 확인하고 새 입력을 받아야 한다. 대상·결과를 읽을 수 없는 후보는 재클릭과 진행 버튼·A/Enter 모두 실행을 차단한다.

포인터 전환·입력 모드 변화 동안 같은 문맥의 유효한 누름을 보존하고, 이미 대기 중인 확정은 모드 변화에서 취소하는 alpha.2 보호를 유지한다. 초점·문맥·대상·연결·소유자 등 안전 경계와 화면 전환의 전체 해제·스틱/트리거 중립도 유지한다. 새 단축키·패드 조합·설정 키는 추가하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 게시 후 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.3.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

새 설정이나 기본값 이행은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용한다. 큰 로그 도구의 **이벤트 선택 팝업 사용: ON/OFF**를 저장하며 OFF는 미확정 대기를 원래 판정으로 돌려보낸다. alpha.2로 복구하면 팝업은 행 선택 후 진행 버튼·A/Enter로 확정하며 전체 로그 터치 잔여 문제가 있다. v1.0.5로 복구하면 이벤트 사전 예약 방식으로 돌아간다.

ZIP에는 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브를 배포하지 않는다.

## 검증·알려진 제한

전체 소스 후보와 기존 모드 DLL·Git·이전 빌드 출력을 제외한 격리 소스 재빌드가 각각 14개 검사 프로그램을 통과했다. 두 빌드의 DLL·cfg·ZIP 바이트가 일치했다. 고정 커밋 재빌드와 공개 다운로드 결과는 게시 단계에 보충한다.

| 확인 | 이번 결과 |
| --- | --- |
| 전체 소스 후보 빌드·자동 검사 | 14개 프로그램 통과 · SDK 8.0.426 · 모드 오류 0개/기존 경고 7개 |
| 격리 소스 재빌드·입력 목록·DLL/cfg/ZIP 동일 바이트 | 14개 통과 · 입력 166개/소스·리소스 69개 · 같은 DLL/ZIP 바이트 |
| 전체 로그의 실제/effective 입력 단계·소비된 이벤트 정리·마우스/별도 터치 회귀 | 공통 UI 2,743개 통과 · 기존 코드의 raw 해제/검색 포커스 실패 재현 |
| 팝업의 첫 선택·선택 행 재클릭·정상/조건 무시/무작위·읽기 실패 차단 | 팝업 981개 통과 · 마우스/변환 터치/별도 터치 |
| 누름 뒤 토큰·대상·결과·진행 방식 변경 및 중복 실행 거부 | 팝업 회귀·실제 컨트롤러 소스의 프레임 사이 입력 검사 통과 · 배포 DLL 연결 83개 |
| 고정 커밋 `--scope prd5` 재빌드·검토 파일 일치 | 준비 중 |
| 공개 태그·prerelease·Latest 보존·익명 다운로드 CRC/두 파일/바이트·해시 | 준비 중 |
| 실제 Windows 게임·트랙패드·마우스로 인식되는 터치·별도 터치·Xbox/XInput | 미실행 · 사용자 확인 대기 |

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API의 경계를 확인한다. 새 대역은 창 안의 유효 입력과 화면 갱신·무시된 입력을 구분하며 Unity 버튼의 마우스 단계 소비·잘림·누름 소유권을 모델링한다. 실제 Windows native 창의 전체 이벤트 전달이나 물리 장치·글꼴·화면 배치·성능까지 실행한 근거는 아니다.

alpha.2에 대한 사용자 보고는 팝업 터치 행 선택 성공과 전체 로그 터치 실패에 한정한다. 자세한 환경·설치 파일 식별값은 제공되지 않았으며 [후속 기록](ISSUE-5-ALPHA2-RELEASE.md#2026-10-20-사용자-후속-확인)에 보존한다. 이 결과를 새 alpha.3 DLL의 성공으로 승계하지 않는다.

팝업 실행 지원 범위와 후보 수의 의미는 기존 alpha.1·alpha.2와 같다. 실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원하며 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음의 조회이고 팝업은 지금 실행된 한 판정의 후보이므로 두 목록 수가 항상 같지는 않다. 큰 로그 이벤트는 조회 전용이며 미션 예약·해제와 주사위는 유지한다.

`EnableGamepadLogOpen=true`의 게임 초점·로그 닫힘 구간에서는 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 옵션을 끄고 남은 입력을 해제한다. Tab 진입의 첫 패드 입력과 Steam Input 구성별 동작도 실제 환경에서 확인한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md).

<details>
<summary>이번 전체 자동 검사 결과</summary>

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
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 2743 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 86 assertions across 17 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 981 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

재빌드 검증기 자체의 회귀 검사 14개도 통과했다. 기존 모드 DLL을 정상 빌드 입력으로 사용하지 않는다.

</details>

## 파일 확인·게시 기록

| 항목 | 이번 식별값 |
| --- | --- |
| 고정 소스 커밋·태그 | 준비 중 · `v1.0.6-alpha.3` 예정 |
| `DiceMaster-v1.0.6-alpha.3.zip` 크기·SHA-256 | 259190바이트 · `2a40b742263042bcfd3389bd867ac3e3c1abd310e1adb90d489dfaa11a8d2b47` |
| `DiceMaster.dll` 크기·SHA-256 | 614400바이트 · `4a8d7ba9b54bfcd9acd3b6b6675ef3d0328bcc30a3347d4a0c8284915472a59e` |
| `config/DiceMaster.cfg` 크기·SHA-256 | 11459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |
| 게시 시각·Release/asset ID | 미게시 |

사용자의 main 반영 지시에 따라 최신 `origin/main`을 기준으로 작업하고 README를 포함해 main에 반영한다. 새 태그·prerelease에는 검증한 고정 소스를 사용하며 기존 태그·Release·자산을 바꾸지 않는다. Latest 정식 v1.0.5와 이슈 #5 open을 유지한다. 실제 원격 확인 결과와 영수증은 게시 후 덧붙인다. 준비 문구를 실제 게시 사실로 읽지 않는다.
