# 지도 로그의 포커스·자료 상태 — alpha.31

<!-- alpha31-publication -->
v1.0.7-alpha.31 게시 준비 중. 로컬 전체21개 자동 검사·실제 DLL 연결·설치 ZIP 검증을 통과했다. 고정 커밋 재빌드·공개 다운로드는 게시 과정에서 기록한다. 이번 변경은 로그·진단 보완이며 최초 불러오기 흰색 마커 누락 버그는 미해결이다. 실제 Windows 확인과 PRD #6 완료는 대기다.
<!-- /alpha31-publication -->

2026-10-22, 공개 alpha.30 이후 main에서 보완했다. 기준 main은 `99ec7537d6bc2c6aef443f9e0760ad4fa380e4a8`, 공개 DLL 소스는 `0247491826dbd2d74f898ab8bbc194331c66be3d`다. 이 변경은 기존 alpha.30 설치 ZIP에 포함되지 않는다. alpha.31 게시를 준비하며 고정 소스·공개 파일 검증은 [게시 기록](ISSUE-6-ALPHA31-RELEASE.md)에 확정한다. 실제 Windows 확인은 대기다.

## 보고와 확인한 경계

사용자는 게임 재실행 후 저장 지도에서 흰색 배지가 없지만 방 안 사사형 툴팁은 보인다고 보고했다. 초회 대화 뒤 다른 장소에 배지가 생기고 이전 세이브를 같은 프로세스에서 다시 불러오면 남제자방에도 표시됐으며, 최초 상태의 지도 로그에는 요청한 `지도 조회 성능`과 `사사형`이 모두 없었다.

기존 지도 제공자는 현재 포커스가 없으면 장소 무관 임무 목록을 읽었다. `지도 조회 성능`도 포커스 장소 상세 조회가 성공한 뒤에만 추가했다. 실제 제공자·경로·조건 reader와 명시적 Unity/게임 경계에서 같은 자료를 유지한 채 선택 → 해제만 바꾸어, 두 행이 함께 사라지는 것을 재현했다. 따라서 진단 행 부재 자체로 조건 자산 부재·횟수 분기·마커 그리기 중 어느 원인인지 단정할 수 없다. 사용자 실행에서 로그를 열 때 정확히 어떤 포커스 해제가 일어났는지는 아직 확인하지 않았다.

alpha.30의 이미 로드된 정의 자료 대체 경로에도 한계가 있다. 필요한 실제 대화 초기화 owner가 로드되지 않은 경우와 현재 owner가 없는 횟수형 분기는 여전히 보류될 수 있다. 이 후보는 최초 불러오기 마커 누락 자체를 해결했다고 주장하는 변경이 아니다.

## 변경

- 자동 화면의 현재 포커스와 명시적 로그의 마지막 선택 장소를 분리한다. 포커스 해제 뒤 자동 툴팁은 사라지지만 지도 안내 도구는 마지막으로 선택한 유효한 장소를 새로 조회할 수 있다. 진단에는 `현재 지도 포커스`와 `상세 조회 대상 · 마지막 선택 장소`를 구분한다.
- 마지막 대상은 기존 등록 목록의 약한 컨트롤러 연결만 이용한다. 숨겨지거나 파괴된 장소를 찾아 살리지 않는다. 새 게임/불러오기/장면 전환·정보 기능 OFF 정리·Lua 문맥 무효화·종료 시 마지막 상세 대상과 실패 진단을 버린다. 자동 조회 범위·마커·포커스·입력은 바꾸지 않는다.
- `지도 조회 성능`은 명시적 로그에서 장소 선택 전·미등록·상세 조회 실패에도 볼 수 있다. 등록 장소 수, 현재/마지막 대상, 경로 준비 상태, 마지막 상세 조회 예외 종류, 기존 로드 자산 네 종류와 시간 표본을 보여준다. `미조회`와 자료 부재를 구분한다.
- 로그 런타임에서 캐시 진단을 실제 장소 조회 성공과 독립적으로 게시한다. 실패한 본문 행은 기존 읽기 실패/이전 값 상태를 유지하며5초 후 재시도한다. 진단 행을 갱신하기 위해 본문을 다시 읽거나 실패를 정상 결과로 바꾸지 않는다.
- 자료 진단은 기존 핸들과 캐시를 읽는다. Resources 검색/로드·Lua 준비·새 조건 판정·이벤트 실행을 추가하지 않는다. 전체 장소 분석이나 마커 계산을 진단 목적으로 시작하지 않는다. 현재 조건은 실제 상세 조회에서 새 reader로 확인한다.

기존 정상0.25초/실패5초, 숨김·게임 창 가림·OFF 조회 중단, warm Lua 재분석 없음과 읽기 전용을 유지한다. 설정 키·기본값·패키지 cfg는 alpha.30과 같다. #5 이벤트 선택·예약·로그 네 탭·주사위·상점·실제 분기 및 Windows Xbox/XInput·키보드·마우스·터치·입력 소유권/해제/중립 보호의 동작을 바꾸지 않는다.

## 검사와 남은 확인

실제 제공자/경로 reader 검사에는 포커스 전 진단, 해제 뒤 상세 보존·현재 조건 재조회, 자동 오버레이 부활 방지, 문맥 정리·과거 요청 거절, 메타데이터 실패·숨김/종료, Lua/자산 검색·게임 변경 없음 회귀를 추가한다. 실제 정보 런타임에는 미등록·최초 실패·이전 값 실패 중 독립 진단 게시, 중복 방지·실패5초 재시도·창 가림·OFF 중단을 추가한다. 실제 후보 DLL의 진단 연결과 읽기 전용 경계도 검사한다.

전체21개 검사 프로그램·실제 후보 DLL 연결·설치 ZIP 검증을 통과했다. 아래에 검사 결과와 로컬 후보 파일을 기록한다. 실제 Windows의 포커스/로그 열기·Xbox 입력·사용자 저장·지연/FPS는 별도 [확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha31--지도-로그의-대상자료-상태-확인-대기)로 남긴다. #6 전체 완료·이슈 종료는 대기다.

## 자동 검사·로컬 후보 결과

SDK 8.0.426, 빌드 입력 296개·생산 소스/리소스 119개·고정 게임 참조 16개의 동일성을 확인했다. 전체21개 검사와 설치 ZIP 두 경로·CRC·내부 DLL/cfg 바이트를 대조했다. cfg는 alpha.30과 바이트까지 같다. git diff --check도 통과했다.

- MapInformationTests: PASS: 679 map/one-shot managed boundary assertions; Unity/Windows runtime not executed.
- MapGuideRouteIntegrationTests: PASS: MapGuideRouteIntegrationTests 3748 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- InformationRuntimeTests: PASS: InformationRuntimeTests: 6016 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- ChoiceStoryTests: PASS: ChoiceStoryTests 7719 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- GamepadTests: PASS: 262831 assertions.

관리 코드와 명시적 Unity/게임 경계 검사이며 실제 Windows/Harmony/native 화면·물리 패드·FPS 실행이 아니다. 진단 formatter의 실제 DLL 연결과 자산 검색/준비를 추가하지 않는 경계를 포함한다. 최초 full 검사에서 기존 ‘선택 해제 뒤 명시적 상세도 전체 임무로만 전환’ 기대가 새 동작과 충돌했다. 자동 조회의 기존 전체 임무 범위와 명시적 상세의 마지막 장소 범위를 각각 검사하도록 갱신했고 최종 전체 검사는 모두 통과했다. 이전 실패 로그도 별도 보존했다.

| 로컬 후보 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| `artifacts/issue-6/map-log-diagnostics/candidate/DiceMaster.dll` | 916992 | `d327aa184340fba7d606018930b4b42f77d6a43b53a6cf3616a032abe34bb409` |
| `artifacts/issue-6/map-log-diagnostics/candidate/DiceMaster-source-candidate.zip` | 382198 | `025a899df3e91a588e64dedda83665c83eaefebb1d8e8bea2d535eb5494a4bda` |
| `기존 패키지 cfg (변경 없음)` | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

게임 종료 후 DLL/cfg를 백업하고 새 로컬 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존한다. 이 검토 후보를 소스 커밋 고정 재빌드와 대조해 alpha.31로 게시한다. 게시 상태는 위 기록을 따른다. 공개 alpha.30은 별도 기존 파일이다. 재현/검증 로그와 최종 영수증은 artifacts/issue-6/map-log-diagnostics/에 남긴다.
