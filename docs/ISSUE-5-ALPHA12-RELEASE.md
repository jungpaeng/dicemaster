# v1.0.6-alpha.12 — 조건·이벤트 일회 예약

상태: **2026-10-20 05:36:03 KST prerelease 게시 완료 · 새 DLL의 실제 Windows·장치 확인 대기**. 조건·이벤트 목록의 예약과 조건 팝업의 신규 기본값 OFF를 포함한다. 소스·주석 태그는 `11a6fb4570ca632ff435d3ddf2ac74beecfe1aa8`이며 후보·고정 재빌드·익명 공개 파일의 바이트·해시를 대조했다.

**[이 버전 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.12/DiceMaster-v1.0.6-alpha.12.zip)** · [Release](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.12) · [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)

## 사용자에게 달라진 점

- 조건 탭은 체크포인트의 **분기 1·분기 2·그 외**를 결과별 행으로 표시한다. 선택한 결과의 조건을 읽고 미션처럼 해당 결과를 한 번 예약하거나 해제한다.
- 이벤트 탭에도 일회 예약·해제를 추가한다. 조건과 이벤트는 각각 하나씩 예약하며, 같은 탭의 다른 결과를 예약하면 그 탭의 이전 예약만 교체한다. 기존 미션 예약은 유지한다.
- 조건 팝업의 신규 기본값을 **`EnableSwitchSelectionPopup=false`**로 바꾼다. 새 cfg나 해당 키가 없는 cfg에 적용하며 **기존에 저장된 ON은 자동으로 OFF로 바꾸지 않는다**. 조건 탭 상단에서 직접 OFF로 변경할 수 있다. 이벤트 팝업의 기본값 `true`와 저장값은 유지한다.
- 지원하는 대화 중 조건·이벤트 선택 팝업, 결과 요약·효과색·상세, 종류별 상단 ON/OFF와 기존 클릭·터치·패드 입력은 유지한다. alpha.11의 조건명 원복을 유지하며 회상·조건 설명에 내부 ID가 남는 문제를 해결했다고 안내하지 않는다.

## 예약 조작과 적용 의미

| 입력·상황 | 동작 |
| --- | --- |
| 첫 클릭·터치 | 행 선택만 수행 |
| 선택한 행을 새로 눌렀다 같은 행에서 떼기 | 해당 결과 예약·해제 |
| ↑↓·왼쪽 스틱으로 선택한 뒤 새 A / Enter | 해당 결과 예약·해제 |
| 선택 없이 확인 입력 | 첫 행 선택만 수행 |
| 예약 표시 | 해당 행에 `(예약 중)` 표시 |
| 읽기 실패·이전 값·대상/결과 불명 | 예약을 변경하지 않음 |

예약은 **다음 일치하는 실제 체크포인트 호출에서 고른 결과를 한 번 지정**한다. 즉시 이벤트를 발생시키거나 멈춘 행동을 재개하지 않는다. 큰 로그를 닫고 게임을 진행해야 할 수 있다. 유효한 예약은 해당 팝업의 ON/OFF보다 우선하며, 같은 팝업이 기다릴 때 큰 로그에서 예약한 결과도 흐름이 재개될 때 적용한다.

수동 예약은 원래 조건·가중치·난수 평가를 건너뛰지만 능력치·호감도·스토리 플래그를 미리 바꾸지 않는다. Switch 자체 실행 횟수는 한 번 증가하고, 반환한 결과 이후의 게임 대사·효과는 실행될 수 있다. 원래 난수 소비 순서나 모든 후속 조건의 통과를 보장하지 않는다.

예약은 메모리에만 유지하며 cfg·세이브에 저장하지 않는다. 일반 이야기/Lua 교체에는 유지하고 로드·새 게임 등 실행 전환에서 해제한다. 실행 전 대상·소속·결과·Switch 모드/횟수 등이 바뀌면 오래된 예약을 버린다. 해제는 이미 소비한 결과나 진행한 게임 상태를 되돌리지 않는다. 상위 `EnableOptionInfo`가 OFF이면 예약을 적용·소비하지 않는다.

## 설치·업데이트와 복구

Windows용 활협전과 **BepInEx가 포함된 활협전용 모드 베이스**가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 위 설치 ZIP을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원으로 이미 진행·저장한 게임 상태가 되돌아가지는 않는다.

**alpha.11에서 저장된 `EnableSwitchSelectionPopup=true`는 DLL만 교체해도 ON으로 유지된다.** 조건 팝업을 끄려면 조건 탭 상단에서 OFF로 바꾸고 저장한다. 새 기본값을 적용하려고 cfg 전체를 교체할 필요는 없다. 조건·이벤트 팝업과 예약 모두 상위 `EnableOptionInfo`가 켜져 있어야 한다.

alpha.11로 복구하면 조건·이벤트 일회 예약은 제공하지 않고 조건 팝업의 새 기본값은 ON이다. 기존 cfg를 보존했다면 저장된 값은 계속 사용한다. 기존 alpha.11 Release·태그·자산과 Latest 정식 v1.0.5는 변경하지 않는다. 설치 ZIP은 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함한다.

## 지원 범위와 알려진 제한

예약은 실제 `CheckPointManager.Position`·`Switch` 경계에서 소비한다. 팝업이 지원하지 않는 Lua 형식·메인 Lua·직접 C# 호출에도 일치하는 유효 예약이 있으면 적용할 수 있다. 같은 체크포인트를 여러 지점에서 호출하면 **다음 일치 호출**이 소비한다. 호출 뒤 Lua가 예약 결과를 반드시 처리하거나 원하는 장면으로 연결한다는 보장은 아니다.

팝업의 지원 범위는 넓히지 않는다. 이벤트 팝업은 지원하는 비메인 Lua의 직접 리터럴 `checkpointmanager.Position`, 조건 팝업은 직접 문자열 리터럴 `Switch`를 지역 변수에 대입하고 바로 정수 동등 비교 `if/elseif/else`로 분기하는 형식에 표시한다. 분기가 둘 미만이거나 지원 밖 형식이면 예약이 없을 때 원래 게임 판정으로 진행한다.

팝업은 처음 클릭·터치로 행을 선택하고 바로 다음 같은 행 클릭·터치나 A/Enter로 진행한다. 하단은 안내 텍스트이며 B/Esc가 원래 판정을 요청한다. 터치 전용 원래 판정 UI는 없다. 미충족 후보는 읽기 가능한 경우 명시적인 조건 무시를 제공하지만 대상·결과 읽기 실패는 실행하지 않는다. 결과 요약·상세는 정적 분석 범위이며 이후의 모든 대사·조건·난수를 확정하지 않는다.

[예약 설계·실행 경계·검사 근거](ISSUE-5-CHECKPOINT-RESERVATIONS.md)와 [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha12-예약기본값-변경-확인표)를 따른다. 구조 리팩터링의 보류한 3단계는 이번 범위에 포함하지 않는다.

## 후보 자동 검사

2026-10-20 05:29:55 KST .NET SDK 8.0.426으로 현재 소스 전체 빌드와 **14개 검사 프로그램**을 통과했다. 빌드 입력 188개·소스/리소스 78개의 해시, 설치 ZIP의 CRC·정확히 두 파일·DLL/cfg 바이트를 대조했다.

| 검사 프로그램 | 이번 후보 결과 |
| --- | --- |
| LogRefreshTests | 17개 단언 |
| LiveLogEngineTests | 16개 시나리오·70개 단언 |
| LifecycleTests | 4,023개 단언 |
| LiveLogIntegrationTests | 431개 단언 |
| RealGameApiTests | 관리 동작 3,946개·메타데이터 계약 1,058개 단언 |
| GamepadTests | 262,831개 단언 |
| SourceBehaviorTests | 3,511개 단언 |
| PreviewIntegrationTests | 2,099개 단언 |
| LogUiModelTests | 421개 단언 |
| LogUiWiringTests | 90개 단언 |
| LogUiKeyboardTests | 5,771개 검사 |
| LogOpenerIntegrationTests | 35개 시나리오·1,408개 단언 |
| EventSelectionRuntimeTests | 실제 MoonSharp 연결 198개 시나리오·1,140개 단언 |
| EventPopupTests | 1,625개 검사 |

예약 검사는 팝업 ON/OFF보다 우선하는 한 번 적용, 대기 중 팝업 대체, 실제 MoonSharp 재개, 메인/지원 밖 Lua·직접 호출, 조건/RNG 조회 없이 결과 반환, Switch 횟수 한 번 증가, 다른 판정·종류 격리, 일반 Lua 교체 뒤 유지, 실행 전환 해제, 대상·결과·설정·횟수 변경의 폐기, 상위 설정 OFF/읽기 실패와 중복·오래된 입력 거부를 포함한다. UI는 결과별 행·예약 표시·A/Enter·마우스/터치·문맥 검증과 기존 미션 예약을 회귀 확인한다.

관리 코드·모의 경계 검사는 실제 Unity/Windows 화면·물리 패드·트랙패드·모니터 터치·다른 모드 조합의 통과 근거로 확대하지 않는다. 이번 새 DLL의 실제 Windows 확인과 이슈 #5 완료는 대기다.

## 파일 확인과 게시 결과

다음 파일은 검토 후보·고정 소스 재빌드·익명 공개 다운로드의 바이트·해시가 일치한다. 소스 커밋은 [11a6fb4570ca632ff435d3ddf2ac74beecfe1aa8](https://github.com/jungpaeng/dicemaster/commit/11a6fb4570ca632ff435d3ddf2ac74beecfe1aa8)이다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `DiceMaster-v1.0.6-alpha.12.zip` | 267,579 | `76d2f8e350b5eab4a57a628a245714705ab6b86f7675db7f7d79e6f8fdb2fc05` |
| `DiceMaster.dll` | 634,368 | `7db4b6dbffd3ed4f4441002fa7c868d5d66db234942395178de5cf4ad6fc802f` |
| `DiceMaster.cfg` | 11,761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

고정 소스 `11a6fb4570ca632ff435d3ddf2ac74beecfe1aa8`를 기존 모드 DLL 0개인 별도 소스 스냅샷에서 재빌드해 2026-10-20 05:34:10 KST에 같은 14개 검사 프로그램을 통과했다. 검토 후보와 188개 입력·DLL/cfg/ZIP 바이트·전체 검사 결과가 일치했다. 구현·검사·README를 `origin/main`에 반영하고 고정 커밋을 가리키는 주석 태그로 새 버전만 게시했다.

| 게시 항목 | 확인 값 |
| --- | --- |
| 게시 시각 | 2026-10-20 05:36:03 KST |
| Release / asset ID | `417723438` / `657372175` |
| 공개 상태 | `draft=false`·`prerelease=true`·`make_latest=false` |
| 공개 파일 대조 | 익명 페이지·다운로드·ZIP CRC·두 파일·내부 바이트·SHA-256 통과 |
| 기존 자산·태그 | 기존 35개 Release·태그·자산 보존 |
| Latest 정식 | v1.0.5 유지 |
| 게시 직전·후 이슈 | #2 closed·#4 closed·#5 open, 이 작업은 이슈를 변경하지 않음 |

사전 확인 뒤 빌드 중 2026-10-20 05:31:35 KST에 `jungpaeng`이 #4를 외부에서 종료했다(`commit_id=null`). 사전 원본·변경 근거를 보존하고 새 상태를 게시 직전 기준으로 다시 확인했다. #4 종료를 이번 DLL의 실제 Windows 확인으로 간주하지 않는다. 기존 35개 Release·태그·자산과 Latest 정식 v1.0.5를 유지하며 이슈 #5는 종료하지 않았다.

후보 영수증은 로컬 `artifacts/checkpoint-reservations/full/candidate/`, 고정 빌드·게시·익명 검증 영수증은 `artifacts/releases/v1.0.6-alpha.12/`에 보존한다. `release-verification.json`의 `published=false`와 고정 소스 문서의 준비 문구는 게시 전 검사 시점이다. 이 게시 결과 보충은 별도 main 문서 커밋으로 반영하며 빌드 소스·태그·공개 자산을 변경하지 않는다.
