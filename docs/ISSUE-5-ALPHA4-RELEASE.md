# DiceMaster v1.0.6-alpha.4 — 로그 탭 터치·팝업 두 번째 터치 보완

alpha.3에서 보고된 이벤트 외 로그 탭의 터치 실패 경로와, 팝업에서 선택한 뒤 두 번 더 터치해야 실행되는 문제를 수정한다. 팝업은 첫 터치로 선택하고 바로 다음 터치로 표시된 진행을 요청하는 조작을 유지한다.

**2026-10-20 01:47:20 KST에 [v1.0.6-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.4) prerelease를 게시했다.** **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.4/DiceMaster-v1.0.6-alpha.4.zip)**. 소스·태그 `bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce`의 고정 재빌드·14개 검사와 인증 없는 공개 페이지·다운로드의 CRC·두 파일·바이트·SHA-256 대조를 통과했다. [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5로 유지하며 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다.

## alpha.3 이후 변경

### 전체 로그의 탭별 터치 차이

사용자는 alpha.3의 이벤트 탭에서는 목록 선택과 다른 탭 이동이 되지만 다른 탭에서는 터치가 동작하지 않는다고 보고했다. 소스에는 이벤트 외 탭에서 매 입력·화면 콜백마다 `GUI.FocusControl(null)`을 호출하는 차이가 있었다. 이번 수정은 반복 호출을 없애고 탭·도구·키보드 전환처럼 초점 정리가 필요한 경계에서만 요청한다. 포인터 누름·해제 중에는 적용하지 않고 안전한 화면 갱신 단계에서 한 번 처리한다.

이벤트 검색 칸은 클릭·터치로 초점을 얻고 키보드로 입력하며, 검색을 벗어날 때 초점을 해제하는 동작을 유지한다. alpha.2의 버튼 영역 선확보와 alpha.3의 실제 입력 단계·소비된 이벤트 정리·별도 터치 전달도 유지한다. 사용자 보고와 포커스 호출 차이는 일치하지만 실제 Windows의 이벤트 전달을 관찰하지 않았으므로 이 경로가 장치 실패의 직접 원인임을 확정하지 않는다.

누름 해제가 끝난 뒤 늦게 온 포인터 전환의 소프트 초기화도 사용자가 연 도구와 이벤트 검색 칸의 입력 커서를 보존하며, 실제 초점·문맥 경계의 초기화는 계속 적용한다.

### 팝업의 두 번째 터치 복구

실제 `LogPadController` 소스를 연결해 다음 순서를 재현했다.

1. 첫 터치의 누름·해제가 행을 선택하고 포인터 기록을 정리한다.
2. 그 뒤 Update에서 늦게 처리된 포인터 전환이 `ResetInput(true)`을 요청한다.
3. 활성 누름이 없다는 이유로 기존 코드가 입력 해제 대기를 다시 켠다. 끝난 터치가 `touchCount=1`로 남고, 다음 터치 전 입력 0의 Update가 없으면 두 번째 터치를 버린다.
4. 추가 터치에서만 선택된 항목이 실행된다.

소프트 초기화는 활성 누름이 없을 때 기존 중립 상태를 보존하도록 수정한다. 유효한 누름이 있으면 해당 누름을 유지하고, 잘못된 누름이면 취소한다. 새 진입·초점 상실·문맥·판정 토큰·대상 객체·결과 교체와 연결·소유자 같은 안전 경계에서는 계속 취소하고 해제·중립을 기다린다.

| 입력 | 동작 |
| --- | --- |
| 다른 행 첫 클릭·터치 | 선택만 하고 조건 설명·진행 방식을 표시한다. |
| 선택된 행의 두 번째 클릭·터치 | 같은 행에서 해제하면 표시된 일반 진행·명시 조건 무시·무작위를 한 번 요청한다. |
| 진행 버튼 또는 A / Enter / Shift+Enter | 표시된 진행 방식으로 확인한다. |
| B / Escape 또는 무작위 버튼 | 원래 게임의 조건·가중치·난수 판정으로 진행한다. |

누름 후 토큰·대상·결과·일반/조건 무시 방식이 바뀌면 실행하지 않는다. 일반 진행을 조건 무시로 자동 전환하지 않으며 대상·결과를 읽을 수 없는 행은 차단한다. 새 단축키·패드 조합·설정 키는 추가하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.4.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

설정·기본값 변경은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용한다. 큰 로그 도구의 팝업 ON/OFF 저장과 OFF 시 미확정 대기를 원래 판정으로 돌려보내는 동작을 유지한다. alpha.3으로 복구하면 이번 탭별 터치·추가 터치 문제가 다시 나타날 수 있다. v1.0.5로 복구하면 이벤트 사전 예약 방식으로 돌아간다.

ZIP에는 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브를 배포하지 않는다.

## 검증·알려진 제한

2026-10-20 01:42:39 KST에 전체 소스 후보와 기존 모드 DLL·Git·이전 빌드 출력을 제외한 격리 소스 재빌드가 각각 14개 검사 프로그램을 통과했다. SDK는 8.0.426이며 모드 컴파일은 오류 0개·기존 경고 7개다. 매니페스트의 빌드 입력 167개·모드 소스/리소스 69개를 확인했고, 격리 빌드는 기존 `DiceMaster.dll` 0개에서 실행했다. 두 빌드의 DLL·cfg·ZIP 바이트와 전체 검사 결과가 일치한다. 01:46:41 KST에 소스 `bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce`의 기존 모드 DLL 0개에서 고정 재빌드도 14개 프로그램을 통과했고, 검토 후보·격리 빌드와 파일 바이트·전체 검사 결과가 일치했다. 게시 후 익명 공개 다운로드의 CRC·두 파일·내부 DLL/cfg·SHA-256을 대조했다.

| 확인 | 이번 결과 |
| --- | --- |
| 전체 소스 후보·자동 검사 | 14개 프로그램 통과 · SDK 8.0.426 · 모드 오류 0개/기존 경고 7개 |
| 격리 소스 재빌드·입력 목록·DLL/cfg/ZIP 동일 바이트 | 14개 통과 · 기존 모드 DLL 0개 · 빌드 입력 167개/소스·리소스 69개 · 검토 DLL/cfg/ZIP 동일 바이트 |
| 전체 로그 네 탭·도구·검색의 초점 해제 시점과 포인터 연속 조작 | 공통 UI 3,081개 통과 · 마우스/변환 터치/별도 터치 · 도구·검색 커서의 늦은 포인터 전환 보존 포함 |
| 첫 선택 해제 → 늦은 실제 컨트롤러 포인터 전환 → 두 번째 터치 | 팝업 1,103개 통과 · 실제 `LogPadController` 소스를 연결해 터치 종료 잔류·입력 0 Update가 없는 순서 포함 |
| 유효/무효 누름·초점·문맥·토큰·대상·결과·일반/조건 무시 변경 보호 | 공통 UI·팝업 회귀 검사 통과 · 실제 후보 DLL 연결 83개 통과 |
| 고정 커밋 `--scope prd5` 재빌드·검토 DLL/cfg/ZIP 동일 바이트 | `bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce` · 기존 모드 DLL 0개에서 14개 프로그램 통과 · 검토/격리 빌드와 파일·검사 결과 일치 |
| 공개 태그·prerelease·Latest 보존·익명 다운로드 CRC/두 파일/바이트·해시 | 검증 통과 · 기존 27개 Release·태그·자산과 Latest v1.0.5 보존 · main에 검증 소스 포함 |
| 실제 Windows 게임·트랙패드·마우스로 인식되는 터치·별도 터치·Xbox/XInput | 미실행 · 사용자 확인 대기 |

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API 경계를 대상으로 한다. 포커스 해제와 늦은 입력 초기화가 포인터 기록에 주는 영향을 검사하지만 실제 Windows native 창 전체·물리 장치·글꼴·화면 배치·성능을 실행한 근거는 아니다. alpha.3의 제한된 사용자 보고는 [당시 후속 기록](ISSUE-5-ALPHA3-RELEASE.md#2026-10-20-사용자-후속-확인)에 보존하고 새 DLL의 성공으로 승계하지 않는다.

실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원한다. 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음의 조회이고 팝업은 지금 실행된 한 판정의 후보이므로 목록 수가 항상 같지는 않다. 큰 로그 이벤트는 조회 전용이며 미션 예약·해제와 주사위는 유지한다.

`EnableGamepadLogOpen=true`의 게임 초점·로그 닫힘 구간에서는 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 옵션을 끄고 남은 입력을 해제한다. Tab 진입의 첫 패드 입력과 Steam Input 구성별 동작도 실제 환경에서 확인한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

<details>
<summary>이번 전체 소스·격리·고정 커밋 재빌드의 자동 검사 결과</summary>

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
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 3081 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 86 assertions across 17 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 1103 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

전체 후보·격리·고정 커밋 재빌드의 전체 14개 검사 결과 문자열이 일치한다. 검사와 명시적 대역의 범위는 각 결과에 기록한 대로이며 실제 Windows 게임 실행 결과로 해석하지 않는다.

</details>

## 파일 확인·게시 기록

| 항목 | 이번 식별값 |
| --- | --- |
| 고정 소스 커밋·태그 | [`bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce`](https://github.com/jungpaeng/dicemaster/commit/bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce) · `v1.0.6-alpha.4` |
| `DiceMaster-v1.0.6-alpha.4.zip` 크기·SHA-256 | 259,207바이트 · `b334228fe455514e824d925ecaad67d51c8e3ebf0f9cfb5f8abd128d8e5c4e1b` |
| `DiceMaster.dll` 크기·SHA-256 | 614,400바이트 · `d46a6a5d0f75bd1c4540e1564f90b5beb4bd6c6b1f95d51dfbf3d128f8261431` |
| `config/DiceMaster.cfg` 크기·SHA-256 | 11,459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |
| 게시 시각·Release/asset ID | 2026-10-20 01:47:20 KST (2026-10-19 16:47:20 UTC) · Release `417518975` · asset `656831696` |

작업 시작 main은 `ea11afbda83433c58eeddc90cada3d021013bdda`다. 사용자 지시대로 최신 `origin/main`을 기준으로 작업했고 README·구현·회귀 검사를 포함한 소스 `bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce`를 main에 반영했다. 새 태그는 검증 소스를 가리키며 `draft=false`·`prerelease=true`·`make_latest=false`로 게시했다. 기존 27개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다.

위 파일 식별값은 로컬 검토 후보·격리 재빌드·고정 소스 재빌드와 공개 다운로드에서 모두 일치한다. 로컬 후보 이름 `DiceMaster-source-candidate.zip`을 공개 설치 ZIP 이름 `DiceMaster-v1.0.6-alpha.4.zip`으로 바꾸면서 재압축하지 않았고 바이트·해시를 대조했다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.4/`의 `release-verification.json`, `build-manifest.json`, `anonymous-verification.json`, `github-publication.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 이후 실제 게시·익명 다운로드 검증과 구분한다.

이 문서의 게시 결과 보충은 main의 별도 후속 문서 커밋에 포함한다. 빌드 소스·태그 `bea9e87`와 구분하며 공개 태그·DLL·cfg·ZIP을 바꾸지 않는다. 고정 소스에 포함된 준비 문구는 당시 검사·게시 단계의 기록이다.

## 2026-10-20 사용자 후속 확인

사용자는 alpha.4에서 **대부분의 로그 터치 문제가 해결됐다**고 보고했다. 남은 항목으로 **도구를 열면 이전 목록 탭도 함께 선택 표시됨**, **터치로 도구를 연 뒤 스틱을 쓰면 이전 탭으로 돌아감**, **팝업에 결과 설명이 빠져 있음**을 보고했다. 이 기록은 해당 관찰의 제한된 확인이며 개별 시나리오 전체·설치 DLL 해시·장치별 환경이 추가 제공된 것은 아니다.

이후 도구 선택 표시·입력 장치 전환 시 화면 유지·팝업 결과 설명의 수정과 새 후보 검증·배포는 [alpha.5 배포 기록](ISSUE-5-ALPHA5-RELEASE.md)에서 구분한다. 이 문서의 게시 파일·검사 수치·해시와 당시의 확인 대기 기록은 보존하며 사용자 보고를 새 DLL의 Windows 검증 완료로 승계하지 않는다.
