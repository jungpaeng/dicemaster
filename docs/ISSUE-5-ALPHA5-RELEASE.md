# DiceMaster v1.0.6-alpha.5 — 도구 화면 유지·팝업 결과 설명

alpha.4에서 보고된 도구의 중복 선택 표시와 입력 장치를 바꿀 때 이전 탭으로 돌아가는 문제를 수정한다. 팝업에는 후보별 결과 요약과 선택 결과 상세를 추가해 발동 조건과 나누어 읽도록 한다.

**2026-10-20 02:09:55 KST에 [v1.0.6-alpha.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.5) prerelease를 게시했다.** **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.5/DiceMaster-v1.0.6-alpha.5.zip)**. 소스·태그 `3f138728c109651533f25b5e7aee3f147a082d14`의 고정 재빌드·14개 검사와 익명 공개 페이지·다운로드의 CRC·두 파일·내부 바이트·SHA-256 대조를 통과했다. [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5로 유지하며 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다.

## alpha.4 이후 변경

### 도구 선택 표시와 입력 장치 전환

로그 도구가 열리면 도구만 선택 표시한다. 도구에서 돌아갈 이전 목록 탭의 상태는 유지하지만 그 탭을 현재 화면처럼 동시에 강조하지 않는다.

터치로 도구를 연 뒤 물리 패드를 다시 사용해도 현재 도구 화면·선택·스크롤을 유지한다. 이 재획득은 `Navigation` 초기화로 이전 확인·클릭 권한과 검색 입력 커서를 취소하고 버튼 해제·스틱/트리거 중립 뒤 새 입력을 받는다. 자동 scheme 변경이나 실제 포인터 전환은 기존 `Pointer` 경로를 사용해 같은 유효 누름을 보존한다. 모든 모드 변화가 클릭을 취소하는 것은 아니며 첫 터치 선택 계약을 유지한다. 실제 초점 상실·문맥·차단 준비·소유자·연결·닫힘 경계는 계속 하드 초기화한다. 새 설정이나 영구 UI 상태는 추가하지 않는다.

### 팝업에서 결과와 발동 조건을 구분

팝업 후보 목록은 **제목 / 발동 자격 / 결과 요약 / 조작 안내**의 네 줄을 유지한다. 각 줄은 현재 폭에 맞춰 말줄임하며 선택 표시와 조작 안내가 바뀌어도 행 높이는 유지한다. 선택 뒤 같은 위치를 다시 터치할 때 행 높이 변화 때문에 다른 영역을 누르지 않도록 한다.

선택 상세는 **예상 결과 → 결과 설명 → 발동 조건 → 조건 설명** 순서다. 목록과 별도의 스크롤 영역에서 휠·오른쪽 스틱으로 결과·조건을 읽고 ↑↓는 후보 행 이동을 유지한다. 선택 대상 객체나 실제 결과가 바뀌면 상세 읽기 위치를 초기화한다.

결과는 후보의 실제 `Result` 식별자로 기존 정적 분석기를 조회한다. 후보 이름이나 표시 제목으로 다른 결과를 추정하지 않으며, 표시를 위해 Lua·이벤트 결과·게임 판정 체인을 실행하지 않는다. 분석할 수 없는 결과·읽기 실패·분석 누락은 미확인으로 표시하고 분석상 효과 없음과 구분한다. 결과 설명은 분석이 지원하는 효과·가능한 후속 변화의 범위이며 모든 미래 분기·실제 게임의 다음 난수 결과를 보장하지 않는다.

정적 설명은 **준비 중 / 미지원·미확인 / 읽기 실패 / 분석상 확인된 효과 없음**을 구분한다. 이는 이벤트 자체의 대상·실제 결과 식별자를 읽지 못한 상태와 다르며 설명 분석 실패가 정상 선택·명시 조건 무시의 가능 여부를 바꾸지는 않는다. 기존 `CanSelect`·`CanForce`는 실제 대상·결과·발동 자격을 계속 검증한다.

팝업이 보일 때 0.25초 갱신마다 최대 두 후보의 정적 결과 설명을 준비한다. 성공 결과는 같은 실제 행동·대상 객체·결과 식별자 동안 캐시하고 읽기 실패·자료 준비 대기는 5초 뒤 재시도한다. OnGUI는 이미 게시한 텍스트만 읽으며 숨은 팝업의 표시용 준비는 실행하지 않는다. 결과가 바뀌면 이전 설명을 새 결과로 재사용하지 않는다.

화면 구성은 조사에서 읽은 조활시스템 main의 고정 커밋 `f0650b26975d9ba97c1a0fb614a0f7ed60a9d631` 중 [ToolWindow.cs 399–495행](https://github.com/Toufff/zhaohuo-system/blob/f0650b26975d9ba97c1a0fb614a0f7ed60a9d631/src/ZhaoHuoSystem/ToolWindow.cs#L399-L495)과 [OptionHints.cs 405–420행](https://github.com/Toufff/zhaohuo-system/blob/f0650b26975d9ba97c1a0fb614a0f7ed60a9d631/src/ZhaoHuoSystem/OptionHints.cs#L405-L420)의 목록 효과 요약 → 선택 결과 상세 → 발동 조건을 구분하는 UI 원칙을 참고했다. 구현은 DiceMaster의 기존 정적 분석기를 재사용하며 상류 소스를 직접 복사하지 않는다.

첫 클릭·터치는 행 선택, 선택된 행의 새 클릭·터치는 표시된 진행 요청이다. 진행 버튼과 A/Enter/Shift+Enter, B/Escape 무작위를 유지한다. 정상 후보는 일반 진행, 미충족·가중치 0·판정 미확인인 읽기 가능한 후보는 명시 조건 무시다. 누름 뒤 판정 토큰·대상·결과·진행 방식이 달라지면 실행하지 않으며 대상·결과를 읽을 수 없는 후보는 계속 차단한다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.5.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

설정·기본값 변경은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용한다. 큰 로그 도구의 팝업 ON/OFF 저장과 OFF 시 미확정 대기를 원래 판정으로 돌려보내는 동작을 유지한다. alpha.4로 복구하면 이번 도구 표시·장치 전환·결과 설명 문제가 다시 나타날 수 있다. v1.0.5로 복구하면 이벤트 사전 예약 방식으로 돌아간다.

ZIP에는 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브를 배포하지 않는다.

## 검증·알려진 제한

2026-10-20 02:05:32 KST에 전체 소스 후보와 기존 모드 DLL·Git·이전 빌드 출력을 제외한 격리 소스 재빌드가 각각 14개 검사 프로그램을 통과했다. SDK는 8.0.426이며 모드 컴파일은 오류 0개·기존 경고 7개다. 빌드 입력 171개·모드 소스/리소스 70개를 확인했고 격리 빌드는 기존 `DiceMaster.dll` 0개에서 실행했다. 두 빌드의 DLL·cfg·ZIP 바이트와 전체 검사 결과가 일치한다. 같은 소스 커밋 `3f138728c109651533f25b5e7aee3f147a082d14`의 고정 재빌드도 기존 모드 DLL 0개에서 14개 프로그램을 통과했고 검토 후보·격리 빌드와 파일 바이트·전체 검사 결과가 일치했다. 게시 후 익명 공개 다운로드의 CRC·정확히 두 파일·내부 DLL/cfg·SHA-256을 대조했다.

| 확인 | 이번 결과 |
| --- | --- |
| 전체 소스 후보·자동 검사·SDK/모드 경고 | 14개 프로그램 통과 · SDK 8.0.426 · 모드 오류 0개/기존 경고 7개 |
| 격리 소스 재빌드·입력 목록·DLL/cfg/ZIP 동일 바이트 | 14개 통과 · 기존 모드 DLL 0개 · 빌드 입력 171개/소스·리소스 70개 · 검토 DLL/cfg/ZIP 동일 바이트 |
| 도구만 선택 표시·뒤 목록 탭의 복귀 상태 유지 | 공통 UI 3,879개 통과 · 실제 renderer/model/controller 소스와 명시적 경계 |
| 물리 패드 재획득의 도구 화면·선택·스크롤 보존과 확인·클릭 권한/검색 커서 취소 | 공통 UI·패드 262,831개 검사에 포함 · 관리 경계 통과 |
| 자동 scheme 변경·실제 포인터 전환의 유효한 첫 누름 보존 | 공통 UI·팝업 회귀 통과 · 포인터/Navigation/하드 초기화 구분 |
| 초점·문맥·대상·연결·소유자 등 안전 경계의 초기화 | 공통 UI·팝업·실제 컨트롤러 연결 회귀 통과 · 후보 DLL 연결 85개 |
| 실제 결과 식별자·네 줄/폭별 말줄임·선택 전후 행 높이·결과/발동 조건 분리 | 팝업 1,287개 관리 UI 검사 통과 |
| 결과 상세 독립 휠·오른쪽 스틱·긴 설명 끝 접근과 ↑↓ 행 이동 | 팝업 입력/배치 회귀 통과 · 실제 장치 스크롤은 확인 대기 |
| 보이는 팝업 0.25초/최대 두 후보 준비·성공 캐시·실패 5초 재시도·숨김 준비 없음 | 실행 런타임 20개 시나리오·103개 단언 통과 |
| Lua/게임 결과 미실행·준비 중/읽기 실패/미지원/효과 없음 구분·선택 가능 여부와 독립·기존 확정 보호 | 실제 후보 DLL 미리보기 1,479개·실행 런타임·팝업 회귀 통과 |
| 고정 커밋 `--scope prd5` 재빌드·검토 DLL/cfg/ZIP 동일 바이트 | `3f138728c109651533f25b5e7aee3f147a082d14` · 기존 모드 DLL 0개에서 14개 프로그램 통과 · 검토/격리 빌드와 파일·검사 결과 일치 |
| 공개 태그·prerelease·Latest 보존·익명 다운로드 CRC/두 파일/바이트·해시 | 검증 통과 · 기존 28개 Release·태그·자산과 Latest v1.0.5 보존 · main에 검증 소스 포함 |
| 실제 Windows 게임·트랙패드·터치·Xbox/XInput | 미실행 · 사용자 확인 대기 |

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API 경계를 대상으로 한다. 실제 Windows 창·물리 장치·글꼴·화면 배치·성능을 실행한 근거와 구분한다. alpha.4의 제한된 사용자 보고는 [당시 후속 기록](ISSUE-5-ALPHA4-RELEASE.md#2026-10-20-사용자-후속-확인)에 보존하고 새 DLL의 Windows 통과로 승계하지 않는다.

실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원한다. 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음의 조회이고 팝업은 지금 실행된 한 판정의 후보이므로 목록 수가 항상 같지는 않다. 큰 로그 이벤트는 조회 전용이며 미션 예약·해제와 주사위는 유지한다.

`EnableGamepadLogOpen=true`의 게임 초점·로그 닫힘 구간에서는 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 옵션을 끄고 남은 입력을 해제한다. Steam Input 구성별 동작도 실제 환경에서 확인한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

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
| `PreviewIntegrationTests` | PASS: 1479 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| `LogUiModelTests` | PASS: 390 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| `LogUiWiringTests` | PASS: 85 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 3879 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 103 assertions across 20 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 1287 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

전체 후보·격리·고정 커밋 재빌드의 14개 검사 결과 문자열이 일치한다. 각 결과에 적힌 명시적 대역과 실제 API의 경계를 실제 Windows 게임 실행으로 확대해 해석하지 않는다.

</details>

## 파일 확인·게시 기록

| 항목 | 이번 식별값 |
| --- | --- |
| 고정 소스 커밋·태그 | [`3f138728c109651533f25b5e7aee3f147a082d14`](https://github.com/jungpaeng/dicemaster/commit/3f138728c109651533f25b5e7aee3f147a082d14) · `v1.0.6-alpha.5` |
| `DiceMaster-v1.0.6-alpha.5.zip` 크기·SHA-256 | 260,791바이트 · `abca88f8e8f929844e02c8137825a40f0b6bad8515f4dfbeadeafa4496e1aece` |
| `DiceMaster.dll` 크기·SHA-256 | 617,984바이트 · `22d31266f4c85f64075e71422edc18795b19b2ce18dbf67d1a7333e4f62986bd` |
| `config/DiceMaster.cfg` 크기·SHA-256 | 11,459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |
| 게시 시각·Release/asset ID | 2026-10-20 02:09:55 KST (2026-10-19 17:09:55 UTC) · Release `417540991` · asset `656889735` |

작업 시작 main은 `3a5ffdf85f5965623c6c585032086976515dd98a`다. 사용자 지시대로 최신 `origin/main`에서 작업했고 README·구현·회귀 검사를 포함한 소스 `3f138728c109651533f25b5e7aee3f147a082d14`를 main에 반영했다. 새 태그는 검증 소스를 가리키며 `draft=false`·`prerelease=true`·`make_latest=false`로 게시했다. 기존 28개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다.

위 식별값은 로컬 검토 후보·격리 재빌드·고정 소스 재빌드와 공개 다운로드에서 모두 일치한다. 로컬 이름 `DiceMaster-source-candidate.zip`을 공개 이름 `DiceMaster-v1.0.6-alpha.5.zip`으로 바꾸면서 재압축하지 않았고 바이트·해시를 대조했다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.5/`의 `release-verification.json`, `build-manifest.json`, `anonymous-verification.json`, `github-publication.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 이후 실제 게시·익명 다운로드 검증과 구분한다.

이 문서의 게시 결과 보충은 main의 별도 후속 문서 커밋에 포함한다. 빌드 소스·태그 `3f13872`와 구분하며 공개 태그·DLL·cfg·ZIP을 바꾸지 않는다. 고정 소스에 포함된 준비 문구는 당시 검사·게시 단계의 기록이다.

## 2026-10-20 사용자 후속 요청

사용자는 같은 이벤트 팝업에서 **반복 클릭·터치 선택/진행 안내 제거**, **결과 요약·상세를 기존 로그와 같은 색으로 표시**, **현재 조건을 충족하지 못한 후보를 회색으로 구분**하는 세 UI 후속을 요청했다. 이는 해당 표시 요구이며 새 DLL이나 모든 Windows 항목의 검증 완료를 의미하지 않는다.

후속 구현·검사·배포는 [alpha.6 배포 기록](ISSUE-5-ALPHA6-RELEASE.md)에서 구분한다. 이 문서의 기존 동작·검사 수치·해시·게시 시점 기록은 보존한다.
