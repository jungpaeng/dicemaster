# DiceMaster v1.0.6-alpha.6 — 팝업 안내·효과색·후보 구분

팝업에서 반복되던 클릭·터치 안내를 줄이고 결과 요약·상세를 기존 로그와 같은 효과색으로 표시한다. 정상 발동 불가 후보의 이름·상태는 회색으로 구분하면서 행 선택과 명시 조건 무시 조작은 유지한다.

**전체 소스 후보·격리 재빌드와 회귀 검사는 완료했으며 고정 커밋 재빌드와 공개는 준비 중이다.** 대상은 `v1.0.6-alpha.6`, 설치 자산은 `DiceMaster-v1.0.6-alpha.6.zip`이다. 아래는 로컬 검토 후보 결과이며 새 고정 소스·공개 파일 검증은 완료 후 보충한다. [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5로 유지하며 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다.

## alpha.5 이후 변경

### 반복 안내 제거와 세 줄 목록

행마다 표시하던 클릭·터치 선택/진행 안내와 하단의 같은 첫 안내 줄을 제거한다. 모든 후보 행은 **제목 / 발동 자격 / 결과 요약**의 세 줄이며 선택 전후 높이를 유지한다.

첫 클릭·터치는 선택만 하고 선택된 같은 행의 새 클릭·터치는 표시된 진행을 요청한다. 진행 버튼의 일반 진행·조건 무시 문구와 A/Enter/Shift+Enter, B/Escape 무작위는 유지한다. ↑↓는 후보 이동이며 선택 결과·발동 조건은 휠·오른쪽 스틱으로 목록과 별도로 읽는다. 안내를 줄여도 선택과 진행의 입력 계약은 바꾸지 않는다.

### 기존 로그의 결과 효과색 보존

같은 실제 결과를 읽는 기존 정적 분석기가 제공한 효과색을 팝업 요약·상세에도 보존한다. 색이 없는 기존 설명은 본문색을 그대로 사용하며 모든 아이템·플래그 등에 새 색을 붙이는 변경은 아니다.

허용하는 서식은 기존 결과 설명의 6/8자리 HEX 또는 기본 색상 범위다. 말줄임은 색 태그를 제외한 표시 문자와 현재 폭을 기준으로 하며 잘린 색 범위를 닫고 UTF-16 쌍을 분리하지 않는다. 크기·굵기 등 기존 서식 태그는 제거하고 그 밖의 글꼴·태그처럼 보이는 문자열도 서식으로 해석하지 않아 행 크기를 바꾸지 않는다. 제목·발동 상태·조건 설명은 결과 효과색을 해석하는 영역과 분리해 안전하게 문자로 표시한다.

### 정상 발동 불가 후보의 회색 표시

`CanSelect`가 거짓인 조건 미충족·최종 가중치 0·판정 미확인·대상/결과 읽기 불가 후보는 이름·상태를 `#B3B3B3` 회색으로 표시한다. 선택 화살표는 노랑이고 결과 자체의 효과색은 유지한다.

회색은 **정상 발동 가능 여부의 구분**이다. 후보 행은 여전히 선택할 수 있으며 읽기 가능한 미충족·가중치 0·미확인 후보의 명시 조건 무시도 유지한다. 실제 대상·결과 읽기 불가 항목은 이전처럼 실행을 차단한다. 표시색으로 실행 권한을 판정하지 않고 기존 토큰·대상·결과·일반/조건 무시 검증을 계속 사용한다.

결과 설명의 준비 중·미지원·읽기 실패·확인된 효과 없음, 실제 결과 식별자 기반 분석·성공 캐시·0.25초/최대 두 후보 준비·실패 5초 재시도는 유지한다. 설명 실패만으로 일반/조건 무시 가능 여부가 달라지지 않는다. 입력 전환 시 유효한 첫 누름 보존·물리 패드 재획득·하드 경계와 로그 도구 화면 유지도 유지한다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.6.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

설정·기본값 변경은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용하며 로그 도구의 ON/OFF 저장을 유지한다. alpha.5로 복구하면 반복 안내가 있는 네 줄 목록·색 없는 결과 표시로 돌아간다. v1.0.5로 복구하면 이벤트 사전 예약 방식으로 돌아간다.

ZIP은 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브를 배포하지 않는다.

## 검증·알려진 제한

2026-10-20 02:24:47 KST에 전체 소스 후보와 기존 모드 DLL·Git·이전 빌드 출력을 제외한 격리 소스 재빌드가 각각 14개 검사 프로그램을 통과했다. SDK는 8.0.426이며 모드 컴파일은 오류 0개·기존 경고 7개다. 빌드 입력 173개·모드 소스/리소스 71개를 확인했고 격리 빌드는 기존 `DiceMaster.dll` 0개에서 실행했다. 두 빌드의 DLL·cfg·ZIP 바이트와 전체 검사 결과가 일치한다. 새 고정 소스 커밋 재빌드와 공개 다운로드 검증은 아직 실행 전이다.

| 확인 | 이번 결과 |
| --- | --- |
| 전체 소스 후보·자동 검사·SDK/모드 경고 | 14개 프로그램 통과 · SDK 8.0.426 · 모드 오류 0개/기존 경고 7개 |
| 격리 재빌드·입력 목록·DLL/cfg/ZIP 동일 바이트 | 14개 통과 · 기존 모드 DLL 0개 · 입력 173개/소스·리소스 71개 · 검토 DLL/cfg/ZIP 동일 바이트 |
| 반복 안내 없음·세 줄/선택 전후 높이·작은 화면/큰 글자 접근 | 팝업 1,416개 관리 입력/배치 검사 통과 |
| 실제 결과 요약/상세의 로그 효과색·서식 허용 범위·말줄임·잘린 색 범위·이모지 | 실제 후보 DLL 미리보기 2,074개·팝업 회귀 검사 통과 |
| 이름/상태/조건의 안전한 표시·서식으로 행 크기를 바꾸지 않음 | 팝업 리치 결과/문자 경계 회귀 통과 |
| 정상 발동 불가 회색·선택 화살표/효과색 유지·회색 행 선택/명시 조건 무시·읽기 실패 차단 | 팝업의 마우스·변환 터치·별도 터치·패드 확인 회귀 통과 |
| 첫 선택/재클릭·터치·A/Enter/B/Escape·독립 상세 읽기·입력 전환 보호 | 팝업·공통 UI 3,879개·패드 262,831개 검사 통과 · 후보 DLL 연결 86개 |
| 표시만으로 게임 결과/Lua/RNG를 실행하지 않음·캐시/재시도·설명과 실행 권한 독립 | 실제 DLL 미리보기와 실행 런타임 20개 시나리오·103개 단언 통과 |
| 고정 커밋 `--scope prd5` 재빌드·검토 DLL/cfg/ZIP 일치 | 고정 소스 확정·검증 후 기록 |
| 공개 태그·prerelease·Latest·익명 다운로드 CRC/두 파일/바이트·해시 | 게시·검증 후 기록 |
| 실제 Windows 게임·터치·트랙패드·Xbox/XInput | 미실행 · 사용자 확인 대기 |

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API 경계를 대상으로 한다. 실제 Windows 글꼴·색·화면 배치·물리 장치·성능은 별도 확인이다. alpha.5의 [UI 후속 요청](ISSUE-5-ALPHA5-RELEASE.md#2026-10-20-사용자-후속-요청)을 새 DLL의 실게임 확인으로 승계하지 않는다.

결과 설명은 정적 분석 범위이며 모든 미래 분기·난수 결과를 보장하지 않는다. 실행 팝업은 실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원한다. 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음을 조회하고 팝업은 현재 판정의 후보여서 목록 수가 항상 같지는 않다.

큰 로그 이벤트는 조회 전용이고 미션 예약·해제와 주사위는 유지한다. 게임 초점·로그 닫힘 상태에서 패드 열기가 켜져 있으면 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 `EnableGamepadLogOpen=false`로 바꾸고 남은 입력을 해제한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

<details>
<summary>이번 전체 소스·격리 재빌드의 자동 검사 결과</summary>

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
| `EventPopupTests` | PASS: EventPopupTests: 1416 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

전체 후보와 격리 재빌드의 14개 검사 결과 문자열이 일치한다. 서식 처리에 필요한 정적 정규식은 중첩 정적 홀더로 분리해 타입 초기화 순서 의존을 제거했고, 실제 DLL의 첫 결과 읽기 호출을 포함한 회귀가 통과했다. 관리 대역과 실제 API의 범위를 실제 Windows 게임 실행으로 확대하지 않는다.

</details>

## 파일 확인·게시 기록

| 항목 | 이번 식별값 |
| --- | --- |
| 고정 소스 커밋·태그 | 커밋 후 기록 · `v1.0.6-alpha.6` |
| 검토 후보 ZIP 크기·SHA-256 | 261,660바이트 · `971387b60042d63c161ba5a116f731cfd287fd86542969f8d92e1ee875162ef2` |
| 검토 후보 `DiceMaster.dll` 크기·SHA-256 | 620,032바이트 · `a7058beed3df74729e0d87de4517d511cd2e7f004f8a1ade698d7467b24c2348` |
| 검토 후보 `config/DiceMaster.cfg` 크기·SHA-256 | 11,459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |
| 게시 시각·Release/asset ID | 게시 후 기록 |

작업 시작 main은 `cb6c20fdef23d7b19c39171df114e855c3e4b363`다. README·구현·회귀 검사를 포함한 소스를 `origin/main`에 반영하고 검증한 소스에 새 태그를 만든다. 기존 Release·태그·자산과 Latest v1.0.5를 보존하며 이슈 #5는 새 DLL의 실제 확인 대기로 유지한다. 실제 게시·보존 결과는 완료 후 기록한다.

위 식별값은 로컬 `artifacts/issue-5/alpha6-final-candidate/build-manifest.json`과 DLL·설치 ZIP에서 확인했고 `artifacts/issue-5/alpha6-final-clean/clean-build.json`의 격리 재빌드와 대조했다. 로컬 ZIP 이름은 `DiceMaster-source-candidate.zip`이다. 아직 고정 소스·공개 설치 파일의 검증값으로 취급하지 않으며 게시 단계에서 다시 대조한다.

영수증 경로는 로컬 `artifacts/releases/v1.0.6-alpha.6/`다. 고정 재빌드 영수증과 이후 게시·익명 다운로드 검증을 구분한다. 게시 결과의 후속 문서 커밋은 main에 반영하며 빌드 소스·태그·설치 자산을 변경하지 않는다.
