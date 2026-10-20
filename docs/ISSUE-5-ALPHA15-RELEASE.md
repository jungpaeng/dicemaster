# v1.0.6-alpha.15 — 조건·이벤트 팝업의 조건과 판정 표시 통일

상태: **2026-10-20 12:54:18 KST prerelease 게시 완료 · 새 DLL의 실제 Windows·장치 확인 대기**. alpha.14 이후 두 팝업에서 같은 수준의 조건 요약과 전체 상세를 읽고, 확정 미충족·미확정·실행에 필요한 자료 확인 실패를 구분하도록 정리한다. 소스·주석 태그는 `ede946e674f2c200d20b76eb5efe9728b61861d0`이며 후보·고정 재빌드·익명 공개 파일의 바이트·해시를 대조했다.

**[이 버전 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.15/DiceMaster-v1.0.6-alpha.15.zip)** · [Release](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.15) · [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha15-팝업-조건요약판정-통일-확인표).

## 사용자에게 달라진 점

- 조건 분기·이벤트 목록을 모두 **굵은 제목·결과 요약·조건 요약의 고정 세 줄**로 표시한다. 선택해도 행 높이와 같은 위치의 다음 터치 대상이 달라지지 않도록 유지한다.
- 선택 상세는 **짧은 판정 이유가 필요한 경우 먼저 표시 → 전체 조건 → 예상 결과** 순서다. 정상 발동 가능/불가 문구를 매 행에 추가하거나 같은 조건식을 상세에 반복하지 않는다. 긴 목록 요약은 줄여 표시하고 상세는 휠·오른쪽 스틱으로 읽는다.
- 이벤트 상세에 활성 조건 전체·기본 가중치·조건별 가중치 증감·최종 가중치를 표시한다. 조건 비교가 세 개를 넘더라도 최대 세 개의 미충족 비교만 추리는 대신 설명 가능한 전체 식을 읽는다. 가중치를 확률 `%`로 표시하지 않는다.
- 조건 분기의 앞 분기 우선순위·기본 분기·이번 실행 횟수 비교를 보존한다. 난수는 실제로 뽑힌 값처럼 보이는 표시용 예시값 대신 **범위와 식·미확정**으로 표시한다. 표시를 위해 게임의 실제 난수를 뽑거나 실행 횟수를 증가시키지 않는다.

| 현재 판정 | 두 팝업의 공통 표시·진행 |
| --- | --- |
| 일반 진행 가능 | 기본색. 기존 일반 진행을 유지하며 불필요한 정상 상태 문구는 생략 |
| 조건 미충족·가중치 0·앞 분기 우선이 확인됨 | 제목·조건 회색과 짧은 이유. 읽기 가능한 결과는 기존 명시 조건 무시로 선택 가능 |
| 난수·미지원 계산·앞 분기 미확정 | 기본색과 `?`·사유. 미충족으로 단정하지 않으며 기존 명시 조건 무시로 선택 가능 |
| 대상·결과 또는 판정 자료 확인 실패 | `!`·사유. 일반·조건 무시 실행을 차단 |

결과 요약·상세의 효과색과 기존 실행 판단을 보존한다. **설명 형식화 실패만으로 이미 확인한 판정을 미충족이나 실행 불가로 바꾸지 않는다.** 순환·깊이·표시 한도 또는 지원하지 않는 값은 해당 위치에 미확인 사유를 남긴다. 설명 문자열·색상은 결과 실행을 허가하는 근거로 사용하지 않는다.

## 조작·설정과 기존 기능

첫 클릭·터치는 행 선택만 수행하고, 선택된 행을 다시 누르고 같은 행에서 떼면 표시된 일반/명시 조건 무시 방식으로 한 번 진행한다. A/Enter도 기존 확인 입력이며 일반 진행이 조건 무시로 자동 승격하지 않는다. B/Esc는 원래 게임 판정이다. 하단은 안내 텍스트이며 별도 실행 버튼이나 무작위 목록 행을 추가하지 않는다.

팝업에서 **로그 열기**·기존 로그 키(기본 Tab)·설정된 같은 패드 L3/R3 클릭 겹침으로 큰 로그를 연다. 열린 동안 로그만 입력을 받고 팝업의 Lua 흐름과 해당 예약 소비·설정 OFF 후 원래 판정도 닫기 전 보류한다. 닫으면 유효한 선택·스크롤을 보존하며 닫기 입력을 진행에 재사용하지 않는다. 해제·중립 뒤 새 입력을 받는다.

조건 팝업 신규 기본값 `EnableSwitchSelectionPopup=false`, 이벤트 기본값 `EnableEventSelectionPopup=true`, 기존 cfg 저장값을 유지한다. alpha.11에서 저장한 Switch ON은 DLL만 교체해도 ON이다. 두 팝업과 예약에는 상위 `EnableOptionInfo`가 필요하다. 체크포인트당 한 행·상세 분기 예약, 독립적인 조건·이벤트 일회 예약과 팝업 우선권, 미션·주사위 조작은 유지한다.

## 설치·업데이트와 복구

Windows용 활협전과 **BepInEx가 포함된 활협전용 모드 베이스**가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 위 설치 ZIP을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원은 이미 진행·저장한 게임 상태를 되돌리지 않는다.

alpha.14로 복구하면 이벤트 팝업은 이전 두 줄 요약과 상세 정보량으로 돌아간다. 조건 분기 팝업과 이벤트 팝업의 판정 표시 차이도 다시 생기지만 팝업에서 로그 열기와 조건 상세 예약은 유지된다. 기존 Release·태그·자산과 Latest 정식 v1.0.5를 보존했다. 설치 ZIP은 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함한다.

## 검증과 알려진 제한

새 설명은 표시용 엄격 평가와 유한한 시간·능력치 번역·직접 지역화 이름 조회를 사용한다. 기존 이름 도우미의 GetFlag/DevNote 표현식 확장은 순환할 수 있어 팝업에서 호출하지 않으며 실패 시 원본 이름·ID를 남긴다. 원래 게임의 조건 검사·가중치·Switch 판정 알고리즘이나 직접 지정의 의미는 변경하지 않는다. 예약/직접 지정은 기존과 같이 해당 원래 판정을 건너뛰고 이후 대사·효과는 게임이 실행한다. 모든 후속 조건·미래 결과를 확정하지 않는다.

이벤트 팝업은 지원하는 비메인 Lua의 직접 리터럴 `Position` 호출, 조건 팝업은 직접 문자열 리터럴 `Switch`를 지역 변수에 대입한 뒤 바로 정수 동등 비교 `if/elseif/else`로 분기하는 형식을 지원한다. 팝업 지원 형식과 예약 소비 경계는 넓히지 않는다. 회상·조건 설명의 내부 ID 문제를 해결한 것으로 안내하지 않으며 구조 리팩터링의 보류한 3단계도 범위에 포함하지 않는다.

관리 코드·제공된 게임 API·MoonSharp와 모의 Unity 경계 검사는 실제 Windows 화면·XInput·물리 패드·트랙패드·모니터 터치 실행과 구분한다. **이번 DLL의 실제 Windows·장치 확인과 이슈 #5 완료는 대기**다. [표시·판정의 구현 경계](ISSUE-5-POPUP-CONDITION-PARITY.md).

## 자동 검사 기록

2026-10-20 12:51:23 KST, .NET SDK 8.0.426 전체 소스 후보 빌드에서 **14개 검사 프로그램**을 통과했다. 194개 빌드 입력·79개 소스/리소스 해시와 ZIP CRC·두 파일·내부 DLL/cfg 바이트를 확인했다. 빌드 오류 0개이며 기존 생산 소스 경고 7개를 포함한다. 실제 Windows·장치는 미실행이다.

| 검사 프로그램 | 이번 후보 결과 |
| --- | --- |
| LogRefreshTests | 17개 단언 |
| LiveLogEngineTests | 16개 시나리오·70개 단언 |
| LifecycleTests | 4,023개 단언 |
| LiveLogIntegrationTests | 480개 단언 |
| RealGameApiTests | 관리 동작 3,988개·메타데이터 계약 1,250개 단언 |
| GamepadTests | 262,831개 단언 |
| SourceBehaviorTests | 3,511개 단언 |
| PreviewIntegrationTests | 2,099개 단언 |
| LogUiModelTests | 444개 단언 |
| LogUiWiringTests | 105개 단언 |
| LogUiKeyboardTests | 5,015개 검사 |
| LogOpenerIntegrationTests | 43개 시나리오·1,839개 단언 |
| EventSelectionRuntimeTests | 실제 MoonSharp 연결 237개 시나리오·1,393개 단언 |
| EventPopupTests | 2,069개 검사 |

새 회귀는 전체 조건·AND/OR·중첩 수식·모든 조건별 가중치, 난수 원래 범위·미확정과 RNG/조건 횟수 보존, 설명 실패와 실행 권한 분리, 이름 변환의 순환 경로 회피, 두 팝업의 상태·상세 순서·고정 터치 좌표·큰 글자/좁은 창, 기존 Switch 우선순위·예약·로그 포커스를 포함한다. 고정 소스 별도 재빌드에서 같은 14개 검사·194개 입력·79개 소스/리소스·전체 검사 결과와 DLL/cfg/ZIP 바이트 일치를 확인했다. 게시 후 익명 공개 페이지·ZIP 다운로드와 패키지 대조도 통과했다.

## 파일 확인과 게시 기록

아래 파일은 검토 후보·고정 소스 재빌드·익명 공개 다운로드의 바이트·해시가 일치한다. 소스 커밋은 [ede946e674f2c200d20b76eb5efe9728b61861d0](https://github.com/jungpaeng/dicemaster/commit/ede946e674f2c200d20b76eb5efe9728b61861d0)다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `DiceMaster-v1.0.6-alpha.15.zip` | 272,831 | `013ef9ca6c90cab0554ece34c78a24acb8210fe147b320c73fd1330e46d51bcb` |
| `DiceMaster.dll` | 648,704 | `9ecc7dff8958f66584ed7589448dc8adfab934982552d3ca4f7b522d8c8f203c` |
| `DiceMaster.cfg` | 11,761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

| 게시 항목 | 현재 기록 |
| --- | --- |
| 사전 원격 main | `5e2d4cf46fbe1236eac5fae4b0e2abb0b13fd63f` |
| 고정 소스·주석 태그 커밋 | `ede946e674f2c200d20b76eb5efe9728b61861d0` |
| 후보 빌드·입력·패키지 대조 | 2026-10-20 12:51:23 KST 완료. 194개 입력·79개 소스/리소스·전체 14개 검사·ZIP CRC/두 파일/바이트 확인 |
| 고정 소스 재빌드·전체 검사·후보 대조 | 2026-10-20 12:53:40 KST 고정 검사 완료, 12:53:54 KST 후보 대조 완료. 194개 입력·79개 소스/리소스·전체 14개 검사 결과·DLL/cfg/ZIP 바이트 일치 |
| 게시 시각 | 2026-10-20 12:54:18 KST |
| Release / asset ID | `417982532` / `658198769` |
| 공개 상태 | `draft=false`·`prerelease=true`·`make_latest=false` |
| 공개 페이지·ZIP CRC·두 파일·내부 바이트·SHA-256 | 익명 공개 페이지·다운로드 대조 통과 |
| 기존 Release·태그·자산 | 사전 기존 38개 Release·38개 태그·자산 보관, 게시 후 보존 확인 |
| Latest 정식·게시 전후 이슈 상태 | v1.0.5 · #2 closed·#4 closed·#5 open 유지. 이 작업은 이슈를 변경하지 않음 |

고정 소스를 기존 모드 DLL 0개인 별도 소스 스냅샷에서 재빌드하고 같은 14개 검사 프로그램을 통과했다. 검토 후보와 입력·소스/리소스·전체 검사 결과·파일 바이트가 일치했다. 구현·검사·README를 `origin/main`에 반영하고 고정 커밋을 가리키는 주석 태그로 새 버전을 게시했다. 기존 38개 Release·태그·자산과 Latest 정식 v1.0.5를 보존했으며 이슈 #5는 종료하지 않았다.

후보 영수증은 로컬 `artifacts/popup-parity/full/candidate/build-manifest.json`, 전체 로그는 `artifacts/popup-parity/full-build-run2.log`에 보존한다. 고정 소스·게시·익명 검증은 `artifacts/releases/v1.0.6-alpha.15/`의 `release-verification.json`·`reviewed-candidate-comparison.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`에 보존한다. 고정 검사 영수증의 `published=false`와 고정 소스 문서의 준비 문구는 게시 전 검사 시점이다. 이 게시 결과 보충은 별도 main 문서 커밋으로 반영하며 빌드 소스·태그·공개 자산은 변경하지 않는다. 이전 DLL의 검사나 실제 Windows 확인을 이번 파일의 통과 근거로 대신 사용하지 않는다.
