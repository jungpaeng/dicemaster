# PRD #6 후속 — v1.0.7-alpha.2 선택 장소 지도 안내와 상점 사용 효과

[사용자 안내](../README.md) · [공개 alpha.1 기록](ISSUE-6-CANDIDATE.md) · [구현 기록](ISSUE-6-IMPLEMENTATION.md) · [패드](GAMEPAD.md)

상태: **[v1.0.7-alpha.2 prerelease 공개](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.2)·고정 소스 재빌드·전체 자동 검사·익명 공개 ZIP 검증 완료 / 새 Windows 확인 대기**. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.2/DiceMaster-v1.0.7-alpha.2.zip) · 빌드 소스/태그 `4bbdffb15c8fd134ea06749e79cd5d75c929fb53`. 공개 alpha.1의 파일·태그·게시 기록을 보존하며 이번 새 파일과 구분한다.

## 사용자 관찰과 반영한 결정

2026-10-21 사용자는 지도 오버레이가 무작위처럼 흩어지고 그 화면에서 심한 렉이 발생하며, 상점의 ‘학습 효과 확인·미충족·확인 불가’만으로는 효과를 알기 어렵다고 알렸다. 이어 **현재 선택한 지도 항목만 표시하는 방식**을 제안하고 논의한 내용을 반영하도록 요청했다. 구체적인 설치 DLL 해시·Windows 환경·프레임 측정은 추가 제공되지 않았다.

소스에서 모든 장소의 라벨을 매 GUI 호출마다 화면 전체의 장애물 경계 조합으로 재배치하는 경로를 확인했다. 데이터의 0.25초 갱신 제한과 별개인 이 배치 탐색이 렉의 유력한 원인이며, 실제 Windows 프로파일링으로 원인을 확정한 기록은 없다.

## 지도: 선택한 장소 하나만

- 게임의 기존 PositionController.OnPositionEnter/OnPositionExit를 관찰한다. 제공 게임 DLL에서 OnSelect가 OnPositionEnter를 호출함도 확인했다. 새 입력·가상 마우스·장소 실행은 추가하지 않는다.
- 자동 표시와 로그의 지도 안내 조회 모두 현재 선택한 살아 있는 장소 하나만 읽는다. 선택하지 않은 장소는 후보 조회 대상으로 만들지 않고 정상 주기마다 장소 목록을 순회하지 않는다. 장소·임무 정의의 기존 캐시는 재사용한다.
- 지도 안내와 1회성 지도 표식은 선택한 장소의 이름을 포함한 **좌측 상단 고정 패널 하나**에 표시한다. 다른 장소를 선택하면 교체하고 선택 해제·숨김·장면 종료에는 숨긴다. 다른 장소의 1회성은 그 장소를 선택했을 때 확인한다.
- 기존 화면 전체 겹침 회피 탐색·카메라 투영·모든 장소의 UI 장애물 수집을 제거했다. 긴 자동 텍스트는 180자로 제한하고 패널 높이는 화면 안으로 제한한다. 전체 이름·조건·효과는 기존 스크롤 가능한 로그에서 읽는다.
- 표시 문자열·글자 크기·화면 크기·로그 가림 영역이 바뀔 때만 높이·배치를 계산한다. 반복 GUI 호출은 캐시한 GUIContent·사각형을 사용한다. 분기 알림은 기존 우측 상단 위치를 기본으로 유지하며 로그와 겹치면 최대 네 고정 모서리만 검사한다.
- 선택 세대가 달라지면 이전 조회 콜백과 오버레이를 거부한다. A→B→A 전환도 과거 A 표시를 재사용하지 않는다. 첫 조회 대기와 같은 대상의 갱신 실패/이전 값도 표시한다.
- 네 설정 키와 기본 true는 유지한다. 두 지도 기능 OFF는 자료를 비우고, 약한 참조의 게임 선택만 유지해 ON 시 현재 선택부터 다시 읽는다. 정상 0.25초·실패 5초·숨김/OFF 조회 0과 정상 주기 Lua 재분석 없음 계약을 유지한다.

## 조활시스템과의 안내 범위 차이

현재 지도 안내는 선택한 장소에 직접 연결된 이야기·이벤트와 현재 단계의 주/부 임무를 읽는다. 조활시스템의 지도 전체·장소 무관 임무 묶음과 같은 범위를 제공하지 않으며, 같은 장소의 후보 수가 일치한다고 보장하지 않는다.

현재 1회성은 직접 연결된 이벤트의 명시 활성 조건과 결과를 보수적으로 판별한다. 조활시스템처럼 **선택지 → 후속 메뉴/스크립트 → 도달 이벤트**를 따라가 지금 발생 가능한 1회성 목록을 안내하는 경로는 구현되지 않았다. 일반 선택지의 기본 ‘확인 불가’는 이 구현 범위의 한계다. 1회성 표식과 현재 진행 가능 여부는 별도 정보다. 조활시스템과 같은 수준의 1회성 탐색을 완료했다고 설명하지 않는다.

## 상점: 실제 재고 종류와 사용 경로 연결

제공 게임의 ShopPanel.Init은 Books·Miscs·Specials를 채운다. alpha.1의 Consumables/Book 처리만으로는 일반·특수 판매 물품의 사용 효과를 읽을 수 없었다.

일반 물품 Misc와 특수 물품 SpecialItem은 게임의 음식 사용 메뉴 food_menu_001에서 해당 종류·ID의 명시적인 RemoveMisc/RemoveSpecial 호출이 있는 선택지와 연결한다. 기존 정의 파서·조건/효과 설명기를 재사용하며 현재 게임 선택지를 변경하거나 스크립트를 실행하지 않는다. 종류와 ID를 함께 비교하고 뒤따르는 스크립트의 제거 호출만으로는 사용 경로를 연결하지 않는다. 여러 사용 선택지·조건부·난수·미지원 효과는 각 설명의 불확실성을 유지한다.

메뉴 분석·물품별 연결 색인은 문맥당 한 번 준비하고 정상 갱신마다 재로드·재분석하지 않는다. 실제 자료의 조건·효과 설명은 선택한 물품에 대해서만 읽는다. 닫기·OFF·로드/장면 정리에서 연결 캐시를 비운다. 사용 경로 미연결은 ‘효과 없음’이나 ‘사용 불가’를 뜻하지 않는다.

| 상태 예 | 의미 |
| --- | --- |
| 학습 조건 충족 / 학습 조건 미충족 | 현재 알려진 책·비급 학습 조건의 판정. 실제 학습 실행이나 학습점 확보를 보장하지 않음 |
| 학습 조건 일부 미지원 / 학습 효과 일부 미지원 | 어느 자료를 해석하지 못했는지 상세에서 확인 |
| 사용 효과 자료 확인 / 사용 효과 일부 확인 | 해당 물품의 사용 메뉴 효과 자료를 연결함. 실제 사용 가능 여부·적용량 확정과 구분 |
| 사용 경로 미연결 / 사용 경로 미확인 | 이 물품의 실행 경로 연결 근거가 없음. 효과가 없다는 뜻이 아님 |
| 효과 해석 미지원 / 효과 조회 미지원 | 지원하지 않는 효과 자료 또는 물품 종류 |

**자동 표시는 이름·종류·상태만, 구체적인 조건·효과는 기존 로그 도구에서 조회하는 규칙을 유지했다.** 상점 설명에 수치까지 바로 보여주는 조활시스템의 UI와 동일하다고 설명하지 않는다. 책은 기존 현재 학습 조건·소모·다음 단계 정적 변화 범위를 유지하며 장비 착용·미래 강화·랜덤 최종값을 확정하지 않는다.

## 검사와 후보

빌드 완료: 2026-10-21 01:21:38 KST · SDK 8.0.426 · 전체 18개 검사 프로그램 PASS. 빌드 입력 229개·소스/리소스 96개·읽기 전용 외부 참조 15개는 빌드 전후 동일하다.

| 파일 | 크기 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 718336바이트 | `ba8e7c492b404c5be6bdf6dbdca6e615587139da2638a7ec69aa4b6a1c7c382f` |
| DiceMaster.cfg | 12526바이트 | `c74383a951b2a957a7aeaa55989fbbbc1758619e69154131f106dcf5c3bc175b` |
| DiceMaster-source-candidate.zip | 304279바이트 | `40ec3d4ce7e8a23959f1a22134347e08e7dbc171d76680a3ed429cf4754fdb48` |

검사별 실제 결과:

| 검사 | 결과 |
| --- | --- |
| MapInformationTests | PASS: 93 map/one-shot managed boundary assertions; Unity/Windows runtime not executed. |
| ShopInformationTests | PASS: ShopInformationTests 659 assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed. |
| PreviewIntegrationTests | PASS: 2107 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures. |
| LogUiKeyboardTests | PASS: LogUiKeyboardTests: 5288 checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered. |
| InformationRuntimeTests | PASS: InformationRuntimeTests: 45 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures. |
| BranchHistoryTests | PASS: BranchHistoryTests: 412 checks (actual-source history/observation, actual candidate DLL IL, supplied save/scene/result API metadata); game/Unity/display helpers are explicit fixtures, Windows/native not executed. |

선택하지 않은 장소 500개가 별도 조회를 만들지 않음, A→B→A의 이전 게시 거부, 1,000회 실제 오버레이 소스 호출에서 동일 패널 높이 계산 1회, 대기/숨김/OFF·화면 크기 변경, 실제 후보 DLL 파서의 물품 종류/ID·조건부 사용 효과 연결을 포함한다. 패키지는 정확히 DLL/cfg 두 파일·CRC·내부 바이트·네 설정 기본 true를 확인했다.

로컬 영수증은 `artifacts/issue-6/selected-map-shop-candidate/build-manifest.json`·`build.log`·`followup-verification.json`이다. 위 검토 후보 생성 당시 기반 HEAD는 `dfa66245eace50cc25a73015b490832d5ce512c6`이며 이후 작업 트리 입력으로 빌드했다. 당시 영수증은 고정 커밋 재빌드·공개 배포 검증 이전의 기록으로 보존한다. v1.0.7-alpha.2의 고정 소스 재빌드와 게시 결과는 후속 영수증으로 구분한다.

자동 검사의 대역·실제 제공 API 메타데이터·실제 후보 DLL 파서 IL 실행과 Windows 게임·Unity UI·물리 장치·프레임 성능은 다른 근거다. 겹침 면적의 전역 최소를 찾는 과거 검사 대신 고정 위치·최대 다섯 후보의 비용 상한·선택 변경·실제 반복 그리기 캐시를 검사한다. 검사 개수 감소를 기능 누락이나 성능 수치로 해석하지 않는다.

## 공개 prerelease 게시와 검증

사용자의 커밋·prerelease 지시에 따라 **2026-10-21 01:35:02 KST에 [v1.0.7-alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.2)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.2/DiceMaster-v1.0.7-alpha.2.zip)은 위 검토 후보와 같은 바이트이며 파일명만 DiceMaster-v1.0.7-alpha.2.zip으로 지정했다.

고정 소스·주석 태그 `4bbdffb15c8fd134ea06749e79cd5d75c929fb53`를 기존 `codex/prd6-information` 브랜치와 atomic push했다. 기존 관리 모드 DLL 0개인 별도 소스 스냅샷을 `verify-preview-release.py --scope prd6`로 재빌드해 2026-10-21 01:33:46 KST에 18개 검사 프로그램을 통과했다. 검토 후보와 SDK·229개 입력·96개 소스/리소스·15개 읽기 전용 참조·전체 검사 결과·DLL/cfg/ZIP 바이트가 일치한다. Python 검증기 회귀 20개도 다시 통과했다.

Release `418628151`·asset `659869905`, `draft=false`·`prerelease=true`·`make_latest=false`다. 익명 공개 API·페이지·설치 ZIP 다운로드, ZIP CRC·정확히 두 파일·내부 바이트·SHA-256과 주석 태그의 소스 커밋을 확인했다. 게시 전 기존 43개 Release·44개 태그·자산과 Latest 정식 v1.0.6을 보존했다. main `375c39715c91d9a2bcdc629dbfe8a88ac0ff127a`과 다른 세션의 변경을 보존하고 이슈 #2 closed·#4 closed·#5 open·#6 open을 유지했다.

영수증은 로컬 `artifacts/releases/v1.0.7-alpha.2/`의 `release-verification.json`·`build-manifest.json`·`build.log`·`publication-preflight-summary.json`·`github-publication.json`·`anonymous-verification.json`·`tag-verification.json`·`release-body.md` 등에 보관한다. 고정 재빌드 영수증의 `published=false`와 소스 문서의 미게시 문구는 게시 전 시점이다. 이 게시 결과 보충은 별도 문서 커밋이며 생산/검사/빌드 입력·소스 태그·설치 파일은 바꾸지 않는다.

새 DLL의 실제 Windows·Unity·물리 입력·프레임 성능은 아래 확인표에서 대기한다. 이전 DLL의 사용자 관찰과 v1.0.6 선행 확인은 이 파일의 Windows 통과 근거가 아니다. #6 전체 완료·이슈 종료로 처리하지 않는다.

## 설치와 실제 Windows 확인

게임을 종료하고 기존 DLL/cfg를 백업한 뒤 새 후보 ZIP의 **plugins/DiceMaster/DiceMaster.dll만 교체**한다. cfg·단축키·배치·글자는 보존한다. 문제 시 게임 종료 후 백업 DLL/필요 cfg 또는 확인된 정식 v1.0.6으로 복구한다. DLL 복구는 저장한 게임 진행을 되돌리지 않는다.

아래 항목은 모두 **미실행·사용자 확인 대기**다.

- [ ] 설치 DLL 해시, 게임·Windows·BepInEx 버전, 해상도·글자 크기, 패드 모델과 Steam Input 설정을 기록한다.
- [ ] 패드·키보드·마우스로 장소를 바꾸면 선택한 장소 이름의 패널 하나만 고정 위치에 보인다. 선택 해제·지도 종료·로그/팝업 열기에는 숨고 예전 장소 내용이 남지 않는다.
- [ ] 후보가 많은 지도와 긴 이름·큰 글자에서 가독성과 실제 렉을 비교한다. 기존 전체 지도 배치가 사라졌는지 확인하고 실제 프레임 시간/할당량은 환경·측정 방법과 함께 기록한다.
- [ ] 지도 안내/1회성의 각 ON/OFF와 둘 다 OFF, 재시작, 선택을 유지한 OFF→ON을 확인한다. 1회성은 선택한 장소만 안내하며 일반 선택지의 판별 한계는 그대로다.
- [ ] 실제 상점의 일반·특수 물품과 책/비급을 연속 선택해 이름·상태·로그 효과가 같은 물품에 붙는지 확인한다. 기존 설명 보존·중복 없음·종류/ID 혼동 없음·미지원 사유 구분을 확인한다.
- [ ] 상점의 새 상태를 실제 사용 가능 확정으로 오해하지 않는지 확인한다. 조회만으로 돈·재고·아이템·능력치·학습점·게임 RNG가 바뀌지 않아야 한다.
- [ ] 분기 5초 알림·20건·저장/로드/장면/OFF 초기화, #5 팝업·네 탭·예약·주사위와 패드 해제/중립·키보드·마우스·터치 조작을 회귀 확인한다.
- [ ] 정상 0.25초·실패 5초·숨김/OFF 조회 중단, 선택 변경 뒤 오래된 자료 폐기와 첫 대기/이전 값 표시를 확인한다.

새 후보의 Windows 확인이 남아 있어 #6 전체 완료·이슈 종료로 처리하지 않는다.
