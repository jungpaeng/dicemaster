# v1.0.7-alpha.27 — 지도 진입 후 대화 조건 안내

현재 상태는 게시 준비다. 사용자의 커밋·prerelease 지시에 따라 main의 소스를 고정해 재빌드하고 검토 후보와 대조한다. 새 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.

지금은 잠겨 있어도 장소 클릭 시 설정되는 플래그로 열리는 대화 선택지를 지도 툴팁에서 확인할 수 있도록 한다. 진입 조건을 안전하게 판독한 가상 값만 사용하며 실제 플래그를 임시 변경·복원하지도 않는다. 남제자방의 현재 Talk_Brother4=0/비교1 보고를 계기로 일반적인 직렬화 진입 설정을 처리하며 인물·플래그 예외 목록을 추가하지 않는다. 사용자 세이브 자체를 재생한 Windows 검증은 아니다.

미확정/잠김·완료·반복 활동 필터, 초회·해금 후속 경로 최대1개와 이어 읽기, 마커/기회 수·행동력/임무 우선순위를 유지한다. 명시적 로그는 현재 조건과 진입 후 가상 조건을 구분한다. [설계·외부 비교·성능 측정](ISSUE-6-POSITION-ENTRY-PROJECTION.md).

설정·cfg 바이트는 변경하지 않았다. EnableMapGuide 및 기존 상위/개별 OFF 규칙에 속한다. 게임 조건·Lua·RNG·세이브·능력치·아이템을 실행/변경하지 않고 정상0.25초/실패5초·숨김/OFF 중단·정상 Lua 재분석 없음과 기존 입력을 유지한다.

## 자동 검사와 성능 경계

전체 소스 빌드의21개 검사 프로그램·283개 입력·113개 소스/리소스·16개 참조, 실제 후보 DLL IL/참조 연결·설치 ZIP의 두 경로/CRC/DLL/cfg 바이트와 배포 검증기 회귀20개를 통과했다. 고정 커밋 재빌드·공개 파일 검증 결과는 게시 후 보충한다.

검토 후보의 주요 성공 기록:

- PASS: MapGuideRouteIntegrationTests 3580 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: InformationRuntimeTests: 5737 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: 2274 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures.
- PASS: MapRouteTests 6884 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed.
- PASS: ChoiceStoryTests 7650 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.

SHA 원본 Lua+합성 직렬화 조건과 관리 코드/입력 경계 검사이며 실제 게임·Lua/Windows·물리 패드 실행은 아니다. 실제 IL/메타데이터와 State 쓰기 횟수 검사는 임시 변경도 검출한다. 초기 대역/한도 표본 오류를 수정한 전체 재실행을 최종 결과로 삼고 이전 실패 로그는 별도 보존한다.

호스트 생산 공급자의 같은 경로 표본에서 진입 그룹0/1/32개별 조회 p95 중앙값0.016834/0.022709/0.168500ms, 할당10040/11236/47514B였다. 각300회×3회이며 warm Lua 재읽기/구문 재분석·실제 플래그 쓰기0이다. 가상 값 해석의 추가 비용과 할당이 있으며 실게임 FPS나 전체 버전 비교가 아니다. 정상0.25초는 유지하고 실제 최초/warm 프레임/GC는 대기한다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.27.zip | 374383 | `faf422b68a5b96ef6a18abbfbc724c1774e33e0ad066529cc43bccfd3ad639d2` |
| DiceMaster.dll | 889344 | `4a09a024cffd5d7ba71811c8a1df26eaf2c07f4873c8e206fe6475bb10e624a4` |
| 기존 cfg (변경 없음) | 13205 | `94d29dcdc35ad3b915aa3b77678a218a55be7a2b9d59d3544f6b097550fd06d0` |

## 설치와 새 Windows 확인

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 **설치 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다.

남제자방 포커스에서 초회 인사/해금 주제 안내, 완료·잠김·반복 대련만 남은 경우의 제거, 로그의 현재/진입 가상 값 구분, 기존 기회 마커·행동력/우선순위·창 가림·Xbox/XInput/키보드/마우스/터치와 #5/로그 네 탭/예약/주사위/상점/실제 분기 기록을 확인한다. 초기화 자료 미확인·미지원/난수 진입·행동 소비 뒤 조건은 안전하게 확정하지 못하면 안내를 보류한다. [확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha27--진입-플래그-가상-조회-확인-대기).

검토 후보·측정 원자료는 artifacts/issue-6/entry-flag-projection/, 고정 커밋·게시·공개 다운로드 영수증은 artifacts/releases/v1.0.7-alpha.27/에 구분한다. 기존 Latest v1.0.6과 이전 Release·태그·자산·이슈 상태를 보존하며 #6을 종료하지 않는다.
