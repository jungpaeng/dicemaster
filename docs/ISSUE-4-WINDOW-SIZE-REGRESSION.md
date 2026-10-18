# PRD #4 alpha.2 창 축소 회귀 — Windows 재검증 대기

2026-10-19 KST. 사용자가 `v1.0.5-alpha.2`의 실제 Windows 게임에서 **좌측 상단에 매우 작은 빈 창만 보인다**고 보고했다. 이는 alpha.2의 실게임 실패이며, 이전 빌드·자동 검사 통과로 대체하지 않는다. 실제 위치가 정확히 `(0,0)`이었다고 단정할 근거는 없다.

이번 로컬 수정은 창을 만드는 API를 고정 크기용 `GUI.Window`로 바꾸는 최소 변경이다. Windows 재검증은 아직 하지 않았고 이슈 #4도 미완료다. 에이전트는 게임을 설치하거나 실행하지 않았다. 이 문서는 새 버전의 게시 성공 기록이 아니다.

## 원인: 비어 있는 자동 배치 루트

alpha.2는 `GUILayout.Window`에 너비·높이 옵션을 전달하면서 탭·상태·검색·본문·하단 안내를 모두 `GUILayout.BeginArea`로 분리했다. 기존 [Windows 자료 수집 기록](REAL-GAME-API-FINDINGS.md)은 Unity `2020.3.49f1`을 식별한다. 이는 과거 수집 자료이며 현재 수신한 macOS 참조 DLL과 버전이 같다는 뜻이나 새 게임 실행 결과가 아니다. Unity 2020.3 공식 소스의 빈 그룹·별도 영역·고정/자동 창 처리 계약을 우선 확인하고, 2021.3의 동일 경로도 보조 대조했다.

| 단계 | 공식 소스의 동작 | 이번 코드에 미친 영향 |
| --- | --- | --- |
| `BeginArea` | 영역을 루트의 일반 자식 대신 별도 `current.windows` 목록에 등록 | 자동 크기를 계산하는 창 루트의 일반 자식이 0개가 됨 |
| `CalcWidth` / `CalcHeight` | 자식이 없으면 기존 크기 옵션을 덮고 스타일의 가로·세로 패딩으로 최소·최대 크기를 설정한 뒤 반환 | `Width/Height` 옵션을 전달했어도 창이 패딩 크기로 축소 |
| `LayoutedWindow` / `LayoutSingleGroup` | `isWindow=true`인 자동 크기 창은 계산 범위로 실제 native 창을 제한하고 `Internal_MoveWindow` 호출 | 작은 native 창 경계가 별도 본문 영역을 가림 |

기본 1080p 조작 글자 `20px`에서 창 스타일의 패딩은 `(왼쪽8, 오른쪽8, 위34, 아래8)`이다. 이 소스 계약으로 계산되는 빈 루트 크기는 **16×42px**이며, 작은 빈 창이라는 보고와 일치한다. 코드는 native 창의 기존 `x/y`를 사용하므로 이 경로가 위치를 `(0,0)`으로 바꾼다고 설명하지 않는다.

주근거는 Unity 2020.3의 [별도 영역 등록](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/GUILayoutUtility.cs#L335), [빈 그룹 너비 계산](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/LayoutGroup.cs#L148), [빈 그룹 높이 계산](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/LayoutGroup.cs#L370), [native 창 크기 반영](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/GUILayoutUtility.cs#L263)이다. `BeginArea`의 크기 지정과 `LayoutedWindow` 래퍼는 함께 읽은 2021.3의 [영역 크기 지정](https://github.com/Unity-Technologies/UnityCsReference/blob/2021.3/Modules/IMGUI/GUILayout.cs#L267), [자동 크기 창 설정](https://github.com/Unity-Technologies/UnityCsReference/blob/2021.3/Modules/IMGUI/GUILayout.cs#L377)을 보조 근거로 남긴다. 공식 소스 분석은 실제 Windows native 실행 검증과 구분한다. 또한 읽기 전용 수신 `UnityEngine.IMGUIModule.dll`(SHA-256 `e84bcf2d25a2bc56b3f31361009b46509482da06279f6780dca8cec1fb91db12`)의 관리 IL에서도 고정 `GUI.Window`의 `forceRect=true`, 자동 창의 `isWindow=true`, 빈 그룹의 패딩 크기 덮어쓰기와 native 창 이동 분기를 확인했다. 원본 DLL은 수정하지 않았고 게임/Unity native를 실행한 결과가 아니다.

## 수정과 놓친 검사

창 호출을 `GUI.Window(id, window, DrawWindow, title, windowStyle)`로 교체하고 자동 크기용 `GUILayout.Width/Height` 옵션을 제거했다. 이 API의 `forceRectOnLayout` 경로는 전달한 너비·높이를 고정 영역으로 보존한다. `GUILayout.Window`의 `LayoutedWindow` 래퍼를 거치지 않아 자동 배치 결과로 native 창을 줄이는 경로를 피한다. Unity 2020.3의 [GUI.Window 호출 경로](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/GUI.cs#L1568), [CallWindowDelegate의 고정 크기 처리](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/GUI.cs#L1623), [고정 영역 크기 보존](https://github.com/Unity-Technologies/UnityCsReference/blob/2020.3/Modules/IMGUI/GUILayoutUtility.cs#L242)에서 확인했다.

기존 `BeginArea` 배치, 남은 영역을 사용하는 목록·상세, 본문 글자 크기, 포커스 추종, 검색·패드 자판·도구·예약, 전체 창 배경 드래그와 설정 저장 계약은 유지한다. 이 기능들의 실제 Windows 정상 동작은 아래 재검증 대상이다.

이전 렌더러 검사 대역은 창 콜백을 실행한 뒤 **입력 사각형을 그대로 반환**했다. 빈 루트에서 Unity가 native 창 크기를 다시 계산하는 동작을 구현하지 않았고, 이 회귀를 검출하지 못했다. 자동 검사 통과만으로 실제 창 배치가 정상이라고 판단한 범위가 부족했다.

또한 `Window` 호출 뒤 `ConstrainWindow`는 관리 코드의 작은 반환값을 최소 `540×440`으로 보정한다. 이 값만 검사하면 이미 그보다 작아진 native 창과 본문 클리핑을 놓친다. 자동 검사에는 창 API의 선택·빈 루트 크기 계약·호출 직후 반환값과 이후 보정값의 차이를 포함해야 한다. 관리 코드 대역 검사는 실제 Unity의 Layout→Repaint, 글꼴·클리핑·마우스 포착·XInput 실행을 대신하지 않는다.

## 적용과 설정 복구

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 게임 폴더 밖에 백업한다.
2. 아래 후보 기록의 SHA-256을 확인한 뒤 **DLL만 교체**한다. 기존 설정 파일을 설치 ZIP의 기본 설정으로 덮어쓰거나 자동 삭제하지 않는다.
3. alpha.2의 보정 결과로 창 크기 `540×440`이 설정에 저장됐을 수 있다. 수정 DLL에서 창이 표시되면 **도구 → 창 위치·크기·글자 기본값 복구**로 화면에 맞는 기본값을 적용하거나, 게임을 종료한 뒤 사용자가 선택한 백업 설정을 복원한다. `540×440`만 보고 사용자 지정 여부를 구분할 수 없으므로 일괄 초기화하지 않는다.
4. 문제가 남으면 게임을 종료하고 백업 DLL과 필요한 설정을 복구한다. 테스트한 DLL 해시·해상도·창 모드·배율·재현 순서·BepInEx 로그를 기록한다.

## Windows 재검증표

다음 항목은 전부 사용자 확인 대기다. 통과 결과는 사용한 후보 DLL 해시와 함께 남긴다.

- [ ] 최초 실행 후 기존 열기 키로 창을 연다. 처음의 Layout→Repaint를 거쳐 작은 빈 창으로 줄어들지 않고 탭·본문·하단 안내가 표시된다. 닫았다 다시 열어도 유지된다.
- [ ] 선택지·조건·이벤트·미션 네 탭을 전환한다. 긴 카드와 목록·상세가 창의 남은 영역에 표시되고 선택 행을 따라 스크롤한다.
- [ ] 도구와 한글/영문 검색 자판을 열고 돌아온다. 작은 창·큰 본문 글자에서도 도구·검색·예약·닫기·기본값 복구에 접근할 수 있다.
- [ ] 창 크기·위치·글자를 바꾼 뒤 게임을 종료하고 재시작한다. 저장한 값으로 창과 내용이 표시된다. alpha.2에서 저장된 작은 설정은 위 복구 절차로 되돌릴 수 있다.
- [ ] 제목·일반 본문·빈 배경에서 창을 끌어 이동한다. 버튼 조작, 검색 문자열 선택, 스크롤바 끌기는 해당 컨트롤이 입력을 소유하며 창 이동과 중복되지 않는다.
- [ ] 패드로 목록·카드·도구·검색 자판을 탐색하고 오른쪽 스틱으로 스크롤한다. 자동 갱신·수동 스크롤 후 탐색 재개, 모드 전환·재연결·Alt-Tab·주사위 왕복에도 입력이 게임 진행·취소·주사위·예약으로 중복 전달되지 않는다.

기존 [Windows 검증표](ISSUE-4-WINDOWS-VALIDATION.md)와 [UI 후속 수정 기록](ISSUE-4-UI-FOLLOWUP.md)의 관련 회귀 항목도 함께 확인한다. 실제 창 표시와 패드 탐색·스크롤·버튼 조작을 유지보수자가 확인하기 전에는 완료로 처리하지 않는다.

## 최종 후보 기록

| 항목 | 기록 |
| --- | --- |
| 최종 소스 기준 / 빌드 시각(KST) | `3b245130a1e6530e4b482d2a4965eb3456d6ec91` / 로컬 검토 후보 2026-10-19 04:20:53 KST. 고정 커밋의 깨끗한 재빌드·해시 일치 확인 |
| 빌드 오류·경고 / 자동 검사 결과 | 오류 0·기존 경고 7 / 전체 11개 프로그램, UI 11,840·실제 DLL 연결 23 단언 통과 |
| 후보 DLL 경로 / SHA-256 | `artifacts/issue-4/fixed-window/DiceMaster.dll` / `e432c5e118760a3d5d75eba9f0920db1ac7bb73bdc0aada3d91ea74e178b3fb9` |
| 후보 ZIP 경로 / SHA-256 | `artifacts/issue-4/fixed-window/DiceMaster-source-candidate.zip` / `471119e2eb263843fe30b86f5a693b3ddafd0ff2fb51a8224993932f68b06af5` |
| Windows 실제 게임 결과 | 사용자 재검증 대기 |

이전 공개 alpha.2 자산의 해시를 수정 후보의 해시로 사용하지 않는다. [alpha.3](ISSUE-4-ALPHA3-RELEASE.md)용 로컬 설치 ZIP은 `artifacts/releases/v1.0.5-alpha.3/DiceMaster-v1.0.5-alpha.3.zip`이다. 고정 커밋만 추출한 소스 스냅샷에서 기존 모드 DLL·Git 데이터·이전 출력 없이 새로 컴파일하고 11개 검사·검토 DLL/설정/ZIP 해시 비교를 통과했다. `release-verification.json`은 게시 전 검사 영수증이며 아직 공개 게시하지 않았다. 실제 게시·공개 다운로드 결과는 게시 후 별도로 기록한다.

새 계약 검사에서 area만 있는 자동 창은 16×42로 축소되고 고정 창은 1200×860을 유지한다. 격리 복사본의 생산 호출을 옛 자동 창으로 되돌린 변이 검사도 새 native 경계 검사에서 예상대로 실패했다. 공개 alpha.2의 실제 DLL은 새 고정 창 연결 검사에서 거부됐고 수정 DLL은 23개 연결 검사를 통과했다. `artifacts/issue-4/window-size-regression-evidence/`에 변이 결과·빌드/거부 로그와 읽기 전용 참조 IL 확인 요약을 보존한다. 대역의 `NativeRect`와 클리핑은 계약 모델의 값이며 실제 Unity/native 화면을 실행하거나 캡처한 결과가 아니다.

전체 빌드 중 입력 불변 검사와 생성 뒤 현재 소스·테스트·설정 일치를 확인했다. ZIP은 CRC가 정상인 설정+새 DLL 두 파일이고 압축 내부 바이트가 후보 DLL과 현재 설정에 일치한다. 게임 DLL·게임 소스는 포함하지 않았다.
