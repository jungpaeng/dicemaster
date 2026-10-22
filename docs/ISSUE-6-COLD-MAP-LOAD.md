# 재실행 후 저장된 지도의 대화 안내 — alpha.30

<!-- alpha30-publication -->
v1.0.7-alpha.30 게시 준비 중. 로컬 전체21개 자동 검사·실제 DLL 연결·설치 ZIP 검증을 통과했다. 소스 커밋 고정 재빌드와 공개 다운로드 검증은 게시 과정에서 별도로 기록한다. 새 DLL의 실제 Windows·사용자 세이브·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.
<!-- /alpha30-publication -->

2026-10-22 main에서 보완했다. 기준 main은 `e247108d2337ffd4659d9e6c1c27643cb69780cc`이며 직전 공개 alpha.29의 기능 소스는 `0bee1e26dcc857ca87b7da0b92967befde8eafd4`다. alpha.30 후보로 게시를 준비한다. 고정 소스·주석 태그·공개 검증은 [게시 기록](ISSUE-6-ALPHA30-RELEASE.md)에서 확정한다. 기존 alpha.29 ZIP은 별도 보존한다. 사용자 Windows 세이브·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.

## 원인과 재현 경계

새 게임은 원본 SaveSystem.NewGameData가 Story/ch1_1에서 시작하도록 설정한다. 저장 불러오기는 PlayerStatManagerData.Load가 저장된 CurrentScene을 복원하며 Free 장면으로 바로 진입할 수 있다. 기존 지도 조건/이벤트 풀은 CheckPointManager의 정적 Instance에, 대화 초기화는 LuaManager의 현재 환경 또는 동일 문맥의 관찰에 의존했다. 이전 스토리에서 준비한 관리 자료가 없는 프로세스에서는 필요한 정의를 얻지 못해 해금 미확정으로 표시를 제외할 수 있었다. 지도 등록 목록은 로드 시 유지되므로 목록 삭제를 주원인으로 판단하지 않았다.

수정 전 현재 provider+route+조건 Reader를 합성 경계에서 실행했다. Story owner 둘 다 없으면 조건부 대화 배지가0개였고, 체크포인트만 주면 일반 조건 경로는1개가 됐지만 ResetTalkFlag 뒤의 조건 경로는0개였다. 체크포인트+Lua 초기화 자료를 모두 제공하면 Lua 재분석 없이1개가 됐다. 원본 게임 관리 DLL 정적 분석과 증상에 부합하지만 실제 사용자 Windows 세이브의 실행 추적은 아니다.

## 구현

- `MapDefinitionAssets`는 이미 로드된 ConditionResultConfig·SwitchResultConfig·PositionResultConfig만 타입별로 조회한다. 대화 초기화는 이미 로드된 StatModifyManager의 실제 직렬화 `_talkOptionFlag`를 읽는다. 여러 owner의 목록이 다르면 보류하며 일반 플래그 자산 전체·인물 이름·플래그 접두사로 초기화 대상을 추측하지 않는다.
- 위치/GameObject/전체 MonoBehaviour 검색, Resources.Load/LoadAll, 생성/Instantiate, Instance getter, InitEnvironment·Awake/Reset·조건/분기/Position 실행을 하지 않는다. **한정된 로드 메타데이터 조회가 새로 추가**되며 이전의 ‘Unity 객체 검색 없음’을 그대로 주장하지 않는다. 위치 탐색은 기존 Start/포커스 등록을 유지한다.
- 현재 유효한 스토리 관리자가 있으면 해당 직렬화 정의를 우선한다. 없을 때만 route reader가 로드된 설정 자산을 읽는다. 현재 owner가 손상되거나 잘못된 Lua 등록이면 이전 관찰/자산으로 우회하지 않는다. Unity가 파괴한 객체를 잡고 있는 CLR 정적 참조는 현재 owner로 간주하지 않는다.
- 설정 간 이름이 같은데 서로 다른 정의 객체이면 임의 첫 항목을 택하지 않는다. 조건은 현재 실제 입력/조회별 가상 진입값으로 판독한다. 대화 초기화는 확인한 실제 목록의 `_default`만 조회별로 투영하며 실제 State를 임시 변경/복원하지 않는다.
- 실행 owner가 없는 횟수형 Switch의 직렬화 `_count`를 현재 실행 횟수로 간주하지 않는다. 해당 분기는 미확정이다. 엄격한 기회/1회성 reader에는 설정 자산 대체 경로를 추가하지 않아 일반 경로를 금색 기회로 승격하지 않는다. 기존 해금·완료·반복 활동·확정 false AND 미확정 조건 정책을 유지한다.
- 위치 설정도 같은 로드된 자산 경로로 연결하므로 관리자가 없어도 이벤트 풀의 정적 경로를 준비할 수 있다. 전체 풀 이름 검색은1024개, 설정별 자산은64개, 기존 경로/조건·준비 예산은 유지한다. 없는 풀 자료는 일관되게 null로 처리해5초 재시도 때 대화 전용 경로를 반복 초기화하지 않는다.
- 자산 참조는 캐시하지만 현재 값/판정은 갱신마다 새로 읽는다. 준비 후 정상0.25초마다 전체 자산 검색을 반복하지 않는다. 미확인 자료는5초 후 다시 확인하고 새 게임/로드/장면 전환·두 정보 기능 OFF·종료에서 자료 캐시를 정리한다. 타입 메타데이터만 별도로 재사용해 AppDomain의 반복 어셈블리 검색을 줄인다.

`EnableMapGuide`/`EnableOneShotHints`/상위 `EnableOptionInfo`의 기존 범위를 따른다. 키·기본값·cfg 바이트는 alpha.29와 같다. 숨김/게임 창 가림/OFF와 Xbox/XInput·키보드·마우스·터치·입력 소유권/해제/중립 보호는 유지한다. 정상0.25초/실패5초·warm Lua 재분석 없음·게임 판정/RNG/세이브/능력치/아이템/행동력 읽기 전용 계약을 유지한다.

명시적 지도 로그의 `지도 조회 성능`에 조건·분기·이벤트 풀·대화 초기화 자료가 각각 미조회/확보/미확인 재시도 대기인지 추가한다. 현재 owner의 정의를 쓰는 경우 자산 대체 경로의 ‘미조회’는 오류가 아니다. 대화별 상세의 초기화 출처도 `현재 게임 Lua 연결`과 `로드된 대화 초기화 설정`을 구분한다.

## 지원 한계

메모리에 필요한 정의 자산/정확한 초기화 owner 자체가 없으면 초기화/로드를 강제로 실행하지 않고 미확정으로 남긴다. 서로 다른 자료·파괴된 자산·손상/상한도 표시 근거가 아니다. 횟수형 실행 값이 미확정인 경로는 owner가 준비될 때까지 보류할 수 있다. 이 후보는 모든 세이브/조건에서 표시를 보장하거나 게임 전체 기회를 완전 탐지한다는 의미가 아니다.

네 타입의 native 검색과 고정 작업은 동기 호출이며 협력식 약5ms 예산으로 중간 취소할 수 없다. 실제 Unity 검색 시간·최초 지연·FPS/GC는 Windows에서 따로 확인해야 한다. 기존 ‘확인하는 중’ 문구를 흰색 마커 전체 로딩 완료 신호로 바꾸는 작업은 포함하지 않았다.

## 자동 검사와 측정

전체21개 검사 프로그램, 실제 고정 관리 DLL16개 연결, 294개 빌드 입력·118개 소스/리소스 SHA-256 불변, 설치 ZIP 두 경로·CRC·DLL/cfg 바이트 검증을 통과했다. 기존 생산 빌드 경고7개·오류0개다. git diff --check도 통과했다.

- MapGuideRouteIntegrationTests 3688개: owner 없는 지도 직행 복구·현재 불가 제거·reset 가상 값/실제 값 분리·횟수 보류·풀 조회·중복 정의/목록 충돌·5초 재시도·늦은 준비·현재 owner 우선·손상 우회 차단·파괴된 Unity owner·없는 풀 자료의 반복 초기화 방지를 포함한다.
- InformationRuntimeTests 5931개: 실제 DLL의 자산 조회 타입·설치/문맥 정리 연결·읽기 전용 IL을 확인한다. ChoiceStory7,719개·MapRoute6,884개·UI5,454개·기존 패드262,831개와 #5 이벤트 선택/예약·로그·주사위·상점·분기 회귀를 포함한다.
- 실제 Unity/Harmony 적용·Windows·물리 입력 실행으로 확대하지 않는다.

owner 없는 합성 지도/대화1개를 준비한 뒤500회 조회했다. 배지는500회 모두1개, 초기 native 자산 경계 검색3회, warm 검색0회·Lua 읽기0회·게임 실행/State 쓰기0회였다. p95 0.016125ms·최대0.087208ms·할당9000 B/회다. 스레드 할당은 .NET8.0.32/macOS arm64 관리 경계에서 측정했으며 native 검색은 명시적 fixture라 실제 Unity 검색 비용을 재현하지 않는다.

관리자가 있는 기존8/32/64장소 표본도 비교했다. 모든 warm 표본에서 전체 배지와 Lua 추가 읽기0을 유지했다. 미포커스 조회 p95는약0.074–1.824ms, 포커스+배경은약0.106–1.685ms였다. alpha.29의 원래 표본과 같은 정도의 범위이며 새 실행의 JIT/순서/동시 빌드 영향을 포함하므로 실게임 성능 동등성을 주장하지 않는다. 갱신 간격은0.25초로 유지한다.

재현 코드/원자료는 artifacts/issue-6/cold-map-load/benchmark.py·benchmark.cs·benchmark.json·warm-map-benchmark.json·full-build.log에 남긴다. 일반 warm 측정 도구는 tools/benchmark-guidance-markers.py이며 새 helper의 명시적 native 경계 fixture를 포함하도록 보완했다.

## 검토 후보·설치

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `artifacts/issue-6/cold-map-load/candidate/DiceMaster.dll` | 914944 | `2b709a4f020690513bfe0701240b61afdc99c5f9f7e7c50d4844a415415584d6` |
| `artifacts/issue-6/cold-map-load/candidate/DiceMaster-source-candidate.zip` | 381325 | `d661bc4ea1e265fa2a1c4ddeba46f6731b3b48b576a63ec11354c1b679e706a1` |
| `기존 패키지 cfg (변경 없음)` | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

게임을 종료하고 현재 DLL/cfg를 백업한 뒤 새 로컬 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체한다. cfg를 통째로 덮어쓰지 않는다. 공개 alpha.29는 별도 기존 파일로 보존한다. 이 검토 후보를 고정 커밋으로 재빌드해 alpha.30 설치 ZIP과 대조한다. 게시 상태는 위 기록을 따른다.

새 프로세스에서 저장된 지도 직행, 동일 세이브의 스토리 진입 전/후, 초회 인사/후속 주제·완료/잠김/반복 대련, 자료 출처·최초 지연/프레임·기존 입력을 [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha30--재실행-후-저장-지도-직행-확인-대기)로 분리한다. 실제 Windows 확인과 PRD #6 완료/종료는 대기다.
