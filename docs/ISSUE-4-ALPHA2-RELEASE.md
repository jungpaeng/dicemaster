# DiceMaster v1.0.5-alpha.2

[PRD #4](https://github.com/jungpaeng/dicemaster/issues/4) 로그 UI의 크기·스크롤·패드 포커스 문제와 창 전체 드래그를 수정한 **prerelease 후보**입니다. 사용자는 alpha.1의 실제 Windows 게임에서 **UI가 너무 작음**, **목록·상세의 스크롤 영역이 낮음**, **패드 포커스를 스크롤이 따라가지 않음**을 보고했습니다. 아래 수정은 그 보고에 대한 새 구현이며, **alpha.2의 실제 Windows 재검증은 아직 진행하지 않았습니다. 이슈 #4는 미완료로 유지합니다.**

사용자 지시에 따른 별도 prerelease이며, 기존 prerelease·자산과 Latest 정식 [v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4)를 보존하는 게시 범위입니다. 에이전트는 게임을 설치하거나 실행하지 않았습니다.

## 이번 수정

- **해상도에 맞는 기본 크기와 글자:** 화면 높이에 따라 기본 창과 본문 글자를 확대합니다. 본문 글자 범위는 **12~64px**이며 설정한 크기를 작은 창에서 임의로 축소하지 않습니다.
- **남은 본문 높이 사용:** 제목·탭 등 고정 조작 영역을 제외한 높이를 목록·상세·카드·도구·검색 자판에 배정합니다. 작은 창·큰 글자에서 공간이 부족하면 안내·검색을 스크롤 본문으로 옮깁니다.
- **실제 표시 영역으로 포커스 추종:** 실제 스크롤 뷰포트와 선택 행의 좌표로 표시 범위를 계산합니다. 긴 카드는 헤더를 따라갑니다. 휠·스크롤바·오른쪽 스틱으로 직접 읽는 동안 자동 갱신이 선택 행으로 되돌리지 않고, 방향 탐색으로 선택을 바꾸면 따라가기를 재개합니다.
- **창 전체 드래그 복원:** 제목·본문 라벨·빈 공간에서 창을 끌 수 있도록 복원했습니다. 버튼·검색 입력란·스크롤바가 먼저 입력을 처리하며, 카드 헤더·목록 행에서는 선택·펼침을 우선합니다.

| 해상도 | 기본 창 크기 | 기본 본문 글자 |
| --- | --- | --- |
| 1920×1080 | 1200×860 | 20px |
| 2560×1440 | 1600×1147 | 27px |
| 3840×2160 | 2400×1720 | 40px |

배율은 `clamp(화면 높이 / 1080, 0.85, 4)`이며 창은 화면 안으로 제한합니다. 기존 설정 키 `LogWindowX`, `LogWindowY`, `LogWindowWidth`, `LogWindowHeight`, `LogFontSize`를 계속 사용하고 새 영구 설정 키는 추가하지 않습니다.

수정하지 않은 alpha.1 기본값 **`(X=20, Y=20, 너비=750, 높이=750, 글자=14)`**와 패키지 기준값 **`(20, 20, 1200, 860, 20)`**만 현재 화면의 기본값으로 이행합니다. 그 밖의 사용자 지정 위치·크기·글자는 물리 픽셀 값으로 유지합니다. 기본값과 정확히 같은 값을 수동 지정한 경우는 구분할 수 없습니다. 기존에 창을 옮기거나 글자만 바꿔 자동 이행되지 않으면 **도구 → 창 위치·크기·글자 기본값 복구**를 사용하세요. 위치·크기·글자는 재시작 후 복원하고 검색·선택·펼침·스크롤은 실행 중에만 유지합니다.

## 기존 조작과 지원 경계

선택지·조건의 복수 펼침 카드, 이벤트·미션의 검색 목록/상세, 한글·영문·숫자 검색 자판, 기존 한국어·마우스·키보드·읽음 표시·주사위·분석·예약 계약을 유지합니다. 로그 열기는 기존 `[DiceMaster].LogMenuToggleKey`이며 기본값은 **Tab**입니다. 패드 열기 조합은 없습니다.

LB/RB는 탭, 십자키·왼쪽 스틱은 항목·컨트롤 이동, 좌우는 영역 이동, A는 선택·펼침·작동, 오른쪽 스틱은 본문 스크롤, Y는 도구, X는 누를 때 한 번 새로고침입니다. B는 도구에서 복귀·상세에서 목록·목록에서 닫기 순서입니다. 도구에서 검색·지우기·크기 설정·기본값 복구·패드 소유자 재선택·닫기에 접근합니다.

`이벤트 우선 예약`과 `미션 조건 통과 예약`은 기존 가중치/조건 통과 **예약**이며 즉시 실행이나 발생을 보장하지 않습니다. 최신 조회에 성공한 현재 대상만 예약할 수 있습니다. 대상 소멸·새 문맥에서는 이전 선택과 실행 초점을 취소하며 새 예약 취소 기능은 추가하지 않습니다.

유효한 게임 입력 모드를 우선하고 모드가 불명확하면 양쪽 입력을 지원합니다. 입력 소유권은 `로그 > 주사위 조절 > 게임`이며 닫기·모드 전환·포커스 상실·재연결 뒤 버튼 해제·스틱 중립·트리거 해제를 기다립니다. 실제 XInput 후킹 경로의 최근 게임 호출을 확인하지 못하면 위험한 패드 동작을 중지하도록 구현했습니다.

보조 입력 차단은 Unity Input System의 Started/Performed 콜백을 대상으로 합니다. **게임·다른 모드가 키보드·마우스 상태를 직접 폴링하는 경로는 이 콜백 차단 밖**입니다. XInput 패드는 별도 상태 필터를 사용합니다. Steam Input의 XInput 출력과 키보드·마우스 변환은 별도 경로이며 미확인 구성·모든 패드의 호환성을 보장하지 않습니다. PRD #2의 유한 후속 추적·부분 분석·지원 밖 구문의 중립 표시·동기 분석 비용 등 [v1.0.4의 알려진 한계](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.2/docs/ISSUE-2-STABLE-RELEASE.md#지원-범위)도 유지합니다.

## 검사 결과와 Windows 재검증

전체 C# 소스로 새 DLL을 빌드하고 **11개 자동 검사 프로그램을 통과했습니다.** UI 렌더러·모델·검색 자판 검사는 **11,773개 단언**이며 실제 후보 DLL의 게시·UI·입력 연결 검사도 포함합니다. 게임 DLL은 읽기 전용 외부 참조이며 설치 ZIP에 포함하지 않습니다.

레이아웃 검사는 Unity 대역에서 호출 영역과 콘텐츠 좌표를 확인한 것입니다. 대역의 창 드래그는 무동작이므로 실제 이동이나 컨트롤과의 마우스 포착 우선순위를 검사하지 못합니다. **실제 Unity 글꼴·줄바꿈·클리핑·마우스 포착·창 이동·native XInput의 성공을 뜻하지 않습니다.** alpha.1의 세 실패는 실제 사용자 보고이고, alpha.2의 수정 성공은 Windows에서 아직 확인하지 않았습니다.

먼저 [UI 재검증 순서](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.2/docs/ISSUE-4-UI-FOLLOWUP.md#windows-재검증)에 따라 기본 크기 이행·사용자 설정 보존, 남은 본문 높이, 패드 첫↔마지막 항목과 긴 카드 추종, 직접 스크롤 후 탐색 재개, 창 전체 드래그와 컨트롤 우선 처리를 확인해 주세요. 작은 창·32/64px 글자·재시작 복원도 포함합니다.

이어서 [Windows 검증표](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.2/docs/ISSUE-4-WINDOWS-VALIDATION.md)의 검색·예약·대상 소멸·홀드 상태 닫기·모드 전환·재연결·Alt-Tab·주사위 왕복·입력 중복을 확인합니다. 실제 패드 탐색·스크롤·버튼 조작과 주요 회귀를 사용자가 확인한 뒤 완료 여부를 결정합니다.

## 설치와 복구

설치 파일은 **`DiceMaster-v1.0.5-alpha.2.zip`**입니다. GitHub의 자동 `Source code` 압축 파일과 구분해 주세요. 설치 ZIP에는 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함합니다.

1. 게임을 종료하고 현재 DLL과 `BepInEx/config/DiceMaster.cfg`를 게임 폴더 밖에 백업합니다.
2. ZIP을 임시 폴더에 풉니다. **기존 설정을 유지하려면 DLL만** `BepInEx/plugins/DiceMaster/DiceMaster.dll`에 교체합니다. 설정 전체 교체는 기존 사용자 설정을 덮으므로 새 설치용으로 사용합니다.
3. 문제가 있으면 게임을 종료하고 백업 DLL을 복원합니다. alpha.2가 기본값을 이행·저장했을 수 있으므로 이전 창 설정도 되돌리려면 백업 설정을 함께 복원합니다. 백업이 없으면 정식 [v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4)의 DLL로 돌아갈 수 있습니다. 세이브 형식은 변경하지 않습니다. alpha.1로 복구하면 보고된 UI 문제가 남습니다.

| 파일 | SHA-256 |
| --- | --- |
| `DiceMaster.dll` | `ad9aa4b460382c81ae72e80596b3b12b5d6583976a8bfa5100ab60d68c2a6646` |
| `DiceMaster-v1.0.5-alpha.2.zip` | `ae6fbec2d25b8c4d6ce795250a5381cbc03601bd2da1f837f727184051231788` |
| ZIP 내부 `config/DiceMaster.cfg` | `2ed5841519cf3ef6719477b0e6b69de989c4b02c5e5fa883b303b786db2faafc` |

Windows PowerShell에서 다운로드한 ZIP과 압축을 푼 DLL을 대조합니다.

```powershell
Get-FileHash .\DiceMaster-v1.0.5-alpha.2.zip -Algorithm SHA256
Get-FileHash .\plugins\DiceMaster\DiceMaster.dll -Algorithm SHA256
```

결과에는 DLL 해시·해상도/배율/창 모드·게임/로더/패드/Steam Input 환경·재현 순서·탭/항목 ID와 관련 BepInEx 최초 실패/복구 로그를 남겨 주세요. 자동 검사 통과와 prerelease 게시는 Windows 기능 완료의 근거로 대신하지 않습니다.

## 게시 결과

**2026-10-19 04:13:07 KST에 [v1.0.5-alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.2) prerelease를 게시했습니다.** 태그 소스 커밋은 `389292a85469c046032d498561a3470387429f61`, Release ID는 `416646044`, asset ID는 `654259429`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 고정 커밋의 깨끗한 재빌드·11개 검사·검토 DLL/설정/ZIP 해시 비교와 검증기 회귀 8개를 통과했습니다. 설치 파일은 [DiceMaster-v1.0.5-alpha.2.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.2/DiceMaster-v1.0.5-alpha.2.zip)입니다.

익명 API 조회 한도 때문에 Release·태그·기존 자산·Latest·이슈 메타데이터는 인증된 GitHub API로 확인했습니다. 공개 페이지와 ZIP은 인증 없이 접근·다운로드했고 ZIP CRC·정확히 두 파일·DLL/설정 바이트·SHA-256이 검증한 결과와 일치했습니다. 기존 15개 Release·태그 커밋·자산을 보존했고 Latest는 정식 v1.0.4, 이슈 #2는 closed, #4는 open을 유지합니다. 영수증은 `artifacts/releases/v1.0.5-alpha.2/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 빌드/검증 로그입니다. 이 게시 기록을 반영하는 후속 문서 커밋은 태그와 공개 설치 자산을 바꾸지 않습니다.

Windows 게임 설치·실행·UI/native 입력 성공이나 PRD #4 완료를 주장하지 않습니다. 실제 패드 탐색·스크롤·버튼·창 이동과 회귀는 사용자가 재검증해야 합니다.
