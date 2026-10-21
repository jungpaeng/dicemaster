# 선택지 안내 — 완료된 진입 대화와 반복 활동의 고정 반응

## alpha.23 후속 — 실제 로드 경계의 원문 손실

<!-- alpha23-publication -->
2026-10-22 03:06:00 KST에 소스 [`fc5dc7f`](https://github.com/jungpaeng/dicemaster/commit/fc5dc7fabc0977b01ee6acdb92a3987148e77370)의 [alpha.23](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.23)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha23-publication -->

아래 원문 기반 검사만으로는 #5가 변환한 컴파일 인수를 뒤늦게 관찰하는 경계를 검증하지 못했다. 실제 변환문에서 완료 인사 잔존을 재현하고 호출별 원문 캡처로 보완한다. [추가 원인과 검증](ISSUE-6-CHOICE-LOAD-SOURCE.md).


<!-- alpha22-publication -->
2026-10-22 02:45:06 KST에 소스·주석 태그 [`bbf7024`](https://github.com/jungpaeng/dicemaster/commit/bbf7024db49acd271061f2903ae2b12e9a44e77e)의 [alpha.22](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.22)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·274개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 63개 Release·64개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·화면·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA22-RELEASE.md).
<!-- /alpha22-publication -->

후속은 [현재 선택의 직접 대화 우선 조회](ISSUE-6-DIRECT-CHOICE-PRIORITY.md)를 보완한다. 아래 이전 후보의 검증을 새 Windows 확인 근거로 사용하지 않는다.

<!-- alpha21-publication -->
2026-10-22 02:19:17 KST에 소스·주석 태그 [`9163d59`](https://github.com/jungpaeng/dicemaster/commit/9163d5931a9e95bb614f77dcfb48bbb45519b376)의 [alpha.21](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.21)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·273개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 62개 Release·63개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA21-RELEASE.md).
<!-- /alpha21-publication -->

후속 작업은 [선택 안내 근거·활동 독립성](ISSUE-6-CHOICE-EVIDENCE.md)을 보완한다. 아래 이전 후보 기록을 새 Windows 확인 근거로 사용하지 않는다.

<!-- alpha20-publication -->
2026-10-22 01:49:00 KST에 소스·주석 태그 [`07363bc`](https://github.com/jungpaeng/dicemaster/commit/07363bce02119bd9734d1b69c0440c613885fcb8)의 [alpha.20](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.20)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·272개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 61개 Release·62개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA20-RELEASE.md).
<!-- /alpha20-publication -->

## 증상과 원인

남제자방의 `사사형`에 `경로 후보: 사사형 · 사사형 대화 · 진행 가능성 있음`이 남아도 그 근거가 반드시 대련은 아니었다. SHA-256을 대조한 원본 `room_001`에서 대련을 닫아도 같은 문구가 재현됐다. 선택된 근거는 `Talk_Brother4_00`의 1번 분기에 있는 진입 대사 `D_Room_001_001`이었다. 기존 읽기 전용 조건기는 횟수형 Switch를 항상 미확정으로 두므로 이미 끝난 초회 인사도 남을 수 있었다. 이것은 사용자 세이브의 현재 횟수 값을 직접 확인한 결과가 아니라 원본 Lua에 합성 직렬화 조건을 연결한 재현이다.

별도로 실제 `talk_brother4`의 반복 대련도 남을 수 있었다. 첫 선택 메뉴 이전의 알려진 화면 준비 호출이 반복 활동의 구조 판정을 흐렸고, 고정 반응을 제외한 뒤의 보수적 fallback 또는 큰 경로 묶음이 같은 고정 반응을 다시 이야기로 고를 수 있었다. 두 원인을 함께 보완한다.

## 구현과 보수적 경계

- 횟수형 Switch 전체를 추측하지 않는다. 닫힌 루트 선택 이후, 선행 Switch·게임 상태 쓰기·불명 호출이 없고 첫 호출 하나임을 확인한 구간에만 `NextSwitch` 표본을 붙인다. 저장된 `_count + 1`을 `_countItems`의 여섯 정수 비교에 대입하고 첫 일치 순서와 기본 분기 규칙을 읽는다. 원래 `Switch`·`Execute`·`Check`·Lua·RNG는 호출하지 않고 `_count`도 쓰지 않는다.
- 같은 식의 복수 호출, 앞선 호출·불명 효과, 누락·손상 자료, 음수/오버플로 횟수에는 이 표본을 적용하지 않는다. 일반 분석의 횟수형 Switch는 계속 미확정이다. 저장된 바인딩을 뒤에서 새 판정으로 다시 쓰지 않는다.
- 이 표본은 현재 필수 조건의 불가 확인용이다. 끝난 진입 대화는 자동 툴팁에서 제외한다. 진입 대화의 횟수·완료 근거를 확인할 수 없을 때도 자동 안내를 생략하고 상세 사유를 제공한다. 정의를 읽을 수 있는 일반/RNG Switch의 미확정은 계속 `진행 가능성 있음`으로 구분한다. 실제로 남은 초회 대화와 다른 해금된 주제는 유지한다.
- 이미 인식하던 순수 화면 준비 문법을 루트 메뉴 전에도 보존해 반복 활동 분류와 현재 불가 표본이 불필요하게 손상되지 않도록 한다. 알 수 없는 호출·재바인딩·게임 상태 변경은 계속 경계로 남는다. 전체 미래 실행을 안전하다고 승격하지 않는다.
- 최종 표시 경로를 고를 때도 각 경로의 반복 활동 판정을 확인한다. 고정 반응은 fallback이나 큰 묶음에서 다시 자동 툴팁·기본 연결 경로로 돌아오지 않는다. 활동이 자신의 해금 조건을 소모하거나 별도 조건부 이야기가 있는 경우까지 일괄 제거하지 않는다. 특정 인물·조건 키 예외 목록은 없다.

기존 미래 선택지 해금 확인과 지도/선택 마커의 대기 임무 또는 엄격한 1회성 증명·현재 조건·경로 기준을 유지한다. 이번 변경이 일반 대화나 횟수형 조건에 금색 배지를 새로 부여하지 않는다. 미지원·상한·누락 경로의 모든 이야기를 탐지한다는 보장은 없다.

## 성능·입력·설정

정의 준비는 기존 캐시와 작업 슬라이스에서 수행한다. 갱신당 기존 32개 미캐시 선택 조건/분류 예산, 32개 표시 경로·16단계 상한과 조건기 작업 상한을 유지하고 횟수 비교 목록은 최대 256개다. 완료 근거 조회도 동일 예약 갱신의 Reader/조건/체크포인트 캐시를 공유하며 결과 값을 다음 갱신에 보관하지 않는다. 정상 0.25초·실패 5초, 숨김/OFF 중단, 렌더링 중 조회 없음, 정상 주기 Lua 재분석 없음 계약을 유지한다.

새 설정·cfg 변경은 없다. 기존 EnableMapGuide가 선택 경로도 제어하며 다른 세 기능의 설정·상위 EnableOptionInfo·OFF/기록 초기화 계약을 보존한다. 기존 패드/키보드/포인터/터치·입력 소유권·해제/중립·네 로그 탭·이벤트/예약·주사위·상점·분기 기록을 유지한다.

## 근거와 검증

- 원본 `room_001`: `artifacts/real-lua-followup/reproduced-corpus/text/id_1896.txt` 29–30행의 초회 Switch, 112행의 진입 대사. SHA-256 `106d054c345c97060b627bf0dac03dcb8046de38eed03397598f08a58e44be10`.
- 원본 `talk_brother4`: 같은 디렉터리의 `id_2658.txt` 33행의 대련 해금과 211–221행의 연결. SHA-256 `3aeebe196f0e81cd147a005bf37688c128f84696880bf76dc3421f1454fa51f3`.
- 원본 반복 대련: `id_1716.txt`, SHA-256 `74acc640cc9495ce94f7658381d1e023a889674dcd3a9c0907ed53ddc4dea72c`.
- 실제 게임 참조의 `Mortal.Story.SwitchResultData.Execute`는 횟수를 먼저 증가시키고 `_countItems`를 순서대로 비교한다. 고정 참조 계약과 기존 RealGameApiTests/이벤트 실행 검사, 새 실제 후보 DLL IL의 NextEntryCount 회귀 검사로 비교 순서·읽기 전용 경계를 확인한다.
- `tests/ChoiceStoryTests/EntryDialogueCases.cs`: 원본 SHA 검증·초회 남음/완료 전환·대련만 남은 메뉴·모든 정수 비교·누락/복구·중복 호출·선행 불명 효과·정상 난수·정상 주기 캐시/횟수 무변경을 검사한다. 원본이 언급한 인물 이름을 생산 코드에 하드코딩하지 않는다.

전체 소스 및 고정 커밋 빌드·실제 DLL 연결·설치 ZIP·공개 다운로드의 결과는 [alpha.20 게시 기록](ISSUE-6-ALPHA20-RELEASE.md)에서 단계별로 구분한다. [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha20--완료-진입-대화반복-활동-확인-대기).
