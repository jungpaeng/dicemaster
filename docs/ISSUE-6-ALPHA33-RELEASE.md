# alpha.33 — 지도 대화 마커 준비·깜빡임 개선

<!-- alpha33-publication -->
main의 alpha.33 후보다. 지도 대화 마커의 현재 메뉴 우선 준비와 입력 변화 확인을 통한 결과 재사용을 구현했다. 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.
<!-- /alpha33-publication -->

기준 main은68772fddc58050984326842a177e030b0613b184다. 같은 main에서 구현·검증하며 별도 branch/worktree는 만들지 않는다. [원인·설계·상한·측정](ISSUE-6-MARKER-CACHE.md) · [실제 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).

## 사용자 변화

현재 메뉴를 깊은 후속 경로보다 먼저 준비해 흰색 대화 마커의 첫 표시를 앞당긴다. 성공 경로가 읽은 관련 입력이 그대로면 결과·표시 행을 재사용하고, 다른 경로의 준비 마감 때문에 기존 표식이 사라지는 경계를 보완한다. 완료·잠김·조건 자료/입력 변경·행동력·우선순위·가림은 계속 반영한다. 특정 인물/플래그 예외 목록은 없다.

표시 범위와 금색 기회/아이보리 안내 구분, 기존 설정·cfg·입력·정상0.25초/실패5초·읽기 전용 계약은 유지한다. 파일 정의 지원 지문은alpha.32와 같고 게임 원문/DLL은 배포하지 않는다.

## 자동 검증

SDK8.0.426으로 전체21개 검사 프로그램을 통과했다. 고정 참조16개와 제공받은 전체 게임 복제본의 DLL16개를 각각 연결해 검사했으며 같은 DLL 바이트를 생성했다. 빌드 입력303개·생산 소스/리소스123개의 불변을 확인했다. 원본 게임 파일과 기존 cfg를 변경하지 않았다. 설치 ZIP의 두 경로·CRC·DLL/cfg 바이트, 배포 검증기 Python20개와 git diff --check를 확인했다.

주요 최종 검사 기록:

- RealGameApiTests: METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed. / PASS: 4265 managed behavior assertions and 1288 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- MapGuideRouteIntegrationTests: PASS: MapGuideRouteIntegrationTests 4070 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- InformationRuntimeTests: PASS: InformationRuntimeTests: 6808 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- ChoiceStoryTests: PASS: ChoiceStoryTests 7719 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- GamepadTests: PASS: 262831 assertions.

실제 파일/번체 Lua2,199개를 사용하는 경로 검사는 명시적인 현재 상태·Unity/게임 대체 객체를 사용한다. 새 회귀는11/64장소, 이미 만료된 준비 마감, 현재 값/조건/배열/목록/소유자/초기화·Lua 연결 교체와 읽기 실패·한도를 검사한다. 일반 경로 재사용이 엄격1회성·기회·게임 입력을 바꾸지 않는 경계를 함께 확인한다. 초기 전체 검사의 분리된 test stub 연결과 이전 지도 phase 정책 검사를 보완한 기록도 보존하며 최종 소스로 전체 검사를 다시 통과했다.

고정 소스 커밋 재빌드는 커밋 후 수행해 이 후보와 대조한다. 실제 Windows 실행·사용자 세이브·물리 입력·FPS 확인을 수행했다는 뜻이 아니다.

## 설치 후보 파일

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1092096 | `b773cb00fb81be7c62e6d4244648f972f157ddce5b430ff136dd7998cc4a625c` |
| DiceMaster-v1.0.7-alpha.33.zip | 431205 | `6852fb7f00e609c418057b701d0bc5ab84461aa90b9ca0c46c11502bb8ae4d7b` |
| 기존 DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

## 설치·복구

[v1.0.7-alpha.33 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.33/DiceMaster-v1.0.7-alpha.33.zip). 활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 DLL만 교체해 기존 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins 두 경로를 BepInEx 아래에 넣는다. 자동 Source code 압축은 설치용이 아니다. 문제 시 게임 종료 후 백업 DLL/cfg를 복구한다. 이전 alpha.32와 Latest 정식v1.0.6은 보존한다.

새 후보는 실제 Windows에서 재실행·지도 저장 직행·포커스 전/후 첫 표시 시간, 기존 흰색 표식의30초 유지, 대화 완료·주제 해금·날짜·행동력·우선순위 변경, 로드/장면/OFF/가림·기존 입력/기능을 확인해야 한다. 사용자 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다.
