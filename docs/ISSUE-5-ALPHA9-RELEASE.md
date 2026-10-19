# DiceMaster v1.0.6-alpha.9 — 클릭·터치 입력 처리 정리

로그창과 이벤트 팝업의 클릭·터치 처리를 공유하고, 입력 전환 시 요청을 보존하거나 취소하는 경계를 명시한다. alpha.8의 화면·조작·설정은 유지하며 후속 입력 수정을 두 화면에 일관되게 적용하기 위한 내부 구조 정리다.

**배포 준비 중이다.** 전체 후보·격리 소스 재빌드에서 각각 14개 검사 프로그램과 동일 DLL/cfg/ZIP·검사 결과를 확인했다. 이 문서를 포함하는 고정 소스 커밋 재빌드·main/태그 반영·prerelease·익명 공개 다운로드 검증은 아직 실행 전이다. 현재 공개 시험판은 [alpha.8](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.8)이며 [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5다. 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다.

## alpha.8 이후 변경

### 공통 포인터 경계

`LogUI/ImGuiPointerButton`은 각 화면이 확보한 Rect와 스타일을 받아 네이티브 `GUI.Button`을 한 번 호출하고 직접 터치를 마우스 누름/해제로 변환한다. 마우스·마우스로 에뮬레이션된 터치·직접 터치를 같은 경계로 검증하며 소비된 `Used` 이벤트를 보존한다. 소비되지 않은 터치는 예외가 나더라도 원래 이벤트 타입으로 되돌린다. `rawType`만 남은 Layout/Repaint/Used는 새 입력으로 해석하지 않는다.

각 Renderer의 배치·선택·예약 ticket·문맥 검증과 팝업 토큰·대상·결과·일반/조건 무시 의도 검증은 유지한다. 공통 어댑터에 Lua 실행 권한이나 각 화면의 선택 정책을 넣지 않는다. 반환값은 readonly struct이며 버튼마다 배열·콜백·참조형 요청 객체를 추가하지 않는다.

### 명시적인 취소 사유와 적용 경로

`LogUI/ModalInputResetPolicy`가 초기화 사유를 효과로 판정하고 `DicePlugin.ResetEventInput`이 적용한다. 기존 `LogPadController`와 Renderer의 상태 소유권은 유지한다.

| 사유 | 유효한 누름 보존 | 접수된 확정 취소 |
| --- | --- | --- |
| 자동 입력 모드 관측·실제 포인터 전환 | 예 | 아니요 |
| 물리 패드 재획득·컨트롤러 경계·로그 표시 전환·앱 초점 전환 | 아니요 | 예 |
| 팝업 대기 상태 변경 | 아니요 | 아니요 |

패드 프레임에 `ResetFocus`가 없으면 적용하지 않는다. 기존 `PreservePointerTransaction`·`PreserveNavigationArea`를 초기화 사유로 번역하며 실제 모드 관측을 새 소유자로 취급하지 않는다. Runtime의 문맥 교체·비활성화·종료 무효화와 alpha.8의 확정 당시 후보·결과 스냅샷 검증을 유지한다.

첫 클릭·터치로 선택하고 선택된 같은 행을 다시 눌러 같은 행에서 떼면 일반/명시 조건 무시를 요청한다. 초기 미선택·A/Enter 확인·B/Esc 무작위·하단 텍스트 안내, 굵은 두 줄 목록·효과색·회색 후보·독립 상세 스크롤과 로그 도구 화면 유지를 그대로 사용한다. **무작위는 B/Esc 전용이며 터치만으로 무작위 진행하는 UI는 없다.**

별도 요청 객체·전체 상태 머신·UI 프레임워크 전환은 수행하지 않았다. 단일 net48 DLL·기존 IMGUI·설정·Lua 게이트·자격 평가·캐시는 유지한다. 기존의 반복 터치 문제가 실제 Windows에서 해결됐다는 새 사용자 보고는 없으며 이번 내부 정리를 추가 장치 수정 완료로 설명하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다. 게시 후 이 버전 Release의 Assets에서 `DiceMaster-v1.0.6-alpha.9.zip`을 사용한다. 현재는 새 자산이 게시 전이다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 게시된 설치 ZIP을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

설정·기본값 변경은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`가 필요하며 로그 도구 ON/OFF 저장을 유지한다. alpha.8로 복구하면 이번 내부 구조 정리 전 구현을 사용하고 v1.0.5는 이벤트 사전 예약 방식이다. ZIP에는 DLL·cfg 두 파일만 포함하며 게임 소스·게임 DLL을 배포하지 않는다.

## 검증·알려진 제한

2026-10-20 03:36:14 KST의 전체 소스 후보와 03:36:13 KST의 격리 소스 재빌드가 각각 **14개 검사 프로그램**을 통과했다. SDK는 8.0.426이며 모드 컴파일은 오류 0개·기존 경고 7개다. 빌드 입력 179개·모드 소스/리소스 73개를 확인했고 격리 빌드는 기존 모드 DLL 0개에서 시작했다. 두 빌드의 DLL·cfg·ZIP 바이트와 전체 검사 결과가 일치하며 현재 179개 빌드 입력의 SHA-256도 후보 manifest와 일치한다. ZIP의 CRC·정확히 두 파일·내부 DLL/cfg 바이트·해시를 대조했다.

런타임은 **실제 후보 DLL의 모드 관측·패드 프레임 적용·입력 초기화 메서드와 정책 enum/효과 타입/판정 IL**을 fixture에 복사하고 소스 팝업·Runtime·실제 MoonSharp Lua 재개를 연결한다. 정책을 대역 구현으로 바꿔 검사하지 않는다. 자동 모드 관측·포인터 전환의 접수 보존과 물리 패드·초점·로그 전환의 취소, 일반/명시 조건 무시/무작위의 결과 한 번 소비를 포함해 150개 시나리오·999개 단언을 확인했다.

실제 후보 정책을 과잉 취소로 변이시키면 늦은 모드 관측 뒤 첫 Lua 완료 단언이 실패하고 실제 적용 메서드에서 취소 호출을 제거하면 물리 패드 전환의 실행 차단 단언이 실패하는 음성 대조도 확인했다. 정상 후보 재실행은 통과했다. 포인터 어댑터의 38개 계약은 마우스·에뮬레이션 터치·직접 터치, 소비 상태·clipping/capture·예외 시 원복을 검사한다. 이 검사는 관리 코드·명시적인 Unity/게임/native 대역과 실제 참조 API 경계를 대상으로 하며 Windows 게임 실행이 아니다.

| 확인 범위 | 이번 상태 |
| --- | --- |
| 전체 후보·격리 재빌드 | 각각 14개 검사 프로그램 통과 · 동일 DLL/cfg/ZIP·검사 결과 |
| 빌드 입력·소스/리소스 | 179개 입력·73개 소스/리소스 SHA-256 확인 |
| 실제 후보 정책·적용 IL → 팝업/Runtime → MoonSharp Lua 결과 한 번 소비 | 런타임 150개 시나리오·999개 단언 통과 |
| 공통 포인터 처리·팝업 입력/표시·실제 후보 연결 | 어댑터 계약 38개 포함 팝업 1,576개 · DLL 연결 90개 통과 |
| 기존 로그·미션·패드·주사위·미리보기·실제 참조 API | 아래 14개 프로그램 결과 |
| 고정 커밋 `--scope prd5` 재빌드·검토 파일 일치 | 실행 전 |
| main/태그 반영·prerelease·익명 공개 ZIP 검증 | 실행 전 |
| 기존 32개 Release·태그·자산·Latest v1.0.5·이슈 상태 보존 | 사전 상태 보관 · 게시 후 재대조 예정 |
| 실제 Windows 게임·트랙패드·터치·Xbox/XInput | 미실행 · 사용자 확인 대기 |

alpha.8의 실제 Windows 성공 보고는 제공되지 않았다. 이전 버전의 제한된 사용자 보고와 v1.0.5의 선행 확인을 이번 새 DLL의 장치 확인으로 승계하지 않는다. 실제 Windows 글꼴·배치·물리 입력·Steam Input 구성·다른 모드 조합과 성능은 별도 확인이다.

결과 설명은 정적 분석 범위이며 모든 미래 분기·난수 결과를 보장하지 않는다. 팝업은 실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원한다. 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음을 조회하고 팝업은 현재 판정 후보여서 목록 수가 항상 같지는 않다. 큰 로그 이벤트는 조회 전용이며 미션 예약·해제와 주사위는 유지한다.

게임 초점·로그 닫힘 상태에서 패드 열기가 켜져 있으면 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 `EnableGamepadLogOpen=false`로 바꾸고 남은 입력을 해제한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

<details>
<summary>이번 전체 소스·격리 재빌드의 14개 자동 검사 결과</summary>

| 프로그램 | 실제 결과 |
| --- | --- |
| `LogRefreshTests` | PASS: 17 assertions against installed DLL method bodies and boundary fixtures. |
| `LiveLogEngineTests` | PASS: 16 refresh engine scenarios, 70 assertions. |
| `LifecycleTests` | Lifecycle assertions passed: 4023 |
| `LiveLogIntegrationTests` | PASS: 340 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| `RealGameApiTests` | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.<br>PASS: 3946 managed behavior assertions and 1058 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| `GamepadTests` | PASS: 262831 assertions. |
| `SourceBehaviorTests` | PASS: 3511 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| `PreviewIntegrationTests` | PASS: 2074 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| `LogUiModelTests` | PASS: 390 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| `LogUiWiringTests` | PASS: 90 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 3879 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 999 assertions across 150 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 1576 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

전체 후보·격리 재빌드의 14개 결과 문자열이 일치한다. 고정 커밋 재빌드와 공개 파일 확인은 아직 실행 전이며 완료 후 해당 실행 결과와 다시 대조한다.

</details>

## 파일 크기와 구조 개선의 효과

| 항목 | alpha.8 대비 차이 |
| --- | ---: |
| DLL 전체 | +512바이트 · 619,520 → 620,032바이트 |
| 메서드 IL | +50바이트 |
| 메서드 헤더·예외 처리 정보 | +22바이트 |
| 메타데이터 | +800바이트 |
| 내장 리소스 | 0바이트 · 8개 크기·해시 동일 |
| 기타 파일 구조·정렬 등 | -360바이트 |

외부 assembly 참조도 동일하며 최종 DLL에 테스트·Mono.Cecil 타입/참조가 없다. 첫 단계 어댑터 추출만으로는 IL이 56바이트 감소했지만 메타데이터·파일 구조·정렬 차이를 합친 DLL 크기는 alpha.8과 같았다. 두 단계 적용 후에는 정책 표현을 위한 타입·메서드의 메타데이터 등이 증가했다. 구조 개선의 목적은 수정 지점과 취소 경계의 명확화이며 용량 절감 효과는 없다. 메모리·할당량·프레임 시간·입력 지연 개선도 측정하거나 입증하지 않았다. [단계별 범위·측정·근거](DEVELOPMENT.md#입력-경계-구조-정리).

## 파일 확인·게시 기록

| 파일 | 이번 검토 후보 크기·SHA-256 |
| --- | --- |
| `DiceMaster-v1.0.6-alpha.9.zip` 예정 | 261,940바이트 · `eed589dd5059774f1b9f7697bfc997953f3cedfdd3bdb0c2ad60d58dfbb865c8` |
| `DiceMaster.dll` | 620,032바이트 · `7da12ec4b77d524e82f2bf8c62d4de6cf8e0fc74b83fb3cc6393619f8efc1d64` |
| `config/DiceMaster.cfg` | 11,459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |

후보·격리 빌드와 로컬 ZIP의 CRC·두 파일·내부 바이트를 확인했다. 공개 이름으로 바꾸는 경우 재압축하지 않고 바이트·해시를 대조한다. 영수증은 로컬 `artifacts/architecture-refactor/final-candidate/build-manifest.json`·`refactor-verification.json`·`final-clean/clean-build.json`·`policy-evidence/`에 보관한다.

구현 커밋은 `da1f3620d0df4c2ff6125887041a39b90646ef09`·`360ca5733adcca31b9acbf62adaf966856c9d6ba`이며 작업 전 원격 main은 `fd9f8180fcd46d8741a863cb2e18fa43be237d69`다. 이 준비 문서를 포함하는 게시용 고정 소스·태그와 실제 Release/asset ID·게시 시각은 아직 정하지 않았다. 사전 확인에서 새 버전은 미사용이고 기존 32개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 확인했다. 게시 후 이 기준과 다시 대조하고 실제 결과는 빌드 소스·태그·자산을 바꾸지 않는 별도 main 문서 커밋에 보충한다.
