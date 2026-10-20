# 지도 이야기·선택 경로 — v1.0.7-alpha.5 후보

2026-10-21 사용자가 지도 첫 진입의 조건, 선택 뒤 연결되는 이야기와 필요한 선택 경로를 안내하는 후속 구현을 승인했다. 원래 작업 폴더의 `main`에서 구현하고 전체 소스의 21개 자동 검사와 실제 DLL 연결·설치 ZIP 검증을 통과했다. **고정 소스 재빌드·게시 결과는 후속 기록에서 확정한다.** 현재 공개 시험판은 [v1.0.7-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.4)이며 아래 새 후보의 사용자 Windows 확인은 별도다.

[사용자 안내](../README.md) · [PRD #6](https://github.com/jungpaeng/dicemaster/issues/6) · [구현 계약](ISSUE-6-IMPLEMENTATION.md) · [소스 빌드](SOURCE-BUILD.md) · [패드](GAMEPAD.md)

## 바뀌는 안내와 상태 의미

지도에 첫 선택으로 연결되는 이야기 이름과 현재 진입 조건을 더 표시한다. 관련 이야기는 현재 스크립트에서 확인한 대사 구간과 정적으로 연결된 후속 스크립트의 대사 구간이다. 단순 메뉴·상점·유틸리티 스크립트의 등록 자체를 새 이야기로 분류하지 않으며 기존 임무·Position 풀 결과와도 구분한다.

지도 문구는 `진입 조건 충족 (현재)`·`진입 조건 미충족 (현재)`·`선택 화면에서 확인`이다. 첫 선택 조건을 지금 읽은 값이며 진입 전후의 효과를 미리 실행하거나 이후 조건 충족을 보장한 결과가 아니다. 실제 클릭의 장소·행동·기존 주/보조 임무 우선순위도 반영한다. 미완성·읽기 실패·생략 경로가 있으면 알려진 음성만으로 미충족을 확정하지 않는다.

실제 선택 화면에서는 패드로 현재 고른 항목의 캐시된 연결 이야기와 필요한 선택 경로 한 개를 짧게 표시한다. 출처를 안전하게 구분한 완전한 연결은 추가 항목을 `외 N개`로 알린다. 준비·부분 해석·읽기 실패 때문에 연결을 확정하지 못했지만 원문과 일치하는 경로를 발견했다면 `경로 후보:`와 `연결 후보 · 이후 진행 확인`으로 보여주고 개수·현재 진행 확정 표시는 하지 않는다. 동일 원문이 서로 다른 선택 위치에 실제로 중복되면 현재 항목에 특정 경로를 붙이지 않고 `연결 선택지 확인 필요`로 남긴다. 미선택이면 관련 항목을 최대 세 개 보여준다. 짧은 경로에 담지 못한 선택 순서는 기존 로그 도구의 지도 안내 상세에서 읽는다. 지도·선택 패널은 중립 문자색과 명시적 상태를 사용해 전체 목록이 모두 진행 가능한 것처럼 보이지 않게 한다.

서로 다른 상황의 예로, 같은 장소라도 게임 대기 자료에 있는 임무가 현재 클릭의 우선 대상으로 확인되면 `진행 가능`으로 안내한다. 대기 등록을 확인하지 못했지만 현재 단계 조건이 충족된 후보는 `발생 조건 충족`이다. 후자는 즉시 발생이나 대기 등록을 보장하지 않는다. 앞선 주/보조 임무가 우선하면 다른 후보의 상태가 달라진다. 장소가 지정된 대기 주 임무는 일반 행동력 검사보다 먼저 처리되는 기존 게임 순서를 따른다.

선택 경로는 **어떤 항목을 고르면 이어지는 연결을 발견했는지**의 안내다. 현재 선택지가 나타나거나 그 이야기가 실행된다는 보장이 아니다. 실제 `room_001 → talk_brother4` 경로에도 메뉴 조건 외에 대화·wait/runwait·시각 처리·Switch와 플래그 변경이 선행한다. 해당 경계를 무해하다고 추정하지 않으며 이후 현재 상태를 확정할 수 없으면 확인 필요로 남긴다. 읽지 않은 경로·동적 대상·생략된 항목을 이야기 없음이나 반복 가능으로 바꾸지 않는다.

## 경로·선택 표시와 읽기 전용 범위

- 게임의 현재 장소·메뉴 선택을 기존 콜백에서 관찰한다. 패드 선택에도 따라가며 새 단축키·별도 확정 입력·자동 선택·호버 의존성을 추가하지 않는다.
- 선택 순서는 원래 스크립트·choose 위치·메뉴 번호와 원문 라벨을 분리해 구조적으로 보존한다. 같은 라벨이 여러 원본 위치에 연결되면 현재 항목의 출처를 추정하지 않고 확인 필요로 남긴다. 사람이 읽는 경로 문자열을 다시 파싱해 선택 근거로 삼지 않는다.
- 원문의 아이콘/번역 키 등 내부 표기를 사용자 제목으로 그대로 내보내지 않는다. 이름을 읽지 못한 경우와 아직 준비하지 않은 경우를 구분한다.
- 기존 후속 등록의 교체·취소, 비활성 선택지, 조건/미지원 분기, 순환·깊이·개수 상한을 유지한다. 별도 이야기 항목을 추가해도 현재 진행 가능·1회성·발생 보장의 근거는 강화하지 않는다.
- 정보 조회는 게임 조건·Lua·RNG·카운터·세이브·예약·능력치·아이템을 변경하지 않는다. 조회로 스토리를 재생하거나 선택·구매·사용·학습을 실행하지 않는다.

## 성능·설정·기존 기능

정상 0.25초·실패 5초, 숨김/OFF 표시용 조회 중단과 정상 주기 Lua 재분석 없음 계약을 유지한다. 새 이야기·선택 경로도 기존 정의 캐시와 준비/현재 조회 예산을 공유한다. 자동 화면에 긴 결과 분석·전체 조건 설명을 추가하지 않으며 상세에서 준비한 값을 재사용한다. 지도 이야기의 현재 조건은 조회당 대표 16개 묶음·32개 게이트 안에서 샘플링하고 자동 패널은 최대 세 항목으로 제한한다. 준비/부분 자료나 상한 뒤의 상태를 진행 가능으로 추정하지 않는다. 준비·읽기 실패·상한/부분 해석·현재 조건 미확정은 성공한 빈 결과와 구분한다. GUI는 게시된 문자열만 읽는다.

자동 표시는 이름·종류·현재 상태를 짧게 제공하고 긴 순서·조건·효과는 스크롤 상세에서 읽는다. 선택 대상·문맥/메뉴 교체·로드/장면 전환에서 다른 항목의 이전 내용을 폐기한다. 폰트/화면 크기가 달라져도 문구 중간을 잘라 상태나 상세 동선을 잃지 않도록 확인한다. 실제 Windows FPS·시간/할당량 개선이나 모든 환경의 지연 해소는 자동 검사만으로 보장하지 않는다.

기존 `[DiceMaster]` 네 bool `EnableMapGuide`, `EnableOneShotHints`, `EnableBranchNotices`, `EnableShopEffectInfo`는 기본 ON, 상위 `EnableOptionInfo`는 시작 시 적용한다. `EnableMapGuide`는 지도와 현재 선택 화면의 이야기·경로 안내를 함께 제어한다. 새 설정 키는 추가하지 않는다. 각 OFF는 해당 표시·수집·스냅샷을 정리하고 분기는 기록도 지운다. 설정 읽기 실패는 해당 기능만 비활성화하며 기존 cfg를 보존한다. 기존 이벤트/조건 선택·로그 네 탭·임무 예약·상점 요약·실제 분기 5초/20건·주사위·입력 소유권과 해제/중립 보호도 회귀 대상으로 유지한다.

## 자동 검사와 파일 식별

2026-10-21 04:47:41 KST에 SDK 8.0.426으로 전체 소스 후보를 만들었다. 생산 소스·리소스 103개, 전체 빌드 입력 248개, 읽기 전용 참조 15개의 시작/종료 해시가 같고 주 모드 컴파일은 오류 0개·기존 경고 7개다. 아래 21개 결과는 이번 새 DLL을 대상으로 실행한 값이며 실제 Windows·Unity·물리 입력 결과가 아니다.

| 검사 프로그램 | 이번 결과 |
| --- | --- |
| LogRefreshTests | 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | 4265 managed behavior assertions and 1280 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | 262831 assertions. |
| SourceBehaviorTests | 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | 2152 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | 119 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | LogUiKeyboardTests: 5305 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | EventPopupTests: 2124 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |
| MapInformationTests | 195 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| MapRouteTests | MapRouteTests 5250 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed. |
| MapGuideRouteIntegrationTests | MapGuideRouteIntegrationTests 277 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed. |
| ShopInformationTests | ShopInformationTests 967 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | InformationRuntimeTests: 2420 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |
| ChoiceStoryTests | ChoiceStoryTests 135 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution. |

후보 검증기 자체의 Python 회귀 20개도 통과했다. 실제 DLL의 Harmony 대상·설치된 게임 API·패드 현재 선택 getter·금지 실행 호출 부재를 확인한다. 실제 Lua 자료는 2,198개 SHA 검증 입력이며 직접 실행하지 않는다. 남제자방의 직접 대화·후속 선택과 부분 경로 후보, 이야기 없는 중복 메뉴·강제 GC 이후 fixture 씬 수명을 함께 확인한다.

큰 결과를 가진 미증명 이벤트 256개와 새 선택 연계 이야기의 자동 표시 대역에서 전체 효과 분석/전체 조건 포맷 호출 0을 확인했다. 기존에 허용한 작은 직선 결과의 1회성 증명 예산은 유지한다. 정상 .25초/실패 5초·숨김 조회 0·warm 정의 재분석 없음과 1,000회 GUI 재사용도 관리 대역에서 검증했다. 이를 실제 Windows FPS나 할당량으로 해석하지 않는다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 787968 | `9fddbe4653aaf058f02d280c7c5d3b3660bd8df318906e4480e5080499d84931` |
| DiceMaster.cfg | 12811 | `e7a598ea94d88910d19df5d235a3178b92e1c46959482b79e78e52d8e8db1388` |
| DiceMaster-v1.0.7-alpha.5.zip | 335315 | `4c2ae9fb946cdb6dd472c99c6c3afbe4ef1fbe4b1e27a78ead1f0921d199dbf7` |

ZIP은 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 정확히 두 파일이며 CRC와 새 DLL/커밋 대상 cfg의 내부 바이트를 확인했다. 게임 참조 DLL·원문 Lua를 포함하지 않는다.

후보 영수증은 `artifacts/issue-6/choice-story-candidate/full-final/`의 build-manifest.json·build.log다. 다음 단계에서 소스 커밋을 확정하고 `verify-preview-release.py --scope prd6`로 기존 관리 모드 DLL 0개인 스냅샷을 재빌드한다. 고정 재빌드·공개 파일 대조는 `artifacts/releases/v1.0.7-alpha.5/`에 별도 보존하며 결과 확정 전에는 게시·Windows 확인으로 표시하지 않는다.

## 설치와 실제 Windows 확인

alpha.5 설치 파일이 확정되면 게임 종료 후 현재 DLL·cfg를 백업하고 ZIP의 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 처음 설치하는 경우에는 BepInEx 아래에 config·plugins 경로를 넣는다. 문제 시 게임을 종료하고 백업 DLL과 필요한 cfg를 복원한다. DLL 복원으로 이미 진행·저장한 게임 상태가 되돌아가지는 않는다.

설치한 버전·DLL/ZIP SHA-256, 게임/Windows/BepInEx, Xbox/XInput/Steam Input 구성, 해상도·UI 배율·글자를 [공통 환경표](ISSUE-6-WINDOWS-VALIDATION.md#환경과-파일)에 기록한다. 아래는 새 후보의 미실행 확인 항목이다.

- [ ] 지도 이야기의 `진입 조건 충족 (현재)`·`진입 조건 미충족 (현재)`·`선택 화면에서 확인`과 게임이 이미 대기 중인 임무를 구분한다. 실제 클릭 우선순위·장소 상태·행동 제한이 반영되고 첫 조건의 현재값을 이후 발생 확정으로 오해하지 않는다.
- [ ] 선택 화면에서 패드로 항목을 바꾸면 해당 항목 뒤의 이야기 정보만 바뀐다. 마우스 호버 없이 볼 수 있고 항목 교체·닫기·다른 메뉴에서 이전 정보가 붙지 않는다.
- [ ] `room_001` 계열처럼 후속 대화·추가 선택이 있는 장소에서 필요한 선택 순서를 읽는다. 조건부 비활성·미지원·상한·동적 경로가 실제 진행 가능으로 잘못 표시되지 않는다.
- [ ] 현재 스크립트의 대사와 정적 후속에서 확인한 대사, 기존 풀 결과가 구분된다. 단순 메뉴·상점·유틸리티는 이야기로 늘리지 않고, 같은 라벨의 다른 원본 위치와 취소/교체된 등록을 잘못 연결하지 않는다.
- [ ] 긴 한국어·여러 선택 단계·큰 글자·작은 화면에서도 상태와 상세 동선을 읽는다. 로그의 항목 이동·오른쪽 스틱 상세 스크롤·B 복귀가 가능하다.
- [ ] 최초 지도/메뉴 선택·연속 이동·재방문·상세 열기/닫기에서 지연과 할당 부담을 관찰한다. 남는 렉은 장소·상황·설치 해시와 함께 기록한다.
- [ ] 기능 OFF/ON·상위 설정·로드/장면 전환·실패 재시도 뒤 이전 표시가 남지 않는다. 일반 선택지의 미확인 배지 도배가 재발하지 않는다.
- [ ] A/B 유지·해제/중립·로그/팝업 전환·패드 재연결 뒤 조회 입력이 게임 선택·구매에 재사용되지 않는다. 키보드·마우스·터치도 유지된다.
- [ ] 기존 이벤트·조건 선택, 로그 네 탭·예약·상점 효과·실제 분기·주사위가 동작한다. 정보 조회 전후 RNG·조건 횟수·예약·세이브·능력치·아이템이 바뀌지 않는다.

새 후보의 실제 Windows 확인과 PRD #6 전체 수용은 대기다. 자동 검사·커밋·prerelease 게시만으로 이슈를 완료하거나 전체 패드 지원 완료로 처리하지 않는다.
