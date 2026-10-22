# alpha.40 — 최초 주사위 화면 안내 복구

<!-- alpha40-publication -->
2026-10-23 03:48:22 KST에 소스 [`00744c6`](https://github.com/jungpaeng/dicemaster/commit/00744c64f226b6122f6582fbba2a1ec5b76ab31a)의 [alpha.40](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.40)을 게시했다. 최초 주사위 화면 초기화로 사라지던 결과 안내를 복구했으며 전체·고정 커밋 22개 검사·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha40-publication -->

## 원인과 수정

양양에서 우소매와 함께 있을 때 ‘수련’을 누르면 ‘당신의 상대……’ 주사위의 `>=50 소매와 대련한다`에 연결 안내가 없고, 상세 로그도 현재 지도 장소/성능만 남았다. 실제 Lua의 결과 2 → 우소매 대화 연결은 분석 가능했다. 문제는 `ExecuteRoll`이 IEnumerator를 반환한 뒤 실제 코루틴에서 화면을 처음 활성화하면서 `MenuDialog.Awake → Clear`가 먼저 관찰한 호출 소유자까지 지운 것이었다. 준비 시간을 늘려 해결할 문제가 아니었다.

호출 인자는 대기로 보관하고, 그 호출의 초기 Awake 안에서 발생한 Clear만 초기화로 구분한다. native `CustomMenuDialog.UpdateOptions` 완료 후 활성 주사위·같은 게임 문맥·순서까지 같은 원본 결과 행 전체가 일치하면 안내 소유자로 결합한다. 실제 Clear/OnDisable/불러오기/원문 변경/초기화 예외는 취소한다. 초기화 예외는 원래 객체 그대로 전달하고, 늦은 준비 통지로 끝난 호출을 살리지 않는다. 준비 대기/원문 미확인에는 현재 주사위의 ‘주사위 연결 자료’를 제공해 이전 지도 결과로 대신하지 않는다.

마커/짧은 안내의 의미·임계값·원래 효과·주사위 조작은 alpha.39와 같다. 아이보리는 해당 결과의 이야기 연결 후보, 어두운 금색은 엄격한 현재 경로와 등록 임무/증명 1회성을 확인한 경우다. 결과 행은 원래대로 직접 선택할 수 없다. alpha.38의 우측 상단 진행 패널이나 주사위 확정 기록은 추가하지 않는다. [기능·설정·성능 상한](ISSUE-6-DICE-RESULT-GUIDANCE.md).

## 설정·성능·안전

새 설정/입력/탭은 없다. `EnableMapGuide`/`EnableOneShotHints`·기존 상위 설정과 cfg를 유지한다. 둘 다 OFF면 조회를 멈추고 준비 중 OFF에서도 파싱하지 않는다. 후킹에서 인자/현재 행만 확인하며 Lua 분석은 기존 예정 조회에서 진행한다. 정상 0.25초/실패 5초·공유 5ms 준비 양보·캐시·숨김 조회 중단을 유지한다. 게임 판정/코루틴/RNG/예약/세이브/조건/능력치/아이템을 조회 코드가 실행하거나 변경하지 않는다. 렌더링은 자기 요약만 복원한다.

## 자동 검사

고정 참조 16개와 사용자 게임 복제본 DLL 16개 각각에 연결한 전체 22개 검사 프로그램이 통과했다. 양쪽 모드 DLL은 바이트가 같으며 입력/참조 불변을 확인했다. 회귀 경계/제공 Lua 검사는 48개, 릴리스 검증기 자체 검사는 20개 통과했다. 빌드 입력 319개·생산 소스/리소스 128개와 ZIP 두 경로/CRC/내부 바이트를 기록했다. cfg SHA는 alpha.39와 같다. 검사 기록은 `artifacts/issue-6/dice-first-activation/`에 보관한다. 회귀 검사는 실제 생산 코드에 명시적인 UI/자원 경계 대체물을 연결한다. 실제 게임 DLL 메타데이터/IL을 읽어 최초 활성화 → Awake/Clear → 결과 준비 순서를 확인하고, 사용자 제공 Lua의 `S2321_04_03_01_004` 결과 2 → `D_S2321_04_03_01_02_001`을 같은 수명 순서로 검사한다. 게임 원문/DLL을 배포하지 않는다.

관리 코드 검사·실제 DLL 연결·원문 대조는 실제 Windows/Unity/Harmony 실행, 사용자 저장, 물리 패드나 FPS 측정이 아니다. [실제 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha40--최초-주사위-화면-확인-대기).

| 검토 산출물 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1118208 | `df8fbe8fcd56d43c6a5762f1cf463a98e5aace4ca0ef0e9b597497a7957bcd28` |
| 설치 ZIP | 441489 | `ccbf5174a6978a38d00b7d7a611bf6992a9a66c3c7540a6206fb179251f13f5a` |
| 기존 cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

소스 `00744c64f226b6122f6582fbba2a1ec5b76ab31a`의 고정 커밋 재빌드에서도 전체 22개 검사가 통과했다. 319개 입력·128개 생산 소스/리소스·고정 참조와 DLL/cfg/ZIP이 검토 후보와 같고 복제본 DLL도 같다. 성공 기록 21개는 정확히 같으며 MapGuideRouteIntegrationTests의 준비 루프 assertion 수만 달랐다. 같은 성공 형식/전체 의미와 양쪽 원본 기록을 보존했다. `artifacts/releases/v1.0.7-alpha.40/reviewed-candidate-comparison.json`과 고정 빌드 manifest에 기록했다.

## 설치와 실제 Windows 확인

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 기존 DLL/cfg를 백업하고 새 ZIP의 `plugins/DiceMaster/DiceMaster.dll`만 교체해 cfg를 보존한다. 최초 설치는 ZIP의 config/plugins를 BepInEx 아래에 넣는다. Source code ZIP은 설치용이 아니다. 문제 시 게임 종료 후 백업을 복원한다.

게임을 완전히 다시 시작하고 양양 세이브를 불러온 뒤, 다른 주사위를 먼저 열지 않고 ‘수련’에 진입한다. 굴리기 전 결과 2의 우소매 연결 마커/요약, 로그의 ‘주사위 연결 자료/결과 2’, 재진입/불러오기/창 가림/설정/기존 입력·예약·로그·상점·분기를 확인한다. 이번 DLL의 실제 Windows 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다.

## 게시

2026-10-23 03:48:22 KST에 Release `421079623`·asset `666755582`를 draft=false·prerelease=true·make_latest=false로 게시했다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.40/DiceMaster-v1.0.7-alpha.40.zip)과 공개 API/페이지를 인증 없이 확인하고 ZIP 크기·SHA·두 경로·CRC·내부 DLL/cfg 바이트를 대조했다.

main과 소스 `00744c64f226b6122f6582fbba2a1ec5b76ab31a`의 새 annotated tag `v1.0.7-alpha.40`만 atomic push했다. 이전 81개 Release·82개 태그·자산·Latest 정식 v1.0.6·#5 open/#6 open 상태를 보존했다. 게시 문서는 별도 main 커밋으로 보충하며 태그/산출물은 바꾸지 않는다. 게시 영수증은 `artifacts/releases/v1.0.7-alpha.40/`에 보관한다. 실제 Windows/Unity/Harmony·사용자 세이브·물리 입력·지연/FPS/GC와 #6 완료는 대기다.
