# v1.0.7-alpha.25 — 해금된 후속 대화 경로 안내

<!-- alpha25-publication -->
2026-10-22 04:43:58 KST에 소스 [`e81a0c4`](https://github.com/jungpaeng/dicemaster/commit/e81a0c420602c5cbaa472a59d1617131e830e3f9)의 [alpha.25](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.25)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha25-publication -->

사용자의 커밋·prerelease 지시에 따라 main의 소스를 고정해 재빌드하고 검토 후보와 대조한 뒤 새 prerelease를 게시했다. 실제 Windows·화면·물리 입력·FPS 확인 및 #6 전체 완료는 대기다.

사사형의 해금된 하위 주제를 바깥 사사형·선택한 남제자방의 경로 툴팁에서 읽을 수 있도록 한다. 게임이 초기화할 대화 플래그의 실제 기본값만 조회 안에서 가상 반영한다. 잠김·해금 미확정·완료 인사·반복 활동과 진입 상태 변경·행동력·임무 우선순위 보호를 유지한다. 경로만으로 마커를 붙이지 않는다. [설계·원본 재현·성능 교환](ISSUE-6-ROUTE-PROJECTION.md).

불필요한 리터럴 분기 복사와 조건 재탐색을 줄이고 확인한 경로는 관련 없는 후속 준비 전에 공개한다. 기존 작업/시간/읽기·누적 상한을 늘리지 않는다. 간격 비교 후 정상 .25초·실패 5초를 유지한다. 특정 인물·플래그 예외 목록·새 설정/cfg 변경은 없다.

## 자동 검사와 남은 확인

전체 소스 후보의 21개 자동 검사 프로그램, 실제 DLL의 게임 참조/후킹 연결과 설치 ZIP의 두 경로·CRC·내부 DLL/cfg 바이트를 확인했다. 280개 입력·113개 소스/리소스·16개 제공 참조를 대조했다. SHA를 확인한 원본 Lua·생산 공급자·합성 직렬화 조건 및 관리 코드/입력 경계 검사이며 Windows 게임 실행 결과가 아니다. 고정 커밋 재빌드도 21개 프로그램·280개 입력·113개 소스/리소스·16개 고정 참조를 통과했다. 검토 후보와 전체 빌드 입력·성공 기록·DLL/cfg/ZIP 바이트가 모두 일치한다. 공개 ZIP의 SHA-256·두 경로·CRC·내부 DLL/cfg 바이트도 인증 없이 대조했다.

- PASS: ChoiceStoryTests 7650 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: MapGuideRouteIntegrationTests 3356 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: MapRouteTests 6884 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed.
- PASS: InformationRuntimeTests: 4712 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.25.zip | 372863 | `78a8cfb763586a5d58b4c7fd3d9d5530faf10ddfe3623447725bc3e2cb6f3751` |
| DiceMaster.dll | 886272 | `140405a57065de854ab28b54613095e3d87308b92b81c8c62b45161f837b88b0` |
| 기존 cfg (변경 없음) | 13078 | `a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e` |

## 설치와 실제 Windows 확인

게임 종료 후 DLL/cfg를 백업하고 **설치 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 활협전용 BepInEx 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다.

해금된 주제의 상위 경로, 잠긴 주제 제외·완료 인사 제거·반복 대련 제외, 지도 진입/행동력/대기 임무 우선순위와 기존 기회 마커를 확인한다. 최초/재진입의 첫 유효 표시와 전체 준비, 반복 프레임/할당량·창 가림·패드와 기존 입력을 각각 확인한다. [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha25--후속-경로-확인-대기).

검토 후보·측정 원자료는 artifacts/issue-6/route-reset-projection/, 고정 커밋·게시·공개 다운로드 영수증은 artifacts/releases/v1.0.7-alpha.25/에 구분한다. 기존 Latest v1.0.6과 이전 Release·태그·자산·이슈 상태를 보존하며 #6을 종료하지 않는다.

전체 21개 프로그램의 성공 기록도 검토 후보와 고정 커밋 재빌드가 정확히 일치한다.

게시 시각 2026-10-22 04:43:58 KST, Release `419983660`·asset `663599558`, draft=false·prerelease=true·make_latest=false다. **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.25/DiceMaster-v1.0.7-alpha.25.zip)**. 기존 66개 Release·67개 태그·자산·정식 Latest v1.0.6과 #6 open을 보존했다. source/tag는 [`e81a0c4`](https://github.com/jungpaeng/dicemaster/commit/e81a0c420602c5cbaa472a59d1617131e830e3f9)이다.

게시 후 보충 문서는 별도 main 커밋이며 소스 태그·빌드 입력·설치 파일을 바꾸지 않는다. 영수증은 artifacts/releases/v1.0.7-alpha.25/의 release-verification.json·reviewed-candidate-comparison.json·tag-verification.json·anonymous-verification.json·final-verification.json에 보존한다.
