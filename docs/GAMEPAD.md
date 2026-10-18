# DiceMaster 패드 조정

[사용자 안내](../README.md) · [개발·빌드 안내](DEVELOPMENT.md) · [Release 등록](RELEASING.md)

Windows에서 Xbox/XInput 패드로 주사위 수치를 조정합니다. 주사위 입력창이 활성화되고 게임 창에 포커스가 있을 때만 적용됩니다.

| 조작 | 동작 |
|---|---|
| LB를 누른 채 십자키/왼쪽 스틱 좌우 | −1 / +1 |
| LB를 누른 채 십자키/왼쪽 스틱 아래·위 | −10 / +10 |
| LB+B | 자동 주사위(0)로 복귀 |
| LB 놓기 | 조정 종료, 설정값 유지 |

방향키를 누르거나 왼쪽 스틱을 기울인 채 유지하면 0.35초 뒤부터 0.08초 간격으로 반복합니다. 0은 자동 모드입니다. 패드에서도 음수와 100을 넘는 양수를 지정할 수 있습니다. 좌우는 ±1, 상하는 ±10을 그대로 적용하므로 1에서 왼쪽은 0, 아래는 −9가 됩니다. 0에서 계속 감소하면 음수로 내려갑니다. 정수 범위를 벗어나는 경우에만 최솟값·최댓값에서 멈춥니다. 주사위 창을 다시 열면 기존 동작대로 0으로 초기화됩니다.

왼쪽 스틱은 충분히 기울여야 입력을 시작하고, 중앙 부근의 작은 흔들림은 무시합니다. 대각선은 더 크게 기울인 축을 따르며, 십자키와 스틱을 함께 쓰면 십자키를 우선합니다.

스틱 방향은 절댓값이 16,000 이상이면 새 입력으로 판단하고, 같은 방향을 유지할 때는 10,000 이상이면 유지합니다. 두 축이 모두 ±8,000 안으로 들어오면 중앙으로 판단합니다. 두 축의 절댓값이 같으면 가로 방향을 선택합니다.

A·X와 오른쪽 스틱·트리거는 원래 입력을 유지하며 키 입력 에뮬레이션은 없습니다. 조정 중에는 XInput에서 조정용 버튼과 왼쪽 스틱 입력을 제외합니다. LB를 먼저 놓아도 B·방향키는 해당 버튼을 놓을 때까지, 왼쪽 스틱은 중앙으로 돌아올 때까지 제외하여 뒤늦은 게임 입력을 방지합니다.

## 설정

`config/DiceMaster.cfg`의 `[DiceMaster]`에 있는 `EnableGamepadAdjustment`로 켜고 끕니다. 기본값은 `true`입니다. `EnableDiceController = false`이면 패드 조정도 비활성화됩니다.

## 입력 처리

- XInputGetState와 XInput 1.3/1.4의 StateEx(ordinal 100)를 지원합니다.
- Windows 게임 프로세스 안에서만 입력을 필터링합니다. 다른 프로그램이나 시스템의 패드 설정은 변경하지 않습니다.
- 게임이 실제로 이 XInput 경로를 읽는 것이 확인된 경우에만 수치를 변경합니다. 다른 입력 경로를 사용하는 환경에서는 대기 안내를 표시하며 원래 수치를 유지합니다.
- MinHook 1.3.4의 x86/x64 라이브러리를 DiceMaster.dll 리소스에 내장합니다. 실행 시 BepInEx 캐시의 `DiceMaster/Gamepad/<해시>`에 해당 비트 수의 라이브러리를 추출합니다. 별도의 BepInEx 플러그인 설치는 필요하지 않습니다.
- 플러그인 종료 시 후킹을 해제합니다. 이미 실행 중인 콜백의 원본 호출이 끝날 수 있도록 라이브러리·트램펄린은 프로세스 종료까지 유지합니다.

자동 검사는 입력 로직과 DLL 구조를 대상으로 합니다. Windows 네이티브 후킹, 게임 안에서의 숫자 반영과 UI 위치, Steam Input 호환성은 실제 게임에서 별도로 확인하고 릴리스 설명에 검사 범위를 기록합니다.

## 소스와 빌드

패드 소스는 `src/DiceMaster/Gamepad/`에서 전체 모드와 같은 프로젝트로 관리합니다. 주 프로젝트가 다음 두 파일을 한 번씩 컴파일하며 기준 DLL에서 추출한 중복 구현은 포함하지 않습니다.

- `PadController.cs`: 수치 조정, 스틱 방향·중립 판정, 반복 입력, 버튼·스틱 입력 필터, 표시 상태.
- `NativeXInput.cs`: Windows XInput 연결과 MinHook 관리.
- `tests/GamepadTests`: 모든 65,536개 버튼 조합, A/X 보존, 버튼 해제 순서, 연결 해제, 스틱의 경계값·입력 차단, 반복 입력, 0 및 정수 경계와 네이티브 구조체 크기를 검사합니다.

루트 솔루션과 .NET 8 SDK로 전체 C# 소스를 빌드합니다. 자동 검사를 포함한 새 후보는 다음 명령으로 `artifacts/source-candidate/`에 생성합니다. 전체 참조·복원·호환성 검사는 [전체 소스 빌드 안내](SOURCE-BUILD.md)를 따릅니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>'
```

보존한 `tools/LegacyPatch/GamepadInjector/`는 같은 패드 소스를 링크하여 기존 DLL에 Update·OnGUI·주사위 창 초기화·설정·종료 지점을 연결하는 비교·복구 도구입니다. 기존 명령은 새 소스 프로젝트와 별도입니다. 기본 입력은 고정 Git 커밋에서 재생성·해시 확인한 alpha.3 DLL이고 기본 출력은 `artifacts/legacy-candidate/gamepad/DiceMaster.dll`이므로 기준을 덮어쓰지 않습니다. 다른 후보 위치에는 `--output`을 지정합니다.

```sh
python3 tools/LegacyPatch/build-gamepad.py --output artifacts/legacy-local/DiceMaster.dll
```

이미 기능이 들어 있는 DLL에서도 패드 코드를 교체하며, 다른 기능과 번역은 유지합니다. 일반 빌드 산출물과 중간 파일은 `artifacts/`에 모으고 Git에서 제외합니다. 예전 `development/releases/` 기록은 과거 검증 증거로 보존합니다.

## 타사 라이선스

[MinHook 1.3.4](https://github.com/TsudaKageyu/minhook/releases/tag/v1.3.4)

공식 `MinHook_134_bin.zip`의 SHA-256은 `c9d723e18e8919a40a45cf35f3ac1f82936c480314225866424b9056c46f02b6`입니다. 라이선스 전체는 [`src/DiceMaster/Resources/native/MinHook.LICENSE.txt`](../src/DiceMaster/Resources/native/MinHook.LICENSE.txt)와 DLL 내 `DiceMaster.Gamepad.Native.LICENSE.txt`에 포함됩니다.
