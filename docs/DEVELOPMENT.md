# 개발 안내

[사용자 안내](../README.md) · [변경 이력](../CHANGELOG.md) · [Release 등록](RELEASING.md) · [이슈 #1 검증 기록](ISSUE-1-VALIDATION.md) · [게임 API 분석 개요](GAME-API-AUDIT.md) · [실제 게임 API 분석](REAL-GAME-API-FINDINGS.md) · [수명주기 분석](GAME-LIFECYCLE-ANALYSIS.md)

## 개발 범위

현재 작업은 [이슈 #1 PRD](https://github.com/jungpaeng/dicemaster/issues/1)의 로그 자동 갱신과 난수 미리보기 후속 구현입니다. 사용자가 문제 수정과 `v1.0.3-alpha.2` prerelease 재발행을 승인했으며 최종 관리 코드·표시·반복 패치·직접 패드와 로컬 ZIP 검증을 통과했습니다. 게시 커밋·공개 다운로드는 준비 중이며 alpha.1의 통과 수치를 승계하지 않습니다. 실제 게임 API 분석과 사용자의 Windows 게임 검증을 완료하기 전에는 기능 완료로 표시하거나 정식 Release를 공개하지 않습니다. [`v1.0.3-alpha.1` prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.1)는 2026-10-18 21:08:36 KST에 게시되었습니다. 배포 기준 커밋은 `2d8936786845b534524b916537e7f1c3e5ab482d`이며 공개 다운로드의 ZIP·DLL 바이트 일치까지 확인했습니다. prerelease 게시 승인은 Windows 실행 승인이 아닙니다. 실제 관리 DLL의 API·로드 경계 정적 분석은 macOS에서 수행했습니다. runtime·native Unity·실제 리소스·Windows 검증은 미실행입니다. Windows PC는 기존 파일·정보 읽기만 허용되며 게임 실행이나 후보 설치를 요청하지 않습니다. 자동 검사는 그중 하나의 검증 단계입니다. 구현 목표와 확인 결과는 [검증 기록](ISSUE-1-VALIDATION.md)에서 구분합니다.

이 저장소는 기존 DiceMaster DLL을 기반으로 유지보수합니다.
`development/`에는 패드 지원 소스, 로그 표시 엔진·게임 경계 어댑터, DLL 패치 도구와 동작 검사가 있습니다.
선택지 분석과 기존 번역 기능의 전체 C# 소스를 새로 빌드하는 프로젝트는 아닙니다.

빌드는 현재 `plugins/DiceMaster/DiceMaster.dll`을 입력으로 사용합니다. 패드 확장과 로그 갱신 패치를 각각 적용하며 다른 기능을 유지하는 방식입니다.

## 파일 구성

```text
README.md                              # 설치·조작·설정 요약
CHANGELOG.md                           # 배포 버전별 변경 이력
docs/
├── DEVELOPMENT.md                     # 개발·빌드·검증
├── RELEASING.md                       # Release 등록 절차와 공개 조건
├── ISSUE-1-VALIDATION.md               # 요구사항·자동 검사·Windows 확인 기록
├── ISSUE-1-RELEASE-DRAFT.md            # alpha.2 준비·alpha.1 게시 이력
├── GAME-API-AUDIT.md                   # 모드·실제 API·검증 범위의 분석 개요
├── REAL-GAME-API-FINDINGS.md           # 수신 게임 DLL의 표시 API 정적 분석
└── GAME-LIFECYCLE-ANALYSIS.md           # 실제 새 게임·로드·Lua 경계 분석
development/
├── src/DiceMaster.Gamepad/
│   ├── PadController.cs                # 조정·반복·입력 필터·안내 문구
│   ├── NativeXInput.cs                 # Windows XInput·MinHook 연결
│   └── native/                        # 내장할 x86/x64 라이브러리와 라이선스
├── src/DiceMaster.LiveLog/
│   ├── RefreshEngine.cs                # 현재 탭·행별 상태·재시도 스케줄
│   ├── LifecycleState.cs               # 전환·Lua 컴파일 토큰과 조회 차단
│   ├── SafeDisplayEvaluation.cs        # 알려진 자료의 읽기 전용 조건·확률 평가
│   ├── RandomPreview.cs                # 확정값·범위·판정 미정·미지원 결과 모델
│   ├── PreviewFormatting.cs            # 결과 모델의 조건·이벤트 설명 연결
│   └── LogBridge.cs                    # 실제 파서·모드·게임 경계 어댑터
├── tests/GamepadTests/                 # 입력 동작 검증
├── tests/LiveLogEngineTests/           # 표시 엔진 경계 검사
├── tests/LiveLogIntegrationFixtures/   # 게임·Unity 경계 대역
├── tests/LiveLogIntegrationTests/      # 실제 파서·패치 DLL 연결 검사
├── tests/LifecycleTests/              # 순수 전환 상태 검사
├── tests/RealGameApiFixtures/          # 실제 IL의 native/게임 경계 대역
├── tests/RealGameApiTests/             # 수신 DLL의 IL·메타데이터 계약 검사
├── tests/LiveLogArtifactTests/         # 메서드·리소스·중복 연결 구조 비교
├── tests/LogRefreshFixtures/           # v1.0.2 기존 검사 대역
├── tests/LogRefreshTests/              # v1.0.2 기존 래퍼 검사 (한계는 아래 참고)
├── tools/GamepadInjector/              # DLL 통합 도구
├── tools/LogRefreshInjector/           # 로그 갱신 IL 패치 도구
│   ├── SafeEvaluationHooks.cs         # 모드 설명 호출을 순수 평가에 연결
│   └── LifecycleHooks.cs              # 실제 SaveSystem·씬·Lua 경계 연결
├── tools/Shared/                      # DLL payload 복사 도구
├── tools/build-gamepad.py              # 빌드·통합·검증 실행
├── tools/build-log-refresh.py          # 로그 패치·검증 실행
└── releases/                          # 생성한 배포 파일, Git 제외
```

소스·테스트·도구는 Git으로 관리합니다. `bin/`, `obj/`, `development/releases/`는 생성 파일이므로 제외합니다.
저장소에 있는 DebugMenu 설정과 DLL은 별도 모드이며 DiceMaster Release ZIP에는 포함하지 않습니다.

## 빌드 환경

- Python 3: 빌드 스크립트 실행
- .NET 8 SDK: 패드 코드, DLL 통합 도구, 테스트 빌드
- 최초 빌드 시 NuGet 패키지를 복원할 수 있는 환경

패드와 로그 표시 payload 코드는 .NET Framework 4.8을 대상으로 합니다. 참조 어셈블리는 `Microsoft.NETFramework.ReferenceAssemblies.net48` 패키지로 가져옵니다.
DLL 통합 도구와 테스트는 .NET 8을 대상으로 하며, 통합 도구는 Mono.Cecil 0.11.6을 사용합니다.

모드의 실제 패드 실행 대상은 Windows/XInput입니다. 다른 OS에서 입력 로직을 빌드·검증하는 것과 Windows 게임에서 실행하는 것은 별도의 확인 항목입니다.

## 빌드 방법

아래 명령은 macOS의 로컬 개발 후보 생성·검사 예시이며 Windows 게임 설치·실행이나 배포가 아닙니다. 읽기 전용 수신 참조는 입력으로만 사용합니다.

아래 명령은 저장소 루트에서 실행합니다. 환경에 따라 Python 실행 명령은 `python3` 대신 `python`일 수 있습니다.

먼저 별도 파일로 생성하여 변경 내용을 확인할 수 있습니다.

```sh
mkdir -p development/releases/local
python3 development/tools/build-gamepad.py \
  --output development/releases/local/DiceMaster.dll
```

저장소의 실행용 DLL을 갱신하려면 다음을 실행합니다.

```sh
python3 development/tools/build-gamepad.py
```

입력 DLL을 따로 지정할 수도 있습니다.

```sh
python3 development/tools/build-gamepad.py \
  --input plugins/DiceMaster/DiceMaster.dll \
  --output development/releases/local/DiceMaster.dll
```

`development/Directory.Build.props`는 임시 중간 출력 경로를 지정한 빌드에서도 소스 트리의 기존 `obj/`·`bin/` 생성 C# 파일을 다시 포함하지 않게 합니다.

도구는 임시 디렉토리에서 빌드·통합하고 테스트를 실행한 뒤 출력 파일을 씁니다.
빌드·테스트가 실패하면 실행용 DLL을 교체하지 않습니다. 기본 입력과 출력이 같은 경우에는 빌드 중 입력 DLL이 바뀌지 않았는지도 확인합니다.

## 패드 입력 처리

조정 흐름은 다음과 같습니다.

1. 게임이 XInput 입력 경로를 사용하는지 확인합니다.
2. 주사위 입력창의 활성 상태, 게임 창 포커스, 설정값을 확인합니다.
3. 원래 패드 상태를 읽어 LB와 십자키·왼쪽 스틱을 수치 조정에 사용합니다.
4. 게임에 전달되는 상태에서는 조정용 버튼과 왼쪽 스틱 입력을 제외합니다.

A·X와 오른쪽 스틱·트리거는 기존 입력을 유지합니다. A·X 전용 처리나 키 입력 에뮬레이션은 없습니다.
LB를 먼저 놓아도 조정에 사용한 B·방향키는 해당 버튼을 놓을 때까지, 왼쪽 스틱은 중앙으로 돌아올 때까지 제외합니다.

입력 경로, 임곗값, 네이티브 라이브러리 수명 관리는 [패드 구현 상세](../development/src/DiceMaster.Gamepad/README.md)를 참고하세요.

### 수치의 의미

- `0`: 게임의 원래 주사위 굴림에 맡깁니다.
- `0`이 아닌 정수: 기존 DiceMaster 코드가 지정값을 주사위 결과에 반영합니다.
- 패드에서도 음수와 100을 넘는 값을 지정할 수 있습니다.
- 좌우 ±1, 상하 ±10을 그대로 적용합니다. 1에서 아래로 조정하면 −9입니다.
- 정수 오버플로를 막기 위해 32비트 부호 있는 정수 범위에서만 값이 증가·감소합니다.
- LB+B는 0으로 복귀하고, 조정 입력이 해제되거나 중앙으로 돌아올 때까지 재조정을 기다립니다.

이는 게임의 주사위 상한을 다시 계산해 제한하는 방식이 아닙니다. 원본의 숫자 입력·결과 적용 코드에도 게임 상한과 음수를 제한하는 처리가 없었습니다.

## 기존 기능의 구현 위치

다음 기능은 기존 DLL 안에 있습니다. 아래 이름은 DLL을 분석할 때의 탐색 기준입니다.

| 기능 | 주요 클래스·메서드 |
|---|---|
| 주사위 입력 생성 | `DicePatch.DiceMenuDialog_Awake_Postfix` |
| 주사위 창 초기화 | `DicePatch.DiceMenuDialog_OnEnable_Postfix` |
| 지정값을 판정에 반영 | `DicePatch.CheckPointManager_Dice_Prefix` |
| Lua 스크립트 분석 시작 | `OptionInfoPatch.LuaEnvironment_LoadLuaFunction_Prefix` |
| 선택지 결과 추출 | `LuaParser.ExtractOptionActionsFromLua`, `LuaParse.Action` |
| 선택지 옆 요약 표시 | `OptionInfoPatch.LeanLocalizationResolver_GetStoryText_Postfix` |
| 로그 창 선택지 표시 | `DicePlugin.AddOptionEntry`, `DicePlugin.DrawOptionEntries` |

선택지 요약은 번역·서식 처리를 거친 뒤 50자로 줄여 붙입니다. 로그 창에는 잘리기 전 설명을 별도로 저장합니다.
분석은 게임이 제공한 Lua를 고정된 규칙으로 읽는 방식입니다. 기존 형식의 선택지 추가는 자동으로 반영할 수 있지만, 새 함수·스크립트 형식·게임 API 변경은 별도 대응이 필요할 수 있습니다.

## 로그 정보 갱신 재설계

### v1.0.2의 확정 회귀

v1.0.2는 자동 갱신에서 네 탭의 표시를 한꺼번에 재구성합니다. 재구성 경로가 공유 분석 결과인 `OptionResult`를 비우면서 Lua 파싱 완료 캐시인 `luaOptions`는 유지하므로, 이미 분석한 Lua의 결과를 다시 채우지 못합니다. 게임 선택지 옆 요약도 같은 분석 데이터를 사용하므로 영향 범위에 포함됩니다. 수동 새로고침 직후 결과가 보이다가 주기 갱신 후 사라질 수 있습니다.

기존 검사는 갱신 래퍼 일부를 실행했지만 실제 재구성 본체와 파서 연결을 단순한 테스트 대역으로 대체했습니다. 과거 검사 통과는 이 캐시 조합을 검증했다는 근거가 아닙니다. 한 항목의 내부 실패가 전체 성공이나 빈 목록으로 처리되는 경로, 전체 탭 재구성과 할당 비용은 재설계에서 확인할 구조적 위험이며 모두 새로 발생한 게임 회귀로 단정하지 않습니다.

### 데이터 소유권과 표시 계약

공유 선택지 분석은 `LuaParser.Instance`를 사용하는 실제 Lua 경계가 소유합니다. 이벤트 고정 설명은 `LogBridge.GetEventParser` → `PositionEventCache.GetDefinitionParser`로 얻는 별도 파서 인스턴스에 맡겨, 콜드·지연 준비 이벤트 스크립트 분석도 공유 선택지 캐시를 덮어쓰지 않게 합니다.

`RefreshEngine`은 현재 탭과 항목별 값·오류·재시도 시각을 관리합니다. `LogBridge`는 기존 모드의 파서·캐시·표시 경계에 연결하며, 통합 도구가 이 소스를 기존 DLL에 넣습니다. 이벤트의 고정 설명은 고정 분석 경계에서 준비하고, 늦게 준비되거나 실패한 정의는 준비 시 한 번 분석할 수 있습니다. 정상 웜 캐시 주기에는 이를 다시 분석하지 않습니다.

전달받은 실제 게임 DLL에서 확인한 `SaveSystem`의 새 게임·공개 로드·공통 로드 6개 메서드와 private `SceneController.StartLoadScene`의 Prefix에서 문맥을 무효화합니다. Prefix/Postfix/Finalizer는 `LifecycleState`의 호출 토큰으로 중첩·중복·오래된 콜백을 구분합니다. void 로드 반환과 씬 코루틴 생성은 준비 완료로 처리하지 않습니다.

Lua Prefix는 원문을 임시 보관하고 이전 문맥을 지우며, `LoadLuaFunction`의 non-null 결과 Postfix에서만 새 분석을 활성화합니다. null·예외·누락 스크립트·전환 중 완료는 계속 차단합니다. 억제 중 자동·수동·탭 전환은 이전 원문 재분석이나 현재값 조회를 하지 않습니다. 제자리 복원이 새 Lua를 만들지 않는 경우 다음 실제 성공 Lua까지 대기하는 것이 현재 안전 정책입니다. 이 연결의 실제 Harmony·UI 실행 검증은 남아 있습니다.

이 절은 이슈 #1의 구현 계약입니다. 실제 구현·자동 검사·게임 확인 상태는 [검증 기록](ISSUE-1-VALIDATION.md)을 따릅니다.

| 데이터 | 소유권과 갱신 원칙 |
|---|---|
| 고정 Lua 분석 결과·파싱 완료 캐시 | Lua 분석 경로가 소유합니다. 자동 표시는 읽기만 하며, 결과와 완료 표시를 함께 유지합니다. 정상 주기 갱신 중 재분석하지 않습니다. |
| 현재값·조건 판정·확률·표시 문자열 | 표시 경로가 소유합니다. 열린 현재 탭의 정상 행만 0.25초 간격으로 다시 평가합니다. |
| 마지막 정상 값·실패 원인·다음 재시도 시각 | 항목별로 보관합니다. 한 항목의 오류가 다른 행이나 다른 탭의 최신 값을 되돌리지 않습니다. |
| 세션·Lua 문맥 | 새 Lua·세이브 로드·새 게임에서 교체합니다. 이전 문맥의 행·실패 상태를 새 정보와 섞지 않습니다. 확인된 전환 Prefix에서 차단하고 성공 Lua 컴파일 뒤 새 세대로 교체합니다. 실제 게임 실행에서의 적용은 미검증입니다. |

`SetFlag`·`AddFlag`의 값은 예정된 변경입니다. 표시용 현재 플래그를 읽더라도 이 값을 현재값으로 덮어쓰지 않습니다. 기존 네 탭과 선택지 옆 50자 요약·로그의 전체 설명 계약을 유지합니다.

### 스케줄과 오류 상태

새 설정은 추가하지 않습니다. `EnableOptionInfo`는 기본 `true`이며 변경 후 게임 재시작이라는 기존 계약을 유지합니다. `false`로 시작하면 관련 정보 표시와 자동 갱신을 끄며 주사위·패드 설정은 유지합니다.

| 상황 | 표시용 처리 |
|---|---|
| 창 닫힘 | 현재값 조회·자동 재시도 0회. 게임 Lua 로딩에 따른 고정 분석과 게임 선택지 옆 요약은 계속 유지합니다. |
| 현재 탭 보임 | 정상 행은 0.25초 간격 평가. 보이지 않는 탭은 주기 평가 0회입니다. |
| 창 재열기·탭 전환 | 정상 행은 즉시 평가. 실패 행의 기존 5초 대기 시각은 보존합니다. |
| 항목 조회 실패 | 해당 행만 실패로 전환합니다. 다른 행은 계속 갱신하며 같은 탭이 보일 때 5초 간격 재시도합니다. |
| 게임 데이터 미준비 | `게임 데이터 대기 중`을 표시합니다. 정상 빈 목록과 구분합니다. |
| 첫 조회 실패 | `읽기 실패`를 표시합니다. |
| 이전 정상 값이 있는 행의 실패 | 마지막 정상 값과 `갱신 실패 · 이전 값`을 표시합니다. 복구하면 새 값으로 교체하고 실패 문구를 제거합니다. |
| 게임 데이터 새로고침 | 관련 고정 분석 캐시를 모두 다시 만들고 현재 탭을 즉시 표시합니다. 실패 대기를 건너뛰며, 다른 탭은 열 때 새 분석을 사용합니다. |

내부 조회·파싱 함수가 예외를 잡아 정상 반환하더라도 성공 또는 정상 빈 결과로 간주하지 않아야 합니다. 색상만으로 상태를 구분하지 않습니다.

### 표시의 부작용과 진단

자동·수동 표시 조회는 게임 상태를 바꾸거나 이벤트·미션 실행 예약을 소비하거나 대화 기록을 반복 추가해서는 안 됩니다. 표시 보호 깊이는 `finally`에서 복원하고, 예외 이후 실제 실행 경로도 계속 동작해야 합니다.

전달받은 실제 게임 DLL에서 `StatValueReference.GetValue`의 전이 호출이 `UnityEngine.Random.Range`에 진입하고 `ConditionResultData.Execute`가 `_count`를 증가시킴을 확인했습니다. `DisplayRefreshDepth`만으로는 이 원본 게임 부작용을 막을 수 없습니다. [실제 게임 API 분석](REAL-GAME-API-FINDINGS.md)에 정확한 서명·IL 근거·17개 값 종류·native 경계를 기록했습니다.

alpha.1의 `SafeDisplayEvaluation`은 알려진 필드와 읽기 전용 getter로 확률·그룹·조건을 별도 계산하며 난수는 `RandomDisplayValueException`으로 거부해 행 실패/이전 값에 연결했습니다. alpha.2는 이 정책을 범위·판정 미정·전용 미지원 결과 모델과 formatter로 분리했습니다. 샘플링·임의 값 생성 없이 제한된 단일 비교에만 이론 확률을 제공하며 실제 조회 실패는 기존 오류 경로에 남깁니다. 알 수 없는 enum·미지원 변환 하위 타입·순환 자료도 거부합니다. `GameStat`은 확인된 변환 타입만 허용하며 native `AnimationCurve.Evaluate`와 실제 리소스는 정적 분석을 넘어 실행 검증하지 않았습니다.

범위·판정 미정·제한적인 확률의 구현 계약과 검사 기준은 [난수 미리보기 후속 구현](RANDOM-PREVIEW-FOLLOWUP.md)을 따릅니다. 최종 alpha.2 관리 코드·통합·반복 패치·직접 패드 결과와 해시는 [검증 기록](ISSUE-1-VALIDATION.md)에 기록했습니다. 실제 게임·Windows 검증과 전체 완료는 별개입니다.

실제 `ConditionResultItem.Check`의 null 목록은 잘못된 자료로 실패하고, 유효한 빈 목록은 false입니다. 후보는 이 차이를 유지하며 기존의 경고·빈 결과로 실패가 숨겨지는 경로를 순수 평가로 바꿉니다. 알려진 모드 설명 호출은 내러티브 처리 중 보호 깊이 0인 경우에도 순수 평가에 연결합니다. 원본 게임 어셈블리와 실제 게임 진행·명시적 실행 코드는 교체하지 않습니다.

진단은 기존 BepInEx 로그를 사용합니다. 같은 게임 세션·항목·원인의 연속 오류는 최초 1회, 복구는 1회 기록합니다. 탭·항목 식별자와 예외 정보는 남기고 전체 Lua 원문과 세이브 내용은 기록하지 않습니다. 실패 원인은 예외 타입과 최초 발생 메서드·IL 위치를 기준으로 식별합니다. 캐시한 예외를 `ExceptionDispatchInfo`로 반복 전달하며 스택 전달 구간이 늘어나도 같은 오류 로그를 반복하지 않도록 검사합니다. 진단에는 스택을 남기지만 예외 `Message`·`ToString` 전체를 기록하지 않습니다. 최초 발생 위치가 바뀌거나 새 문맥이 시작되는 경우의 식별과 복구 로그도 자동 검사 대상입니다.

0.25초는 재계산 빈도를 제한하는 기존 선택값이며 실측으로 확정한 성능 기준이 아닙니다. 이번 작업에는 고정 수치 상한을 추가하지 않습니다. 구조적 기준과 시간·할당량 측정 절차는 [검증 기록의 성능 측정](ISSUE-1-VALIDATION.md#성능-측정)에 적습니다.

로그 패치만 재생성하려면 다음 명령을 실행합니다. 패드 빌드 도구와 같은 `--input`, `--output` 옵션을 지원합니다.

```sh
python3 development/tools/build-log-refresh.py \
  --game-managed '<읽기 전용 참조>/Mortal_Data/Managed' \
  --output development/releases/local/DiceMaster.dll
```

`--game-managed`는 읽기 전용 게임 DLL에서 실제 IL과 후킹 계약을 검사합니다. macOS의 .NET 검사로 native·Unity 경계를 대역에 옮겨 실행하며 게임을 실행하지 않습니다.

`build-log-refresh.py`는 출력 전 패치를 다시 적용한 별도 후보에도 실제 파서 통합 검사와 DLL 구조 검사를 실행합니다. payload 교체 전에 메서드 본문을 읽고 이전 후킹 참조를 새 참조에 연결하며, 해결되지 않은 호출이 있으면 출력하지 않습니다.

자동 검사를 통과한 뒤 기본 출력으로 빌드하면 저장소 실행용 DLL을 갱신합니다. 패드 빌드와 로그 패치는 서로의 변경을 유지해야 하며, 반복 적용·두 도구의 교차 실행 후에도 갱신 호출과 패드 호출이 중복되지 않아야 합니다.

## 검증

### 패드 빌드 검사

테스트는 패드 지원 어셈블리와 통합한 DiceMaster DLL에 각각 실행됩니다.

- LB가 없는 경우와 주사위 창 밖에서 수치가 유지되는지
- 양수·음수·0을 오가는 조정과 정수 경계 처리
- LB+B 복귀, 반복 입력과 방향 변경
- 스틱 임곗값, 대각선 방향, 십자키 우선 처리
- 전체 디지털 버튼 조합과 조정용 입력 차단
- A·X, 오른쪽 스틱, 트리거 보존
- 버튼 해제 순서, 스틱 중앙 복귀, 연결 해제 시 상태 초기화
- XInput 구조체 크기와 비지원 OS의 초기화 실패 처리

이 검사는 Windows 네이티브 후킹과 실제 게임 화면을 실행하는 검사는 아닙니다.

### 로그 갱신 검사

`build-log-refresh.py`는 패치한 DLL의 실제 메서드 본문을 검사에 사용하며 게임·Unity 경계는 필요한 경우 모의 구현합니다. 이슈 #1 회귀 검사는 **실제 Lua 파서 → 공유 분석 캐시 → 실제 표시 갱신 → 게임 선택지 옆 요약**을 연결해야 합니다. 파서나 갱신 본체를 단순한 행 추가 함수로 대체하면 이 회귀를 검증했다고 기록하지 않습니다.

- 콜드·웜 캐시 연속 자동 갱신 및 수동→자동 갱신 후 선택지 결과 유지
- 현재 조건값·판정 색상·확률·상태 의존 설명 갱신, `SetFlag`·`AddFlag` 예정값 보존
- 네 탭, 창 숨김·재열기·탭 전환, 설정 비활성화와 타이머·재진입 방지
- 첫 행·중간 행·내부 함수 실패 격리, 정상 빈 목록과 데이터 미준비 구분
- 마지막 정상 값·세 상태 문구·5초 재시도·전환 시 대기 유지·수동 즉시 재시도·복구
- 실제 전환·컴파일 토큰으로 같은 manager·같은 Lua·로드 실패·null 결과·오래된 콜백에서도 이전 행·실패 상태 제거
- 실제 게임 IL·순수 평가의 17개 값 종류·RNG 비소비·조건 카운터 보존·null/빈 목록·미지원 자료 구분
- 이벤트·미션 예약과 대화 로그 보존, 예외 후 보호 깊이 복구
- 오류 최초 1회·복구 1회 로그와 민감한 전체 원문 비기록
- 숨김 조회·비활성 탭 주기 평가·정상 주기 Lua 재분석 각각 0회
- 실제 `Update`의 Tab 토글·일반 경로·화면 크기 변경에서 갱신과 패드 호출 유지
- 반복 패치·패드 재빌드의 중복 방지와 무관한 메서드·내장 리소스 보존

검사별 실제 실행 결과, 알려진 대역과 미확인 경로는 [검증 기록](ISSUE-1-VALIDATION.md)에 남깁니다. 이 목록 자체는 검사 통과 기록이 아닙니다.

### 기록된 검증 범위

v1.0.1 DLL에는 별도 비교로 다음을 확인했습니다.

- 직전 배포 DLL과 비교해 변경된 메서드는 패드 수치 조정 함수 1개
- 나머지 344개 메서드와 8개 내장 리소스 보존
- 입력 동작 및 정수 경계 검사 통과

과거 v1.0.2 DLL에는 로그 관련 자동 검사 45개와 패드 검사 525,430개를 통과한 기록이 있습니다. 이슈 #1에서 실제 파서와 재구성 본체를 연결하지 않은 기존 로그 검사의 한계를 확인했으므로, 이 기록은 선택지 소실 회귀 해결이나 이슈 #1 완료 근거로 사용하지 않습니다. 별도 구조 비교로 기존 메서드 중 로그 갱신과 관련한 13개만 변경되고 나머지 332개 및 내장 리소스 8개가 보존되었음을 확인했습니다. 로그 갱신용 메서드 4개를 추가했습니다.
로그 패치 반복 적용과 패드 재빌드 후의 동작 검사도 통과했습니다. 로그 검사의 게임·Unity 경계는 모의 구현이며 실제 Windows 게임에서의 재현 검증은 수행하지 않았습니다.

이 구조 비교는 매번 빌드 스크립트가 자동 실행하는 검사가 아닙니다. 기존 기능을 수정하는 작업에서는 관련 메서드와 번역·에셋·라이브러리 리소스가 의도대로 유지되는지도 따로 확인하세요.

### 실제 게임 확인

이슈 #1의 실제 게임 실행 확인은 향후 필수 완료 조건이며 현재 승인되지 않았습니다. 현재는 기존 Windows 파일·정보의 읽기와 macOS 개발 검사만 허용됩니다. 아래 연결은 향후 별도 승인 후 검토할 계획이며 실행 요청이 아닙니다. [Windows 검증표](ISSUE-1-VALIDATION.md#사용자-windows-검증)에 게임·모드 베이스·BepInEx 버전, DLL 식별값, 패드 환경과 결과를 사용자가 기록합니다. 실제 게임 API 내부 분석과 이 검증이 끝나기 전에는 기능 완료나 정식 Release 공개로 처리하지 않습니다. 승인된 prerelease 게시와 이 완료 조건은 구분합니다.

릴리스 설명에는 실제로 확인한 게임·모드 베이스 버전, 패드 환경, 검사한 동작을 적습니다.
게임 내 숫자 반영, 입력 중복, UI 배치, Steam Input 호환성은 향후 별도 승인된 실제 Windows 검증의 대상이며 현재 확인하지 않았습니다.
수신된 기존 파일·로그는 Steam appid `1859910` / buildid `26021712`, Unity `2020.3.49f1`, BepInEx `6.0.0-be.692`를 식별합니다. 이는 새 실행이나 호환성 통과 기록이 아닙니다.

## 원본과 사용 라이브러리

이 저장소의 기반은 기존 DiceMaster DLL 배포본입니다. 외부 원본 배포 URL과 원제작자 정보는 아직 확인되지 않아 임의로 기재하지 않았습니다.

| 항목 | 용도 | 버전·출처 |
|---|---|---|
| MinHook | Windows XInput 후킹 | [1.3.4](https://github.com/TsudaKageyu/minhook/releases/tag/v1.3.4) |
| Mono.Cecil | DLL 통합 도구 | [0.11.6](https://github.com/jbevain/cecil) |

MinHook의 x86/x64 DLL과 전체 라이선스는 `development/src/DiceMaster.Gamepad/native/`에 보관합니다.
실행용 DiceMaster DLL에도 라이브러리와 `DiceMaster.Gamepad.Native.LICENSE.txt` 리소스를 내장합니다.
원본 출처를 확인하면 이 절과 사용자 README의 출처 안내를 함께 갱신하세요.
