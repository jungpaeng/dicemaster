# DiceMaster v1.0.5-alpha.3

**alpha.2에서 작은 빈 창만 보이는 회귀를 수정한 prerelease 후보**입니다. 사용자가 실제 Windows 게임에서 좌측 상단에 매우 작은 빈 창만 표시된다고 보고했습니다. alpha.3은 이 문제를 수정한 새 DLL이며 **실제 Windows 성공 여부는 아직 확인하지 않았습니다. [이슈 #4](https://github.com/jungpaeng/dicemaster/issues/4)는 미완료로 유지합니다.**

기존 prerelease 지시의 후속 수정으로 새 태그·설치 ZIP을 사용하고 기존 16개 Release·자산과 Latest 정식 [v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4)를 보존하는 범위입니다. 에이전트는 게임을 설치하거나 실행하지 않았습니다.

## 원인과 수정

alpha.2는 `GUILayout.Window` 안의 내용을 별도 `BeginArea`로 배치했습니다. Unity의 자동 배치 계산에서 창 루트가 비어 있는 것으로 처리돼, 지정한 너비·높이가 창 스타일의 패딩 크기로 대체되는 경로가 생겼습니다. 기본 1080p 스타일의 계산값은 **16×42px**로, 작은 빈 창이라는 보고와 일치합니다. 실제 창 위치가 정확히 `(0,0)`이었다고 단정하지 않습니다.

창 생성 호출을 고정 사각형을 사용하는 **5개 인자 `GUI.Window`**로 바꾸고 자동 크기용 옵션을 제거했습니다. 기존 본문 배치·해상도별 기본 크기·12~64px 글자·선택 추종·수동 스크롤 보존·창 전체 드래그·검색·패드 탐색·예약·설정 저장은 유지합니다. 상세 원인과 재검증 절차는 [창 축소 회귀 기록](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.3/docs/ISSUE-4-WINDOW-SIZE-REGRESSION.md)에 있습니다.

**이전 검사 대역은 입력한 창 사각형을 그대로 반환해 이 문제를 놓쳤습니다.** 관리 코드에서 반환 크기를 최소 540×440으로 보정하는 검사만으로는 이미 축소된 native 창의 클리핑을 찾을 수 없었습니다. 이번에는 그 차이를 검사하는 계약 모델과 실제 DLL 검사를 추가했습니다.

## 자동 검사와 남은 확인

전체 소스 빌드와 **11개 자동 검사 프로그램**을 통과했습니다. UI 렌더러·모델·자판 검사는 이전 11,773개에 67개를 추가한 **11,840개 단언**입니다.

- 관리 코드의 반환 사각형과 별개인 native 창 사각형·클리핑을 모델링했습니다. 빈 자동 배치 루트는 **16×42**, 수정한 고정 크기 경로는 **1200×860**을 유지하는 양쪽 사례를 검사합니다.
- 잘못된 창 처리로 바꾸면 회귀 검사가 실패하는지 확인했습니다. 공개 alpha.2의 실제 DLL은 새 창 API 검증에서 거부하고 수정 DLL은 **23개 검사**를 통과했습니다.
- Unity 공식 소스와 수신 참조 DLL의 IL을 읽기 전용으로 대조했습니다. 수신 게임 DLL·소스는 저장소나 설치 ZIP에 추가하지 않습니다.

이는 **Unity 계약을 반영한 관리 코드 검사와 DLL 구조 검사**입니다. 실제 Unity Layout→Repaint·글꼴·클리핑·마우스 포착·창 드래그·native XInput의 성공을 검증한 결과가 아닙니다. alpha.1의 크기·스크롤·포커스 실패와 alpha.2의 빈 창 실패는 실제 사용자 보고이며, alpha.3에서 해결됐는지는 Windows 재검증으로 확인해야 합니다.

먼저 기존 열기 키로 창을 열어 **본문 표시 → 네 탭 전환 → 닫기·다시 열기**가 되는지 확인해 주세요. 그다음 [회귀 재검증표](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.3/docs/ISSUE-4-WINDOW-SIZE-REGRESSION.md#windows-재검증표)에 따라 본문·목록/상세, 패드 탐색·스크롤, 창 드래그·컨트롤 우선 처리, 설정 복원을 확인합니다. 이어서 [기존 Windows 검증표](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.3/docs/ISSUE-4-WINDOWS-VALIDATION.md)의 모드 전환·재연결·Alt-Tab·홀드 상태 닫기·주사위 왕복·입력 중복도 확인합니다. 실제 패드 탐색·스크롤·버튼과 주요 회귀를 사용자가 확인하기 전에는 완료로 처리하지 않습니다.

## 적용과 복구

설치 파일은 **`DiceMaster-v1.0.5-alpha.3.zip`**입니다. GitHub의 자동 `Source code` 압축 파일과 구분해 주세요. ZIP에는 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함합니다.

1. 게임을 종료하고 현재 DLL과 `BepInEx/config/DiceMaster.cfg`를 게임 폴더 밖에 백업합니다.
2. ZIP을 임시 폴더에 풀고 해시를 확인한 뒤 **DLL만** `BepInEx/plugins/DiceMaster/DiceMaster.dll`에 교체합니다. 기존 설정을 ZIP의 기본 설정으로 덮어쓰거나 임의로 삭제하지 마세요.
3. alpha.2가 작은 창을 보정하면서 **540×440**을 설정에 저장했을 수 있습니다. 수정 DLL에서 창이 표시되면 **도구 → 창 위치·크기·글자 기본값 복구**를 사용하거나, 게임 종료 후 선택한 백업 설정을 복원합니다. 이 크기만으로 사용자 지정 여부를 구분할 수 없어 자동 초기화하지 않습니다.
4. 문제가 남으면 게임을 종료하고 백업 DLL과 필요한 설정을 복원합니다. 백업이 없으면 정식 [v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4) DLL로 돌아갈 수 있습니다. alpha.2에는 빈 창 회귀, alpha.1에는 이전 UI 문제가 남습니다. 세이브 형식은 변경하지 않습니다.

| 파일 | SHA-256 |
| --- | --- |
| `DiceMaster.dll` | `e432c5e118760a3d5d75eba9f0920db1ac7bb73bdc0aada3d91ea74e178b3fb9` |
| `DiceMaster-v1.0.5-alpha.3.zip` | `471119e2eb263843fe30b86f5a693b3ddafd0ff2fb51a8224993932f68b06af5` |
| ZIP 내부 `config/DiceMaster.cfg` | `2ed5841519cf3ef6719477b0e6b69de989c4b02c5e5fa883b303b786db2faafc` |

다운로드한 ZIP과 압축을 푼 폴더의 DLL을 Windows PowerShell로 대조합니다.

```powershell
Get-FileHash .\DiceMaster-v1.0.5-alpha.3.zip -Algorithm SHA256
Get-FileHash .\plugins\DiceMaster\DiceMaster.dll -Algorithm SHA256
```

로그 열기는 기존 `[DiceMaster].LogMenuToggleKey`(기본 **Tab**)이며 패드 열기 조합은 없습니다. 이벤트·미션 버튼은 즉시 실행을 보장하지 않는 기존 **예약** 기능입니다. 키보드·마우스 직접 상태 폴링은 보조 콜백 차단 범위 밖이고, 미확인 Steam Input 구성·모든 패드의 호환성은 보장하지 않습니다. 자세한 조작과 지원 경계는 [패드 안내](https://github.com/jungpaeng/dicemaster/blob/v1.0.5-alpha.3/docs/GAMEPAD.md)를 따릅니다.

결과에는 DLL 해시·해상도/배율/창 모드·게임/로더/패드/Steam Input 환경·재현 순서와 관련 BepInEx 로그를 남겨 주세요. 자동 검사와 prerelease 게시는 실제 게임 성공의 근거를 대신하지 않습니다.

## 게시 결과

**2026-10-19 04:24:38 KST에 [v1.0.5-alpha.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.3) prerelease를 게시했습니다.** 소스 태그 커밋은 `3b245130a1e6530e4b482d2a4965eb3456d6ec91`, Release ID는 `416651578`, asset ID는 `654282422`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 구현·후보 검증 문서 커밋을 푸시했고 고정 소스 스냅샷의 깨끗한 재빌드·11개 프로그램·UI 11,840/실제 DLL 연결 23 단언과 검토 DLL·설정·ZIP 해시 일치를 확인했습니다. 공개 설치 파일은 [DiceMaster-v1.0.5-alpha.3.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.3/DiceMaster-v1.0.5-alpha.3.zip)입니다.

Release·태그·보존 상태는 인증된 GitHub API로 확인했고 공개 페이지와 ZIP은 인증 없이 접근·다운로드했습니다. ZIP CRC·정확히 두 파일·DLL/설정 바이트·해시가 검증한 패키지와 일치합니다. 기존 16개 Release·태그 커밋·자산, Latest 정식 v1.0.4, 이슈 #2 closed와 #4 open을 보존했습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.3/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 빌드/검증 로그에 있습니다. 게시 결과를 기록하는 후속 문서 커밋은 태그·공개 자산을 바꾸지 않습니다. **실제 Windows 창 표시·패드 탐색·스크롤·버튼·드래그와 회귀 확인은 대기이며 이슈 #4를 완료 처리하지 않았습니다.**
