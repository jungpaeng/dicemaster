# v1.0.7-alpha.26 — 지도 이야기 툴팁과 순차 탐색 개선

<!-- alpha26-publication -->
2026-10-22 05:40:32 KST에 소스 [`4dc837c`](https://github.com/jungpaeng/dicemaster/commit/4dc837c25824952078f0cddc0df0ba8e41831eb0)의 [alpha.26](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.26)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha26-publication -->

사용자의 커밋·prerelease 지시에 따라 main의 소스를 고정해 재빌드하고 검토 후보와 대조한 뒤 새 prerelease를 게시했다. 실제 Windows·화면·물리 입력·FPS 확인 및 #6 전체 완료는 대기다.

지도에서도 추가 주제 선택 없는 초회 인사를 툴팁에 포함한다. 지도와 실제 선택 화면은 이야기의 해금·완료·반복 활동·현재 필수 조건 필터를 공유한다. 미선택 장소의 새 분석과 일반 경로의 마커 추가는 없다. 기존 대기 임무·증명 1회성·도달성·행동력·우선순위 기준을 유지한다.

앞의 32개 경로만 매번 읽어 뒤 후보를 영구 누락할 수 있던 조회를 이어 읽기로 수정했다. 직전 성공 경로의 정의 참조만 재사용하고 현재 상태는 새로 확인한다. 완료·잠김은 즉시 제거하며 이전 false도 다시 읽는다. 모든 경로의 즉시 발견이나 발생 자체를 보장하지 않는다. [설계·원본 재현·측정](ISSUE-6-MAP-STORY-SCAN.md).

정상0.25초/실패5초·숨김/OFF 중단·정상 Lua 재분석 없음·기존 작업/시간/읽기/누적 상한을 유지한다. 설정 도움말과 기본 cfg의 설명만 바뀌며 키·섹션·기본값은 같다. 게임 판정·RNG·횟수·조건·세이브·능력치·아이템을 변경하지 않는다. 특정 인물·플래그 예외 목록은 없다.

## 자동 검사와 성능 경계

전체 소스 빌드의 자동 검사 프로그램 21개, 281개 빌드 입력·113개 소스/리소스·16개 제공 참조와 실제 후보 DLL 연결·설치 ZIP의 두 경로·CRC·내부 DLL/cfg 바이트를 대조했다. 배포 검증기 자체 회귀20개도 통과했다. 원본 SHA+합성 직렬화 조건·관리 코드/입력 경계 검사이며 실제 Lua/Windows/물리 입력 실행은 아니다. 고정 커밋 재빌드도 21개 프로그램·281개 입력·113개 소스/리소스·16개 고정 참조를 통과했다. 검토 후보와 전체 빌드 입력·DLL/cfg/ZIP 바이트가 모두 일치한다. 공개 ZIP의 SHA-256·두 경로·CRC·내부 DLL/cfg 바이트도 인증 없이 대조했다.

검토 후보의 주요 성공 기록:

- PASS: MapGuideRouteIntegrationTests 3554 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: ChoiceStoryTests 7650 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: MapRouteTests 6884 actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed.
- PASS: InformationRuntimeTests: 4716 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: 2266 preview integration assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures.

실제 IL 검사 대역의 새 MapStoryPath/MapGuidanceRejection 의존 타입 누락으로 실패한 뒤 의존 타입과 회귀를 연결하고 전체 검사를 재실행해 통과했다. 이전 실패 로그도 별도 보존한다.

생산 지도 제공자+합성 조건의 28개 이야기/26번째 유효 표본에서 매번 처음부터 읽기와 직전 유효 정의 우선 읽기를 비교했다. 300회×3회 중앙값 CPU 35.822875→5.598083ms, 할당/조회 47098→11640B, 현재 플래그 읽기/조회26→1회, Lua 자원 읽기0회다. 호스트 .NET8 측정이며 전체 버전 비교나 실게임 FPS 개선율이 아니다. 정의 참조를 위한 보존 메모리는 추가된다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.26.zip | 373675 | `c01fd6f4a96899ae3d1bbb717158014260dfe9b3852df230221e9d7ac87e3791` |
| DiceMaster.dll | 887296 | `6d88b93b339456c02ec5bfe8bb31f57442c344bd46c95b0d5ad061ccb449c907` |
| 기본 cfg (설명만 변경) | 13205 | `94d29dcdc35ad3b915aa3b77678a218a55be7a2b9d59d3544f6b097550fd06d0` |

## 설치와 실제 Windows 확인

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 **설치 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다.

지도 남제자방의 초회 인사 전/완료 후, 색종이 등 해금된 후속 주제·잠김/반복 대련만 남은 상태, 첫 표시와 완료 후 제거 지연을 확인한다. 마커·행동력/우선순위·창 가림·Xbox/XInput/키보드/마우스/터치·#5/로그 네 탭/예약/주사위/상점/실제 분기와 최초/warm 프레임·할당을 확인한다. [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha26--공통-이야기-툴팁이어-읽기-확인-대기).

검토 후보·성능 원자료는 artifacts/issue-6/map-story-scan/, 고정 커밋·게시·공개 다운로드 영수증은 artifacts/releases/v1.0.7-alpha.26/에 구분한다. 기존 Latest v1.0.6과 이전 Release·태그·자산·이슈 상태를 보존하며 #6을 종료하지 않는다.

전체 21개 프로그램의 성공 기록도 검토 후보와 고정 커밋 재빌드가 정확히 일치한다.

게시 시각 2026-10-22 05:40:32 KST, Release `420034646`·asset `663731037`, draft=false·prerelease=true·make_latest=false다. **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.26/DiceMaster-v1.0.7-alpha.26.zip)**. 기존 67개 Release·68개 태그·자산·정식 Latest v1.0.6과 #6 open을 보존했다. source/tag는 [`4dc837c`](https://github.com/jungpaeng/dicemaster/commit/4dc837c25824952078f0cddc0df0ba8e41831eb0)이다.

게시 후 보충 문서는 별도 main 커밋이며 소스 태그·빌드 입력·설치 파일을 바꾸지 않는다. 영수증은 artifacts/releases/v1.0.7-alpha.26/의 release-verification.json·reviewed-candidate-comparison.json·tag-verification.json·anonymous-verification.json·final-verification.json에 보존한다.
