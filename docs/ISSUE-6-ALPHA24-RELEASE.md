# v1.0.7-alpha.24 — 직접 대화 안내 먼저 표시

<!-- alpha24-publication -->
전체 소스 빌드의 자동 검사 21개·276개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·설치 ZIP 검증을 통과했다. 고정 커밋 재빌드·게시·공개 다운로드는 후속 기록한다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha24-publication -->

현재 선택의 직접 대화가 준비되면 먼저 표시하고, 후속 선택 경로는 준비 뒤 기존 해금 검사를 거쳐 추가한다. 기존 하나의 작업/읽기 예산을 공유하고 완료·미확정 완료·반복 활동·잠긴 후속 경로와 기회 마커 기준을 완화하지 않는다. 루트 중복 메뉴·생략 본문·동적 라벨·상한은 먼저 표시하는 유일성 근거로 사용하지 않는다. [설계·원본 재현·성능 교환](ISSUE-6-LOCAL-CHOICE-PREPARATION.md).

전체 후속 분석이 더 빨리 끝나는 변경은 아니다. 별도 원본 Lua/합성 조건 재현에서 첫 표시는 10회→4회, 전체 준비는 10회→13회, 자원은 각각 6개였다. Windows 실측 시간/FPS 결과나 모든 장소의 즉시 표시 보장이 아니다.

## 자동 검사와 남은 확인

전체 소스 빌드의 21개 검사 프로그램·실제 DLL 연결·설치 ZIP을 확인했다. 생산 공급자/실제 원문 캡처·SHA 원본·합성 조건·IL 검사이며 새 DLL의 실제 Windows·Unity 화면·물리 입력·FPS 결과가 아니다. 고정 커밋 재빌드·게시·익명 다운로드 결과는 검증 후 기록한다. #6은 open이며 최신 정식 v1.0.6을 유지한다.

집중 선택지 검사와 전체 빌드는 준비 주기/도달 원문에 따른 동적 단언 집계가 달라질 수 있다. 검토 후보의 실제 성공 기록은 다음과 같다.

- PASS: ChoiceStoryTests 7157 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: InformationRuntimeTests: 4634 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.24.zip | 370369 | `01e7f978038836f40c95e877de10ec0f2f37b54aae1bdb28cc50829ce2b2477a` |
| DiceMaster.dll | 881152 | `9abc76c731a6d5114500b9dfbf37764c199cbf58db9d0016c8c8362c0cde5465` |
| 기존 cfg (변경 없음) | 13078 | `a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e` |

## 설치와 실제 Windows 확인

게임 종료 후 DLL/cfg를 백업하고 **설치 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 활협전용 BepInEx 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다. 새 설정/cfg 변경은 없다.

미완료 직접 인사의 첫 표시·완료 후 제거·반복 대련 제외·후속 주제의 단계적 추가와 첫/전체 준비 시간을 확인한다. 패드/기존 이벤트 선택·마커/창 가림도 함께 확인한다. [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha24--직접-대화-먼저-표시-확인-대기).

검토 후보와 고정 커밋·게시/다운로드 영수증은 artifacts/issue-6/local-choice-preparation/와 artifacts/releases/v1.0.7-alpha.24/에 구분해 보존한다.
