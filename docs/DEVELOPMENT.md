# 개발 안내

## alpha.37 — 카드 선택지의 실제 이름 기준 배치

<!-- alpha37-publication -->
2026-10-22 23:53:20 KST에 소스 [`15d0962`](https://github.com/jungpaeng/dicemaster/commit/15d0962e821d41eec0ef5bcc041e3c806f714ccd)의 [alpha.37](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.37)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **카드 선택지의 이름 옆 마커/스피너·카드를 피하는 툴팁을 구현했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha37-publication -->

alpha.37은 alpha.36의 카드 배치를 유지하면서 지도·일반 선택지의 같은 좌표를 한 번 그리는 동안 재사용해 중복 투영을 줄인다. 다음 그리기와 원천 조회는 새 좌표를 읽는다. 표시·판정·입력 규칙은 같다.

PositionOptionButton/BreakOptionButton의 활성 _titleText와 이미 생성된 Text.m_TextCache 정점만 읽어 글자 경계를 투영한다. 비활성 포커스 변형·공백/종료 정점은 제외하고 텍스트 생성/레이아웃 갱신/게임 UI 쓰기를 호출하지 않는다. 같은 Draw의 기하를 재사용하고 finally에서 참조를 해제한다. 기존 예약 조회가 모든 실제 버튼 anchor를 발행하며, 카드 그림/설명까지 포함한 장애물 최대64개와 고정8개 후보로 툴팁을 배치한다. [근거·측정·한계](ISSUE-6-CARD-ANCHORS.md) · [후보](ISSUE-6-ALPHA37-RELEASE.md).

## alpha.36 — 카드 선택지의 실제 이름 기준 배치

<!-- alpha36-publication -->
2026-10-22 23:42:22 KST에 소스 [`b20d898`](https://github.com/jungpaeng/dicemaster/commit/b20d898d01fa33b85823090cff9e1a392940c63f)의 [alpha.36](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.36)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **카드 선택지의 이름 옆 마커/스피너·카드를 피하는 툴팁을 구현했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha36-publication -->

PositionOptionButton/BreakOptionButton의 활성 _titleText와 이미 생성된 Text.m_TextCache 정점만 읽어 글자 경계를 투영한다. 비활성 포커스 변형·공백/종료 정점은 제외하고 텍스트 생성/레이아웃 갱신/게임 UI 쓰기를 호출하지 않는다. 같은 Draw의 기하를 재사용하고 finally에서 참조를 해제한다. 기존 예약 조회가 모든 실제 버튼 anchor를 발행하며, 카드 그림/설명까지 포함한 장애물 최대64개와 고정8개 후보로 툴팁을 배치한다. [근거·측정·한계](ISSUE-6-CARD-ANCHORS.md) · [후보](ISSUE-6-ALPHA36-RELEASE.md).

## alpha.35 — 항목별 마커 준비 상태

<!-- alpha35-publication -->
2026-10-22 23:07:45 KST에 소스 [`cff13f2`](https://github.com/jungpaeng/dicemaster/commit/cff13f2960f27a9c8f32c6f0b335ff86d49beae5)의 [alpha.35](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.35)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **지도·선택지의 마커 준비 스피너를 구현했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha35-publication -->

MapMarkerSnapshot/ChoiceStorySnapshot의 Preparing을 실제 마커 발행과 함께 만든다. 음성 첫 검사 진행 기록은 조건 결과를 캐시하지 않으며, 현재 조건/ReadStamp와 기존 예산을 유지한다. 렌더러는 지연 시간만 관리하고 원천 조회·분석을 시작하지 않는다.32KiB 스피너 그림8프레임을1회 업로드해 Repaint당 UV 하나로 그린다. 가림/OFF/문맥의 지연 정리와 기존 배지 해제 수명을 따른다. [수명·검사·성능](ISSUE-6-LOADING-INDICATORS.md) · [후보](ISSUE-6-ALPHA35-RELEASE.md).

## alpha.34 — 고정 날짜 정의와 현재 시각 분리

<!-- alpha34-publication -->
2026-10-22 21:58:37 KST에 소스 [`cf10c0a`](https://github.com/jungpaeng/dicemaster/commit/cf10c0ab37d30ec125ef16baee2edfd0765e1c7e)의 [alpha.34](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.34)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **고정 날짜 조건 누락에 따른 연단방 첫 대화의 지도 안내를 보완했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha34-publication -->

DMD2는 GameTimeData 위치를 검토한 고정 날짜37개와 native 현재/임무 시각19개로 구분한다. 플레이어 시계·GameTimeDataCollection은 파일 기본값을 빌리지 않는다. 기존 native 참조/충돌을 우선하고, 없는 고정 날짜만6개 파일 지문 확인 후 private DTO로 읽는다. 현재 입력 ReadStamp·엄격 증명 분리·작업 스레드와 기존 수명/준비 예산을 유지한다. [근거·스키마·상한·검사](ISSUE-6-FIXED-CALENDAR.md) · [후보](ISSUE-6-ALPHA34-RELEASE.md).

## alpha.33 — 마커 경로의 입력 변화 확인

<!-- alpha33-publication -->
2026-10-22 21:20:36 KST에 소스 [`6242395`](https://github.com/jungpaeng/dicemaster/commit/62423951e9e429caf592818ac683a64d838e17e6)의 [alpha.33](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.33)을 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **지도 마커의 초기 준비·입력 변화 확인·깜빡임 경계를 보완했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha33-publication -->

MapGuideSource의 지도 준비에도 PrioritizeRootPreparation을 적용한다. PreviewConditions.ReadStamp는 성공한 일반 경로가 읽은 현재 값·기존 소유자/자료 참조·배열 구성·List revision·경로 준비 상태의 제한된 읽기 계획을 저장한다. 다음 조회에서는 그 입력부터 검증하고 동일할 때만 경로 판정과 행/문자열 생성을 생략한다. 오래된 참 값을 무조건 유지하지 않으며 실패·변경·한도 초과는 새 판정으로 돌아간다.

이미 표시된 장소의 입력 검증은 선택적 준비의 시간 예산보다 먼저 수행한다. 입력이 동일한 성공 경로는 새 탐색의 경로/조건 예산을 소모하지 않으므로 신규 장소를 막지 않는다. 정상0.25초/실패5초와 Lua1회·8192작업·선택적 준비 약5ms를 유지한다. 5ms는 기존처럼 협력식 준비 예산이며 전체 프레임의 하드 상한이 아니다. [상한·수명·검사·성능](ISSUE-6-MARKER-CACHE.md) · [후보·배포](ISSUE-6-ALPHA33-RELEASE.md).

## alpha.32 — 설치 파일 정의와 현재 값의 분리

MapDefinitionFileReader는 SHA-256·크기·인덱스·스키마를 검증한 설치 파일의 조건/분기/초기화 정의만 읽는다. 내장 MapDefinitions.index에는 파일 지문·타입·오프셋만 있고 게임 payload는 없다. State/시간/능력치/실행 count를 디스크에서 복구하지 않는다. MapFileDefinitions는 기존 등록 장소의 현재 missions/player 카탈로그로 정확한 타입/이름의 입력 객체를 연결한다. 불명·중복 객체는 미확정이며 현재 Story owner가 우선한다. 파일 정의는 경로 reader 전용으로 기회·1회성 권한에 쓰지 않는다.

해시·해석은 취소 가능한 worker, Unity 이름/기존 참조 수집은 main이다. worker에는 Unity/game API가 없고 설치 파일은 Open/Read/ShareRead다. 문맥별 캐시·5초 IO 재시도·지원 불일치 보류·가림/숨김 취소를 분리한다. 독립 경로의 해금/완료/활동 판정을 유지하면서 무관한 풀 실패의 전역 차단만 제거한다. 인덱스 재생성·검사·지원 지문은 [상세 문서](ISSUE-6-FILE-DEFINITIONS.md), [배포 기록](ISSUE-6-ALPHA32-RELEASE.md)을 따른다. 실제 Windows 확인은 대기다.


## alpha.31 — 지도 상세 대상과 독립 진단

<!-- alpha31-publication -->
2026-10-22 17:17:04 KST에 소스 [`cc656fc`](https://github.com/jungpaeng/dicemaster/commit/cc656fc901466aba0c105e2b17c4c508ab672094)의 [alpha.31](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.31)을 게시했다. 전체·고정 커밋 자동 검사21개·실제 후보 DLL 연결·설치 ZIP과 익명 공개 다운로드 검증을 통과했다. **로그·진단 보완이며 최초 불러오기 흰색 마커 누락 버그는 미해결이다. 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha31-publication -->

MapGuideSource의 detailPlace는 기존 Place의 약한 컨트롤러만 참조하며 자동 selectedPlace와 별도로 사용한다. 문맥 정리에서 지우고 지연된 이전 요청은 문맥 키로 거절한다. InformationRuntime은 명시적 지도 도구에만 DiagnosticRow를 독립 게시하여 실패5초 재시도를 우회하지 않는다. 정의/현재값 읽기나 자산 검색을 진단 formatter에서 시작하지 않는다. [구현 경계·회귀·실제 Windows 대기](ISSUE-6-MAP-LOG-DIAGNOSTICS.md).

## alpha.30 — cold map의 읽기 전용 정의 자료

<!-- alpha30-publication -->
2026-10-22 16:22:23 KST에 소스 [`0247491`](https://github.com/jungpaeng/dicemaster/commit/0247491826dbd2d74f898ab8bbc194331c66be3d)의 [alpha.30](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.30)을 게시했다. 전체·고정 커밋 자동 검사21개·실제 후보 DLL 연결·설치 ZIP과 익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·사용자 저장·물리 입력·최초 지연/FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha30-publication -->

PreviewConditions의 route reader에만 이미 로드된 설정 자산 대체 경로를 제공한다. MapDefinitionAssets는 세 설정 타입과 StatModifyManager의 직렬화 _talkOptionFlag만 조회한다. 위치/GameObject 검색·Resources.Load·생성·Init/Reset·Lua 실행이 없고 현재 owner의 손상/서로 다른 자료·횟수형 실행 값은 미확정이다. Unity가 파괴한 객체를 잡고 있는 CLR 정적 참조를 현재 owner로 취급하지 않는다. 정상 갱신은 정의 참조를 재사용하고 현재 값은 새로 읽는다. [수명·상한·회귀·성능·Windows 경계](ISSUE-6-COLD-MAP-LOAD.md). alpha.30 후보이며 게시 상태와 고정 소스는 [게시 기록](ISSUE-6-ALPHA30-RELEASE.md)을 따른다.

## alpha.29 — 지도·선택 메뉴의 안내 배지

<!-- alpha29-publication -->
2026-10-22 15:04:05 KST에 소스 [`0bee1e2`](https://github.com/jungpaeng/dicemaster/commit/0bee1e26dcc857ca87b7da0b92967befde8eafd4)의 [alpha.29](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.29)를 게시했다. 전체·고정 커밋 자동 검사21개·실제 후보 DLL 연결·설치 ZIP과 익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·가독성·물리 입력·프레임 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha29-publication -->

InformationRow.Guidance와 기존 Opportunity를 분리하고 HasBadge에서 합친다. 금색 기회가 우선하고 Guide ON의 현재 대화/경로만 밝은 배지를 받는다. 지도는 보이는 장소 최대64개를 순환하며 선택 장소와 배경의 새 정의 준비가 한 갱신당8192작업·Lua1회/풀1회·협력식 약5ms를 공유한다. 현재 조건/가상 진입값은 장소별 Reader에만 두고 DefinitionScope는 한 갱신의 이름/정의 색인만 공유한다. EndScheduledReads의 finally에서 참조를 지우며 최대4개의 빈 색인 버퍼만 남긴다. [상세 설계·재현 가능한 벤치마크·자동 검사/Windows 구분](ISSUE-6-GUIDANCE-BADGES.md).

이 변경은 main의 alpha.29 후보이며 게시 상태는 위 기록과 [설치·검증 기록](ISSUE-6-ALPHA29-RELEASE.md)을 따른다. 아래 alpha.28 이하의 게시·검증 이력은 해당 파일의 기록이다.

## alpha.28 — 현재 게임 Lua 연결에서 대화 초기화 자료 조회

<!-- alpha28-publication -->
2026-10-22 06:59:56 KST에 소스 [`96a78a3`](https://github.com/jungpaeng/dicemaster/commit/96a78a30da8ba14eae2e0fc201576c96001f2c48)의 [alpha.28](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.28)을 게시했다. 전체·고정 커밋 자동 검사21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha28-publication -->

PreviewConditions는 기존 LuaManager의 Instance backing field와 직렬화 _luaEnvironment를 읽고 raw checkpointmanager가 현재 CheckPointManager와 같은 객체인지 확인한다. raw statmodifymanager의 실제 _talkOptionFlag와 _default만 조회한다. 마지막 컴파일 관찰의 약한 참조가 사라져도 현재 소유자에서 복구하며, 현재 소유자 자료가 손상되면 이전 관찰로 우회하지 않는다. 관찰 대체 경로도 문맥·raw 연결을 재확인하고 로드/장면/종료·두 기능 OFF에서 정리한다. [설계·재현·성능](ISSUE-6-CURRENT-TALK-CONTEXT.md) · [후보·실제 Windows 대기](ISSUE-6-ALPHA28-RELEASE.md).

## alpha.27 — 읽기 전용 진입 플래그 예측

<!-- alpha27-publication -->
2026-10-22 06:29:59 KST에 소스 [`2a176ac`](https://github.com/jungpaeng/dicemaster/commit/2a176ac90ebe82bc397955409e54e1e0c5c1ad8c)의 [alpha.27](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.27)을 게시했다. 전체·고정 커밋 자동 검사21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha27-publication -->

PreviewConditions.Reader.ProjectPositionEntry가 직렬화 조건을 해석해 조회별 가상 값에 순서대로 대입한다. 조건32그룹/대상32개·2,048단계 하위 한도는 기존 조건16,384단계 안에서 소비한다. strict 증명·기회 마커에는 적용하지 않는다. 전용 한도 예외로 남은 피연산자 순회를 중단하며 파생 getter·행동 소비 뒤 조건은 보수적으로 보류한다. [상세 설계·검사·측정](ISSUE-6-POSITION-ENTRY-PROJECTION.md) · [후보·실제 확인 대기](ISSUE-6-ALPHA27-RELEASE.md).

## alpha.26 — 지도 이야기의 공통 필터·이어 읽기

<!-- alpha26-publication -->
2026-10-22 05:40:32 KST에 소스 [`4dc837c`](https://github.com/jungpaeng/dicemaster/commit/4dc837c25824952078f0cddc0df0ba8e41831eb0)의 [alpha.26](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.26)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha26-publication -->

MapRoutePath.ReadGuidance를 지도/현재 선택 화면이 공유한다. MapRouteDefinitions.StoryPaths의 append-only 정의 목록을 선택 장소당64셀씩 색인하고, 현재 조회는32경로/조건·분류 예산의 커서에서 이어 진행한다. 직전 성공 경로는 정의 참조만 보관하고 새 Reader로 재확인한다. [원인·안전 경계·성능·검사](ISSUE-6-MAP-STORY-SCAN.md). alpha.26이며 아래 alpha.25의 설치 파일/확인 이력과 구분한다.

## alpha.25 — 후속 경로 가상 조회

<!-- alpha25-publication -->
2026-10-22 04:43:58 KST에 소스 [`e81a0c4`](https://github.com/jungpaeng/dicemaster/commit/e81a0c420602c5cbaa472a59d1617131e830e3f9)의 [alpha.25](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.25)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha25-publication -->

현재 선택지/선택 장소에만 확인된 대화 초기화 기본값을 가상 적용한다. 지도 진입 쓰기는 읽기 대상만 마스킹하고 조건을 실행하지 않는다. 기회 마커는 기존 읽기로 분리한다. 리터럴 분기 이어 처리·조회 내부 색인/노드 재사용·확인한 경로의 조기 공개를 적용했다. [설계·원본 재현·성능 교환·검증](ISSUE-6-ROUTE-PROJECTION.md). 정상 .25초/실패 5초·기존 분석 상한·입력 계약은 유지하며 새 설정은 없다. alpha.25이며 실제 Windows 확인은 대기다.

## alpha.24 — 현재 원문과 후속 준비의 공개 시점 분리

<!-- alpha24-publication -->
2026-10-22 03:30:55 KST에 소스 [`4849759`](https://github.com/jungpaeng/dicemaster/commit/484975971727410de78ec0bedc6c4ab089b4d0c1)의 [alpha.24](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.24)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha24-publication -->

선택 공급자만 현재 스크립트 작업을 먼저 처리한다. 두 작업 큐는 기존 하나의 예산/누적 상한을 공유하고 현재 메뉴 범위가 준비된 경우만 루트 직접 대사를 먼저 공개한다. 후속 경로·기회 마커는 기존 완료/해금 검사를 유지한다. 정의 준비 상태에 현재 조건 값을 캐시하지 않는다. [설계·교환·검사](ISSUE-6-LOCAL-CHOICE-PREPARATION.md).


## alpha.23 — 원문 관찰과 실행 변환의 경계

<!-- alpha23-publication -->
2026-10-22 03:06:00 KST에 소스 [`fc5dc7f`](https://github.com/jungpaeng/dicemaster/commit/fc5dc7fabc0977b01ee6acdb92a3987148e77370)의 [alpha.23](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.23)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha23-publication -->

선택 안내는 로드의 첫 Prefix에 저장한 호출별 원문을 성공 Finalizer에서 분석한다. #5의 마지막 Prefix가 만든 실행용 래퍼는 이야기 정의가 아니다. 실제 변환기·관찰 훅·원본 corpus·DLL 우선순위를 함께 검사한다. [원인·설계·한계](ISSUE-6-CHOICE-LOAD-SOURCE.md).


## alpha.22 — 직접 대사 우선 경로 정의

<!-- alpha22-publication -->
2026-10-22 02:45:06 KST에 소스·주석 태그 [`bbf7024`](https://github.com/jungpaeng/dicemaster/commit/bbf7024db49acd271061f2903ae2b12e9a44e77e)의 [alpha.22](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.22)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·274개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 63개 Release·64개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·화면·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA22-RELEASE.md).
<!-- /alpha22-publication -->

Binding.DirectPaths는 정의 색인에서 현재 선택의 TopicStep인 경로 최대 32개를 앞에 배치한다. 값 조회·게임 상태에 따른 정렬이 아니며 전체 경로 공통 불가 교집합은 그대로 유지한다. per-query 32개 상한과 정상 주기 캐시를 보존한다. [설계·한계·검증](ISSUE-6-DIRECT-CHOICE-PRIORITY.md).


## alpha.21 — 선택 안내 근거·활동 독립성

<!-- alpha21-publication -->
2026-10-22 02:19:17 KST에 소스·주석 태그 [`9163d59`](https://github.com/jungpaeng/dicemaster/commit/9163d5931a9e95bb614f77dcfb48bbb45519b376)의 [alpha.21](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.21)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·273개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 62개 Release·63개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA21-RELEASE.md).
<!-- /alpha21-publication -->

Reader의 플래그 독립성은 nullable bool로 증명/실제 영향/미확정을 구분한다. 정수 횟수형 스키마 검증은 플래그 독립성만 증명하며 plain Switch의 값을 읽거나 실행하지 않는다. 고정 활동의 미확정 독립성은 상세로 제한하고, 실제 조건 소모는 기존 이야기 후보로 남긴다. 준비 중 위치/색인에서 임시 툴팁을 만들지 않는다. 명시적 상세만 표시/제외 표본 각 한 건을 진단한다. [설계·범위·검증](ISSUE-6-CHOICE-EVIDENCE.md).


## alpha.20 — 완료 진입 대화와 고정 활동 반응

<!-- alpha20-publication -->
2026-10-22 01:49:00 KST에 소스·주석 태그 [`07363bc`](https://github.com/jungpaeng/dicemaster/commit/07363bce02119bd9734d1b69c0440c613885fcb8)의 [alpha.20](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.20)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·272개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 61개 Release·62개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA20-RELEASE.md).
<!-- /alpha20-publication -->

NextSwitch는 첫 안전한 루트 호출에만 부여하며 일반 횟수형 Switch의 보호를 바꾸지 않는다. 현재 필수 조건의 거짓만 억제에 사용하고 전체 미래 이야기나 마커를 확정으로 올리지 않는다. 최종 경로 선택에서도 반복 활동 분류를 재사용해 fallback의 재유입을 막는다. 같은 갱신 Reader와 기존 조건/분류 예산을 공유한다. [설계·한계·검증](ISSUE-6-ENTRY-DIALOGUES.md).


## alpha.19 — 후속 선택지 해금과 이야기 조건 분리

<!-- alpha19-publication -->
2026-10-22 01:06:52 KST에 소스·주석 태그 [`0b984ff`](https://github.com/jungpaeng/dicemaster/commit/0b984ff9345b1feca3831c4e25c4c2fa43f6046e)의 [alpha.19](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.19)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·271개 입력·112개 소스/리소스·16개 고정 참조와 실제 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 60개 Release·61개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [설치·파일·검증 기록](ISSUE-6-ALPHA19-RELEASE.md).
<!-- /alpha19-publication -->

`MapChoiceStep.ReadAvailability`는 닫힌 하위 메뉴 진입과 선행 쓰기·대화 플래그 초기화를 반영한 해금 조건을 읽고, `MapRoutePath.ReadChoiceAvailability`는 모든 남은 선택 단계를 확인한다. 선택 화면은 실제 현재 버튼 다음부터, 지도 이벤트는 첫 단계부터 확인한다. 해금 Unknown/NotMet은 경로 이름을 자동으로 노출하지 않는다. 이야기 내부 Unknown과 해금 Unknown을 합치지 않는다.

선택 조회의 새 해금 예산은 갱신당 32개 미캐시 조건이며 같은 `MapRouteRead`의 조건/체크포인트 캐시를 재사용한다. 마커는 기존 확정 경로·현재 이벤트/대기 등록·1회성 증명에 해금 확인을 AND로 추가한다. 기존 `.25초/실패 5초`, 숨김/OFF 중단, 렌더링 시 조회 없음, 정상 주기 Lua 재분석 없음·입력 소유권을 유지한다. 인물·플래그 예외 목록이나 게임 판정 호출을 추가하지 않는다. [설계·검사·제약](ISSUE-6-CHOICE-UNLOCKS.md).


## alpha.18 — 툴팁 조건·배지 표시 개선

<!-- alpha18-publication -->
2026-10-22 00:14:28 KST에 소스·주석 태그 [`0d7d03d`](https://github.com/jungpaeng/dicemaster/commit/0d7d03d51b4e4cbcd7aadce2269e8570994ef3b3)의 [alpha.18](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.18)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·270개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·익명 공개 ZIP 바이트 대조를 통과했다. 기존 59개 Release·60개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [설치·검증·확인표](ISSUE-6-ALPHA18-RELEASE.md).
<!-- /alpha18-publication -->

### 선택지 툴팁의 현재 필수 조건

닫힌 현재 루트 메뉴의 불가 확인 전용 필수 조건을 툴팁에 연결하고, 정의 색인에서 공통 AND 조건을 준비해 기존 개별 경로 조회 상한을 유지한다. 같은 조건 상태의 경로에서 표시 인물·경로·상태·개수를 고르며 진행 불가 판정은 상세에만 남긴다. 새 후보의 실제 Windows 확인은 대기다. [설계·검증](ISSUE-6-TOOLTIP-PREREQUISITES.md).

### 배지 크기·게임 창 가림

배지 크기를 로그 글자와 분리해 화면 비율에 맞추고, 원래 게임의 상태/설정/저장/대사 로그 창이 열리면 지도·선택 안내의 표시와 조회를 멈춘다. 기존 현재 기회 판정·입력·캐시·설정은 유지한다. 새 후보의 실제 Windows 확인은 대기다. [원인·구현·검증·확인표](ISSUE-6-BADGE-VISIBILITY.md).

## alpha.17 — 배지 내부 두루마리 변경

<!-- alpha17-publication -->
2026-10-21 22:54:40 KST에 소스·주석 태그 [`d55a999`](https://github.com/jungpaeng/dicemaster/commit/d55a9998e65270626b66a76f30cd013c08881257)의 [alpha.17](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.17)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·267개 입력·111개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 58개 Release·59개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·검증·확인표](ISSUE-6-ALPHA17-RELEASE.md).
<!-- /alpha17-publication -->

금색 테두리·바탕·크기·지도 핀/선택지 배치를 유지하고 내부 그림만 사용자가 제공한 Lucide scroll-text로 바꾼다. 원본의 ISC 고지까지 DLL에 내장하며 기존 판정·입력·설정·캐시는 유지한다. **새 후보의 실제 Windows 확인은 대기다.** [범위·검증·후보](ISSUE-6-ALPHA17-RELEASE.md).

## alpha.16 — 금색 이야기 배지와 지도 핀

<!-- alpha16-publication -->
2026-10-21 22:40:23 KST에 소스·주석 태그 [`f6f10d5`](https://github.com/jungpaeng/dicemaster/commit/f6f10d5aca59aaee04b1a7fd2b5f0f1858a1c06c)의 [alpha.16](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.16)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·267개 입력·111개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 57개 Release·58개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·검증·확인표](ISSUE-6-ALPHA16-RELEASE.md).
<!-- /alpha16-publication -->

main에서 금색 두루마리 아이콘과 화면별 배치를 구현했다. 당문 지도는 장소 선택 영역 상단 중앙의 핀/짧은 연결선, 실제 선택 메뉴는 버튼 모서리 배지다. 현재 기회 판정·일반 대화 설명·개별 설정·입력/캐시 계약은 alpha.15를 유지한다. 아이콘은 DLL 내장 이미지 한 번 업로드/재사용이며 모드 종료 시 해제한다. **새 후보의 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [구현·검증·후보](ISSUE-6-ALPHA16-RELEASE.md).

## alpha.15 — 현재 기회와 일반 대화 판정 분리

<!-- alpha15-publication -->
2026-10-21 22:03:38 KST에 소스·주석 태그 [`48ac8e7`](https://github.com/jungpaeng/dicemaster/commit/48ac8e732271410242ee2ea2c0d810a9d27c4ae5)의 [alpha.15](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.15)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·261개 입력·107개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 56개 Release·57개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·지원 범위·확인표](ISSUE-6-ALPHA15-RELEASE.md).
<!-- /alpha15-publication -->

InformationRow.Opportunity를 조건 상태 Available과 분리해 모든 붉은 점에 같은 기준을 적용한다. ChoiceOpportunities는 현재 활성 메뉴의 실제 대기 시작 스크립트/확정 Init 연결 또는 엄격한 1회성 증명·해당 선택 경로·현재 이벤트 조건의 교집합만 발행한다. 일반 대화의 상태·경로 설명은 유지한다. 조건값/멤버십은 조회별 새로 읽고 연결 색인은 64셀씩 준비해 재사용한다. 메뉴의 1회성 설정을 일반 지도 안내와 독립적으로 처리한다. [지원 한계·검사·후보](ISSUE-6-ALPHA15-RELEASE.md). 아래는 이전 버전 기록이다.

## alpha.14 — 현재 기회 표식과 native 포커스 배치

<!-- alpha14-publication -->
2026-10-21 21:01:57 KST에 소스·주석 태그 [`cfe1cf7`](https://github.com/jungpaeng/dicemaster/commit/cfe1cf7bc6e52a0b075ba057a9b686dbc8f72468)의 [alpha.14](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.14)를 게시했다. 전체·고정 커밋 재빌드 21개 검사·259개 입력·106개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 55개 Release·56개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·파일·확인표](ISSUE-6-ALPHA14-RELEASE.md).
<!-- /alpha14-publication -->

MapGuideMarkers는 실제 대기 목록 한 번/장소별 첫 항목·현재 행동력·임무 우선순위와 준비된 1회성 증명·새 현재 도달성만 발행한다. 미선택 장소의 GetSource·Lua/효과 준비·전체 임무 색인은 열지 않는다. 장소 64·대기 256·증명 후보 64/현재 이벤트 16·약 5 ms 양보와 0.25초/실패 5초/숨김·OFF 중단을 적용한다. 전체 표식의 한도가 전체 기회 없음 판정은 아니다.

InformationAnchors는 등록된 컨트롤과 collider UI/root canvas·현재 카메라의 좌표·native EventSystem 포커스만 읽는다. 전체 객체 검색·raycast·입력 소비·Select/플래그 쓰기가 없다. InformationLayout.Near의 네 위치와 cached PassiveLabel로 포커스 설명 하나를 배치한다. 정지 마우스는 패드 포커스를 빼앗지 않는다. Canvas/RectTransformUtility를 위해 제공된 UnityEngine.UIModule.dll을 추가 참조·해시 고정해 총 16개 참조를 검증하며 ZIP에 외부 DLL을 넣지 않는다. [범위·근거·검사](ISSUE-6-ALPHA14-RELEASE.md).

## alpha.13 — 자동 조회 비용과 구문 재사용

2026-10-21 18:06:04 KST에 소스·주석 태그 [`53d6f6a`](https://github.com/jungpaeng/dicemaster/commit/53d6f6a2165b3c57f786e1920ff2b114e2144719)의 [alpha.13](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.13)을 게시했다. 전체·고정 커밋 재빌드 21개 검사·254개 입력·104개 소스/리소스·15개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 54개 Release·55개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·파일·비교 기록](ISSUE-6-ALPHA13-RELEASE.md).

대기 임무의 실시간 멤버십·행동력·우선순위를 경로 준비보다 먼저 읽는다. 자동이고 1회성 OFF이면 GetSource를 호출하지 않는다. ON이어도 미증명 이벤트의 경로 현재값·이름/경로 문자열·상세 행을 만들지 않으며 명시적 상세에서 전체를 조회한다. 증명된 이벤트는 조회별 MapRouteRead를 공유하고 기존 16개/4개/1개 상한을 유지한다.

AdvanceBudgeted는 기존 8,192 작업 상한과 약 5 ms 양보 시각을 함께 확인한다. 구문/프로필은 최대 256단위 사이에서 확인하며 원래 작업을 보존해 재개한다. 동기 리소스 읽기·단일 분석·의미 처리 한 번을 중단하는 강제 프레임 제한이 아니다.

지도·선택 화면은 완료된 정상 MapRouteSyntax만 공유한다. 소유자마다 리소스를 읽어 원문을 ordinal 비교하며 최대 16개·1,310,720 문자·131,072 토큰 LRU를 유지한다. 재사용분도 소유자 문자/토큰 한도에 반영한다. 누락·변경·좁은 한도·미완성/오류는 재사용하지 않는다. 의미 작업·현재 조건·게임 객체·플래그 값은 공유하지 않는다. 일반 Lua 교체에는 이 구문만 보존하고 현재 그래프·증명은 해제하며, 전체 OFF·장면/로드 해제에는 공유 구문도 지운다.

지도 조회 성능은 선택 장소 자료 제공자 내부의 최근 64표본 p95/최대·단계별 평균을 자동 대기/자동 1회성/상세로 나눈다. GC.GetAllocatedBytesForCurrentThread가 있을 때만 할당량을 읽고 없으면 미지원으로 표시한다. 측정을 위해 GC를 실행하지 않고 파일·설정·세이브에 기록하지 않는다. 메타데이터·진단문·로그 UI·프레임 전체는 측정에서 제외한다. [검사·남은 측정](ISSUE-6-ALPHA13-RELEASE.md).

## alpha.12 — 자동 안내 범위·장소 준비분리

자동 갱신은 현재 대기 임무와 도달 가능성이 확인된 1회성에 한정한다. 후속 NPC 대화 조건과 미등록 단계 정의 색인은 명시적인 상세에서만 조회한다. 자동 목록·외 N개·패널 색 모두 같은 표시 허용 기준을 사용한다. 현재 대기 임무 → 제한된 전역 정의 색인의 순서와 별도 준비 상태, 중첩 조건의 부분 판정, 명시적 상세의 제한된 현재 조건 표본을 구현했다. 외부 색인의 설명 문자열을 정답·인물 예외로 사용하지 않고 독립 전수 계산과 실제 DLL 연결로 교차 검사한다. 구현·검사·새 Windows 확인 범위는 [장소 준비 분리 기록](ISSUE-6-INDEPENDENT-PREPARATION.md)에 둔다. 아래 alpha.11은 이전 공개 파일의 기록이다.

<!-- alpha12-current-status -->
2026-10-21 16:38:52 KST에 소스·주석 태그 [`6242514`](https://github.com/jungpaeng/dicemaster/commit/6242514be6e387bfeb9bf9c47a16cd0d8f950acc)의 [alpha.12](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.12)를 게시했다. 지도 자동 안내는 현재 실행 가능한 기존 대기 임무와 도달 가능성을 확인한 증명된 1회성으로 제한하고, 후속 대화·미등록 단계는 상세 조회로 분리한다. 전체·고정 커밋 재빌드 21개 프로그램·254개 입력·104개 소스/리소스·15개 참조·실제 DLL 연결·DLL/cfg/ZIP 동일성과 익명 공개 ZIP 대조를 통과했다. 기존 53개 Release·54개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·성능 확인과 #6 전체 완료는 대기다.** [설치·파일·확인표](ISSUE-6-ALPHA12-RELEASE.md).
<!-- /alpha12-current-status -->

## alpha.11 — 선택 주제와 활동 반응 분리

원래 main에서만 구현·검증한다. 이야기 발행 전에 선택 구간별 분기·후속 연결을 정의 캐시에 준비하고, 고정 활동 반응과 개별 이야기를 구분한다. 이번 양성 활동 프로필은 고정 대사 뒤 리터럴 Switch 하나의 평평한 두 분기가 자원 차감 후 전투/핀볼 실행과 직접 거절 종료로 나뉘는 닫힌 구조다. 그 활동에 연결된 평평한 두 결과의 보상/손실·고정 대사·종료도 활동 반응으로 묶는다. 인물명·플래그명·체크포인트 키·번역 문구의 제외 목록은 없다. 상점·음식 등 임의 활동 API 전체를 지원한다고 확대하지 않는다.

특별 조건·중첩 선택·동적/미지원 호출·불완전/상한은 분류를 확정하지 않는다. 선택 주제의 활성 조건을 앞선 쓰기가 소모할 수 있으면 그 뒤 대사를 고정 활동으로 단정하지 않는다. 쓰기와 조건의 실제 직렬화 의존 입력을 대조하며 키와 플래그 이름의 동일성을 가정하지 않는다. 대화 선택지 초기화 이후에는 기존 Lua 환경의 실제 관리자가 가진 플래그 목록을 읽기 전용으로 확인하고 영향받는 입력만 미확정으로 둔다. 목록이 없거나 잘못됐으면 플래그 의존 비교를 보수적으로 마스킹하되 독립적인 확정 false는 보존한다. 게임 판정·초기화·RNG를 실행하거나 상태값을 캐시에 저장하지 않는다. 부모 조건의 소비 여부는 발화 전 쓰기와 활동 결과 프로필 전체의 쓰기를 함께 확인한다. 같은 플래그의 모든 쓰기가 같은 상수의 리터럴 `SetFlag`이고 그 값이 현재 `FlagData.State`와 같을 때만 무변경으로 구분한다. 다른 값·충돌하는 쓰기·`AddFlag`·동적 쓰기는 이 예외로 통과시키지 않는다. 현재 State는 정확한 플래그 필드만 읽고 조회 사이에 저장하지 않는다. 앞선 ResetTalkFlag가 있으면 초기화 목록이 확정되고 해당 플래그를 포함하지 않을 때만 동일값 SetFlag 면제를 적용한다.

지도는 개별 이야기별 현재 조건을 먼저 판정하고 안내할 이야기를 합친다. 반복 활동의 고정 반응은 특별 이야기와 분리하여 로그에서 원래 선택 경로와 사유를 확인할 수 있게 한다. `Candidate.Preparing`은 실제 경로/효과/임무 색인의 미완료 작업에만 사용한다. 영구 분석 제한·읽기 실패·게임 데이터 대기를 확인 중으로 합치지 않으며 준비 완료 후 자동 행 0개이면 Summary의 AutoText를 비운다. 상세 Entries는 보존한다.

정의 준비는 기존 한도 안에서 나누고 캐시한다. 주제별 누락 보호는 최대 32개 묶음에 각 4개 필요조건 prefix를 보관하고 넘친 대안은 공통 필요조건으로 압축한다. 누락을 배제하지 못하면 미확정으로 남긴다. 분류·필수 조건은 예정된 조회의 새로운 gate 32개와 같은 Reader의 16,384 작업 예산을 공유한다. 프로필 플래그 쓰기도 32개 상한이며 조건·쓰기·상수 메타데이터를 고정 복사한 키로 조회 중 결과만 재사용한다. 정상 0.25초/실패 5초·숨김/OFF 조회 중단·정상 주기 Lua 재분석 없음, 기존 입력·예약·팝업·주사위 계약은 유지한다. 이번 자동 검사와 실제 Windows·물리 성능은 [alpha.11 후보 기록](ISSUE-6-ALPHA11-RELEASE.md)에서 구분한다.

다음 alpha.10 이하의 게시 상태는 이전 후보의 기록이다.

<!-- alpha10-current-status -->
2026-10-21 07:05:30 KST에 소스·주석 태그 [`05c4880`](https://github.com/jungpaeng/dicemaster/commit/05c4880ff11e0e8a79ea545495b04c6c28772bed)의 [alpha.10](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.10)을 게시했다. 랜덤 AND의 확정 false와 현재 필수 선택 조건의 미충족을 보존하여 불가능한 대화의 자동 지도를 정리했다. 전체·고정 커밋 재빌드 21개 프로그램·252개 입력·103개 소스/리소스·15개 참조·성공 기록·DLL/cfg/ZIP 동일성과 익명 공개 파일 대조를 통과했다. 기존 51개 Release·52개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·성능 확인과 #6 전체 완료는 대기다.** [설치·파일·확인표](ISSUE-6-ALPHA10-RELEASE.md).
<!-- /alpha10-current-status -->

## alpha.10 — 현재 필수 조건의 false 보존

`PreviewConditions`는 체크포인트 내부의 AND/OR를 3상태로 집계하고, RNG·미지원·예외는 해당 비교만 Unknown으로 둔다. Switch는 무작위 추첨 없이 가능한 우선순위 인덱스 집합을 유지한다. `SnapshotGate`는 읽기 시점의 선행 플래그 쓰기와 미지원 호출을 마스킹한 현재 필수 조건이다. 확정 false만 자동 표시 제외에 사용하고 전체 경로의 Met 보호를 해제하지 않는다. 정확히 연속된 루트 화면 준비 5문장과 짝지은 메뉴 닫기만 현재값 투영으로 구분한다. 임의 콜백·재바인딩·다른 화면 문법은 보수적으로 남긴다. 첫 루트 메뉴 조건은 단일 직선 정의일 때만 공통 필요조건으로 사용한다. 구조 생략은 최대 32개 prefix와 예산으로 보호하며 생략 대안을 배제하지 못하면 Unknown을 유지한다. 기존 논리 환경 셀·분기·깊이 상한은 그대로이고 셀마다 AST 참조가 하나 늘어난다. 정상 주기 Lua 재분석은 없다. 이미 열린 선택 화면에서는 첫 메뉴 전 선행 플래그 쓰기가 하나라도 있으면 기존 guard를 현재값으로 재판정해 미충족으로 내리지 않는다. 경로 예산은 논리 조회 단위이고 경로당 전체 상태와 미충족 조건의 두 투영을 고정 상한으로 읽는다. 32개를 넘는 관찰 경로도 정의 인덱스를 만들 때 모든 경로의 공통 AND prefix를 교집합으로 압축하므로, 이미 확인한 우회 대안을 잃지 않고 미충족 근거를 보존한다. fresh 후속 등록 바로 뒤의 정확한 terminal `luamanager.Init()`은 모든 남은 scope가 비었을 때만 이미 추적한 대상의 중복 누락 표식을 생략하고 `Finish`로 넘긴다. Init의 full/snapshot opaque와 자식 스크립트·풀의 별도 누락 보호는 유지하며 순수 호출로 취급하지 않는다.


2026-10-21 06:25:46 KST에 소스·태그 `1a782f206f1aa7ab9f9e490125fbe916aabbad66`의 [alpha.9](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.9)를 게시했다. 지도 오버레이에서 진행 불가 항목을 제외하고 로그 상세의 조건·사유는 보존한다. 전체·고정 커밋 재빌드 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조와 DLL/cfg/ZIP 동일성·익명 공개 다운로드 대조를 통과했다. 기존 50개 Release·51개 태그·자산과 Latest 정식 v1.0.6을 보존했다. 새 DLL의 실제 Windows·물리 패드·가독성·성능 확인과 #6 전체 완료는 대기다. [필터·파일·게시·Windows 확인](ISSUE-6-ALPHA9-RELEASE.md).

[사용자 안내](../README.md) · [변경 이력](../CHANGELOG.md) · [Release 등록](RELEASING.md) · [전체 소스 빌드](SOURCE-BUILD.md) · [이슈 #1 검증 기록](ISSUE-1-VALIDATION.md) · [게임 API 분석 개요](GAME-API-AUDIT.md) · [실제 게임 API 분석](REAL-GAME-API-FINDINGS.md) · [수명주기 분석](GAME-LIFECYCLE-ANALYSIS.md)

## alpha.9 — 지도 불가 항목 제외

`MapGuideSource.Snapshot`의 상세 `Entries`를 먼저 보존하고, 자동 notices 누적에서 최종 `Rank == 1`을 제외한다. guide/once OR 전체에 적용하여 1회성 전용 모드도 일관되게 제외한다. 기존 판정·실패/준비·조건·입력은 바꾸지 않는다. 필터 후 목록으로 상위 세 행·나머지 수를 계산하고 정상 빈 문구는 현재 표시 대상이 없다는 의미로 바꾼다. 전체·고정 재빌드 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조·실제 DLL 연결·공개 ZIP을 대조했다. [검사·파일·새 Windows 확인](ISSUE-6-ALPHA9-RELEASE.md).

## alpha.8 — 이야기 내부 조건과 진행 상태

첫 선택 조건과 이야기 내부 조건을 별도로 보관하고 읽는다. `ReadStoryConditions`는 리터럴 플래그 쓰기의 영향과 조건 그래프의 의존 입력을 bounded 조회로 대조하고 관련 쓰기·불명 효과·누락을 Unknown으로 둔다. 불투명 효과 뒤의 무조건 대화와 미선택 풀의 후속도 양성으로 올리지 않는다. 이미 열린 선택 화면의 선행 변경은 앞선 조건을 현재 값으로 재해석하지 않는다. 엄격한 기존 이벤트/실행 API는 유지한다. 안전한 전체 조건 양성은 진행 가능, 완전한 음성은 지금 진행 불가, 나머지는 진행 가능성 있음이다. 기존 정확한 양성 상태 문구와 진행 가능은 초록이며 실패 이전 값은 중립이다. 읽기 전용·입력·캐시·0.25초/5초·숨김 조회 중단 계약을 유지한다. 전체·고정 재빌드 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조와 실제 DLL 연결·공개 ZIP을 대조했다. [검사·파일·Windows 확인](ISSUE-6-ALPHA8-RELEASE.md).

## alpha.7 — 진입 조건 충족 초록 표시

지도 `진입 조건 충족 (현재)`와 선택 화면 `현재 이야기 진입 조건 충족`의 상태 문구만 기존 연녹색(`#BFFFCC`)으로 표시한다. 이름·미충족·불명·연결 후보는 중립색이고 실패 이전 값에는 초록을 붙이지 않는다. 첫 조건의 현재값과 이후 발생 보장은 별개다. 원문 꺾쇠를 literal 처리하고 색 문자열은 게시 내용 변경 때만 준비하며 rich text 모드를 배치 캐시 키에 포함한다. 입력·조회·Lua 해석·설정 계약은 유지한다. [파일·자동 검사·Windows 확인](ISSUE-6-ALPHA7-RELEASE.md).

## v1.0.7-alpha.6 — 인물 대화·1회성 안내

사용자가 기본 반복 활동인 남제자방 휴식의 진입 조건 안내를 제외하고, 안전하게 판별된 1회성 또는 인물 대화만 안내하도록 요청했다. 원래 작업 폴더의 `main`에서 작업하며 공개 alpha.5 파일과 당시 검사 기록은 보존한다. 새 후보 전체 소스의 21개 자동 검사·실제 DLL 연결·ZIP 검증을 통과했고 [후보 기록](ISSUE-6-ALPHA6-RELEASE.md)에 식별값을 남긴다. 고정 커밋 재빌드·게시·공개 파일 대조도 통과했으며 Windows·물리 패드·화면/성능 확인은 [새 확인표](ISSUE-6-WINDOWS-VALIDATION.md)에 별도로 남긴다.

원인은 경로의 실제 `say` 호출이면 안내·독백도 `MapStoryEntry`로 수집하던 분류다. 남제자방 휴식 표본에는 이 형식의 안내·독백만 있고, 사사형 경로에는 인물 발화가 연결된다. `talk` 원문 종류나 `D_` 접두사는 플레이어만 등장하는 표본에도 있어 인물 대화의 충분한 근거가 아니다. 새 분류는 같은 지원 경로에서 원본의 명시적인 플레이어 이외 화자와 실제 발화를 연결하고, 동적/미확정 화자·플레이어 독백·기본 설명을 양성으로 만들지 않는다. 기존 원본 활성 조건에 연결된 엄격한 1회성 증명은 별도로 유지한다.

기본 활동을 탐색 단계에서 제거하지 않는다. 선택·등록·후속 스크립트의 기존 정의 캐시와 한도를 유지하면서 휴식 뒤의 특수 인물 대화·증명된 1회성도 남긴다. 앞선 나레이션 때문에 나중 인물 발화를 놓치거나 다른 조건 경로의 화자를 가져와 분류하지 않아야 한다. 지도·현재 선택의 기본 안내는 이 근거가 있는 부분집합이며 미분류·미발견을 반복 가능·전체 기회 없음으로 바꾸지 않는다. 현재 조건과 진입 후 상태 변경·미완성 경로의 불확실성도 유지한다.

회귀 검사는 기본 휴식/독백 제외, 사사형 유지, 휴식 후 특수 대화, 앞선 안내 뒤 인물 발화, `talk`/`D_` 단독 추론 음성, 동적 화자와 조건 경로 분리를 포함한다. 기존 임무·엄격 1회성·메뉴/문맥 수명·입력·정상 0.25초/실패 5초·숨김/OFF 조회 0·정상 주기 Lua 재분석 없음·읽기 전용 계약도 확인한다. 이 문단은 검사 계획이며 실제 통과 수·해시나 Windows 성능 측정을 대신하지 않는다.

## alpha.5 공개 기록 — 이야기 연결과 구조화된 선택 경로

사용자가 지도 첫 진입 조건, 현재 선택 항목 뒤의 이야기와 필요한 선택 순서를 안내하는 후속 구현을 승인했다. 원래 작업 폴더의 `main`에서만 작업하며 별도 branch/worktree를 만들지 않는다. [alpha.5 후보 기록](ISSUE-6-ALPHA5-RELEASE.md)에 실제 지원 범위·최종 검사·소스/파일 식별값과 새 Windows 확인을 분리한다. 아래 alpha.4는 이미 공개한 이전 버전의 기록이다.

경로 정의의 발견과 현재 도달 판정은 별개다. 기존 선택 번호·라벨·분기 근거를 구조화해 순서를 보존하고, 단순 후속 스크립트 및 스크립트 안의 이야기 항목을 기존 Position 풀 결과와 구분한다. 원문 경로 문자열을 다시 쪼개거나 파일명·미독·활성 UI만으로 의미를 추정하지 않는다. 최종 후속 등록의 교체/취소, 선택지 비활성, 같은 라벨의 다른 번호, 조건 분기, 순환과 누락 경로를 보존한다.

`MapChoiceStep`은 스크립트·choose offset·원래 번호·원문 라벨을 분리하고 `MapStoryEntry`는 선택 뒤 확인된 리터럴 대사 구간을 보존한다. 단순 후속 등록만으로 이야기 행을 만들지 않는다. 지도는 첫 선택 묶음을 16개·게이트 32개 이내에서 현재 조회하고 `진입 조건 충족 (현재)`·`진입 조건 미충족 (현재)`·`선택 화면에서 확인`으로 표시한다. 미완성/실패/생략 경로의 음성은 불명으로 남긴다. 실제 선택 화면의 동일 라벨/다른 원본 위치는 임의 연결하지 않는다.

현재 패드 선택의 관찰 경계를 공유하고 자동 표시는 캐시된 행의 문자열을 읽는다. 선택한 항목은 캐시된 연결 경로 하나를 표시하고 전체 경로는 기존 지도 안내 도구로 발행한다. 원문이 실제 여러 원본 노드에 겹치는 경우와 연결 자료가 아직 불완전한 경우를 구분한다. 전자는 특정 자동 경로를 붙이지 않고, 후자는 발견된 `경로 후보`만 보여주며 개수·진행 확정은 숨긴다. 완전한 연결에서만 추가 수를 알린다. 지도·선택 패널은 중립색을 사용하며 `EnableMapGuide`가 함께 제어한다. 새 설정 키는 없다. 대화·wait/runwait·시각 API·플래그 변경 등 기존 불확실 경계를 무해하다고 허용하지 않는다. 현재값을 미래 선택 시점의 보장으로 캐시하지 않으며, 새 경로와 이야기 행도 기존 준비·현재 조회의 작업 예산을 공유한다. 큰 결과 분석은 자동 화면에 추가하지 않고 상세 진입에서만 준비한다. 정상 0.25초/실패 5초, 숨김/OFF 조회 0, 정상 주기 Lua 재분석 없음과 RNG·조건 횟수·세이브·아이템·능력치 불변 계약을 유지한다.

## alpha.4 공개 기록 — 짧은 상점 효과·지도 비용·1회성 표시

2026-10-21 03:53:15 KST에 [v1.0.7-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.4)를 게시했다. 소스·태그 `8e052e1`의 전체·고정 재빌드에서 20개 프로그램과 입력/파일 동일성, 익명 공개 ZIP을 확인했다. 기존 정식/시험판과 이슈 상태는 보존하며 실제 Windows·화면/입력/속도 확인과 #6 완료는 대기다.

사용자 피드백에 따라 상점 AutoText를 조활 0.3.33 기준의 짧은 수치/성장 방향으로 바꾸고 학습 전문을 Detail에만 둔다. 지도는 Discover의 details 모드와 Runtime 문맥을 구분하여 명시적 상세에서만 전체 조건/결과를 준비하며, 자동 경로는 공통 조건 조회·정의 증명을 재사용한다. 자동 배지는 Proven만, Unknown/실패는 캐시된 상세 delegate로 게시한다. [현재 수정·검사·Windows 확인](ISSUE-6-ALPHA4-RELEASE.md)을 따른다. 아래 alpha.3과 상점의 긴 설명은 당시 기록이다.

## 지도 안내 개선 — v1.0.7-alpha.3 prerelease

[2026-10-21 03:26:15 KST 공개 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.3). 소스 `9a9b6176e038595d55d72ef152a7df0910fb3a4e`의 고정 재빌드에서 20개 검사와 검토 후보의 입력·결과·패키지 바이트 일치를 확인했다. 새 Windows 확인과 #6 완료는 대기다.

main `5e951cf`에서 작성한 [분석 설계](ISSUE-6-MAP-GUIDANCE-ANALYSIS.md)를 사용자의 후속 구현·커밋·prerelease 지시에 따라 구현한다. 기존 대기/현재 조건/dispatch 구분·장소 무관 임무·Symbolic Position 반환 경로·단일 패널/개별 로그 행과 준비/캐시 경계를 연결한다. [현재 구현·검사·배포·Windows 확인](ISSUE-6-MAP-GUIDANCE-RELEASE.md)에 확정 결과를 기록하며 아래 상점 후보·공개 alpha.2/alpha.1은 각각 당시 이력이다.

## alpha.3 게시 전 상점 설명 후보 기록 — 효과를 직접 표시

2026-10-21 사용자가 상점 설명에서 사용·학습 효과를 바로 보도록 요청했다. 상점의 자동 표시를 이름·종류·분석 상태로 제한하던 기존 #6 규칙은 이 요청에 따라 바꾸며 지도·1회성·분기의 규칙은 유지한다. 원래 `_dialogDesc` 아래에 사용 효과 또는 다음 학습 효과를 여러 줄로 추가하고, 전체 상세는 기존 로그에도 남긴다. 효과를 얻었는데도 부분 분석 진단만 제품 설명에 노출되던 문제가 대상이다.

`ShopEffectSource`는 현재 선택의 캐시된 효과 문구를 게시한다. `ShopUseEffects`는 종류·ID로 연결한 사용 메뉴의 기존 설명기를 재사용해 수치·소모와 조건부·난수·부분 해석 표기를 유지한다. `사용 효과:` 또는 `학습 효과:` 아래에 효과를 먼저 표시하고 책은 현재·다음 단계와 학습 진행 → 소모 → 조건을 이어서 읽는다. 최대 단계·확인된 변화 없음·미지원·연결 실패를 구분한다. 기존 설명의 줄바꿈과 내용을 보존하며 중복 추가를 막고 OFF·닫기·선택 변경에서 자기 문구만 복원한다. 추가 효과 문구는 설명기의 서식 태그를 제거하고 알 수 없는 꺾쇠를 이스케이프해 텍스트로 붙인다.

기존 `ShopPanel.OnItemHover` 관찰 경계를 공유해 패드 선택에도 연결하고 새 입력·입력 소비·포커스 이동을 추가하지 않는다. 구매·사용·학습·게임 조건 실행·세이브·RNG 호출을 추가하지 않는다. 정상 0.25초/실패 5초·숨김/OFF 조회 중단·문맥별 기존 Lua 연결 캐시·정상 갱신 재분석 없음 계약을 유지한다. 상위 `EnableOptionInfo`의 시작 시 적용과 `EnableShopEffectInfo`의 기본 ON·즉시 변경/저장·OFF 복원도 유지한다.

원래 작업 폴더의 main에서 구현·검증하며 별도 브랜치/worktree를 만들지 않는다. 이 절은 alpha.3 게시 전 로컬 후보 당시 기록이며 당시 공개 alpha.2·Latest 정식 v1.0.6을 보존했다. [구현·자동 검사·후보 식별값·실제 Windows 확인표](ISSUE-6-SHOP-DESCRIPTION.md)를 따른다. 아래 alpha.2·alpha.1은 해당 게시 버전의 이력이다.

## v1.0.7-alpha.2 — 선택 대상과 그리기 비용

[2026-10-21 01:35:02 KST 공개 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.2). 소스 `4bbdffb`의 고정 재빌드·18개 검사와 검토 후보의 입력/검사/패키지 일치·익명 다운로드 검증을 완료했습니다. 새 Windows·성능 확인과 #6 완료는 대기이며 main·기존 Release/Latest를 보존했습니다. [게시·검증 근거](ISSUE-6-SELECTED-MAP-SHOP.md#공개-prerelease-게시와-검증).

선택 장소 하나만 DisplayQuery로 만들고 선택 세대를 키에 포함합니다. 약한 참조의 실제 게임 선택은 OFF 동안 유지하되 자료는 비웁니다. 선택 변경/해제는 전 주기 게시와 오래된 콜백을 즉시 무효화합니다. 화면은 선택 장소 이름의 좌측 상단 패널 하나를 사용하고 높이·사각형·GUIContent를 캐시합니다. 다른 장소 UI의 카메라 투영/장애물 수집과 전역 겹침 최소화 탐색은 제거했습니다.

ShopUseEffects는 실제 재고 Misc/SpecialItem을 food_menu_001의 명시 제거 호출과 연결합니다. 기존 정의 파서·설명기와 문맥당 연결 색인을 사용하며 게임 사용/학습·조건 실행을 호출하지 않습니다. 자동 상태와 상세 효과를 분리하고 읽기 실패는 기존 5초 재시도를 따릅니다. [결정·지원 경계·전체 자동 결과·Windows 확인](ISSUE-6-SELECTED-MAP-SHOP.md). 아래 alpha.1은 해당 공개 버전의 기록입니다.

## PRD #6 — alpha.1의 구현·게시 기록

[v1.0.7-alpha.1 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.1)는 [PRD #6](https://github.com/jungpaeng/dicemaster/issues/6)의 구현·자동 검사·고정 소스 재빌드 검증 후보다. 사용자가 2026-10-21 같은 채팅에서 “v1.0.6 Windows 확인을 완료했고 남은 문제가 없습니다.”라고 답해 #5 착수 조건을 충족했다. 확인 버전·파일·기준 커밋과 미제공 환경은 [#5 후속 기록](ISSUE-5-STABLE-RELEASE.md#2026-10-21-사용자-windows-후속-확인)에, 새 구현·검사·한계는 [#6 구현 기록](ISSUE-6-IMPLEMENTATION.md)에 구분한다. 새 후보는 2026-10-21 00:46:36 KST에 공개했고 익명 ZIP·기존 Release/태그/Latest 보존을 검증했다. 검증 소스 태그 `949ebcc`와 `codex/prd6-information` 브랜치를 원격에 반영했으며 main은 변경하지 않았다. [게시 기록](ISSUE-6-CANDIDATE.md#공개-prerelease-게시와-검증)을 따른다. 새 Windows 확인은 미실행이고 #6은 open이다.

`InformationRuntime`은 기존 `RefreshEngine`을 기능별로 소유하고 `InformationRow`만 게시한다. 지도·상점의 `DisplayQuery`는 현재 소속·ID·문맥을 검증하고 숨은 전체 맵이나 게임 실행 경로를 탐색하지 않는다. 정상 0.25초·실패 5초와 숨김/OFF 표시 조회 0, 같은 문맥의 실패에만 이전 값 유지, 새 대상·로드·장면 뒤 스냅샷 폐기를 유지한다. Lua 정의·결과 분석은 기존 캐시로 재사용하며 GUI는 게시 문자열만 읽는다.

| 경계 | 구현 계약 |
| --- | --- |
| 지도 | 살아 있는 현재 `PositionController`의 수명·선택을 관찰한다. 연결된 리터럴 Position 후보·고정 이야기·현재 위치의 임무를 읽고 가능/불가/불명을 구분한다. 원래 순서를 유지한 가능 우선 정렬과 후보 수를 게시한다. |
| 1회성 | 현재 명시 활성 조건과 완전한 정적 결과에서 확정 상수 플래그 변경 후 조건이 거짓이 되는 경우만 증명한다. 읽음 기록으로 추정하지 않고 후속·선택·동적·미지원 경로는 불명으로 남긴다. NormalOptionButton에는 원래 활성 조건 참조가 없어 기본 선택지의 판정은 확인 불가이며 interactable·미독만으로 추론하지 않는다. 양성 증명은 지도·실제 이벤트 팝업의 명시 조건 경로다. |
| 분기 | 원래 Position/Switch 결과 반환을 관찰한다. 실행 토큰으로 중복 관찰만 제거하고 별도 실제 호출은 별도 기록한다. 최신 20건·5초 알림은 메모리에서 관리하며 저장·로드·장면·OFF에서 지운다. 반환값·예외·#5 선택 권한을 변경하지 않는다. |
| 상점 | `ShopPanel.OnItemHover(IGameItem)`는 게임의 `ShopItemPanel.OnSelect`와 포인터 콜백이 공유하는 관찰 경계다. 기존 `_dialogDesc`에 캐시 이름·종류·상태만 붙이고 교체·닫기·OFF에서 자기 표시만 복원한다. |

표시용 `FreePositionData.GetActiveConditionResult/GetDialog`, `ConditionResultData.Execute`, `GetRate/GetResult/Check/Execute` 게임 체인을 호출하지 않는다. 책의 `Learn`·소모·Apply, 구매·아이템 사용·게임 이벤트 발생·세이브 API도 호출하지 않는다. 상점의 필드 해석은 실제 `MartialLearnCondition`의 묶음 간 AND·묶음 안 OR(빈 OR는 false), 소모 AND/첫 양수 OR, 중앙 50·비급 등급/25 경계·상한/하한을 현재 상태에만 적용한다. 로컬 사본에서 순서대로 계산하며 원본 필드와 RNG는 건드리지 않는다. 소모품은 정적 효과 배열을 읽지만 제공 참조에서 실제 사용 실행 경로가 확인되지 않으면 실제 적용 변화는 확인 불가다. 변환·연동 능력치·미지원 효과도 확정 최종값으로 안내하지 않는다.

상위 `EnableOptionInfo`는 시작 시 고정하고 네 bool(`EnableMapGuide`, `EnableOneShotHints`, `EnableBranchNotices`, `EnableShopEffectInfo`)은 기본 true로 기존 cfg에 추가한다. 개별 변경 즉시 적용·저장과 실패 시 해당 기능 비활성화, OFF 정리·ON 과거 재생 없음 계약을 유지한다. 별도 이력 파일·세이브 필드·서버·원격 분석은 추가하지 않는다.

기존 네 탭·예약·주사위·패드 소유권을 그대로 사용하면서 로그 도구에 지도 안내·분기 기록·상점 효과 조회를 추가한다. 새 정보 화면은 행 선택·상세 스크롤만 허용하고 실행 ticket을 만들지 않는다. 입력 한 번을 뒤의 게임·팝업·구매에 재사용하지 않는 기존 해제/중립 보호를 회귀 확인한다.

자동 검사 등록·빌드·패키지 식별값은 [후보 안내](ISSUE-6-CANDIDATE.md)에 확정 결과만 기록한다. `ShopInformationTests`는 실제 상점 provider/patch/DTO/RefreshEngine 소스를 링크하고 제공 게임 DLL의 필드·콜백 서명을 Cecil로 확인한다. 명시적 Unity/Harmony/게임 경계 대역을 실제 Windows 실행으로 취급하지 않는다. 합성 시간·할당량 수치와 실제 Unity 프레임 비용을 구분하며 [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)에서 게임·로더·OS·패드·Steam Input·배율·실제 비용/할당량을 별도로 기록한다.

## 입력 경계 구조 정리

alpha.8 이후 공통 포인터 처리와 입력 취소 정책을 단계별로 정리해 v1.0.6-alpha.9로 게시했다. 구현 커밋 `da1f362`·`360ca57`과 준비 문서를 포함하는 고정 소스·태그는 `af7bcd94ee1398717acec3066a0a907d588c49f9`다. 아래 검사·측정은 해당 소스 입력을 대상으로 한다. [alpha.9 배포 기록](ISSUE-5-ALPHA9-RELEASE.md)에 공개 파일·검증 상태를 기록하며 단일 net48 DLL·기존 IMGUI·설정·조작·Lua 판정과 한 번 소비 검증을 유지한다.

첫 단계는 `LogUI/ImGuiPointerButton` 추출이다. 각 Renderer가 기존 스타일·레이아웃 옵션으로 Rect를 확보하고, 어댑터가 실제 이벤트의 누름/해제 분류와 직접 터치의 네이티브 마우스 변환·`GUI.Button` 한 번 호출을 담당한다. `Used`는 보존하고 소비되지 않은 터치는 원래 타입으로 복구한다. `rawType`만 남은 Layout/Repaint/Used를 새 입력으로 해석하지 않는다.

로그창의 예약 ticket·문맥 검증과 팝업의 토큰·대상·결과·일반/조건 무시 의도 검증, 입력 해제/중립 보호와 GUI 종료 후 지연 실행은 각 화면에 남긴다. 반환값은 readonly struct이며 이 추출 때문에 배열·콜백·참조형 요청 객체를 매 버튼마다 추가하지 않는다. Lua 실행 권한을 공통 버튼에 옮기지 않는다.

첫 단계만 분리한 소스 스냅샷에서 전체 14개 검사 프로그램이 통과했다. 빌드 입력 178개·소스/리소스 72개, 런타임 84개 시나리오·581개 단언, 팝업 1,576개·로그 UI 3,879개·실제 후보 연결 89개 검사를 확인했다. 마우스·마우스 에뮬레이션 터치·직접 터치, 소비된 이벤트·clipping/capture·예외 시 원복을 검증하는 어댑터 계약 38개를 포함한다. 명시적인 Unity/게임/native 대역과 실제 참조 API의 관리 검사이며 Windows 물리 장치 실행은 별도다.

첫 단계 DLL은 619,520바이트·SHA-256 `7fa2095b2ec965d8bc7a1209b0b3c7f55d52beafbc9be6e1fd36951e55807e1e`다. alpha.8과 파일 크기는 같고 IL은 56바이트 감소, 메서드 헤더/예외 정보는 5바이트 감소, 메타데이터는 224바이트 증가했다. 나머지 파일 구조·정렬 등의 차이 -163바이트를 합하면 전체 차이는 0이다. 내장 리소스 바이트·해시는 동일하다. 구조 정리의 목적은 입력 수정 지점과 취소 경계를 명확히 하는 것이며 파일 크기·메모리·입력 지연 개선을 같은 결과로 취급하지 않는다. 검사·측정 영수증은 로컬 `artifacts/architecture-refactor/stage1/`에 보관한다.

둘째 단계는 `LogUI/ModalInputResetPolicy`와 `DicePlugin.ResetEventInput`이다. 실제 적용을 한곳으로 모으고, 아래 사유가 어떤 포인터 상태와 대기 요청을 보존하는지 순수 정책으로 표현한다. `LogPadController`와 두 Renderer의 기존 리셋 API·탐색 상태 소유권은 유지한다.

| 초기화 사유 | 유효한 누름 보존 | 접수된 확정 취소 |
| --- | --- | --- |
| 자동 입력 모드 관측·실제 포인터 전환 | 예 | 아니요 |
| 물리 패드 재획득·컨트롤러 경계·로그 표시 전환·앱 초점 전환 | 아니요 | 예 |
| 팝업 대기 상태 변경 | 아니요 | 아니요 |

패드 프레임의 `ResetFocus`가 없으면 적용하지 않고, 기존 `PreservePointerTransaction`·`PreserveNavigationArea`를 초기화 사유로 번역한다. 단순 모드 관측을 새 소유자로 취급하지 않으며 Runtime의 문맥 교체·비활성화·종료 `Invalidate` 경계는 유지한다. 확정 후보·결과 스냅샷은 alpha.8에 이미 있으므로 별도 요청 타입·전체 상태 머신으로 바꾸지 않았다.

두 단계가 적용된 전체 소스·격리 소스 재빌드에서 각각 14개 검사 프로그램이 통과했다. 입력 179개·소스/리소스 73개, 런타임 150개 시나리오·999개 단언, 팝업 1,576개·로그 UI 3,879개·실제 후보 연결 90개 검사를 확인했다. 격리 빌드는 기존 모드 DLL 0개에서 시작했고 두 빌드의 DLL/cfg/ZIP·검사 결과가 일치했다. 설치 ZIP의 CRC·정확히 두 파일·내부 바이트도 대조했다.

실행 통합 검사는 실제 후보 DLL의 모드 관측·패드 프레임 적용·초기화 메서드와 정책 enum/효과 타입/판정 IL을 fixture에 복사해 Lua 재개까지 실행한다. 정책을 대역 구현으로 바꿔 검사하지 않는다. 실제 후보 정책을 과잉 취소로 변이시키면 늦은 모드 관측 뒤 첫 Lua 완료 단언이 실패하고, 실제 적용 메서드에서 취소 호출을 제거하면 물리 패드 전환의 실행 차단 단언이 실패하는 음성 대조도 확인했다. 정상 후보 재실행은 통과했다.

| 전체 두 단계 적용 DLL | 결과 |
| --- | --- |
| 크기 | 620,032바이트 · alpha.8 대비 +512바이트 |
| SHA-256 | `7da12ec4b77d524e82f2bf8c62d4de6cf8e0fc74b83fb3cc6393619f8efc1d64` |
| alpha.8 대비 IL / 메서드 헤더·예외 / 메타데이터 | +50 / +22 / +800바이트 |
| 내장 리소스 / 기타 파일 구조·정렬 등 | 0 / -360바이트 |

최종 DLL에 테스트·Mono.Cecil 타입이나 참조가 없고 alpha.8과 내장 리소스 및 외부 assembly 참조가 동일하다. 정책을 표현하는 타입·메서드의 메타데이터 증가가 있으며 이번 리팩터링을 용량 절감으로 설명하지 않는다. 메모리·프레임 시간·입력 지연은 측정하지 않았다. 실제 Windows·물리 터치 확인도 별도이며 공개 alpha.8의 확인 상태를 새 개발 DLL에 승계하지 않는다. 영수증은 로컬 `artifacts/architecture-refactor/final-candidate/`·`final-clean/`·`policy-evidence/`에 보관한다.

## PRD #5 — v1.0.6-alpha.9 prerelease

사용자는 입력 경계를 정리한 두 로컬 커밋 `da1f362`·`360ca57`을 `origin/main`에 반영하고 README 갱신·새 prerelease를 진행하도록 지시했다. 공통 포인터 어댑터와 취소 정책의 두 단계만 포함하며 별도 요청 객체·전체 상태 머신·UI 프레임워크 전환은 수행하지 않았다. 조작·설정·지원 경로를 유지한다.

전체 후보·격리 재빌드에서 각각 14개 프로그램, 179개 빌드 입력·73개 소스/리소스, 실제 후보 IL과 MoonSharp를 연결한 런타임 150개 시나리오·999개 단언을 확인했다. DLL은 620,032바이트로 alpha.8 대비 +512바이트이며 내장 리소스·외부 assembly 참조는 동일하다. 메모리·프레임 시간·입력 지연은 미측정이다. [alpha.9 변경·설치·검증 기록](ISSUE-5-ALPHA9-RELEASE.md)에 파일 식별값과 실제 검사 결과를 기록한다.

**2026-10-20 03:43:52 KST에 [alpha.9 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.9)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.9/DiceMaster-v1.0.6-alpha.9.zip). 두 구현 커밋과 README를 포함한 소스·태그 `af7bcd94ee1398717acec3066a0a907d588c49f9`를 `origin/main`에 반영했다. 03:42:52 KST 고정 커밋 재빌드도 기존 모드 DLL 0개에서 14개 프로그램을 통과했고 후보·격리 빌드와 파일·전체 결과가 일치했다. 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256과 기존 32개 Release·태그·자산, Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open 보존을 확인했다. 새 DLL의 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기하며 alpha.8 사용자 성공 보고는 제공되지 않았다. 게시 결과 보충은 빌드 소스·태그·공개 자산을 바꾸지 않는 별도 main 문서 커밋이며 고정 소스의 준비 문구와 아래 alpha.8은 당시 기록이다.

## PRD #5 — v1.0.6-alpha.8 prerelease

사용자는 구조 리팩터링을 보류하고 터치 반복 실행 문제 수정·하단 실행 버튼의 텍스트 안내 전환·회귀 검사·커밋·`origin/main` 반영·README 갱신·새 prerelease를 지시했다. 시작 main은 `572ba1df65a18e94af20be21d566430dd84c13ba`다. 자동 입력 모드 관측만으로 같은 유효 행동에 접수된 확정을 취소하지 않으며 접수한 후보·결과를 보관해 이후 후보 갱신이 확정 대상을 바꾸지 않게 한다. 초점·문맥·대상·연결·소유자 등의 하드 경계와 실행 직전 검증은 유지한다.

하단 실행 버튼은 제거하고 A/Enter의 일반/명시 조건 무시 안내·B/Esc 무작위 안내를 텍스트로 표시한다. 클릭·터치 실행은 목록의 첫 선택 뒤 같은 행 재확인으로 모으고 무작위는 B/Esc로만 요청한다. 새 설정·키·패드 조합·구조 프레임워크는 추가하지 않는다. 터치만 사용하는 환경의 무작위 실행 UI는 없다.

**2026-10-20 03:18:58 KST에 [v1.0.6-alpha.8](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.8) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.8/DiceMaster-v1.0.6-alpha.8.zip). 최신 `origin/main`의 `572ba1df65a18e94af20be21d566430dd84c13ba`에서 시작했고 README·구현·검사를 포함한 소스·태그 `7d79363bc33dffac80d453fe6a945a6e3edeb513`를 main에 반영했다.

전체·격리·고정 소스 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램과 동일 DLL/cfg/ZIP·전체 검사 결과를 확인했다. 고정 재빌드는 03:18:16 KST에 기존 모드 DLL 0개에서 완료했고 빌드 입력 176개·소스/리소스 71개를 대조했다. 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256도 확인했다. 런타임은 실제 후보 observer IL·소스 팝업/런타임·MoonSharp를 연결해 84개 시나리오·581개 단언을 통과했다. [alpha.8 배포 기록](ISSUE-5-ALPHA8-RELEASE.md)에 상세 결과를 기록한다.

기존 31개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다. 게시 결과 보충은 빌드 소스·태그·공개 자산을 바꾸지 않는 별도 main 문서 커밋이며 고정 소스의 준비 문구와 아래 alpha.7은 당시 이력이다.

## PRD #5 — v1.0.6-alpha.7 prerelease

**2026-10-20 02:43:23 KST에 [v1.0.6-alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.7) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.7/DiceMaster-v1.0.6-alpha.7.zip). 최신 `origin/main`의 `661622d8f221f25d58f871321caed935c952d6e9`에서 시작했고 README·구현·검사를 포함한 소스·태그 `5630a1a88a6f99cabad146c405810b1918ca83e1`를 main에 반영했다. 팝업 목록·기본 선택을 보완했다. 목록은 정상 발동 가능/불가 상태 행을 제거한 **굵은 제목·결과 요약 두 줄**이다. 제목은 기존 문자 이스케이프·회색 구분을 유지하고 굵은 폭으로 측정해 말줄임한다. 결과 자체의 효과색·선택 화살표·조건 설명·진행 버튼은 유지한다.

가상 무작위 목록 항목을 제거하고 초기에는 후보 미선택이다. A/Enter는 선택 전에는 진행하지 않고 ↑↓의 첫 탐색은 첫 후보부터 시작한다. B/Escape는 기존 무작위 판정이며 하단 `무작위로 진행 [B / Esc]` 버튼 자체로 안내한다. 터치만 사용하는 환경을 위해 이 버튼을 클릭할 수 있게 유지하며 별도 하단 안내에는 같은 문구를 반복하지 않는다. 첫 선택/재클릭·터치 진행, 실제 후보의 토큰·대상·결과·일반/조건 무시 검증과 읽기 실패 차단을 유지한다.

전체·격리·고정 소스 재빌드에서 각각 14개 검사 프로그램과 검토 DLL/cfg/ZIP의 동일 바이트·전체 검사 결과 일치를 확인했다. 고정 재빌드는 02:42:13 KST에 기존 모드 DLL 0개에서 완료했고 빌드 입력 174개·소스/리소스 71개를 대조했다. 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256도 확인했다. [alpha.7 배포 기록](ISSUE-5-ALPHA7-RELEASE.md)에 실제 검사·파일·게시 결과를 기록하며 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)의 새 DLL·장치 확인은 대기한다. 기존 30개 Release·태그·자산과 Latest v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 게시 결과를 보충하는 이번 문서 커밋은 빌드 소스·태그·공개 자산과 구분하며 고정 소스의 준비 문구와 아래 alpha.6은 당시 이력이다.

## PRD #5 — v1.0.6-alpha.6 prerelease

**2026-10-20 02:29:50 KST에 [v1.0.6-alpha.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.6) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.6/DiceMaster-v1.0.6-alpha.6.zip). 최신 `origin/main`의 `cb6c20fdef23d7b19c39171df114e855c3e4b363`에서 작업했고 README·구현·검사를 포함한 소스·태그 `b68046494656c03a7d0c9cb4a13b78258e73f52f`를 main에 반영했다. 전체·격리·고정 재빌드의 14개 검사와 검토 DLL/cfg/ZIP 동일 바이트·전체 결과 일치를 확인했고 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256 검증도 통과했다. [alpha.6 배포 기록](ISSUE-5-ALPHA6-RELEASE.md)에 상세 검사·파일·게시 결과를 기록한다.

`EventResultPreview.Formatting.cs`는 기존 정적 분석 결과에서 안전한 색 범위만 보존하고 표시 문자 기준 말줄임·닫는 태그·UTF-16 쌍을 처리한다. 크기·굵기 등 기존 서식 태그는 제거하고 그 밖의 글꼴·태그처럼 보이는 문자열도 서식으로 해석하지 않아 행 기하를 바꾸지 않는다. 이름·상태·발동 조건도 안전한 문자로 표시하며 기존 분석기가 효과색을 주지 않은 설명에 새 색을 부여하지 않는다.

`!CanSelect`인 후보의 이름·상태는 `#B3B3B3`, 선택 화살표는 노랑이며 결과 요약·상세의 효과색은 그대로 유지한다. 행 선택·명시 조건 무시·읽기 실패 차단과 기존 입력 경계·결과 캐시·독립 상세 스크롤은 유지한다. 새 후보의 자동 검사·파일 식별값과 Windows 확인은 이전 alpha.5에서 승계하지 않으며 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)를 사용한다. Latest v1.0.5와 기존 29개 Release·태그·자산, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.6/`에 보존한다. 이번 게시 결과를 보충하는 후속 문서 커밋은 main에 별도로 반영하며 빌드 소스·태그·자산을 바꾸지 않는다. 고정 소스의 준비 문구와 아래 alpha.5는 당시 기록이다.

## PRD #5 — v1.0.6-alpha.5 prerelease

**2026-10-20 02:09:55 KST에 [v1.0.6-alpha.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.5) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.5/DiceMaster-v1.0.6-alpha.5.zip). 최신 `origin/main`의 `3a5ffdf85f5965623c6c585032086976515dd98a`에서 작업했고 README·구현·검사를 포함한 소스·태그 `3f138728c109651533f25b5e7aee3f147a082d14`를 main에 반영했다. 전체·격리·고정 재빌드의 14개 검사와 검토 DLL/cfg/ZIP 동일 바이트·결과 일치를 확인했고 익명 공개 페이지·다운로드의 CRC·두 파일·내부 바이트·SHA-256 검증을 통과했다. [alpha.5 배포 기록](ISSUE-5-ALPHA5-RELEASE.md)에 상세 수치·파일·게시 결과를 기록한다.

도구가 열리면 목록 탭의 선택 표시를 끄고 도구만 표시한다. 물리 패드 재획득은 `Navigation` 초기화로 이전 확인·클릭 권한과 검색 입력 커서를 취소하고 해제·중립을 요구하면서 현재 도구 화면·선택·스크롤을 보존한다. 자동 scheme 변경과 실제 포인터 전환은 기존 `Pointer` 경로의 유효한 첫 누름을 보존한다. 실제 초점·문맥·차단 준비·소유자·연결·닫힘 경계는 계속 하드 초기화한다.

팝업은 실제 결과 식별자로 기존 정적 분석기를 사용한다. 목록은 폭에 맞춰 줄이는 네 줄과 선택 전후 같은 높이를 유지하고, 선택 상세의 예상 결과·발동 조건은 독립 스크롤 영역에서 휠·오른쪽 스틱으로 읽는다. ↑↓는 행 이동이다. 보이는 팝업의 0.25초 갱신마다 최대 두 후보의 결과를 준비하고 같은 실제 행동·대상·결과의 성공값을 캐시하며 읽기 실패는 5초 뒤 재시도한다. OnGUI는 게시된 텍스트만 읽고 Lua·결과 실행을 호출하지 않는다. 준비 중·미지원·읽기 실패·분석상 효과 없음을 구분하고 설명 실패는 `CanSelect`·`CanForce`에 영향을 주지 않는다. 새 DLL의 Windows 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기하며 alpha.4 후속 사용자 보고를 새 후보 통과로 승계하지 않는다. Latest v1.0.5와 기존 28개 Release·태그·자산, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.5/`에 보존한다. 이번 게시 결과를 보충하는 문서 커밋은 main에 별도로 반영하며 빌드 소스·태그·자산은 바꾸지 않는다. 고정 소스 문서의 준비 문구와 아래 alpha.4의 검사·파일·게시 기록은 당시 이력이다.

## PRD #5 — v1.0.6-alpha.4 prerelease

**2026-10-20 01:47:20 KST에 [v1.0.6-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.4) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.4/DiceMaster-v1.0.6-alpha.4.zip). 최신 `origin/main`의 `ea11afbda83433c58eeddc90cada3d021013bdda`에서 작업했고 README·검사·구현을 포함한 소스·태그 `bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce`를 main에 반영했다. 전체·격리·고정 커밋 재빌드의 14개 검사와 검토 DLL/cfg/ZIP 동일 바이트·전체 검사 결과 일치를 확인했다. 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256 검증도 통과했다. [alpha.4 배포 기록](ISSUE-5-ALPHA4-RELEASE.md)에 상세 수치·파일·게시 식별값을 기록한다.

로그는 이벤트 외 탭의 매 콜백 `GUI.FocusControl(null)`을 제거하고 탭·도구·키보드 경계의 해제 요청을 안전한 화면 갱신에서 한 번 처리한다. 진행 중인 포인터 누름·해제를 해제 요청 때문에 끊지 않으며 늦은 소프트 초기화도 도구·검색 입력 커서를 보존한다. 실제 Windows에서 이 경로가 탭별 터치 실패의 직접 원인이었는지는 새 장치 확인이 필요하다.

팝업은 실제 `LogPadController` 소스를 연결한 재현 순서에서 첫 터치 해제 후 늦은 포인터 전환이 `ResetInput(true)`을 호출해 다음 입력을 막는 경로를 수정한다. 소프트 초기화는 유효한 누름과 이미 해제된 중립 상태를 보존한다. 잘못된 누름·초점 상실·새 진입·토큰/대상/결과 교체 같은 안전 경계에서는 계속 취소하고 해제·중립을 기다린다.

Latest v1.0.5와 기존 27개 Release·태그·자산, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.4/`에 보존한다. 이번 게시 결과를 보충하는 후속 문서 커밋은 main에 반영하며 소스 태그·설치 자산을 바꾸지 않는다. 고정 소스의 준비 문구는 당시 단계의 기록이다. 새 DLL의 실제 Windows·장치 확인은 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다. 아래 alpha.3은 당시 기록이다.

## PRD #5 — v1.0.6-alpha.3 prerelease

**2026-10-20 01:23:02 KST에 [v1.0.6-alpha.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.3) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.3/DiceMaster-v1.0.6-alpha.3.zip). 최신 `origin/main`을 기준으로 구현·검사·README를 반영했으며 소스·태그는 `b2036a9c8a08d259c6cecee4ee360f24c4af3d4b`다. 전체·격리·고정 커밋 재빌드에서 각각 14개 검사 프로그램과 검토 DLL/cfg/ZIP 동일 바이트를 확인했다. 공통 UI 2,743개·팝업 981개·후보 DLL 연결 83개를 포함하며, 인증 없는 공개 페이지·다운로드의 CRC·두 파일·내부 바이트·SHA-256도 통과했다. [alpha.3 배포 기록](ISSUE-5-ALPHA3-RELEASE.md)에 파일·게시 식별값을 기록한다.

전체 로그는 effective 입력 단계에서만 누름·해제를 시작하고, `Used`+원래 입력은 정리에만 사용한다. 버튼·이벤트 검색 칸 안에서 별도 터치 단계를 마우스로 전달해 native 입력의 잘림·소유권 처리를 유지한다. 팝업은 첫 클릭으로 선택하고 선택된 행의 새 클릭으로 표시된 일반/명시 조건 무시/무작위를 확정한다. 누름 뒤 토큰·대상 객체·결과·진행 방식이 바뀌면 취소한다.

Latest 정식 v1.0.5와 기존 26개 Release·태그·자산을 보존했고 이슈 #2 closed·#4 open·#5 open을 유지했다. 영수증은 `artifacts/releases/v1.0.6-alpha.3/`에 보존하며 게시 결과의 후속 문서 커밋은 빌드 소스·태그·설치 자산을 바꾸지 않는다.

사용자는 alpha.2의 팝업 터치 선택 성공과 전체 로그 터치 실패를 보고했다. 실제 Windows 창의 이벤트 전달은 관찰하지 않았으므로 native 창 필터를 증상의 확정 원인으로 쓰지 않는다. 새 alpha.3의 실제 장치 결과는 [Windows 검증표](ISSUE-5-WINDOWS-VALIDATION.md)에 별도로 남긴다. 아래 alpha.2의 검사·게시 수치는 당시 기록이다.

## PRD #5 — v1.0.6-alpha.2 prerelease

**2026-10-20 01:02:17 KST에 [v1.0.6-alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.2) prerelease를 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.2/DiceMaster-v1.0.6-alpha.2.zip). 고정 소스·태그 `e71020020b1b7f1938f22ff4a60d046c8bace839`의 재빌드·14개 검사와 공개 다운로드의 SHA-256·CRC·두 파일·내부 바이트 검증을 통과했습니다. 공통 UI 2,654개·팝업 503개·후보 DLL 연결 83개 검사를 포함하며 상세 결과와 파일 식별값은 [alpha.2 배포 기록](ISSUE-5-ALPHA2-RELEASE.md)에 있습니다. 새 DLL의 실제 Windows 확인은 [검증표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기하며 아래 alpha.1의 검사·게시 결과를 새 검사로 사용하지 않습니다.

팝업은 선택 후보의 상태에 맞춘 일반/명시 조건 무시 확인을 A/Enter 하나로 요청합니다. Shift+Enter도 같은 확인이며 X의 조건 무시 확정은 제거합니다. 표시 때 확인한 일반 진행이 확정 직전에 불가능해지면 차단하고 새로 표시된 진행 방식을 다시 확인합니다. 대상·결과 읽기 실패는 차단합니다.

공통 GUI는 `GUILayoutUtility.GetRect`로 영역을 확보한 뒤 `GUI.Button`을 호출해 Unity의 이벤트 소비에 영향을 받지 않도록 합니다. 팝업의 새 프레임·포인터 전환·입력 모드 변화는 같은 문맥의 유효한 누름 기록을 보존하며 이미 대기 중인 확정은 취소합니다. 초점·문맥·대상·연결·소유자 등 안전 경계와 해제·중립 보호는 유지합니다. 자동 검사에는 실제 Unity와 같은 버튼 누름 이벤트 소비 및 프레임을 나눈 누름/해제 회귀를 포함하고, 실제 장치 결과와 구분합니다.

`verify-preview-release.py --scope prd5`로 기존 모드 DLL 0개에서 고정 커밋을 재빌드해 검토 후보의 DLL/cfg/ZIP 바이트 일치를 확인했습니다. 영수증은 `artifacts/releases/v1.0.6-alpha.2/`에 보존합니다. 기존 `codex/prd5-prerelease` 작업 브랜치와 새 태그를 사용했고 기본 main은 변경하지 않았습니다. Latest 정식 v1.0.5와 기존 25개 Release·태그·자산을 보존하고 이슈 #5는 open으로 유지했습니다. 게시 결과를 보충하는 후속 문서 커밋은 소스 태그·설치 자산을 바꾸지 않습니다.

## PRD #5 — v1.0.6-alpha.1 게시 이력

**2026-10-20 00:32:06 KST에 [v1.0.6-alpha.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.1) prerelease를 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.1/DiceMaster-v1.0.6-alpha.1.zip). 고정 소스·태그 `9c257b76b8a551022b6ea606c35b91e870c4a654`의 새 빌드·14개 검사와 공개 다운로드의 SHA-256·CRC·정확히 두 파일·내부 바이트를 확인했습니다. Latest 정식 v1.0.5와 기존 24개 Release·태그·자산을 보존했습니다. 새 DLL의 Windows 확인과 이슈 #5 완료는 대기입니다.

아래 준비 절차는 게시 직전 기록입니다. 최종 식별·설치·복구·확인 범위는 [배포 기록](ISSUE-5-ALPHA1-RELEASE.md)를 따릅니다.

2026-10-20 사용자의 “v1.0.5에서 Windows 확인 완료”를 선행 기준으로, 현재 구현의 커밋·prerelease 게시 지시를 받았습니다. 대상은 `v1.0.6-alpha.1`, 설치 자산은 `DiceMaster-v1.0.6-alpha.1.zip`입니다. [변경·지원 경로·해시·설치/복구](ISSUE-5-ALPHA1-RELEASE.md), [구현 당시 기록](ISSUE-5-IMPLEMENTATION.md), [새 Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)를 구분합니다. Latest 정식 v1.0.5를 유지하고 이슈 #5는 종료하지 않습니다.

`EventSelection/`은 실제 manager의 직접 리터럴 점 `Position` 호출이 비메인 Lua 코루틴에서 실행될 때만 대기·문맥 토큰·일반/명시 조건 무시 결과 직접 지정을 담당합니다. `LogUI/EventPopupRenderer.cs`는 조회와 명시 요청을 담당합니다. 동적/별칭/콜론/메인 Lua/직접 동기 경로는 원래 판정으로 넘깁니다. 이벤트 예약·99999 보정은 제거하고 미션 예약·주사위와 기존 로그/공유 native 입력·해제/중립을 유지합니다. 표시용 평가는 원본 판정 체인을 호출하지 않고 가시성·0.25초/5초·캐시 계약을 유지합니다.

로컬 후보와 격리 소스 재빌드의 14개 검사 프로그램이 통과했습니다. 게시에는 고정 커밋을 `verify-preview-release.py --scope prd5`로 재검증해 검토 DLL/cfg/ZIP 바이트와 비교합니다. 영수증 경로는 `artifacts/releases/v1.0.6-alpha.1/`이며 실제 결과는 검증 완료 후 기록합니다. DLL/cfg 두 파일 패키지·게임 참조 비배포와 DLL만 교체/이전 DLL 복구를 유지합니다. **새 DLL의 Windows 게임·입력·화면 확인은 대기**이며 자동 대역 검사와 구분합니다. 아래 PRD #4·이전 개발 기록은 당시 이력입니다.

## 개발 범위

PRD #4의 현재 로컬 작업은 [로그 GUI·입력 구현 기록](ISSUE-4-IMPLEMENTATION.md)을 따릅니다. 선행 PRD #2 완료 후 `102cafe`에서 시작했으며 `LogBridge.Model`→`LogUI.LogRenderer`→동일 게시 대상 예약 검증과 공유 XInput의 로그 우선 소유권을 전체 소스로 빌드합니다. `LogUiModelTests`와 `LogUiWiringTests` 및 기존 실제 DLL 통합 검사를 후보 생성 경로에 연결합니다. 새 후보의 사용자 Windows 확인은 [별도 검증표](ISSUE-4-WINDOWS-VALIDATION.md)에 남기며 과거 배포/검증 이력과 구분합니다.

현재 기본 개발·배포 경로는 `src/DiceMaster/DiceMaster.csproj`의 전체 소스 빌드입니다. [이슈 #3 PRD](https://github.com/jungpaeng/dicemaster/issues/3)에 따라 전체 모드 C#·리소스를 복원하고 Gamepad·LiveLog를 같은 프로젝트에 통합했습니다. 기존 `DiceMaster.dll`을 컴파일 입력으로 읽거나 패치하지 않습니다. 루트 솔루션·검증·Release 패키징을 이 경로에 맞춰 사용하며 [정식 v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)을 2026-10-19 00:10:53 KST에 게시했습니다. 소스 태그 커밋은 `ced38df9f0560a212559e34fd281fddbd2581ff4`이며 이슈 #3은 00:11:02 KST에 완료로 닫았습니다. 기본 정책과 사용자 확인 근거는 `build/release-policy.json`에서 관리합니다.

복원·비교 기준은 `v1.0.3-alpha.3` / `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec` / DLL SHA-256 `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633`입니다. 구성·재생성·빌드·검사·실패 처리는 [SOURCE-BUILD.md](SOURCE-BUILD.md)에 기록합니다. 기존 패치 공정은 비교·복구용으로 보존하며 입력은 고정 Git 커밋에서 해시를 검증해 재생성하고 출력은 `artifacts/legacy-candidate/`에 분리합니다.

2026-10-19 사용자가 공개 [alpha.4 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)을 직접 내려받아 Windows 검증을 완료했다고 확인했습니다. 대상은 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`에서 빌드한 DLL SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`입니다. 정식 v1.0.3 DLL은 번역 리소스의 주석 세 줄 정리로 전체 해시가 다릅니다. Windows 사용자 확인과 두 DLL의 자동 비교를 별도 증거로 기록하며 정식 v1.0.3 DLL 자체를 Windows에서 새로 실행한 결과로 바꾸어 쓰지 않습니다. 게임·로더·패드 환경과 세부 검증 결과는 제공되지 않았습니다. 이 작업 중 에이전트가 Windows 설치·게임 실행을 수행하거나 요청하지 않습니다.

이 전환은 기존 동작을 보존하는 소스 복원 범위입니다. 로그 갱신과 모드 전용 난수는 alpha.3에서 이어지며 실제 게임의 미래 판정을 예측하지 않습니다. 이슈 #1·#2의 기능이나 상태를 이번 작업에서 추가 변경하지 않습니다. 다음 성공 Lua까지 기다릴 수 있는 기존 수명주기 제한과 과거 검증 기록은 보존합니다. 이전 prerelease의 게시 커밋·검사·해시는 [배포 기록](ISSUE-3-RELEASE.md)과 [이슈 #1 검증 기록](ISSUE-1-VALIDATION.md)에서 당시 기록으로 확인할 수 있습니다.

## PRD 2 prerelease·실제 자료 후속

2026-10-19 `v1.0.4-alpha.5`의 사용자 Windows 테스트 완료 확인을 근거로 [이슈 #2](https://github.com/jungpaeng/dicemaster/issues/2)를 completed로 종료했습니다. 현재 완료 근거는 [별도 기록](ISSUE-2-COMPLETION.md)을 따르며 아래 과거 후보의 미확인 설명과 지원 한계는 당시 기록으로 구분합니다.

`v1.0.4-alpha.1` prerelease의 변경은 [PRD 2 구현 기록](ISSUE-2-IMPLEMENTATION.md)을 따릅니다. 과거 인계문과 달리 최신 HEAD의 전체 소스로 구현하며 alpha.2는 실패 재현용 비교 입력입니다. 새 분석기는 `src/DiceMaster/LiveLog/Preview*.cs`, 기존 파서·Action 연결은 `src/DiceMaster/LuaParse/`, 리소스 경계는 `DataHelper.ReadPreviewScript`에 둡니다. 고정 분석과 표시 시점의 읽기 전용 현재값을 분리합니다. 공유 선택지·별도 이벤트 파서, 자동 갱신 중 재분석 없음, 같은 문맥의 미지원 최초 진단과 실제 읽기 실패 재시도 계약을 유지합니다.

`build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-2/candidate`로 별도 후보와 검사를 생성합니다. 실제 TextAsset 2,685개와 번체 이야기 2,198개의 정적 대조를 추가했습니다. [실제 Lua 검증 기록](ISSUE-2-REAL-LUA-VALIDATION.md)과 [재현 도구](../tools/RealLuaAudit/README.md)에서 범위를 구분합니다. 후속은 FIFO 큐와 문맥별 정적 구문·상수 계획 LRU를 사용하며 오프라인 추적 한도는 깊이 8·고유 스크립트 32·1,310,720자·262,144토큰·524,288작업입니다. 실제 게임 상태·Windows·Unity 프레임 비용은 별도 미검증이며 과거 alpha.4 확인을 이 후보에 적용하지 않습니다. 비교·복구용 로그 패치는 새 파서를 포함하므로 `--game-managed`의 읽기 전용 참조가 필요합니다. 설정·세이브·리소스·입력 조작의 변경은 포함하지 않습니다.

이번 alpha.5 후속은 [정적 분석 공유·현재 조건 표시](ISSUE-2-SHARED-ANALYSIS.md)에 기록합니다. 현재 충족 힌트는 안전한 문맥에만 적용하며, 입증하지 못한 콜백·후속 이야기·선행 변경은 중립 표시를 유지합니다. 공개 alpha.4와 이번 alpha.5 후보의 검사·해시를 구분합니다. [alpha.5 게시 설명](ISSUE-2-ALPHA5-RELEASE.md)과 고정 커밋 재빌드·공개 다운로드 검증을 별도로 기록합니다.

## 파일 구성

```text
DiceMaster.sln                         # 전체 모드와 검사 프로젝트
Directory.Build.props                  # 공통 설정·artifacts 출력
global.json                           # .NET SDK 8.0.426, latestPatch
README.md                              # 설치·조작·설정 요약
CHANGELOG.md                           # 배포 버전별 변경 이력
src/DiceMaster/
├── DiceMaster.csproj                  # .NET Framework 4.8 전체 모드
├── DicePlugin.cs
├── Config/                           # 설정 관리
├── Patches/                          # 주사위·선택지·진단 후킹
├── LuaParse/                         # 선택지 분석
├── EmbeddedTranslations/             # 번역 구현
├── Gamepad/                          # 패드 입력·XInput 연결
├── LiveLog/                          # 현재 탭·난수·수명주기·게임 경계
├── Properties/                       # 어셈블리 속성
└── Resources/                        # 번역·UI와 native/MinHook·라이선스
tests/                                # 동작·통합·후킹·ABI·리소스 검사와 대역
build/
├── build-source.py                   # 자동 검사 후 별도 후보 생성
├── source_build.py                   # 공통 빌드·참조·출력 관리
├── baseline.py                       # 고정 Git 커밋에서 기준 DLL 재생성
├── verify-baseline.py                # 기준 재생성·해시·캐시 보호 검사
├── verify-source.py                  # 고정 기준과 후보 비교
├── verify-clean-source.py            # 기존 모드 DLL 없는 격리 빌드
├── verify-source-failures.py          # 실패 진단·산출물 보존 검사
├── verify-release.py                 # 소스 스냅샷 빌드·비교·Release ZIP 검사
├── release-policy.json              # 기본 소스 빌드·Windows 사용자 확인 근거
└── reference-contract.json           # 읽기 전용 외부 참조의 고정 해시
tools/
├── SourceRecovery/                   # 기준 DLL에서 최초 복원 재현·템플릿
├── LegacyPatch/                      # 보존한 기존 패치 도구·payload 프로젝트
└── Shared/                           # 기존 DLL payload 복사 도구
packaging/config/DiceMaster.cfg         # 새 후보 ZIP에 포함할 설정 원본
docs/                                  # 개발·복원·배포·검증 기록
artifacts/                             # 생성 파일, Git 제외
├── bin/                              # 일반 dotnet build 산출물
├── obj/                              # 공통 중간 산출물
├── source-candidate/                 # 검증한 DLL·ZIP·검사 보고서
├── release-candidate/                # Release 사전 검증 결과
├── baselines/v1.0.3-alpha.3/          # 고정 Git 기준에서 재생성한 DLL
└── legacy-candidate/                 # 기존 패치 공정의 별도 후보 출력
```

`DiceMaster.csproj`는 Gamepad·LiveLog를 실제 하위 폴더의 C# 소스로 한 번 컴파일합니다. `tools/LegacyPatch/`의 payload 프로젝트는 이 같은 소스를 링크하여 기존 비교·복구 공정을 유지하며 루트 솔루션에는 포함하지 않습니다. 게임 DLL·게임 소스는 외부 참조 경로에서 읽기만 합니다.

소스·테스트·도구·패키지 설정은 Git으로 관리합니다. `artifacts/`, `bin/`, `obj/`는 생성 파일이므로 제외합니다. 예전 `development/releases/`의 출력과 게시 영수증은 이동하거나 정리하지 않고 그대로 보존하며 새 결과의 기준으로 사용하지 않습니다. 기존 `development/tools/`의 패드·로그 패치와 복원 명령 세 개는 새 `tools/` 명령을 호출하는 호환 진입점입니다.
루트 `config/`·`plugins/`는 제거했습니다. 새 패키지 설정은 `packaging/config/DiceMaster.cfg`에서 읽습니다. 비교·복원·기존 패치 명령은 `build/baseline.py`로 고정 Git 커밋의 DLL을 꺼내 SHA-256을 검증하고 `artifacts/baselines/v1.0.3-alpha.3/DiceMaster.dll`을 사용합니다. 기준 캐시는 지워도 재생성할 수 있으며 패치 출력과 분리합니다. 일반 소스 빌드는 Git 데이터나 기준 캐시에 의존하지 않습니다. ZIP 내부 설치 경로 `config/`·`plugins/`는 계속 유지합니다.

## 빌드 환경

- Python 3: 빌드 스크립트 실행
- .NET 8 SDK `8.0.426`: 전체 모드, DLL 통합 도구, 테스트 빌드. 루트 `global.json`은 같은 SDK 기능 대역의 최신 패치만 허용합니다.
- 최초 빌드 시 NuGet 패키지를 복원할 수 있는 환경

전체 모드와 보존한 패드·로그 payload 코드는 .NET Framework 4.8을 대상으로 합니다. 참조 어셈블리는 `Microsoft.NETFramework.ReferenceAssemblies.net48` 패키지로 가져옵니다.
DLL 통합 도구와 테스트는 .NET 8을 대상으로 하며, 통합 도구는 Mono.Cecil 0.11.6을 사용합니다.

모드의 실제 패드 실행 대상은 Windows/XInput입니다. 다른 OS에서 입력 로직을 빌드·검증하는 것과 Windows 게임에서 실행하는 것은 별도의 확인 항목입니다.

## 전체 소스 빌드

루트 솔루션으로 프로젝트를 열거나 다음 명령으로 컴파일합니다.

```sh
dotnet build DiceMaster.sln -c Release \
  -p:GameReferences='<읽기 전용 참조 루트>'
```

주 DLL은 `artifacts/bin/DiceMaster/release/DiceMaster.dll`에 생성됩니다. 기존 자동 검사와 두 파일 ZIP 생성을 포함한 별도 후보 빌드는 다음 명령입니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>'
```

이 결과는 `artifacts/source-candidate/`에 DLL·ZIP·영수증을 함께 생성합니다. 산출물을 한 후보 폴더에서 교체해야 검사 실패나 중단 시 서로 다른 실행의 DLL과 ZIP이 섞이지 않으므로, 검증 후보는 일반 컴파일 결과와 별도 위치를 사용합니다. 참조 해시·기존 모드 DLL 없는 빌드·기준 비교·재생성은 [SOURCE-BUILD.md](SOURCE-BUILD.md)를 따릅니다. 기본 개발·배포에 이 소스 빌드를 사용하며 자동 검사 결과와 alpha.4 사용자 Windows 확인 근거를 함께 기록합니다.

## 비교·복구용 기존 패치 빌드 방법

기본 전체 소스 빌드와 검사는 [SOURCE-BUILD.md](SOURCE-BUILD.md)를 따릅니다. 아래 기존 패치 명령은 이전 공정을 재현하거나 비교·복구할 때 사용하며 일반 제품 빌드·배포에는 사용하지 않습니다.

아래 명령은 macOS의 로컬 개발 후보 생성·검사 예시이며 Windows 게임 설치·실행이나 배포가 아닙니다. 읽기 전용 수신 참조는 입력으로만 사용합니다.

아래 명령은 저장소 루트에서 실행합니다. 환경에 따라 Python 실행 명령은 `python3` 대신 `python`일 수 있습니다.

먼저 별도 파일로 생성하여 변경 내용을 확인할 수 있습니다.

```sh
python3 tools/LegacyPatch/build-gamepad.py \
  --output artifacts/legacy-local/DiceMaster.dll
```

기본 후보 `artifacts/legacy-candidate/gamepad/DiceMaster.dll`을 생성하려면 다음을 실행합니다.

```sh
python3 tools/LegacyPatch/build-gamepad.py
```

입력 DLL을 따로 지정할 수도 있습니다. 다음 기준 파일은 기본 실행이나 `python3 build/baseline.py`가 재생성합니다.

```sh
python3 tools/LegacyPatch/build-gamepad.py \
  --input artifacts/baselines/v1.0.3-alpha.3/DiceMaster.dll \
  --output artifacts/legacy-local/DiceMaster.dll
```

루트 `Directory.Build.props`는 `artifacts/`에 일반 출력과 중간 파일을 모으며, 임시 중간 출력 경로를 지정해도 소스 트리의 기존 `obj/`·`bin/` 생성 C# 파일을 다시 포함하지 않게 합니다.

도구는 임시 디렉토리에서 빌드·통합하고 테스트를 실행한 뒤 출력 파일을 씁니다.
빌드·테스트가 실패하면 후보 DLL을 교체하지 않습니다. 기본 입력과 출력은 분리되며, 명시적으로 같은 경로를 지정한 경우에는 빌드 중 입력 DLL이 바뀌지 않았는지도 확인합니다.

## 패드 입력 처리

조정 흐름은 다음과 같습니다.

1. 게임이 XInput 입력 경로를 사용하는지 확인합니다.
2. 주사위 입력창의 활성 상태, 게임 창 포커스, 설정값을 확인합니다.
3. 원래 패드 상태를 읽어 LB와 십자키·왼쪽 스틱을 수치 조정에 사용합니다.
4. 게임에 전달되는 상태에서는 조정용 버튼과 왼쪽 스틱 입력을 제외합니다.

A·X와 오른쪽 스틱·트리거는 기존 입력을 유지합니다. A·X 전용 처리나 키 입력 에뮬레이션은 없습니다.
LB를 먼저 놓아도 조정에 사용한 B·방향키는 해당 버튼을 놓을 때까지, 왼쪽 스틱은 중앙으로 돌아올 때까지 제외합니다.

입력 경로, 임곗값, 네이티브 라이브러리 수명 관리는 [패드 구현 상세](GAMEPAD.md)를 참고하세요.

### 수치의 의미

- `0`: 게임의 원래 주사위 굴림에 맡깁니다.
- `0`이 아닌 정수: 기존 DiceMaster 코드가 지정값을 주사위 결과에 반영합니다.
- 패드에서도 음수와 100을 넘는 값을 지정할 수 있습니다.
- 좌우 ±1, 상하 ±10을 그대로 적용합니다. 1에서 아래로 조정하면 −9입니다.
- 정수 오버플로를 막기 위해 32비트 부호 있는 정수 범위에서만 값이 증가·감소합니다.
- LB+B는 0으로 복귀하고, 조정 입력이 해제되거나 중앙으로 돌아올 때까지 재조정을 기다립니다.

이는 게임의 주사위 상한을 다시 계산해 제한하는 방식이 아닙니다. 원본의 숫자 입력·결과 적용 코드에도 게임 상한과 음수를 제한하는 처리가 없었습니다.

## 기존 기능의 구현 위치

다음 기능은 `src/DiceMaster/`의 전체 소스로 복원했습니다. 아래 이름은 기준 DLL과 복원 소스를 대조할 때의 탐색 기준입니다. 기존 패치 도구가 같은 기능이 들어 있는 DLL을 입력으로 사용하는 경로는 비교·복구용으로만 보존합니다.

| 기능 | 주요 클래스·메서드 |
|---|---|
| 주사위 입력 생성 | `DicePatch.DiceMenuDialog_Awake_Postfix` |
| 주사위 창 초기화 | `DicePatch.DiceMenuDialog_OnEnable_Postfix` |
| 지정값을 판정에 반영 | `DicePatch.CheckPointManager_Dice_Prefix` |
| Lua 스크립트 분석 시작 | `OptionInfoPatch.LuaEnvironment_LoadLuaFunction_Prefix` |
| 선택지 결과 추출 | `LuaParser.ExtractOptionActionsFromLua`, `LuaParse.Action` |
| 선택지 옆 요약 표시 | `OptionInfoPatch.LeanLocalizationResolver_GetStoryText_Postfix` |
| 로그 창 선택지 표시 | `LogBridge.Publish` → `LogBridge.Model` → `LogUI.LogRenderer` (기존 `DicePlugin.AddOptionEntry` 튜플 경계 보존) |

선택지 요약은 번역·서식 처리를 거친 뒤 50자로 줄여 붙입니다. 로그 창에는 잘리기 전 설명을 별도로 저장합니다.
조건별 전체 설명의 줄바꿈은 요약에서 ` / `로 바꾸며, 태그는 제거합니다. 미해석 부분이 있으면 `추가 영향 가능`을 앞에 두어 잘린 요약에서도 안내가 남습니다. 실제 같은 `if/elseif/else`의 인접 효과만 경우 경로별로 묶고 중립색을 붙입니다. 조건문이 하나면 `[경우 A]`, 여러 개이거나 중첩되면 `[경우 1-A]` / `[경우 1-A → 2-B]`처럼 구분합니다. 독립 조건문·중첩 경로·선행 미지원 호출·단락 평가를 구분하며 Lua 조건을 실행하거나 갈래 색상을 현재 충족 판정으로 사용하지 않습니다. 불확실한 현재 효과는 `가능한 변화`, 후속 효과는 `이후 가능한 변화` 아래에 묶으며, `조건부`를 효과마다 반복하지 않습니다. 표시 순서를 유지하고 이후 확실한 현재 효과가 있으면 `현재 선택` 제목으로 묶음 경계를 끝냅니다. 부호와 중복되는 `가산`을 제거하고 설정·초기화·center/mental 의미 설명은 유지합니다. 스크립트 이름으로 중복 제거한 연결에 진입 분기 경로를 임의로 승계하지 않습니다.
분석은 게임이 제공한 Lua를 고정된 규칙으로 읽는 방식입니다. 기존 형식의 선택지 추가는 자동으로 반영할 수 있지만, 새 함수·스크립트 형식·게임 API 변경은 별도 대응이 필요할 수 있습니다.

## 로그 정보 갱신 재설계

### v1.0.2의 확정 회귀

v1.0.2는 자동 갱신에서 네 탭의 표시를 한꺼번에 재구성합니다. 재구성 경로가 공유 분석 결과인 `OptionResult`를 비우면서 Lua 파싱 완료 캐시인 `luaOptions`는 유지하므로, 이미 분석한 Lua의 결과를 다시 채우지 못합니다. 게임 선택지 옆 요약도 같은 분석 데이터를 사용하므로 영향 범위에 포함됩니다. 수동 새로고침 직후 결과가 보이다가 주기 갱신 후 사라질 수 있습니다.

기존 검사는 갱신 래퍼 일부를 실행했지만 실제 재구성 본체와 파서 연결을 단순한 테스트 대역으로 대체했습니다. 과거 검사 통과는 이 캐시 조합을 검증했다는 근거가 아닙니다. 한 항목의 내부 실패가 전체 성공이나 빈 목록으로 처리되는 경로, 전체 탭 재구성과 할당 비용은 재설계에서 확인할 구조적 위험이며 모두 새로 발생한 게임 회귀로 단정하지 않습니다.

### 데이터 소유권과 표시 계약

공유 선택지 분석은 `LuaParser.Instance`를 사용하는 실제 Lua 경계가 소유합니다. 이벤트 고정 설명은 `LogBridge.GetEventParser` → `PositionEventCache.GetDefinitionParser`로 얻는 별도 파서 인스턴스에 맡겨, 콜드·지연 준비 이벤트 스크립트 분석도 공유 선택지 캐시를 덮어쓰지 않게 합니다.

`RefreshEngine`은 현재 탭과 항목별 값·오류·재시도 시각을 관리합니다. `LogBridge`는 기존 모드의 파서·캐시·표시 경계에 연결하며, 전체 소스 후보는 이를 같은 어셈블리에서 컴파일합니다. 보존한 기존 패치 공정은 통합 도구가 같은 소스를 기존 DLL에 넣습니다. 이벤트의 고정 설명은 고정 분석 경계에서 준비하고, 늦게 준비되거나 실패한 정의는 준비 시 한 번 분석할 수 있습니다. 정상 웜 캐시 주기에는 이를 다시 분석하지 않습니다.

전달받은 실제 게임 DLL에서 확인한 `SaveSystem`의 새 게임·공개 로드·공통 로드 6개 메서드와 private `SceneController.StartLoadScene`의 Prefix에서 문맥을 무효화합니다. Prefix/Postfix/Finalizer는 `LifecycleState`의 호출 토큰으로 중첩·중복·오래된 콜백을 구분합니다. void 로드 반환과 씬 코루틴 생성은 준비 완료로 처리하지 않습니다.

Lua Prefix는 원문을 임시 보관하고 이전 문맥을 지우며, `LoadLuaFunction`의 non-null 결과 Postfix에서만 새 분석을 활성화합니다. null·예외·누락 스크립트·전환 중 완료는 계속 차단합니다. 억제 중 자동·수동·탭 전환은 이전 원문 재분석이나 현재값 조회를 하지 않습니다. 제자리 복원이 새 Lua를 만들지 않는 경우 다음 실제 성공 Lua까지 대기하는 것이 현재 안전 정책입니다. 이 연결의 자동 검사와 alpha.4 사용자 Windows 완료 확인을 구분해 기록하며 Harmony·UI 항목별 상세 결과는 제공되지 않았습니다.

이 절은 이슈 #1의 구현 계약입니다. 실제 구현·자동 검사·게임 확인 상태는 [검증 기록](ISSUE-1-VALIDATION.md)을 따릅니다.

| 데이터 | 소유권과 갱신 원칙 |
|---|---|
| 고정 Lua 분석 결과·파싱 완료 캐시 | Lua 분석 경로가 소유합니다. 자동 표시는 읽기만 하며, 결과와 완료 표시를 함께 유지합니다. 정상 주기 갱신 중 재분석하지 않습니다. |
| 현재값·조건 판정·확률·표시 문자열 | 표시 경로가 소유합니다. 열린 현재 탭의 정상 행만 0.25초 간격으로 다시 평가합니다. |
| 마지막 정상 값·실패 원인·다음 재시도 시각 | 항목별로 보관합니다. 한 항목의 오류가 다른 행이나 다른 탭의 최신 값을 되돌리지 않습니다. |
| 세션·Lua 문맥 | 새 Lua·세이브 로드·새 게임에서 교체합니다. 이전 문맥의 행·실패 상태를 새 정보와 섞지 않습니다. 확인된 전환 Prefix에서 차단하고 성공 Lua 컴파일 뒤 새 세대로 교체합니다. alpha.4 사용자 Windows 완료 확인은 받았으며 이 항목의 별도 실행 로그는 제공되지 않았습니다. |

`SetFlag`·`AddFlag`의 값은 예정된 변경입니다. 표시용 현재 플래그를 읽더라도 이 값을 현재값으로 덮어쓰지 않습니다. 기존 네 탭과 선택지 옆 50자 요약·로그의 전체 설명 계약을 유지합니다.

### 스케줄과 오류 상태

새 설정은 추가하지 않습니다. `EnableOptionInfo`는 기본 `true`이며 변경 후 게임 재시작이라는 기존 계약을 유지합니다. `false`로 시작하면 관련 정보 표시와 자동 갱신을 끄며 주사위·패드 설정은 유지합니다.

| 상황 | 표시용 처리 |
|---|---|
| 창 닫힘 | 현재값 조회·자동 재시도 0회. 게임 Lua 로딩에 따른 고정 분석과 게임 선택지 옆 요약은 계속 유지합니다. |
| 현재 탭 보임 | 정상 행은 0.25초 간격 평가. 보이지 않는 탭은 주기 평가 0회입니다. |
| 창 재열기·탭 전환 | 정상 행은 즉시 평가. 실패 행의 기존 5초 대기 시각은 보존합니다. |
| 항목 조회 실패 | 해당 행만 실패로 전환합니다. 다른 행은 계속 갱신하며 같은 탭이 보일 때 5초 간격 재시도합니다. |
| 게임 데이터 미준비 | `게임 데이터 대기 중`을 표시합니다. 정상 빈 목록과 구분합니다. |
| 첫 조회 실패 | `읽기 실패`를 표시합니다. |
| 이전 정상 값이 있는 행의 실패 | 마지막 정상 값과 `갱신 실패 · 이전 값`을 표시합니다. 복구하면 새 값으로 교체하고 실패 문구를 제거합니다. |
| 게임 데이터 새로고침 | 관련 고정 분석 캐시를 모두 다시 만들고 현재 탭을 즉시 표시합니다. 실패 대기를 건너뛰며, 다른 탭은 열 때 새 분석을 사용합니다. |

내부 조회·파싱 함수가 예외를 잡아 정상 반환하더라도 성공 또는 정상 빈 결과로 간주하지 않아야 합니다. 색상만으로 상태를 구분하지 않습니다.

### 표시의 부작용과 진단

자동·수동 표시 조회는 게임 상태를 바꾸거나 이벤트·미션 실행 예약을 소비하거나 대화 기록을 반복 추가해서는 안 됩니다. 표시 보호 깊이는 `finally`에서 복원하고, 예외 이후 실제 실행 경로도 계속 동작해야 합니다.

전달받은 실제 게임 DLL에서 `StatValueReference.GetValue`의 전이 호출이 `UnityEngine.Random.Range`에 진입하고 `ConditionResultData.Execute`가 `_count`를 증가시킴을 확인했습니다. `DisplayRefreshDepth`만으로는 이 원본 게임 부작용을 막을 수 없습니다. [실제 게임 API 분석](REAL-GAME-API-FINDINGS.md)에 정확한 서명·IL 근거·17개 값 종류·native 경계를 기록했습니다.

alpha.1의 `SafeDisplayEvaluation`은 알려진 필드와 읽기 전용 getter로 확률·그룹·조건을 별도 계산하며 난수는 `RandomDisplayValueException`으로 거부해 행 실패/이전 값에 연결했습니다. alpha.2는 범위·판정 미정·전용 미지원 결과 모델과 formatter, 제한적인 단일 비교 이론 확률을 사용했습니다. 이 정책은 각 게시 버전의 이력으로 보존합니다.

alpha.3은 모드 전용 단일 `System.Random`을 잠금으로 보호해 난수를 생성합니다. 평가 세션은 개별 `ConditionResultItem` 설명 하나 또는 전체 이벤트 설명(헤더·활성 조건·모든 확률 항목 조건) 하나를 경계로 합니다. 이 안에서 객체 동일성에 따라 값·그룹·비교·조건 결과를 재사용하므로 숫자·색상·이벤트 헤더와 하위 설명이 같은 계산에 근거합니다. 서로 다른 난수 참조는 각각 추첨하고 다음 갱신은 새 세션을 시작합니다. 미션·스위치의 전체 행은 여러 조건 항목을 별도로 평가하므로 항목 사이에 공유한 참조는 다시 추첨할 수 있습니다. 독립 조건 설명의 경계는 미션·스위치 설명 전체가 아닌 `ConditionResultItem` 하나입니다. 게임의 RNG 호출이나 `ConditionResultData.Execute()`는 복원하지 않습니다. Sum/Max/Min/Multiply, 중첩·여러 난수·조건 설정을 별도 계산하며 `_count`를 보존합니다. 양끝을 포함하는 전체 int 구간을 안전하게 다루고 역순 저장 구간은 정규화합니다. 게임 원본의 `max + 1` overflow나 역순 구간 의미를 그대로 모사하는 규칙은 아닙니다.

UI에는 난수 숫자와 계산된 판정 색상을 그대로 표시하고 `표시용`·판정 미정·미지원·이론 확률 부가 문구를 붙이지 않습니다. 이 숫자·색상·이벤트 확률은 모드 난수에 따른 해당 갱신의 계산이며 실제 게임 실행 결과나 확률 분포를 보장하지 않습니다. 알 수 없는 enum·미지원 변환 하위 타입·순환 자료와 실제 조회 실패는 기존 오류 경로에 남깁니다. `GameStat`은 확인된 변환 타입만 허용하며 native `AnimationCurve.Evaluate`와 실제 리소스는 실행 검증하지 않았습니다. 세부 구현 계약과 새 검사 결과는 [난수 후속 구현](RANDOM-PREVIEW-FOLLOWUP.md)과 [검증 기록](ISSUE-1-VALIDATION.md)을 따릅니다.

실제 `ConditionResultItem.Check`의 null 목록은 잘못된 자료로 실패하고, 유효한 빈 목록은 false입니다. 후보는 이 차이를 유지하며 기존의 경고·빈 결과로 실패가 숨겨지는 경로를 순수 평가로 바꿉니다. 알려진 모드 설명 호출은 내러티브 처리 중 보호 깊이 0인 경우에도 순수 평가에 연결합니다. 원본 게임 어셈블리와 실제 게임 진행·명시적 실행 코드는 교체하지 않습니다.

진단은 기존 BepInEx 로그를 사용합니다. 같은 게임 세션·항목·원인의 연속 오류는 최초 1회, 복구는 1회 기록합니다. 탭·항목 식별자와 예외 정보는 남기고 전체 Lua 원문과 세이브 내용은 기록하지 않습니다. 실패 원인은 예외 타입과 최초 발생 메서드·IL 위치를 기준으로 식별합니다. 캐시한 예외를 `ExceptionDispatchInfo`로 반복 전달하며 스택 전달 구간이 늘어나도 같은 오류 로그를 반복하지 않도록 검사합니다. 진단에는 스택을 남기지만 예외 `Message`·`ToString` 전체를 기록하지 않습니다. 최초 발생 위치가 바뀌거나 새 문맥이 시작되는 경우의 식별과 복구 로그도 자동 검사 대상입니다.

0.25초는 재계산 빈도를 제한하는 기존 선택값이며 실측으로 확정한 성능 기준이 아닙니다. 이번 작업에는 고정 수치 상한을 추가하지 않습니다. 구조적 기준과 시간·할당량 측정 절차는 [검증 기록의 성능 측정](ISSUE-1-VALIDATION.md#성능-측정)에 적습니다.

로그 패치만 재생성하려면 다음 명령을 실행합니다. 패드 빌드 도구와 같은 `--input`, `--output` 옵션을 지원합니다.

```sh
python3 tools/LegacyPatch/build-log-refresh.py \
  --game-managed '<읽기 전용 참조>/Mortal_Data/Managed' \
  --output artifacts/legacy-local/DiceMaster.dll
```

`--game-managed`는 읽기 전용 게임 DLL에서 실제 IL과 후킹 계약을 검사합니다. macOS의 .NET 검사로 native·Unity 경계를 대역에 옮겨 실행하며 게임을 실행하지 않습니다.

`build-log-refresh.py`는 출력 전 패치를 다시 적용한 별도 후보에도 실제 파서 통합 검사와 DLL 구조 검사를 실행합니다. payload 교체 전에 메서드 본문을 읽고 이전 후킹 참조를 새 참조에 연결하며, 해결되지 않은 호출이 있으면 출력하지 않습니다.

로그 패치의 기본 출력은 `artifacts/legacy-candidate/live-log/DiceMaster.dll`입니다. 패드 빌드와 로그 패치는 서로의 변경을 유지해야 하며, 반복 적용·두 도구의 교차 실행 후에도 갱신 호출과 패드 호출이 중복되지 않아야 합니다. 교차 실행 검사는 직전 후보를 다음 도구의 `--input`으로 명시합니다.

## 검증

전체 소스 후보는 새 DLL을 기존 관리 코드·실제 파서 통합·게임 API 메타데이터·수명주기·패드 검사에 연결합니다. 컴파일러가 바꿀 수 있는 메서드 IL 지문과 보존해야 하는 동작·메타데이터·후킹·native ABI·리소스 계약을 구분한 검사와 이번 실행 결과는 [SOURCE-BUILD.md](SOURCE-BUILD.md)에 기록합니다. 아래 과거 패치 검사 기록은 새 소스 후보의 통과 기록으로 승계하지 않습니다.

### 패드 빌드 검사

테스트는 패드 지원 어셈블리와 통합한 DiceMaster DLL에 각각 실행됩니다.

- LB가 없는 경우와 주사위 창 밖에서 수치가 유지되는지
- 양수·음수·0을 오가는 조정과 정수 경계 처리
- LB+B 복귀, 반복 입력과 방향 변경
- 스틱 임곗값, 대각선 방향, 십자키 우선 처리
- 전체 디지털 버튼 조합과 조정용 입력 차단
- A·X, 오른쪽 스틱, 트리거 보존
- 버튼 해제 순서, 스틱 중앙 복귀, 연결 해제 시 상태 초기화
- XInput 구조체 크기와 비지원 OS의 초기화 실패 처리

이 검사는 Windows 네이티브 후킹과 실제 게임 화면을 실행하는 검사는 아닙니다.

### 로그 갱신 검사

`build-log-refresh.py`는 패치한 DLL의 실제 메서드 본문을 검사에 사용하며 게임·Unity 경계는 필요한 경우 모의 구현합니다. 이슈 #1 회귀 검사는 **실제 Lua 파서 → 공유 분석 캐시 → 실제 표시 갱신 → 게임 선택지 옆 요약**을 연결해야 합니다. 파서나 갱신 본체를 단순한 행 추가 함수로 대체하면 이 회귀를 검증했다고 기록하지 않습니다.

- 콜드·웜 캐시 연속 자동 갱신 및 수동→자동 갱신 후 선택지 결과 유지
- 현재 조건값·판정 색상·확률·상태 의존 설명 갱신, `SetFlag`·`AddFlag` 예정값 보존
- 네 탭, 창 숨김·재열기·탭 전환, 설정 비활성화와 타이머·재진입 방지
- 첫 행·중간 행·내부 함수 실패 격리, 정상 빈 목록과 데이터 미준비 구분
- 마지막 정상 값·세 상태 문구·5초 재시도·전환 시 대기 유지·수동 즉시 재시도·복구
- 실제 전환·컴파일 토큰으로 같은 manager·같은 Lua·로드 실패·null 결과·오래된 콜백에서도 이전 행·실패 상태 제거
- 실제 게임 IL·별도 평가의 17개 값 종류·게임 RNG 비소비·조건 카운터 보존·null/빈 목록·미지원 자료 구분
- 모드 전용 난수의 int 경계·역순 구간·중첩/다중 집계, 개별 조건 항목 또는 전체 이벤트의 숫자/색상/확률 결과 재사용·조건 항목 사이와 다음 갱신의 별도 추첨, UI 부가 문구 부재
- 이벤트·미션 예약과 대화 로그 보존, 예외 후 보호 깊이 복구
- 오류 최초 1회·복구 1회 로그와 민감한 전체 원문 비기록
- 숨김 조회·비활성 탭 주기 평가·정상 주기 Lua 재분석 각각 0회
- 실제 `Update`의 Tab 토글·일반 경로·화면 크기 변경에서 갱신과 패드 호출 유지
- 반복 패치·패드 재빌드의 중복 방지와 무관한 메서드·내장 리소스 보존

검사별 실제 실행 결과, 알려진 대역과 미확인 경로는 [검증 기록](ISSUE-1-VALIDATION.md)에 남깁니다. 이 목록 자체는 검사 통과 기록이 아닙니다.

### 기록된 검증 범위

v1.0.1 DLL에는 별도 비교로 다음을 확인했습니다.

- 직전 배포 DLL과 비교해 변경된 메서드는 패드 수치 조정 함수 1개
- 나머지 344개 메서드와 8개 내장 리소스 보존
- 입력 동작 및 정수 경계 검사 통과

과거 v1.0.2 DLL에는 로그 관련 자동 검사 45개와 패드 검사 525,430개를 통과한 기록이 있습니다. 이슈 #1에서 실제 파서와 재구성 본체를 연결하지 않은 기존 로그 검사의 한계를 확인했으므로, 이 기록은 선택지 소실 회귀 해결이나 이슈 #1 완료 근거로 사용하지 않습니다. 별도 구조 비교로 기존 메서드 중 로그 갱신과 관련한 13개만 변경되고 나머지 332개 및 내장 리소스 8개가 보존되었음을 확인했습니다. 로그 갱신용 메서드 4개를 추가했습니다.
로그 패치 반복 적용과 패드 재빌드 후의 동작 검사도 통과했습니다. 로그 검사의 게임·Unity 경계는 모의 구현이며 실제 Windows 게임에서의 재현 검증은 수행하지 않았습니다.

이 구조 비교는 매번 빌드 스크립트가 자동 실행하는 검사가 아닙니다. 기존 기능을 수정하는 작업에서는 관련 메서드와 번역·에셋·라이브러리 리소스가 의도대로 유지되는지도 따로 확인하세요.

### 실제 게임 확인

사용자가 2026-10-19 공개 alpha.4를 직접 내려받아 Windows 검증을 완료했다고 확인했습니다. 에이전트가 직접 게임을 실행한 결과와 구분하며 사용자 확인의 파일 식별·환경 제공 범위는 [소스 빌드 안내](SOURCE-BUILD.md#사용자-windows-확인과-기본-전환)에 기록합니다. 상세 항목이 없는 포괄 확인을 게임 버전·모드 베이스·BepInEx·패드·Steam Input 조합별 통과 결과로 확대하지 않습니다.

수신된 기존 파일·로그는 Steam appid `1859910` / buildid `26021712`, Unity `2020.3.49f1`, BepInEx `6.0.0-be.692`를 식별합니다. 이 정적 자료의 환경을 이번 사용자 Windows 검증 환경으로 간주하지 않습니다. 정식 산출물과 사용자가 확인한 alpha.4의 자동 비교, 고정 소스 커밋의 새 빌드·정식 게시·공개 다운로드 검증은 [배포 기록](ISSUE-3-RELEASE.md)에 구분합니다.

## 원본과 사용 라이브러리

이 저장소의 기반은 기존 DiceMaster DLL 배포본입니다. 외부 원본 배포 URL과 원제작자 정보는 아직 확인되지 않아 임의로 기재하지 않았습니다.

| 항목 | 용도 | 버전·출처 |
|---|---|---|
| MinHook | Windows XInput 후킹 | [1.3.4](https://github.com/TsudaKageyu/minhook/releases/tag/v1.3.4) |
| Mono.Cecil | DLL 통합 도구 | [0.11.6](https://github.com/jbevain/cecil) |

MinHook의 x86/x64 DLL과 전체 라이선스는 `src/DiceMaster/Resources/native/`에 보관합니다.
실행용 DiceMaster DLL에도 라이브러리와 `DiceMaster.Gamepad.Native.LICENSE.txt` 리소스를 내장합니다.
원본 출처를 확인하면 이 절과 사용자 README의 출처 안내를 함께 갱신하세요.
