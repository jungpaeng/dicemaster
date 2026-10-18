# 로그 갱신 게임 API 분석 기록

[개발 안내](DEVELOPMENT.md) · [실제 조회 API 근거](REAL-GAME-API-FINDINGS.md) · [게임 수명주기 근거](GAME-LIFECYCLE-ANALYSIS.md) · [검증 결과](ISSUE-1-VALIDATION.md) · [요구사항 #1](https://github.com/jungpaeng/dicemaster/issues/1)
게시된 `v1.0.3-alpha.1`·`v1.0.3-alpha.2`·`v1.0.3-alpha.3` prerelease는 기능·이슈 완료/정식 Release 판정과 구분합니다. Windows 게임 실행·설치 검증은 계속 미승인·미실행입니다.

사용자가 승인한 alpha.3은 모드 전용 `System.Random`으로 난수 숫자를 갱신하고 UI에 부가 난수 문구를 붙이지 않습니다. 개별 `ConditionResultItem` 설명 또는 전체 이벤트 설명의 숫자·색상·확률은 같은 세션의 결과를 재사용하지만 실제 게임의 판정을 예측하지 않습니다. alpha.3의 최종 macOS 관리 동작 3,914개·메타데이터 1,011개·통합 134개를 최초/반복에서 통과했고 패드→로그 왕복·반복 구조·직접 패드·로컬 ZIP도 검증했습니다. 원본 참조 159개의 크기·SHA-256은 유지됐습니다. 2026-10-18 22:43:45 KST에 [v1.0.3-alpha.3 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.3)를 배포 태그 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`에서 게시했습니다. 익명 공개 API·다운로드·CRC·정확히 두 파일·각 파일의 배포 커밋 바이트·ZIP/DLL SHA-256 일치를 검증했습니다. 파일 해시·범위·게시 영수증은 [검증 기록](ISSUE-1-VALIDATION.md)에 별도로 남깁니다.

기록일: 2026-10-18. **실제 관리 DLL의 조회 API와 새 게임·로드 경계 정적 분석을 수행했습니다. 실제 게임 런타임·사용자 Windows 검증·게임 성능 측정은 미실행입니다.** 자동 검사와 정적 분석으로 이슈 완료나 정식 Release 공개 조건을 충족했다고 처리하지 않습니다. Windows는 파일·정보 읽기만 승인되었으며 게임 실행을 수행하거나 요청하지 않았습니다.

## 수신한 실제 자료와 식별

원본 참조 루트는 다음과 같습니다.

```text
/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018
```

`Mortal_Data/Managed`의 131 DLL, `BepInEx/core`의 21 DLL, `BepInEx/plugins`의 6 DLL과 DiceMaster 설정을 읽었습니다. 게임 리소스·EXE·native UnityPlayer·세이브·실제 로그 본문은 포함되지 않습니다. 보고서에는 기존 로그·속성에서 확인한 Unity `2020.3.49f1`, BepInEx `6.0.0-be.692`, DiceMaster `1.0.0`, Steam appid `1859910` / buildid `26021712`가 기록되어 있습니다. legacy 로더 DLL이 공존하므로 파일 존재만으로 활성 버전을 추정하지 않습니다.

원본 ZIP은 11,500,849바이트, SHA-256 `42F946C26956B61FFE4ACA4C3391BDF0D9EF343FDA6B7B242AA617E9E46BC5CC`입니다. 참조 루트의 형제 파일 `verification-20261018.json`에 CRC·경로·가독성·CSV/JSON 일치와 159개 DLL/설정의 크기·SHA-256 검증이 기록되어 있습니다. 입력 보고서의 과거 전송 미확인 문구는 이 Mac 수신 검증으로 갱신된 상태입니다.

| 대상 | SHA-256 | 역할 |
|---|---|---|
| 실제 `Mortal.Core.dll` | `0c20ee36782666f3ab70ec8889068bdf230acc2d630446ea1db551836866e8fa` | 조건·수치·시간·저장·새 게임 본문 |
| 실제 `Mortal.Story.dll` | `ee9139a5e4631dc275b80cc3ff6a81a4959f3c908a3fe751e778a42350372249` | 이벤트 확률·스토리 Lua 경계 |
| 실제 `Fungus.dll` | `bcb6d47eb31dedc5d23d0da281e0835af0f939f30b64a39aae7d5f6dab96cad2` | Lua 컴파일 성공·실패 반환 |
| 설치된 DiceMaster `1.0.0` | `62ade4b719381fb31bfa7a356ae004299d391f53f13a4f085dc1a39e62cf5179` | 수정 전 기준 DLL과 동일한 분석·비교 사본 |
| 설치된 DebugMenu | `ed035ebfe6cbdd566e070def647ac72b37e33c308bed20b1bdf9f056b15e938d` | 별도 모드 비교 자료 |

설치된 DiceMaster 사본을 개발 후보로 복사하지 않았습니다. 실제 DLL 확보 전의 개발 후보 `56d85cd000cf6bf065237bcf032a1c6e18d2e739a09ead59e9fac2860b24206f`와 이후 새 후보도 별개입니다. 최종 빌드 후보의 크기·해시는 [검증 기록](ISSUE-1-VALIDATION.md)의 해당 실행 결과로 식별합니다. 원본 참조는 수정하지 않습니다.

분석 도구는 ILSpy `9.1.0.7988`이며 임시 출력은 `/private/tmp/dicemaster-real-api`에만 생성했습니다. 상세 문서는 어셈블리 identity, metadata token, 메서드 서명·IL 오프셋과 재현 명령을 포함합니다. 게임을 실행하지 않았습니다.

## 실제 조회 API에서 확인한 문제

`PositionEventData.GetRate()` 자체는 기본 확률·조건·가산 확률을 읽지만, 조건의 하위 호출까지 순수하지는 않습니다.

```text
PositionEventData.GetRate
  → ConditionResultItem.Check
    → StatCompareItem.Check
      → StatGroupVariable.GetResult
        → StatValueReference.GetValue
          → Random.Range                 [隨機值]
          → 다른 StatGroupVariable       [數值群組]
          → ConditionResultData.Execute  [條件設定]
```

- 난수 분기는 Unity 공유 RNG의 native 진입점을 호출합니다. 정확한 native 상태 진행 횟수는 실행 측정하지 않았습니다.
- `ConditionResultData.Execute()`는 먼저 `_count++`를 수행합니다. 난수 상태 복원만으로 보호할 수 없는 명시적인 게임 객체 쓰기입니다.
- `ExtractConditions`는 표시 숫자에 양쪽 `GetResult()`를 호출한 뒤 색상 판정에 `Check()`를 다시 호출합니다. 시간 조건에는 추가 `Check()`도 있습니다. 원본 API를 사용하면 평가·부작용이 반복됩니다.
- `ConditionResultItem.Check()`는 비교 목록을 전부 평가한 뒤 AND/OR를 적용합니다. `ConditionResultData.Execute()`는 첫 참 조건에서 반환합니다.
- `ConditionResultItem._items == null`은 오류입니다. 유효한 빈 목록은 false입니다. null을 조용히 빈 설명이나 기간 밖으로 바꾸면 실패를 숨깁니다.

17개 변수 종류와 읽기 getter의 하위 호출은 [실제 조회 API 근거](REAL-GAME-API-FINDINGS.md)에 기록했습니다. `GameStat.FinalValue`의 제공된 변환 타입은 수치·곡선 계산과 읽기만 수행하지만 다른 stat을 재귀 조회할 수 있습니다. 재귀·미지원 변환을 검사해야 하며 `AnimationCurve.Evaluate` 등의 native 본문은 본 관리 DLL 분석으로 실행 검증한 것이 아닙니다.

## 개발 소스의 표시 평가 경계

`development/src/DiceMaster.LiveLog/SafeDisplayEvaluation.cs`는 확인한 필드와 읽기 getter에서 값을 계산합니다. 게임의 `GetValue/GetResult/Check/Execute` 사슬을 표시를 위해 호출하지 않습니다.

- 그룹은 eager 평가 후 checked Sum, Max/Min, unchecked Multiply를 계산합니다. 빈 그룹·null·알 수 없는 enum을 구분합니다.
- 비교·조건을 평가하고 조건 설정은 `_count`를 건드리지 않고 목록을 OR 평가합니다. 재귀 경로와 깊이를 검사합니다.
- alpha.1의 설명 경로는 난수를 `RandomDisplayValueException`으로 실패시켰고 alpha.2는 범위/판정 미정/미지원·제한적인 단일 비교 이론 N/D를 표시했습니다. alpha.3은 잠금으로 보호한 모드 전용 단일 `System.Random`을 사용하며 개별 `ConditionResultItem` 설명 또는 전체 이벤트 설명(헤더·활성 조건·모든 확률 항목 조건)의 세션에서 동일 객체의 값·그룹·비교·조건 평가를 재사용합니다. 미션·스위치 전체 행의 서로 다른 조건 항목은 각각 별도 세션이므로 공유 참조도 다시 추첨할 수 있습니다. 중첩/다중 난수·Sum/Max/Min/Multiply·조건 설정·숫자 이벤트 확률을 다룹니다. 게임 RNG와 `_count`를 변경하지 않고 부가 UI 문구도 붙이지 않습니다. 전체 int 양끝 포함·역순 구간 정규화는 모드 화면의 규칙이며 Unity 경계 동작이나 실제 미래 판정을 재현하지 않습니다. [구현·최종 검사 범위](RANDOM-PREVIEW-FOLLOWUP.md)는 실제 게임 실행 검증과 구분합니다.
- `GameStat`과 그 변환 그래프에서 확인한 타입·재귀를 검사한 뒤 감사한 읽기 getter를 사용합니다. native 곡선 계산은 남은 런타임 경계입니다.

`development/tools/LogRefreshInjector/SafeEvaluationHooks.cs`는 **모드의 알려진 설명 계산 호출 위치**를 순수 평가로 바꾸는 경계입니다. 일반 게임 진행 중 체크포인트 내러티브 설명도 깊이 0에서 같은 helper를 사용하므로, 설명 호출은 보호 깊이와 관계없이 안전 평가를 사용해야 합니다. 게임 어셈블리의 원본 메서드나 의도적인 이벤트·미션 실행 자체를 대체하지 않습니다. 해당 연결·반복 패치·자동 검사 결과는 [검증 기록](ISSUE-1-VALIDATION.md)과 구분해 확인합니다.

`DisplayRefreshDepth`는 계속 DiceMaster의 Lua 분석 재진입, 체크포인트 대화 로그 추가, `TargetEvent`/`TargetMission` 소비를 막고 `finally`에서 복원합니다. 이 깊이 보호만으로 게임 원본의 RNG·카운터 쓰기를 막을 수 있다는 기존 가정은 사용하지 않습니다. 명시적인 이벤트·미션 실행 버튼은 별도의 게임 진행 경로입니다.

## 표시 실패와 캐시 경계

기존 기준 DLL에는 예외를 기록하고 빈 결과를 반환하는 `ExtractConditions`, 누락 필드를 기본값으로 바꾸는 `ReflectionExtensions.GetFieldValue<T>`, 데이터 미준비를 빈 목록으로 바꾸는 이벤트·미션 열거 경계가 있었습니다. 현재 표시 계층은 엄격한 조회 실패를 항목별 실패 상태로 전달하고, 마지막 정상값·독립적인 정상 행·재시도를 보존하는 방식으로 검사합니다. 실제 null 조건 목록도 오류로 전달해야 합니다.

공유 Lua 파서의 완료 캐시를 유지한 채 `OptionResult.Clear()`만 실행하던 동작이 선택지 결과 소실을 만들었습니다. 공유 고정 분석과 항목의 현재값 갱신을 구분하고, 이벤트 고정 설명에는 분리된 파서를 사용합니다. 이미 준비된 정상 항목의 주기 조회에서 Lua 재분석이 없어야 합니다. 실제 게임 Lua·리소스가 전달본에 없어 게임 콘텐츠 전체에 대한 파서 재현 검증은 미실행입니다.

## 실제 새 게임·로드·Lua 경계

아래는 실제 DLL에서 확인한 경계이며, 세부 호출 순서·실패·취소와 IL 근거는 [수명주기 분석](GAME-LIFECYCLE-ANALYSIS.md)에 있습니다.

| 실제 타입·메서드 | 정적 분석 결과 | 표시 정책 |
|---|---|---|
| `Mortal.Core.SaveSystem.NewGameData()` / `NewActGameData()` | 같은 player/mission/item/shop 객체들을 Reset | 진입 전 이전 문맥 무효화. 반환을 새 Lua 준비 완료로 보지 않음 |
| `LoadGameData()` / `AutoLoadGameData(string)` / `LoadRestoreSaveData()` | null 반환·역직렬화 실패·부분 상태 변경 가능 | 실패·빈 저장·같은 객체 로드에서도 오래된 행을 재사용하지 않음 |
| private `ExecuteLoadGameData(GameSave)` | 같은 manager들을 순서대로 Load, rollback 없음 | 공통 변경 시작 경계. 완료만으로 이전 Lua 표시 재개 금지 |
| private `SceneController.StartLoadScene(string,bool)` | 코루틴 factory만 반환 | Prefix는 전환 시작이며 Postfix는 씬 로드 완료가 아님 |
| `Fungus.LuaEnvironment.LoadLuaFunction(string,string)` | 전처리 예외 가능, 컴파일 실패는 null, 성공은 Closure 반환 | Prefix는 이전 문맥 무효화, non-null 성공 Postfix에서만 새 고정 분석 활성화 |

`MissionManagerData.UpdateCheckMissions()`와 `SubMissionsData.GetActiveMissions()`는 플래그·현재 키·예약을 바꾸므로 표시 준비 확인용으로 추가 호출하지 않습니다. `ReadStorySystem.ExistKey`는 `ContainsKey`만 수행하며 universe 로드·Reset에서 사전이 교체됩니다. 일반 슬롯 로드에서 manager 교체를 가정하지 않습니다.

`LuaManager.PrologueSave`의 제자리 복원은 새 Lua 컴파일을 자동 호출하지 않습니다. 이전 문맥을 지운 후 다음 성공 Lua까지 차단하는 정책에서는 그 사이 표시가 비어 있을 수 있습니다. 현재 Lua를 임의로 재사용하여 준비 완료로 처리하지 않으며, 이 경로의 즉시 표시 재개는 별도 미구현 한계입니다.

## 검증 상태와 남은 한계

| 항목 | 현재 증거 |
|---|---|
| 실제 참조 수신·설치본 식별 | Mac 검증 JSON 159/159, 주요 DLL 해시 재확인 |
| `GetRate`·`GetResult`·`Check`와 하위 호출 | 관리 DLL 본문·IL 정적 분석, RNG 진입·카운터 쓰기 확인 |
| null·빈 목록·수치 의미 | 실제 본문에서 확정, 자동 검사의 회귀 기준으로 사용 |
| 새 게임·로드·씬·Lua 경계 | 실제 정확한 서명·본문·객체 재사용·실패 경로 분석 |
| 순수 평가·수명주기·통합 자동 검사 | Mac에서 실행한 결과만 [검증 기록](ISSUE-1-VALIDATION.md)에 기재 |
| 실제 콘텐츠 파서 재현 | 게임 리소스·Lua가 없어 미실행 |
| Unity native·실제 Harmony/외부 모드 상호작용 | 게임 런타임이 없어 미실행 |
| 실제 게임 UI·진행·패드·성능 | 미실행 |
| 사용자 Windows 게임 검증 | 미실행, 현재 실행 승인 없음 |

모든 개발과 자동 검사는 현재 macOS에서만 수행합니다. 원본 파일 수집이나 과거 로그의 로더 식별은 게임 실행 검증과 다릅니다. 실제 게임 API의 관리 코드 분석을 진행했더라도 native·자료·런타임 한계와 Windows 검증은 남아 있으므로 **이슈 완료·정식 Release 공개·실제 게임 성능 검증 완료로 처리하지 않습니다.**
