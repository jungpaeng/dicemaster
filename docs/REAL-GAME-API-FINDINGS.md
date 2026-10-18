# 실제 게임 표시 조회 API 정적 분석

기록일: 2026-10-18. 대상은 이슈 #1의 표시 전용 조회입니다. 실제 게임 DLL의 메서드 본문과 하위 호출을 macOS에서 분석했습니다. **게임 런타임 실행·실제 Unity 동작·Windows 검증은 수행하지 않았으며, 이 기록은 구현 완료나 정식 Release 공개 판정이 아닙니다.**
게시된 `v1.0.3-alpha.1`·`v1.0.3-alpha.2`·`v1.0.3-alpha.3` prerelease는 기능·이슈 완료/정식 Release 판정과 구분합니다. Windows 게임 실행·설치 검증은 계속 미승인·미실행입니다.

사용자가 승인한 alpha.3은 게임 RNG와 분리된 모드 전용 `System.Random`으로 숫자와 조건 색상·이벤트 확률을 계산합니다. UI 부가 표기는 붙이지 않지만 문서에서는 실제 게임 미래 판정의 예측값이 아님을 명시합니다. 최종 macOS 관리 동작 3,914개·메타데이터 1,011개·통합 134개를 최초/반복에서 통과했고 패드→로그 왕복·반복 구조·직접 패드·로컬 ZIP도 검증했습니다. 원본 참조 159개의 크기·SHA-256은 유지됐습니다. 2026-10-18 22:43:45 KST에 [v1.0.3-alpha.3 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.3)를 배포 태그 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`에서 게시했습니다. 익명 공개 API·다운로드·CRC·정확히 두 파일·각 파일의 배포 커밋 바이트·ZIP/DLL SHA-256 일치를 검증했습니다. 파일 해시·범위·게시 영수증은 [검증 기록](ISSUE-1-VALIDATION.md)에 별도로 남깁니다.

세이브 로드·새 게임·이야기 읽음 표시의 별도 분석은 [게임 수명주기 분석](GAME-LIFECYCLE-ANALYSIS.md)을 참조합니다.

## 입력 자료와 재현 범위

원본 참조 루트:

```text
/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018
```

관리 DLL은 `Mortal_Data/Managed`, 설치 모드와 로더 참조는 `BepInEx/plugins`, `BepInEx/core` 아래입니다. 원본은 읽기 전용 분석 입력으로 보존하며 저장소의 개발 후보 DLL로 복사하거나 덮어쓰지 않았습니다.

전달된 `verification-20261018.json`은 ZIP SHA-256 `42F946C26956B61FFE4ACA4C3391BDF0D9EF343FDA6B7B242AA617E9E46BC5CC`, 압축 CRC, 안전한 경로, 159개 DLL/설정의 크기·해시 일치를 기록합니다. 보고서의 과거 전송 미확인 문구는 이 macOS 수신 검증보다 이전 상태입니다. 분석 시 아래 주요 파일의 SHA-256을 다시 계산하여 검증 기록과 일치함을 확인했습니다.

| 파일 | SHA-256 |
|---|---|
| `Mortal.Core.dll` | `0c20ee36782666f3ab70ec8889068bdf230acc2d630446ea1db551836866e8fa` |
| `Mortal.Story.dll` | `ee9139a5e4631dc275b80cc3ff6a81a4959f3c908a3fe751e778a42350372249` |
| `UnityEngine.CoreModule.dll` | `33435ff1025f34c7ac661477dbcaa329af7c5d7ae2bac81bcb8b0d636ca07aee` |
| `OBB.Framework.dll` | `bc6ebc4bdf1df52e74fafa687d66112bf94808c1ecd25b80d3d3f3913c93175f` |
| `Fungus.dll` | `bcb6d47eb31dedc5d23d0da281e0835af0f939f30b64a39aae7d5f6dab96cad2` |

Windows의 기존 파일·로그에 관한 수집 보고서는 Steam appid `1859910` / buildid `26021712`, `Mortal.exe` / `Mortal_Data`, Unity `2020.3.49f1`, BepInEx `6.0.0-be.692`, DiceMaster `1.0.0`을 식별합니다. 이는 기존 자료이며 새 실행 결과가 아닙니다. legacy 로더 DLL의 공존만으로 활성 버전을 추정하지 않습니다.

ILSpy `9.1.0.7988`을 사용했습니다. 임시 역컴파일 출력은 `/private/tmp/dicemaster-real-api/audit`이며 게임 소스 전체를 저장소에 추가하지 않았습니다. 대표 재현 명령은 다음과 같습니다.

```sh
DOTNET_ROOT=/private/tmp/dicemaster-dotnet \
  /private/tmp/dicemaster-tools/ilspycmd --disable-updatecheck \
  -r '<참조 루트>/Mortal_Data/Managed' \
  -t Mortal.Core.StatValueReference \
  -o /private/tmp/dicemaster-real-api/audit \
  '<참조 루트>/Mortal_Data/Managed/Mortal.Core.dll'
```

같은 명령의 `-il` 출력으로 분기·쓰기·호출을 대조했습니다. IL 출력은 타입 옵션과 관계없이 모듈 전체를 포함할 수 있어, 아래 서명과 메서드 내부 IL 오프셋으로 근거를 식별합니다.

두 게임 어셈블리의 메타데이터 identity는 각각 `Mortal.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`, `Mortal.Story, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`입니다. 버전 필드가 빌드를 구분하지 못하므로 위 SHA-256과 함께 사용합니다. 원본 메서드의 metadata token은 다음과 같습니다.

| 메서드 | 어셈블리 | token |
|---|---|---|
| `PositionEventData.GetRate` | `Mortal.Story` | `0x06000059` |
| `StatGroupVariable.GetResult` | `Mortal.Core` | `0x060002b6` |
| `StatCompareItem.Check` | `Mortal.Core` | `0x060004b5` |
| `ConditionResultItem.Check` | `Mortal.Core` | `0x06000403` |
| `StatValueReference.GetValue` | `Mortal.Core` | `0x060004b8` |
| `ConditionResultData.Execute` | `Mortal.Core` | `0x060000a0` |

## 핵심 결론

`GetRate`·`GetResult`·`Check`는 표시를 위해 반복 호출해도 되는 순수 조회 API가 아닙니다. 중첩된 `StatValueReference.GetValue`의 종류에 따라 **게임 RNG를 호출하여 난수를 소비할 수 있고, `ConditionResultData._count`를 증가시킵니다**. DiceMaster의 `DisplayRefreshDepth` 콜백 보호만으로 원본 게임 메서드의 이 동작은 차단되지 않습니다. 난수 상태만 저장·복원해도 카운터 쓰기는 남습니다.

| 정확한 공개 인스턴스 서명 | 본문과 하위 호출 | 표시 구현에 대한 의미 |
|---|---|---|
| `System.Int32 Mortal.Story.PositionEventData.GetRate()` | 기본 확률을 읽고, 활성 조건을 검사한 뒤 각 확률 항목의 조건이 참이면 가산. 마지막에 하한 0 적용 | 본문 자체는 이벤트 실행·예약·필드 쓰기 없음. 그러나 조건의 난수·카운터 부작용이 전이됨. 반환값은 100을 초과할 수 있음 |
| `System.Int32 Mortal.Core.StatGroupVariable.GetResult()` | `_value`의 모든 `StatValueReference.GetValue()`를 먼저 평가하고 Sum/Max/Min/Multiply 집계 | 그룹 안에 난수·조건 실행이 포함될 수 있음. 그룹의 이름·최상위 종류만 확인해서는 충분하지 않음 |
| `System.Boolean Mortal.Core.StatCompareItem.Check()` | 양쪽 null이면 각각 오류 로그를 남긴 후 양쪽 `GetResult`를 호출하여 비교 | 표시값을 얻기 위해 이미 `GetResult`를 호출한 뒤 `Check`를 호출하면 피연산자를 다시 평가함. 같은 조건을 색상·기간용으로 재호출하면 다시 평가됨 |
| `System.Boolean Mortal.Core.ConditionResultItem.Check()` | 모든 `StatCompareItem.Check()`를 `Select(...).ToArray()`로 평가한 다음 AND/OR 집계 | AND의 앞 항목이 false여도, OR의 앞 항목이 true여도 나머지 비교는 평가됨 |
| `System.Int32 Mortal.Core.StatValueReference.GetValue()` | 숫자·상태 조회 외에 난수, 중첩 그룹, 조건 실행 분기 포함 | 표시에는 재귀적인 안전 평가가 필요 |
| `System.Boolean Mortal.Core.ConditionResultData.Execute()` | 먼저 `_count`를 증가시킨 후 `_items`를 순서대로 평가하고 처음 true에서 반환 | 표시에서 호출하면 게임 객체를 변경함. 조건 목록의 논리만 별도로 읽어야 함 |

주요 IL 근거:

- `StatValueReference.GetValue`, `IL_00ba`: `UnityEngine.Random.Range(System.Int32,System.Int32)` 호출. 최댓값 인자는 `_randomMax + 1`입니다.
- 같은 메서드, `IL_00fe`: 중첩 `StatGroupVariable.GetResult()` 호출.
- 같은 메서드, `IL_0179`: `ConditionResultData.Execute()` 호출.
- `ConditionResultData.Execute`, `IL_0009`: `_count`에 기존 값 + 1을 `stfld`. 목록을 읽기 전에 발생하므로 빈 목록·뒤따르는 오류도 쓰기를 취소하지 않습니다.
- `ConditionResultItem.Check`, `IL_0025` / `IL_002a`: `Enumerable.Select` / `Enumerable.ToArray`; `IL_0035`에서 `GameStatUtils.LogicOp` 호출.
- `StatCompareItem.Check`, `IL_0036` / `IL_0041`: 양쪽 `GetResult()` 호출. `IL_004e`에서 `GameStatUtils.StatCompare` 호출.

제공된 Unity 관리 DLL의 정수 `Random.Range`는 native internalcall `RandomRangeInt`로 위임합니다. `Random.state`도 `GetScriptingRand()`에 대한 native get/set 경계입니다. 이 분석은 공유 RNG 진입 호출을 증명하며, native UnityPlayer가 없는 상태에서 범위가 같은 경우 등을 포함한 정확한 native 상태 진행 횟수를 실행 측정한 것은 아닙니다.

LINQ용 정적 delegate 초기화 캐시 쓰기도 확인되지만, 이것은 위 게임 객체 카운터·난수 변경과 별개입니다. 분석한 직접·전이 경로에서 그 외 이벤트 실행, 미션 예약, `OnValueChanged` 호출은 발견하지 않았습니다. 다른 모드가 런타임에 추가한 Harmony 패치의 영향은 이 정적 결론에 포함하지 않습니다.

기존 DiceMaster에는 일반 게임 진행의 `PrefixExecuteConditionResult` / `PrefixExecuteSwitchResult` / `PrefixCheckMission`에서 체크포인트 설명을 만드는 경로도 있습니다. 이들은 `DisplayRefreshDepth == 0`에서 `AppendConditions` → `DataHelper.ExtractConditions`를 호출합니다. 따라서 안전 평가를 보호 깊이가 양수일 때만 적용하면 주기·수동 갱신은 보호할 수 있어도, 이 기존 내러티브 설명 경로는 같은 부작용을 유지합니다. 새 `SafeEvaluationHooks` 소스는 알려진 모드 설명 호출을 깊이와 관계없이 순수 평가로 바꾸며 원본 게임 메서드로 되돌아가는 fallback을 두지 않습니다. 게임 원본 메서드·Harmony의 실제 진행·명시적인 강제 실행 경로 자체는 교체하지 않습니다.

## 변수 종류별 하위 호출

`StatCheckType`의 실제 숫자와 동작입니다. `_negative`는 처음 네 가지 종류에만 반영되며, 다른 종류에 일괄 적용하면 원본과 달라집니다.

| 값 | 실제 enum 이름 | 평가와 쓰기 여부 |
|---:|---|---|
| 0 | `常數` | `_constant`와 부호 곱. 필드 읽기 |
| 1 | `角色數值` | `GameStat.FinalValue`와 부호 곱. 아래 전이 경로 참조 |
| 2 | `好感度` | `RelationshipStat.Value` → `_value` 읽기와 부호 곱 |
| 3 | `旗標` | `FlagData.State` 필드 읽기와 부호 곱 |
| 4 | `書籍` | `Book`이 상속한 `ItemData.CurrentCount` → `_currentCount` |
| 5 | `雜物` | `Misc`의 같은 상속 getter |
| 6 | `貴重品` | `SpecialItem`의 같은 상속 getter |
| 7 | `隨機值` | `UnityEngine.Random.Range` 호출. 표시에서 그대로 호출하면 게임 RNG에 진입하며 난수를 소비할 수 있음 |
| 8 | `數值群組` | 다른 그룹의 `GetResult` 재귀 호출 |
| 9 | `遊戲時間` | `GameTimeData.GameTime` 필드 → `GameTime.ConvertToRounds()`. 연·월·단계 산술만 수행 |
| 10 | `技能` | `PlayerTalentData.Level` → `_level` |
| 11 | `秘笈等級` | `Book.CurrentMartialLevel` → `_currentMartialLevel` |
| 12 | `已開發項目` | `UpgradeItemData.AlreadyDevelop` → `_alreadyDevelop`를 1/0으로 변환 |
| 13 | `開發項目等級` | `UpgradeItemData.CurrentLevel` → `_currentLevel` |
| 14 | `書籍精通` | `BookDataCollection.GetMaxLevelCount()`: `CollectionData<Book>.List` 필드를 순회하여 `Book.IsMaxLevel` 합산 |
| 15 | `風雲史` | `LibraryItemData.Count` → `_count` 읽기 |
| 16 | `條件設定` | `ConditionResultData.Execute()` 호출. `_count` 증가 및 중첩 조건의 난수 가능 |

`GameStat.FinalValue`는 값과 `AdditionValue`를 더하여 범위로 제한합니다. `AdditionValue`는 `StatConvertCollectionData.GetSummary()`를 호출하고 `Mathf.RoundToInt`로 합산합니다. `GetSummary`는 `CollectionData<StatConvertData>.List`를 읽으며 변환 자료별 `GetResult`의 합과 배율을 계산합니다. 제공 DLL의 구체 변환 타입은 다음과 같습니다.

- `GameStatConvertData`, `GameStatRateConvertData`: 다른 `GameStat.FinalValue`를 재귀적으로 읽고 곡선·배율 계산.
- `FlagDataRateConvertData`: `FlagData.State`와 배율·역전 산술.
- `BookLevelStatConvertData`: 책의 현재/최대 레벨과 곡선 계산.
- `UpgradeItemStatConvertData`: 개발 현재/최대 레벨과 곡선 계산.
- 공통 `CurveStatConvertData.GetResult`: `Mathf.InverseLerp`와 `AnimationCurve.Evaluate`.

이 경로의 제공된 관리 코드에는 명시적인 게임 필드 쓰기·난수·콜백·실행 호출이 없습니다. `Mathf`·`AnimationCurve.Evaluate`는 Unity의 native 경계이므로 본 DLL만으로 native 구현을 검증한 것은 아닙니다. 다른 모드가 추가한 `StatConvertData` 하위 타입이나 패치는 별도 분석 대상입니다. 잘못된 순환 데이터는 `FinalValue`·그룹·조건 재귀를 끝내지 못할 수 있으므로 표시 계층은 재귀 가드와 항목 실패 처리가 필요합니다.

## null, 빈 목록, 수치 의미

`ConditionResultItem._items == null`은 정상적인 무조건 충족을 뜻하지 않습니다. 생성자는 빈 `List<StatCompareItem>`을 준비하지만, `Check()`는 null 방어 없이 LINQ에 전달합니다. 정상 빈 목록은 `GameStatUtils.LogicOp`에서 **false**가 됩니다. 따라서 null을 조용히 빈 설명·기간 밖으로 바꾸는 기존 경계는 읽기 실패를 숨깁니다. null은 해당 항목의 실패로 전달하고, 유효한 빈 목록과 구분해야 합니다.

| 입력 | 제공 DLL의 의미 |
|---|---|
| `ConditionResultItem._items == null` | 실패. LINQ null 인수 예외 |
| 같은 목록이 비어 있음 | false |
| 비교 항목 또는 `_value1`/`_value2`가 null | 실패. null 피연산자는 오류 로그 후 접근 실패 |
| `ConditionResultData._items`가 비어 있음 | `_count` 증가 후 false |
| 같은 목록이 null | `_count` 증가 후 접근 실패 |
| `StatGroupVariable._value == null` / 목록에 null 항목 | 실패 |
| 빈 그룹 Sum / Multiply | 0 / 1 |
| 빈 그룹 Max / Min | 실패 |
| `_toggleCondition == false`인 이벤트 | `_activeCondition`은 접근하지 않음 |
| 활성 조건이 false인 이벤트 | 0을 반환하며 `_eventRateItem`은 접근하지 않음 |
| 활성 조건이 true인 이벤트의 `_eventRateItem == null` | 실패 |

`StatGroupType`은 `Sum=1`, `Max=2`, `Min=3`, `Multiply=4`입니다. Sum은 LINQ의 checked 합산, Multiply는 unchecked 곱셈입니다. `StatCompareType`은 `Equal=0`, `NotEqual=1`, `Less=2`, `LessEqual=3`, `Greater=4`, `GreaterEqual=5`; `LogicOperatorType`은 `AND=0`, `OR=1`입니다. 확률 가산은 unchecked 정수 덧셈이며 하한만 0입니다.

원본은 알려지지 않은 그룹 enum에 0, 비교 enum에 false를 반환하며, 알려지지 않은 논리 enum은 첫 비교값을 유지합니다. 안전 표시 계층이 알 수 없는 형태를 명시적으로 거부하는 것은 더 엄격한 정책이며, 오류 상태로 보여야 합니다.

## 개발 소스 반영과 회귀 검사 기준

`src/DiceMaster/LiveLog/SafeDisplayEvaluation.cs`는 원본 `GetRate`·`GetResult`·`Check`를 직접 실행하지 않고 확인된 필드·읽기 전용 getter를 사용합니다. 조건 설정 분기는 `_count`를 건드리지 않고 조건 목록을 직접 OR 평가합니다. alpha.1은 난수를 `RandomDisplayValueException`으로 거부해 행 실패로 표시했습니다. alpha.2는 `RandomPreview`·`PreviewFormatting`의 범위·판정 미정·전용 미지원 결과와 제한된 단일 비교의 이론 N/D를 사용했습니다. 이 정책은 해당 버전 이력으로 보존합니다.

alpha.3 설명 경로는 잠금으로 보호한 모드 전용 단일 `System.Random`으로 숫자를 생성하며 게임 RNG를 호출하지 않습니다. 평가 세션은 개별 `ConditionResultItem` 설명 또는 전체 이벤트 설명(헤더·활성 조건·모든 확률 항목 조건)을 경계로 하며 동일 객체의 값·그룹·비교·조건 결과를 재사용해 숫자·색상·헤더와 하위 설명을 일치시킵니다. 다른 난수 참조는 각각 추첨하고 다음 갱신은 새 세션입니다. 미션·스위치 전체 행의 서로 다른 조건 항목은 각각 별도 세션이므로 공유 참조도 다시 추첨할 수 있습니다. 독립 조건 설명은 `ConditionResultItem` 하나를 뜻합니다. Sum/Max/Min/Multiply와 중첩/여러 난수·조건 설정·숫자 이벤트 확률을 다루며 `_count`는 보존합니다. 양끝을 포함하는 전체 int 구간과 역순 구간 정규화는 모드 화면의 규칙으로, Unity의 `max + 1` overflow·역순 인수 의미를 그대로 모사하지 않습니다. UI 부가 문구는 붙이지 않으며 실제 게임의 반복 추첨 횟수·미래 판정·분포를 예측하지 않습니다. [후속 구현·검사 기록](RANDOM-PREVIEW-FOLLOWUP.md)에 새 계약과 검사 결과를 명시합니다.

소스에서 실제 17개 변수 종류, 부호 적용 범위, 빈 집계·null 처리, checked Sum/unchecked Multiply, 시간 산술, 조건 eager 평가와 조건 설정 단축 평가를 실제 IL과 대조했습니다. 알 수 없는 enum은 원본의 임의 기본값 대신 명시적인 실패로 처리합니다. 객체 동일성에 따른 재귀 가드와 최대 깊이 128을 둡니다.

`GameStat.FinalValue`를 호출하기 전 stat 객체, 변환 컬렉션, 변환 객체를 확인한 타입에 한정하고 재귀적인 GameStat 참조를 검사합니다. `StatConvertCollectionData`의 미지원 하위 타입도 reflection getter를 호출하기 전에 거부합니다. 이로써 미지원 변환·순환 그래프가 원본의 재귀 getter에 진입하는 것을 막습니다. 검증한 관리 코드 getter가 이용하는 `Mathf`·`AnimationCurve.Evaluate`의 native 동작, 다른 모드의 Harmony 패치와 runtime 교체 자료는 실제 게임에서 실행 검증하지 않았습니다.

`tools/LegacyPatch/LogRefreshInjector/SafeEvaluationHooks.cs`는 알려진 모드 설명 계산 호출 위치를 위 평가로 연결합니다. 게임 관리 DLL 입력은 수정하지 않습니다. 실제 반영된 후보 DLL과 자동 검사 결과는 [검증 기록](ISSUE-1-VALIDATION.md)의 해당 빌드 식별값과 함께 확인해야 합니다.

검사에서 확인할 의미 있는 경계는 다음과 같습니다.

- 반복 표시와 실패 후 재시도 전후에 RNG 호출 수·조건 실행 카운터·게임 상태가 변하지 않는지. 실제 DLL에서 가져온 IL을 대역에 복사한 검사는 원본 부작용의 재현 근거와 구분합니다.
- 직접 난수, 그룹 안 난수, 조건 설정 안 난수의 모드 전용 추첨과 개별 조건 항목 또는 전체 이벤트 설명 안의 결과 재사용, 조건 항목 사이·다음 갱신의 별도 추첨. 게임 RNG 비소비와 다른 정상 행의 독립 갱신.
- `_count`가 있는 중첩 조건 설정을 표시로 읽어도 카운터가 보존되고, 일반 게임 실행 경로는 여전히 카운터를 증가시키는지.
- null 목록·null 비교 피연산자·빈 목록의 구분, 빈 Sum/Multiply/Max/Min, 음수 부호의 종류별 적용, 합산 overflow, 확률의 하한 및 100 초과.
- 조건 항목의 eager 평가와 조건 설정의 첫 true 단축 평가, 재귀 자료의 안전 실패.
- 표시 전용 경계 밖의 일반 이벤트·조건·미션 실행과 DiceMaster의 의도적인 강제 실행이 바뀌지 않는지.

실제 Lua·이벤트 리소스는 전달 자료에 없어 해당 빌드에서 난수·조건 설정이 실제 어떤 행에 사용되는지 빈도나 자료 완전성을 확정하지 못합니다. 런타임 리소스·native Unity·다른 모드의 패치·표시 품질·실제 성능과 사용자 Windows 검증은 미실행입니다. Windows 실행은 승인되지 않았으며 이 분석을 위해 실행하거나 요청하지 않았습니다.
