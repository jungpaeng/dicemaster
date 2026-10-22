# alpha.38 — 주사위 판정 안내·확정 결과 기록

<!-- alpha38-publication -->
main에서 alpha.38의 주사위 판정 안내와 확정 결과 기록을 구현·자동 검사했다. 마커 유무와 독립적으로 실제 열린 주사위를 관찰한다. 고정 커밋 검사·게시 결과는 별도로 기록하며 실제 Windows 확인과 PRD #6 전체 완료는 대기다.
<!-- /alpha38-publication -->

기준 main은 `b8b698596513cf2516c6c47321bc8c00a8a97577`다. 원래 main에서 구현·검증했으며 별도 브랜치/worktree는 만들지 않았다. [설계·수명·실패·한계](ISSUE-6-DICE-INFORMATION.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).

## 사용자 변화

주사위가 실제 열리면 우측 상단에 게임이 표시한 판정 이름과 확인된 진입 선택을 안내한다. 마커 없는 경로·자동 진행도 대상이며 출처를 확인하지 못하면 주사위 정보만 표시한다. 진행 확정·정상 메뉴 정리가 확인된 결과만 기존 분기 알림/최근20건 메모리에 함께 보관한다. 재굴림 중간 결과는 기록하지 않는다.

기존 EnableBranchNotices/EnableOptionInfo와 5초·최근20건·초기화 수명을 사용한다. 네 설정 키/기본값과 cfg 바이트, 지도/선택 마커의 의미·배치, 0.25초/5초 갱신·Lua 캐시, 패드/키보드/마우스/터치와 기존 주사위 값 조절·#5 이벤트/예약을 유지한다.

## 자동 검증

SDK8.0.426·전체21개 프로그램·318개 빌드 입력·129개 생산 소스/리소스를 검사했다. 고정 참조16개와 게임 복제본 DLL16개를 각각 연결해 같은 DLL을 생성하고 읽기 전용 원본 불변을 확인했다. 배포 검증기 Python20개와 diff 공백 검사도 통과했다. 기존 임시 SDK/패키지의 필수 파일이 없어 같은 고정 버전을 별도 검사 환경에 복구했으며 공식 SDK SHA-512·NuGet 패키지 출처와 실패/최종 로그를 artifacts/issue-6/dice-context/에 보관했다.

- PASS: BranchHistoryTests: 1738 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed.
- PASS: LogUiKeyboardTests: 5505 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered.
- PASS: InformationRuntimeTests: 7104 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.
- PASS: 4265 managed behavior assertions and 1288 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- PASS: 262831 assertions.

원래 게임 코루틴·Harmony·물리 입력·FPS를 실제 Windows에서 실행한 결과는 아니다. 실제 생산 코드의 관리 경계로 초기/재굴림 제외·정상 확정·취소/정리·예외 보존·부정확한 이름 재사용 방지·5초/20건·설정/가림·웜 표시 재사용을 확인했다. 고정 커밋 재빌드·게시 결과는 후속 기록으로 남긴다.

## 설치 후보 파일

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1114112 | `c670050b55bef125f283a145156d0bee771d7357876f53d9ba815c24b1a59870` |
| DiceMaster-v1.0.7-alpha.38.zip | 439647 | `5017710ca9cde3c84235cc3404e767b46f3f3eff56e521d949e41df24e228a16` |
| 기존 DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

## 설치·복구와 Windows 대기

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 기존 DLL/cfg를 백업하고 DLL만 교체해 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins를 BepInEx 아래에 넣는다. 자동 Source code 압축은 설치용이 아니다. 문제 시 게임 종료 후 백업 DLL/cfg를 복구한다. alpha.37과 Latest 정식 v1.0.6을 보존한다.

마커 없는/자동/일반 선택 경로의 주사위, ExecuteRoll/legacy timer·수동/자동·재굴림·계속 확정, 진입 선택 연결/미확인·다음 메뉴/스크립트, 실제 5초/20건·기록 초기화·가림/작은 화면/입력/FPS를 새 DLL에서 확인해야 한다. 전체 완료나 #6 이슈 종료로 처리하지 않는다.
