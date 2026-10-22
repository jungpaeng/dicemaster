# alpha.36 — 카드 선택지의 마커·스피너·툴팁 배치

<!-- alpha36-publication -->
main에서 alpha.36의 카드 선택지 배치를 구현·자동 검사했다. 이름 옆에 마커·스피너를 정렬하고 툴팁은 보이는 카드의 그림·설명을 피한다. 고정 커밋 검사·게시 기록과 새 DLL의 실제 Windows 확인은 별도로 기록하며 PRD #6 전체 완료는 대기다.
<!-- /alpha36-publication -->

기준 main은 `d5328b5a796352d54c5454c4dec1f01c2cbd6947`다. 원래 main에서 구현·검증하며 별도 브랜치/worktree를 만들지 않았다. [원인·설계·측정](ISSUE-6-CARD-ANCHORS.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).

## 사용자 변화

남제자방 등의 그림 카드에서 마커와 준비 스피너를 현재 보이는 이름 글자 왼쪽에 같은 중심으로 표시한다. 스피너가 완료 마커로 바뀔 때 위치가 이동하지 않는다. 툴팁은 선택 이름을 표시하고, 안내 없는 카드까지 포함한 그림·이름·설명 영역을 피한다. 빈 공간이 없으면 툴팁을 숨기며 기존 지도 상세 로그는 계속 사용할 수 있다. 당문 지도 핀·일반 선택지는 기존 배치다.

판정 결과·금색 기회/아이보리 경로 안내·네 설정/상위 설정·cfg·정상0.25초/실패5초·0.2초 준비 표시 지연·공유 준비 예산·숨김/OFF 중단·정상 웜 Lua 재분석 없음·읽기 전용·기존 입력 보호는 그대로다. 이미 생성된 글자 정점만 읽고 게임 UI·레이아웃을 변경하지 않는다. 게임 자료/DLL을 설치 ZIP에 넣지 않는다.

## 자동 검증

SDK 8.0.426으로 전체21개 검사 프로그램을 통과했다. 고정 참조16개와 제공받은 게임 복제본 DLL16개를 각각 연결해 같은 DLL을 생성했고 원본 게임 파일 불변을 확인했다. 빌드 입력 312개·생산 소스/리소스 126개다. 배포 검증기 Python20개와 diff 공백 검사를 통과했다. cfg는 alpha.35와 같다.

- METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.
- PASS: 4265 managed behavior assertions and 1288 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- PASS: MapGuideRouteIntegrationTests 4212 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: ChoiceStoryTests 7824 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: LogUiKeyboardTests: 5489 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered.
- PASS: InformationRuntimeTests: 7081 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: 262831 assertions.

실제 생산 표시 코드와 UI 경계 모형으로 스크린샷과 같은 두 카드 배치, 루트 밖 이름/그림/설명, 빈/비활성 형제, 활성 표시 변형, 글자 배율, 스피너→마커 중심, 겹치지 않는 툴팁/공간 부족 숨김, 해상도 변화, 1,000회 웜 텍스트 측정·이미지 업로드 불변·입력 불변을 검사했다. 제공받은 level2의 직렬화 위치와 Unity Text의 실제 생성 캐시 구현도 대조했다. 실제 Windows/Unity/Harmony·사용자 세이브·물리 입력·FPS는 실행하지 않았다.

관리 모형의 생산 기하 계산 10,000회/표본에서 3카드 p95 0.011458ms,16카드 0.095750ms,64카드 0.045833ms, 추가 할당0byte/프레임이었다. JIT/스케줄링 영향을 포함한 참고치며 카드 수 증가에 따른 단조 비교나 실제 Unity FPS 보증으로 사용하지 않는다. 한 Draw 안에서 같은 카드의 위치를 반복 읽어도 기하를 재사용하며 Draw 종료 finally에서 참조를 버린다. 주기를 늘리거나 조회/분석 예산을 바꾸지 않았다.

고정 커밋 재빌드·게시 결과는 후속 기록으로 남긴다.

## 설치 후보 파일

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1101312 | `65f11bc81de7177041b78675dbec300a42151795b8cf3b20e3d80fc790a8b8cd` |
| DiceMaster-v1.0.7-alpha.36.zip | 435273 | `edf084228af6ae8316a59ef1eb4f2128f0ff0e502c9fd94d2fd8d585386b0d47` |
| 기존 DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

## 설치·복구와 Windows 대기

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 DLL만 교체해 기존 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins를 BepInEx 아래에 넣는다. 자동 Source code 압축은 설치용이 아니다. 문제 시 게임 종료 후 백업 DLL/cfg를 복구한다. alpha.35와 Latest 정식 v1.0.6을 보존한다.

새 후보에서 남제자방/Break 카드의 이름 옆 위치·활성 표시 전환·카드 간 툴팁 겹침·여러 해상도와 원래 입력/FPS를 실제 Windows에서 확인해야 한다. 지도와 일반 선택지·기존 기능의 회귀 확인도 남아 있다. 사용자 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다.
