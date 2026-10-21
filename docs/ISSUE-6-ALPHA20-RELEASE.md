# v1.0.7-alpha.20 — 완료 진입 대화·반복 활동 안내 정리

<!-- alpha20-publication -->
2026-10-22 01:49:00 KST에 소스·주석 태그 [`07363bc`](https://github.com/jungpaeng/dicemaster/commit/07363bce02119bd9734d1b69c0440c613885fcb8)의 [alpha.20](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.20)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·272개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 61개 Release·62개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA20-RELEASE.md).
<!-- /alpha20-publication -->

## 변경과 지원 범위

사사형 툴팁은 반복 대련뿐 아니라 이미 끝난 초회 진입 인사를 근거로 남을 수 있었다. 안전한 첫 루트 호출의 다음 횟수 비교를 읽어 완료된 진입 대화를 제외하며, 완료 근거를 읽을 수 없으면 자동 안내를 생략하고 상세에 사유를 남긴다. 실제 남은 초회 대화·다른 해금된 주제·알려진 일반/RNG 미확정은 유지한다.

반복 활동 판정은 알려진 순수 화면 준비 문법을 루트 메뉴 앞에서도 보존하고 최종 표시 경로에서도 적용해 고정 반응이 fallback으로 되살아나지 않도록 한다. 특정 인물·플래그 예외 목록이나 대련 한 종류만을 제외하는 방식이 아니다. 모든 이야기 탐지를 보장하지 않으며 불명 호출·복수 횟수 호출·미지원 조건·자료 누락·조회 상한은 보수적으로 생략할 수 있다.

기존 지도/선택 마커의 대기 임무 또는 엄격한 1회성 증명·현재 조건·경로, 후속 선택지 해금 필터·상점·실제 분기 기록·이벤트/예약/주사위와 패드·키보드·포인터·터치·입력 소유권·해제/중립을 유지한다. 새 설정/cfg 변경이 없으며 정상 .25초/실패 5초·숨김/OFF 조회 중단·Lua 정의 캐시 계약을 유지한다. 게임 판정·RNG·카운터·플래그·세이브·능력치·아이템을 변경하지 않는다.

[원인·구조·제약·원본 근거](ISSUE-6-ENTRY-DIALOGUES.md) · [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha20--완료-진입-대화반복-활동-확인-대기).

## 자동 검증과 설치 파일

전체 소스 빌드의 21개 검사 프로그램·272개 입력·112개 소스/리소스·16개 고정 참조와 실제 후보 DLL 연결·설치 ZIP의 두 경로·CRC·바이트 대조를 통과했다. 고정 커밋 재빌드도 21개 프로그램이 모두 통과했고 검토 후보와 전체 입력·DLL/cfg/ZIP 바이트가 일치했다. 공개 설치 ZIP의 두 경로·CRC·내부 바이트·SHA-256도 인증 없이 검증했다. **새 DLL의 실제 Windows·물리 입력·화면·프레임 성능 확인은 대기이며 #6은 open이다.** 선택지 4,900개·경로 6,882개·지도 통합 3,347개·실제 DLL preview 2,253개·정보 런타임 4,566개·기존 패드 262,831개 검사를 포함한다. 원본 Lua와 합성 게임 경계/실제 후보 IL의 자동 검사이며 사용자 세이브나 Windows Unity 화면의 실행 결과가 아니다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.20.zip | 369224 | 4ff38b788d033f8e6da0e0dc11baf2a854498d93aad36cfb15eeafd418c84ed3 |
| DiceMaster.dll | 877568 | ab0c5175528b4744e23236daeb30fae219d502821675bf0365a59b349d97c6b7 |
| 기존 cfg (변경 없음) | 13078 | a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e |

검토 후보는 artifacts/issue-6/entry-dialogues/candidate/의 build-manifest.json·build.log, 단계별 근거는 상위 verification.json에 보존한다. 고정 커밋·태그·익명 다운로드 증거는 artifacts/releases/v1.0.7-alpha.20/에 별도로 보존한다. 지도 통합 단언 개수는 초기 준비 슬라이스별 조회 단언으로 변할 수 있으므로 검토/재빌드의 실제 개수를 비교 기록한다.

Release `419811206`·asset `663170047`, draft=false·prerelease=true·make_latest=false다. **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.20/DiceMaster-v1.0.7-alpha.20.zip)**.

소스는 [`07363bc`](https://github.com/jungpaeng/dicemaster/commit/07363bce02119bd9734d1b69c0440c613885fcb8)이다. 모든 21개 프로그램의 성공 기록도 정확히 일치한다. reviewed-candidate-comparison.json에 입력·파일·성공 기록 비교를 보존한다.

공개 증거는 artifacts/releases/v1.0.7-alpha.20/의 release-verification.json·reviewed-candidate-comparison.json·tag-verification.json·anonymous-verification.json·final-verification.json으로 구분한다. 기존 릴리스·태그·자산·정식 Latest·이슈 상태도 대조했다.

## 설치와 실제 Windows 확인

게임을 종료하고 현재 DLL/cfg를 백업한다. 업데이트는 설치 ZIP의 **plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 기존 활협전용 BepInEx 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다.

이번 DLL에서 초회 대화 남음/완료 전환·대련만 남은 메뉴·다른 열린 주제 유지·로드/장면 변경·마커/툴팁/기본 상세/개수, 패드·키보드·마우스·터치와 가독성/창 가림/최초·반복 프레임을 확인해야 한다. 새 Windows 확인 전 전체 완료나 #6 종료로 처리하지 않는다.
