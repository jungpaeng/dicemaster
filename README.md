# DiceMaster

활협전의 선택지 결과를 미리 확인하고 주사위 판정값을 직접 지정할 수 있는 DiceMaster 수정 배포본입니다.
기존 모드를 기반으로 한국어 표시를 정리하고, Xbox/XInput 패드의 십자키·왼쪽 스틱 조정을 추가했습니다.

**[최신 버전 다운로드](https://github.com/jungpaeng/dicemaster/releases/latest)** · [변경 이력](CHANGELOG.md) · [개발 안내](docs/DEVELOPMENT.md)

## 설치 방법

**BepInEx가 포함된 활협전용 모드 베이스를 먼저 설치해야 합니다.**
Release ZIP에는 DiceMaster의 설정과 DLL만 포함됩니다. 패드 조작은 Windows의 Xbox/XInput 환경을 기준으로 합니다.

1. 게임을 종료합니다.
2. [Releases](https://github.com/jungpaeng/dicemaster/releases)의 **Assets**에서 `DiceMaster-vX.Y.Z.zip`을 다운로드합니다.
3. ZIP의 내용을 게임의 기존 `BepInEx` 폴더에 압축 해제합니다.

```text
게임 설치 폴더/
└── BepInEx/
    ├── config/
    │   └── DiceMaster.cfg
    └── plugins/
        └── DiceMaster/
            └── DiceMaster.dll
```

기존 버전에서 설정을 유지하며 업데이트하려면 **DLL만 교체**하세요. 설정 변경이 필요한 버전은 릴리스 설명에 안내합니다.
GitHub가 제공하는 `Source code` 압축 파일은 저장소 전체를 받는 용도입니다. 설치에는 Assets의 `DiceMaster-vX.Y.Z.zip`을 사용하세요.

## 조작 방법

주사위 창에서 숫자를 직접 입력하거나 패드로 조정할 수 있습니다.

| 조작 | 동작 |
|---|---|
| LB + 십자키/왼쪽 스틱 왼쪽·오른쪽 | −1 / +1 |
| LB + 십자키/왼쪽 스틱 아래·위 | −10 / +10 |
| LB+B | 자동 주사위인 0으로 복귀 |
| LB 놓기 | 조정 종료, 선택한 값 유지 |
| Tab | 로그 창 표시·숨김 |

- **0은 게임의 원래 주사위 굴림에 맡기는 값**입니다.
- 음수와 게임의 일반적인 주사위 상한을 넘는 값도 지정할 수 있습니다.
- 방향키를 누르거나 스틱을 기울인 채 유지하면 반복 조정됩니다. A·X는 기존 게임 동작을 유지합니다.
- 주사위 창을 다시 열면 값이 0으로 초기화됩니다.

패드 조정은 주사위 창이 활성화되고 게임 창에 포커스가 있을 때 동작합니다.

## 설정

`config/DiceMaster.cfg`의 `[DiceMaster]` 섹션을 수정한 뒤 게임을 다시 실행하세요.

| 설정 | 기본값 | 설명 |
|---|---|---|
| `EnableDiceController` | `true` | 주사위 수치 조정 |
| `EnableGamepadAdjustment` | `true` | 패드 조정. `EnableDiceController`도 켜져 있어야 합니다. |
| `EnableOptionInfo` | `true` | 선택지 옆 결과 요약 |
| `LogMenuToggleKey` | `Tab` | 로그 창 표시·숨김 키 |

## 기능 및 변경 사항

기존 DiceMaster는 선택지의 능력치·호감도 변화, 아이템 획득, 전투 진입 등의 정보를 표시하고 주사위 수치를 지정하는 기능을 제공합니다.
이 배포본에서는 다음을 추가·정리했습니다.

- 한국어 번역 처리를 DiceMaster DLL에 통합
- 십자키·왼쪽 스틱을 이용한 패드 조정과 음수 지정 지원
- 게임 시작 시 로그 창 기본 숨김
- 모드 베이스 중복 파일을 정리하고 실행용 파일만 Release로 배포

선택지 옆 설명은 50자를 넘으면 말줄임표로 줄이며, 전체 설명은 Tab 로그 창의 선택지 탭에서 확인할 수 있습니다.
결과 표시는 Lua 스크립트에서 인식한 명령의 요약입니다. 기존 형식의 새 선택지는 자동으로 읽지만, 새로운 효과나 스크립트 형식에는 분석 규칙 보완이 필요할 수 있습니다.

## 개발 및 빌드

추가한 패드 지원 소스·테스트·DLL 통합 도구는 `development/`에서 관리합니다.
Python 3와 .NET 8 SDK를 준비한 뒤 저장소 루트에서 실행합니다.

```sh
python3 development/tools/build-gamepad.py
```

기존 DiceMaster DLL에 패드 지원 코드를 통합하고, 검증을 통과하면 `plugins/DiceMaster/DiceMaster.dll`을 갱신합니다.
소스 구성과 검증 범위는 [개발 안내](docs/DEVELOPMENT.md)를 참고하세요.

## Release 등록

태그는 `vX.Y.Z`, ZIP 이름은 `DiceMaster-vX.Y.Z.zip` 형식입니다. 다음 두 파일만 폴더 구조를 유지해 포함합니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

개발 파일과 DebugMenu 등 다른 모드는 포함하지 않습니다. ZIP과 릴리스 태그는 동일한 커밋을 기준으로 만듭니다.
**검증 → 커밋·푸시 → ZIP 생성 → 초안 업로드 → 다운로드 검증 → 공개** 순서로 진행합니다.
상세 명령과 확인 방법은 [Release 등록 안내](docs/RELEASING.md)를 참고하세요.

## 오류 제보

[Issues](https://github.com/jungpaeng/dicemaster/issues)에 모드·게임·모드 베이스 버전, 재현 순서, 패드 종류와 Steam Input 사용 여부, 관련 로그 또는 화면을 함께 남겨 주세요.

## 원본 및 사용 라이브러리

기존 DiceMaster를 기반으로 유지보수합니다. 원본 출처 확인 상태와 [MinHook](https://github.com/TsudaKageyu/minhook)·[Mono.Cecil](https://github.com/jbevain/cecil)의 사용 범위는 [개발 안내](docs/DEVELOPMENT.md#원본과-사용-라이브러리)에 기록합니다.
