# v1.0.7-alpha.42 — 흰 마커 준비 대기 개선

## 변경과 범위

장면 교체로 파괴된 장소 등록을 정리하고, 실패 재시도 대기 장소가 정상 장소의 준비 순서를 낭비하지 않도록 한다. 선택 장소의 첫 유효 안내가 준비되면 다른 장소에 준비 순서를 양보하며 최대 네 번 뒤 선택 장소 우선을 회복한다. 배경 조회가 없는 프레임에서는 기존 공유 슬롯을 선택 장소에 돌려준다.

정상 0.25초/실패 5초, 공유 원문/풀/작업·시간 상한, 현재 조건 재검증, 숨김/OFF 중단과 기존 입력 보호를 유지한다. 주사위 자동 이야기 안내를 복원하지 않는다. 기존 설정과 cfg를 유지한다. [단계별 구현·원본 결과·재현·한계](ISSUE-6-WHITE-MARKER-PREPARATION.md).

## 자동 비교와 한계

macOS .NET8에서 운영 provider와 관리 경계로 측정했다. 실제 게임 원문·설치 정의를 사용한 경우도 현재 상태는 합성 기본값이며 사용자 세이브·게임 Lua·Unity·Windows 프레임 실행 결과가 아니다.

| 비교 항목 | alpha.41 기준 | 변경 |
| --- | ---: | ---: |
| 장면 재생성 100회 후 장소 등록 | 1,100개 | 11개 |
| 실패 장소 8곳 뒤 3단계 정상 경로 첫 마커 | 19번째 조회 | 11번째 조회 |
| 포커스한 16분기 장소 옆 정상 마커 | 17번째 조회 | 2번째 조회 |
| 실제 원문·남제자방 포커스 중 여제자방 첫 흰 마커 | 38번째 조회 | 16번째 조회 |
| 같은 조건의 연단방 첫 흰 마커 | 47번째 조회 | 27번째 조회 |

기존 54개와 추가 27개 비교에서 최종 표시 SHA-256이 같다. 정상 추가 원문 읽기는 0회, 한 조회의 준비 원문 읽기는 최대 1회다. 선택 장소의 모든 후속 준비를 한 조회 늦추는 절충이 있으며 일부 cold/갱신 위상에서는 첫 표시가 한 갱신 늦어지는 변동도 남는다. 모든 첫 표시나 Windows FPS가 개선됐다고 주장하지 않는다.

## 검토 후보·고정 커밋 재빌드

원래 main에서 검토 후보의 전체 21개 검사 프로그램·실제 설치 파일/원문 경계·복제본 DLL 연결을 통과했다. 입력 315개, 생산 소스/리소스 126개, 고정 참조 16개다. 복제본 API 검사는 모드/검사기/fixture를 새로 빌드해 4,265개 managed behavior / 1,288개 metadata contract를 통과했다. 설치 ZIP의 두 경로·CRC·cfg/DLL 바이트가 새 후보와 같다.

| 검토 후보 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1105408 | `9cc25e380e22af4936e860089dab8de615a32b80bba2bc3ca1ebee97f5116511` |
| 설치 ZIP | 436859 | `71cf293d885c506f9bbc401ebd91a898ac41f55a940963ab5f0d5079327d77e9` |
| DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

소스 태그 `v1.0.7-alpha.42`는 `b70073c309021a7d8aa215758c201d3b2244e94d`를 가리킨다. 해당 커밋만 추출해 관리 모드 DLL 0개에서 재빌드했으며 검토 후보의 전체 입력 SHA·SDK·고정 참조·DLL/cfg/ZIP이 일치했다. 21개 검사 프로그램이 다시 통과했다. 게시 결과 문서는 이후 별도 main 커밋이며 소스 태그·패키지를 바꾸지 않는다.

## 설치와 Windows 확인 대기

게임 종료 후 DLL/cfg를 백업한다. 기존 설치에서는 DLL만 교체하고 cfg를 보존한다. 최초 설치는 설치 ZIP의 config·plugins를 BepInEx 아래에 배치한다. GitHub Source code 압축은 설치용이 아니다. 문제 시 백업 DLL을 복원한다.

- [ ] 새 게임/완전 재시작 후 저장 직행에서 무포커스·남제자방/연단방 계속 포커스·빠른 포커스 변경을 각각 비교한다.
- [ ] 여러 번 장면 이동 후 마커 준비와 GC가 누적 악화되지 않는지 확인한다. 완료·잠금·재해금·기한 경과와 금색 기회 마커를 회귀 확인한다.
- [ ] 창 가림·로그·OFF/ON·불러오기 뒤 오래된 작업/표시가 남지 않고 키보드·마우스·터치·Xbox/XInput 소유권/해제/중립 보호가 유지되는지 확인한다.
- [ ] 기존 이벤트 선택·예약·로그 네 탭·주사위·상점·실제 분기 기록을 확인한다. 첫 안내·전체 준비·실제 프레임 p95/GC를 별도로 측정한다.

[실제 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md). 새 DLL의 사용자 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다. v1.0.6 확인은 이 후보로 승계하지 않는다.

## 게시 완료와 공개 파일 확인

<!-- alpha42-publication -->
2026-10-23 22:13:01 KST에 소스 [`b70073c`](https://github.com/jungpaeng/dicemaster/commit/b70073c309021a7d8aa215758c201d3b2244e94d)의 [alpha.42](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.42)를 게시했다. 고정 커밋의 21개 검사·검토 입력/파일 일치·복제본 게임 DLL/API 경계·익명 공개 ZIP 대조를 통과했다. 실제 Windows 성능·물리 입력 확인과 #6 전체 완료는 대기다.
<!-- /alpha42-publication -->

- [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.42/DiceMaster-v1.0.7-alpha.42.zip) · 소스 `b70073c309021a7d8aa215758c201d3b2244e94d`. annotated tag와 main을 함께 atomic push했다.
- Release `421860631`, asset `669174531`. `draft=false`, `prerelease=true`, `make_latest=false`. 공개 시각 `2026-10-23T13:13:01Z`.
- 인증 없는 공개 Release API/페이지/ZIP 다운로드가 성공했다. ZIP 436,859 B와 SHA-256, 두 내부 경로·CRC·DLL/cfg 바이트가 고정 커밋 산출물 및 검토 후보와 같다.
- 기존 83개 Release의 본문·태그·게시 상태·자산 ID/이름/크기/해시와 84개 원격 tag 참조가 보존됐다. 현재는 84개 Release·85개 tag 참조다. latest 정식은 v1.0.6이며 #5/#6는 open이다.
- 복제본 게임 DLL/API 경계는 이번 후보·검사기·fixture를 새로 빌드한 결과다. 현재 DLL 바이트가 고정 참조 빌드와 같다. 실제 Unity/Harmony·Windows 게임은 실행하지 않았다.
- 고정 커밋 영수증: `artifacts/releases/v1.0.7-alpha.42/release-verification.json`. 공개 파일/보존 검증: 같은 폴더의 `anonymous-verification.json`. 검토 후보 대조: `artifacts/issue-6/white-marker-release/reviewed-commit-comparison.json`. 이 폴더는 Git/설치 ZIP에 포함하지 않는다.

Windows 확인 전 새 후보나 #6을 전체 완료/종료로 처리하지 않는다.
