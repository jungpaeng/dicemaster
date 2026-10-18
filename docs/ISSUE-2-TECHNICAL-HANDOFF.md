# PRD 2 선택지 미리보기 구현 기술 인계

[PRD 2](https://github.com/jungpaeng/dicemaster/issues/2) · [시작 프롬프트](ISSUE-2-START-PROMPT.md) · [개발 안내](DEVELOPMENT.md) · [기존 검증](ISSUE-1-VALIDATION.md)

2026-10-18 KST 기준. 다음 세션은 확정된 PRD를 읽고 현재 alpha.2 코드를 기준으로 로컬 구현과 검사를 시작할 수 있다. 요구사항의 기준은 GitHub PRD 2이며, 이 문서는 분석 근거·코드 연결 지점·개발 환경을 전달한다. PRD 2의 제품 코드는 아직 구현하지 않았다.

## 시작 기준과 현재 상태

| 항목 | 확인한 값 |
| --- | --- |
| 저장소 | `https://github.com/jungpaeng/dicemaster` |
| 현재 HEAD | `6eb52e6b5cc7a3b3684d2ea2b7c1a555dcf60f00` |
| alpha.2 구현과 배포 태그 기준 | `015c80c2fd9f905aaece574b3b11da52e501d432` |
| 현재 `plugins/DiceMaster/DiceMaster.dll` | 462,848바이트, SHA-256 `b6f7ae790cde736dcf1a282e9242c46b8ab0204602c7c462d59c571f321740ec` |
| 작업 폴더 | `/Users/im-yongbeen/dev/jungpaeng/활협전 DiceMaster` |
| 인계문 작성 전 작업 트리 | 추적 파일 변경 없음. `.agents/`와 `skills-lock.json`은 기존 미추적 파일 |

위 값은 시작 확인용 기록이다. 새 세션에서 HEAD·작업 트리·입력 DLL을 다시 확인하고 달라진 내용을 먼저 대조한다. 기존 파일을 정리하거나 이전 커밋으로 되돌려 기록과 맞추지 않는다. 이 인계문과 시작 프롬프트도 새 문서이므로 아직 커밋되지 않았을 수 있다. 새 worktree를 쓰면 두 문서와 필요한 로컬 자료의 접근 가능 여부를 확인한다.

alpha.2의 로그·난수 관리 코드와 prerelease 게시·다운로드 검증은 끝났다. 실제 Lua·native Unity·Harmony·사용자 Windows 확인은 남아 있어 이슈 1의 기능 완료와 정식 Release 조건은 충족하지 않았다. PRD 2는 이 코드 위의 최우선 후속 작업이며 별도 날짜 기한은 없다. 기존 검증 한계와 기능을 보존하면서 가능한 로컬 구현을 진행한다.

## 확정한 요구사항

세부 함수 목록·수용 기준·검사 시나리오는 PRD 2를 따른다. 다시 질문해서 결정할 항목은 아니다.

- `StatModifyManager` 미지원 16개와 `center`·`mental`, 전투 기술·장비·시간·미션 시간의 `LuaManager` 12개, `SetNextScript`를 지원한다. 조회·화면 표시 전용 함수는 실제 상태 변경과 구분한다.
- 숫자 상수의 `+ - * /`, 괄호, 단항 부호를 지원한다. 변수·대입 추론·나머지·거듭제곱·동적 호출은 제외한다. 게임의 정수 인자로 안전하게 변환할 수 없는 결과는 미지원이다.
- 현재 상태와 실행 순서가 안전하게 해석되면 예상 변경량을 표시한다. 불확실하면 효과의 의미와 조건을 설명한다.
- 리터럴 대상의 후속 스크립트를 여러 단계 추적하되 다음 사용자 선택 전까지만 포함한다. `후속 스크립트`와 `조건부`를 표시하며 루프·동적 흐름을 확정 결과로 합치지 않는다.
- 추적 깊이·스크립트 수·원문량·처리 비용의 구체 제한은 실제 Lua와 비용 측정을 근거로 구현 담당자가 정하도록 위임됐다. 무제한 추적은 금지한다. 결정값·선정 근거·초과 동작·경계 검사를 문서화한다. 실제 리소스를 확보하기 전의 시험용 한도를 검증된 최종값으로 쓰지 않는다.
- 정상 효과를 유지하고 일부 미지원이면 `일부 결과 미리보기 미지원`, 전부 미지원이면 `결과 미리보기 미지원`, 효과 미발견이면 `확인된 효과 없음`을 표시한다. 미지원 안내는 50자 요약 앞에 두고 로그에는 전체 내용을 유지한다.
- 미지원은 캐시된 분석 상태이며 주기 재시도하지 않는다. 같은 세션·스크립트·선택지·원인은 최초 1회 진단한다. 실제 미준비·조회 실패에는 기존 대기·5초 재시도·수동 복구 정책을 적용한다.
- 기존 `EnableOptionInfo`, 설정·세이브 형식·네 탭·번역·주사위·패드를 유지한다. 새 기능 플래그·외부 통신·계정·별도 영구 저장·외부 지표 수집은 제외한다.
- 단일 PRD 이슈에서 자동 검사·실제 게임 DLL 의미·실제 Lua 대조·사용자 Windows 확인까지 완료 조건으로 관리한다. 배포 버전·일정은 구현 후 정하고 게시에는 별도 승인이 필요하다.

## 현재 alpha.2에서 확인한 근거

인계문 작성 시 현재 DLL을 새로 역컴파일했다. `DiceMaster.LuaParse.Action`과 `LuaParser`는 alpha.1 분석본과 같은 역컴파일 결과였다. 실제 alpha.2의 LuaParse IL을 경계 대역으로 복제해 실행한 결과도 다음과 같다.

| 입력 | 수집 | 기존 최종 설명 |
| --- | --- | --- |
| `SetPlayer("money", 100)` | 1개 | 빈 문자열 |
| `Player("money", 100, "", 1)` | 1개 | `money +100` |
| `Player("money", reward, "", 1)` | 1개 | 빈 문자열 |
| `Player("money", 50 + 50, "", 1)` | 1개 | 빈 문자열 |
| `Player("disposition", 10, "center", 0)` | 1개 | `disposition +10` |
| `SetBattleSkillLevel("skill", 2)`, `NextMonth()`, `SetNextScript("next")` | 각각 0개 | 빈 문자열 |
| `ChangeScene("Combat")`, `ChangeScene("Battle")` | 각각 1개 | 결투, 난전 |

`money` 등 영문 식별자는 번역 대역의 출력이다. 파서·Action·최종 설명 본체는 실제 DLL의 IL이며 게임·번역·RichText 경계는 대역이다. 이 실행은 게임 진행이나 실제 선택지 화면의 재현 검증이 아니다. 사용자가 발견한 특정 선택지의 Lua는 아직 확보하지 못했다.

새 역컴파일·실행 결과는 `/private/tmp/dicemaster-issue2-handoff-current/`에 있다. `baseline.json`, `DiceMaster.LuaParse.Action.cs`, `DiceMaster.LuaParse.LuaParser.cs`, `DiceMaster.OptionInfoPatch.cs`, `DiceMaster.DataHelper.cs`, `alpha2-runtime-results.txt`를 참고한다. 임시 폴더가 없어도 위 기준 DLL과 아래 원본 참조에서 재생성할 수 있다.

과거 `/private/tmp/dicemaster-effect-audit/working.dll`과 `working.*.cs`는 **alpha.1 시점의 사본**이다. 파일 이름의 working을 현재 코드라고 해석하거나 새 빌드의 입력으로 사용하지 않는다. `alpha1.dll` 역시 비교용 이력이다.

## 구현 연결 지점

이 저장소는 전체 DiceMaster 소스를 다시 빌드하는 프로젝트가 아니다. 기존 DLL에 관리 소스 payload와 IL 패치를 넣는다. 파서와 Action의 전체 원본 C#은 저장소에 없으므로 현재 DLL을 읽어 교체·연결 경계를 정해야 한다. 게임 DLL 전체나 역컴파일한 게임 소스를 저장소에 추가하지 않는다.

| 위치 | 역할과 구현 시 주의점 |
| --- | --- |
| DLL `DiceMaster.LuaParse.LuaParser` | `ExtractOptionActionsFromLua`, `BuildActionListFromBlock`, `AddActionsToList`, `Description`. 현재 수집·결과 캐시 경계 |
| DLL `DiceMaster.LuaParse.Action` | 생성자에서 `ParseAction()`을 호출해 Description을 한 번 만든다. `ParseModifyAction`, `GetActionDescription`, `GetDescription` 확인 |
| DLL `DiceMaster.OptionInfoPatch` | `LeanLocalizationResolver_GetStoryText_Postfix`가 게임 선택지 요약에 같은 분석 결과를 사용 |
| DLL `DiceMaster.DataHelper` | `LoadLuaScript`가 리소스를 읽지만 현재는 리소스 부재와 빈 스크립트를 모두 빈 문자열로 반환할 수 있음 |
| `development/src/DiceMaster.LiveLog/LogBridge.cs` | 실제 Lua 성공 경계, Analyze, Discover, 문맥과 표시 연결. 선택지 표시 때 `LuaParser.Description(actions)` 호출 |
| `development/src/DiceMaster.LiveLog/RefreshEngine.cs`, `LifecycleState.cs` | 항목별 갱신·실패와 세션/로드/Lua 토큰. 기존 계약 보존 |
| `development/src/DiceMaster.LiveLog/SafeDisplayEvaluation.cs`, `RandomPreview.cs`, `PreviewFormatting.cs` | 읽기 전용 평가와 alpha.2 난수 범위·판정 미정·미지원 모델. 새 효과 분석과 의미를 혼동하지 않도록 연결 |
| `development/tools/LogRefreshInjector/Program.cs`, `SafeEvaluationHooks.cs`, `LifecycleHooks.cs` | payload·설명·실제 lifecycle 후킹 생성 |
| `development/tools/Shared/PayloadCopier.cs` | 실제 IL·타입 복제. 현재 injector의 payload 선택은 `DiceMaster.LiveLog` 네임스페이스 기준 |
| `development/tests/LiveLogIntegrationTests/`, `LiveLogIntegrationFixtures/` | 실제 파서·Action·설명 경로를 경계 대역에 연결하는 검사 출발점 |
| `development/tests/LiveLogArtifactTests/Program.cs` | 교체 허용 메서드를 명시한 구조 보존 검사. 의도한 파서 변경에 맞춰 좁게 갱신하고 보존 검사를 유지 |
| `development/tests/RealGameApiTests/`, `RealGameApiFixtures/` | 실제 DLL IL과 메타데이터·순수 평가 계약 검사 |

### 고정 분석과 현재값 평가

기존 Action 생성자에 현재값 계산을 추가하는 것만으로는 자동 갱신 시 최신 예상량을 얻지 못한다. 고정 분석 결과와 표시 시점의 읽기 전용 평가를 분리하고 게임 선택지 요약과 로그가 같은 의미를 사용하도록 연결한다. `DisplayRefreshDepth`가 0인 내러티브 설명 호출에서도 원래 실행 API를 호출하지 않아야 한다.

공유 `LuaParser.Instance.OptionResult`를 후속 스크립트 분석의 임시 저장소로 사용하면 현재 선택지 결과를 덮어쓸 수 있다. 기존 이벤트용 별도 파서와 새 게임·로드·씬·성공 Lua의 문맥 계약을 보존하고 후속 분석의 출처·제한·실패도 같은 문맥에서 관리한다. 정상 주기 갱신은 Lua를 재분석하거나 고정 결과를 지우지 않는다.

### 구문 경계와 실제 실행 의미

기존 정규식은 첫 닫는 괄호에서 끊고 인자를 쉼표로 분할한다. 상수 산술 평가만 덧붙이면 괄호·문자열·주석·여러 줄·분기 문제는 남는다. 이 경계를 올바르게 처리하는 분석 계층과 구조화된 상태가 필요하다. 구체 클래스 이름과 구현 방식은 고정하지 않았다.

현재 수집은 모든 stat 효과를 먼저 넣고 전투 호출을 나중에 넣을 수 있어 원래 명령 순서를 잃는다. 분기·조기 반환·알 수 없는 호출을 처리할 때 확정 가능한 경로와 조건부/미지원 경계를 함께 보존한다.

`SetNextScript`는 대상 이름 등록이며 즉시 실행이 아니다. 이후 지정이나 Story/Free 씬 전환 중 미션 평가가 대상을 교체할 수 있다. 대상 스크립트를 읽었다는 이유만으로 즉시 확정 효과로 합치지 않는다.

## 실제 게임 API의 중요한 의미

메서드 이름보다 확인한 게임 DLL 본문과 하위 호출을 기준으로 설명한다.

- `center`: 기준값 50과 현재 `FinalValue` 차이를 사용한다. 인자 10에 FinalValue 40/48/60/50이면 계산 변경량 +10/+2/−10/0. 실제 가산에는 능력치 제한·부가 값이 별도로 작용한다.
- `mental`: 심리위생 FinalValue가 20 이하면 변경량에서 1을 뺀다.
- `UpdateSetPlayerStat`: 변경 알림만 표시하며 실제 스탯 설정이 아니다. `GetModifyString`은 표시 보조 함수다.
- `SetPlayerBattleSkill`: 현재 DLL의 index 인자를 사용하지 않고 첫 빈 슬롯에 배치한다. `SetBattleSkillActive`는 active가 1이면 활성화하고 그 외 값이면 비활성화한다.
- `ResetBattleSkill`: 슬롯·활성 상태 초기화와 조건 재평가를 포함한다. 원본 실행은 조건 카운터를 바꿀 수 있으므로 설명 생성에서 호출하지 않는다.
- `SwapUpgradeItem`: 음수 level은 교체 대상 장비의 기존 레벨을 재설정하지 않는다. 0 이상이면 유효 레벨 범위로 제한한다. `RestoreUpgradeItem`은 지정 키에 해당하는 장비 종류의 백업 복원이다.
- `NextMonth`·`NextRound`: 날짜 외 행동 횟수·플래그·상점 갱신 등 하위 효과도 있다. 확인하지 못한 최종 수치를 추정하지 않는다.
- `SetMissionTime`: 현재 DLL에서 year 인자를 사용하지 않는다. 현재 게임 시각을 복사한 뒤 양수 month·stage만큼 상대 진행시키므로 절대 날짜 설정으로 설명하지 않는다.

## 원본 참조와 로컬 도구

읽기 전용 원본 루트는 다음과 같다.

```text
/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018
```

관리 DLL은 `Mortal_Data/Managed`, 로더·모드 참조는 `BepInEx/core`와 `BepInEx/plugins` 아래다. 이 원본을 수정하거나 개발 후보로 덮어쓰지 않는다.

| 참조 DLL | SHA-256 |
| --- | --- |
| `Mortal.Story.dll` | `ee9139a5e4631dc275b80cc3ff6a81a4959f3c908a3fe751e778a42350372249` |
| `Mortal.Core.dll` | `0c20ee36782666f3ab70ec8889068bdf230acc2d630446ea1db551836866e8fa` |

현재 `Mortal_Data`에는 Managed만 있고 실제 Lua 리소스·게임 EXE·native UnityPlayer·세이브는 없다. 자료가 필요한 실제 연결 대조·최종 추적 한도 검증은 남겨 두고, 합성 Lua·자료 제공 대역으로 진행할 수 있는 파서·상태·통합 검사는 계속한다.

| 임시 위치 | 내용 |
| --- | --- |
| `/private/tmp/dicemaster-dotnet/dotnet` | .NET SDK 8.0.426 |
| `/private/tmp/dicemaster-tools/ilspycmd` | ILSpy 역컴파일 도구 |
| `/private/tmp/dicemaster-nuget` | Mono.Cecil·.NET Framework 참조 등의 NuGet 캐시 |
| `/private/tmp/dicemaster-real-api/lifecycle/` | 게임 타입 역컴파일 자료. Mortal.Story 및 Mortal.Core 하위 타입 확인 |
| `/private/tmp/dicemaster-effect-audit-repro/` | 기존 실제 파서 IL 실험의 runner·fixtures 소스 및 alpha.1 실행 결과 |
| `/private/tmp/dicemaster-issue2-handoff-current/` | alpha.2 입력 스냅샷·새 역컴파일·동일 실험 실행 결과 |
| `/private/tmp/dicemaster-prd-choice-results/decision-ledger.json` | 사용자 결정 근거. 요구사항 기준은 공개 PRD |

임시 폴더는 재부팅·정리 후 사라질 수 있다. 도구·원본 접근을 먼저 확인한다. 임시 자료가 없으면 저장소와 원본 DLL에서 필요한 부분을 다시 만들고, 기존 실험 코드나 검사 수치의 존재만으로 새 구현이 통과했다고 기록하지 않는다. 공개 PRD에 접근할 수 없을 때만 `/private/tmp/dicemaster-prd-choice-results/issue-body.txt`를 등록 시점 스냅샷으로 참고한다.

## 첫 구현 순서

아래 순서는 기술적인 진행 제안이며 확정된 제품 범위를 줄이지 않는다.

1. PRD 2, 이 인계문, `DEVELOPMENT.md`, `REAL-GAME-API-FINDINGS.md`, `GAME-LIFECYCLE-ANALYSIS.md`, `RANDOM-PREVIEW-FOLLOWUP.md`, `RELEASING.md`를 읽고 현재 상태를 확인한다. 현재 DLL의 파서와 설명 경계를 다시 읽는다.
2. 기존 alpha.2 기준의 실제 파서 통합 검사를 확인하고 SetPlayer·상수식·center·미수집 명령의 실패 사례를 재현 가능한 회귀 검사로 만든다. 기대 문자열을 직접 반환하는 가짜 파서로 실제 본체를 대체하지 않는다.
3. 주석·문자열·인자·괄호·분기·순서 경계와 구조화된 정상/부분/미지원/효과 미발견/오류 상태를 만든다. 순수 조회·표시 전용 호출을 구분한다.
4. 16개·12개 명령의 의미와 상수식, 안전한 현재값 예상과 의미/조건 fallback을 구현해 실제 선택지 요약과 로그에 연결한다. 관련 injector와 구조 보존 검사도 함께 갱신한다.
5. 교체 가능한 로컬 스크립트 읽기 경계에서 후속 추적·다음 선택지 종료·순환·부분 결과·제한을 검사한다. 실리소스가 없다는 이유로 나머지 개발을 중단하지 않고, 실제 대조·최종 측정에 필요한 의존성은 명확하게 남긴다.
6. 기존 로그·난수·수명주기·캐시·부작용·패드 계약과 반복/교차 패치를 검사한다. 후보 DLL과 소스·도구·문서의 일치를 기록한다.

## 빌드와 검증 출발점

다음 명령은 저장소 또는 작업용 worktree 루트에서 실행한다. 현재 기본 PATH에는 dotnet이 없어 임시 도구를 먼저 설정해야 한다. 경로가 사라졌다면 기존 개발 문서에 맞춰 도구를 준비한다.

```sh
export DOTNET_ROOT=/private/tmp/dicemaster-dotnet
export PATH="$DOTNET_ROOT:/private/tmp/dicemaster-tools:$PATH"
export NUGET_PACKAGES=/private/tmp/dicemaster-nuget
dotnet --list-sdks
```

첫 로그 빌드에서는 입력과 출력을 명시해 기존 플러그인을 보존한다. 아래 명령은 현재 빌드 스크립트의 사용 예이며, 인계문 작성 과정에서 전체 빌드 파이프라인을 새로 실행한 결과가 아니다.

```sh
python3 development/tools/build-log-refresh.py \
  --input plugins/DiceMaster/DiceMaster.dll \
  --output development/releases/local/issue-2/DiceMaster.baseline.dll \
  --game-managed '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018/Mortal_Data/Managed'
```

`--output`을 생략하면 기본값이 추적 중인 플러그인 경로다. 탐색·중간 검사에서는 별도 후보 경로를 사용한다. 위 스크립트는 엔진·수명주기·실제 파서/표시 통합·구조 보존·실제 게임 IL 계약과 반복 패치 검사를 실행한다. .NET Framework 4.8 참조와 Mono.Cecil 0.11.6 등은 NuGet 캐시 또는 복원 접근이 필요하다.

PRD 2에서 파서 교체 범위가 넓어지면 injector와 `LiveLogArtifactTests`의 허용 메서드 계약도 의도한 범위로 갱신해야 한다. 검사를 끄거나 무관한 변경을 모두 허용해서 통과시키지 않는다. 패드 빌드의 실제 검사와 패드→로그 교차 빌드도 기존 계약대로 확인한다.

현재 DLL 재분석 예시는 다음과 같다. 출력은 생성 자료로 관리한다.

```sh
ilspycmd --disable-updatecheck \
  -t DiceMaster.LuaParse.LuaParser \
  plugins/DiceMaster/DiceMaster.dll
ilspycmd --disable-updatecheck \
  -t DiceMaster.LuaParse.Action \
  plugins/DiceMaster/DiceMaster.dll
```

## 실행 경계와 완료 보고

새 세션의 시작 요청은 PRD 2의 로컬 구현·검사·관련 문서 갱신을 위한 것이다. 현재 Windows PC는 기존 파일·정보 읽기만 허용된다. 후보 설치·게임 실행·오류 주입·런타임 검증은 수행하거나 요청하지 않는다. 실제 게임 진행용 API·RNG·카운터·예약·대화·세이브를 표시 목적으로 변경하지 않는다.

이번 인계문 작성과 PRD 등록은 commit·push·태그·Release 게시·기존 이슈 종료 승인을 포함하지 않는다. 별도 사용자 지시가 없다면 검토 가능한 로컬 변경으로 남기며 필요한 승인 시점에는 검토할 결과를 먼저 준비한다. 과거 alpha.2 게시 승인을 PRD 2에 전용하지 않는다.

완료 보고에서는 구현한 경로·변경 이유·실행한 검사·입력/후보 DLL 식별값을 기록하고, 실제 Lua 대조·최종 추적 예산·native/Harmony·Windows·실게임 성능의 미확인 항목을 구분한다. 로컬 구현과 자동 검사를 마쳐도 PRD 전체 조건이 남아 있으면 기능 완료·이슈 완료·정식 Release로 처리하지 않는다.
