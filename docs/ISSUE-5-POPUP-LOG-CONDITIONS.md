# 선택 팝업의 로그 전환과 조건 분기 표시

상태: **2026-10-20 12:28:24 KST v1.0.6-alpha.14 prerelease 게시 완료 · 실제 Windows·장치 확인 대기**. 소스·주석 태그는 `3c9aa94512add361818511fe1f57d5987a5855d2`다. 후보·고정 소스에서 각각 14개 검사와 동일 입력·파일 바이트를 확인하고 익명 공개 파일을 대조했다. [배포·파일 기록](ISSUE-5-ALPHA14-RELEASE.md) · [실제 Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md#alpha14-팝업로그-포커스조건-표시-확인표) · [현재 안내](../README.md#시험판의-조건이벤트-선택).

## 화면과 조작

팝업 상단의 **로그 열기**를 누르거나 `LogMenuToggleKey`(기본 Tab)로 큰 로그를 연다. `EnableGamepadLogOpen=true`이면 팝업 대기 중에도 같은 패드 L3/R3 클릭 겹침으로 연다. 새 조합·시간 제한·설정 키를 추가하지 않는다. OFF는 패드 열기만 끄며 이미 열린 로그·대기 팝업의 입력 차단과 탐색을 끄지 않는다.

큰 로그가 열리면 로그만 입력을 받는다. 로그를 닫을 때까지 대기 중인 팝업의 Lua 흐름과 그 판정에서의 예약 소비·설정 OFF 후 원래 판정 실행을 보류한다. 단순 열기·닫기로 결과를 확정하지 않는다. 닫은 뒤 유효한 팝업 선택과 읽던 위치를 유지하며, 닫기에 쓴 B/Esc나 누른 채인 A를 다음 실행에 재사용하지 않는다. 버튼 해제·스틱/트리거 중립 뒤 새 입력을 받는다. 실제 문맥·대상 교체의 취소는 유지한다.

조건 분기 목록은 **굵은 제목·결과 요약·조건 요약**의 세 줄이다. 긴 조건은 목록에서 줄이고 선택 상세의 **분기 조건 → 전체 조건 → 예상 결과** 순서로 모두 읽는다. 이벤트 팝업의 기존 두 줄과 상세 순서는 유지한다. 전체 상세는 휠·오른쪽 스틱으로 스크롤하고 ↑↓는 후보를 이동한다.

| 상태 | 조건 분기 표시·조작 |
| --- | --- |
| 충족 | 기존 일반 진행 |
| 확실한 미충족·앞 분기 우선 | 제목·조건 회색, 읽기 가능한 결과는 A/Enter·선택 행의 다음 새 터치로 명시 조건 무시 |
| 난수·미지원 | 판정 미확인 설명과 조건을 유지. 미충족으로 단정해 회색 처리하지 않음 |
| 실제 대상·결과 읽기 실패 | 얻은 설명은 보존하지만 일반·조건 무시 실행을 막음 |

결과 요약·상세의 기존 효과색은 유지한다. 조건은 문자로 표시하고 태그 같은 문자열을 서식으로 해석하지 않는다. 앞 분기 우선순위와 기본 분기 조건, 횟수 모드의 **이번 실행 횟수**와 비교식을 보존한다. 회상·조건 설명에 내부 ID가 남는 문제와 모든 미래 분기의 확정은 이번 범위에 포함하지 않는다.

## 구현 경계와 회귀 근거

- [DicePlugin.cs](../src/DiceMaster/DicePlugin.cs): 로그 열기 콜백·설정 키·패드 열기를 같은 전환에 연결한다. 로그 가시성을 런타임에 전달하고, 큰 로그가 열려 있으면 팝업 갱신·입력 대신 로그만 처리한다. 그리기도 한 화면만 처리한다.
- [EventPopupRenderer.cs](../src/DiceMaster/LogUI/EventPopupRenderer.cs): 로그 콜백을 GUILayout 종료 뒤 실행한다. 기존 선택·스크롤을 초기화하지 않고 포인터 입력 권한만 전환하며, 로그 열기와 결과 확정의 누름을 구분한다. 조건 요약은 고정 세 줄·전체 조건은 상세 앞부분에 배치해 선택 전후 행 좌표를 유지한다.
- [EventSelectionRuntime.cs](../src/DiceMaster/EventSelection/EventSelectionRuntime.cs)·[Switch.cs](../src/DiceMaster/EventSelection/EventSelectionRuntime.Switch.cs): 로그가 열린 동안 신규 팝업 선택/원래 판정 요청을 거부하고, 대기 중인 팝업의 Lua 흐름과 그 판정에서의 예약 소비를 닫기 전 보류한다. 설정 OFF로 내부 해제 상태가 바뀌어도 그 판정·효과 실행은 닫기 전 진행하지 않는다.
- [Switch.cs](../src/DiceMaster/EventSelection/EventSelectionRuntime.Switch.cs): 정상·미충족·난수·미지원·읽기 실패 경로에서 확보한 조건 표현식을 유지한다. 앞 분기의 실패가 뒤 분기의 조건 설명까지 지우지 않게 하고, 기본 분기와 실제 비교식을 별도로 설명한다. 판정 미확인의 이유를 미충족과 구분한다.
- [GamepadBridge.cs](../src/DiceMaster/Gamepad/GamepadBridge.cs)·[NativeXInput.cs](../src/DiceMaster/Gamepad/NativeXInput.cs)·[PadController.cs](../src/DiceMaster/Gamepad/PadController.cs): 이미 입력을 소유한 팝업에서도 설정된 L3/R3 열기를 허용하되 게임에는 전달하지 않는다. 로그 열기 옵션 OFF·초점 상실·해제/중립 계약을 유지한다.

관리 경계 검사는 실제 소스·후보 DLL의 연결과 제공된 MoonSharp를 사용한다. Unity 네이티브 배치·Windows XInput·물리 장치를 실행한 검사는 아니다.

| 검사 | 이번 후보 결과 |
| --- | --- |
| EventSelectionRuntimeTests | 실제 MoonSharp 233개 시나리오·1,359개 단언. 로그 열림 중 이벤트/Switch 대기·예약/설정 OFF와 닫은 후 한 번 실행, 조건 설명·우선순위·횟수·미확인 구분 |
| EventPopupTests | 1,807개 검사. 마우스·마우스 인식 터치·직접 터치, 로그 열기 지연 콜백·중복/오래된 누름 차단·선택/스크롤 보존, 조건 앞 배치·회색/미확인·효과색, 네 해상도×세 글자 크기의 긴 조건 배치 |
| LogOpenerIntegrationTests | 43개 시나리오·1,839개 단언. 실제 훅/필터/브리지/컨트롤러 소스 연결, 캡처 중 패드 로그 열기·설정 OFF·초점·입력 차단 경계 |
| LogUiWiringTests | 실제 후보 DLL 연결·게시·입력 경계 100개 단언 |
| 전체 소스 후보 | 2026-10-20 12:23:20 KST, SDK 8.0.426. 14개 프로그램 전체 통과, 191개 빌드 입력·78개 소스/리소스와 ZIP CRC·정확히 두 파일·내부 바이트 대조 |
| 고정 커밋 재빌드·게시·공개 파일 | `3c9aa94512add361818511fe1f57d5987a5855d2`의 별도 소스 스냅샷 재빌드·14개 검사·191개 입력·78개 소스/리소스·후보 파일과 전체 검사 결과 대조 통과. 2026-10-20 12:28:24 KST 게시 후 익명 공개 ZIP의 CRC·두 파일·내부 바이트·해시 일치 |
| 실제 Windows·물리 장치 | 미실행 |

후보 결과는 로컬 `artifacts/popup-log-conditions/full/candidate/build-manifest.json`, 전체 로그는 `artifacts/popup-log-conditions/full/build-run.log`, 입력·패키지 대조는 `artifacts/popup-log-conditions/full/verification.json`에 보존한다. 고정 소스·게시·익명 검증 영수증은 `artifacts/releases/v1.0.6-alpha.14/`에 보존하며 공개 파일 해시와 게시 상태는 [alpha.14 기록](ISSUE-5-ALPHA14-RELEASE.md)에 남긴다. 고정 소스 문서와 검사 영수증의 미게시 표시는 게시 전 검사 시점이다.
