# 이벤트·조건 선택과 일회 예약 — v1.0.6

## 2026-10-21 사용자 Windows 후속 확인

사용자가 이 작업의 같은 채팅에서 **“v1.0.6 Windows 확인을 완료했고 남은 문제가 없습니다.”**라고 답했다. PRD #5 안정화 후보 `v1.0.6`의 소스·태그는 `0d38cc6ce0e33fecf3341eba6df12f96831788e2`, DLL은 654,336바이트·SHA-256 `134ef356b512628e16795314a9308a91c5bb3c16d5c521aaa7a5e5bb0da198dc`다. #6 준비 당시 HEAD `375c39715c91d9a2bcdc629dbfe8a88ac0ff127a`는 해당 소스 이후 문서 변경만 포함한다. 이 답변을 **#5 안정화·해당 후보 사용자 확인에 따른 #6 착수 근거**로 기록한다. v1.0.5의 과거 확인을 대신 사용한 것이 아니다.

사용자가 설치 DLL 해시를 다시 제출하거나 게임·BepInEx·Windows 버전, 장치·Steam Input·화면 배율, 항목별 관찰 결과를 추가한 것은 아니다. 사용자가 명시한 버전의 포괄 확인과 기존 공개 파일 식별값을 연결하며, 미제공 환경을 추정하지 않는다. 아래 배포 당시 자동 검사·파일·세부 시나리오의 미실행 기록은 역사 기록으로 보존한다. 포괄 확인을 각 세부 표의 독립 실행 통과로 바꾸지 않으며, 새 #6 후보의 Windows 검증으로 승계하지 않는다. 이 기록 자체로 GitHub 이슈를 닫지 않는다.

아래 내용은 각 후보의 게시·구현 당시 기록이다. 최신 후속 확인은 위 절을 따른다.

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6/DiceMaster-v1.0.6.zip) · [최신 정식판](https://github.com/jungpaeng/dicemaster/releases/latest) · [사용자 안내](../README.md) · [변경 이력](../CHANGELOG.md)

v1.0.5 이후 개발한 이벤트·조건 선택 팝업, 조건 상세의 분기 예약, 공통 판정·한국어 표시와 클릭·터치 입력 수정을 정식 채널로 배포한다. 이번 DLL의 실제 Windows·Unity 화면·물리 패드·터치 검증은 대기다. 정식 공개는 이 검증이나 이슈 #5 완료를 뜻하지 않는다.

## 최종 사용자 변화

### 판정 직전 결과 선택

지원하는 이벤트 `Position`·조건 `Switch` 판정 직전에 좌측 하단 팝업을 띄워 결과를 고른다. 가능 후보는 일반 진행하며 읽기 가능한 미충족·가중치 0·난수/미지원으로 판정 미확인인 후보는 **조건 무시하고 이번만 진행**으로 구분한다. 둘 다 A/Enter 또는 선택한 행의 다음 클릭·터치로 요청한다. 대상·결과를 읽을 수 없는 항목은 실행하지 않는다. 실행 직전 조건·대상·결과가 바뀌면 오래된 요청을 거부하며 일반 진행을 조건 무시로 자동 변경하지 않는다.

B/Esc는 이벤트의 원래 추첨 또는 Switch의 원래 조건·난수 판정으로 한 번 진행한다. 결과 직접 지정은 해당 판정의 원래 조건·가중치·난수를 건너뛰지만 능력치·호감도·조건 데이터를 먼저 수정하지 않는다. Switch 자체 실행 횟수는 한 번 증가하고 선택한 결과 뒤의 대사·효과·내부 분기는 게임이 처리한다.

이벤트 팝업의 새 기본값은 `EnableEventSelectionPopup=true`, 조건 팝업은 `EnableSwitchSelectionPopup=false`다. 각 탭 상단·도구에서 별도로 켜고 끄며 빈 목록에서도 변경·저장할 수 있다. 기존 cfg의 저장값을 보존하므로 기존 ON을 자동으로 OFF로 바꾸지 않는다. 두 기능은 `EnableOptionInfo=true`가 필요하다. 유효한 예약이 없을 때 설정 OFF는 새 팝업을 생략하고 기존 미확정 대기는 원래 판정으로 돌린다. 로그가 열려 있으면 실제 실행은 닫을 때까지 기다린다.

### 조건 상세와 일회 예약

조건 목록은 체크포인트당 한 행으로 두고 상세에 모든 분기·조건을 함께 표시한다. 상세의 첫 클릭·터치는 분기 선택, 같은 분기의 새 누름·해제는 예약·해제다. 키보드·패드는 체크포인트 선택 후 → 또는 A/Enter로 상세 진입, ↑↓로 분기 선택, 새 A/Enter로 예약·해제, ← 또는 B/Esc로 목록 복귀다. 체크포인트 선택이나 상세 진입만으로 예약하지 않는다.

조건·이벤트는 서로 독립적으로 하나씩 예약하며 같은 탭의 다른 예약은 이전 것을 교체한다. 예약된 분기·체크포인트·이벤트·미션에 `(예약 중)`을 표시한다. 다음 일치하는 실제 체크포인트 호출에서 결과를 한 번 지정하며 팝업 ON/OFF보다 우선한다. 팝업 대기 중 큰 로그에서 같은 판정을 예약한 경우도 Lua 재개 때 적용한다. 기존 이벤트의 `TargetEvent` 가중치 99999 보정 대신 실제 호출의 결과를 지정한다. 미션 예약은 유지한다.

예약은 즉시 실행·행동 재개를 보장하지 않는다. 대상·결과·소속·Switch 모드/횟수가 바뀌면 폐기하고, 실패·이전 값으로는 예약을 바꾸지 않는다. 일반 이야기/Lua 교체에는 유지하지만 로드·새 게임 등 실행 전환에서 해제하며 cfg·세이브에 저장하지 않는다. 예약 해제는 이미 소비된 결과나 진행·저장한 게임 상태를 되돌리지 않는다. `EnableOptionInfo=false`일 때 조건·이벤트 예약은 적용하지 않는다.

### 조건·결과 표시

두 팝업은 굵은 제목·효과색을 유지한 결과 요약·조건 요약의 고정 세 줄이다. 선택 상세는 필요한 짧은 판정 이유 → 전체 조건 → 예상 결과 순서로 읽으며 같은 조건식을 중복하지 않는다. 이벤트 상세는 활성 조건 전체·기본/증감/최종 가중치를, 조건 분기는 앞 분기 우선순위·기본 분기·이번 실행 횟수 비교를 보존한다. 가중치는 확률 `%`가 아니다.

로그 이벤트의 제목·상세 조건과 조건 상세의 각 분기도 같은 읽기 전용 판정으로 표시한다. 확정 미충족·최종 가중치 0·앞 분기 우선만 회색이다. 난수·미지원 계산·앞 분기 미확정은 기본색과 `?`·사유로 구분하며 대상·결과/판정 자료 확인 실패는 `!`와 사유를 표시한다. 읽기 가능한 회색 후보의 선택·명시 조건 무시·예약은 허용하며 결과 효과색과 체크포인트 목록의 기본 표시는 유지한다.

표시 기준은 과거 실행 당시가 아닌 마지막 정상 조회 시점의 현재 상태이며 조건 횟수 분기는 다음 실행 횟수로 본다. 조회 대기·실패·게시 실패 때 이전 회색을 현재 미충족처럼 유지하지 않는다. 조건 난수는 범위·식·미확정으로 표시하고 실제로 뽑힌 값처럼 보이는 예시값을 쓰지 않는다. 표시·정렬·설정 조회가 RNG·조건 카운터·예약·세이브를 바꾸지 않는다.

플래그·호감도·아이템·기능·개발 항목·도감·날짜와 결과 설명에 기존 한국어 이름·번역을 연결한다. 이름 자료가 늦게 준비되면 기존 예산 안에서 다시 조회한다. 번역이 없는 이름과 식별용 내부 ID는 남을 수 있다. 설명 형식화 실패만으로 이미 확인한 실행 자격을 바꾸지 않는다. 정적 분석 범위 밖의 후속 결과를 확정적으로 예측하지 않는다.

### 클릭·터치·패드와 로그 전환

전체 로그의 탭·목록·설정과 팝업에 누름·해제 기반 클릭 처리를 적용한다. 다른 로그 탭에서 터치가 동작하지 않던 경로와 도구·이전 탭 동시 선택 표시를 수정한다. 터치로 도구를 연 뒤 패드로 전환해도 화면·선택·스크롤을 유지하고 새 패드 입력은 해제·중립 뒤 받는다.

팝업의 첫 클릭·터치는 선택, 선택된 같은 행의 바로 다음 새 누름·해제는 한 번 진행이다. 자동 입력 모드 관측만으로 유효한 누름·이미 접수한 진행을 취소하지 않으며 초점·Lua 문맥·대상·소유자·연결의 실제 안전 경계는 취소한다. 접수한 후보·결과를 보관해 이후 표시 갱신이 실행 대상을 바꾸지 않게 한다.

팝업 중에도 기존 `LogMenuToggleKey`(기본 Tab)와, `EnableGamepadLogOpen=true`일 때 같은 패드의 L3/R3 클릭 겹침으로 큰 로그를 연다. **팝업의 로그 열기 버튼은 제거했다.** 하단 실행 버튼·별도 무작위 목록 항목도 두지 않으며 A/Enter와 B/Esc 텍스트 안내를 제공한다. 실행을 요청하는 포인터 영역은 후보 목록이다. 터치만으로 원래 판정을 요청하는 UI는 제공하지 않는다.

로그가 열린 동안 로그만 입력을 받고 대기 팝업 Lua 흐름·같은 판정의 예약 소비·설정 OFF 뒤 원래 판정은 닫을 때까지 기다린다. 닫으면 유효한 팝업 선택·스크롤을 복원하고 닫기 B/Esc나 누른 채인 A를 진행에 재사용하지 않는다. 전체 버튼 해제·스틱/트리거 중립 뒤 새 입력을 받는다.

## 설치·업데이트와 복구

Windows용 활협전과 **BepInEx를 포함한 활협전용 모드 베이스**가 필요하다. 설치 자산은 `DiceMaster-v1.0.6.zip`이며 게임 소스·게임 DLL·다른 모드를 포함하지 않는다. GitHub 자동 Source code 압축 파일은 설치용이 아니다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 위 설치 ZIP을 푼다. 업데이트는 **DLL만 교체**해 기존 cfg를 보존한다. 최초 설치는 아래 두 파일을 기존 `BepInEx` 아래에 넣는다.
3. 설정 파일에서 바꾼 값은 게임 재시작 후 사용한다. 로그의 팝업 설정·창 위치·크기·글자는 바로 반영하고 저장한다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

`EnableEventSelectionPopup=true`·`EnableSwitchSelectionPopup=false`는 새 cfg 또는 키가 없는 경우의 기본값이다. 시험판에서 저장한 조건 팝업 ON은 유지하므로 끄려면 조건 탭 상단에서 OFF로 바꾼다. 단축키·창 배치·글자 크기 등 기존 설정은 DLL만 바꾸면 보존한다. 새로운 UI 저장 파일·세이브 형식을 추가하지 않는다.

문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. 직전 정식 [v1.0.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5)로 복구하면 이번 선택 팝업·조건 상세 예약·표시·입력 보완은 제공하지 않고 해당 버전의 이벤트 예약 방식을 사용한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

## 지원 범위·알려진 제한

- 이벤트 팝업은 직접 문자열 리터럴 `Position` 호출을 사용하는 비메인 Lua 코루틴에 표시한다. 전체 정상 후보가 하나면 생략하지만 미충족·가중치 0·미확정 후보가 함께 있으면 표시한다.
- Switch 팝업은 직접 리터럴 호출 결과를 지역 변수에 대입한 뒤 바로 정수 동등 비교 `if/elseif/else`로 분기하는 비메인 Lua 코루틴을 지원한다. 분기가 둘 미만이거나 지원 밖 형식이면 원래 판정으로 진행한다. 강릉 포위전 휴식 뒤 `Ch_3_6_4_002`가 지원 예다.
- 예약 소비는 실제 체크포인트 호출에 연결하므로 팝업 밖 Lua 형식·메인 Lua·직접 C# 호출에서도 일치하는 유효 예약을 적용할 수 있다. 해당 호출자가 지정된 결과를 처리하거나 모든 후속 조건을 통과한다고 보장하지 않는다.
- 패드는 Windows Xbox/XInput 기준이다. 다른 패드·Steam Input 구성·다른 모드와의 조합은 환경별 확인이 필요하다. native 입력 차단 경계를 확인하지 못하거나 실패하면 위험한 패드 확정을 막고 키보드·마우스로 복구한다.
- 패드 열기가 켜져 있고 게임 창 초점·로그 닫힘 상태이면 단독 L3/R3도 게임에 보내지 않는다. 원래 스틱 클릭 기능이 필요하면 `EnableGamepadLogOpen=false`로 끈다. 이는 열린 로그·팝업의 패드 탐색과 입력 차단 전체를 끄는 옵션이 아니다.
- Tab 진입 뒤 첫 패드 입력이 먹지 않는 기존 입력 모드 경로는 별도 확인 대상이다. 같은 패드의 L3/R3 겹침으로 열어 볼 수 있다.
- 표시 조회는 읽기 전용이며 조건·결과 설명은 정적 분석이다. 이름·내부 ID·지원하지 않는 계산·순환·표시 한도에 따른 미확정은 남는다. 자동 검사·바이트 일치는 실게임의 화면·입력 호환성·성능 보장이 아니다.

## 이번 파일 검증

2026-10-20 23:30:56 KST, .NET SDK 8.0.426으로 전체 소스 후보를 빌드하고 14개 검사 프로그램을 통과했다. 198개 빌드 입력·81개 소스/리소스와 읽기 전용 참조를 기록했다. 이번 버튼 제거는 후보 GUI 연결 113개·팝업 입력/배치 2,116개 검사에 반영했으며 기존 로그 단축키·대기/재개와 선택·스크롤 보존 검사도 통과했다. 고정 커밋을 기존 관리 모드 DLL 0개인 별도 소스 스냅샷에서 재빌드해 23:35:34 KST 같은 14개 검사와 전체 입력·참조·검사 결과·DLL/cfg/ZIP 바이트 일치를 확인했다.

후보 소스 빌드와 고정 소스 재빌드, 설치 ZIP의 CRC·정확한 두 파일·내부 바이트·SHA-256, 공개 다운로드 대조 결과는 아래 표와 게시 결과에 기록한다. 과거 시험판의 검사 횟수·Windows 확인을 이번 새 실행으로 옮기지 않는다.

| 자동 검사 | 이번 결과 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed. / PASS: 4265 managed behavior assertions and 1278 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2101 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 113 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5232 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2116 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

관리 코드·모의 게임/Unity/Win32 경계와 실제로 제공된 MoonSharp를 사용한 검사를 실제 Windows·Unity·물리 장치 실행과 구분한다. 빌드 입력·게임/로더 참조 무변경 확인과 검사별 상세 영수증은 로컬 산출물에 보관하며 공개 설치 파일에 게임 참조를 포함하지 않는다.

## Windows·실제 장치 확인표

이번 `v1.0.6` DLL은 아래 항목을 아직 실제 게임에서 실행하지 않았다. 2026-10-20의 “v1.0.5에서 Windows 확인 완료” 답변은 선행 버전의 근거이며 이번 파일에 자동 승계하지 않는다. 시험 뒤 이번 파일 해시·날짜·환경·관찰 결과를 이 표에 추가하고 이슈 #5 완료 여부를 따로 판단한다. 상세한 이전 시나리오는 [Windows 검증표](ISSUE-5-WINDOWS-VALIDATION.md)에 보존한다.

| 기록할 항목 | 상태 |
| --- | --- |
| 확인 일시·확인자, 이번 DLL SHA-256 | 미기록 |
| 게임·BepInEx·Windows 버전 | 미기록 |
| 패드·터치/트랙패드 모델·연결/인식 방식·Steam Input·해상도/UI 배율 | 미기록 |
| 이벤트/조건 팝업 ON/OFF·기존 cfg 보존·빈 탭 설정 | 미실행 |
| 지원 Position/Switch 대기·원래 판정·일회 명시 조건 무시·실행 횟수 | 미실행 |
| 조건 체크포인트/상세 분기 선택·예약/교체/해제·일회 소비·팝업 우선순위 | 미실행 |
| 후보 첫 터치 선택·바로 다음 재터치 한 번 진행·A/Enter·B/Esc | 미실행 |
| 로그 모든 탭·도구의 클릭/터치·도구 선택 표시·터치 후 패드 복귀 | 미실행 |
| 팝업 내 로그 버튼 없음·Tab/L3·R3 열기·로그만 초점·선택/스크롤 복원 | 미실행 |
| 미충족 회색·난수 미확정·한국어 이름/날짜·결과색·긴 설명/작은 화면 | 미실행 |
| 로드/새 게임·초점·문맥/대상 교체·패드 연결 변화·중복 실행 방지 | 미실행 |
| native XInput 차단·해제/중립·기존 로그/미션/주사위·설치/복구 | 미실행 |

## 파일·소스 식별값

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.6.zip | 274965 | `5e6b1a87ed77ffc34f05719ebb94e80900b84e92e0af372b2464e86445ef6662` |
| DiceMaster.dll | 654336 | `134ef356b512628e16795314a9308a91c5bb3c16d5c521aaa7a5e5bb0da198dc` |
| DiceMaster.cfg | 11761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

위 값은 검토 후보·고정 커밋 재빌드·익명 공개 다운로드에서 일치한 파일 식별값이다. 빌드 소스와 [`v1.0.6` 주석 태그](https://github.com/jungpaeng/dicemaster/tree/v1.0.6)는 [`0d38cc6ce0e33fecf3341eba6df12f96831788e2`](https://github.com/jungpaeng/dicemaster/commit/0d38cc6ce0e33fecf3341eba6df12f96831788e2)로 고정했다.

## 게시 결과

사용자가 정식 `v1.0.6`을 명시적으로 선택한 뒤 **2026-10-20 23:36:49 KST에 [정식 Release](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6)를 게시했다.** 구현·회귀 검사·README·정식 준비 문서가 포함된 소스 커밋 `0d38cc6ce0e33fecf3341eba6df12f96831788e2`와 주석 태그를 `origin/main`에 atomic push했다. Release ID `418504455`·asset ID `659572711`, `draft=false`·`prerelease=false`·`make_latest=true`이며 Latest는 v1.0.5에서 v1.0.6으로 변경했다.

공개 Release·설치 자산을 인증 없이 다시 읽고 다운로드한 ZIP의 CRC·정확히 두 파일·내부 바이트·SHA-256을 검토 후보·고정 커밋 재빌드와 대조해 통과했다. 기존 41개 Release·42개 태그와 자산을 보존했고 이슈 #2 closed·#4 closed·#5 open 상태를 유지했다. 실제 Windows·장치 실행과 이슈 #5 완료는 대기다.

고정 소스 재빌드는 정식 배포 지시를 별도 영수증에 기록하고 기존 과거 소스 동일성 정책·prerelease 전용 CLI를 수정하지 않았다. 정식 채널 선택을 Windows 검증 완료로 해석하지 않으며 파일의 실제 미실행 경계를 유지했다. 영수증은 로컬 `artifacts/releases/v1.0.6/`의 `release-verification.json`·`reviewed-candidate-comparison.json`·`build-manifest.json`·`github-publication.json`·`anonymous-verification.json`·`tag-verification.json`·`release-body.md`에 보관한다. 독립 감사는 `artifacts/popup-log-button-removal/independent-audit.json`이다.

이 게시 결과 보충은 별도 main 문서 커밋이며 빌드 소스·태그·공개 설치 파일은 변경하지 않는다.

## 변경 근거와 이전 기록

- [팝업·로그 전환과 조건 표시](ISSUE-5-POPUP-LOG-CONDITIONS.md)
- [조건·요약·판정의 공통 기준](ISSUE-5-POPUP-CONDITION-PARITY.md) · [로그 판정 기준](ISSUE-5-LOG-JUDGMENT-DISPLAY.md)
- [조건 상세 선택](ISSUE-5-CONDITION-DETAIL-SELECTION.md) · [예약 수명·실행 경계](ISSUE-5-CHECKPOINT-RESERVATIONS.md)
- [한국어 표시 연결·검증](ISSUE-5-ALPHA18-RELEASE.md) · [입력 경계 구조·측정](DEVELOPMENT.md#입력-경계-구조-정리)
- 개발 순서별 변경·검사·게시 당시 상태는 [변경 이력](../CHANGELOG.md)과 각 시험판 배포 기록에 보존한다.
