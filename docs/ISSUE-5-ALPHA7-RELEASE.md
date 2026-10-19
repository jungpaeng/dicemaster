# DiceMaster v1.0.6-alpha.7 — 두 줄 팝업 목록·무작위 조작·굵은 제목

팝업 목록에서 정상 발동 가능/발동 불가 상태 행과 별도 무작위 항목을 제거하고 굵은 이벤트 제목·효과색을 보존한 결과 요약의 두 줄로 정리한다. 무작위는 하단 `무작위로 진행 [B / Esc]` 버튼으로 안내하며 클릭·터치도 지원한다.

상태: **전체 소스·격리 재빌드와 14개 검사 통과 · 고정 커밋 재빌드·게시 검증 준비 · 새 DLL의 Windows·장치 확인 대기**. 로컬 검토 후보의 파일 식별값은 아래와 같으며 새 태그·Release는 아직 게시하지 않았다. 고정 소스 재빌드와 공개 자산 검증 후 설치 링크·소스·게시 결과를 이 문서에 보충한다. [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5이며 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)를 별도로 사용한다.

## alpha.6 이후 변경

### 상태 행 없는 두 줄 목록

후보 행은 **제목 / 결과 요약**의 두 줄이며 정상 발동 가능/발동 불가 텍스트 행을 제거한다. 정상 발동 불가 후보의 제목은 기존 `#B3B3B3` 회색으로 구분하고 선택 화살표는 노랑, 결과 요약·상세의 기존 로그 효과색은 그대로 유지한다.

선택 상세의 발동 조건과 진행 버튼의 `이 이벤트로 진행` / `조건 무시하고 이번만 진행` 문구는 유지한다. 회색 행도 선택할 수 있으며 읽기 가능한 미충족·가중치 0·판정 미확인 후보의 명시 조건 무시는 유지한다. 대상·결과를 읽을 수 없는 후보는 실행할 수 없다. 색·굵기·표시 문구가 실행 권한의 근거가 되지는 않는다.

### 무작위 목록 항목 제거

목록에는 실제 후보만 표시하고 가상 `무작위로 진행` 항목은 제거한다. 새 행동의 초기 후보는 **미선택**이며 선택 전 A/Enter/Shift+Enter는 진행하지 않는다. ↑ 또는 ↓·왼쪽 스틱의 첫 탐색은 첫 후보부터 시작하고 이후 후보 이동·선택 추적을 유지한다.

**B/Escape는 선택을 폐기하고 원래 조건·가중치·난수 판정으로 한 번 진행한다.** 하단의 클릭·터치 가능한 `무작위로 진행 [B / Esc]` 버튼 자체로 이를 안내하며 별도 하단 안내에 같은 문구를 반복하지 않는다. 버튼은 키보드·패드가 없는 터치 환경에서도 무작위로 진행하는 경로다.

후보 첫 클릭·터치는 선택이고 선택된 같은 행을 새로 누르고 같은 행에서 떼면 표시된 일반/명시 조건 무시 진행을 요청한다. 진행 버튼·A 확인과 기존 토큰·대상·결과·진행 방식 재검증, 로그 복귀 시 B 재사용 차단, 입력 해제·중립 보호를 유지한다.

### 제목 첫 줄 굵게 표시

`Section_...:이벤트_이름` 첫 줄 전체를 굵게 표시한다. 제목의 실제 굵은 글자 폭을 기준으로 말줄임하고 회색 제목·노랑 선택 화살표와 조합해도 선택 전후 같은 행 높이를 사용한다. 원문의 태그처럼 보이는 문자열은 기존처럼 안전한 문자로 처리하고 렌더러가 제목 굵기만 적용한다. 결과 요약·상세의 원문 굵기·크기 태그를 새로 허용하는 변경은 아니다.

결과 분석·성공 캐시·0.25초 최대 두 후보 준비·실패 5초 재시도와 숨김 분석 0, 목록과 별도의 상세 스크롤, 도구 화면 유지·물리 패드 재획득과 하드 경계는 유지한다. 이벤트 결과나 Lua를 표시 때문에 실행하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 게시 후 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.7.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

설정·기본값 변경은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용하며 로그 도구 ON/OFF 저장은 유지한다. alpha.6으로 복구하면 제목·발동 상태·결과 요약의 세 줄과 가상 무작위 목록 항목·초기 무작위 선택으로 돌아간다. v1.0.5는 이벤트 사전 예약 방식이다.

ZIP은 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브를 배포하지 않는다.

## 검증·알려진 제한

2026-10-20 02:39:59 KST에 전체 소스 후보와 기존 모드 DLL·Git·이전 빌드 출력을 제외한 격리 소스 재빌드가 각각 **14개 검사 프로그램**을 통과했다. SDK는 8.0.426이며 모드 컴파일은 오류 0개·기존 경고 7개다. 빌드 입력 174개·모드 소스/리소스 71개를 확인했고 격리 빌드는 기존 `DiceMaster.dll` 0개에서 실행했다. 두 빌드의 DLL·cfg·ZIP 바이트와 전체 검사 결과가 일치하며 현재 174개 빌드 입력의 SHA-256도 후보 manifest와 일치한다. 로컬 ZIP의 CRC·정확히 두 파일·내부 DLL/cfg 바이트·해시를 대조했다. 고정 커밋 재빌드·게시·공개 다운로드 검증은 아직 수행 전이다. 아래 값은 이번 실행에서 읽었으며 이전 alpha.6 결과를 재사용하지 않는다.

| 확인 범위 | 이번 결과 |
| --- | --- |
| 전체 소스·격리 빌드·입력 목록·DLL/cfg/ZIP 동일 바이트 | 각각 14개 프로그램 통과 · SDK 8.0.426 · 기존 모드 DLL 0개 · 입력 174개/소스·리소스 71개 · 동일 파일·검사 결과 |
| 상태 행 제거·두 줄 고정 높이·해상도/글자 크기별 접근 | 팝업 1,477개 관리 입력/배치 검사 통과 |
| 실제 후보만 표시·초기 미선택·선택 전 A/Enter 무동작·↑/↓ 첫 후보 선택 | 팝업 후보 목록·키보드/패드 회귀 통과 |
| B/Escape·하단 클릭/터치 무작위 버튼·화면 전환 뒤 입력 재사용 방지 | 팝업 마우스·변환 터치·별도 터치·키보드·패드 회귀 통과 |
| 제목 전체 굵게·굵은 폭 말줄임·회색 제목/노랑 화살표/효과색 | 팝업 굵은 글자 측정·서식/색·문자 경계 회귀 통과 |
| 첫 선택/동일 행 재클릭·터치 진행·오래된 토큰/대상/결과/방식 거부 | 팝업·공통 UI 3,879개·패드 262,831개 검사 통과 · 후보 DLL 연결 86개 |
| 결과 정적 분석·캐시/재시도·독립 상세·권한 분리·기존 로그/패드/미션/주사위 | 실제 후보 DLL 미리보기 2,074개·실행 런타임 20개 시나리오/103개 단언과 기존 회귀 통과 |
| 고정 커밋 `--scope prd5` 재빌드·검토 파일 일치 | 커밋 후 실행 |
| 태그·prerelease·익명 페이지/ZIP CRC·정확히 두 파일·내부 바이트/해시 | 게시 후 실행 |
| 기존 Release·태그·자산·Latest v1.0.5·이슈 상태 보존 | 게시 전후 대조 |
| 실제 Windows 게임·트랙패드·터치·Xbox/XInput | 미실행 · 사용자 확인 대기 |

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API 경계를 대상으로 한다. 실제 Windows 글꼴·화면 배치·물리 장치·성능은 별도 확인이다. alpha.6의 [후속 UI 요청](ISSUE-5-ALPHA6-RELEASE.md#2026-10-20-사용자-후속-요청)을 새 DLL의 전체 실게임 확인으로 승계하지 않는다.

결과 설명은 정적 분석 범위이며 모든 미래 분기·난수 결과를 보장하지 않는다. 실행 팝업은 실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원한다. 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음을 조회하고 팝업은 현재 판정의 후보여서 목록 수가 항상 같지는 않다.

큰 로그 이벤트는 조회 전용이고 미션 예약·해제와 주사위는 유지한다. 게임 초점·로그 닫힘 상태에서 패드 열기가 켜져 있으면 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 `EnableGamepadLogOpen=false`로 바꾸고 남은 입력을 해제한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

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
| `LogUiWiringTests` | PASS: 86 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 3879 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 103 assertions across 20 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 1477 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

전체 후보와 격리 재빌드의 14개 검사 결과 문자열이 일치한다. 관리 대역과 실제 API 경계의 검사이며 실제 Windows 게임 실행으로 확대하지 않는다. 고정 커밋 재빌드·공개 다운로드 검증은 별도 단계로 남아 있다.

</details>

## 파일 확인·게시 기록

| 항목 | 이번 로컬 검토 후보 식별값 |
| --- | --- |
| `DiceMaster-source-candidate.zip` 크기·SHA-256 | 261,468바이트 · `2d3d1b50e23f849aa98568d919a53f86e90af259fab5b36de56d184a1d5dafb1` |
| `DiceMaster.dll` 크기·SHA-256 | 619,520바이트 · `4e7274cce47079e6ca9b80f66f54b379e152d3e5651281e2c71fadee0300eeb3` |
| `config/DiceMaster.cfg` 크기·SHA-256 | 11,459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |
| 고정 소스·태그·게시 시각·Release/asset ID | 아직 미확정 · 고정 재빌드/게시 후 기록 |

위 값은 이번 로컬 검토 후보·격리 재빌드와 로컬 ZIP에서 일치한다. 검토 기록은 `artifacts/issue-5/alpha7-final-candidate/build-manifest.json`과 `artifacts/issue-5/alpha7-final-clean/clean-build.json`에 보존한다. 이후 같은 파일을 공개할 때 공개 이름 `DiceMaster-v1.0.6-alpha.7.zip`으로 바꾸면서 재압축하지 않고 바이트·해시를 대조한다.

작업 시작 main은 `661622d8f221f25d58f871321caed935c952d6e9`다. 새 소스·태그 커밋과 고정 재빌드·공개 파일 비교, 게시 시각·Release/asset ID는 해당 단계가 완료된 후 기록한다. `draft=false`·`prerelease=true`·`make_latest=false`로 게시하고 Latest v1.0.5와 기존 Release·태그·자산, 이슈 #2 closed·#4 open·#5 open을 보존한다. 아직 새 Release를 게시하거나 이슈 #5를 완료 처리하지 않았다.

실제 영수증은 생성 후 로컬 `artifacts/releases/v1.0.6-alpha.7/`에 보존한다. 공개 Release에서는 이 문서·구현 문서·Windows 확인표의 고정 커밋 URL을 사용한다. 게시 후 문서 보충 커밋은 빌드 소스·태그·공개 자산과 구분한다.
