# 조건 체크포인트 목록과 상세 분기 선택

상태: **2026-10-20 05:59:52 KST v1.0.6-alpha.13 prerelease 게시 완료 · 실제 Windows·장치 확인 대기**. 조건을 분기별 목록 행으로 표시한 alpha.12에서 체크포인트당 한 행과 상세 분기 선택으로 변경한다. 소스·주석 태그는 `f9ec0be282ec86d14a0bfec23ab5a7ff6723101b`이며 후보·고정 소스 재빌드에서 각각 14개 검사와 동일 입력·DLL/cfg/ZIP 바이트를 확인했다. 게시 후 익명 공개 ZIP의 CRC·두 파일·내부 바이트·해시를 대조했다. [alpha.13 설치·배포 기록](ISSUE-5-ALPHA13-RELEASE.md)을 따르며 alpha.12의 공개 파일과 당시 이력은 보존한다.

[현재 README](../README.md#시험판의-조건이벤트-선택) · [변경 이력](../CHANGELOG.md) · [실제 Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha13-조건-상세-선택-확인표)

## 표시와 조작

조건 탭 목록을 **체크포인트당 한 행**으로 되돌린다. 체크포인트를 고르면 상세에 **분기 1·분기 2·그 외**를 별도 설명 블록으로 모두 표시한다. 각 분기의 내부 조건·앞 분기 우선순위·fallback 맥락을 유지하며, 실행 횟수 기반 Switch도 다음 실행 횟수와 비교식을 읽을 수 있다. 줄바꿈이나 번역 문구로 분기를 다시 나누지 않는다.

| 입력 | 조건 탭의 동작 |
| --- | --- |
| 목록 첫 클릭·터치 / ↑↓ | 체크포인트 선택, 전체 조건 읽기 |
| 체크포인트 선택 후 → 또는 새 A / Enter / Space | 상세 분기 선택 영역 진입. 예약하지 않음 |
| 상세의 첫 클릭·터치 | 해당 분기 선택만 수행 |
| 상세 ↑↓ | 선택 분기 이동 |
| 선택 분기의 새 A / Enter / Space | 해당 분기 예약 또는 같은 예약 해제 |
| 선택 분기를 다시 누르고 같은 곳에서 떼기 | 해당 분기 예약 또는 같은 예약 해제 |
| 상세 ← 또는 B / Esc | 체크포인트 목록으로 복귀 |
| 오른쪽 스틱·휠 | 상세 본문 스크롤 |

목록에서 체크포인트 행을 다시 눌러도 예약하지 않는다. 상세 진입 시 이전에 선택한 유효 분기가 있으면 유지하고, 없으면 첫 분기를 선택하기만 한다. 목록이 미선택이면 확인 입력은 첫 체크포인트를 고르기만 한다. 예약한 분기와 해당 체크포인트에는 `(예약 중)`을 표시한다. 읽기 실패·이전 값·대상 확인 불가 항목은 설명을 읽을 수 있어도 예약을 변경하지 못한다.

## 변경 범위와 입력 검증

- [LogBridge.cs](../src/DiceMaster/LiveLog/LogBridge.cs)는 체크포인트를 한 번 게시하고, 전체 조건 블록에 불변 분기 ID와 실제 예약 대상을 연결한다. 표시를 위해 게임 조건을 추가로 평가하지 않는다.
- [LogModel.cs](../src/DiceMaster/LogUI/LogModel.cs)는 체크포인트 선택과 상세 분기 선택을 구분한다. 예약 입력은 부모 체크포인트 ID·분기 ID·실제 게시 대상과 연결하고, 분기 변경 시 이전 입력 권한을 취소한다.
- [LogRenderer.cs](../src/DiceMaster/LogUI/LogRenderer.cs)는 상세 분기 탐색과 클릭·터치의 누름/해제 검증을 연결한다. 첫 선택 입력을 예약으로 재사용하지 않고, 이미 선택한 분기의 다음 새 누름만 예약 권한을 얻는다. 터치 후 패드로 전환하거나 도구에서 돌아올 때 유효한 상세 탐색 영역을 유지한다.

같은 대상의 정상 갱신은 체크포인트·분기 선택과 읽던 위치를 유지한다. 분기 대상·Switch 횟수/모드가 교체되거나 게시가 실패하면 이전 누름을 무효화한다. 오래된 분기 대상을 다른 체크포인트의 결과로 예약하지 않도록 현재 게시 대상 소속도 확인한다. 문맥 교체와 예약 소비·재등록 중의 이전 누름 차단을 유지한다.

분기를 고르거나 예약을 교체·해제해도 제목 위치가 움직이지 않도록 제목의 문구·굵기를 유지하고 상태 설명은 가능한 모든 상태의 최대 높이를 확보한다. 목록·상세의 하단 안내도 두 안내의 최대 높이를 공통 사용한다. 좁은 창에서 본문에 들어가는 안내에도 적용해 스크롤 끝에서 첫 터치 후 같은 위치를 다시 누를 수 있도록 한다. 이는 관리 경계의 배치·입력 검사이며 실제 Unity·모니터 터치 검증과 구분한다.

예약 저장소와 실제 실행 경로는 변경하지 않는다. 다음 해당 판정에 한 번 적용하고 팝업보다 우선하며, 현재 조건을 우회하는 의미·일반 이야기 교체 뒤 유지·로드/새 게임에서 해제하는 수명은 [alpha.12 예약 설계](ISSUE-5-CHECKPOINT-RESERVATIONS.md)를 따른다. 조건 팝업 신규 기본값 OFF와 기존 cfg 저장값 보존, 이벤트·미션의 목록 예약·해제, 기존 팝업·주사위 조작도 유지한다.

## 검증 기록

| 검사 | 현재 기록 |
| --- | --- |
| LogUiModelTests | 새 상세 분기 선택·입력 권한 경계를 포함해 444개 단언 통과 |
| LiveLogIntegrationTests | 새로 빌드한 실제 DLL을 대상으로 480개 단언 통과 |
| LogUiKeyboardTests·GUI 연결 검사 | 관리 경계 입력·배치 5,015개, 실제 후보 DLL GUI 연결·게시·입력 경계 90개 통과 |
| 기존 실행·팝업 회귀 | EventSelectionRuntimeTests 198개 시나리오·1,140개 단언, EventPopupTests 1,625개 검사 통과 |
| 전체 소스 후보 빌드·14개 검사 프로그램·설치 ZIP | 전체 통과. 189개 빌드 입력·78개 소스/리소스, 새 DLL/cfg/ZIP 해시·ZIP CRC·정확한 두 파일과 내부 바이트 대조 통과 |
| 실제 Unity/Windows·물리 패드·트랙패드·모니터 터치 | 미실행 |
| 커밋·origin/main 반영·새 prerelease | 소스·태그 `f9ec0be282ec86d14a0bfec23ab5a7ff6723101b`를 main에 반영하고 2026-10-20 05:59:52 KST alpha.13 게시. 공개 파일 대조 통과 |

최종 후보는 `artifacts/condition-detail-selection/final/candidate/`에 생성했다. 전체 실행 로그는 `artifacts/condition-detail-selection/final/build-run.log`, 최종 입력·패키지 대조 기록은 `artifacts/condition-detail-selection/final/verification.json`, 14개 프로그램의 결과는 후보의 `build-manifest.json`에 기록했다. 빌드는 오류 0개이며 기존 생산 소스 경고 7개를 포함한다. 관리 경계 검사와 실제 게임·장치 실행은 구분한다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 640,000 | `a51bf83e35dc57da9d083f5c09f0234fa931b2782d9b315f9cf26d8aa0acceb2` |
| DiceMaster.cfg | 11,761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |
| DiceMaster-source-candidate.zip | 269,758 | `94c82f31c9c6253f4ca1fff77efcfb57990055ac0c44e671e1878750548ab387` |

같은 바이트의 공개 설치 파일은 [DiceMaster-v1.0.6-alpha.13.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.13/DiceMaster-v1.0.6-alpha.13.zip)이며 고정 재빌드·게시·익명 검증 영수증은 로컬 `artifacts/releases/v1.0.6-alpha.13/`에 보존한다. 검토 후보 파일명은 후보 생성 당시 이름이다. ZIP에는 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll`만 들어 있다. alpha.12의 DLL·ZIP 해시나 자동 검사 결과를 이번 변경 파일의 근거로 대신 사용하지 않는다. 실제 장치 시험은 [alpha.13 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha13-조건-상세-선택-확인표)에 결과와 시험 파일 식별값을 남긴다.
