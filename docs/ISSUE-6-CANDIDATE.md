# 지도·1회성·분기·상점 정보 — 후보·검증 이력

## alpha.19 — 후속 선택지 해금 확인

<!-- alpha19-publication -->
2026-10-22 01:06:52 KST에 소스·주석 태그 [`0b984ff`](https://github.com/jungpaeng/dicemaster/commit/0b984ff9345b1feca3831c4e25c4c2fa43f6046e)의 [alpha.19](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.19)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·271개 입력·112개 소스/리소스·16개 고정 참조와 실제 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 60개 Release·61개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [설치·파일·검증 기록](ISSUE-6-ALPHA19-RELEASE.md).
<!-- /alpha19-publication -->


## alpha.18 — 툴팁 조건·배지 표시 개선

<!-- alpha18-publication -->
2026-10-22 00:14:28 KST에 소스·주석 태그 [`0d7d03d`](https://github.com/jungpaeng/dicemaster/commit/0d7d03d51b4e4cbcd7aadce2269e8570994ef3b3)의 [alpha.18](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.18)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·270개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·익명 공개 ZIP 바이트 대조를 통과했다. 기존 59개 Release·60개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [설치·검증·확인표](ISSUE-6-ALPHA18-RELEASE.md).
<!-- /alpha18-publication -->

### 선택지 툴팁의 현재 필수 조건

확정 거짓이 미확정으로 덮이는 툴팁과 인물 이름/판정 경로 불일치를 수정한다. 자동 툴팁에서 불가 대화를 제외하되 상세와 실제 선택·패드 입력은 유지한다. 앞선 배지 크기·게임 창 가림 변경을 포함한 후보이며 실제 Windows 확인은 대기다. [범위·검증](ISSUE-6-TOOLTIP-PREREQUISITES.md).

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

## alpha.15 — 지도·유학 메뉴 공통 표식 후보

<!-- alpha15-publication -->
2026-10-21 22:03:38 KST에 소스·주석 태그 [`48ac8e7`](https://github.com/jungpaeng/dicemaster/commit/48ac8e732271410242ee2ea2c0d810a9d27c4ae5)의 [alpha.15](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.15)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·261개 입력·107개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 56개 Release·57개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·지원 범위·확인표](ISSUE-6-ALPHA15-RELEASE.md).
<!-- /alpha15-publication -->

당문·유학 행동 메뉴의 붉은 점을 실제 대기 임무/증명 1회성과 현재 도달성으로 통일한다. 일반 대화의 조건 상태·경로 설명은 유지한다. 유학의 불확실한 선행 변경·실행 연결은 점으로 표시하지 않으며 전체 유학 이벤트 탐지 완료를 주장하지 않는다. [지원 범위·설정·검사·설치·Windows 확인](ISSUE-6-ALPHA15-RELEASE.md). 아래는 이전 후보 기록이다.

## alpha.14 — 장소·선택지 근처 표시 후보

<!-- alpha14-publication -->
2026-10-21 21:01:57 KST에 소스·주석 태그 [`cfe1cf7`](https://github.com/jungpaeng/dicemaster/commit/cfe1cf7bc6e52a0b075ba057a9b686dbc8f72468)의 [alpha.14](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.14)를 게시했다. 전체·고정 커밋 재빌드 21개 검사·259개 입력·106개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 55개 Release·56개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·파일·확인표](ISSUE-6-ALPHA14-RELEASE.md).
<!-- /alpha14-publication -->

현재 확인된 기회의 붉은 점과 포커스 옆 설명을 추가한다. 미선택 장소는 얕은 현재 대기 목록과 이미 준비된 1회성만 사용하며 전체 Lua 분석을 시작하지 않는다. 새 설정은 없고 cfg를 보존해 DLL만 업데이트한다. 게시 상태·설치 파일·검사·남은 실제 Windows 확인은 [alpha.14 기록](ISSUE-6-ALPHA14-RELEASE.md)을 따른다. 아래 alpha.13 이하는 당시 기록이다.

## alpha.13 성능 개선 후보

2026-10-21 18:06:04 KST에 소스·주석 태그 [`53d6f6a`](https://github.com/jungpaeng/dicemaster/commit/53d6f6a2165b3c57f786e1920ff2b114e2144719)의 [alpha.13](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.13)을 게시했다. 전체·고정 커밋 재빌드 21개 검사·254개 입력·104개 소스/리소스·15개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 54개 Release·55개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.** [설치·파일·비교 기록](ISSUE-6-ALPHA13-RELEASE.md).

자동 대기 임무 조회를 먼저 처리하고, 1회성 OFF 경로의 Lua 준비와 일반 이야기의 숨은 조건/상세 행 생성을 생략한다. 완성 구문 공유와 준비 양보·성능 표본을 추가한다. [후보·검사·설치·Windows 확인](ISSUE-6-ALPHA13-RELEASE.md). 새 Windows 확인 전에는 #6 완료로 처리하지 않는다.

**alpha.2 당시 공개 후보 기록:** [선택 장소 지도 안내·상점 사용 효과·공개 파일 검증·새 Windows 확인표](ISSUE-6-SELECTED-MAP-SHOP.md). 아래는 공개 alpha.1의 구현·파일·게시 당시 미실행 확인 기록으로 보존하며 새 수정 파일과 구분한다.

[사용자 안내](../README.md) · [패드 조작](GAMEPAD.md) · [구현·지원 범위](ISSUE-6-IMPLEMENTATION.md) · [실제 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [변경 이력](../CHANGELOG.md)

상태: **[prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.1) 게시·자동 검사·고정 소스 재빌드·익명 공개 ZIP 검증 완료 / 새 사용자 Windows 확인 미실행**. 2026-10-21 00:46:36 KST에 게시했으며 Latest 정식 v1.0.6과 이슈 #6 open을 유지한다. 공개 최신 정식은 [v1.0.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6)이다. 아래 최종 설치 파일·소스·해시는 검토 후보와 고정 소스 재빌드에서 일치했다. 이전 후보 파일·검사 수치·사용자 확인을 새 후보에 옮기지 않는다.

## 이번 변경

현재 지도 장소의 이야기·임무 후보와 진행 가능 수, 안전하게 증명한 1회성, 게임이 실제로 선택한 분기의 5초 알림·최신 20건 기록, 상점 선택 물품의 사용·학습 정보 조회를 추가한다. 게임 자동 화면은 이름·종류·상태만 제공하고 조건·효과는 기존 로그 도구의 **지도 안내 조회·분기 기록 조회·상점 효과 조회**에서 읽는다.

기존 Tab 또는 같은 패드 L3/R3 겹침으로 로그를 연다. 해제·중립 뒤 Y 도구에서 조회를 고르고 ↑↓·왼쪽 스틱으로 항목 이동, 오른쪽 스틱으로 상세, B/Esc로 도구 복귀, LB/RB로 기존 탭에 이동한다. 새 조회 선택은 게임 실행·예약·구매·사용·학습을 만들지 않는다. 기존 팝업·로그 네 탭·미션 예약·주사위와 단일 XInput 소유권·다중 슬롯·해제/중립을 유지한다.

## 설정과 의미

| `[DiceMaster]` 키 | 기본값 | OFF 정리 |
| --- | --- | --- |
| EnableMapGuide | true | 지도 안내 표시·조회 스냅샷 |
| EnableOneShotHints | true | 지도·선택지·이벤트 팝업 1회성 정보 |
| EnableBranchNotices | true | 분기 알림·20건 메모리 기록·진행 중 관찰 |
| EnableShopEffectInfo | true | 상점 요약·선택 물품 정보 스냅샷 |

도구의 개별 변경은 즉시 적용·저장하고 다른 개별 값·기존 단축키·배치·글자를 보존한다. `EnableOptionInfo=false` 시작은 네 저장값을 보존하면서 모두 비활성화한다. 상위 옵션은 기존 재시작 규칙을 따른다. 개별 설정 읽기 실패는 그 기능만 끈다. ON은 현재 유효 문맥부터 읽으며 이전 알림을 재생하지 않는다.

지도 후보 수는 인식한 관련 항목 수이며 실제 발생 보장이 아니다. `1회성`은 현재 활성 조건이 실행 뒤 꺼지는 것으로 증명한 항목이며 미독·첫 방문·유일 기회를 뜻하지 않는다. 미지원·동적·활성 조건 미연결은 확인 불가다. 현재 제공 게임의 선택지 버튼에는 원래 활성 조건 참조가 없어 선택지의 기본 판정은 확인 불가이며 interactable·미독만으로 추론하지 않는다. 양성 표시는 지도·실제 이벤트 팝업의 명시 활성 조건과 무조건 확정 상수 플래그 결과가 연결되는 범위에 적용한다. 상점 책/비급은 현재 알려진 조건·학습 소모·다음 단계의 정적 변화만 제공한다. 소모품 사용 효과 배열이 읽혀도 실제 사용 실행 경로가 미확인이면 적용 변화는 확인 불가다. 장비 착용·미래 강화·랜덤 최종값은 범위 밖이다.

분기 알림은 최신 1건 5초, 기록은 최신순 20건을 메모리에만 보관하며 저장·로드·장면 전환·분기 OFF에서 모두 지운다. 같은 분기의 별도 실행은 별도 기록이고 같은 실행 관찰의 중복만 제거한다. 재실행·ON 뒤 복원하지 않는다.

표시는 게임 RNG·판정/실행 횟수·예약·세이브·능력치·아이템을 변경하지 않는다. 현재 보이는 문맥만 정상 0.25초·실패 5초로 읽고 자동·상세 모두 숨김/OFF면 표시 조회는 0회다. 기존 캐시를 재사용해 정상 주기마다 Lua를 재분석하지 않는다. 데이터 대기·빈 대상·확인 불가·읽기 실패·같은 문맥 이전 값을 구분하고 대상 교체 때 오래된 자료를 버린다.

## 설치 파일과 자동 검사

| 최종 기록 항목 | 결과 |
| --- | --- |
| 후보 빌드 | 2026-10-21 00:34:15 KST · SDK 8.0.426 · net48 · 오류 0/기존 경고 7 |
| 소스·참조 | 빌드 입력 227개 · 소스/리소스 95개 · 고정 외부 참조 15개 전후 무변경 |
| 고정 소스 재빌드 | `949ebcc88792a9c05d2c96dfda97d1f77f84271a` · 2026-10-21 00:36:49 KST · 기존 관리 DLL 0개인 격리 소스에서 동일 18개 검사 PASS |
| 전체 자동 검사 | 18개 검사 프로그램 PASS · Python 후보 검증기 회귀 20개 PASS |
| 설정·패키지 | [DiceMaster] 네 키 기본 true · 정확히 DLL/cfg 두 파일 · CRC/내부 바이트 PASS |
| DiceMaster.dll | 717312바이트 · SHA-256 `02ba6280037c80b0dc467dfa7564347f2bdd9eb9321467aeb7d00d8a1766ebed` |
| DiceMaster.cfg | 12509바이트 · SHA-256 `7f77b7f23de885333f9ecdf5d402be506b759be391ac1bd37f2a76622b07702c` |
| 설치 ZIP | 303887바이트 · SHA-256 `dbbf9f7421f0e20dc96c269f7c4a34c4d63ef61a31b79108a876ff8ef48a998d` |
| 설치 ZIP | [공개 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.1/DiceMaster-v1.0.7-alpha.1.zip) · 로컬 `artifacts/releases/v1.0.7-alpha.1/`와 원래 작업 폴더 `artifacts/issue-6/v1.0.7-alpha.1/`에도 동일 파일 제공 |
| 영수증 | `artifacts/releases/v1.0.7-alpha.1/release-verification.json` · `reviewed-candidate-comparison.json` · `build-manifest.json` · `preview-verifier-tests.log` |
| 실제 Windows·Unity·물리 장치·비용/할당량 | **미실행·사용자 확인 대기** |

### 이번 검사 프로그램 결과

| 검사 | 실제 결과 |
| --- | --- |
| LogRefreshTests | PASS: 23 assertions against installed DLL method bodies and boundary fixtures. |
| LiveLogEngineTests | PASS: 16 refresh engine scenarios, 70 assertions. |
| LifecycleTests | Lifecycle assertions passed: 4023 |
| LiveLogIntegrationTests | PASS: 485 integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures. |
| RealGameApiTests | METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed. / PASS: 4265 managed behavior assertions and 1278 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed. |
| GamepadTests | PASS: 262831 assertions. |
| SourceBehaviorTests | PASS: 3541 source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed. |
| PreviewIntegrationTests | PASS: 2101 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiModelTests | PASS: 444 log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets). |
| LogUiWiringTests | PASS: 113 actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5821 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| LogOpenerIntegrationTests | PASS: LogOpenerIntegrationTests: 1839 assertions across 43 isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation). |
| EventSelectionRuntimeTests | PASS: event execution runtime 1393 assertions across 237 scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries) |
| EventPopupTests | PASS: EventPopupTests: 2116 actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering. |
| MapInformationTests | PASS: 85 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| ShopInformationTests | PASS: ShopInformationTests 459 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 45 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |

준비 단계에서 #5 기준선 자동 검사와 API 메타데이터/IL을 조사한 결과는 새 #6 후보의 검증과 구분한다. 새 기능의 실제 소스·관리 대역·실제 제공 MoonSharp/참조 메타데이터를 사용한 자동 검사도 Windows 게임·물리 장치 실행을 대신하지 않는다. 최종 자동 영수증에는 실행한 검사·대역·미실행 경계를 남긴다.

## 설치·업데이트·복구

Windows용 활협전과 BepInEx를 포함한 활협전용 모드 베이스가 필요하다. 위 표의 고정 소스 재빌드 설치 후보에 다음 절차를 사용한다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. **업데이트는 후보 ZIP의 `plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg·단축키·배치·글자를 보존한다. 최초 설치는 ZIP의 config/plugins를 기존 BepInEx 아래에 넣는다. 새 네 키는 실행 시 기존 cfg에 추가된다.
3. 설치 DLL 해시를 위 후보 값과 대조하고 [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)의 환경·관찰을 기록한다. 공개 GitHub Source code 압축 파일은 설치용이 아니다.
4. 문제가 있으면 게임을 종료하고 백업 DLL/필요 cfg 또는 확인된 정식 v1.0.6으로 복구한다. v1.0.6에는 이번 네 정보 기능이 없으며 DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

ZIP은 DLL/cfg 두 파일만 포함하고 게임 DLL·게임 소스를 배포하지 않는다. 새 영구 데이터는 기존 cfg의 네 bool뿐이다. 사용자 환경·실행 경로와 새 후보 해시를 기록하고 문제를 제보하되 전체 세이브·게임 소스를 기본으로 보내지 않는다.

## 다음 실제 확인

새 후보에서 네 기능·패드 도구/상세 이동·자동 표시의 입력 간섭 없음·분기 시간/20건/초기화·상점 연속 선택/설명 보존·미지원 구분·큰 한글/화면/스크롤을 확인해야 한다. 저장·로드·장면·OFF·실패/이전 값·숨김 조회 중단·실제 Windows 비용/할당량과 기존 #5/예약/주사위 회귀도 [확인표](ISSUE-6-WINDOWS-VALIDATION.md)에 남긴다. **이 확인이 남아 있는 동안 #6 전체 완료나 이슈 종료로 처리하지 않는다.**

## 고정 소스와 검토 후보 동일성

검토 후보와 고정 소스 재빌드의 DLL/cfg/ZIP 바이트·SHA-256, 빌드 입력 227개, 소스/리소스 95개, 고정 참조 15개, 18개 검사 프로그램 결과가 모두 일치했다. ZIP CRC·정확히 두 항목·내부 바이트와 cfg 네 기본 true도 다시 확인했다. 후보 검증기 Python 회귀는 20개 PASS다. 재빌드에는 기존 관리 모드 DLL·Git 디렉터리·게임 참조를 복사하지 않았다. 외부 참조는 무변경이다.

빌드 소스는 위 커밋이며 이후 결과 보충 문서 커밋은 생산·검사·빌드 입력을 바꾸지 않는다. 게시 전 영수증의 `published:false`는 당시 상태로 보존하고 아래 별도 게시·공개 검증 영수증으로 후속 결과를 기록한다. 실제 Windows 실행·이슈 종료는 수행하지 않았다.

## 공개 prerelease 게시와 검증

사용자가 “커밋하고 prerelease를 진행해”라고 명시한 뒤 **2026-10-21 00:46:36 KST에 [v1.0.7-alpha.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.1)을 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.1/DiceMaster-v1.0.7-alpha.1.zip).

- 빌드 소스·주석 태그는 `949ebcc88792a9c05d2c96dfda97d1f77f84271a`, 태그 객체는 `ef1a3331f89b8db2802c419c8cfe591e022bef69`다. 구현·검증 보충 문서 커밋은 `codex/prd6-information`에 올렸으며 원격 main `375c39715c91d9a2bcdc629dbfe8a88ac0ff127a`과 다른 세션 작업을 보존했다. 태그와 설치 파일은 이후 문서 보충 커밋으로 바꾸지 않는다.
- Release `418579272`·asset `659749168`, `draft=false`·`prerelease=true`·`make_latest=false`다. 기존 42개 Release·43개 태그·자산을 보존하고 Latest 정식 v1.0.6을 유지했다. 이슈 #2 closed·#4 closed·#5 open·#6 open을 유지했다.
- 인증 없는 공개 API·페이지·ZIP 다운로드를 확인했다. ZIP 303,887바이트·SHA-256 `dbbf9f7421f0e20dc96c269f7c4a34c4d63ef61a31b79108a876ff8ef48a998d`, CRC·정확히 두 파일·내부 DLL/cfg 바이트·해시가 검토 후보 및 고정 소스 재빌드와 일치했다. 공개 API에서 주석 태그가 검증 소스를 가리킴도 확인했다.
- 로컬 영수증: `artifacts/releases/v1.0.7-alpha.1/publication-preflight/`, `github-publication.json`, `github-upload.json`, `anonymous-verification.json`, `tag-verification.json`. 이 경로는 Git 제외 자료이며 공개 Release에서는 이 문서로 연결한다.

실제 Windows·Unity·물리 장치·비용/할당량 확인은 미실행이다. [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md)를 전달하며 #6 전체 완료·이슈 종료로 처리하지 않는다.
