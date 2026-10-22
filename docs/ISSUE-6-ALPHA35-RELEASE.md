# alpha.35 — 지도·선택지의 마커 준비 스피너

<!-- alpha35-publication -->
2026-10-22 23:07:45 KST에 소스 [`cff13f2`](https://github.com/jungpaeng/dicemaster/commit/cff13f2960f27a9c8f32c6f0b335ff86d49beae5)의 [alpha.35](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.35)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **지도·선택지의 마커 준비 스피너를 구현했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha35-publication -->

기준 main은 `fdce0c702d149b17c7f1798ff86ab835605dd149`다. 원래 main에서 구현·검증하며 별도 브랜치/worktree를 만들지 않는다. [설계·검사·측정](ISSUE-6-LOADING-INDICATORS.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).

## 사용자 변화

포커스 시 뜨는 일반 준비 중 툴팁을 제거하고 지도 장소·실제 활성 선택 버튼에 지연된 작은 금색 스피너를 제공한다. 같은 마커 조회가 준비 상태도 발행하며 긍정은 즉시 기존 금색/아이보리 마커로 바꾼다. 빈 항목·읽기 실패/미지원은 무한 대기로 숨기지 않는다. 이미 확인한 마커·실제 내용 툴팁·판정 기준을 유지한다.

네 설정·상위 설정·cfg·정상0.25초/실패5초·공유 준비 예산·숨김/OFF 중단·정상 웜 Lua 재분석 없음·읽기 전용·기존 입력 보호는 그대로다. 스피너 그림은 코드가 만들고 공유 텍스처1개를 재사용한다. 추가 설치 파일이나 게임 원문/DLL은 배포하지 않는다.

## 자동 검증

SDK8.0.426으로 전체21개 검사 프로그램을 통과했다. 고정 참조16개와 제공받은 게임 복제본 DLL16개를 각각 연결해 같은 DLL을 생성했다. 빌드 입력310개·생산 소스/리소스125개와 원본 게임 파일의 불변을 확인했다. cfg는 alpha.34와 같다. 배포 검증기 Python20개, 설치 ZIP의 두 경로·CRC·내부 DLL/cfg 바이트를 확인했다.

- METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.
- PASS: 4265 managed behavior assertions and 1288 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- PASS: MapGuideRouteIntegrationTests 4212 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: ChoiceStoryTests 7814 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: LogUiKeyboardTests: 5472 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered.
- PASS: 691 map/one-shot managed boundary assertions; Unity/Windows runtime not executed.
- PASS: InformationRuntimeTests: 6946 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: 262831 assertions.

생산 경로/표시 코드와 실제 설치 파일/Lua를 연결한 관리 검사다. 실제 Windows/Unity/Harmony·사용자 세이브·물리 입력·FPS를 실행하지 않았다. 지연·마커 교체·빈/실패 종료·재해금·가림/상태/입력 불변, 공유 텍스처 업로드와 UV 변경을 검사했다. 초기 별도 fixture의 타입 연결 오류와 최종 로그를 artifacts에 보존했다.

alpha.34 비교18개 관리 표본에서 첫/전체 마커 표시 조회 횟수와 Lua 읽기 상한이 같았다.64항목 지연 상태 관리100,000프레임은428.15ms, 약0.0043ms/프레임이며 루프 할당은0이었다. Unity geometry/GPU/FPS 비용을 포함하지 않는다. 자세한 p95와 한계는 설계 문서에 남겼다.0.25초 주기를 늘리지 않았다.

소스·annotated tag는 `cff13f2960f27a9c8f32c6f0b335ff86d49beae5` / `v1.0.7-alpha.35`다. 고정 커밋 재빌드의310개 입력·125개 소스/리소스·21개 성공 기록과 DLL/cfg/ZIP이 검토 후보와 모두 일치한다.

## 설치 후보 파일

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1096704 | `756a88aeddf7420042ea6a251a56f0d5a88b7a8390def1659db24d59b385d8dd` |
| DiceMaster-v1.0.7-alpha.35.zip | 433375 | `6f9a592498fcb8e4f0c006c669a3abbcb01b12cc9c9d604486d08a4d8cc89031` |
| 기존 DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

## 설치·복구와 Windows 대기

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 DLL만 교체해 기존 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins를 BepInEx 아래에 넣는다. 자동 Source code 압축은 설치용이 아니다. 문제 시 게임 종료 후 백업 DLL/cfg를 복구한다. alpha.34와 Latest 정식v1.0.6을 보존한다.

완전 재실행·지도 저장 직행과 유학 등 선택 메뉴에서 스피너 표시/완료·빈/실패 종료·기존 마커 유지·가림/OFF·물리 입력과 실제 크기/색·FPS/GC를 확인해야 한다. alpha.34 연단방/기한 회귀도 확인한다. 사용자 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다.

## 게시 결과

2026-10-22 23:07:45 KST에 Release `420803017`·asset `666059787`를 draft=false·prerelease=true·make_latest=false로 게시했다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.35/DiceMaster-v1.0.7-alpha.35.zip)의 SHA-256·크기·두 경로·CRC·내부 DLL/cfg 바이트를 인증 없이 확인했다.

이전 76개 Release·77개 태그·자산·Latest 정식v1.0.6과 #5/#6 상태를 보존했다. main과 새 annotated tag만 atomic push했으며 게시 보충은 별도 main 문서 커밋이다. 소스 태그·배포 파일은 바꾸지 않는다. 실제 Windows/사용자 세이브·입력·FPS/GC 확인과 #6 완료는 대기다. 영수증은 artifacts/releases/v1.0.7-alpha.35/에 보관한다.
