# alpha.34 — 지도 첫 대화의 고정 날짜 조건 보완

<!-- alpha34-publication -->
2026-10-22 21:58:37 KST에 소스 [`cf10c0a`](https://github.com/jungpaeng/dicemaster/commit/cf10c0ab37d30ec125ef16baee2edfd0765e1c7e)의 [alpha.34](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.34)를 게시했다. 전체·고정 커밋 검사21개·복제본 DLL 연결·설치 ZIP·익명 공개 다운로드 대조를 통과했다. **고정 날짜 조건 누락에 따른 연단방 첫 대화의 지도 안내를 보완했으며, 새 DLL의 실제 Windows 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha34-publication -->

기준 main은 `8dd027264fc8a49f97ede68093f43c07e4666b78`다. 원래 main에서 구현·검증하며 별도 브랜치/worktree는 만들지 않는다. [원인·설계·판정 경계](ISSUE-6-FIXED-CALENDAR.md) · [실제 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).

## 사용자 변화

연단방 첫 대화처럼 고정 기한과 현재 시각을 비교하는 해금 조건에서, 고정 날짜 자료 누락 때문에 지도 아이보리 마커가 빠지는 경계를 보완한다. 검토한 고정 날짜만 지문이 일치하는 설치 파일에서 확보하고 현재 시각·임무 저장 시각·플래그·수량·횟수는 기존 현재 참조를 사용한다. 특정 인물/플래그 예외 목록은 없다. 완료·기한 경과·현재 입력 누락/충돌을 계속 반영한다.

금색 기회/엄격1회성·아이보리 일반 경로의 구분, 설정 키·기본값·cfg·기존 입력·정상0.25초/실패5초·숨김/OFF 중단·정상 웜 주기 Lua 재분석 없음·읽기 전용 계약을 유지한다. 기존 준비/ReadStamp 캐시를 사용하며 고정 날짜를 매번 파일 조회하지 않는다. 지원6개 파일 지문은 alpha.32/33과 같고, 게임 날짜 값·원문/DLL은 배포하지 않는다.

## 자동 검증

SDK8.0.426으로 전체21개 검사 프로그램을 통과했다. 고정 참조16개와 제공받은 전체 게임 복제본의 DLL16개를 각각 연결해 검사했으며 같은 DLL 바이트를 생성했다. 빌드 입력304개·생산 소스/리소스123개의 불변을 확인했다. 원본 게임 파일과 cfg는 변경하지 않았다. 설치 ZIP의 config/plugins 두 경로·CRC·내부 DLL/cfg 바이트, 배포 검증기 Python20개와 git diff --check를 확인했다.

주요 최종 검사 기록:

- METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.
- PASS: 4265 managed behavior assertions and 1288 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- PASS: MapGuideRouteIntegrationTests 4170 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- PASS: InformationRuntimeTests: 6862 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- PASS: ChoiceStoryTests 7719 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- PASS: 262831 assertions.

실제 파일/번체 Lua2,199개와 명시적 현재 상태·Unity/게임 대체 객체로 이전 누락을 재현하고, 장소 미선택·미진입의 연단방/남제자방 안내, 기한 마지막 순/경과·완료 후 제거, 현재 시각 누락·충돌·native 우선순위·가변 시각 제외·잘못된 분류를 검사했다. 정상 웜 주기의 Lua 자원 재조회/재분석 없음과 엄격1회성 분리를 확인했다. 초기 검사 가정의 보완은 기술 문서에 기록하고 최종 로그를 artifacts에 보존했다.

소스·annotated tag는 `cf10c0ab37d30ec125ef16baee2edfd0765e1c7e` / `v1.0.7-alpha.34`다. 고정 커밋 재빌드의304개 입력·123개 소스/리소스·21개 성공 기록과 DLL/cfg/ZIP이 검토 후보와 모두 일치한다. 실제 Windows/Unity/Harmony·사용자 세이브·물리 입력·FPS/GC를 실행한 것이 아니다.

## 설치 후보 파일

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1093120 | `ed3fddb83e00bad6588403c008d43a0f6020e77e69e5b41f24ce9da071eee1fe` |
| DiceMaster-v1.0.7-alpha.34.zip | 431523 | `dc908dc2ba81304add726e7436b29142a7dbb22bbcf4a5ccb9ad93b39346cf8c` |
| 기존 DiceMaster.cfg | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |


## 설치·복구와 Windows 대기

활협전용 BepInEx 모드 베이스가 필요하다. 게임 종료 후 DLL/cfg를 백업하고 DLL만 교체해 기존 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins 두 경로를 BepInEx 아래에 넣는다. 자동 Source code 압축은 설치용이 아니다. 문제 시 게임 종료 후 백업 DLL/cfg를 복구한다. alpha.33과 Latest 정식v1.0.6을 보존한다.

완전 재실행·1년4월상순 지도 저장 직행 후 연단방에 들어가기 전 마커/툴팁, 한담 완료·기한 경과 뒤 제거와 남제자방 등 기존 안내·준비 속도/깜빡임·기존 입력/기능을 실제 Windows에서 확인해야 한다. 사용자 확인 전 #6 전체 완료나 이슈 종료로 처리하지 않는다.

## 게시 결과

2026-10-22 21:58:37 KST에 Release `420725895`·asset `665891276`를 draft=false·prerelease=true·make_latest=false로 게시했다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.34/DiceMaster-v1.0.7-alpha.34.zip)의 SHA-256·크기·두 경로·CRC·내부 DLL/cfg 바이트를 인증 없이 확인했다.

이전 75개 Release·76개 태그·자산·Latest 정식v1.0.6과 #5/#6 상태를 보존했다. main과 새 annotated tag만 atomic push했으며 게시 보충은 별도 main 문서 커밋이다. 소스 태그·배포 파일은 바꾸지 않는다. 실제 Windows/사용자 세이브·입력·FPS/GC 확인과 #6 완료는 대기다. 영수증은 artifacts/releases/v1.0.7-alpha.34/에 보관한다.
