# DiceMaster

활협전의 선택지 결과를 미리 확인하고 주사위 판정값을 직접 지정할 수 있는 DiceMaster 수정 배포본입니다.
기존 모드를 기반으로 한국어 표시를 정리하고, Xbox/XInput 패드의 십자키·왼쪽 스틱 조정을 추가했습니다.

**[공개 버전 다운로드](https://github.com/jungpaeng/dicemaster/releases)** · [변경 이력](CHANGELOG.md) · [개발 안내](docs/DEVELOPMENT.md) · [전체 소스 빌드](docs/SOURCE-BUILD.md)

기본 개발·배포 경로를 전체 C# 소스 빌드로 전환하고 **[정식 v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)을 2026-10-19 00:10:53 KST에 게시했습니다.** [설치 ZIP 다운로드](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3/DiceMaster-v1.0.3.zip) · [PRD #3 완료](https://github.com/jungpaeng/dicemaster/issues/3). [이슈 #3 PRD](https://github.com/jungpaeng/dicemaster/issues/3)에 따라 모드 소스·리소스를 복원하고 Gamepad·LiveLog를 같은 프로젝트에 통합했습니다. 일반 빌드는 기존 `DiceMaster.dll`을 입력으로 사용하지 않습니다. 복원·비교 기준은 `v1.0.3-alpha.3`의 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`과 DLL 해시로 고정하며 기존 패치 공정은 비교·복구용으로 보존합니다.

2026-10-19 사용자가 공개 [alpha.4 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)을 직접 내려받아 Windows 검증을 완료했다고 확인했습니다. 확인 대상 DLL은 SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`입니다. 현재 빌드는 번역 주석 세 줄 정리를 포함하므로 DLL 전체 해시가 다릅니다. 사용자 확인 범위와 두 DLL의 비교, 자동 검사·정식 게시 결과는 [소스 빌드 안내](docs/SOURCE-BUILD.md)와 [배포 기록](docs/ISSUE-3-RELEASE.md)에 구분합니다.

이 전환은 기존 동작을 보존하는 소스 복원 범위입니다. [이슈 #1](https://github.com/jungpaeng/dicemaster/issues/1)·[이슈 #2](https://github.com/jungpaeng/dicemaster/issues/2)의 기능이나 상태를 추가 변경하지 않으며 기존 알려진 제한은 보존합니다. 게임 RNG와 분리한 모드 전용 `System.Random`의 숫자·색상·이벤트 확률은 화면 갱신 때 얻는 값이며 실제 게임 판정의 예측값은 아닙니다.

## 공개 버전 설치 안내

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

## 공개 버전 설정

설정 위치는 `config/DiceMaster.cfg`의 `[DiceMaster]` 섹션입니다. 기존 계약상 설정 변경은 다음 게임 시작 때 반영됩니다.

| 설정 | 기본값 | 설명 |
|---|---|---|
| `EnableDiceController` | `true` | 주사위 수치 조정 |
| `EnableGamepadAdjustment` | `true` | 패드 조정. `EnableDiceController`도 켜져 있어야 합니다. |
| `EnableOptionInfo` | `true` | 선택지 결과 요약 및 로그 정보 자동 갱신 |
| `LogMenuToggleKey` | `Tab` | 로그 창 표시·숨김 키 |

## 기능과 개발 중인 로그 갱신

기존 DiceMaster는 선택지의 능력치·호감도 변화, 아이템 획득, 전투 진입 등의 정보를 표시하고 주사위 수치를 지정합니다. 이 배포본에는 한국어 번역, 십자키·왼쪽 스틱 패드 조정과 음수 지정 지원이 포함되어 있습니다. 게임 시작 시 로그 창은 숨겨져 있습니다.

선택지 옆 설명은 50자를 넘으면 말줄임표로 줄이며, 전체 설명은 Tab 로그 창의 선택지 탭에서 확인합니다. 결과는 Lua 스크립트에서 인식한 명령의 요약이므로 새로운 효과나 스크립트 형식에는 분석 규칙 보완이 필요할 수 있습니다.

다음은 alpha.3에서 이어지는 로그 갱신의 동작 계약입니다. 소스 빌드 전환은 이 계약이나 이슈 #1·#2의 범위를 확대하지 않습니다.

- `EnableOptionInfo=true`이면 열린 로그 창의 **현재 탭**만 0.25초 주기로 평가합니다. 창이 닫혀 있거나 탭이 보이지 않으면 표시용 현재값 조회를 중단합니다. 게임 Lua 로딩에 따른 분석과 선택지 옆 요약은 유지합니다.
- 창 재열기·탭 전환 시 정상 항목은 즉시 갱신합니다. 현재 조건값·판정 색상·확률은 다시 읽고, `SetFlag`·`AddFlag`의 예정된 값은 Lua 명령의 의미를 유지합니다.
- 한 항목이 실패해도 다른 항목은 계속 갱신합니다. 실패 항목은 창과 해당 탭이 보일 때 5초 간격으로 재시도하며, 창 재열기·탭 전환으로 대기를 초기화하지 않습니다.
- 새 게임·로드·씬 전환이 시작되면 이전 정보와 재시도 상태를 지우고, 새 Lua가 실제로 컴파일되어 non-null 결과를 반환할 때까지 현재값 조회와 수동 갱신을 차단합니다. 제자리 복원 뒤 새 Lua가 없으면 다음 성공 Lua까지 `게임 데이터 대기 중` 상태가 유지될 수 있습니다.
- **게임 데이터 새로고침**은 문맥이 준비된 경우 고정 분석 캐시를 다시 만들고 현재 탭을 즉시 표시합니다. 실패 항목도 대기를 건너뛰고 재시도하며, 다른 탭은 열 때 새 분석 결과를 사용합니다.

| 상태 문구 | 의미와 복구 |
|---|---|
| `게임 데이터 대기 중` | 게임 데이터가 아직 준비되지 않았습니다. 해당 탭이 보일 때 다시 확인합니다. |
| `읽기 실패` | 첫 조회가 실패해 표시할 정상 값이 없습니다. 자동 재시도 또는 준비된 문맥의 수동 새로고침 대상입니다. |
| `갱신 실패 · 이전 값` | 마지막 정상 값을 유지하고 있습니다. 현재 게임 상태와 다를 수 있으며, 복구하면 새 값으로 바뀌고 문구가 사라집니다. |

확률·조건의 원래 게임 API는 난수 소비와 조건 실행 카운터 변경을 일으킬 수 있어, 알려진 모드 설명 경로에는 게임 상태를 쓰지 않는 별도 평가를 적용합니다. alpha.3은 모드 전용 `System.Random`으로 숫자를 생성하며 게임 RNG와 `_count`는 건드리지 않습니다. 개별 `ConditionResultItem` 설명에서는 숫자와 색상이 같은 평가 결과를 사용하고, 이벤트 설명에서는 헤더·활성 조건·모든 확률 항목이 한 평가 결과를 공유합니다. 미션·스위치의 서로 다른 조건 항목은 별도로 평가하므로 같은 참조도 다시 추첨할 수 있습니다. 화면의 난수 숫자·색상·난수에 의존하는 이벤트 확률은 해당 갱신에서 얻은 값이며 실제 게임 실행 결과를 보장하지 않습니다. UI에는 `표시용` 같은 부가 문구를 붙이지 않습니다. alpha.1의 난수 행 실패 정책과 alpha.2의 범위·판정 미정·미지원·제한적 이론 확률은 [변경 이력](CHANGELOG.md)에 보존합니다. 지원하지 않는 자료·순환 참조와 실제 조회 오류는 기존 오류 상태로 구분합니다. 정상적으로 항목이 없는 경우에는 기존 빈 목록 표시를 유지합니다. 새 설정은 추가하지 않으며 `EnableOptionInfo=false`로 설정하고 재시작하면 정보 표시와 자동 갱신을 끕니다.

진단은 기존 BepInEx 로그를 사용합니다. 같은 게임 세션·항목·원인의 연속 실패는 최초 1회, 복구는 1회 기록하는 것이 목표입니다. 제보에는 관련 탭·항목, 재현 순서와 해당 오류·복구 로그를 포함하고 전체 Lua 원문이나 세이브 내용은 첨부하지 않아도 됩니다. 실제 적용 범위와 미확인 API는 [개발 안내](docs/DEVELOPMENT.md)와 [실제 게임 API 분석](docs/REAL-GAME-API-FINDINGS.md)·[문맥 전환 분석](docs/GAME-LIFECYCLE-ANALYSIS.md)을 확인하세요.

## 공개 버전 업데이트와 롤백

기존 설정을 유지하려면 게임 종료 후 DLL만 교체합니다. 이번 재설계는 설정 및 세이브 형식을 바꾸지 않습니다. 문제가 생기면 게임을 종료하고 이전 Release ZIP의 `plugins/DiceMaster/DiceMaster.dll`로 되돌립니다. 설치 전 DLL을 별도로 보관하면 같은 파일로 복원할 수 있습니다.

**v1.0.2로 되돌리면 자동 갱신 후 선택지 결과가 사라지는 문제가 다시 나타날 수 있습니다.** v1.0.1에는 자동 로그 갱신이 없으며, 이전 버전으로 돌아가면 해당 버전의 기능 범위가 적용됩니다. 사용자의 alpha.4 Windows 완료 확인은 받았으며 환경·항목별 상세 기록은 제공되지 않았습니다.

## 개발 및 빌드

루트의 `DiceMaster.sln`에서 전체 소스와 검사 프로젝트를 열 수 있습니다. 주 프로젝트는 `src/DiceMaster/DiceMaster.csproj`이며 Gamepad·LiveLog도 같은 프로젝트의 폴더로 통합했습니다. 검사는 `tests/`, 빌드·검증 명령은 `build/`, 복원·기존 패치 도구는 `tools/`, 설치 설정 원본은 `packaging/config/`에 있습니다. 생성 결과는 Git에서 제외한 `artifacts/`에 모읍니다.

Python 3와 `global.json`에 지정한 .NET 8 SDK를 준비한 뒤 저장소 루트에서 기본 소스 빌드를 실행합니다. 이 명령은 기존 `DiceMaster.dll`을 입력으로 사용하지 않습니다.

```sh
python3 build/build-source.py \
  --references '<읽기 전용 참조 루트>'
```

새 DLL·설치 ZIP·검사 영수증은 `artifacts/source-candidate/`에 함께 생성됩니다. 일반 C# 빌드는 다음과 같습니다. 이 명령의 DLL은 `artifacts/bin/DiceMaster/release/DiceMaster.dll`이며 설치 ZIP이나 호환성 검사 보고서를 만들지는 않습니다.

```sh
dotnet build DiceMaster.sln -c Release \
  -p:GameReferences='<읽기 전용 참조 루트>'
```

전체 명령과 외부 참조 계약은 [소스 빌드 안내](docs/SOURCE-BUILD.md)를 따릅니다. 루트 `config/`와 `plugins/`는 제거했습니다. 패키지 설정은 `packaging/config/DiceMaster.cfg`에서 읽고, 비교·복원용 기준 DLL은 고정 Git 커밋에서 SHA-256을 검증하여 `artifacts/baselines/v1.0.3-alpha.3/DiceMaster.dll`에 재생성합니다. 일반 소스 빌드는 Git이나 이 기준 DLL을 사용하지 않습니다.

기존 모드 DLL·Git 데이터 없는 소스 스냅샷 빌드, 일곱 자동 검사, 기준 비교와 Release ZIP 검증을 함께 실행하려면 다음 명령을 사용합니다. 로컬 후보만 만들며 GitHub에 게시하지 않습니다.

```sh
python3 build/verify-release.py \
  --references '<읽기 전용 참조 루트>' \
  --output-dir artifacts/release-candidate
```

기존 패치 빌드는 `tools/LegacyPatch/`에서 비교·복구용으로 보존합니다. 기본 제품 빌드는 위의 전체 소스 빌드이며, 이전 공정의 별도 후보 출력 예시는 다음과 같습니다.

```sh
python3 tools/LegacyPatch/build-log-refresh.py \
  --game-managed '<읽기 전용 참조>/Mortal_Data/Managed' \
  --output artifacts/legacy-local/DiceMaster.dll
```

기존 패치 명령은 기본 입력을 고정 Git 기준에서 재생성하고, 기본 출력은 각각 `artifacts/legacy-candidate/gamepad/DiceMaster.dll`과 `artifacts/legacy-candidate/live-log/DiceMaster.dll`에 둡니다. 입력·출력이 분리되어 기준 DLL을 덮어쓰지 않습니다. 소스 구성과 검증 범위는 [개발 안내](docs/DEVELOPMENT.md)를 참고하세요.

## Release 등록

정식 [v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)은 태그 커밋 `ced38df9f0560a212559e34fd281fddbd2581ff4`의 전체 소스 프로젝트를 새로 빌드하고 같은 커밋의 `packaging/config/DiceMaster.cfg`와 함께 패키징했습니다. `draft=false`, `prerelease=false`로 게시하여 Latest 정식 버전으로 지정했으며, 공개 다운로드의 CRC·정확히 두 파일·DLL/설정 바이트·SHA-256을 확인했습니다. [이슈 #3](https://github.com/jungpaeng/dicemaster/issues/3)은 2026-10-19 00:11:02 KST에 완료로 닫았습니다. [상세 게시 기록](docs/ISSUE-3-RELEASE.md)을 참고하세요.

설치 DLL은 461,312바이트 / SHA-256 `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1`, [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3/DiceMaster-v1.0.3.zip)은 191,442바이트 / SHA-256 `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7`입니다. Release ID는 `416518288`, asset ID는 `653759221`입니다. 기존 일곱 Release·자산과 alpha.4 태그는 보존했습니다.

태그는 `vX.Y.Z`, ZIP 이름은 `DiceMaster-vX.Y.Z.zip` 형식입니다. 다음 두 파일만 폴더 구조를 유지해 포함합니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

게임 DLL·게임 소스·개발 파일과 다른 모드는 포함하지 않습니다. 저장소 루트의 `config/`·`plugins/` 사본은 필요하지 않습니다. 일반 빌드·패키징은 소스·리소스·패키지 설정을 사용하고, 비교·복원용 alpha.3 기준 DLL만 고정 Git 이력에서 재생성합니다.

이전 [alpha.4 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)는 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`의 소스 빌드입니다. DLL SHA-256은 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`, ZIP SHA-256은 `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8`입니다. alpha.3 이하의 ZIP은 각 배포 커밋의 루트 배포 파일과 일치하는 과거 이력입니다. 기존 태그·Release·설치 자산은 덮어쓰지 않습니다.

## 오류 제보

[Issues](https://github.com/jungpaeng/dicemaster/issues)에 모드·게임·모드 베이스 버전, 재현 순서, 패드 종류와 Steam Input 사용 여부, 관련 로그 또는 화면을 함께 남겨 주세요.

## 원본 및 사용 라이브러리

기존 DiceMaster를 기반으로 유지보수합니다. 원본 출처 확인 상태와 [MinHook](https://github.com/TsudaKageyu/minhook)·[Mono.Cecil](https://github.com/jbevain/cecil)의 사용 범위는 [개발 안내](docs/DEVELOPMENT.md#원본과-사용-라이브러리)에 기록합니다.
