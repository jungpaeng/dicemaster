# v1.0.7-alpha.21 — 선택 안내 근거·반복 활동 판정 보완

<!-- alpha21-publication -->
2026-10-22 02:19:17 KST에 소스·주석 태그 [`9163d59`](https://github.com/jungpaeng/dicemaster/commit/9163d5931a9e95bb614f77dcfb48bbb45519b376)의 [alpha.21](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.21)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·273개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 62개 Release·63개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [파일·검증·설치](ISSUE-6-ALPHA21-RELEASE.md).
<!-- /alpha21-publication -->

## 변경과 한계

완료된 초회 대화가 준비 중에 미확정으로 표시되는 경로와, 부모 횟수형 조건 때문에 고정 활동의 플래그 독립성이 미확정으로 남는 경로를 보완한다. 준비/색인·메뉴 위치 미확정의 임시 이야기 툴팁은 보류한다. 정수 횟수형 스키마가 플래그 쓰기와 독립임을 확인하되 일반 Switch를 예측/실행하지 않는다. 고정 활동에서 독립성 미확정은 상세로 제한하고 실제 조건 소모는 이야기 후보로 유지한다.

명시적 상세에는 표시한 이야기와 제외한 표본 각 한 건의 스크립트·대사 ID·제외 사유·현재 필수 조건 진단을 제공한다. 자동 화면에 진단을 붙이지 않는다. 특정 인물/조건 예외 목록은 없으며 일반 난수·별도 조건부 이야기·실제 남은 초회 대화와 기존 기회 마커/해금/설정/입력/캐시 계약을 유지한다. 미지원·누락·상한으로 실제 가능한 이야기를 생략할 수 있고 모든 이야기 탐지를 보장하지 않는다.

사용자는 alpha.20에서 기다려도 문구가 남는다고 확인했다. 이번 자동 재현은 원본 Lua와 합성 게임 조건·관찰 환경·자료 누락 경계를 사용한 것이며 사용자 Windows 세이브를 실행한 결과가 아니다. 그 세이브의 정확한 근거 확인과 새 Windows 화면·입력·프레임 검증은 대기다. [원인·구현·검사 범위](ISSUE-6-CHOICE-EVIDENCE.md) · [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha21--선택-안내-근거반복-활동-확인-대기).

## 자동 검사와 파일

전체 소스 빌드의 21개 검사 프로그램·273개 입력·112개 소스/리소스·16개 고정 참조·실제 후보 DLL 연결·설치 ZIP의 두 경로/CRC/바이트 대조를 통과했다. 고정 커밋 재빌드도 21개 프로그램이 모두 통과했고 검토 후보와 전체 입력·DLL/cfg/ZIP 바이트가 일치했다. 공개 설치 ZIP의 두 경로·CRC·내부 바이트·SHA-256도 인증 없이 검증했다. **새 DLL의 실제 Windows·물리 입력·화면·프레임 성능 확인과 #6 전체 완료는 대기다.** 선택지 6,313개·경로 6,882개·지도 통합 3,347개·실제 DLL preview 2,258개·정보 런타임 4,593개·기존 패드 262,831개 검사를 포함한다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.21.zip | 369784 | e246d1894bedd6c967cbdfd862429b9825186dd32010be6054e831c52f3aaf3f |
| DiceMaster.dll | 879616 | 7e68939c81f16ee00fe3c0d04524b0c9054c51483789da245862225cc9408c78 |
| 기존 cfg (변경 없음) | 13078 | a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e |

검토 후보는 artifacts/issue-6/choice-evidence/candidate/의 build-manifest.json·build.log, 단계별 근거는 상위 verification.json에 보존한다. 고정 커밋·태그·공개 파일 증거는 artifacts/releases/v1.0.7-alpha.21/에 별도로 기록한다. 지도 통합 단언 수는 초기 준비 슬라이스의 조회 단언으로 달라질 수 있어 실제 두 결과를 비교한다.

Release `419842640`·asset `663249870`, draft=false·prerelease=true·make_latest=false다. **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.21/DiceMaster-v1.0.7-alpha.21.zip)**.

소스는 [`9163d59`](https://github.com/jungpaeng/dicemaster/commit/9163d5931a9e95bb614f77dcfb48bbb45519b376)이다. 20개 프로그램의 성공 기록은 동일하다. 선택지 단언 집계는 검토 6,313개/고정 재빌드 6,314개이며 둘 다 통과했다. 준비 완료까지의 Read 조회와 도달한 원문의 SHA 검사가 동적으로 집계된다. 원래 로그에는 합계만 있어 추가된 한 단언을 사후 특정하지는 못한다. 생산 코드와 단언 조건을 바꾸지 않은 임시 집계 계측은 세 번 모두 6,312개로 통과했다. 전체 입력과 DLL/cfg/ZIP은 동일하며 검사 횟수까지 같다고 주장하지 않는다. 별도 counter-audit.json에 원문별 집계를 보존했다. reviewed-candidate-comparison.json에 입력·파일·성공 기록 비교를 보존한다.

공개 증거는 artifacts/releases/v1.0.7-alpha.21/의 release-verification.json·reviewed-candidate-comparison.json·tag-verification.json·anonymous-verification.json·final-verification.json으로 구분한다. 기존 릴리스·태그·자산·정식 Latest·이슈 상태도 대조했다.

## 설치와 실제 Windows 확인

게임을 종료하고 현재 DLL/cfg를 백업한 뒤 **plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 기존 활협전용 BepInEx 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다.

사사형의 초회 완료/미완료·대련만 남은 메뉴·다른 열린 주제·실제 표시/제외 상세 근거, 장면/로드·패드/키보드/마우스/터치·창 가림·최초/반복 프레임을 확인한다. 최신 정식은 v1.0.6이며 이슈 #6은 open으로 유지한다.
