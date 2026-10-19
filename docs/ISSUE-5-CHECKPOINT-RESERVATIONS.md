# 조건·이벤트 일회 예약과 조건 팝업 기본값

상태: **v1.0.6-alpha.12 prerelease 게시 완료 · 새 DLL의 실제 Windows·장치 확인 대기**. 조건 팝업의 신규 기본값 OFF와 조건·이벤트 탭의 일회 예약을 포함한다. 2026-10-20 05:36:03 KST 소스·태그 `11a6fb4570ca632ff435d3ddf2ac74beecfe1aa8`에서 게시했으며 고정 커밋 재빌드·익명 공개 자산 검증은 아래 후보 검사와 구분해 [alpha.12 배포 기록](ISSUE-5-ALPHA12-RELEASE.md)에 남긴다. 공개 alpha.11은 이벤트 탭 조회 전용·Switch 팝업 기본값 ON인 당시 파일로 보존한다.

[현재 README](../README.md#시험판의-조건이벤트-선택) · [변경 이력](../CHANGELOG.md) · [alpha.12 배포 기록](ISSUE-5-ALPHA12-RELEASE.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)

## 사용자 동작과 설정

조건 탭은 체크포인트 하나에 속한 각 결과를 **분기 1·분기 2·그 외**의 별도 행으로 표시한다. 선택한 행의 조건만 상세에 표시하되, 원래 검사는 위에서 처음 만족하는 분기를 사용한다는 공통 안내를 유지한다. 실행 횟수를 비교하는 Switch도 다음 실행 횟수와 비교식을 표시한다. 이벤트 탭은 전체 목록과 가능한 항목 우선 정렬을 유지한다.

| 조작 | 동작 |
| --- | --- |
| 첫 클릭·터치 / 목록 탐색 | 행 선택 |
| 선택 후 새 A / Enter / Space | 예약하거나 같은 예약 해제 |
| 선택된 행을 다시 누르고 같은 행에서 떼기 | 예약하거나 같은 예약 해제 |
| 같은 탭의 다른 행 예약 | 그 탭의 이전 예약 교체 |
| 조건·이벤트의 팝업 설정 버튼 | 해당 팝업 ON/OFF 저장. 예약은 해제하지 않음 |

선택이 없을 때 확인 입력은 첫 항목을 고르기만 한다. 예약 행에는 `(예약 중)`을 표시한다. 조건과 이벤트에 각각 하나의 독립된 예약이 있으며 미션 예약과도 별개다. 읽기 실패·이전 값 상태 또는 대상·결과를 확인할 수 없는 행에서는 예약을 변경할 수 없다. 누름 중 게시·대상·결과·예약 상태가 교체되면 이전 누름으로 새 예약을 만들거나 해제하지 않는다.

| 설정 | alpha.12의 신규 기본값 | 기존 cfg |
| --- | --- | --- |
| `EnableSwitchSelectionPopup` | `false` | 이미 저장된 `true`/`false` 유지 |
| `EnableEventSelectionPopup` | `true` | 저장값 유지 |
| `EnableOptionInfo` | `true` | 예약·두 팝업 모두 이 상위 설정이 켜져 있어야 함 |

새 기본값은 새 cfg 또는 Switch 설정 키가 없는 cfg에 적용한다. alpha.11 실행으로 `EnableSwitchSelectionPopup=true`가 이미 저장되었다면 자동으로 OFF로 바꾸지 않는다. 조건 탭 상단 또는 도구에서 직접 OFF로 변경할 수 있다.

## 예약을 소비하는 시점과 의미

예약은 **다음 이름·관리자·데이터가 일치하는 실제 체크포인트 호출**에서 결과를 한 번 지정한다. 큰 로그에서 예약해도 즉시 이벤트를 발생시키거나 현재 행동을 재개하지 않는다. 필요하면 큰 로그를 닫고 게임을 진행해 해당 호출에 도달한다. 다른 체크포인트 호출은 예약을 소비하지 않는다.

유효한 예약은 해당 팝업 설정보다 우선한다. 팝업 ON에서도 다시 선택창을 요구하지 않으며, OFF에서도 예약은 적용된다. 같은 판정의 팝업이 이미 기다리는 동안 큰 로그에서 예약한 경우에는 해당 Lua 흐름이 재개될 때 기존 팝업의 선택 대신 예약을 사용한다. 다른 대기 중인 판정의 소유권을 빼앗지는 않는다.

직접 지정은 수동 결과 우회다. 현재 조건 충족 여부·가중치·원래 난수 판정을 실행하지 않고 알려진 결과를 반환한다. 조건 목록·능력치·호감도·스토리 플래그를 미리 바꾸지 않는다. Switch는 자체 `_count`만 한 번 증가시키며 원래 조건의 난수·중첩 조건 카운터를 실행하지 않는다. 그 결과를 받은 게임의 후속 대사·효과는 정상적으로 실행될 수 있으므로 이후 상태 변화까지 없다는 의미는 아니다. 원래 난수 소비 순서를 보존하거나 모든 후속 조건을 통과시키는 기능도 아니다.

예약이 없으면 기존 팝업 ON/OFF에 따른 동작을 유지한다. 팝업에서 B/Esc로 원래 판정을 요청하는 기능도 유지한다. 상위 `EnableOptionInfo`가 꺼져 있거나 설정을 읽지 못하면 예약을 적용·소비하지 않는다.

## 수명과 최종 검증

예약은 메모리 상태이며 cfg·세이브에 쓰지 않는다. 기존 미션 예약처럼 일반적인 이야기/Lua 교체에는 유지하고, 로드·새 게임 등 `LogBridge.EnterTransition`으로 처리하는 실제 실행 전환에서 해제한다. UI에 보이는 목록·초점·확정 누름은 Lua 문맥이 바뀔 때 별도로 무효화한다.

예약 등록 때 보았던 선택을 실행 경계에서도 검증한다.

- 공통: 현재 관리자, 설정 컬렉션, 체크포인트 객체와 이름의 동일성.
- 이벤트: 후보가 실제 목록에 속하는지, 후보 객체·스크립트 접두사·결과가 그대로인지.
- 조건: 분기 모드·실행 횟수·결과 번호 및 해당 번호의 조건/횟수 객체 또는 fallback 대상의 동일성.
- 소비: 현재 저장된 예약 객체를 한 번만 가져온다. 소비 후 같은 행을 다시 예약해도 이전 누름이나 이전 객체로 새 예약을 취소할 수 없다.

대상·결과·횟수 등이 바뀐 예약은 폐기한다. 실행 경계에서 발견하면 원래 판정으로 진행하며 다른 결과를 대신 채택하지 않는다. 지원하는 Lua 팝업 진입 전에 오래된 예약을 발견했다면 이를 버리고 현재 팝업 설정을 따른다. 이미 소비한 결과는 예약 해제로 되돌리지 않는다.

## 팝업 지원 범위와 예약 지원 범위

기존 팝업의 지원 범위는 넓히지 않는다. 이벤트는 지원하는 비메인 Lua의 직접 리터럴 `Position`, 조건은 직접 리터럴 Switch를 지역 변수에 대입한 직후 정수 동등 비교 `if/elseif/else`로 분기하는 형식에 표시한다. 실제 Switch 결과와 해당 Lua 지점이 처리하는 분기가 둘 이상이어야 한다.

예약은 대기·yield가 필요하지 않아 실제 `CheckPointManager.Position`·`Switch` 경계에서 소비한다. 따라서 팝업이 지원하지 않는 Lua 형식·메인 Lua·직접 C# 호출도 일치하는 유효 예약이 있으면 사용할 수 있다. 이 사실이 해당 호출 뒤 Lua가 예약한 결과를 반드시 처리하거나 원하는 장면을 보여준다고 보장하지는 않는다. 같은 체크포인트를 여러 지점에서 호출하면 다음 일치 호출이 예약을 소비한다.

## 구현 근거와 변경 범위

| 파일 | 역할 |
| --- | --- |
| [CheckpointReservation.cs](../src/DiceMaster/EventSelection/CheckpointReservation.cs) | 조건·이벤트의 독립된 불변 예약, 새 등록마다 새 객체를 만드는 한 번 소비 저장소 |
| [EventSelectionRuntime.Reservations.cs](../src/DiceMaster/EventSelection/EventSelectionRuntime.Reservations.cs) | 실제 실행 경계 검증·소비와 대기 중 팝업보다 예약을 우선하는 처리 |
| [EventSelectionRuntime.cs](../src/DiceMaster/EventSelection/EventSelectionRuntime.cs)·[Switch partial](../src/DiceMaster/EventSelection/EventSelectionRuntime.Switch.cs) | 기존 Wait/Begin 경계에서 예약 처리 연결. 기존 직접 결과 반환·Switch 횟수 한 번 증가 재사용 |
| [EventExecutionPatches.cs](../src/DiceMaster/EventSelection/EventExecutionPatches.cs) | 기존 실제 관리자 호출 Prefix와 결과 Prefix를 그대로 사용 |
| [LogBridge.cs](../src/DiceMaster/LiveLog/LogBridge.cs) | 분기별 게시·소속/선택 검증·예약 표시와 해제·실제 전환에서 초기화 |
| [LogModel.cs](../src/DiceMaster/LogUI/LogModel.cs)·[LogRenderer.cs](../src/DiceMaster/LogUI/LogRenderer.cs) | 미션의 예약 입력·초점·클릭/터치 트랜잭션을 조건·이벤트에 확장 |
| [ConfigManager.cs](../src/DiceMaster/Config/ConfigManager.cs)·[배포 cfg](../packaging/config/DiceMaster.cfg) | Switch 신규 기본값 OFF, 기존 저장값 보존 |

구조 리팩터링 3단계의 요청 필드 묶음 정리는 이번 범위에 포함하지 않는다. 기존 Lua 팝업 변환기·미션 조건 통과 방식·주사위 기능과 단일 DLL 배포 형태를 유지한다.

## 검증 기록

후보 검사 당시인 2026-10-20 05:29:55 KST에 .NET SDK 8.0.426으로 전체 소스를 빌드하고 **14개 검사 프로그램**을 통과했다. 188개 빌드 입력·78개 소스/리소스의 해시를 대조하고 설치 ZIP의 CRC·두 파일·DLL/cfg 바이트 일치를 확인했다. 이 시점의 후보 검사는 이후 고정 커밋 재빌드 및 실제 Windows 게임 실행과 구분한다.

| 검사 | 현재 기록 |
| --- | --- |
| EventSelectionRuntimeTests | 198개 시나리오·1,140개 단언 통과. 현재 후보 DLL·프로덕션 런타임·저장소와 제공된 MoonSharp 사용 |
| LogUiKeyboardTests | 5,771개 검사 통과 |
| LogUiModelTests | 421개 단언 통과 |
| LiveLogIntegrationTests | 현재 DLL의 브리지·예약·분기별 게시·전환 경계 431개 단언 통과 |
| 전체 소스 후보 빌드·14개 검사 프로그램 | 전체 통과. 로그 갱신·수명·게임 API 경계·패드·번역·미리보기·GUI·열기·예약 런타임·팝업 포함 |
| 빌드 입력·설치 ZIP | 188개 입력 해시·ZIP CRC·정확히 두 파일·DLL/cfg 바이트 일치 |
| 실제 Unity/Windows·물리 패드·트랙패드·모니터 터치 | 미실행 |
| 고정 커밋 재빌드·새 prerelease·공개 다운로드 대조 | 소스 `11a6fb4`의 별도 스냅샷에서 14개 검사와 188개 입력·DLL/cfg/ZIP·전체 결과 일치. 05:36:03 KST 게시 후 익명 공개 ZIP CRC·두 파일·바이트·해시 통과 |

현재 후보는 `artifacts/checkpoint-reservations/full/candidate/`에 보존하며 기준 Git HEAD `1f189a9` 이후의 작업 트리를 빌드했다. 후보·검사 영수증은 `build-manifest.json`·`build.log`와 상위 `verification.json`에 있다. 이 후보는 alpha.12 고정 재빌드·익명 공개 파일과 바이트·해시가 일치하며 alpha.11과 같은 파일이 아니다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `DiceMaster-source-candidate.zip` | 267,579 | `76d2f8e350b5eab4a57a628a245714705ab6b86f7675db7f7d79e6f8fdb2fc05` |
| `DiceMaster.dll` | 634,368 | `7db4b6dbffd3ed4f4441002fa7c868d5d66db234942395178de5cf4ad6fc802f` |
| `DiceMaster.cfg` | 11,761 | `435d221a32f505ce079c01c4afb9ecf39f4bbe29d5dd8d7ca8e3975f2a9b5400` |

업데이트는 게임 종료 후 기존 DLL/cfg를 백업하고 후보 DLL만 교체해 설정을 유지한다. 이미 저장된 Switch 팝업 ON을 끄려면 조건 탭의 설정 버튼을 사용한다. 최초 설치만 ZIP의 두 파일을 기존 BepInEx 아래에 넣는다.

런타임 검사는 팝업 ON/OFF와 예약 우선순위, 대기 중 팝업 대체, 실제 MoonSharp 재개, 메인/지원 밖 Lua·직접 호출, 조건/RNG 조회 0, Switch 횟수 한 번 증가, 중복 소비 방지, 다른 판정·종류 격리, 일반 Lua 무효화 뒤 유지, 변경된 대상·결과·설정·횟수 폐기, 상위 설정 OFF/읽기 실패, 다른 모달 소유권 보호와 재등록 객체의 ABA 거부를 포함한다. [ReservationExecutionRegression.cs](../tests/EventSelectionRuntimeTests/ReservationExecutionRegression.cs)

alpha.12의 실제 Windows 확인 순서와 환경·파일 식별값은 [예약·기본값 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha12-예약기본값-변경-확인표)에 기록한다. 개별 관리 코드 검사를 실제 장치·게임·다른 모드의 통과로 확대하지 않는다.
