# v1.0.7-alpha.23 — 완료 인사 안내의 원문 복구

<!-- alpha23-publication -->
2026-10-22 03:06:00 KST에 소스 [`fc5dc7f`](https://github.com/jungpaeng/dicemaster/commit/fc5dc7fabc0977b01ee6acdb92a3987148e77370)의 [alpha.23](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.23)를 게시했다. 전체·고정 커밋 자동 검사 21개와 실제 후보 DLL 연결·설치 ZIP·익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·화면·물리 입력·FPS 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha23-publication -->

변환된 컴파일 인수 때문에 초회 인사의 원래 완료 조건이 사라지는 경계를 수정한다. 선택 안내는 첫 Prefix의 원문을 성공 Finalizer에서 읽고 #5의 실제 실행용 변환은 유지한다. 특정 인물/플래그 예외 목록이나 게임 상태 쓰기를 추가하지 않는다. [사용자 증거·재현·수정·지연 한계](ISSUE-6-CHOICE-LOAD-SOURCE.md).

## 검사와 한계

집중 검사에서 실제 #5 변환기·생산 관찰 훅을 연결하고 원본 SHA를 대조했다. 전체 소스 빌드와 고정 커밋 재빌드 각각 21개 프로그램·275개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·설치 ZIP 검증을 통과했다. 검토/재빌드의 입력과 DLL/cfg/ZIP 바이트가 모두 같고 공개 ZIP의 SHA-256·두 경로·CRC·내부 바이트도 인증 없이 대조했다. 합성 횟수/조건과 원본 Lua의 자동 검사이며 새 DLL의 실제 Windows·Unity 화면·물리 입력·FPS 검증과 구분한다. 전체 경로 준비 지연이 모두 사라지는 변경은 아니다. #6은 open이며 최신 정식 v1.0.6을 유지한다.

## 설치와 실제 Windows 확인

게임 종료 후 DLL/cfg를 백업하고 **설치 ZIP의 plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 활협전용 BepInEx 모드 베이스 아래에 config·plugins를 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축은 설치용이 아니다. 새 설정/cfg 변경은 없다.

초회 인사 전 표시, 완료 후 재진입 시 제거, 실제 열린 별도 주제 유지, 최초/재진입 표시 시간과 패드/기존 이벤트 선택을 함께 확인한다. [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha23--원문-보존-확인-대기).

## 검토 후보 파일

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.23.zip | 369968 | `b55325f344d1fc3f2640a2e8ae8afa765d9905b0980e4c0d5f44ca2bb433608f` |
| DiceMaster.dll | 880128 | `cbf86b0728e1a66f87e04e8854f0473f88af790e7b2beb3131de8fcee75620f8` |
| 기존 cfg (변경 없음) | 13078 | `a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e` |

집중·전체 검사에서 ChoiceStoryTests 6,856개와 InformationRuntimeTests 4,604개 검사를 통과했다. 이 검토 후보와 고정 커밋 재빌드의 성공 기록·입력·파일을 별도로 비교하며, 실제 Windows 확인으로 대체하지 않는다. 로컬 영수증은 artifacts/issue-6/original-choice-source/candidate/와 artifacts/releases/v1.0.7-alpha.23/에 보존한다.

전체 21개 프로그램의 성공 기록도 검토 후보와 고정 커밋 재빌드가 정확히 일치한다.

게시 시각 2026-10-22 03:06:00 KST, Release `419887552`·asset `663362969`, draft=false·prerelease=true·make_latest=false다. **[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.23/DiceMaster-v1.0.7-alpha.23.zip)**. 기존 64개 Release·65개 태그·자산·정식 Latest v1.0.6과 #6 open을 보존했다. source/tag는 [`fc5dc7f`](https://github.com/jungpaeng/dicemaster/commit/fc5dc7fabc0977b01ee6acdb92a3987148e77370)이다.

고정 소스·태그·익명 다운로드·문서 검증 영수증은 artifacts/releases/v1.0.7-alpha.23/의 release-verification.json·reviewed-candidate-comparison.json·tag-verification.json·anonymous-verification.json·final-verification.json에 보존한다.
