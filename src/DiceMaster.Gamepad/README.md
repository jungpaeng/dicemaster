# DiceMaster 패드 조정

Windows에서 Xbox/XInput 패드로 주사위 수치를 조정합니다. 주사위 입력창이 활성화되고 게임 창에 포커스가 있을 때만 적용됩니다.

| 조작 | 동작 |
|---|---|
| LB를 누른 채 십자키 좌우 | −1 / +1 |
| LB를 누른 채 십자키 아래·위 | −10 / +10 |
| LB+B | 자동 주사위(0)로 복귀 |
| LB 놓기 | 조정 종료, 설정값 유지 |

방향키를 길게 누르면 0.35초 뒤부터 0.08초 간격으로 반복합니다. 0은 자동 모드이며, 패드로 설정하는 고정값은 1 이상입니다. 기존 모드와 같이 100을 넘는 양수도 지정할 수 있습니다. 주사위 창을 다시 열면 기존 동작대로 0으로 초기화됩니다.

A·X와 나머지 버튼·스틱·트리거를 처리하는 코드나 키 입력 에뮬레이션은 없습니다. XInput 상태에서 조정용 버튼 비트만 제외합니다. LB를 놓기 전에 누른 B·방향키는 해당 버튼도 놓을 때까지 제외하여 뒤늦은 게임 입력을 방지합니다.

## 설정

`config/DiceMaster.cfg`의 `[DiceMaster]`에 있는 `EnableGamepadAdjustment`로 켜고 끕니다. 기본값은 `true`입니다. `EnableDiceController = false`이면 패드 조정도 비활성화됩니다.

## 입력 처리

- XInputGetState와 XInput 1.3/1.4의 StateEx(ordinal 100)를 지원합니다.
- Windows 게임 프로세스 안에서만 입력을 필터링합니다. 다른 프로그램이나 시스템의 패드 설정은 변경하지 않습니다.
- 게임이 실제로 이 XInput 경로를 읽는 것이 확인된 경우에만 수치를 변경합니다. 다른 입력 경로를 사용하는 환경에서는 대기 안내를 표시하며 원래 수치를 유지합니다.
- MinHook 1.3.4의 x86/x64 라이브러리를 DiceMaster.dll 리소스에 내장합니다. 실행 시 BepInEx 캐시의 `DiceMaster/Gamepad/<해시>`에 해당 비트 수의 라이브러리를 추출합니다. 별도의 BepInEx 플러그인 설치는 필요하지 않습니다.
- 플러그인 종료 시 후킹을 해제합니다. 이미 실행 중인 콜백의 원본 호출이 끝날 수 있도록 라이브러리·트램펄린은 프로세스 종료까지 유지합니다.

게임 본체와 Windows 실행 환경이 없는 상태에서 입력 로직과 DLL 구조를 검사했습니다. 실제 게임에서 버튼 중복 동작, 주사위 UI 위치, 사용 중인 Steam Input 설정과의 호환성은 확인이 필요합니다.

## 소스와 빌드

- `PadController.cs`: 수치 조정, 반복 입력, 입력 비트 필터, 표시 상태.
- `NativeXInput.cs`: Windows XInput 연결과 MinHook 관리.
- `tools/GamepadInjector`: 기존 DLL에 코드를 내장하고 Update·OnGUI·주사위 창 초기화·설정·종료 지점에 연결합니다.
- `tests/GamepadTests`: 모든 65,536개 버튼 조합, A/X 보존, 버튼 해제 순서, 연결 해제, 반복 입력, 정수 경계 및 네이티브 구조체 크기를 검사합니다.

.NET 8 SDK와 Python 3가 필요합니다. 저장소 루트에서 다음을 실행합니다.

```sh
python3 tools/build-gamepad.py
```

이미 기능이 들어 있는 DLL도 재생성할 수 있습니다. 기존 패드 코드를 교체하며, 다른 기능과 번역은 유지합니다. 다른 경로에 결과를 만들려면 `--output`을 사용합니다.

```sh
python3 tools/build-gamepad.py --output /path/to/DiceMaster.dll
```

## 타사 라이선스

MinHook 1.3.4: https://github.com/TsudaKageyu/minhook/releases/tag/v1.3.4

공식 `MinHook_134_bin.zip`의 SHA-256은 `c9d723e18e8919a40a45cf35f3ac1f82936c480314225866424b9056c46f02b6`입니다. 라이선스 전체는 `native/MinHook.LICENSE.txt`와 DLL 내 `DiceMaster.Gamepad.Native.LICENSE.txt`에 포함됩니다.
