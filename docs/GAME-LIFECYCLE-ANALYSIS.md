# 실제 게임 새 게임·로드·Lua 문맥 경계 분석

기록일: 2026-10-18. 이 문서는 전달받은 실제 관리 DLL을 macOS에서 역컴파일하여 확인한 **정적 분석 증거**입니다. Unity 게임 런타임 실행, Windows 실행, 게임 리소스 기반 재현, 성능 측정의 증거가 아닙니다. #1 구현 완료·정식 Release 공개 조건은 충족하지 않습니다.
사용자가 별도로 승인한 `v1.0.3-alpha.1` prerelease 게시와 기능·이슈 완료/정식 Release 판정은 구분합니다. Windows 게임 실행·설치 검증은 계속 미승인·미실행입니다.


## 입력과 재현

읽기 전용 원본 루트:

```text
/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018
```

관리 DLL은 위 루트의 `Mortal_Data/Managed`에서 읽었습니다. 설치 메타데이터에 기록된 게임은 Steam appid `1859910`, buildid `26021712`, Unity `2020.3.49f1`입니다. BepInEx 활성 버전은 전달 보고서의 기존 로그·속성 근거로 `6.0.0-be.692`이며, legacy DLL 공존만으로 활성 버전을 추정하지 않았습니다. 설치된 DiceMaster `1.0.0`은 참조 사본이며 개발 후보를 덮어쓰지 않았습니다.

| 분석 DLL | SHA-256 |
|---|---|
| Mortal.Core.dll | `0c20ee36782666f3ab70ec8889068bdf230acc2d630446ea1db551836866e8fa` |
| Mortal.Story.dll | `ee9139a5e4631dc275b80cc3ff6a81a4959f3c908a3fe751e778a42350372249` |
| Mortal.Free.dll | `0abc19100f216515bca88975e52051afce11955a8439ffacf4ff02118fd27920` |
| Assembly-CSharp.dll | `8a14a5d689b25d4eb09279cb85cbfa8092d33bdd86b22a4364e8cd8ded1b3cf3` |
| Fungus.dll | `bcb6d47eb31dedc5d23d0da281e0835af0f939f30b64a39aae7d5f6dab96cad2` |

ILSpy 출력은 `/private/tmp/dicemaster-real-api/lifecycle`에만 생성했습니다. 본문과 IL을 함께 확인했습니다. 재현 도구는 `DOTNET_ROOT=/private/tmp/dicemaster-dotnet`, `/private/tmp/dicemaster-tools/ilspycmd`입니다. 다음 형식으로 해당 DLL을 다시 읽을 수 있습니다.

```sh
DOTNET_ROOT=/private/tmp/dicemaster-dotnet /private/tmp/dicemaster-tools/ilspycmd \
  -t Mortal.Core.SaveSystem '<참조 루트>/Mortal_Data/Managed/Mortal.Core.dll'
DOTNET_ROOT=/private/tmp/dicemaster-dotnet /private/tmp/dicemaster-tools/ilspycmd \
  -il -t Mortal.Core.SaveSystem '<참조 루트>/Mortal_Data/Managed/Mortal.Core.dll'
```

ILSpy의 `-il` 결과에 다른 타입이 함께 포함되더라도 아래 타입·메서드명과 메서드 내 IL 오프셋을 기준으로 식별합니다.

## 확인한 정확한 경계

아래 메서드는 추정 이름이 아니라 실제 어셈블리의 정의입니다. `SaveSystem`과 `SceneController`의 네임스페이스는 `Mortal.Core`, `LuaManager`는 `Mortal.Story`입니다.

| 실제 서명 | 관찰한 역할 | 표시 계층의 의미 |
|---|---|---|
| `public void SaveSystem.NewGameData()` | 같은 ScriptableObject들을 Reset하고 `ch1_1`/`Story`를 설정한 뒤 저장 | 메서드 진입 전에 이전 문맥 무효화 필요 |
| `public void SaveSystem.NewActGameData()` | 같은 데이터를 Reset하고 `act1`/`Story`를 설정한 뒤 저장 | 별도 새 게임 경로도 포함해야 함 |
| `public void SaveSystem.LoadGameData()` | 현재 슬롯 역직렬화, null이면 반환, 아니면 아래 공통 로더 호출 | 파일 읽기 실패도 다루려면 공개 진입점에서 표시 억제 |
| `public void SaveSystem.AutoLoadGameData(string slot)` | `AutoGameSave` 역직렬화, null이면 반환, `GameSave`를 공통 로더에 전달 | 자동 저장·계속하기 경로 |
| `public void SaveSystem.LoadRestoreSaveData()` | `_restoreData` JSON을 파싱하고 값이 있으면 공통 로더 호출 | 현재 Lua 실행 중 제자리 복원 경로도 있음 |
| `private void SaveSystem.ExecuteLoadGameData(GameSave gameSave)` | player → mission → item → shop Load, 마지막 `_restoreData` 할당 | 데이터 변경의 공통 시작 경계, 객체 동일성으로 감지 불가 |
| `private IEnumerator SceneController.StartLoadScene(string scene, bool unloadCurrentScene)` | 코루틴 상태 기계 생성 | Prefix는 시작 무효화에 사용 가능. Postfix는 완료가 아님 |
| `public virtual Closure Fungus.LuaEnvironment.LoadLuaFunction(string luaString, string friendlyName)` | 환경 초기화·전처리·컴파일, 실패하면 null 반환 | Prefix만으로 새 문맥 사용 가능 상태를 확정하면 안 됨 |

### 새 게임의 실제 순서

`SaveSystem.NewGameData`의 IL은 다음 순서입니다.

1. `GetSaveDataFile` → `CreateDir` (`IL_0007`/`IL_000f`). 이 단계에서 먼저 실패할 수 있습니다.
2. 기존 필드 `_playerStat.ResetData()` (`IL_001a`), `_missionData.ResetData()` (`IL_0025`), `_itemDatabase.Reset()` (`IL_0030`), `_shopDatabase.Reset()` (`IL_003b`). 새 manager를 대입하지 않습니다.
3. `SetStoryScript("ch1_1")`, `SetStartScript("ch1_1")`, `SetCurrentScene("Story")`, 게임 시간·Demo 설정. 조건에 따라 NewGamePlus와 라이브러리 보상을 적용합니다.
4. `CreateSaveData()` (`IL_00db`) → `ExecuteSaveData` (`IL_00f0`). 저장 예외는 `ExecuteSaveData` 내부에서 기록·복구하며 삼킵니다. `NewGameData`의 정상 반환은 디스크 저장 성공의 증거가 아닙니다.

호출 예:

- `TitleManager.NewGame` → `NewGameData` → `SceneController.LoadStory`.
- `LoadSlotPanel.OnTitleClick`의 빈 슬롯 분기 → `NewGameData` → `SaveUniverseData` → `UpdateCheckMissions` → `LoadStory`.
- `TitleManager.StartActGame` → `NewActGameData` → `LoadStory`.

`NewGameData` Postfix 시점에는 새 씬 로딩이나 새 Lua 컴파일이 시작되지 않았을 수 있습니다. 여기서 표시 조회를 재개하면 이전 Lua·씬 객체와 새 player/mission 상태를 섞을 수 있습니다.

### 로드의 실제 순서·실패·취소

`LoadGameData`는 `GetSaveData(_currentSlot)` 후 값이 null이면 `IL_0010`에서 바로 반환합니다. 성공 값은 `IL_002e`에서 `ExecuteLoadGameData`로 전달됩니다. `AutoLoadGameData`도 같은 null 반환 패턴이며 `IL_0029`에서 `AutoGameSave.GameSave`를 전달합니다. 해당 필드 자체의 null은 별도로 검사하지 않습니다.

`ExecuteLoadGameData`의 IL은 `_playerStat.Load(gameSave)` (`IL_0007`) → `_missionData.Load(gameSave)` (`IL_0013`) → `_itemDatabase.Load(gameSave)` (`IL_001f`) → `_shopDatabase.Load(gameSave)` (`IL_002b`) → `_restoreData = gameSave.RestoreData` (`IL_0037`)입니다. 이 함수에는 예외 처리·롤백이 없습니다. 중간 실패 시 이미 같은 객체의 일부 값이 변경되었을 수 있습니다.

`GetSaveData<T>`는 파일 부재 시 null을 반환하고, 파일이 있으면 `BinaryFormatter.Deserialize`를 호출합니다. 파일 읽기·형변환·역직렬화 예외는 이 메서드에서 잡지 않습니다. `LoadRestoreSaveData`의 `JsonUtility.FromJson<GameSave>`에도 catch가 없습니다. 따라서 공개 로드 함수의 void 정상 반환, 함수 호출 자체, manager 객체의 동일성만으로 성공·실패를 분류할 수 없습니다.

성공 호출자들은 이후 `MissionManagerData.UpdateCheckMissions`와 `SceneController.LoadCurrentScene`을 호출할 수 있습니다. `UpdateCheckMissions`는 읽기 전용 준비 상태 조회가 아닙니다. 새 라운드 플래그·시간을 갱신하고, `ActiveSubMissions` 사전을 교체하며, 조건에 따라 `SetStoryScript`와 미션 `SetKey`를 실행합니다. `SubMissionsData.GetActiveMissions`도 `ExecuteModifyFlagsWhenTrue/False`를 호출합니다. 표시를 재개하기 위해 이 함수들을 추가 호출하면 안 됩니다.

`LoadSlotPanel.OnTitleClick`의 `SceneController.IsPrepare` 조기 반환과 지원하지 않는 데모 분기, `RecentSaveSlotPanel`·`AutoSaveSlotPanel`의 빈/데모 분기는 SaveSystem 진입 전에 반환합니다. 화면의 취소·닫기·선택만으로 세션 변경으로 처리할 근거는 없습니다. 실제 SaveSystem 함수에는 cancel token이나 완료 이벤트가 없습니다.

### 객체 재사용과 읽은 이야기

`PlayerStatManagerData.Load`는 먼저 자신의 `ResetData`를 호출하고 같은 stats·relationships·talents 등의 컨테이너를 채웁니다. `_gameTimeData.GameTime`은 새 값으로 교체되지만 manager는 재사용됩니다. `MissionManagerData.Load`도 `ResetData` 후 같은 `_main`, `_subMissions`에서 현재 키·플래그를 다시 설정합니다. `MissionData.Reset`는 `_currentKey = _initKey`, `SetCurrentKey`는 같은 필드를 할당합니다. 같은 위치·같은 Lua·같은 manager로 돌아오는 로드는 정상적인 가능 경로입니다.

`ReadStorySystem`은 일반 `GameSave`가 아니라 `SaveSystem.LoadUniverseData`의 대상입니다. `Reset`·`Load(UniverseSave)`에서 `_dicStoryIndex`를 새 사전으로 교체합니다. `Load`의 `ReadStoryData == null`은 빈 사전으로 정상 반환합니다. 읽은 이야기 조회 `ExistKey`는 `ContainsKey`만 수행하지만 사전 자체가 null이면 예외가 발생합니다. 새 게임/슬롯 로드에서 ReadStorySystem manager가 반드시 교체된다고 가정하면 안 됩니다.

`CheckPointManager.Awake`는 자기 Instance를 저장하고 dice/condition/position/switch 설정의 Reset을 호출합니다. 새 씬의 Awake는 새 checkpoint 준비의 근거지만 모든 세이브·제자리 복원을 포괄하지 않습니다. `PositionResultConfig.Reset`은 null 목록이면 반환하고 각 항목 Reset을 부르며, `PositionResultData.Reset`의 본문은 비어 있습니다.

## 씬과 Lua의 완료 의미

`SceneController.LoadNewScene(string)`은 `StartLoadScene(name, true)`와 `StartCoroutine`을 호출합니다. `LoadScene(string)`과 `LoadNextScene()`도 이 코루틴을 직접 호출하므로 `LoadNewScene` 하나만 패치하면 모든 씬 전환이 잡히지 않습니다. 공통 `StartLoadScene` Prefix는 모든 호출의 시작 무효화에 적합합니다.

그러나 `StartLoadScene` 본문 IL은 `new <StartLoadScene>d__45`, 인수 필드 설정, `ret`뿐입니다. 실제 작업은 상태 기계의 `MoveNext`에서 수행됩니다. 코루틴은 `DOTween.KillAll` → 현재 씬 이름 설정 → 해당할 때 로딩 씬 로드 → `WaitForSeconds(0)` → 이전 씬 unload → 대상 씬 load → active scene 설정 → `Time.timeScale = 1` 순서입니다. 공통 factory Postfix가 돌아왔다는 사실은 이 순서의 완료를 뜻하지 않습니다. `IsLoading == false`만으로 성공도 확정할 수 없습니다. `LoadTargetScene`은 빈 씬 이름이면 상태를 `None`으로 하고 종료합니다.

`Mortal.Story.LuaManager.Start`는 localization/read-story 참조 설정 후 `Init`을 호출합니다. `Init`은 환경을 초기화하고, `ScriptName`이 비어 있으면 오류 후 반환합니다. 이후 `ExecuteLuaScript`는 `Resources.Load<TextAsset>("Story/ChineseTraditional/" + ScriptName)`을 수행하고 없으면 오류 후 반환합니다. 이 경우 `LoadLuaFunction`이 전혀 호출되지 않습니다. 실제 리소스는 전달본에 없어 성공 여부를 실행 검증하지 않았습니다.

`Fungus.LuaEnvironment.LoadLuaFunction`은 다음을 수행합니다.

- `InitEnvironment` (`IL_0001`), initializer가 있으면 `PreprocessScript` (`IL_0018`). 이 부분의 예외는 뒤의 catch 범위 밖입니다.
- `interpreter.LoadString` (`IL_002d`). `InterpreterException`을 잡아 로깅합니다 (`IL_0035`부터).
- 결과가 null이거나 Function 타입이 아니면 오류를 기록하고 null 반환 (`IL_0058`/`IL_0059`). 정상적으로 컴파일된 경우에만 `dynValue.Function` 반환 (`IL_005b`/`IL_0060`).

기존 Prefix는 컴파일 이전에 실행됩니다. 따라서 Prefix에서 새 Lua 분석과 표시 재개를 동시에 하면 컴파일 실패한 원문도 정상 문맥으로 보일 수 있습니다. 성공 Postfix에서 `__result != null`을 확인해야 합니다. 이 확인도 **컴파일 성공만** 의미하며 이후 `RunLuaFunction`의 실제 스토리 진행·코루틴 완료를 보장하지 않습니다. `RunLuaFunction`은 null closure면 callback 후 반환하고, coroutine 모드이면 다른 코루틴을 시작합니다.

### 제자리 복원의 특별 경로

`LuaManager.PrologueSave(int mode)`는 `mode == 1`일 때 `UpdateRestoreData`, 그 외에는 `LoadRestoreSaveData`를 호출합니다. 복원은 현재 Lua 실행 중 같은 manager들을 바꾸고 반환하며 새 `LoadLuaFunction`을 자동 호출하지 않습니다. 그러므로 안전하게 이전 문맥을 지우고 새 성공 Lua까지 표시를 억제하는 정책을 택하면 이 경로에서는 다음 실제 Lua까지 표시가 비어 있을 수 있습니다. 이를 게임 상태가 준비됐다는 임의 추정으로 자동 해제해서는 안 됩니다. 현재 Lua를 재분석하여 재개하려면 별도 설계·회귀 검증이 필요합니다.

같은 제한은 파일 없는 슬롯이나 빈 `_restoreData`에도 적용됩니다. 원본 로더가 실제로 자료를 바꾸지 않고 반환하더라도 후보는 진입 시 이전 문맥을 지우며, 반환만으로 이를 복원하지 않습니다. 후속 Lua 컴파일이 없으면 수동 새로고침으로도 대기를 해제할 수 없습니다. 무동작·실패 로드 뒤의 즉시 표시 재개 역시 남은 기능 한계이며, 후보를 완료로 판정하지 않는 이유에 포함합니다.

## 구현에 적용할 안전 조건과 macOS 회귀 기준

이 분석은 다음 조건을 구현 검토 기준으로 제안합니다. 실제 반영 여부·테스트 수는 `ISSUE-1-VALIDATION.md`에 기록합니다.

1. 확인된 새 게임·로드 함수와 공통 씬 시작 경계에서 먼저 조회 억제를 켜고 이전 행, 오류/재시도, Lua 원문·friendly name, 분석 결과, 이벤트 고정 캐시를 제거합니다. 같은 객체·같은 Lua도 새 세대로 취급합니다. 전환을 넘겨 유지할 이유가 없는 `TargetEvent`/`TargetMission` 등의 모드 요청도 함께 검토합니다.
2. 억제 중 자동 Tick·수동 갱신·탭 전환·켜기에서 `GetRate/GetResult/Check`, Lua 재분석, 이전 source 조회를 0회로 유지합니다. 실패·누락·부분 로드 후 이전 Lua로 복구하면 안 됩니다.
3. Lua Prefix에서는 이전 문맥을 무효화하고 새 원문을 임시 보관하되 정상 활성 문맥으로 확정하지 않습니다. non-null 반환의 Postfix에서만 새 고정 분석을 활성화합니다. null 반환·예외·누락 스크립트는 억제를 유지합니다. 표시 경계에서 원래 예외를 삼키거나 게임 성공으로 바꾸지 않습니다.
4. `ExecuteLoadGameData` Postfix나 scene coroutine factory Postfix에서 준비 완료를 선언하지 않습니다. 기존 게임의 `UpdateCheckMissions`, 난수, 예약, 저장, Lua 실행을 표시 준비용으로 추가 호출하지 않습니다.
5. 대역 검사에 동일 manager 재사용, 같은 Lua 재로드, public load 실패, inner load 도중 예외, null Lua 반환, 새 Lua 예외, 수동 갱신 중 차단, 다음 성공 Lua 복구를 포함합니다. 대역의 성공은 실제 Unity 실행 검증과 구분합니다.

현재 추가한 순수 상태 계층 `development/src/DiceMaster.LiveLog/LifecycleState.cs`는 manager에 접근하지 않고 invocation token으로 중첩 전환·Lua 단계를 구분합니다. `ExitTransition`은 토큰을 한 번만 제거하며 표시를 활성화하지 않습니다. 성공 Lua 토큰도 transition이 하나라도 살아 있으면 소비 후 차단 상태를 유지합니다. 오래된 완료·실패 콜백은 새 단계에 영향을 주지 않습니다. Harmony finalizer가 성공 Postfix 뒤에 다른 패치의 예외를 전달하는 경우에는 현재 Lua 토큰의 준비 상태만 취소할 수 있습니다.

`development/tests/LifecycleTests`를 macOS의 .NET 8에서 실행하여 4,023개 assertion을 통과했습니다. 초기 차단, 중첩/역순 전환, 중복·미지 finalizer, void load 반환 후 차단, 동일 객체 전환, null 컴파일, 예외 취소, 오래된 성공/실패 콜백, 전환 중 컴파일, 다음 성공 Lua 복구와 1,000회 반복을 검사했습니다. 이것은 순수 상태 계층의 검사이며 게임 실행·실제 Harmony 적용 성공의 증거가 아닙니다. `development/tools/LogRefreshInjector/LifecycleHooks.cs`에는 확인한 원래 메서드를 대상으로 Prefix/Finalizer와 Lua Prefix/Postfix/Finalizer를 생성하는 보조 코드를 추가했습니다. 전체 injector·bridge 연결 및 통합 검사 결과는 상위 검증 문서에 별도 기록합니다.

추가로 `LiveLogIntegrationTests`는 실제 후보 DLL의 lifecycle Prefix/Postfix/Finalizer 본문과 순수 조회 wrapper 본문을 UI·컴파일·게임 경계 대역으로 이식하여 직접 실행했습니다. 기존 parser/bridge/helper 검사와 합쳐 94개 assertion이 첫 패치와 반복 패치 모두 통과했습니다. 시작 시 UI·파서·이벤트 캐시·요청 제거, 모든 탭의 자동/수동 조회 차단, 같은 manager의 중첩 로드와 중복 finalizer, null/stale Lua, 이후 성공 Lua 복구, 성공 Postfix 뒤 예외 취소, 새 Lua 없는 제자리 복원의 대기 유지와 예외 객체 보존을 검사합니다. 실제 Unity/Harmony native 런타임을 실행한 결과로 기록하지 않습니다.

남은 실행 한계: macOS에 Windows Unity 실행 환경·게임 리소스·세이브가 없고, Windows 게임 실행은 승인되지 않았습니다. 실제 새 게임·로드·제자리 복원에서 위 후킹이 런타임에 적용되는지, 외부 Harmony 패치와의 상호작용, 실제 UI·게임 진행·성능은 미실행입니다. 이를 위해 Windows 실행을 수행하거나 요청하지 않았습니다.
