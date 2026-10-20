# 현재 진입 조건의 색 구분 — v1.0.7-alpha.7 prerelease

원래 작업 폴더의 `main`에서 지도·선택 안내의 현재 조건 충족 문구를 연녹색으로 구분했다. **2026-10-21 05:27:56 KST에 [alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.7)을 게시하고 익명 공개 ZIP을 대조했다.** 전체·고정 소스 재빌드의 21개 검사 결과와 DLL/cfg/ZIP 바이트가 같다. 최신 정식은 v1.0.6이며 새 DLL의 실제 Windows·물리 입력·가독성·성능 확인과 PRD #6 전체 완료는 대기다.

[사용자 안내](../README.md) · [기존 분류·검사 범위](ISSUE-6-ALPHA6-RELEASE.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md) · [패드](GAMEPAD.md)

## 표시 범위와 의미

지도에서 정확한 양성 상태 `진입 조건 충족 (현재)`, 현재 선택 화면에서 `현재 이야기 진입 조건 충족`인 **상태 문구 부분만** 연녹색(`#BFFFCC`)으로 표시한다. 이름·제목은 중립색이며 같은 패널에 섞인 미충족·불명·경로 후보도 중립색을 유지한다. 갱신 실패로 남긴 이전 값에는 초록 표시를 붙이지 않는다. 색을 구분하기 어려워도 기존 상태 문구로 읽을 수 있다.

초록은 첫 선택 조건을 현재 읽은 값이다. 진입·대화·플래그 변경 뒤의 이야기 발생이나 이후 선택 활성화를 보장하지 않는다. alpha.6의 명시된 플레이어 이외 화자에 근거한 인물 대화 필터, 기존 엄격 1회성·임무와 현재/미래 조건 구분은 유지한다.

표시 문자열이 바뀔 때 색 태그와 원문 꺾쇠 이스케이프를 준비하고, rich text 모드를 포함한 배치 캐시를 사용한다. 화면 렌더링에서 조건 조회나 문자열 분석을 새로 반복하지 않는다. 기존 설정 키·기본값·입력 소유권·해제/중립, 읽기 전용·정상 0.25초/실패 5초·숨김/OFF 조회 중단·정상 주기 Lua 재분석 없음 계약을 유지한다.

## 자동 검사·후보 식별 상태

이번 전체 소스 후보는 **2026-10-21 05:25:07 KST**에 SDK 8.0.426로 생성했다. 전체 빌드 입력 249개·생산 소스/리소스 103개·읽기 전용 참조 15개의 시작/종료 해시를 대조했다. 이번 21개 프로그램이 모두 통과했으며 이전 후보의 결과를 승계하지 않는다.

검사: `LogRefreshTests`, `LiveLogEngineTests`, `LifecycleTests`, `LiveLogIntegrationTests`, `RealGameApiTests`, `GamepadTests`, `SourceBehaviorTests`, `PreviewIntegrationTests`, `LogUiModelTests`, `LogUiWiringTests`, `LogUiKeyboardTests`, `LogOpenerIntegrationTests`, `EventSelectionRuntimeTests`, `EventPopupTests`, `MapInformationTests`, `MapRouteTests`, `MapGuideRouteIntegrationTests`, `ShopInformationTests`, `BranchHistoryTests`, `InformationRuntimeTests`, `ChoiceStoryTests`.

렌더러 회귀는 **5,309개** 검사다. 충족/미충족/확인 필요가 섞인 목록, 원문 꺾쇠 표시, 실패 이전 값의 초록 제거, 현재 선택의 초록 표시와 후보의 중립색, 작은 창/폰트 변경과 1,000회 반복 GUI의 측정 캐시를 확인했다. 새 Lua 조회나 게임 판정을 추가하지 않았으며 기존 입력 회귀도 통과했다. 영수증은 로컬 `artifacts/issue-6/entry-color-candidate/full-final/build-manifest.json`이다.

| 후보 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 790528 | `7ec09ad68a786de23616ef5e626cecf387f33f61ab57a1a3078514be502f570d` |
| DiceMaster.cfg | 12912 | `442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a` |
| DiceMaster-v1.0.7-alpha.7.zip | 336316 | `e9aa6039d47dcf93d1f4b9bd81081da46e5c5756996002ca69569c89be37e7c8` |

ZIP의 두 파일은 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll`이며 CRC·내부 바이트를 대조했다. 소스·주석 태그 [`4aa5d2c`](https://github.com/jungpaeng/dicemaster/commit/4aa5d2c524f63a21197668407f1f70c67a089ab1)를 관리 모드 DLL 0개인 격리 소스에서 같은 SDK·249개 입력·103개 소스/리소스·15개 참조로 재빌드해 21개 검사 성공 기록·DLL/cfg/ZIP 바이트를 대조했다. [게시 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.7/DiceMaster-v1.0.7-alpha.7.zip)도 같다. Release `418840582`·asset `660400796`, `draft=false`·`prerelease=true`·`make_latest=false`이며 기존 48개 Release·49개 태그·자산·Latest v1.0.6·이슈 상태를 보존했다. main과 주석 태그를 atomic push했다. 게시 보충 문서는 별도 main 커밋이며 빌드 입력·태그·설치 파일은 변경하지 않는다.

실제 DLL 연결·관리 대역 검사와 Windows·Unity 화면·물리 입력 확인은 별도다. 자동 배치·캐시 검사를 실제 Windows 가독성이나 FPS 측정으로 바꾸지 않는다.

## 설치와 실제 Windows 확인 — 대기

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.7/DiceMaster-v1.0.7-alpha.7.zip)을 사용한다. 게임 종료 후 DLL/cfg를 백업하고 **DLL만 교체해 기존 cfg를 보존**한다. 문제가 있으면 종료 후 백업 DLL과 필요한 cfg 또는 정식 v1.0.6으로 복구한다. DLL 복원은 이미 저장한 게임 진행을 되돌리지 않는다.

- [ ] **미실행** — 지도·현재 선택의 정확한 양성 상태 문구만 연녹색으로 보인다. 이름·제목과 미충족·불명·경로 후보가 함께 있을 때 각 상태를 구별할 수 있다.
- [ ] **미실행** — 현재 조건 변화·선택/장소 이동·갱신 실패·메뉴 닫기·OFF에서 이전 초록 표시가 남지 않는다. 기존 인물 대화·1회성·임무 분류도 유지된다.
- [ ] **미실행** — 긴 한국어·꺾쇠가 있는 이름·큰 글자·작은 창·해상도/배율 변경에서 색 태그가 드러나거나 문구가 잘리지 않는다. 로그 상세와 패드의 열기 → 해제·중립 → 조회 → 복귀가 유지된다.
- [ ] **미실행** — 동일 화면에서 표시 전/후 Unity 프레임·할당량과 정상/실패/숨김 조회를 관찰한다. 색 표시가 조회·입력을 늘리거나 게임 조건·RNG·세이브·능력치·아이템을 바꾸지 않는다.

설치 DLL 해시·환경·관찰 결과를 기록하기 전에는 새 후보의 Windows 통과나 #6 전체 완료로 처리하지 않는다.
