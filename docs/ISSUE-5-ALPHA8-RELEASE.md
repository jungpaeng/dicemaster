# DiceMaster v1.0.6-alpha.8 — 터치 실행 요청 보존·하단 안내 정리

선택된 팝업 행을 다시 터치해 접수한 진행 요청이 자동 입력 모드 관측으로 지워지던 경로를 보완한다. 하단의 선택 이벤트·무작위 실행 버튼은 텍스트 안내로 바꾸고 클릭·터치 실행은 목록에 모은다. 구조 리팩터링은 이번 범위에서 보류한다.

**2026-10-20 03:18:58 KST에 [v1.0.6-alpha.8](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.8) prerelease를 게시했다.** **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.8/DiceMaster-v1.0.6-alpha.8.zip)**. 소스·태그 `7d79363bc33dffac80d453fe6a945a6e3edeb513`의 고정 재빌드·14개 검사와 익명 공개 페이지·다운로드의 CRC·두 파일·내부 바이트·SHA-256 대조를 통과했다. [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)은 v1.0.5로 유지하며 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다.

## alpha.7 이후 변경

### 접수한 터치 진행 요청 보존

사용자는 선택된 행의 재터치와 하단 선택 이벤트·무작위 버튼에서 여러 번 터치해야 실행되는 증상을 보고했다. 추가 답변은 **패드 A에서는 문제 없음 / 키보드는 없어 미시험**이다. 트랙패드 클릭 결과·설치 파일 해시·실제 장치의 입력 모드 변화 로그는 제공되지 않았다. [alpha.7의 제한된 후속 보고](ISSUE-5-ALPHA7-RELEASE.md#2026-10-20-사용자-후속-확인)를 새 DLL의 통과 근거로 사용하지 않는다.

관리 경계 조사에서는 일반·명시 조건 무시·무작위 요청을 접수한 뒤 다음 Lua 재개 전에 게임 입력 모드 관측이 바뀌면 요청이 취소되는 경로를 재현했다. 이 경로의 존재를 사용자 장치에서 관찰된 모든 반복 터치의 단일 원인으로 확정하지 않는다.

자동 입력 모드 관측만으로 같은 유효 행동에 이미 접수된 요청을 취소하지 않도록 수정한다. 접수한 후보·결과를 보관하여 이후 후보 갱신이 확정 대상을 바꾸지 않게 한다. 첫 클릭·터치는 선택만 하고 선택된 같은 행을 새로 눌러 같은 행에서 떼면 일반 진행/명시 조건 무시를 요청한다. 실제 초점·문맥·대상·결과·소유자·연결·차단 준비·화면 전환의 안전 경계는 요청을 취소하며 실행 직전 후보 자격·대상·결과·토큰과 한 번 소비 권한을 다시 검증한다. 일반 진행을 조건 무시로 자동 승격하지 않는다.

### 하단은 조작 안내 텍스트

하단의 `이 이벤트로 진행` / `조건 무시하고 이번만 진행`과 `무작위로 진행` 실행 버튼을 제거한다. 선택 상태에 맞춘 A/Enter 진행 안내와 **B/Esc 무작위 진행 안내**를 텍스트로 표시한다. 읽을 수 없는 후보는 실행 불가 안내를 표시하며 안내를 클릭·터치해도 실행하지 않는다.

클릭·터치 실행은 목록에서만 요청한다. 목록에 가상 무작위 항목도 없으므로 **무작위는 B/Esc 전용이며 터치만으로 무작위 진행하는 UI는 제공하지 않는다.** 초기 후보 미선택·선택 전 A/Enter 무동작·↑↓ 첫 후보 탐색을 유지한다. Enter/Shift+Enter와 패드 A의 기존 확인을 유지하며 새 키·패드 조합은 추가하지 않는다.

굵은 이벤트 제목·효과색을 보존한 결과 요약의 두 줄, 회색 후보 구분·노랑 선택 화살표, 예상 결과·발동 조건의 독립 상세 스크롤은 유지한다. 결과 정적 분석·성공 캐시·0.25초 최대 두 후보 준비·실패 5초 재시도·숨김 분석 0과 로그 도구 화면 유지·입력 해제/중립 보호도 유지한다. 표시 때문에 이벤트 결과나 Lua를 실행하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 이 버전 Release의 Assets에서 **`DiceMaster-v1.0.6-alpha.8.zip`**을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

설정·기본값 변경은 없다. `EnableEventSelectionPopup=true`와 상위 `EnableOptionInfo`를 사용하며 로그 도구 ON/OFF 저장을 유지한다. alpha.7으로 복구하면 하단 선택 이벤트·무작위 버튼과 접수 뒤 모드 관측 취소 경로도 돌아온다. v1.0.5는 이벤트 사전 예약 방식이다.

ZIP은 `plugins/DiceMaster/DiceMaster.dll`과 `config/DiceMaster.cfg` 두 파일만 포함한다. 게임 소스·게임 DLL·새 세이브를 배포하지 않는다.

## 검증·알려진 제한

2026-10-20 03:16:06 KST의 전체 소스 후보와 03:16:05 KST의 격리 소스 재빌드가 각각 **14개 검사 프로그램**을 통과했다. SDK는 8.0.426이며 모드 컴파일은 오류 0개·기존 경고 7개다. 빌드 입력 176개·모드 소스/리소스 71개를 확인했고 격리 빌드는 기존 모드 DLL 0개에서 실행했다. 두 빌드의 DLL·cfg·ZIP 바이트와 전체 검사 결과가 일치하며 현재 176개 빌드 입력의 SHA-256도 후보 manifest와 일치한다. 로컬 ZIP의 CRC·정확히 두 파일·내부 DLL/cfg 바이트·해시를 대조했다. 03:18:16 KST에 소스 `7d79363bc33dffac80d453fe6a945a6e3edeb513`의 고정 재빌드도 기존 모드 DLL 0개에서 14개 프로그램을 통과했고 검토 후보·격리 빌드와 파일 바이트·전체 검사 결과가 일치했다. 게시 후 익명 공개 다운로드의 CRC·정확히 두 파일·내부 DLL/cfg·SHA-256을 대조했다. 아래 수치는 이번 실행에서 읽었으며 이전 alpha.7 결과를 재사용한 것이 아니다.

클릭 접수만이 아닌 **실제 소스 팝업 → 런타임 → 실제 MoonSharp Lua 재개 → `Position` 한 번 실행·결과 소비**까지 연결하는 회귀로 확장했다. 자동 모드 관측은 실제 후보 DLL의 `ObserveEventInputMode` IL을 명시적 게임/Unity 대역에 복사해 실행한다. 팝업 실행 경계 51개 시나리오와 직접 선택 스냅샷 4개 사례를 포함해 런타임 전체 84개 시나리오·581개 단언을 확인했다.

| 확인 범위 | 이번 상태 |
| --- | --- |
| 접수 → 늦은 자동 모드 관측 → Lua 재개 → 일반/명시 조건 무시/무작위 결과 한 번 소비 | 런타임 84개 시나리오·581개 단언 통과 · 실제 후보 observer IL·소스 팝업/런타임·MoonSharp 연결 |
| 접수 뒤 후보 갱신·대상/결과 변경 거부·실제 초점·문맥·연결·소유자·차단 준비·화면 전환 취소 | 런타임·팝업 입력 회귀 통과 · 갱신 전후 결과/자격 교체 거부 · 명시 하드 취소 뒤 새 확인 필요 |
| 하단 실행 버튼 없음·안내 클릭 무동작·목록 첫 선택/재확인·B/Esc 무작위 | 팝업 1,538개 관리 입력/배치 검사와 후보 DLL 연결 87개 통과 |
| 굵은 두 줄·효과색·회색 후보·독립 상세 스크롤·해상도/글자 크기별 읽기 | 팝업 표시·입력 회귀와 실제 후보 DLL 미리보기 2,074개 검사 통과 |
| 기존 로그·패드·미션·주사위·캐시/재시도·실제 참조 API | 전체 후보·격리 빌드 각각 14개 프로그램 통과 · 아래 실제 결과 |
| 후보·격리 재빌드의 동일 DLL/cfg/ZIP·입력 해시·검사 결과 | 두 빌드 일치 · 176개 입력/71개 소스·리소스 · 로컬 ZIP CRC·두 파일·내부 바이트·해시 확인 |
| 고정 커밋 `--scope prd5` 재빌드·검토 파일 일치 | `7d79363bc33dffac80d453fe6a945a6e3edeb513` · 기존 모드 DLL 0개에서 14개 프로그램 통과 · 검토/격리 빌드와 동일 파일·검사 결과 |
| 태그·prerelease·익명 공개 페이지/ZIP의 CRC·정확히 두 파일·바이트·해시 | 검증 통과 · main에 검증 소스 포함 |
| 기존 Release·태그·자산·Latest v1.0.5·이슈 상태 보존 | 기존 31개 Release·태그·자산과 Latest v1.0.5, #2 closed·#4 open·#5 open 보존 |
| 실제 Windows 게임·트랙패드·터치·Xbox/XInput | 미실행 · 사용자 확인 대기 |

회귀 검출력도 음성 대조로 확인했다. alpha.7의 이전 observer를 실제 DLL에서 가져오면 `DelayedSchemeObservation`의 “first accepted request survives … completes exactly once” 단언이 실패했다. 후보 스냅샷 보호가 없는 중간 구현에서는 접수 뒤 결과 변경·후보 갱신·Lua 재개 사례의 “never bypasses changed result/eligibility” 단언이 실패했다. 수정 후 같은 검사를 통과했다. 원문은 로컬 `/tmp/dicemaster-alpha8-runtime-fix-runtime-alpha7-negative.log`와 `/tmp/dicemaster-alpha8-runtime-fix-drift-negative.log`이며 재현 방법은 `tests/EventSelectionRuntimeTests/RuntimeModeIntegration.cs`·`PopupExecutionRegression.cs`·`Program.cs`에 남긴다. 이 음성 대조는 Windows 장치 재현이 아니다.

자동 검사는 관리 코드·명시적 Unity/게임/native 대역과 실제 참조 API 경계를 대상으로 한다. 실제 Windows 글꼴·화면 배치·물리 장치·성능은 별도 확인이다. 이번 새 DLL의 실제 터치에서 개선됐는지는 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)에 기록한다.

결과 설명은 정적 분석 범위이며 모든 미래 분기·난수 결과를 보장하지 않는다. 실행 팝업은 실제 manager의 직접 리터럴 점 `checkpointmanager.Position("...")`이 비메인 Lua 코루틴에서 실행되는 경로를 지원한다. 동적 인수·별칭·콜론·메인 Lua·직접 C# 동기 호출은 원래 판정으로 진행한다. 큰 로그는 여러 판정 묶음을 조회하고 팝업은 현재 판정의 후보여서 목록 수가 항상 같지는 않다.

큰 로그 이벤트는 조회 전용이고 미션 예약·해제와 주사위는 유지한다. 게임 초점·로그 닫힘 상태에서 패드 열기가 켜져 있으면 단독 L3/R3도 게임에 보내지 않는다. 원래 클릭 기능이 필요하면 `EnableGamepadLogOpen=false`로 바꾸고 남은 입력을 해제한다. [입력 지원 범위](GAMEPAD.md) · [구현 경계](ISSUE-5-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

<details>
<summary>이번 전체 소스·격리·고정 커밋 재빌드의 14개 자동 검사 결과</summary>

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
| `LogUiWiringTests` | PASS: 87 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| `LogUiKeyboardTests` | PASS: LogUiKeyboardTests: 3879 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| `LogOpenerIntegrationTests` | PASS: LogOpenerIntegrationTests: 1408 assertions across 35 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| `EventSelectionRuntimeTests` | PASS: event execution runtime 581 assertions across 84 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| `EventPopupTests` | PASS: EventPopupTests: 1538 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |

전체 후보·격리·고정 커밋 재빌드의 14개 결과 문자열이 일치한다. 관리 대역과 실제 API 경계의 검사이며 실제 Windows 게임 실행으로 확대하지 않는다.

</details>

## 파일 확인·게시 기록

| 파일 | 이번 공개 파일 크기·SHA-256 |
| --- | --- |
| `DiceMaster-v1.0.6-alpha.8.zip` | 261,428바이트 · `9f8b5a7bc4e1e5004c763bce4cc4d66cbeea61a411dce173736f9b939eb48479` |
| `DiceMaster.dll` | 619,520바이트 · `884a278ccacb1119696e00bb66140dcc900206909d3dba105f9cdbb677bbe53a` |
| `config/DiceMaster.cfg` | 11,459바이트 · `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |

위 파일은 전체 후보·격리·고정 소스 재빌드와 공개 다운로드에서 일치한다. 로컬 이름 `DiceMaster-source-candidate.zip`을 공개 이름 `DiceMaster-v1.0.6-alpha.8.zip`으로 바꾸면서 재압축하지 않았고 바이트·해시를 대조했다. DLL 크기는 alpha.7과 같은 619,520바이트다. 로컬 기록은 `artifacts/issue-5/alpha8-final-candidate/build-manifest.json`·`alpha8-final-clean/clean-build.json`·`alpha8-final-candidate-audit.json`에 남긴다.

| 게시 항목 | 이번 식별값 |
| --- | --- |
| 고정 소스 커밋·태그 | [`7d79363bc33dffac80d453fe6a945a6e3edeb513`](https://github.com/jungpaeng/dicemaster/commit/7d79363bc33dffac80d453fe6a945a6e3edeb513) · `v1.0.6-alpha.8` |
| 게시 시각·Release/asset ID | 2026-10-20 03:18:58 KST (2026-10-19 18:18:58 UTC) · Release `417602057` · asset `657055734` |

작업 시작 main은 `572ba1df65a18e94af20be21d566430dd84c13ba`다. 최신 `origin/main`에서 작업했고 소스·README·회귀 검사를 포함한 검증 소스 `7d79363bc33dffac80d453fe6a945a6e3edeb513`를 main에 반영했다. 새 태그는 검증 소스를 가리키며 `draft=false`·`prerelease=true`·`make_latest=false`로 게시했다. 기존 31개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.8/`의 `release-verification.json`·`build-manifest.json`·`anonymous-verification.json`·`github-publication.json`·`tag-verification.json`·`independent-anonymous-audit.json`·`release-body.md` 등에 보존한다. 별도로 인증 없이 공개 API·페이지·다운로드·주석 태그를 읽어 Release/asset ID·파일 바이트·CRC·해시·Latest를 대조한 독립 확인도 통과했다. `release-verification.json`의 `published=false`는 게시 전 고정 커밋 검사 시점이며 실제 게시·익명 검증과 구분한다. 게시 결과 보충은 빌드 소스·태그·DLL/cfg/ZIP을 바꾸지 않는 별도 main 문서 커밋에 기록하고 Release 본문은 해당 고정 문서 커밋의 공개 절대 URL로 연결한다.
