# alpha.37 — 카드 선택지의 마커·스피너·툴팁 배치

<!-- alpha37-publication -->
2026-10-22 23:53:20 KST에 소스 [`15d0962`](https://github.com/jungpaeng/dicemaster/commit/15d0962e821d41eec0ef5bcc041e3c806f714ccd)의 [alpha.37](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.37)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **카드 선택지의 이름 옆 마커/스피너·카드를 피하는 툴팁을 구현했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha37-publication -->

alpha.37은 alpha.36의 카드 배치를 유지하면서 지도·일반 선택지의 같은 좌표를 한 번 그리는 동안 재사용해 중복 투영을 줄인다. 다음 그리기와 원천 조회는 새 좌표를 읽는다. 표시·판정·입력 규칙은 같다.

기준 main은 `6c96945302c097b09b7019bfdb514673af8d8112`다. 원래 main에서 구현·검증하며 별도 브랜치/worktree를 만들지 않았다. [원인·설계·측정](ISSUE-6-CARD-ANCHORS.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).

## 사용자 변화

남제자방 등의 그림 카드에서 마커와 준비 스피너를 현재 보이는 이름 글자 왼쪽에 같은 중심으로 표시한다. 스피너가 완료 마커로 바뀔 때 위치가 이동하지 않는다. 툴팁은 선택 이름을 표시하고, 안내 없는 카드까지 포함한 그림·이름·설명 영역을 피한다. 빈 공간이 없으면 툴팁을 숨기며 기존 지도 상세 로그는 계속 사용할 수 있다. 당문 지도 핀·일반 선택지는 기존 배치다.

판정 결과·금색 기회/아이보리 경로 안내·네 설정/상위 설정·cfg·정상0.25초/실패5초·0.2초 준비 표시 지연·공유 준비 예산·숨김/OFF 중단·정상 웜 Lua 재분석 없음·읽기 전용·기존 입력 보호는 그대로다. 이미 생성된 글자 정점만 읽고 게임 UI·레이아웃을 변경하지 않는다. 게임 자료/DLL을 설치 ZIP에 넣지 않는다.

## 자동 검증

SDK 8.0.426으로 전체21개 검사 프로그램을 통과했다. 고정 참조16개와 제공받은 게임 복제본 DLL16개를 각각 연결해 같은 DLL을 생성했고 원본 게임 파일 불변을 확인했다. 빌드 입력 312개·생산 소스/리소스 126개다. 배포 검증기 Python20개와 diff 공백 검사를 통과했다. cfg는 alpha.36과 같다.

- METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.
- PASS: 4265 managed behavior assertions and 1288 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- PASS: MapGuideRouteIntegrationTests 4212 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: ChoiceStoryTests 7819 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: LogUiKeyboardTests: 5492 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered.
- PASS: InformationRuntimeTests: 7089 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: 262831 assertions.

실제 생산 표시 코드와 UI 경계 모형으로 스크린샷과 같은 두 카드 배치, 루트 밖 이름/그림/설명, 빈/비활성 형제, 활성 표시 변형, 글자 배율, 스피너→마커 중심, 겹치지 않는 툴팁/공간 부족 숨김, 해상도 변화, 1,000회 웜 텍스트 측정·이미지 업로드 불변·입력 불변을 검사했다. 제공받은 level2의 직렬화 위치와 Unity Text의 실제 생성 캐시 구현도 대조했다. 실제 Windows/Unity/Harmony·사용자 세이브·물리 입력·FPS는 실행하지 않았다.

alpha.36에서 수행한 관리 모형의 생산 카드 기하 참고 측정은 10,000회/표본에서 3카드 p95 0.011458ms,16카드 0.095750ms,64카드 0.045833ms, 추가 할당0byte/프레임이었다. JIT/스케줄링 영향을 포함한 참고치며 카드 수 증가에 따른 단조 비교나 실제 Unity FPS 보증으로 사용하지 않는다. 한 Draw 안에서 같은 카드의 위치를 반복 읽어도 기하를 재사용하며 Draw 종료 finally에서 참조를 버린다. 주기를 늘리거나 조회/분석 예산을 바꾸지 않았다.

소스·annotated tag는 `15d0962e821d41eec0ef5bcc041e3c806f714ccd` / `v1.0.7-alpha.37`다. 고정 커밋 재빌드의 312개 입력·126개 소스/리소스·21개 검사 통과와 DLL/cfg/ZIP이 검토 후보와 모두 일치한다. 예약 준비 루프의 동적 assertion 수가 달라질 수 있는 두 검사는 원래 로그와 허용 경계 비교를 따로 보존한다.

## 설치 후보 파일

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1101824 | `5a26dd8e83ef3501c208e37739c18fb993095a9081064fb8d25bd1ab73611837` |
| DiceMaster-v1.0.7-alpha.37.zip | 435329 | `d7673f12298cb6d2f49823a1b0662be2a441f86581fea342b651b866541b522f` |
| 기존 DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

## 설치·복구와 Windows 대기

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 DLL만 교체해 기존 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins를 BepInEx 아래에 넣는다. 자동 Source code 압축은 설치용이 아니다. 문제 시 게임 종료 후 백업 DLL/cfg를 복구한다. alpha.36과 Latest 정식 v1.0.6을 보존한다.

새 후보에서 남제자방/Break 카드의 이름 옆 위치·활성 표시 전환·카드 간 툴팁 겹침·여러 해상도와 원래 입력/FPS를 실제 Windows에서 확인해야 한다. 지도와 일반 선택지·기존 기능의 회귀 확인도 남아 있다. 사용자 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다.



## alpha.37 — 같은 그리기 안의 루트 좌표 재사용

alpha.36 게시 후 검토에서 DrawBadge와 Focused가 기존 TryRect 결과를 얻은 뒤 공통 TryBadgeRect를 통해 같은 루트를 다시 투영하는 것을 확인했다. TryRect의 실제 계산을 ComputeRect로 분리하고, 기존 카드 기하와 같은 BeginPaint/EndPaint 수명 안에서 성공/실패 루트 사각형도 재사용한다. finally에서 두 캐시를 비우므로 다음 그리기·해상도/포커스/좌표 변경과 원천의 예약 visibility 조회는 새 기하를 읽는다. 캐시는 조건/해금/마커 판정 결과를 보관하지 않는다.

자동 검사에서 같은 루트의20회 요청은 GetWorldCorners1회였고, 그리기 밖2회 조회는 각각 다시 읽었다. 다음 그리기에서도 이동한 좌표를 새로 읽었다. 같은 생산 코드의 비캐시/그리기 재사용 경로를 대조한10,000프레임 관리 모형은3루트 p95 0.026708→0.006417ms,64루트 0.075916→0.045833ms였으며 웜 추가 할당은 모두0byte/프레임이었다. 이는 반복 요청을 강조한 대조 모형이며 실제 게임의 일반 호출 횟수나 Unity/native/FPS 개선 폭을 뜻하지 않는다. 기록은 artifacts/issue-6/card-anchor-reuse/root-benchmark.json에 있다.

alpha.37에서도 고정 참조/게임 복제본 DLL 연결의 전체21개 검사가 통과하고 같은 DLL을 생성했다. 설정/cfg·0.25초/5초·준비 수명·표시 범위/판정·카드 이름 옆 배치·형제 카드 회피·입력 보호는 그대로다. [최종 후보·게시](ISSUE-6-ALPHA37-RELEASE.md)의 새 DLL은 실제 Windows 확인 대기다. alpha.36과 이전 배포는 보존한다.

## 게시 결과

2026-10-22 23:53:20 KST에 Release `420849854`·asset `666166898`를 draft=false·prerelease=true·make_latest=false로 게시했다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.37/DiceMaster-v1.0.7-alpha.37.zip)의 SHA-256·크기·두 경로·CRC·내부 DLL/cfg 바이트를 인증 없이 확인했다.

이전 78개 Release·79개 태그·자산·Latest 정식v1.0.6과 #5/#6 상태를 보존했다. main과 새 annotated tag만 atomic push했으며 게시 보충은 별도 main 문서 커밋이다. 소스 태그·배포 파일은 바꾸지 않는다. 실제 Windows/사용자 세이브·입력·FPS/GC 확인과 #6 완료는 대기다. 영수증은 artifacts/releases/v1.0.7-alpha.37/에 보관한다.
