# PRD 2 게임 API 의미 대조

2026-10-19 KST에 읽기 전용 게임 참조의 메타데이터·본문·하위 호출을 다시 확인했다. 이 문서는 구현한 설명과 실제 연출 모듈의 정적 의미 근거이며 Unity·Windows 실행 결과가 아니다. 게임 Lua나 효과 API를 실행하지 않았다.

## 입력 식별

참조 루트는 `/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018`이다. 아래 파일은 모두 `Mortal_Data/Managed/`의 읽기 전용 입력이다.

| 파일 | SHA-256 |
| --- | --- |
| `Mortal.Story.dll` | `ee9139a5e4631dc275b80cc3ff6a81a4959f3c908a3fe751e778a42350372249` |
| `Mortal.Core.dll` | `0c20ee36782666f3ab70ec8889068bdf230acc2d630446ea1db551836866e8fa` |
| `Fungus.dll` | `bcb6d47eb31dedc5d23d0da281e0835af0f939f30b64a39aae7d5f6dab96cad2` |

`ilspycmd --disable-updatecheck -t <타입> <DLL>`과 Mono.Cecil 메타데이터 읽기로 재생성했다. 원본 전체 소스·Lua·세이브를 저장소나 진단 로그에 추가하지 않았다. 아래 토큰은 위 입력에만 해당한다. `s`는 문자열 리터럴, `i`는 손실 없이 Int32로 변환되는 상수식을 뜻한다.

## StatModifyManager 추가 16개

모두 `Mortal.Story.StatModifyManager`의 공개 메서드다. 인자에 기본값은 없다.

| 함수·인자 | 토큰 | 확인한 의미와 표시 경계 |
| --- | --- | --- |
| `SetPlayer(s name, i value)` | `0x060000b5` | 능력치의 기초 Value 직접 설정. `PlayerStatData.Set` → `GameStat.SetValue`의 min/max 제한 적용. 가산이나 FinalValue 지정으로 설명하지 않는다. |
| `SetCharacter(s name, i value)` | `0x060000b6` | 호감도 직접 설정. `RelationshipStat.SetValue`는 값을 대입하며 **범위 제한이 없다**. 가산 메서드의 제한과 혼동하지 않는다. |
| `RemoveBook(s id)` | `0x060000bd` | `ItemDatabase.RemoveItem(Book,id)` → 수량 **1 감소**. 전체 제거가 아니다. |
| `RemoveSpecial(s id)` | `0x060000bf` | 귀중품 수량 1 감소. 기존 `RemoveMisc`도 같은 의미다. |
| `Mission(s name, s key)` | `0x060000b8` | 이름이 main이면 메인 미션, 그 외 서브 미션의 현재 단계 키 설정. |
| `IsTriggerSubMission(s key, i value)` | `0x060000b9` | 지정 미션 키와 발동 여부 저장. value가 1일 때만 true. |
| `ModifyEnemyId(s id)` | `0x060000c2` | 현재 적 부대 선택. 새 적 객체 생성으로 설명하지 않는다. |
| `ModifyEnemyPeople(s id, i value, i display)` | `0x060000c3` | 해당 부대가 있으면 병력 가산. display=1은 알림을 제어한다. |
| `ModifyEnemyLevel(s id, i value, i display)` | `0x060000c6` | 해당 부대가 있으면 레벨 가산. 현재 본문은 display 인자를 사용하지 않는다. |
| `AddDeadLib(s id)` | `0x060000c7` | 사망 도감 항목의 기록 횟수 1 증가. |
| `AddEndGameLib(s id)` | `0x060000c8` | 결말 도감 항목의 기록 횟수 1 증가. |
| `AddAchievementLib(s id)` | `0x060000c9` | IEnumerator. 이미 획득한 항목은 중복 획득하지 않음. 미획득 업적 기록·알림·게임 저장 경로를 포함하지만 미리보기에서 호출하지 않는다. |
| `ResetTalkFlag()` | `0x060000cb` | 대화 선택지 플래그 컬렉션 ResetData. |
| `UpdateShopFlag()` | `0x060000c4` | 누적 상점 플래그가 1000 이상이면 1000 단위 몫만큼 `brother4` 호감도를 더하고 사용한 플래그를 차감한다. 1000 미만이면 변화 없음. 현재 누적량은 읽거나 추정하지 않는다. |
| `UpdateAssetStatus()` | `0x060000cd` | 활성 시설의 수입·효과와 구역 효과 실행. E11000 플래그 등에 의존하므로 고정 수치를 만들지 않는다. |
| `AddStory(s key)` | `0x060000cf` | 이야기 키 목록에 중복 없이 등록. |

## LuaManager 추가 12개와 연결 등록

모두 `Mortal.Story.LuaManager`의 공개 메서드다. 이 Lua 노출 메서드의 인자에는 기본값이 없다. 하위 `PlayerStatManagerData.SwapUpgradeItem`의 선택 인자 `level=-1`을 노출 메서드에 전용하지 않는다.

| 함수·인자 | 토큰 | 확인한 의미와 표시 경계 |
| --- | --- | --- |
| `ResetBattleSkill()` | `0x060000a4` | 네 슬롯 초기화, 전체 비활성화, 조건 재평가 후 기술 활성화·배치. 원래 조건 평가를 미리보기에서 실행하지 않는다. |
| `SetPlayerBattleSkill(s key, i index)` | `0x060000a5` | index를 사용하지 않는다. 첫 빈 슬롯에 배치하고 해당 기술을 활성화한다. 빈 슬롯이 없으면 배치는 일어나지 않을 수 있다. |
| `SetBattleSkillActive(s key, i active)` | `0x060000a6` | active가 정확히 1일 때 활성화, 나머지는 비활성화. |
| `SetBattleSkillLevel(s key, i level)` | `0x060000a7` | 해당 기술이 있으면 레벨 직접 설정. 가산이 아니다. |
| `SwapUpgradeItem(s key, i level)` | `0x060000a8` | 장비 종류별 원래 장비를 최초 한 번 백업하고 교체. level<0이면 교체 대상의 기존 상태 유지. level≥0이면 유효 레벨 범위 제한, 포인트 0·개발 완료로 설정한다. |
| `RestoreUpgradeItem(s key)` | `0x060000a9` | key 장비 자체를 무조건 장착하는 것이 아니라 해당 **종류**의 백업 복원. 백업 상태·레벨·포인트·개발 여부 복원에 의존한다. |
| `RestoreAllUpgradeItems()` | `0x060000aa` | 저장된 종류별 백업을 복원하고 백업 목록을 비운다. |
| `SetGameTime(i year, i month, i stage)` | `0x0600008b` | 현재 날짜 설정, 행동 횟수 초기화, 새 회차 표시, 시간 UI 갱신. `GameTime` 생성 시 낮 상태로 초기화된다. |
| `SetGameDayEnvironment(i type)` | `0x0600008c` | 낮=1, 밤=2. 그 외 정수는 해당 enum 값의 지정으로 표시하며 임의 보정하지 않는다. |
| `NextMonth()` | `0x060000ab` | 다음 달 상순으로 진행. 행동 횟수·회차 플래그·상점·시간 UI 갱신. 실제 행동 횟수는 계산하지 않는다. |
| `NextRound()` | `0x060000ac` | 다음 순 진행. 행동 횟수·회차 플래그 갱신, 상순 진입 때 상점 갱신, 시간 UI 갱신. |
| `SetMissionTime(s name, i year, i month, i stage)` | `0x06000095` | 현재 시각 복사 후 양수 month개월·양수 stage순 상대 진행. year는 사용하지 않음. month 진행 후 원래 순을 유지하고 stage만큼 추가 진행한다. |
| `SetNextScript(s script)` | `0x06000097` | 현재 이야기 스크립트 이름 등록. 즉시 실행이 아니다. 이후 지정이나 Story/Free 전환의 미션 검사로 대상이 바뀔 수 있으므로 후속 효과에는 불확실성·조건부 표시를 유지한다. |
| `SetCurrentStoryScript(s script)` | `0x06000090` | `SetNextScript`가 이 메서드를 호출하며 둘 다 `PlayerStatManagerData.SetStoryScript`에 같은 예약 대상을 기록한다. 두 API 사이의 덮어쓰기·빈 이름 초기화·동적 대상·다음 선택 경계를 공유한다. 등록 자체는 결과 보상으로 표시하지 않는다. |
| `SetEnableActions(i enabled)` | `0x06000096` | 정수 **1일 때만** `StoryManager.EnableAction=true`. 그 외 정수는 false다. 메뉴·상태창·로그·자동진행·다음 대사·스킵 입력의 게이트이므로 `이야기 조작 허용/차단`으로 표시한다. 행동력이나 보상 횟수의 변화가 아니다. |

기존 전투 진입 `ChangeScene(s name,s key,s nextScene)`은 `0x0600009c`이다. 원래 미리보기가 인식하던 `ChangeScene("Combat")`·`ChangeScene("Battle")` 형식도 전투 이름 표시 호환을 유지한다. 이 1인자 표시 호환은 실제 게임 메서드의 기본 인자 존재를 뜻하지 않는다. 정상 3인자 호출은 각 인자를 검증한다.

후속 정적 대조에서 `SetEnableActions` → `StoryManager.SetEnableActions(Boolean)`(`0x060000e8`) → setter(`0x060000da`)가 bool 필드 하나만 기록함을 확인했다. 위 입력 getter의 사용처는 `StoryManager`의 `0x060000e0`부터 `0x060000e5`까지다. 이 메서드를 미리보기에서 호출하지 않는다.

## 대화·연출 호출을 일괄 제외하지 않는 이유

실제 `lua/fungus` TextAsset과 공급된 Fungus IL을 함께 정적으로 확인했다. 모듈 본문 SHA-256은 `7942b9b8110a83e4e647ff264801cbfffff0748ea75c98544cb4a9f4a56c8de0`이다. 이름이 연출처럼 보여도 아래 호출은 계속 미지원·실행 불확실성으로 남긴다.

- `LuaManager.Init()`(`0x0600009a`)은 `InitLuaScript`·`ExecuteLuaScript`를 통해 Lua 컴파일·실행으로 이어진다.
- `runwait`는 임의 IEnumerator를 코루틴으로 넘기며 `runblock`은 Flowchart 블록의 명령을 실행한다.
- `say`의 대사 태그 처리에는 `Flowchart.BroadcastFungusMessage`와 `WriterSignals.DoTextTagToken`의 핸들러 호출이 있다. 문자열 리터럴이나 `GetStoryText` 인자만으로 태그의 부작용이 없다고 증명할 수 없다.
- `wait`는 yield 경계다. 순수한 지연으로 분류할 경우에도 이후 상태를 현재 스냅샷과 같다고 취급해서는 안 된다. 이번 후속에서는 연출 허용 목록을 넓히지 않았다.

이 분류와 전체 Lua 추출·리소스 경로·후속 대조의 범위는 [실제 Lua 검증 기록](ISSUE-2-REAL-LUA-VALIDATION.md)에 구분한다. unknown callback·동적 조건·게임 진행을 실제로 실행하여 표시를 채우지 않는다.

## Player의 상태 의존 계산

`Player(s name,i value,s mode,i display)`는 `0x060000b4`이다. `center`·`mental` 이외 문자열 mode는 실제 switch의 기본 경로인 일반 가산으로 설명한다. display는 알림만 제어하며 효과 자체를 제거하지 않는다.

- center는 `distance=50-FinalValue`에서 `abs(min(abs(distance),value))*sign(distance)`를 계산한다. distance=0이면 0이다. value=10, 현재 FinalValue=40/48/60/50이면 +10/+2/−10/0. 음수 value를 양수 한도로 고쳐 해석하지 않는다. Int32 중간 계산 경계 밖은 수치 대신 의미를 표시한다.
- mental은 심리위생의 FinalValue가 20 이하이면 전달된 value에서 1을 뺀다. 대상 능력치가 심리위생 자체가 아니어도 이 조건을 따른다.
- 화면의 숫자는 `예상 변경량`이고 현재 상태 기준·실제 가산 적용 전 값임을 명시한다. 기초 Value의 제한, 부가값, 누적 능력치 갱신·이벤트 통지를 포함한 실행 이후 FinalValue를 보장하지 않는다.
- 선행 효과·선행 미지원 효과·조건 분기·후속 스크립트가 있으면 현재 스냅샷으로 실행 상태를 추정하지 않고 의미·조건을 설명한다. 이벤트 고정 설명(`SnapshotAllowed=false`)도 현재값을 조회하지 않는다.

상태 조회는 `PlayerStatManagerData.Instance` getter를 호출하지 않는다. 이 getter에는 Unity 객체 탐색과 singleton 쓰기가 있다. 같은 타입에 직접 선언된 `_instance`(`0x04000108`)와 `_stats`(`0x040000e6`)를 읽고, `CollectionData<T>.List`(`0x0600009a`, 단순 필드 반환)에서 문자열 키의 enum 메타데이터를 대조한다. attribute 생성자도 실행하지 않는다.

`GameStat`은 공급된 Mortal.Core 내에 파생 타입이 없으며 FinalValue getter(`0x060000c5`)는 비가상이다. 기존 `SafeDisplayEvaluation`의 순환·정확한 구체 타입 검사를 공유한다. 허용하는 부가 변환은 `GameStatConvertData`, `GameStatRateConvertData`, `FlagDataRateConvertData`, `BookLevelStatConvertData`, `UpgradeItemStatConvertData`이다. 알 수 없는 변환 구현·컬렉션·능력치 타입은 getter를 실행하지 않고 의미 설명으로 폴백한다. null·순환·실제 조회 오류는 실패 정책에 전달한다.

## 상수 계산과 표시 전용 경계

공급된 `Fungus.dll`에는 MoonSharp가 내장되어 있다. `DynValue.Number`는 System.Double(`0x0600043d`), `NewNumber`도 double 입력(`0x0600044a`)이다. VM의 `ExecAdd/Sub/Mul/Div/Neg`는 각각 `0x06000aa0/0x06000aa1/0x06000aa2/0x06000aa4/0x06000aa6`에서 숫자 분기에 double 연산을 적용한다. 이 본문을 정적으로 대조했으며 인터프리터나 게임 Lua를 실행하지 않았다.

미리보기의 상수 계산기는 십진 숫자·소수·지수 표기, 사칙연산, 괄호, 단항 부호·우선순위를 처리한다. 결과가 유한한 정확한 Int32일 때만 인자로 사용한다. `%`, `^`, 변수·필드·호출·대입 추론, 16진 리터럴 등 범위 밖 문법은 미지원이다. 0 나누기, 소수 결과, 정수 범위 초과는 0이나 반올림 값으로 대체하지 않는다. Lua 주석은 구문 경계에서 제거하되 토큰 사이 공백을 보존하여 `1 2`를 12로 합치지 않는다.

구문 공격이나 정밀도 손실을 막는 내부 산술 보호는 재귀 깊이 128과 중간 절댓값 `2^53-1`이다. 이 범위를 넘었다가 상쇄되는 식도 미지원으로 남긴다. 이 제한은 실제 게임 Lua에서 정한 후속 추적 예산과 별개인 보수적인 계산 안전 경계다.

`GetDisplayTime`(`0x060000b2`)·`GetModifyString`(`0x060000ca`)은 보조 조회/서식 함수, `UpdateSetPlayerStat`(`0x060000b7`)은 변경 알림만 표시하는 함수다. 이들만 있는 블록은 상태 변경 효과나 미지원 효과로 만들지 않는다. `UpdateSetPlayerStat`를 능력치 직접 설정으로 설명하지 않는다.

효과 API, 임의 Lua 함수, 원래 조건 평가 사슬, RNG, 장비·기술·시간·미션 변경 API는 설명을 위해 호출하지 않는다. 정적 상수·지원 여부는 분석에 캐시하고 상태 스냅샷은 선택지 표시 시점에만 평가한다. 실제 Lua는 별도 정적 대조를 수행하며, native Unity·Windows·현재 실행 상태에서의 수치는 그 결과와 구분한 실행 확인 항목이다.

## 표시 이름 조회의 singleton 경계

기존 `DataHelper.GetTeamName/GetTalentName/GetItemName`은 `PlayerStatManagerData.Instance` 또는 `ItemDatabase.Instance`를 호출한다. `FlagTranslations.TranslateDevNote`도 ID 모양 입력에는 `MissionManagerData.Instance`를 호출한다. 이 getter들은 인스턴스가 없을 때 Unity 객체를 찾고 singleton 필드에 기록하므로 새 미리보기 경로에서는 호출하지 않는다. 기존 헬퍼 자체의 게임 호환 동작은 변경하지 않았다.

팀·기능·아이템 이름은 `PreviewState`가 해당 타입의 기존 `_instance` 필드만 읽고 `_enemyTeam`·`_talents`·아이템 종류별 필드에서 알려진 구체 컬렉션의 `_list`, 알려진 항목의 `_id`를 대조한다. 이름 키는 확인한 순수 생성 규칙 `EnemyTeam/`, `PlayerTalent/Name/`, `Book/Name/`, `Misc/Name/`, `Special/Name/`를 사용한다. 임의의 Get 메서드·가상 이름 메서드·객체 탐색을 호출하지 않는다.

플래그 이름은 기존 미션 singleton 필드와 `_flagsCollection`·컬렉션 `_list`에서 찾는다. 알려진 `FlagData`의 Unity `name`은 이름 대조를 위한 읽기 전용 경계이며 `_devNote`는 필드로 읽는다. 개발 메모가 ID 모양이 아닐 때만 기존 `TranslateDevNote`의 번역 분기를 사용하므로 그 함수의 singleton 탐색 분기에 진입하지 않는다. 메모를 찾지 못하거나 ID 모양 메모이면 원래 플래그 ID를 표시한다.

능력치·캐릭터·시스템 문구 번역의 `LocalizationManager.Instance`와 `LocaleResolver`는 공급 DLL에서 자동 속성의 단순 필드 반환이다. `LeanLocalizationResolver.GetString`은 번역 텍스트 조회이며 `SetLang`을 호출하지 않는다. RichText 헬퍼는 문자열만 다룬다. 기존 번역 캐시·미번역 로그 계약은 유지한다.

표시 이름의 인스턴스·컬렉션·항목·번역이 준비되지 않거나 알려진 타입과 다르면 **원래 문자열 ID**를 남긴다. 이름 장식 실패만으로 이미 확인한 효과를 지우거나 실제 singleton을 초기화하지 않는다. 이 폴백은 center/mental의 수치 조회와 다르다. 예상량 계산에 필요한 실제 상태가 없거나 null·순환·조회 오류가 발생하면 `게임 데이터 대기 중`/`읽기 실패`와 기존 재시도 정책으로 전달하며 성공한 수치로 보정하지 않는다.
