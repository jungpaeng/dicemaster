# 개발 안내

[사용자 안내](../README.md) · [변경 이력](../CHANGELOG.md) · [Release 등록](RELEASING.md)

## 개발 범위

이 저장소는 기존 DiceMaster DLL을 기반으로 유지보수합니다.
`development/`에는 추가한 패드 지원 소스, 로그 갱신 패치 도구와 각각의 동작 테스트가 있습니다.
선택지 분석과 기존 번역 기능의 전체 C# 소스를 새로 빌드하는 프로젝트는 아닙니다.

빌드는 현재 `plugins/DiceMaster/DiceMaster.dll`을 입력으로 사용합니다. 패드 확장과 로그 갱신 패치를 각각 적용하며 다른 기능을 유지하는 방식입니다.

## 파일 구성

```text
README.md                              # 설치·조작·설정 요약
CHANGELOG.md                           # 배포 버전별 변경 이력
docs/
├── DEVELOPMENT.md                     # 개발·빌드·검증
└── RELEASING.md                       # Release 등록 절차
development/
├── src/DiceMaster.Gamepad/
│   ├── PadController.cs                # 조정·반복·입력 필터·안내 문구
│   ├── NativeXInput.cs                 # Windows XInput·MinHook 연결
│   └── native/                        # 내장할 x86/x64 라이브러리와 라이선스
├── tests/GamepadTests/                 # 입력 동작 검증
├── tests/LogRefreshFixtures/           # 게임·Unity 경계의 모의 구현
├── tests/LogRefreshTests/              # 실제 DLL 메서드의 로그 갱신 검증
├── tools/GamepadInjector/              # DLL 통합 도구
├── tools/LogRefreshInjector/           # 로그 갱신 IL 패치 도구
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

패드 코드는 .NET Framework 4.8을 대상으로 합니다. 참조 어셈블리는 `Microsoft.NETFramework.ReferenceAssemblies.net48` 패키지로 가져옵니다.
DLL 통합 도구와 테스트는 .NET 8을 대상으로 하며, 통합 도구는 Mono.Cecil 0.11.6을 사용합니다.

모드의 실제 패드 실행 대상은 Windows/XInput입니다. 다른 OS에서 입력 로직을 빌드·검증하는 것과 Windows 게임에서 실행하는 것은 별도의 확인 항목입니다.

## 빌드 방법

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

## 로그 정보 갱신

로그 창이 열려 있고 `EnableOptionInfo`가 켜져 있으면 `DicePlugin.RefreshLiveLogDisplay`가 게임의 일시정지 배율에 영향을 받지 않는 시간으로 0.25초 간격을 확인합니다.
`OptionInfoPatch.RebuildLiveLogEntries`는 마지막 Lua의 분석 캐시를 재사용하면서 네 탭의 표시 항목을 재구성합니다. 현재 조건값과 판정 색상은 다시 평가하고, `SetFlag`·`AddFlag`의 예정된 변경값은 Lua에 적힌 의미를 유지합니다.

창이 닫혀 있으면 주기적 갱신 함수는 즉시 반환합니다. 다만 게임의 Lua 로딩에 따른 기존 분석은 창의 표시 여부와 관계없이 동작합니다. Tab으로 창을 열 때도 이 두 처리 경로는 구분됩니다.

자동 갱신은 변경 알림을 받는 방식이 아니라 주기적으로 현재 정보를 조회하는 폴링입니다. 0.25초는 재계산 횟수를 제한하려고 선택한 값이며 게임·DebugMenu에서 가져온 기준이나 성능 실측 결과가 아닙니다. DebugMenu는 창이 열려 있을 때 GUI 호출마다 플래그의 현재 값을 읽으며 별도 0.25초 타이머를 사용하지 않습니다.

현재 구현은 매 갱신마다 네 탭의 목록과 설명 문자열을 생성하므로 임시 객체 할당·GC·조건 평가 비용이 발생합니다. 실제 Windows 게임의 CPU·메모리 부하는 아직 측정하지 않았습니다.

- `PositionEventCache.GetPositionEventDesc`: 고정 결과 설명만 캐시하며 동적 설명은 조회할 때마다 생성합니다.
- `UiTextMapper`: `GetFlag`가 들어 있는 문자열은 상태에 따라 설명이 바뀔 수 있으므로 번역 캐시 조회·저장에서 제외합니다.
- 수동 새로고침: 위치 이벤트 목록·이름·고정 설명·스크립트 접두사 캐시를 비운 뒤 Lua 분석과 표시를 재생성합니다.
- 빈 이벤트 목록: 게임 데이터가 늦게 준비되어도 다음 조회에서 다시 읽을 수 있게 보관하지 않습니다.
- 표시 평가 보호: `DisplayRefreshDepth`가 활성화된 동안 체크포인트 로그와 실행 예약을 소비하는 패치를 건너뜁니다. 특히 `GetRate()`를 표시 목적으로 호출해도 `TargetEvent`를 소비하지 않습니다.
- 실패 처리: 이전 네 탭의 목록을 복원하고, 자동 갱신 재시도 간격을 5초로 늘립니다. 보호 상태는 `finally`에서 복구합니다.

로그 패치만 재생성하려면 다음 명령을 실행합니다. 패드 빌드 도구와 같은 `--input`, `--output` 옵션을 지원합니다.

```sh
python3 development/tools/build-log-refresh.py
```

패드 빌드와 로그 패치는 서로의 변경을 유지합니다. 두 도구를 모두 실행해도 로그 갱신 호출이나 패드 호출이 중복 추가되지 않아야 합니다.

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

`build-log-refresh.py`는 패치한 DLL의 실제 메서드 본문을 테스트 어셈블리에 옮겨 실행합니다. 게임 데이터와 Unity 경계는 모의 구현으로 대체합니다.

- 플래그 변경 후 숫자·판정 색상·확률 재평가와 고정 설명 캐시 재사용
- 로그 창 표시·숨김, 설정 비활성화, 주기 제한과 재진입 방지
- 이벤트·미션 실행 예약 보존과 일반 실행 시 예약 소비
- 수동 새로고침, 빈 목록 재조회, 상태 의존 번역의 캐시 우회
- 오류 시 이전 목록 복원, 재시도 제한, 보호 상태 복구
- 실제 `Update`의 Tab 토글·일반 경로·화면 크기 변경에서 로그 갱신과 패드 호출 유지
- 자동 갱신의 Lua 캐시 재사용, 중복 호출 방지 등 IL 연결 검사

이 검사는 실제 게임 DLL의 조건 계산 구현이나 Windows 게임 실행을 검증하지 않습니다. 릴리스 전에는 게임 진행으로 플래그가 바뀌는 상황과 이벤트·미션 실행 기능도 게임 안에서 확인하세요.

### 기록된 검증 범위

v1.0.1 DLL에는 별도 비교로 다음을 확인했습니다.

- 직전 배포 DLL과 비교해 변경된 메서드는 패드 수치 조정 함수 1개
- 나머지 344개 메서드와 8개 내장 리소스 보존
- 입력 동작 및 정수 경계 검사 통과

v1.0.2 DLL에는 로그 관련 자동 검사 45개와 패드 검사 525,430개를 통과했습니다. 별도 구조 비교로 기존 메서드 중 로그 갱신과 관련한 13개만 변경되고 나머지 332개 및 내장 리소스 8개가 보존되었음을 확인했습니다. 로그 갱신용 메서드 4개를 추가했습니다.
로그 패치 반복 적용과 패드 재빌드 후의 동작 검사도 통과했습니다. 로그 검사의 게임·Unity 경계는 모의 구현이며 실제 Windows 게임에서의 재현 검증은 수행하지 않았습니다.

이 구조 비교는 매번 빌드 스크립트가 자동 실행하는 검사가 아닙니다. 기존 기능을 수정하는 작업에서는 관련 메서드와 번역·에셋·라이브러리 리소스가 의도대로 유지되는지도 따로 확인하세요.

### 실제 게임 확인

릴리스 설명에는 실제로 확인한 게임·모드 베이스 버전, 패드 환경, 검사한 동작을 적습니다.
게임 내 숫자 반영, 입력 중복, UI 배치, Steam Input과의 호환성은 실제 Windows 게임에서 확인합니다.
이 문서 작성 시점에는 테스트한 게임과 모드 베이스의 정확한 버전이 기록되어 있지 않습니다.

## 원본과 사용 라이브러리

이 저장소의 기반은 기존 DiceMaster DLL 배포본입니다. 외부 원본 배포 URL과 원제작자 정보는 아직 확인되지 않아 임의로 기재하지 않았습니다.

| 항목 | 용도 | 버전·출처 |
|---|---|---|
| MinHook | Windows XInput 후킹 | [1.3.4](https://github.com/TsudaKageyu/minhook/releases/tag/v1.3.4) |
| Mono.Cecil | DLL 통합 도구 | [0.11.6](https://github.com/jbevain/cecil) |

MinHook의 x86/x64 DLL과 전체 라이선스는 `development/src/DiceMaster.Gamepad/native/`에 보관합니다.
실행용 DiceMaster DLL에도 라이브러리와 `DiceMaster.Gamepad.Native.LICENSE.txt` 리소스를 내장합니다.
원본 출처를 확인하면 이 절과 사용자 README의 출처 안내를 함께 갱신하세요.
