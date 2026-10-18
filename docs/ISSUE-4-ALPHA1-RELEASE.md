# DiceMaster v1.0.5-alpha.1

[PRD #4](https://github.com/jungpaeng/dicemaster/issues/4)의 로그 창 개편과 Xbox/XInput 패드 탐색을 확인하기 위한 **prerelease**입니다. 선행 PRD #2가 완료된 정식 [v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4)를 기반으로 합니다. **이번 후보의 Windows 게임 검증은 아직 진행하지 않았으며, 이슈 #4는 사용자 확인까지 열어 둡니다.**

## 바뀐 로그 창

- 선택지·조건·이벤트·미션 네 탭을 유지합니다. 선택지·조건은 여러 항목을 함께 펼쳐 비교하는 카드, 이벤트·미션은 검색 가능한 목록과 상세 영역으로 표시합니다.
- 자동 갱신 중 같은 항목의 선택·펼침·스크롤을 유지합니다. 대상이 사라지거나 검색에서 제외되면 상세·예약 초점을 취소합니다. 새 Lua·로드·새 게임·씬 전환에서는 이전 선택과 실행 초점을 취소합니다.
- 조회 대기·실패·이전 값과 검색 밖 오류를 구분합니다. 기존 한국어·마우스·키보드·읽음 표시·주사위·예약 및 PRD #2의 분석·갱신 계약을 유지합니다.
- 이벤트·미션 버튼을 **`이벤트 우선 예약`**, **`미션 조건 통과 예약`**으로 표시합니다. 기존 가중치/조건 통과 예약이며 즉시 실행이나 발생을 보장하지 않습니다. 최신 조회에 성공한 현재 대상만 예약할 수 있고, 별도 예약 취소 기능은 추가하지 않습니다.

## 패드 조작

로그 열기는 기존 `[DiceMaster].LogMenuToggleKey`를 사용하며 기본값은 **Tab**입니다. 패드로 로그를 여는 새 조합은 없습니다.

| 입력 | 동작 |
| --- | --- |
| LB / RB | 이전 / 다음 탭 |
| 십자키·왼쪽 스틱 상하 | 항목·컨트롤 이동 |
| 좌우 | 목록·카드와 상세 영역 이동 |
| A | 현재 초점의 펼침·진입·컨트롤 작동 |
| 오른쪽 스틱 | 본문 스크롤 |
| Y | 도구 영역 |
| X | 누를 때 한 번 수동 새로고침 |
| B | 도구에서 복귀 → 상세에서 목록 → 목록에서 닫기 |

검색·검색 지우기·새로고침·창 위치/크기·글자 크기·패드 소유자 재선택·예약·닫기에 패드로 접근할 수 있도록 구현했습니다. 화면 검색 자판은 한글 자모 조합, 영문·숫자·원본 ID 입력, 지우기·확정·취소를 제공합니다. 한글 표시·물리 키보드 IME·실제 패드 조작감은 Windows 확인 대상입니다.

유효한 게임 입력 모드를 우선하며, 모드를 확인할 수 없거나 값이 불명확하면 양쪽 입력을 지원합니다. `ControlSchemeType`의 0만으로 키보드·마우스 모드를 확정하지 않습니다. 입력 소유권은 `로그 > 주사위 조절 > 게임` 순서입니다. 닫기·모드 전환·포커스 상실·재연결 뒤에는 버튼 해제·스틱 중립·트리거 해제를 기다리며, 패드 소유자는 도구에서 명시적으로 다시 선택합니다.

## 저장되는 설정

기존 BepInEx 설정 파일의 `[DiceMaster]`에 다음 항목을 추가합니다.

| 키 | 기본값 | 범위·동작 |
| --- | --- | --- |
| `LogWindowX`, `LogWindowY` | 20, 20 | 화면 안으로 위치 보정 |
| `LogWindowWidth` | 750 | 최소 `min(540, 화면 너비)`, 최대 화면 너비 |
| `LogWindowHeight` | 750 | 최소 `min(440, 화면 높이)`, 최대 화면 높이 |
| `LogFontSize` | 14 | 희망 크기 12~28; 작은 창에서는 조작 영역 확보를 위해 표시 크기 축소 |

위치·크기·희망 글자 크기는 재시작 후 복원합니다. 검색·선택·펼침·스크롤은 실행 중에만 유지하며 설정에 저장하지 않습니다.

## 검사 결과와 남은 확인

전체 C# 소스로 새 DLL을 빌드했고 **11개 자동 검사 프로그램을 통과했습니다.** 실제 후보 DLL의 데이터 게시→UI 모델→렌더러·입력·예약 연결, 선택 유지/대상 소멸/오래된 예약 대상, 버튼 홀드·해제/중립, 모드 전환·재연결·Alt-Tab·주사위 왕복, 한글 조합을 검사했습니다. 수신 게임 DLL은 외부 읽기 전용 참조로만 사용하며 설치 ZIP에 포함하지 않습니다.

자동 검사는 실제 Windows 게임 실행 결과가 아닙니다. **native XInput/Harmony 적용, 패드 탐색·스크롤·버튼 동작, 게임 대화/취소/주사위와의 입력 중복, 한글 표시·레이아웃·성능은 아직 미확인입니다.** 실제 후킹 경로의 최근 게임 호출을 확인하지 못하면 위험한 패드 조작을 중지하도록 구현했습니다.

보조 입력 차단은 Unity Input System의 Started/Performed 콜백을 대상으로 합니다. **게임이나 다른 모드가 키보드·마우스 상태를 직접 폴링하는 경로는 이 콜백 차단 밖**입니다. XInput 패드는 별도 상태 필터를 사용합니다. Steam Input의 XInput 출력과 키보드·마우스 변환은 서로 다른 경로이므로, 미확인 Steam Input 구성과 모든 패드 환경의 호환성을 보장하지 않습니다.

PRD #2의 유한 후속 추적·부분 분석·동적/지원 밖 구문의 중립 표시와 동기 분석 비용 등 [v1.0.4의 알려진 한계](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.1/docs/ISSUE-2-STABLE-RELEASE.md#지원-범위)는 유지합니다. 이 후보로 해당 한계가 해소됐다고 판단하지 않습니다.

[단계별 Windows 검증표](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.1/docs/ISSUE-4-WINDOWS-VALIDATION.md)에 결과를 기록해 주세요. 실제 패드 탐색·스크롤·버튼 조작과 주요 회귀를 사용자가 확인한 뒤 완료 여부를 결정합니다. [구현·자동 검사 상세](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.1/docs/ISSUE-4-IMPLEMENTATION.md)와 [패드 안내](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.1/docs/GAMEPAD.md)도 참고할 수 있습니다.

## 설치와 복구

설치 파일은 **`DiceMaster-v1.0.5-alpha.1.zip`**입니다. GitHub의 자동 `Source code` 압축 파일과 구분해 주세요. 설치 ZIP에는 다음 두 파일만 있습니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

1. 게임을 종료하고 현재 DLL과 `BepInEx/config/DiceMaster.cfg`를 게임 폴더 밖에 백업합니다.
2. ZIP을 임시 폴더에 풉니다. **기존 설정을 유지하려면 DLL만** `BepInEx/plugins/DiceMaster/DiceMaster.dll`에 교체합니다. 새 로그 설정은 다음 시작 때 추가됩니다. ZIP의 설정 전체 교체는 기존 사용자 설정을 덮으므로 새 설치용으로 사용합니다.
3. 문제가 있으면 게임을 종료하고 백업 DLL을 복원합니다. 필요하면 설정도 복원합니다. 백업이 없으면 정식 [v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4)의 DLL로 돌아갈 수 있습니다. 이전 DLL은 새 로그 설정 키를 사용하지 않으며 세이브 형식은 변경하지 않습니다. 실제 업데이트·복구 동작은 Windows 검증표에서 확인합니다.

| 파일 | SHA-256 |
| --- | --- |
| `DiceMaster.dll` | `3ea62dd5c8fbee368438dd6e3ec8a58c023fcab5d01608321fe7e97ce80a6553` |
| `DiceMaster-v1.0.5-alpha.1.zip` | `3b6c3684f23f53d8dde7360d690aff544365e4c65248431bee6c1c7e1caccd0c` |

Windows PowerShell에서 `Get-FileHash .\DiceMaster-v1.0.5-alpha.1.zip -Algorithm SHA256`과 추출한 DLL의 `Get-FileHash` 결과를 대조할 수 있습니다. 문제를 보고할 때 DLL 해시, 게임/로더/패드/Steam Input/해상도, 재현 순서와 탭·항목 ID, 관련 BepInEx 최초 실패/복구 로그를 함께 남겨 주세요.

## 게시 결과

**2026-10-19 03:47:14 KST에 [v1.0.5-alpha.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.1)을 게시했습니다.** `draft=false`, `prerelease=true`, `make_latest=false`이며 소스 태그 커밋은 `f9f3b596db03372d219a4244c5c6a6fe90f9cc83`, Release ID는 `416633648`, asset ID는 `654210019`입니다.

고정 커밋만 추출한 새 소스 스냅샷에서 기존 모드 DLL·Git 데이터·이전 출력 없이 컴파일했습니다. `verify-preview-release.py --scope prd4`의 11개 검사와 검토 DLL/설정·ZIP 바이트/해시 비교를 통과했습니다. prerelease 경계 검증기의 회귀 검사8개도 통과했습니다. 게시 후 인증 없는 공개 API·다운로드로 ZIP CRC·정확히 두 파일·DLL/설정 바이트·각 해시·태그 커밋을 확인했습니다. 설치 ZIP은 [DiceMaster-v1.0.5-alpha.1.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.1/DiceMaster-v1.0.5-alpha.1.zip)입니다.

기존14개 Release와 자산의 ID/이름/크기/서버 digest·태그·설명을 보존했고 `/releases/latest`는 정식v1.0.4를 유지했습니다. 이슈 #4는 open이며 실제 Windows 확인·기능 완료·게임 실행을 주장하지 않습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.1/{release-verification,github-publication,anonymous-verification}.json`과 같은 폴더의 빌드/검증 로그에 있습니다. 이 게시 기록의 후속 문서 커밋은 소스 태그·공개 자산을 바꾸지 않습니다.
