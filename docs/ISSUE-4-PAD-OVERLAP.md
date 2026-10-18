# PRD #4 후속: 시간 제한 없는 양쪽 스틱 클릭 겹침

이 문서는 로컬 후보 생성 당시 기록입니다. 이후 사용자 지시에 따라 **2026-10-19 06:50:38 KST에 [v1.0.5-alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.7)로 커밋·게시**했으며 [최신 설치·해시·복구·Windows 확인](ISSUE-4-ALPHA7-RELEASE.md)을 따릅니다. 아래 개별 후보 경로·수치와 당시 미게시 설명은 이력으로 보존합니다.

2026-10-19 KST. 사용자 후속 지시에 따라 패드 로그 열기의 **300ms 제한을 제거**한다. 같은 패드에서 어느 한쪽 스틱 클릭을 먼저 누르고 계속 유지하다가 반대쪽도 눌러 두 클릭이 겹치면 닫힌 로그를 연다. 누르는 순서는 자유이며 동시 클릭도 가능하다.
이 변경은 로컬 후속이다. 공개 [v1.0.5-alpha.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.6), [기존 패드 열기 후보](ISSUE-4-PAD-OPEN.md)의 300ms 계약·검사 수·해시는 당시 기록으로 보존한다. 2026-10-19 06:29:49 KST에 새 로컬 후보를 생성했고 전체 소스 빌드·12개 검사 프로그램을 통과했다. 이번 후보의 검사·해시는 아래에 별도로 기록한다.
게임 설치·실행·커밋·공개 게시를 수행하지 않으며 실제 Windows 확인과 이슈 #4 완료는 사용자 대기다.

## 바뀌는 조합 판정

- 같은 패드에서 **L3 유지 → R3 추가**, **R3 유지 → L3 추가**, **L3+R3 동시 클릭**을 모두 받는다. 스틱 기울이기와 스틱 클릭은 구분한다.
- 첫 클릭과 두 번째 클릭 사이의 고정 시간 제한은 없다. 첫 클릭을 길게 누른 뒤 반대 클릭을 추가해도 게임 폴링·초점·연결·차단 준비가 계속 정상이고 다른 취소 조건이 없으면 연다.
- 두 클릭이 겹쳐야 한다. 먼저 누른 클릭을 뗀 뒤 반대쪽만 누르는 연속 클릭이나 서로 다른 패드의 클릭을 합치지 않는다.
- **열기 전용**이다. 이미 열린 로그에서 조합을 유지하거나 다시 누른다고 닫기·예약이 발생하지 않는다. 닫기는 기존 B 복귀·닫기, 설정 키와 화면의 닫기 버튼을 사용한다.

게임 폴링의 기존 **1초 신선도 보호는 유지**한다. 이는 첫 클릭 후 두 번째 클릭을 누를 수 있는 시간이 아니다. 게임의 실제 XInput 폴링이 끊기거나 오래된 입력 상태가 되면 후보를 취소하고 해제·중립을 다시 기다리는 보호다. 정상 폴링이 이어지는 동안 첫 클릭을 1초 이상 유지하는 것은 허용한다.

## 유지하는 설정·소유권·취소 계약

`[DiceMaster].EnableGamepadLogOpen=true`의 기본값과 `EnableGamepadAdjustment`·`EnableDiceController`로부터의 독립성을 유지한다. 기존 cfg는 그대로 사용하며 새 설정 키는 추가하지 않는다. 기존 `[DiceMaster].LogMenuToggleKey`(기본 Tab)도 유지한다.

**옵션 켜짐·게임 창 초점·로그 닫힘에서는 단독 L3/R3도 게임으로 보내지 않는다.** 첫 클릭이 게임 기능에도 전달되는 일을 막기 위한 기존 예약이다. `EnableGamepadLogOpen=false`에서는 이 예약을 해제하고, 남아 있는 클릭 누름은 해제한 뒤 게임에 반환한다. 초기화·재설정의 전체 중립 대기 중에는 다른 버튼·스틱·트리거의 남은 입력도 소비한다. 게임의 직렬화된 바인딩 자료가 없어 원래 L3/R3 기능이 미사용이라고 확정하지 않는다.

같은 입력 소유자와 전체 해제·스틱 중립·트리거 해제 뒤의 새 입력만 사용한다. 다른 버튼, 중립 범위를 벗어난 스틱, 눌린 트리거가 동반되면 조합 후보를 취소하고 전체 해제·중립을 다시 기다린다. 초점 상실·연결 끊김·입력 차단 준비 상실·오래된 폴링 상태에도 같은 보호를 적용한다. 창을 닫은 뒤 누름이 남아 있어도 자동으로 다시 열지 않는다.

열린 뒤에도 조합을 모두 해제·중립으로 돌린 다음 새 탐색 입력을 받는다. 한 번의 겹침과 홀드로 반복해서 열거나 게임 대화·취소·주사위 조정·예약에 중복 전달하지 않는 계약을 유지한다. 패드 열기 의도·포인터/터치 전환과 게임 모드를 강제 변경하지 않는 기존 동작도 유지한다.

**Tab 진입의 기존 모드 전환·첫 패드 입력 소비 문제는 이번 시간 제한 제거의 수정 범위가 아니다.** 패드 조합 경로와 별도로 기록한다. 지원되는 실제 XInput 폴링·차단 경로에서 사용하며 실패·미지원 시 기존 Tab·마우스·키보드로 접근한다.

## 후보·자동 검사

전체 C# 소스와 읽기 전용 외부 게임 참조를 사용한다. 게임 소스·DLL을 저장소나 설치 ZIP에 추가하지 않는다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/pad-overlap
```

| 산출물 | 경로 | 결과·크기·SHA-256 |
| --- | --- | --- |
| 후보 DLL | [`DiceMaster.dll`](../artifacts/issue-4/pad-overlap/DiceMaster.dll) | 582,656바이트 · `c14dfdff3cc599f09231a85c6ec3ba658cfc95d1c4035248481a08eb66a96825` |
| 후보 ZIP | [`DiceMaster-source-candidate.zip`](../artifacts/issue-4/pad-overlap/DiceMaster-source-candidate.zip) | 245,027바이트 · `8a210fa221d92d3ecf42ca7670b1c5447205cfba0f6b3e08fb7438f565e688e6` |
| ZIP 내부 cfg | `config/DiceMaster.cfg` | 11,175바이트 · `bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0` |
| 빌드 manifest | [`build-manifest.json`](../artifacts/issue-4/pad-overlap/build-manifest.json) | 생성 UTC `2026-10-18T21:29:49.671724+00:00` · KST 2026-10-19 06:29:49 |
| 빌드 로그 | [`build.log`](../artifacts/issue-4/pad-overlap/build.log) | 전체 소스 빌드·12개 검사 프로그램 통과 |
| 독립 검증 | [`local-verification.json`](../artifacts/issue-4/pad-overlap-evidence/local-verification.json) | 입력 153개·생산 소스 63개 일치, ZIP CRC·정확히 두 파일·DLL/설정 바이트·해시와 사전 검토 DLL 일치 |

새 manifest에 기록된 **12개 검사 프로그램 모두 통과**했다. 공개 alpha.6의 검사 수를 승계한 결과가 아니다.

| 검사 프로그램 | 이번 후보 결과 |
| --- | --- |
| LogRefreshTests | 19개 단언 통과 |
| LiveLogEngineTests | 16개 시나리오·70개 단언 통과 |
| LifecycleTests | 4,023개 단언 통과 |
| LiveLogIntegrationTests | 실제 helper·게시·모델 연결 203개 단언 통과 |
| RealGameApiTests | 관리 동작 3,914개·메타데이터 계약 1,047개 단언 통과 |
| GamepadTests | 262,831개 단언 통과 |
| SourceBehaviorTests | 2,170개 단언 통과 |
| PreviewIntegrationTests | 1,453개 단언 통과 |
| LogUiModelTests | 372개 단언 통과 |
| LogUiWiringTests | 실제 후보 DLL GUI·게시·입력·설정 연결 48개 단언 통과 |
| LogUiKeyboardTests | 관리 대역 UI·입력 2,720개 검사 통과 |
| LogOpenerIntegrationTests | 실제 native hook/filter/bridge/controller/action gate 소스 연결 35개 격리 시나리오·1,408개 단언 통과 |

양쪽 순서·동시 입력·긴 첫 클릭 홀드·겹치지 않는 클릭·다른 소유자·동반 입력 취소·중립 재설정·폴링 중단·초점/연결·닫기 홀드·반복 방지와 실제 DLL의 설정/입력 연결을 검사했다. 실제 시간 기준으로 각 순서의 첫 클릭을 1.28초씩 유지해 합계 2.56초의 홀드 경로를 확인했다. 순수 입력 모델의 60Hz·최대 60초 상당 이력은 폴링 이력 스트레스 검사이며 실제 60초 게임/장치 홀드 검사가 아니다.

별도 임시 소스 복사본에 기존 300ms 판정만 되살린 회귀 실험은 컴파일을 통과한 뒤 긴 홀드의 실행 단언에서 실패했다. 세 인수 호출 형태를 유지해 컴파일 실패 대신 실제 시간 제한 퇴행을 검출했으며 원본 소스는 바꾸지 않았다. 실험 기록은 `artifacts/issue-4/pad-overlap-evidence/timeout-regression/mutation-result.json`에 별도로 보존한다.

게임·Win32·원래 XInput export·Unity 경계는 명시적인 관리 대역이다. 자동 검사만으로 실제 패드 탐색·스크롤·버튼·후킹 성공이나 이슈 #4 완료를 주장하지 않는다.

## 적용·복구

1. 후보 생성·해시 검증이 끝난 파일을 사용한다. 게임 종료 후 현재 DLL과 `BepInEx/config/DiceMaster.cfg`를 게임 폴더 밖에 백업한다.
2. 후보 ZIP을 임시 폴더에 풀고 **`BepInEx/plugins/DiceMaster/DiceMaster.dll`만 교체**한다. 기존 cfg를 보존하며 새 cfg 전체로 덮어쓸 필요는 없다.
3. 전체 해제·중립 후 긴 홀드와 양쪽 순서를 확인한다. 패드 열기를 끄려면 게임 종료 후 기존 `[DiceMaster]`의 `EnableGamepadLogOpen=false`로 바꾸고 재시작한다. 남은 누름의 해제 보호는 유지한다.
4. 문제 시 게임 종료 후 백업 DLL·필요한 cfg를 복원한다. 공개 [alpha.6 설치·해시](ISSUE-4-ALPHA6-RELEASE.md)로 복구하면 패드 열기는 기존 **300ms 미만 겹침** 계약으로 돌아간다. 기존 Tab도 계속 사용할 수 있다.

```powershell
Get-FileHash .\DiceMaster-source-candidate.zip -Algorithm SHA256
Get-FileHash .\plugins\DiceMaster\DiceMaster.dll -Algorithm SHA256
```

## Windows 확인표

모든 항목은 **사용자 확인 대기**다. 결과에 DLL SHA-256, 패드·Steam Input·게임/로더 환경, 설정값·재현 순서·관련 BepInEx 로그를 남긴다. 게임 설치·실행은 사용자가 직접 수행한다.

| 순서 | 조작 | 확인할 결과 |
| --- | --- | --- |
| 1 | 게임 폴링이 이어지는 전경에서 중립 → L3를 2초 이상 유지 → R3 추가 | 긴 간격에도 한 번 열림. 첫 클릭과 조합이 게임에 중복 전달되지 않음 |
| 2 | 중립 → R3를 2초 이상 유지 → L3 추가, 이후 동시 클릭 | 반대 순서·동시 클릭도 한 번 열림. 두 클릭의 순서 제한 없음 |
| 3 | L3를 뗀 뒤 R3만 누르기, 두 패드에서 한쪽씩 클릭 | 겹침·같은 소유자 조건이 없으므로 열리지 않음 |
| 4 | 첫 클릭 홀드 중 다른 버튼·스틱 기울임·트리거 동반 후 반대 클릭 | 후보 취소. 다른 입력을 떼기만 해서는 뒤늦게 열리지 않고 전체 중립 뒤 새 조합 필요 |
| 5 | 첫 클릭 홀드 중 Alt-Tab·연결 끊김·로드 등으로 폴링/초점/준비 경계 통과 | 오래된 후보로 열리지 않음. 복구·중립 뒤 새 조합만 허용. 단축키 시간 제한과 폴링 중단을 구분 |
| 6 | 창을 연 조합을 유지하고 B·설정 키·닫기 버튼으로 닫기 | 누름이 남아 있는 동안 재열기·예약·게임 대화/취소·주사위 조정 없음 |
| 7 | 열린 뒤 해제·중립 → 첫 새 방향·A·Y, 반복 열기/닫기 | 첫 새 탐색 입력을 추가로 잃지 않고 조합 홀드 자체는 탐색·예약이 되지 않음 |
| 8 | 옵션 켬의 단독 L3/R3, 옵션 `false` 재시작, 주사위 옵션 끔/켬 | 단독 클릭 예약 제약과 해제 후 복구, 옵션 독립 유지 |
| 9 | 기존 Tab·사용자 지정 키, 터치·휠·클릭·탭·예약·주사위 왕복 | 기존 입력·화면 보호 유지. Tab 진입의 첫 패드 입력 문제는 별도로 결과 기록 |

기존 읽기 화면·조건 분기·미션 상태·예약·자동 갱신 회귀는 [alpha.6 확인표](ISSUE-4-ALPHA6-RELEASE.md#windows-확인표)를 함께 사용한다. 실제 Windows 확인을 마치기 전 이슈 #4를 완료 처리하지 않는다.
