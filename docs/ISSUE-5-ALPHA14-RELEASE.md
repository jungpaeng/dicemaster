# v1.0.6-alpha.14 — 팝업에서 로그 열기와 분기 조건 표시

상태: **2026-10-20 12:28:24 KST prerelease 게시 완료 · 새 DLL의 실제 Windows·장치 확인 대기**. alpha.13 이후 선택 팝업에서 큰 로그를 열어 확인하고 돌아올 수 있게 하며, 조건 분기 팝업에 실제 조건과 미충족 회색 구분을 추가한다. 소스·주석 태그는 `3c9aa94512add361818511fe1f57d5987a5855d2`이며 후보·고정 재빌드·익명 공개 파일의 바이트·해시를 대조했다.

**[이 버전 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.14/DiceMaster-v1.0.6-alpha.14.zip)** · [Release](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.14) · [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)

## 사용자에게 달라진 점

- 이벤트·조건 분기 팝업 상단의 **로그 열기**로 큰 로그를 연다. 기존 `LogMenuToggleKey`(기본 Tab)도 사용하며, `EnableGamepadLogOpen=true`이면 팝업 대기 중 같은 패드 L3/R3 클릭 겹침으로도 연다.
- 큰 로그가 열린 동안에는 로그만 조작한다. 대기 중인 팝업의 Lua 흐름과 그 판정에서의 예약 소비, 팝업 OFF 뒤 원래 판정 실행도 로그를 닫을 때까지 기다린다. 닫으면 기존의 유효한 팝업 선택·읽던 위치로 돌아오며 닫기 B/Esc·누른 채인 A를 진행에 재사용하지 않는다. 버튼 해제·스틱/트리거 중립 뒤 새 입력을 받는다.
- 조건 분기 행에 **조건 요약**을 더하고, 선택 상세에는 **전체 분기 조건을 예상 결과보다 먼저** 표시한다. 긴 조건은 목록에서 줄이고 상세에서 모두 읽는다. 여러 조건·앞 분기 우선순위·기본 분기·이번 실행 횟수 비교를 유지한다.
- 미충족이나 앞 분기 우선으로 선택되지 않는 분기는 **제목·조건을 회색**으로 표시한다. 난수·미지원은 판정 미확인으로 구분하며 미충족으로 단정하지 않는다. 결과 효과색과 읽기 가능한 결과의 기존 A/Enter 명시 조건 무시를 유지한다. 실제 대상·결과 읽기 실패는 실행을 막는다.

팝업의 첫 클릭·터치는 선택만, 선택된 행의 다음 새 누름·같은 행 해제는 한 번 진행이다. A/Enter도 현재 표시된 일반/조건 무시 방식으로 확인하며, B/Esc는 원래 게임 판정을 요청한다. 하단 진행 안내는 텍스트이며 실행 버튼이 아니다. 터치 전용 원래 판정 UI는 제공하지 않는다.

## 설정과 기존 기능

| 설정 | 신규 기본값 | 이번 동작 |
| --- | --- | --- |
| `EnableSwitchSelectionPopup` | `false` | 조건 탭 상단/도구에서 ON으로 바꾸면 지원하는 대화 분기에서 표시 |
| `EnableEventSelectionPopup` | `true` | 기존 이벤트 팝업 유지 |
| `LogMenuToggleKey` | `Tab` | 팝업에서도 큰 로그 열기·닫기 |
| `EnableGamepadLogOpen` | `true` | 팝업에서도 같은 패드 L3/R3 클릭 겹침으로 로그 열기 |

기존 cfg 저장값을 보존한다. **alpha.11에서 이미 저장한 Switch ON은 DLL만 교체해도 ON**이다. OFF를 원하면 조건 탭 상단에서 바꾼다. 두 팝업과 예약은 상위 `EnableOptionInfo`가 켜져 있어야 한다.

패드 열기를 켜고 게임 창에 초점이 있으며 로그가 닫혀 있을 때 단독 L3/R3 클릭도 게임에 전달하지 않는 기존 계약을 유지한다. 원래 스틱 클릭이 필요하면 `EnableGamepadLogOpen=false`로 바꾼다. 이 옵션은 열기 조합만 끄며 **열린 로그·대기 팝업의 입력 차단이나 패드 탐색을 끄지 않는다**. 주사위 조정 옵션과도 독립적이다.

alpha.13의 체크포인트당 한 행·상세 분기 예약, 조건·이벤트 각각의 독립적인 일회 예약과 팝업 우선순위·수명은 유지한다. 로그에서 예약해도 즉시 결과를 실행하지 않으며, 닫은 뒤 다음 일치하는 실제 판정에서 한 번 적용한다. 직접 지정은 해당 조건·가중치·난수 평가를 건너뛰지만 능력치·호감도·스토리 플래그를 미리 바꾸지 않는다. Switch 자체 횟수만 한 번 증가하고 후속 대사·효과는 게임이 실행한다. 예약은 cfg·세이브에 저장하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 **BepInEx가 포함된 활협전용 모드 베이스**가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 이 버전의 설치 ZIP을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원으로 이미 진행·저장한 게임 상태가 되돌아가지는 않는다.

alpha.13으로 복구하면 이번 팝업 로그 열기 보완·조건 표시 개선은 빠지며 조건 상세 예약은 유지된다. 기존 Release·태그·자산과 Latest 정식 v1.0.5는 보존한다. ZIP에는 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함한다.

## 검증과 알려진 제한

이벤트 팝업은 지원하는 비메인 Lua의 직접 리터럴 `Position` 호출, 조건 팝업은 직접 문자열 리터럴 `Switch`를 지역 변수에 대입한 뒤 바로 정수 동등 비교 `if/elseif/else`로 분기하는 형식을 지원한다. 분기가 둘 미만이거나 지원 밖 형식이고 예약도 없으면 원래 게임 판정으로 진행한다. 예약 자체는 실제 체크포인트 호출에서 적용하므로 팝업 지원 밖 호출에서도 일치하는 유효 예약이 있으면 적용된다. 이는 반환 이후 Lua가 원하는 장면으로 연결하는지까지 보장하지 않는다.

설명은 기존 정적 분석 범위이며 모든 후속 조건·미래 결과를 확정하지 않는다. alpha.11의 조건명 원복을 유지하며 회상·조건 설명의 내부 ID 문제는 해결한 것으로 안내하지 않는다. 구조 리팩터링의 보류한 3단계도 이번 범위에 포함하지 않는다.

관리 코드·모의 경계 검사는 실제 Unity/Windows 화면·XInput·물리 패드·트랙패드·모니터 터치·다른 모드 조합과 구분한다. **이번 DLL의 실제 Windows·장치 확인과 이슈 #5 완료는 대기**다. [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha14-팝업로그-포커스조건-표시-확인표) · [구현·검증 경계](ISSUE-5-POPUP-LOG-CONDITIONS.md).

## 후보 자동 검사

2026-10-20 12:23:20 KST, .NET SDK 8.0.426의 전체 소스 후보 빌드에서 **14개 검사 프로그램**을 통과했다. 191개 빌드 입력·78개 소스/리소스 해시와 설치 ZIP의 CRC·정확히 두 파일·DLL/cfg 내부 바이트를 대조했다. 빌드 오류 0개이며 기존 생산 소스 경고 7개를 포함한다.

| 검사 프로그램 | 이번 후보 결과 |
| --- | --- |
| LogRefreshTests | 17개 단언 |
| LiveLogEngineTests | 16개 시나리오·70개 단언 |
| LifecycleTests | 4,023개 단언 |
| LiveLogIntegrationTests | 480개 단언 |
| RealGameApiTests | 관리 동작 3,946개·메타데이터 계약 1,058개 단언 |
| GamepadTests | 262,831개 단언 |
| SourceBehaviorTests | 3,511개 단언 |
| PreviewIntegrationTests | 2,099개 단언 |
| LogUiModelTests | 444개 단언 |
| LogUiWiringTests | 100개 단언 |
| LogUiKeyboardTests | 5,015개 검사 |
| LogOpenerIntegrationTests | 43개 시나리오·1,839개 단언 |
| EventSelectionRuntimeTests | 실제 MoonSharp 연결 233개 시나리오·1,359개 단언 |
| EventPopupTests | 1,807개 검사 |

새 회귀는 팝업의 마우스/터치 로그 열기·지연 콜백·선택/스크롤 보존·오래된 누름 차단, 캡처 중 L3/R3 열기와 옵션 OFF, 실제 Lua의 로그 열림 중 판정·예약 소비·설정 OFF 보류와 닫은 후 한 번 실행, 조건 앞 배치·미충족 회색/판정 미확인 구분·효과색과 긴 조건 배치를 포함한다. 이전 예약·전체 로그·미션·주사위 회귀도 통과했다.

## 파일 확인과 게시 기록

아래 파일은 검토 후보·고정 소스 재빌드·익명 공개 다운로드의 바이트·해시가 일치한다. 소스 커밋은 [3c9aa94512add361818511fe1f57d5987a5855d2](https://github.com/jungpaeng/dicemaster/commit/3c9aa94512add361818511fe1f57d5987a5855d2)다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `DiceMaster-v1.0.6-alpha.14.zip` | 270,258 | `a8eb1d88c616b6eef22e1c77283ae1e9c145d19924089a8cbb11ed9b0d972733` |
| `DiceMaster.dll` | 641,024 | `3d206a91ce3724e407fc229ec1f727bde6d0f574a6695ff4ef51ae8a7558e95e` |
| `DiceMaster.cfg` | 11,761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

| 게시 항목 | 현재 기록 |
| --- | --- |
| 고정 소스·주석 태그 커밋 | `3c9aa94512add361818511fe1f57d5987a5855d2` |
| 고정 소스 재빌드·14개 검사·후보 대조 | 2026-10-20 12:27:31 KST 완료. 191개 입력·78개 소스/리소스·전체 검사 결과와 DLL/cfg/ZIP 바이트 일치 |
| 게시 시각 | 2026-10-20 12:28:24 KST |
| Release / asset ID | `417969778` / `658150525` |
| 공개 상태 | `draft=false`·`prerelease=true`·`make_latest=false` |
| 공개 페이지·ZIP CRC·두 파일·내부 바이트·SHA-256 | 익명 공개 페이지·다운로드 대조 통과 |
| 사전 원격 main | `182f356f95f19742f374d14f17af883c124c5110` |
| 기존 Release·태그·자산 | 사전 기존 37개 Release·37개 태그·자산 보관, 게시 후 보존 확인 |
| Latest 정식·게시 전후 이슈 상태 | v1.0.5 · #2 closed·#4 closed·#5 open 유지. 이 작업은 이슈를 변경하지 않음 |

고정 소스 `3c9aa94512add361818511fe1f57d5987a5855d2`를 기존 모드 DLL 0개인 별도 소스 스냅샷에서 재빌드하고 2026-10-20 12:27:31 KST 같은 14개 검사 프로그램을 통과했다. 검토 후보와 191개 입력·78개 소스/리소스·전체 검사 결과와 DLL/cfg/ZIP 바이트가 일치했다. 구현·검사·README를 `origin/main`에 반영하고 고정 커밋을 가리키는 주석 태그로 새 버전을 게시했다. 기존 37개 Release·태그·자산과 Latest 정식 v1.0.5를 보존했으며 이슈 #5는 종료하지 않았다.

후보 영수증은 로컬 `artifacts/popup-log-conditions/full/candidate/`와 `artifacts/popup-log-conditions/full/verification.json`, 전체 로그는 `artifacts/popup-log-conditions/full/build-run.log`에 보존한다. 고정 소스·게시·익명 검증은 `artifacts/releases/v1.0.6-alpha.14/`의 `release-verification.json`·`reviewed-candidate-comparison.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`에 보존한다. 고정 검사 영수증의 `published=false`와 고정 소스 문서의 준비 문구는 게시 전 검사 시점이다. 이 게시 결과 보충은 별도 main 문서 커밋으로 반영하며 빌드 소스·태그·공개 자산을 변경하지 않는다. 이전 alpha.13 파일이나 자동 검사 결과를 이번 변경 DLL의 근거로 대신 사용하지 않는다.
