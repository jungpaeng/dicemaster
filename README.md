# DiceMaster

활협전의 선택지 결과를 미리 확인하고 주사위 판정값을 직접 지정할 수 있는 DiceMaster 수정 배포본입니다.
기존 모드를 기반으로 한국어 표시를 정리하고, Xbox/XInput 패드의 십자키·왼쪽 스틱 조정을 추가했습니다.

**[공개 버전 다운로드](https://github.com/jungpaeng/dicemaster/releases)** · [변경 이력](CHANGELOG.md) · [개발 안내](docs/DEVELOPMENT.md) · [전체 소스 빌드](docs/SOURCE-BUILD.md)

## PRD #4 — v1.0.5-alpha.3 빈 로그 창 수정

alpha.2에서 작은 빈 창만 표시된다는 실제 Windows 실패가 보고되었습니다. 창의 자동 크기 계산이 `BeginArea` 내용을 포함하지 않아 축소되는 원인을 확인하고 고정 크기 창 API로 수정했습니다. [원인·검사 보강·설정 복구·Windows 재검증](docs/ISSUE-4-WINDOW-SIZE-REGRESSION.md)을 따릅니다. 전체 소스 빌드·11개 검사와 이전 잘못된 창 호출을 거부하는 회귀 검사를 통과했고 [alpha.3 설치·복구·해시](docs/ISSUE-4-ALPHA3-RELEASE.md)를 준비했습니다. 고정 커밋 `3b24513`의 깨끗한 재빌드까지 통과한 로컬 후보이며 공개 게시 전입니다. 실제 게임 표시 성공은 아직 확인하지 않았습니다.

## PRD #4 — v1.0.5-alpha.2 UI 후속

alpha.1에서 보고된 작은 UI·좁은 본문·선택 항목 스크롤 추적 문제를 수정하고 **[v1.0.5-alpha.2 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.2)를 2026-10-19 04:13:07 KST에 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.2/DiceMaster-v1.0.5-alpha.2.zip). 조활 시스템의 화면 높이 배율을 참고해 기본 창/본문을 **1080p 1200×860·20px, 1440p 1600×1147·27px, 4K 2400×1720·40px**로 맞춥니다. 본문은 탭·검색·안내를 제외한 남은 높이를 채우고, 실제 스크롤 영역으로 선택 항목을 따라갑니다. 본문 글자 설정을 창 크기로 다시 줄이지 않습니다.

변경하지 않은 alpha.1 기본값은 새 기본 배치로 옮기며 사용자 지정 위치·크기·글자는 유지합니다. 사용자 지정 배치에서도 도구의 `창 위치·크기·글자 기본값 복구`로 새 기본값을 적용할 수 있습니다. 제목뿐 아니라 본문·빈 공간을 끌어서 창을 옮기던 기존 동작도 복원합니다. [alpha.2 설치·복구·해시](docs/ISSUE-4-ALPHA2-RELEASE.md)와 [수정·검사 기록](docs/ISSUE-4-UI-FOLLOWUP.md)을 확인하세요. 새 후보의 실제 Unity 배치와 Windows 게임 확인은 대기 중입니다. 아래 alpha.1 게시 파일은 당시 버전입니다.

## 이전 PRD #4 — v1.0.5-alpha.1 prerelease

로그 화면을 선택지·조건의 복수 펼침 카드와 이벤트·미션의 검색 목록/상세로 개편했습니다. 기존 `LogMenuToggleKey`(기본 Tab)로 열며 패드 열기 조합은 없습니다. 열린 창은 LB/RB 탭, 십자키/왼쪽 스틱 이동, A 선택, 오른쪽 스틱 본문 스크롤, Y 도구, X 새로고침, B 복귀/닫기를 사용합니다. 도구에서 검색/한글 화면 키보드·지우기·위치/크기/글자 크기·닫기와 패드 소유자 재선택에 접근합니다.

창 설정만 기존 BepInEx 설정에 저장합니다. 검색·선택·펼침·스크롤은 실행 중 상태이며 새 문맥의 이전 실행 초점은 취소됩니다. 게임 입력 모드가 유효하면 우선 사용하고 불명일 때 양쪽 입력을 지원합니다. 패드 조작은 실제 XInput 차단 경로가 확인돼야 동작하며 로그가 주사위보다 우선합니다. 버튼은 기존 기능의 의미에 맞춰 `이벤트 우선 예약`·`미션 조건 통과 예약`으로 표시하고 즉시 발생을 보장하지 않습니다.

사용자 지시에 따라 **[v1.0.5-alpha.1 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.1)를 2026-10-19 03:47:14 KST에 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.1/DiceMaster-v1.0.5-alpha.1.zip) · [게시 기록](docs/ISSUE-4-ALPHA1-RELEASE.md). 고정 커밋 재빌드·11개 검사·공개 다운로드/해시 검증을 통과했으며 Latest 정식은 v1.0.4입니다. [적용·복구 및 Windows 검증표](docs/ISSUE-4-WINDOWS-VALIDATION.md), [입력 상세와 미확인 범위](docs/GAMEPAD.md)를 확인하세요. **실제 Windows 패드 탐색·스크롤·버튼과 한글 가독성은 확인 대기이며 이슈 #4는 미완료**입니다. 아래 공개 버전의 과거 검증을 새 후보의 결과로 승계하지 않습니다.

기본 개발·배포 경로를 전체 C# 소스 빌드로 전환하고 **[정식 v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)을 2026-10-19 00:10:53 KST에 게시했습니다.** [설치 ZIP 다운로드](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3/DiceMaster-v1.0.3.zip) · [PRD #3 완료](https://github.com/jungpaeng/dicemaster/issues/3). [이슈 #3 PRD](https://github.com/jungpaeng/dicemaster/issues/3)에 따라 모드 소스·리소스를 복원하고 Gamepad·LiveLog를 같은 프로젝트에 통합했습니다. 일반 빌드는 기존 `DiceMaster.dll`을 입력으로 사용하지 않습니다. 복원·비교 기준은 `v1.0.3-alpha.3`의 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`과 DLL 해시로 고정하며 기존 패치 공정은 비교·복구용으로 보존합니다.

2026-10-19 사용자가 공개 [alpha.4 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)을 직접 내려받아 Windows 검증을 완료했다고 확인했습니다. 확인 대상 DLL은 SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`입니다. 정식 v1.0.3 빌드는 번역 주석 세 줄 정리를 포함하므로 DLL 전체 해시가 다릅니다. 사용자 확인 범위와 두 DLL의 비교, 자동 검사·정식 게시 결과는 [소스 빌드 안내](docs/SOURCE-BUILD.md)와 [배포 기록](docs/ISSUE-3-RELEASE.md)에 구분합니다.

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

## v1.0.4-alpha.3 prerelease — 선택지 미리보기

[설치 ZIP 다운로드](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.4-alpha.3/DiceMaster-v1.0.4-alpha.3.zip) · [검사·게시 기록](docs/ISSUE-2-IMPLEMENTATION.md). 2026-10-19 01:14:16 KST에 게시하고 공개 다운로드를 검증했습니다. 정식 Latest는 v1.0.3으로 유지합니다.

[v1.0.4-alpha.3 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.3)는 불확실한 현재 효과를 `가능한 변화`, 후속 효과를 `이후 가능한 변화` 아래에 묶습니다. `조건부`를 효과마다 반복하지 않으며, `+5 가산`은 `+5`로 줄입니다. 직접 지정하는 효과의 `10으로 설정`과 초기화 같은 별도 의미는 유지합니다. 실제 같은 조건문의 인접 효과는 `[경우 A]` / `[경우 B · 그 외]`로 묶고 조건문이 여러 개이거나 중첩되면 번호를 함께 표시합니다. Tab 로그에는 전체 설명, 선택지 옆에는 기존 50자 요약을 표시합니다. 설치 파일과 검사 범위는 [alpha.3 게시 설명](docs/ISSUE-2-ALPHA3-RELEASE.md)에 있습니다.

alpha.1에서 추가한 16개 StatModifyManager 함수와 12개 LuaManager 함수, 상수 사칙연산과 조건부·후속 효과 분석을 유지합니다. 일부 결과를 해석하지 못하면 `추가 영향 가능`, 확인된 효과 없이 미해석 부분만 있으면 `결과는 진행 후 확인`으로 안내합니다. center·mental은 안전하게 현재값을 읽을 수 있을 때 예상량을 표시하고, 선행 변경·분기·후속 실행이 불확실하면 의미와 조건을 설명합니다. `SetNextScript`는 다음 이야기 지정이며 즉시 실행을 뜻하지 않습니다. 로컬 리터럴 연결은 다음 사용자 선택까지만 분석합니다. 변수·동적 호출·루프 실행과 변수 대입 추론은 지원하지 않습니다.

분기 색상은 갈래를 구분하며 현재 조건의 충족 여부를 뜻하지 않습니다. 후속 연결 자체가 능력치 조건이라는 뜻도 아닙니다. 능력치·플래그·미션·이야기 경로 등 어떤 조건인지를 임의로 추론하지 않습니다. 개발 안전 한도와 검사·후보 해시는 [PRD 2 구현 기록](docs/ISSUE-2-IMPLEMENTATION.md)에 적습니다. 실제 Lua 대조·최종 추적 제한·이번 DLL의 Windows 확인은 남아 있으며 정식 v1.0.3에는 이 기능이 포함되지 않습니다.

설정은 기존 `EnableOptionInfo`를 사용합니다. 제보는 후보 DLL 해시, 선택지·스크립트 식별자, 재현 순서, 최초 오류/복구 로그만으로 시작하며 전체 Lua·세이브는 진단 로그에 남기지 않습니다. 후보 설치나 Windows 실행을 이번 작업에서 수행하거나 요청하지 않습니다.

## v1.0.4-alpha.5 prerelease — 분석 공유와 현재 조건 힌트

2026-10-19 사용자가 이번 버전의 Windows 테스트 완료를 확인해 [PRD #2](https://github.com/jungpaeng/dicemaster/issues/2)를 완료로 닫았습니다. [완료 기록](docs/ISSUE-2-COMPLETION.md)에 사용자 확인과 기존 자동 검사·배포 검증의 범위를 구분합니다. 아래 설명의 조건·추적 지원 경계와 배포 당시 이력은 유지합니다.

[alpha.5 게시 설명](docs/ISSUE-2-ALPHA5-RELEASE.md)의 대상입니다. 공통 본문의 구문·인자·상수 분석도 유한 캐시로 공유하며 무거운 사례의 Mac 대역 추출 시간이 약 484→336ms, 전체 검사 시간이 평균 약 4.4% 줄었습니다. 현재 조건을 안전하게 읽을 수 있는 분기에만 `현재 조건 충족`/`현재 조건 미충족`과 녹색/회색을 덧붙입니다. 선행 변경·미확인 콜백·후속 이야기·입증하지 못한 바깥 문맥은 기존 중립 표시를 유지합니다. 현재 제공된 실제 자료의 선택지는 모두 미입증 문맥 경계에 걸려 중립 표시를 유지합니다. 안전한 문맥의 기능과 자동 검사는 구현했지만 실제 게임의 색상 적용 범위는 미확인입니다. 미래 실행 결과를 보장하는 표시는 아닙니다. [구현·검사와 적용 제한](docs/ISSUE-2-SHARED-ANALYSIS.md)은 공개 alpha.4와 구분합니다. 2026-10-19 03:18:01 KST에 [alpha.5 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.5)를 게시하고 고정 커밋 재빌드·공개 다운로드·해시·CRC를 확인했습니다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.4-alpha.5/DiceMaster-v1.0.4-alpha.5.zip)을 제공합니다. 정식 Latest는 v1.0.3을 유지합니다.

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
