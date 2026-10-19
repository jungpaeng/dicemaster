# v1.0.6-alpha.13 — 조건 목록 정리와 상세 분기 예약

상태: **2026-10-20 05:59:52 KST prerelease 게시 완료 · 새 DLL의 실제 Windows·장치 확인 대기**. alpha.12에서 결과마다 행이 늘어나던 조건 목록을 체크포인트당 한 행으로 정리하고, 전체 분기 조건을 상세에 보여주며 상세에서 예약할 분기를 선택한다. 소스·주석 태그는 `f9ec0be282ec86d14a0bfec23ab5a7ff6723101b`이며 후보·고정 재빌드·익명 공개 파일의 바이트·해시를 대조했다.

**[이 버전 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.13/DiceMaster-v1.0.6-alpha.13.zip)** · [Release](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.13) · [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest)

## 사용자에게 달라진 점

- 조건 목록에는 **체크포인트마다 한 행**만 표시한다. 체크포인트를 고르면 상세에서 **분기 1·분기 2·그 외**의 전체 조건을 함께 읽고 예약할 분기를 선택한다. 분기 안의 여러 조건과 앞 분기 우선순위·실행 횟수 안내를 유지한다.
- 상세의 **첫 클릭·터치는 분기 선택**, 선택한 분기의 **다음 새 누름·해제는 예약·해제**다. 체크포인트 목록을 다시 누르거나 상세에 진입하는 것만으로 예약하지 않는다.
- 분기를 고르거나 예약을 교체·해제할 때 상세 제목이 움직이지 않도록 제목과 상태·안내 영역의 배치를 유지한다. 큰 글자·좁은 창·상세 스크롤 끝에서도 같은 위치의 다음 터치로 조작하도록 관리 경계 회귀를 보강한다.
- 조건·이벤트의 독립적인 일회 예약과 팝업보다 우선하는 적용, 조건 팝업 신규 기본값 OFF·기존 cfg 보존, 이벤트·미션의 목록 조작과 기존 팝업·주사위 조작은 유지한다. alpha.11의 조건명 원복도 유지하며 회상·조건 설명의 내부 ID 문제를 해결한 것으로 안내하지 않는다.

## 조건 탭의 조작

| 입력·상황 | 동작 |
| --- | --- |
| 목록 첫 클릭·터치 / ↑↓ | 체크포인트 선택, 전체 조건 읽기 |
| 체크포인트 선택 후 → 또는 새 A / Enter / Space | 상세 분기 선택 영역 진입. 진입만으로 예약하지 않음 |
| 상세 첫 클릭·터치 / ↑↓ | 분기 선택 |
| 선택 분기를 새로 눌렀다 같은 곳에서 떼기 / 새 A / Enter / Space | 선택 분기 예약·해제 |
| 상세 ← 또는 B / Esc | 체크포인트 목록 복귀 |
| 오른쪽 스틱·휠 | 상세 본문 스크롤 |
| 예약 표시 | 예약 분기와 해당 체크포인트에 `(예약 중)` 표시 |
| 읽기 실패·이전 값·대상/결과 불명 | 설명은 읽을 수 있어도 예약을 변경하지 않음 |

목록이 미선택이면 확인 입력은 첫 체크포인트만 고른다. 상세에 다시 진입하면 유효한 이전 분기를 유지하고, 없으면 첫 분기를 선택한다. 터치 뒤 패드를 해제·중립으로 돌려 조작하거나 도구에서 돌아오면 유효한 상세 탐색 위치를 유지한다.

이벤트·미션은 기존처럼 목록에서 첫 클릭·터치로 선택하고 선택 행의 다음 클릭·터치 또는 A/Enter로 예약·해제한다. 조건과 이벤트는 각각 하나씩 독립적으로 예약하고 같은 탭의 다른 결과를 예약하면 그 탭의 이전 예약만 교체한다.

## 예약 의미와 설정

예약은 **다음 일치하는 실제 체크포인트 판정에서 선택 결과를 한 번 지정**한다. 즉시 이벤트를 발생시키거나 대기 중 행동을 재개하는 기능은 아니므로 큰 로그를 닫고 게임을 진행해야 할 수 있다. 유효 예약은 해당 팝업의 ON/OFF보다 우선하고, 같은 팝업이 기다릴 때 큰 로그에서 예약했다면 흐름이 재개될 때 적용한다.

직접 지정은 원래 조건·가중치·난수 평가를 건너뛰지만 능력치·호감도·스토리 플래그를 미리 바꾸지 않는다. Switch 자체 실행 횟수만 한 번 증가하고 반환한 결과 이후의 게임 대사·효과는 게임이 실행한다. 원래 난수 소비 순서나 모든 후속 조건의 통과는 보장하지 않는다.

예약은 cfg·세이브에 저장하지 않는다. 일반 이야기/Lua 교체에는 유지하고 로드·새 게임 등 실행 전환에서 해제한다. 표시·실행 경계에서 대상·결과·소속·Switch 모드/횟수 등이 달라진 예약은 폐기한다. 해제는 이미 진행한 결과를 되돌리지 않는다. 상위 `EnableOptionInfo`가 OFF이면 예약을 적용·소비하지 않는다.

| 설정 | 신규 기본값 | 기존 설정 |
| --- | --- | --- |
| `EnableSwitchSelectionPopup` | `false` | 이미 저장된 ON/OFF 유지 |
| `EnableEventSelectionPopup` | `true` | 이미 저장된 ON/OFF 유지 |

조건·이벤트 탭 상단과 도구에서 각 팝업을 독립적으로 켜고 끄며 변경값을 저장한다. **alpha.11에서 저장된 Switch ON은 새 DLL만 교체해도 ON**이다. 끄려면 조건 탭 상단에서 OFF로 바꾼다. cfg 전체를 교체할 필요는 없다.

## 설치·업데이트와 복구

Windows용 활협전과 **BepInEx가 포함된 활협전용 모드 베이스**가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 위 설치 ZIP을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원으로 이미 진행·저장한 게임 상태가 되돌아가지는 않는다.

alpha.12로 복구하면 조건 목록이 결과별 행으로 돌아가며 예약 기능과 설정 기본값은 유지된다. 기존 alpha.12 Release·태그·자산과 Latest 정식 v1.0.5는 보존한다. ZIP에는 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함한다.

## 검증과 알려진 제한

이번 UI 변경은 예약 저장소·실제 실행 경로·팝업 지원 범위를 변경하지 않는다. 예약은 실제 `CheckPointManager.Position`·`Switch` 경계에서 소비하므로 팝업이 지원하지 않는 Lua 형식·메인 Lua·직접 C# 호출에도 일치하는 유효 예약이 있으면 적용할 수 있다. 같은 체크포인트를 여러 곳에서 호출하면 다음 일치 호출이 소비한다. 반환 이후 Lua가 해당 결과를 처리하거나 원하는 장면으로 연결하는지까지 보장하지 않는다.

이벤트 팝업은 지원하는 비메인 Lua의 직접 리터럴 `Position` 호출, 조건 팝업은 직접 문자열 리터럴 `Switch`를 지역 변수에 대입한 뒤 바로 정수 동등 비교 `if/elseif/else`로 분기하는 형식을 지원한다. 분기가 둘 미만이거나 지원 밖 형식이고 예약도 없으면 원래 게임 판정으로 진행한다. 팝업 하단은 안내 텍스트이며 B/Esc가 원래 판정을 요청한다. 터치 전용 원래 판정 UI는 없다.

결과 요약·조건 설명은 정적 분석 범위이며 모든 후속 대사·조건·난수를 확정하지 않는다. 읽기 가능한 미충족 후보의 명시적인 조건 무시는 유지하고 실제 대상·결과 읽기 실패는 실행을 막는다. 구조 리팩터링의 보류한 3단계는 이번 범위에 포함하지 않는다.

관리 코드·모의 경계 검사와 실제 Unity/Windows 화면·물리 패드·트랙패드·모니터 터치·다른 모드 조합은 구분한다. **이번 DLL의 실제 Windows·장치 확인과 이슈 #5 완료는 대기**다. [alpha.13 Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha13-조건-상세-선택-확인표) · [상세 선택 설계·검증 경계](ISSUE-5-CONDITION-DETAIL-SELECTION.md) · [기존 예약 실행 설계](ISSUE-5-CHECKPOINT-RESERVATIONS.md).

## 후보 자동 검사

2026-10-20 05:53:58 KST .NET SDK 8.0.426의 전체 소스 후보 빌드에서 **14개 검사 프로그램**을 통과했다. 189개 빌드 입력·78개 소스/리소스 해시와 설치 ZIP의 CRC·정확히 두 파일·DLL/cfg 내부 바이트를 대조했다. 빌드 오류 0개이며 기존 생산 소스 경고 7개를 포함한다.

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
| LogUiWiringTests | 90개 단언 |
| LogUiKeyboardTests | 5,015개 검사 |
| LogOpenerIntegrationTests | 35개 시나리오·1,408개 단언 |
| EventSelectionRuntimeTests | 실제 MoonSharp 연결 198개 시나리오·1,140개 단언 |
| EventPopupTests | 1,625개 검사 |

새 회귀는 체크포인트당 한 게시 행·전체 조건 블록 보존, 부모만 선택한 상태의 예약 거부, 분기 선택·입력 권한·소속 확인, 정상 갱신 시 선택/스크롤 보존과 대상 교체/실패 시 오래된 권한 거부를 포함한다. UI는 상세 진입/복귀·분기 탐색·예약 표시, 첫 선택과 다음 새 클릭·터치 예약, 터치→패드·도구 복귀, 좁은 창·큰 글자·스크롤 끝에서 선택과 예약 교체/해제 전후의 제목 좌표 유지까지 관리 경계에서 검사한다. 기존 예약 런타임·팝업·미션·주사위 회귀도 통과했다.

## 파일 확인과 게시 기록

아래 파일은 검토 후보·고정 소스 재빌드·익명 공개 다운로드의 바이트·해시가 일치한다. 소스 커밋은 [f9ec0be282ec86d14a0bfec23ab5a7ff6723101b](https://github.com/jungpaeng/dicemaster/commit/f9ec0be282ec86d14a0bfec23ab5a7ff6723101b)이다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `DiceMaster-v1.0.6-alpha.13.zip` | 269,758 | `94c82f31c9c6253f4ca1fff77efcfb57990055ac0c44e671e1878750548ab387` |
| `DiceMaster.dll` | 640,000 | `a51bf83e35dc57da9d083f5c09f0234fa931b2782d9b315f9cf26d8aa0acceb2` |
| `DiceMaster.cfg` | 11,761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

| 게시 항목 | 현재 기록 |
| --- | --- |
| 고정 소스·주석 태그 커밋 | `f9ec0be282ec86d14a0bfec23ab5a7ff6723101b` |
| 고정 소스 재빌드·14개 검사·후보 대조 | 2026-10-20 05:58:52 KST 완료. 189개 입력·78개 소스/리소스·전체 검사 결과와 DLL/cfg/ZIP 바이트 일치 |
| 게시 시각 | 2026-10-20 05:59:52 KST |
| Release / asset ID | `417748369` / `657424358` |
| 공개 상태 | `draft=false`·`prerelease=true`·`make_latest=false` |
| 공개 페이지·ZIP CRC·두 파일·내부 바이트·SHA-256 | 익명 공개 페이지·다운로드 대조 통과 |
| 사전 원격 main | 게시 전 작업 시작 HEAD와 같은 `a57c286bbe193005919ba48d4c723afc56d04ba0` |
| 기존 Release·태그·자산 | 사전 기존 36개 Release·36개 태그·자산 보관, 게시 후 보존 확인 |
| Latest 정식·게시 전후 이슈 상태 | v1.0.5 · #2 closed·#4 closed·#5 open 유지. 이 작업은 이슈를 변경하지 않음 |

고정 소스 `f9ec0be282ec86d14a0bfec23ab5a7ff6723101b`를 기존 모드 DLL 0개인 별도 소스 스냅샷에서 재빌드하고 2026-10-20 05:58:52 KST에 같은 14개 검사 프로그램을 통과했다. 검토 후보와 189개 입력·전체 검사 결과 및 DLL/cfg/ZIP 바이트가 일치했다. 구현·검사·README를 `origin/main`에 반영하고 고정 커밋을 가리키는 주석 태그로 새 버전을 게시했다. 기존 36개 Release·태그·자산과 Latest 정식 v1.0.5를 보존했으며 이슈 #5는 종료하지 않았다.

후보 영수증은 로컬 `artifacts/condition-detail-selection/final/candidate/`와 `artifacts/condition-detail-selection/final/verification.json`에 보존한다. 고정 소스·게시·익명 검증 기록은 `artifacts/releases/v1.0.6-alpha.13/`의 `release-verification.json`·`reviewed-candidate-comparison.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 고정 검사 영수증의 `published=false`와 고정 소스 문서의 준비 문구는 게시 전 검사 시점이다. 이 게시 결과 보충은 별도 main 문서 커밋으로 반영하며 빌드 소스·태그·공개 자산을 변경하지 않는다.
