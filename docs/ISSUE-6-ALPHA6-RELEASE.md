# 기본 휴식 제외·인물 대화 안내 — v1.0.7-alpha.6 prerelease

원래 작업 폴더의 `main`에서 기본 활동 안내를 줄이고 인물 대화의 원본 근거를 구분했다. **2026-10-21 05:19:19 KST에 [alpha.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.6)를 게시하고 익명 공개 ZIP을 대조했다.** 전체·고정 소스 재빌드의 21개 검사 결과와 DLL/cfg/ZIP 바이트가 같다. 최신 정식은 v1.0.6이며 새 DLL의 실제 Windows·물리 입력·가독성·성능 확인과 PRD #6 전체 완료는 대기다.

[사용자 안내](../README.md) · [구현 계약](ISSUE-6-IMPLEMENTATION.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [패드](GAMEPAD.md) · [alpha.5 당시 기록](ISSUE-6-ALPHA5-RELEASE.md)

## 원인과 바뀐 분류

alpha.5는 선택 뒤 지원된 `say`와 리터럴 대사 참조가 있으면 이야기 항목으로 수집했다. 남제자방의 휴식 뒤 안내·독백도 이 형식이라 기본 반복 활동에 `진입 조건 충족 (현재)`이 붙었다.

새 후보는 같은 지원 경로에서 `setcharacter(characters.Get("인물 ID"))`로 명시된 플레이어 이외 화자와 실제 대사 호출이 연결된 경우만 인물 대화로 안내한다. 플레이어·내레이션·동적/미확정 화자는 양성 근거가 아니다. `characters.Focus`는 초상 연출이며 화자를 확정하지 않는다. `talk` 아이콘/선택 종류나 `D_` 대사 키도 플레이어 독백에 쓰이므로 단독 분류 근거로 삼지 않는다.

- 남제자방의 기본 휴식과 그 뒤 안내·독백만 있는 경로는 이야기 자동 안내에서 빠진다.
- 사사형 선택을 통한 실제 인물 발화와 필요한 선택 경로는 남긴다. 표시 인물은 해당 경로의 **첫 확인 화자 대표**이며 전체 등장인물 목록을 뜻하지 않는다.
- 휴식 경로 자체를 탐색에서 제거하지 않는다. 그 뒤 특수 인물 대화나 기존 엄격 증명에 통과한 원본 1회성이 있으면 안내 대상으로 남긴다. 앞선 나레이션 때문에 나중의 인물 발화를 버리지 않는다.
- 기존 임무와 원본 활성 조건에 연결된 1회성 증명·표식을 유지한다. 인물 대화라는 사실을 1회성 근거로 사용하지 않는다.

화자 설정의 재정의·별칭·동적 값·미지원 호출이나 불명 분기를 지나면 화자 근거를 폐기하거나 확인 불가로 남긴다. 다른 조건 경로의 화자를 가져와 분류하지 않는다. 지도·선택 안내는 근거를 확인한 부분집합이며 미분류·표시 없음은 반복 가능이나 전체 기회 없음의 증명이 아니다. 첫 선택 조건의 현재값과 이후 조건·플래그 변경도 구분하며 발견한 경로의 실제 발생을 보장하지 않는다.

## 캐시·설정·기존 조작

기존 경로의 준비·깊이·개수·복사 상한과 정의 캐시를 유지한다. 정상 0.25초·실패 5초, 숨김/OFF 표시용 조회 중단과 정상 주기 Lua 재분석 없음 계약을 따른다. 자동 화면에 전체 효과 분석을 추가하지 않으며 GUI는 게시된 문자열을 읽는다. 합성 검사 결과를 실제 Windows FPS·프레임 시간·할당량 측정으로 취급하지 않는다.

`EnableMapGuide`가 지도·현재 선택의 안내를 함께 제어하며 새 설정 키·기본값 변경은 없다. 상위 `EnableOptionInfo`와 네 개별 설정의 기존 계약을 유지한다. 기존 패드 선택·키보드·마우스·터치, 로그 상세와 입력 소유권·해제/중립 보호를 사용한다. 조회는 선택·예약·대사·조건·RNG·세이브·능력치·아이템을 변경하지 않는다. 기존 이벤트/조건 팝업·임무 예약·상점·분기 기록·주사위도 보존한다.

## 이번 자동 검사와 후보 파일

영수증은 `artifacts/issue-6/dialogue-filter-candidate/full-final/build-manifest.json`이다. 생성 시각은 **2026-10-20 20:14:39 UTC / 2026-10-21 05:14:39 KST**, SDK는 8.0.426이다. 매니페스트의 생산 소스·리소스는 **103개**, 전체 빌드 입력은 **249개**, 읽기 전용 참조 파일은 **15개**다. 아래는 이번 후보의 21개 검사 결과이며 실제 Windows·Unity·물리 입력 시험이 아니다.

| 검사 프로그램 | 매니페스트 결과 |
| --- | --- |
| LogRefreshTests | PASS · 23개 DLL/경계 단언 |
| LiveLogEngineTests | PASS · 16개 시나리오·70개 단언 |
| LifecycleTests | PASS · 4,023개 단언 |
| LiveLogIntegrationTests | PASS · 485개 실제 DLL 통합 단언 |
| RealGameApiTests | PASS · 관리 동작 4,265개·메타데이터 계약 1,280개 |
| GamepadTests | PASS · 262,831개 단언 |
| SourceBehaviorTests | PASS · 3,541개 소스/실제 모드 IL 동작 단언 |
| PreviewIntegrationTests | PASS · 2,152개 실제 DLL 미리보기 통합 단언 |
| LogUiModelTests | PASS · 444개 로그 모델 단언 |
| LogUiWiringTests | PASS · 119개 실제 후보 연결·패키지 경계 단언 |
| LogUiKeyboardTests | PASS · 5,305개 검사 |
| LogOpenerIntegrationTests | PASS · 43개 격리 시나리오·1,839개 단언 |
| EventSelectionRuntimeTests | PASS · 237개 시나리오·1,393개 단언 |
| EventPopupTests | PASS · 2,124개 팝업 입력/배치 경계 검사 |
| MapInformationTests | PASS · 195개 지도/1회성 경계 단언 |
| MapRouteTests | PASS · 5,303개 경로/구문/현재값 조회 단언 |
| MapGuideRouteIntegrationTests | PASS · 345개 실제 provider/경로/1회성 단언 |
| ShopInformationTests | PASS · 967개 상점/갱신/메타데이터 단언 |
| BranchHistoryTests | PASS · 412개 실제 소스/후보 DLL/원본 API 검사 |
| InformationRuntimeTests | PASS · 2,489개 일정/설정/읽기 전용/DLL 연결 단언 |
| ChoiceStoryTests | PASS · 684개 실제 선택 UI 소스/라벨/경로/현재값/갱신 단언 |

실제 DLL 메서드·원본 API 서명·제공 MoonSharp와 명시적인 Unity/게임/입력/자료 대역의 검사를 구분한다. 메타데이터 연결 확인을 원래 게임·Harmony/native·Windows 화면 실행으로 바꾸지 않는다.

| 후보 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 790016 | `cbd19644255a43a928b10d3f6f066595af7b3366fce54a5dc46b2ba4c1075b88` |
| DiceMaster.cfg | 12912 | `442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a` |
| DiceMaster-v1.0.7-alpha.6.zip | 336123 | `c875801a4167912207dc772a350ac02f7e8166961851445f98c841acb846316a` |

ZIP은 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 두 파일로 구성하며 CRC·내부 파일·바이트·해시가 위 값과 일치한다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.6/DiceMaster-v1.0.7-alpha.6.zip)은 소스·태그 [`08b9fb6`](https://github.com/jungpaeng/dicemaster/commit/08b9fb623c43cd62b42fcae8a24fb913f372027c)의 고정 재빌드와 같다.

## 고정 커밋 재빌드와 게시

기존 관리 모드 DLL 0개인 고정 소스에서 같은 SDK·249개 입력·103개 소스/리소스·15개 참조로 21개 검사를 다시 실행했다. 전체 검사 성공 기록과 DLL/cfg/ZIP 바이트가 로컬 후보와 같다. 검증기 Python 회귀 20개와 독립 관리 대역의 화자·별칭·분기·warm cache 회귀도 통과했다. Lua/Windows/native 게임 실행이나 실제 FPS 검증은 아니다.

Release `418832910`·asset `660380127`, `draft=false`·`prerelease=true`·`make_latest=false`다. 기존 47개 Release·48개 태그·자산과 Latest 정식 v1.0.6을 보존했다. main과 주석 태그를 atomic push했고 이슈 #2/#4 closed·#5/#6 open을 유지했다. 원래 작업 트리 하나와 main만 사용하며 `.agents/`·`skills-lock.json`은 보존했다. 게시 보충 문서는 별도 main 커밋이며 빌드 입력·태그·설치 파일을 바꾸지 않는다.

## 설치와 실제 Windows 확인

[게시한 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.6/DiceMaster-v1.0.7-alpha.6.zip)은 위 파일과 일치한다. 게임 종료 후 기존 DLL/cfg를 백업하고 **DLL만 교체해 기존 cfg를 보존**한다. 문제가 있으면 종료 후 백업 DLL과 필요한 cfg 또는 정식 v1.0.6으로 복구한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

- [ ] **미실행** — 남제자방 기본 휴식과 일반 안내·독백에 불필요한 이야기/진입 조건 자동 표시가 사라지고 원래 선택·게임 진행은 유지된다. `talk`/`D_`를 쓰는 플레이어 독백도 인물 대화로 잘못 표시하지 않는다.
- [ ] **미실행** — 사사형 인물 대화와 필요한 선택 경로가 유지된다. 표시 화자 대표·현재 첫 조건과 실제 이후 발생 보장을 구분하고 긴 이름/경로가 읽힌다.
- [ ] **미실행** — 휴식 뒤 특수 인물 대화·증명된 원본 1회성을 확인한다. 앞선 나레이션 뒤 인물 발화도 남고 기존 임무·1회성 표식이 유지된다. 해당 상황이 없으면 사유와 함께 미실행으로 남긴다.
- [ ] **미실행** — Xbox/XInput으로 선택 → L3/R3 겹침 → 해제·중립 → Y 도구 → 지도 안내 상세 → B 복귀를 확인한다. 키보드·마우스·터치, OFF/ON·메뉴 닫기·장소/선택 전환과 기존 팝업·예약·주사위에서 이전 표시나 입력 재사용이 없다.
- [ ] **미실행** — 같은 지도/긴 후속 경로에서 Unity 프레임·할당량과 정상/실패/숨김 조회를 관찰한다. 정상 주기 재분석이 없고 조회가 RNG·조건 횟수·세이브·능력치·아이템을 바꾸지 않는지 확인한다. 환경·설치 DLL 해시와 관찰 방법/결과를 [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)에 기록한다.

자동 검사·커밋 고정 재빌드·게시·사용자 Windows 확인은 별도 단계다. 새 후보의 사용자 확인 전에는 전체 완료나 이슈 종료로 처리하지 않는다.
