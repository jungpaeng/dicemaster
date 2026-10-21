# v1.0.7-alpha.28 — 지도 후속 주제 조회 복구

<!-- alpha28-publication -->
alpha.28 후보의 구현·자동 검증을 진행했다. 커밋 고정 재빌드·게시 검증은 아래 게시 기록으로 확정한다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha28-publication -->

지도에서 초회 인사는 보이지만 사사형 하위의 해금된 대화 주제가 빠질 수 있는 조회 경로를 보완한다. 마지막 Lua 관찰 대신 현재 게임 LuaManager의 기존 연결을 다시 읽고, 같은 CheckPointManager에 속한 실제 등록과 대화 초기화 목록을 확인한다. 관찰 손실/GC·늦은 초기화 후에도 재로드 없이 재평가한다. 사용자 Windows 세이브의 실제 원인 전체를 확정한 것은 아니다. [설계·재현·성능·안전 경계](ISSUE-6-CURRENT-TALK-CONTEXT.md).

현재 소유자 자료가 손상/누락되거나 다른 문맥이면 이전 자료로 우회하지 않고 보류한다. 잠김/완료·반복 활동·미확정·우소매 같은 확정 거짓 조건과 일반 경로 최대1개·기회 마커 기준을 유지한다. 상세에 대화 초기화 자료의 확인 여부와 출처를 추가한다. cfg·설정 키/기본값은 변경하지 않으며 기존 패드/키보드/마우스/터치·입력 소유권/해제/중립과 창 가림을 유지한다.

## 자동 검사와 성능 경계

검토 후보 전체21개 검사 프로그램·284개 빌드 입력·113개 소스/리소스·16개 고정 참조, 실제 후보 DLL IL/메타데이터 연결·설치 ZIP의 두 경로/CRC/DLL/cfg 바이트와 배포 검증기 회귀20개를 통과했다. 커밋 고정 재빌드·게시·공개 다운로드 검증은 게시 후 기록한다.

- PASS: MapGuideRouteIntegrationTests 3595 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: InformationRuntimeTests: 5774 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: 2284 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures.
- PASS: MapRouteTests 6884 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed.
- PASS: ChoiceStoryTests 7718 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.

원문 Lua의 해시를 대조하고 합성 직렬화 조건과 Unity/게임 경계를 사용했다. 실제 Lua·게임·Windows·화면/물리 패드/FPS 실행은 아니다. 원문 남제자방→사사형→조건부 주제에서 관찰 손실/GC·늦은 자료 준비 복구, 다른 체크포인트/잘못된 raw 등록·미초기화/손상과 이전 자료 우회 차단을 검사한다. 변경된 기본값·잠김/완료 필터와 임시 변경까지 검출하는 State 쓰기 횟수도 확인한다.

alpha.27 고정 소스와 동일한 초기화 의존 2단계 유효 경로를 호스트에서 각300조회×6회 측정했다. 조회 p95 중앙값은0.014104→0.014688ms, 조회당 할당은9840→10144B였다. 소량의 검증 비용이 늘었으며 warm Lua 재읽기·재분석·실제 플래그 쓰기/게임 실행은0이다. 순서 고정 합성 표본으로 Windows FPS를 보장하지 않는다. 정상0.25초/실패5초와 기존 읽기/분석 예산을 유지한다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.28.zip | 374892 | `e2968d3e5d8652294a6a9b63188c557c0dda83f233bc81c74c15c4efa1bb47fa` |
| DiceMaster.dll | 890368 | `08290bea2c0d217713991a79c12584b4a4cf64ca7fb13ca1438cab5972ed363a` |
| 기존 cfg (변경 없음) | 13205 | `94d29dcdc35ad3b915aa3b77678a218a55be7a2b9d59d3544f6b097550fd06d0` |

## 설치와 새 Windows 확인

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 **설치 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다.

초회 인사를 이미 본 세이브에서 지도 남제자방과 방 안 사사형을 비교하고 실제로 해금된 ‘안색’·‘색종이’ 주제의 경로를 확인한다. 초회 미완료/완료, 잠김·완료 주제와 반복 대련만 남은 상태·로드/창/재진입·OFF/ON, 행동력/임무 우선·기존 입력과 상점/분기/로그/#5를 구분해 확인한다. 누락 시 ‘대화 초기화 자료’ 확인 여부·출처와 조건/진입 가상 진단을 기록한다. [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha28--현재-게임-lua-연결후속-주제-확인-대기).

새 DLL의 Windows·물리 입력·프레임 검증과 PRD #6 전체 완료는 대기다. 이전 후보의 사용자 부분 확인을 새 DLL 전체 통과로 승계하지 않는다. 기존 Latest v1.0.6, 이전 Release/태그/자산과 #6 open을 보존한다. 검토 후보·측정 원자료는 artifacts/issue-6/current-talk-context/, 고정 커밋·게시·익명 다운로드 영수증은 artifacts/releases/v1.0.7-alpha.28/에 구분한다.
