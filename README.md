# DiceMaster

활협전의 선택지 결과를 미리 확인하고 주사위 판정값을 직접 지정할 수 있는 DiceMaster 수정 배포본입니다.
기존 모드를 기반으로 한국어 표시를 정리하고, Xbox/XInput 패드의 십자키·왼쪽 스틱 조정을 추가했습니다.

**[공개 버전 다운로드](https://github.com/jungpaeng/dicemaster/releases)** · [변경 이력](CHANGELOG.md) · [개발 안내](docs/DEVELOPMENT.md) · [전체 소스 빌드 후보](docs/SOURCE-BUILD.md)

현재 개발은 [이슈 #3 PRD](https://github.com/jungpaeng/dicemaster/issues/3)의 전체 C# 소스·리소스 복원과 별도 소스 빌드 후보 검증입니다. 복원·비교 기준은 `v1.0.3-alpha.3`의 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`과 아래 게시 DLL로 고정합니다. 일반 소스 빌드는 기존 `DiceMaster.dll`을 입력으로 사용하지 않습니다. 기존 패치 공정과 배포 DLL은 Git 이력에서 재현하며, 자동 검사와 사용자 Windows 확인이 모두 끝나기 전에는 기본 빌드를 전환하거나 이슈 #3을 완료로 처리하지 않습니다. 이슈 #2의 기능 확장은 포함하지 않습니다. 명령·검사 범위·후보 식별은 [전체 소스 빌드 안내](docs/SOURCE-BUILD.md)에 기록합니다.

**[v1.0.3-alpha.4 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)를 2026-10-18 23:42:14 KST에 게시했습니다.** 사용자 승인에 따라 구조 변경을 commit·push하고 태그 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`의 `src/DiceMaster/DiceMaster.csproj`를 새로 빌드했습니다. 설치 파일은 `DiceMaster-v1.0.3-alpha.4.zip`이며 익명 공개 다운로드의 CRC·두 파일·소스 빌드 DLL/패키지 설정 바이트·SHA-256 일치를 확인했습니다. 태그의 루트 `plugins/DiceMaster/DiceMaster.dll`은 비교·복구 기준인 alpha.3 DLL로 보존하므로 새 설치 ZIP의 DLL과 다릅니다. 최종 검사·해시·게시 ID는 [이슈 #3 배포 기록](docs/ISSUE-3-RELEASE.md)에 기록했습니다. Latest 정식 `v1.0.2`, 기본 빌드와 Windows 확인 보류 조건은 유지합니다.

다음은 [이슈 #1 PRD](https://github.com/jungpaeng/dicemaster/issues/1)의 재설계 후보입니다. 사용자가 게임 RNG와 분리된 난수 숫자 갱신과 `v1.0.3-alpha.3` prerelease 게시를 승인했습니다. 화면에 부가 표기를 붙이지 않고, 모드 전용 `System.Random`으로 난수를 생성해 숫자와 조건 색상을 같은 평가 결과에서 만듭니다. 실제 게임 판정의 예측값은 아닙니다. alpha.3의 최종 macOS 자동 검사·패드→로그 왕복·반복 패치·직접 패드·두 파일 로컬 ZIP 검증을 마쳤습니다. 2026-10-18 22:43:45 KST에 [v1.0.3-alpha.3 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.3)를 배포 태그 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`에서 게시했습니다. 익명 공개 API·다운로드·CRC·정확히 두 파일·각 파일의 배포 커밋 바이트·ZIP/DLL SHA-256 일치를 검증했습니다. 게시 기록과 남은 검증 범위는 [검증 기록](docs/ISSUE-1-VALIDATION.md)에 구분해 기록합니다.

**기능 완료·정식 Release 공개는 보류합니다.** [`v1.0.3-alpha.2`](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.2)는 2026-10-18 21:52:10 KST, [`v1.0.3-alpha.1`](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.1)은 21:08:36 KST에 prerelease로 게시하고 각각 익명 공개 다운로드·배포 커밋 바이트 일치를 검증했습니다. 기존 Latest `v1.0.2`는 유지합니다. 실제 게임 DLL의 API·로드 경계 정적 분석과 macOS 자동 검사는 수행했지만 런타임·native Unity·실제 게임 리소스·사용자 Windows 검증은 미실행입니다. prerelease 게시 승인은 Windows 게임 실행·설치 검증 승인이 아닙니다. 현재 Windows PC는 기존 파일·정보 읽기만 허용되며 실행을 요청하지 않습니다. v1.0.2에는 선택지 결과 소실 문제가 있습니다.

## 공개 버전 설치 안내

아래는 기존 공개 버전의 참고 안내입니다. 현재 후보의 Windows 설치·게임 실행 요청이나 승인이 아닙니다.

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

설정 위치는 `config/DiceMaster.cfg`의 `[DiceMaster]` 섹션입니다. 기존 계약상 설정 변경은 다음 게임 시작 때 반영됩니다. 현재 단계에서 Windows 설정 변경·실행을 요청하지 않습니다.

| 설정 | 기본값 | 설명 |
|---|---|---|
| `EnableDiceController` | `true` | 주사위 수치 조정 |
| `EnableGamepadAdjustment` | `true` | 패드 조정. `EnableDiceController`도 켜져 있어야 합니다. |
| `EnableOptionInfo` | `true` | 선택지 결과 요약 및 로그 정보 자동 갱신 |
| `LogMenuToggleKey` | `Tab` | 로그 창 표시·숨김 키 |

## 기능과 개발 중인 로그 갱신

기존 DiceMaster는 선택지의 능력치·호감도 변화, 아이템 획득, 전투 진입 등의 정보를 표시하고 주사위 수치를 지정합니다. 이 배포본에는 한국어 번역, 십자키·왼쪽 스틱 패드 조정과 음수 지정 지원이 포함되어 있습니다. 게임 시작 시 로그 창은 숨겨져 있습니다.

선택지 옆 설명은 50자를 넘으면 말줄임표로 줄이며, 전체 설명은 Tab 로그 창의 선택지 탭에서 확인합니다. 결과는 Lua 스크립트에서 인식한 명령의 요약이므로 새로운 효과나 스크립트 형식에는 분석 규칙 보완이 필요할 수 있습니다.

다음은 이슈 #1 개발 후보의 동작 계약입니다. 현재 단계는 macOS의 정적 분석·개발·자동 검사이며 후보를 Windows에 설치하거나 실행하는 단계가 아닙니다.

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

## 공개 버전 업데이트와 향후 롤백 계획

prerelease 파일이 게시되었어도 현재 Windows PC의 후보 설치·실행 검증은 승인되지 않았습니다. 다음은 공개 버전에 관한 안내이며 현재 후보의 적용 요청이 아닙니다.

기존 설정을 유지하려면 게임 종료 후 DLL만 교체합니다. 이번 재설계는 설정 및 세이브 형식을 바꾸지 않습니다. 문제가 생기면 게임을 종료하고 이전 Release ZIP의 `plugins/DiceMaster/DiceMaster.dll`로 되돌립니다. 설치 전 DLL을 별도로 보관하면 같은 파일로 복원할 수 있습니다.

**v1.0.2로 되돌리면 자동 갱신 후 선택지 결과가 사라지는 문제가 다시 나타날 수 있습니다.** v1.0.1에는 자동 로그 갱신이 없으며, 이전 버전으로 돌아가면 해당 버전의 기능 범위가 적용됩니다. Windows에서 설정 유지 업데이트와 롤백을 실제 확인하기 전에는 이를 검증 완료로 기록하지 않습니다.

## 개발 및 빌드

루트의 `DiceMaster.sln`에서 전체 소스와 검사 프로젝트를 열 수 있습니다. 주 프로젝트는 `src/DiceMaster/DiceMaster.csproj`이며 Gamepad·LiveLog도 같은 프로젝트의 폴더로 통합했습니다. 검사는 `tests/`, 빌드·검증 명령은 `build/`, 복원·기존 패치 도구는 `tools/`, 설치 설정 원본은 `packaging/config/`에 있습니다. 생성 결과는 Git에서 제외한 `artifacts/`에 모읍니다.

Python 3와 `global.json`에 지정한 .NET 8 SDK를 준비한 뒤 저장소 루트에서 검증을 포함한 후보 빌드를 실행합니다. 이 명령은 기존 `DiceMaster.dll`을 입력으로 사용하지 않습니다.

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

전환 전 기존 패치 빌드는 `tools/LegacyPatch/`에서 비교·복구용으로 보존합니다. 별도 후보 출력 예시는 다음과 같습니다.

```sh
python3 tools/LegacyPatch/build-log-refresh.py \
  --game-managed '<읽기 전용 참조>/Mortal_Data/Managed' \
  --output artifacts/legacy-local/DiceMaster.dll
```

기존 패치 명령은 기본 입력을 고정 Git 기준에서 재생성하고, 기본 출력은 각각 `artifacts/legacy-candidate/gamepad/DiceMaster.dll`과 `artifacts/legacy-candidate/live-log/DiceMaster.dll`에 둡니다. 입력·출력이 분리되어 기준 DLL을 덮어쓰지 않습니다. 소스 구성과 검증 범위는 [개발 안내](docs/DEVELOPMENT.md)를 참고하세요.

## Release 등록

alpha.4는 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`의 전체 소스 프로젝트 빌드로 게시한 prerelease입니다. DLL은 461,312바이트 / SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`, 설치 ZIP은 191,482바이트 / SHA-256 `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8`입니다. Release ID `416503505`, asset ID `653700999`이며 공개 API·익명 다운로드 검증을 마쳤습니다. 후속 문서 커밋은 태그·자산을 바꾸지 않습니다. [상세 게시 기록](docs/ISSUE-3-RELEASE.md)을 참고하세요.

`v1.0.3-alpha.3`의 최종 DLL(462,336바이트, SHA-256 `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633`)과 두 파일 로컬 ZIP(191,369바이트, SHA-256 `d4a5bb2276a1d1089e02284286456f4ec16f5e41f8a8481ad6c23db0fde7a6b9`)을 검증했습니다. 2026-10-18 22:43:45 KST에 배포 태그 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`에서 prerelease로 게시하고 익명 공개 다운로드의 CRC·두 파일·태그/커밋 바이트·해시 일치를 검증했습니다. 게시 기록의 문서 후속 커밋은 태그·ZIP 기준을 바꾸지 않습니다. 이전 alpha.2는 배포 태그 커밋 `015c80c2fd9f905aaece574b3b11da52e501d432`에서 게시하고 익명 공개 API·다운로드·CRC·두 파일·커밋 바이트 일치를 검증했습니다. 이전 prerelease를 덮어쓰지 않고 기존 Latest `v1.0.2`를 유지합니다. Windows 실행·기능 완료·정식 Release 승인은 추가되지 않았습니다.

태그는 `vX.Y.Z`, ZIP 이름은 `DiceMaster-vX.Y.Z.zip` 형식입니다. 다음 두 파일만 폴더 구조를 유지해 포함합니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

개발 파일과 다른 모드는 포함하지 않습니다. alpha.4 ZIP의 DLL은 릴리스 태그 커밋의 소스 프로젝트를 빌드한 결과이며, 설정은 같은 커밋의 `packaging/config/DiceMaster.cfg`입니다. alpha.4 태그 당시 루트 `plugins/`의 기준 DLL을 ZIP에 복사하지 않았습니다. alpha.3 이하의 ZIP은 각 배포 커밋의 루트 배포 파일과 일치하는 과거 이력입니다. 현재 작업 트리에서 루트 두 폴더를 제거해도 기존 태그·게시 파일은 바뀌지 않습니다.
**`v1.0.3-alpha.1`은 배포 기준 커밋 `2d8936786845b534524b916537e7f1c3e5ab482d`에서 태그·두 파일 ZIP을 만들고 공개 다운로드 검증까지 마친 prerelease**입니다. 정식 Release는 실제 게임·사용자 Windows 검증까지 충족하기 전에는 공개하지 않습니다. prerelease는 기능 완료나 Windows 확인 완료로 처리하지 않으며 최신 정식 버전으로 지정하지 않습니다.
향후 공개 조건은 [Release 등록 안내](docs/RELEASING.md)를 참고하세요.

## 오류 제보

[Issues](https://github.com/jungpaeng/dicemaster/issues)에 모드·게임·모드 베이스 버전, 재현 순서, 패드 종류와 Steam Input 사용 여부, 관련 로그 또는 화면을 함께 남겨 주세요.

## 원본 및 사용 라이브러리

기존 DiceMaster를 기반으로 유지보수합니다. 원본 출처 확인 상태와 [MinHook](https://github.com/TsudaKageyu/minhook)·[Mono.Cecil](https://github.com/jbevain/cecil)의 사용 범위는 [개발 안내](docs/DEVELOPMENT.md#원본과-사용-라이브러리)에 기록합니다.
