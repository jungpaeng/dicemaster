# 소스 기반 Release와 게시 이력

[Release 작성 가이드](RELEASE-WRITING.md) · [사용자 안내](../README.md) · [소스 빌드·검증](SOURCE-BUILD.md)

제목·본문·설치 안내·검증 상태를 쓰거나 기존 Release를 정리할 때는 [작성 가이드](RELEASE-WRITING.md)를 따른다. 아래 버전별 절은 게시 준비와 실행 당시의 기록이며, 과거의 미게시·확인 대기 문구를 현재 상태로 해석하지 않는다. 현재 정식판·시험판과 최신 사용자 확인은 [README](../README.md#다운로드와-지원-환경) 및 각 버전의 후속 기록에서 확인한다.

## 로그창의 이벤트·분기 판정 색상 — v1.0.6-alpha.16 prerelease 준비

현재 팝업과 같은 구조화된 판정을 로그의 이벤트 목록과 조건 상세 개별 분기에 연결한다. 최신 정상 조회에서 확정 미충족·가중치 0·앞 분기 우선만 제목·조건을 회색으로 표시하고, 미확정 `?`·자료 확인 실패 `!`·갱신 실패와 이전 값을 구분한다. 조건 체크포인트 부모 행과 결과 효과색, 선택·예약·터치 동작은 유지한다. 공통 Switch 읽기 helper는 기존 분기 순서·기본 분기·다음 실행 횟수 계산을 보존하며, 이벤트 로그는 조건과 정적 결과 설명을 분리한다. [변경·설치·검증](ISSUE-5-ALPHA16-RELEASE.md) · [표시·갱신 경계](ISSUE-5-LOG-JUDGMENT-DISPLAY.md).

기준 main은 `887741887c456df735966403e9348e6205497c42`다. 최종 전체 후보는 2026-10-20 21:39:56 KST에 14개 검사·197개 입력·80개 소스/리소스·패키지 대조를 통과했다. 고정 커밋 재빌드·14개 검사·동일 입력과 DLL/cfg/ZIP 대조 후 main과 새 태그를 atomic push하고 새 prerelease를 준비한다. 사전 기존 39개 Release·태그·자산과 Latest 정식 v1.0.5·이슈 #2 closed·#4 closed·#5 open을 보관했으며 게시 후 대조한다. 실제 게시 결과는 별도 문서 커밋으로 보충한다. Windows 개발 환경 설치·실행 검사는 하지 않으며 실제 Windows·터치 확인은 대기다.

## 선택 팝업의 조건·판정 표시 통일 — v1.0.6-alpha.15 prerelease

사용자의 구현·커밋·새 prerelease 요청에 따라 **2026-10-20 12:54:18 KST에 [alpha.15](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.15)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.15/DiceMaster-v1.0.6-alpha.15.zip). 이벤트·조건 팝업의 목록을 제목·결과 요약·조건 요약의 고정 3줄로 맞추고 상세를 필요한 짧은 판단 이유 → 전체 조건 → 전체 결과 순서로 정리했다. 이벤트의 활성 조건·기본/조건별/최종 가중치를 보강하고 미확정 `?`와 대상·결과 확인 실패 `!`를 구분한다. 표시용 난수 샘플과 재귀 이름 표현식 확장을 거치지 않으며 실행 권한과 예약·Lua·터치·로그 포커스 흐름을 보존한다. 구현·회귀 검사·README·준비 문서를 포함한 고정 소스·주석 태그는 `ede946e674f2c200d20b76eb5efe9728b61861d0`이며 main과 태그를 함께 atomic push했다. [변경·설치·검증](ISSUE-5-ALPHA15-RELEASE.md) · [표시 설계·회귀 근거](ISSUE-5-POPUP-CONDITION-PARITY.md).

후보 전체 소스 빌드와 고정 소스의 별도 스냅샷 재빌드에서 각각 14개 검사 프로그램을 통과했다. 고정 재빌드는 2026-10-20 12:53:40 KST에 기존 모드 DLL 0개에서 완료했고 검토 후보와 194개 빌드 입력·79개 소스/리소스·전체 검사 결과·DLL/cfg/ZIP 바이트가 일치했다. Release `417982532`·asset `658198769`, `draft=false`·`prerelease=true`·`make_latest=false`다. 게시 후 익명 공개 페이지·다운로드에서 ZIP CRC·정확히 두 파일·내부 바이트·SHA-256과 고정 태그를 확인했다.

사전 원격 main `5e2d4cf46fbe1236eac5fae4b0e2abb0b13fd63f`와 기존 38개 Release·태그·자산을 보관하고 게시 후 보존을 확인했다. Latest 정식 v1.0.5와 이슈 #2 closed·#4 closed·#5 open도 유지했다. 실제 Windows·Unity 화면·물리 패드·터치 확인과 이슈 #5 완료는 대기다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.15/`의 `release-verification.json`·`build-manifest.json`·`reviewed-candidate-comparison.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 후보의 독립 감사는 `artifacts/popup-parity/full/independent-audit.json`이다. 고정 검사 영수증의 `published=false`와 소스 태그의 준비 문구는 게시 전 검사 시점이며 이후 게시 결과와 구분한다. 이 결과 보충은 별도 main 문서 커밋이며 빌드 소스·태그·공개 자산을 변경하지 않는다.

## 팝업 중 로그 열기·조건 분기 설명 — v1.0.6-alpha.14 prerelease

사용자의 구현·검증 후 커밋·새 prerelease 요청에 따라 **2026-10-20 12:28:24 KST에 [alpha.14](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.14)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.14/DiceMaster-v1.0.6-alpha.14.zip). 이벤트·조건 팝업에서 버튼·설정된 로그 열기 키·패드의 양쪽 스틱 클릭으로 로그를 열고, 닫을 때까지 로그가 입력을 소유하며 대기 중인 팝업의 Lua 판정을 보류하도록 연결한다. 조건 팝업은 목록에 조건 요약을 표시하고 전체 조건을 상세의 결과보다 먼저 보여주며 확정 미충족 분기를 회색으로 구분한다. 구현·회귀 검사·README·준비 문서를 포함한 고정 소스·주석 태그는 `3c9aa94512add361818511fe1f57d5987a5855d2`이며 main과 태그를 함께 atomic push했다. [변경·설치·검증](ISSUE-5-ALPHA14-RELEASE.md) · [입력·표시 설계와 회귀](ISSUE-5-POPUP-LOG-CONDITIONS.md).

후보 전체 소스 빌드와 고정 소스의 별도 스냅샷 재빌드에서 각각 14개 검사 프로그램을 통과했다. 고정 재빌드는 2026-10-20 12:27:31 KST에 기존 모드 DLL 0개에서 완료했고 검토 후보와 191개 빌드 입력·78개 소스/리소스·DLL/cfg/ZIP·전체 검사 결과가 일치했다. Release `417969778`·asset `658150525`, `draft=false`·`prerelease=true`·`make_latest=false`다. 게시 후 익명 공개 페이지·다운로드에서 ZIP CRC·정확히 두 파일·내부 바이트·SHA-256과 고정 태그를 확인했다.

사전 원격 main `182f356f95f19742f374d14f17af883c124c5110`와 기존 37개 Release·태그·자산을 보관하고 게시 후 보존을 확인했다. Latest 정식 v1.0.5와 이슈 #2 closed·#4 closed·#5 open도 유지했다. 실제 Windows·Unity 화면·물리 패드·터치 확인과 이슈 #5 완료는 대기다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.14/`의 `release-verification.json`·`build-manifest.json`·`reviewed-candidate-comparison.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 고정 검사 영수증의 `published=false`와 소스 태그의 준비 문구는 게시 전 검사 시점이며 이후 게시 결과와 구분한다. 이 결과 보충은 별도 main 문서 커밋이며 빌드 소스·태그·공개 자산은 변경하지 않는다.

## 조건 상세에서 분기 예약 — v1.0.6-alpha.13 prerelease

사용자의 커밋·`origin/main` 반영·README 갱신·새 prerelease 요청에 따라 **2026-10-20 05:59:52 KST에 [alpha.13](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.13)을 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.13/DiceMaster-v1.0.6-alpha.13.zip). 조건 목록을 체크포인트당 한 행으로 되돌리고 상세에서 전체 분기를 읽고 예약할 결과를 선택한다. 선택·예약 상태와 하단 안내가 바뀌어도 터치 위치를 유지하도록 일반·좁은 창의 배치를 보완했다. 구현·회귀 검사·README·준비 문서를 포함한 고정 소스·주석 태그는 `f9ec0be282ec86d14a0bfec23ab5a7ff6723101b`이며 main과 태그를 함께 atomic push했다. [변경·설치·검증](ISSUE-5-ALPHA13-RELEASE.md) · [상세 선택 설계·검사](ISSUE-5-CONDITION-DETAIL-SELECTION.md).

후보 전체 소스 빌드와 고정 소스의 별도 스냅샷 재빌드에서 각각 14개 검사 프로그램을 통과했다. 고정 재빌드는 2026-10-20 05:58:52 KST에 기존 모드 DLL 0개에서 완료했고 검토 후보와 189개 빌드 입력·78개 소스/리소스·DLL/cfg/ZIP·전체 검사 결과가 일치했다. Release `417748369`·asset `657424358`, `draft=false`·`prerelease=true`·`make_latest=false`다. 게시 후 익명 공개 페이지·다운로드에서 ZIP CRC·정확히 두 파일·내부 바이트·SHA-256과 고정 태그를 확인했다.

사전 원격 main `a57c286bbe193005919ba48d4c723afc56d04ba0`와 기존 36개 Release·태그·자산을 보관하고 게시 후 보존을 확인했다. Latest 정식 v1.0.5와 이슈 #2 closed·#4 closed·#5 open도 유지했다. 실제 Windows·장치 확인과 이슈 #5 완료는 대기다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.13/`의 `release-verification.json`·`build-manifest.json`·`reviewed-candidate-comparison.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 고정 검사 영수증의 `published=false`와 소스 태그의 준비 문구는 게시 전 검사 시점이며 이후 게시 결과와 구분한다. 이 결과 보충은 별도 main 문서 커밋이며 빌드 소스·태그·공개 자산은 변경하지 않는다.

## 조건·이벤트 일회 예약 — v1.0.6-alpha.12 prerelease

사용자의 커밋·`origin/main` 반영·README 갱신·새 prerelease 요청에 따라 **2026-10-20 05:36:03 KST에 [alpha.12](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.12)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.12/DiceMaster-v1.0.6-alpha.12.zip). 조건 목록을 결과별 행으로 나누고 조건·이벤트의 독립적인 일회 예약·해제와 조건 팝업의 신규 기본값 OFF를 포함한다. 이미 저장된 ON은 유지한다. 구현·검사·README·준비 문서를 포함한 고정 소스·주석 태그는 `11a6fb4570ca632ff435d3ddf2ac74beecfe1aa8`이며 `origin/main`에 반영했다. [사용자 변화·설치·검증](ISSUE-5-ALPHA12-RELEASE.md) · [예약 설계·검사 근거](ISSUE-5-CHECKPOINT-RESERVATIONS.md).

후보 전체 소스 빌드와 고정 소스의 별도 스냅샷 재빌드에서 각각 14개 검사 프로그램을 통과했다. 고정 재빌드는 2026-10-20 05:34:10 KST에 기존 모드 DLL 0개에서 완료했고 검토 후보와 188개 입력·DLL/cfg/ZIP 바이트·전체 검사 결과가 일치했다. Release `417723438`·asset `657372175`, `draft=false`·`prerelease=true`·`make_latest=false`다. 게시 후 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256과 고정 태그를 확인했다.

사전 원격 main `1f189a9cf21fcc2ede30c183f6d9e2dc1a8905af`와 기존 35개 Release·태그·자산을 보관하고 게시 후 보존을 확인했다. Latest 정식 v1.0.5를 유지했다. 빌드 중 2026-10-20 05:31:35 KST에 `jungpaeng`이 이슈 #4를 외부에서 종료한 것을 확인했다(`commit_id=null`). 원래 사전 원본과 변경 근거를 보존하고 게시 직전 기준을 #2 closed·#4 closed·#5 open으로 다시 확인했다. 이 배포 작업은 이슈를 변경하지 않았고 해당 기준을 게시 후에도 유지했다. 이슈 #4의 외부 종료를 alpha.12 DLL의 Windows 검증으로 해석하지 않는다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.12/`의 `release-verification.json`·`build-manifest.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 외부 이슈 변경은 `artifacts/releases/alpha12-work/issue-state-change.json`과 `artifacts/checkpoint-reservations/alpha12-prepublish/external-issue-change.json`에 기록했다. 고정 검사 영수증의 `published=false`와 고정 소스의 준비 문구는 게시 전 검사 시점이며 이후 게시 결과와 구분한다. 이 결과 보충은 별도 main 문서 커밋이며 빌드 소스·태그·공개 자산은 변경하지 않는다. 이번 DLL의 실제 Windows·장치 확인과 이슈 #5 완료는 대기다.

## 대화 중 조건 분기 선택 — v1.0.6-alpha.11 prerelease

사용자의 테스트용 prerelease 요청에 따라 **2026-10-20 05:13:44 KST에 [alpha.11](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.11)을 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.11/DiceMaster-v1.0.6-alpha.11.zip). 구현 `3e051b8`과 조건명 원복 `0b3e758` 및 README·배포 준비 문서를 포함한 고정 소스·주석 태그는 `abc621eff772597972c6da61694b0fd589389d8d`이며 `origin/main`에 반영했다. [변경·설치·지원 범위·검사](ISSUE-5-ALPHA11-RELEASE.md) · [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md).

후보 전체 소스 빌드와 고정 커밋의 별도 소스 스냅샷 재빌드에서 각각 14개 검사 프로그램을 통과했다. 고정 검사는 05:13:01 KST에 기존 모드 DLL 0개에서 완료했고 검토 후보의 185개 입력·DLL/cfg/ZIP 바이트·전체 검사 결과와 일치했다. Release `417702109`·asset `657322364`, `draft=false`·`prerelease=true`·`make_latest=false`다. 게시 후 익명 공개 페이지·다운로드에서 CRC·정확히 두 파일·내부 바이트·SHA-256을 대조했다.

사전 원격 main `db8006e486b1e393ece295c63a0942b30b1ef989`와 기존 34개 Release·태그·자산을 보관하고 게시 후 보존을 확인했다. Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open도 유지했다. 조건명 원복은 실제 내부 ID 표시 문제 해결을 뜻하지 않으며 기존 alpha.10 공개 파일은 변경하지 않았다. [원복 근거](ISSUE-5-ALPHA10-RELEASE.md#2026-10-20-사용자-후속-보고와-원복)를 따른다.

영수증은 로컬 `artifacts/releases/v1.0.6-alpha.11/`의 `release-verification.json`·`build-manifest.json`·`github-publication.json`·`anonymous-verification.json`·`public-release.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 고정 검사 영수증의 `published=false`와 고정 소스 문서의 준비 문구는 게시 전 시점이며 위 게시 결과와 구분한다. 이 결과 보충은 별도 main 문서 커밋이며 빌드 소스·태그·공개 자산을 변경하지 않는다. 새 DLL의 실제 Windows·장치 확인과 이슈 #5 완료는 대기다.

## 조건 로그 능력치 이름 — v1.0.6-alpha.10 prerelease

2026-10-20 사용자의 prerelease 발행 요청에 따라 실제 능력치 참조로 조건 이름을 구하는 수정본을 준비한다. `Ch_3_6_4_3_001`을 학문으로 고정 치환하지 않으며, 해석 실패 시 기존 이름 처리와 그 오표시 가능성은 남는다. [alpha.10 변경·검증·한계](ISSUE-5-ALPHA10-RELEASE.md)에 자동 검사와 실제 게임 확인 대기를 구분한다.

사전 원격 main은 `656b493ab73cb1d1f36f1ebf00d02bbddb4cb33a`로 로컬 기준과 같고 기존 Release·태그는 각각 33개다. Latest 정식 v1.0.5와 기존 자산을 보존하고 새 버전만 `prerelease=true`·`make_latest=false`로 게시한다. 전체 후보의 14개 검사 통과 뒤 고정 커밋을 별도 소스 스냅샷에서 재빌드하고, 게시 후 익명 다운로드의 바이트·해시를 대조한다. 이 문단은 게시 전 준비 기록이며 결과는 후속 기록에 남긴다.

**2026-10-20 04:15:46 KST에 [alpha.10 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.10)를 게시했다.** 소스·태그 `df627348e3aa099a087389642d04f7ac4b0a845c`의 고정 재빌드·14개 검사와 검토 후보의 180개 입력·DLL/cfg/ZIP·전체 검사 결과 일치를 확인했다. Release `417652240`·asset `657185876`, `draft=false`·`prerelease=true`이며 익명 공개 ZIP의 CRC·두 파일·내부 바이트·SHA-256 대조를 통과했다. 기존 33개 Release·태그·자산과 Latest v1.0.5·이슈 상태를 보존했다. 게시 결과 문서 보충은 별도 main 커밋이며 고정 소스·태그·자산을 변경하지 않는다. [상세 결과·남은 한계](ISSUE-5-ALPHA10-RELEASE.md)를 따른다.

## PRD #5 — v1.0.6-alpha.9 prerelease

사용자는 로컬 구현 커밋 `da1f362`·`360ca57`을 `origin/main`에 반영하고 README 갱신·새 prerelease를 지시했다. 공통 포인터 어댑터·명시적인 취소 정책을 포함하며 조작·설정·단일 net48 DLL은 유지한다. DLL은 alpha.8보다 512바이트 증가한 620,032바이트이고 리소스와 외부 assembly 참조는 동일하다. [alpha.9 변경·설치·측정·검증](ISSUE-5-ALPHA9-RELEASE.md)을 따른다.

2026-10-20 사전 확인에서 alpha.9 Release·태그는 미사용이고 원격 main 계보와 push 권한을 확인했다. 기존 32개 Release·태그·자산, Latest 정식 v1.0.5와 이슈 #2 closed·#4 open·#5 open을 보존할 기준으로 보관했다. 전체 후보·격리 빌드 각각 14개 검사, 입력 179개·소스/리소스 73개와 동일 DLL/cfg/ZIP·검사 결과를 확인했다.

**2026-10-20 03:43:52 KST에 [alpha.9 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.9)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.9/DiceMaster-v1.0.6-alpha.9.zip). 두 구현 커밋과 README·준비 문서를 포함하는 고정 소스·태그 `af7bcd94ee1398717acec3066a0a907d588c49f9`를 main·주석 태그로 atomic push했다. 03:42:52 KST에 `verify-preview-release.py --scope prd5`가 기존 모드 DLL 0개에서 14개 프로그램을 통과했고 검토·격리 파일과 DLL/cfg/ZIP·전체 검사 결과가 일치했다. Release ID `417623770`·asset ID `657114332`, `draft=false`·`prerelease=true`·`make_latest=false`다. 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256과 기존 32개 Release·태그·자산, Latest v1.0.5, 이슈 #2 closed·#4 open·#5 open 보존을 확인했다. 영수증은 로컬 `artifacts/releases/v1.0.6-alpha.9/`의 `release-verification.json`·`build-manifest.json`·`anonymous-verification.json`·`public-release.json`·`github-publication.json`·`tag-verification.json`·`source-document-links.json`·`release-body.md` 등에 보관한다. 고정 검사 영수증의 `published=false`는 게시 전 시점이다. 게시 결과 보충은 빌드 소스·태그·공개 자산을 바꾸지 않는 별도 main 문서 커밋이며 Release 본문은 그 고정 문서 커밋의 공개 절대 URL로 연결한다. 실제 Windows·장치 확인과 이슈 #5 완료는 대기다.

## PRD #5 — v1.0.6-alpha.8 prerelease

사용자는 구조 리팩터링을 보류하고 터치 반복 실행 문제 수정·하단 실행 버튼의 텍스트 안내 전환·회귀 검사·커밋·`origin/main` 반영·README 갱신·새 prerelease를 지시했다. 시작 main은 `572ba1df65a18e94af20be21d566430dd84c13ba`다. 자동 입력 모드 관측만으로 같은 유효 행동에 접수된 확정을 취소하지 않으며 접수한 후보·결과를 보관해 이후 후보 갱신이 확정 대상을 바꾸지 않게 한다. 초점·문맥·대상·연결·소유자 등의 하드 경계와 실행 직전 검증은 유지한다.

하단 실행 버튼은 제거하고 A/Enter의 일반/명시 조건 무시 안내·B/Esc 무작위 안내를 텍스트로 표시한다. 클릭·터치 실행은 목록의 첫 선택 뒤 같은 행 재확인으로 모으고 무작위는 B/Esc로만 요청한다. 새 설정·키·패드 조합·구조 프레임워크는 추가하지 않는다. 터치만 사용하는 환경의 무작위 실행 UI는 없다.

**2026-10-20 03:18:58 KST에 [v1.0.6-alpha.8](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.8) prerelease를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.8/DiceMaster-v1.0.6-alpha.8.zip). 최신 `origin/main`의 `572ba1df65a18e94af20be21d566430dd84c13ba`에서 시작했고 README·구현·검사를 포함한 소스·태그 `7d79363bc33dffac80d453fe6a945a6e3edeb513`를 main에 반영했다. Release ID `417602057`·asset ID `657055734`, `draft=false`·`prerelease=true`·`make_latest=false`다.

전체·격리·고정 소스 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램과 동일 DLL/cfg/ZIP·전체 검사 결과를 확인했다. 고정 재빌드는 03:18:16 KST에 기존 모드 DLL 0개에서 완료했고 빌드 입력 176개·소스/리소스 71개를 대조했다. 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256도 확인했다. 런타임은 실제 후보 observer IL·소스 팝업/런타임·MoonSharp를 연결해 84개 시나리오·581개 단언을 통과했다. [alpha.8 배포 기록](ISSUE-5-ALPHA8-RELEASE.md)에 상세 결과를 기록한다.

기존 31개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 새 DLL의 실제 Windows·장치 확인은 [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에서 대기한다. 게시 결과 보충은 빌드 소스·태그·공개 자산을 바꾸지 않는 별도 main 문서 커밋이며 고정 소스의 준비 문구와 아래 alpha.7은 당시 이력이다. 영수증은 로컬 `artifacts/releases/v1.0.6-alpha.8/`의 `release-verification.json`·`build-manifest.json`·`anonymous-verification.json`·`github-publication.json`·`tag-verification.json`·`release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 실제 게시·익명 검증과 구분한다.

## PRD #5 — v1.0.6-alpha.7 prerelease

사용자의 같은 이벤트 팝업 후속 요청인 정상 발동 가능/불가 상태 행 제거·무작위 목록 항목 제거·첫 줄 제목 굵게 표시를 반영해 **2026-10-20 02:43:23 KST에 [v1.0.6-alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.7)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.7/DiceMaster-v1.0.6-alpha.7.zip). 최신 main `661622d8f221f25d58f871321caed935c952d6e9`에서 시작했고 README·구현·검사를 포함한 소스·태그 `5630a1a88a6f99cabad146c405810b1918ca83e1`를 main에 반영했다. Release ID `417571269`·asset ID `656972428`, `draft=false`·`prerelease=true`·`make_latest=false`다. [변경·설치·복구·검증 기록](ISSUE-5-ALPHA7-RELEASE.md)을 따른다.

전체·격리·고정 소스 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램과 검토 DLL/cfg/ZIP의 동일 바이트·전체 검사 결과 일치를 확인했다. 02:42:13 KST에 기존 모드 DLL 0개에서 고정 재빌드를 완료했고 빌드 입력 174개·소스/리소스 71개를 대조했다. 게시 후 태그·익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256 검증을 통과했다. 검사별 수치는 배포 기록에 둔다.

기존 30개 Release·태그·자산과 Latest v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.7/`의 `release-verification.json`, `build-manifest.json`, `anonymous-verification.json`, `github-publication.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 후속 게시·익명 검증과 구분한다.

새 DLL의 실제 Windows·장치 확인은 대기다. alpha.6의 [후속 UI 요청](ISSUE-5-ALPHA6-RELEASE.md#2026-10-20-사용자-후속-요청)은 새 DLL 확인 완료 근거가 아니다. 이번 게시 결과 문서는 별도 main 후속 커밋으로 반영하고 빌드 소스·태그·공개 자산을 바꾸지 않는다. 고정 소스의 준비 문구와 아래 alpha.6은 당시 기록이다.

## PRD #5 — v1.0.6-alpha.6 prerelease

사용자의 같은 이벤트 팝업 UI 후속 요청인 반복 클릭 안내 제거·로그와 같은 결과 효과색·정상 발동 불가 이름/상태 회색 표시를 반영해 **2026-10-20 02:29:50 KST에 [v1.0.6-alpha.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.6)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.6/DiceMaster-v1.0.6-alpha.6.zip). 최신 main `cb6c20fdef23d7b19c39171df114e855c3e4b363`에서 시작했고 README·구현·검사를 포함한 소스·태그 `b68046494656c03a7d0c9cb4a13b78258e73f52f`를 main에 반영했다. Release ID `417558935`·asset ID `656938962`, `draft=false`·`prerelease=true`·`make_latest=false`다. [변경·설치·복구·검증 기록](ISSUE-5-ALPHA6-RELEASE.md)을 따른다.

전체·격리·고정 소스 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램과 검토 DLL/cfg/ZIP의 동일 바이트·전체 검사 결과 일치를 확인했다. 02:29:06 KST에 기존 모드 DLL 0개에서 고정 재빌드를 완료했고 빌드 입력 173개·소스/리소스 71개를 대조했다. 게시 후 태그·익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256 검증을 통과했다. 검사별 수치는 배포 기록에 둔다.

기존 29개 Release·태그·자산과 Latest v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.6/`의 `release-verification.json`, `build-manifest.json`, `anonymous-verification.json`, `github-publication.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 후속 게시·익명 검증과 구분한다.

새 DLL의 실제 Windows·장치 확인은 대기다. alpha.5의 후속 UI 요청은 [당시 기록](ISSUE-5-ALPHA5-RELEASE.md#2026-10-20-사용자-후속-요청)에 한정하며 새 DLL 확인 완료로 쓰지 않는다. 이번 게시 결과 문서는 별도 main 후속 커밋으로 반영하고 빌드 소스·태그·공개 자산을 바꾸지 않는다. 고정 소스의 준비 문구와 아래 alpha.5는 당시 기록이다.

## PRD #5 — v1.0.6-alpha.5 prerelease

사용자의 로그 도구 선택 표시·장치 전환 시 화면 유지·팝업 결과 설명 구현과 회귀 검사·main 반영·README 갱신·새 prerelease 지시에 따라 **2026-10-20 02:09:55 KST에 [v1.0.6-alpha.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.5)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.5/DiceMaster-v1.0.6-alpha.5.zip). 최신 main `3a5ffdf85f5965623c6c585032086976515dd98a`에서 시작했고 README·구현·검사를 포함한 소스·태그 `3f138728c109651533f25b5e7aee3f147a082d14`를 main에 반영했다. Release ID `417540991`·asset ID `656889735`, `draft=false`·`prerelease=true`·`make_latest=false`다. [변경·설치·복구·검증 기록](ISSUE-5-ALPHA5-RELEASE.md)을 따른다.

전체·격리·고정 소스 커밋 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램과 검토 DLL/cfg/ZIP의 동일 바이트·전체 검사 결과 일치를 확인했다. 고정 재빌드는 기존 모드 DLL 0개에서 실행했고 빌드 입력 171개·소스/리소스 70개를 대조했다. 게시 후 태그·익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256 검증을 통과했다. 검사별 수치는 배포 기록에 둔다.

기존 28개 Release·태그·자산과 Latest v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.5/`의 `release-verification.json`, `build-manifest.json`, `anonymous-verification.json`, `github-publication.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 후속 게시·익명 검증과 구분한다.

새 DLL의 실제 Windows·장치 확인은 대기다. alpha.4에 대한 사용자 보고는 [당시 후속 기록](ISSUE-5-ALPHA4-RELEASE.md#2026-10-20-사용자-후속-확인)에 한정하며 새 DLL 통과로 승계하지 않는다. 이번 게시 결과 문서는 별도 main 후속 커밋으로 반영하고 빌드 소스·태그·공개 자산은 바꾸지 않는다. 고정 소스 문서의 준비 문구와 아래 alpha.4의 수치는 당시 이력이다.

## PRD #5 — v1.0.6-alpha.4 prerelease

사용자의 두 터치 문제 수정·회귀 검사·커밋·`origin/main` 반영·README 갱신·새 prerelease 지시에 따라 **2026-10-20 01:47:20 KST에 [v1.0.6-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.4)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.4/DiceMaster-v1.0.6-alpha.4.zip). 최신 main `ea11afbda83433c58eeddc90cada3d021013bdda`에서 시작했고 README·구현·검사를 포함한 소스·태그 `bea9e87f8a9e138150fd6b70e2d17f0fcb92ccce`를 main에 반영했다. Release ID `417518975`·asset ID `656831696`, `draft=false`·`prerelease=true`·`make_latest=false`다. [alpha.4 변경·설치·복구·검증 기록](ISSUE-5-ALPHA4-RELEASE.md)을 따른다.

전체·격리·고정 소스 커밋 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램과 검토 DLL/cfg/ZIP의 동일 바이트·검사 결과 일치를 확인했다. 기존 모드 DLL 0개에서 고정 재빌드했으며 빌드 입력 167개·소스/리소스 69개를 대조했다. 게시 후 태그와 익명 공개 페이지·다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256 검증을 통과했다. 자세한 검사별 수치는 배포 기록에 둔다.

기존 27개 Release·태그·자산과 Latest v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.4/`의 `release-verification.json`, `build-manifest.json`, `anonymous-verification.json`, `github-publication.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 고정 재빌드 영수증의 `published=false`는 게시 전 검사 시점이며 후속 게시·익명 검증과 구분한다.

새 DLL의 실제 Windows·장치 확인은 대기다. alpha.3의 사용자 보고는 [당시 후속 기록](ISSUE-5-ALPHA3-RELEASE.md#2026-10-20-사용자-후속-확인)에 한정하며 새 후보 통과로 승계하지 않는다. 게시 결과를 보충하는 이번 문서 커밋은 main에 별도로 반영하며 빌드 소스·태그·공개 설치 자산을 바꾸지 않는다. 고정 소스 문서의 준비 문구와 아래 alpha.3의 수치는 당시 이력이다.

## PRD #5 — v1.0.6-alpha.3 prerelease

사용자의 전체 로그 터치 수정·팝업 선택 행 재클릭/터치 진행 요청에 따라 **2026-10-20 01:23:02 KST에 [v1.0.6-alpha.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.3)를 게시했다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.3/DiceMaster-v1.0.6-alpha.3.zip). 최신 `origin/main`을 기준으로 구현·검사·README를 main에 반영했으며 소스·태그는 `b2036a9c8a08d259c6cecee4ee360f24c4af3d4b`다. Release ID `417499227`·asset ID `656778224`, `draft=false`·`prerelease=true`·`make_latest=false`다. [변경·설치·복구·검증 상태](ISSUE-5-ALPHA3-RELEASE.md)를 따른다.

전체 소스·격리 소스·고정 커밋 `verify-preview-release.py --scope prd5` 재빌드에서 각각 14개 검사 프로그램을 통과했고 검토 DLL/cfg/ZIP과 동일 바이트였다. 공통 UI 2,743개·팝업 981개·후보 DLL 연결 83개, 빌드 입력 166개·모드 소스/리소스 69개를 확인했다. 게시 후 공개 태그·인증 없는 공개 페이지·다운로드의 SHA-256·CRC·정확히 두 파일·내부 바이트도 대조했다.

사전 main `b0d4b291d4b3dbbfb7046134a5eae5875b1f2fa9`에서 검증한 소스 `b2036a9`로 main을 갱신했다. 기존 26개 Release·태그·자산과 Latest 정식 v1.0.5, 이슈 #2 closed·#4 open·#5 open을 보존했다. 영수증은 `artifacts/releases/v1.0.6-alpha.3/`의 `release-verification.json`, `anonymous-verification.json`, `public-release.json`, `tag-verification.json`, `release-body.md` 등에 보존한다. 게시 결과를 보충하는 후속 문서 커밋은 main에 반영하며 빌드 소스·태그·공개 설치 자산을 변경하지 않는다.

새 DLL의 실제 Windows·장치 확인은 대기이며 alpha.2 팝업 터치 행 선택 성공을 새 DLL의 통과로 쓰지 않는다. 아래 alpha.2의 별도 브랜치 게시 방식은 당시 이력이다.

## PRD #5 — v1.0.6-alpha.2 prerelease

사용자의 A 확인 통일·클릭/터치 수정과 검증 후 커밋·prerelease 지시에 따라 **2026-10-20 01:02:17 KST에 [v1.0.6-alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.2)를 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.2/DiceMaster-v1.0.6-alpha.2.zip). 소스·태그는 `e71020020b1b7f1938f22ff4a60d046c8bace839`, Release ID `417484069`·asset ID `656733547`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. [변경·설치·복구·검증 상태](ISSUE-5-ALPHA2-RELEASE.md)를 따릅니다. 새 Windows 확인과 이슈 #5 완료는 대기입니다.

`verify-preview-release.py --scope prd5`로 기존 모드 DLL 0개에서 고정 커밋을 재빌드하고 14개 자동 검사·검토 DLL/cfg/ZIP 동일 바이트를 확인했습니다. 인증 없는 공개 페이지·설치 ZIP의 SHA-256·CRC·정확히 두 파일·내부 바이트와 공개 태그를 대조했습니다. 기존 `codex/prd5-prerelease` 작업 브랜치와 새 태그를 사용했고 기본 main은 변경하지 않았습니다. Latest v1.0.5와 기존 25개 Release·태그·자산, 이슈 #2 closed·#4 open·#5 open을 보존했습니다. 영수증은 `artifacts/releases/v1.0.6-alpha.2/`의 `release-verification.json`, `anonymous-verification.json`, `public-release.json` 등에 둡니다. 게시 결과를 보충하는 후속 문서 커밋은 소스 태그·설치 자산을 변경하지 않습니다.

## PRD #5 — v1.0.6-alpha.1 prerelease 게시 이력

**2026-10-20 00:32:06 KST에 [v1.0.6-alpha.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.1) prerelease를 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.1/DiceMaster-v1.0.6-alpha.1.zip). 고정 소스·태그 `9c257b76b8a551022b6ea606c35b91e870c4a654`의 새 빌드·14개 검사와 공개 다운로드의 SHA-256·CRC·정확히 두 파일·내부 바이트를 확인했습니다. Latest 정식 v1.0.5와 기존 24개 Release·태그·자산을 보존했습니다. 새 DLL의 Windows 확인과 이슈 #5 완료는 대기입니다.

아래 준비 절차는 게시 직전 기록입니다. 최종 식별·설치·복구·확인 범위는 [배포 기록](ISSUE-5-ALPHA1-RELEASE.md)을 따릅니다.

2026-10-20 사용자가 현재 PRD #5 구현의 커밋·prerelease 게시를 지시했습니다. 대상은 `v1.0.6-alpha.1`, 자산은 `DiceMaster-v1.0.6-alpha.1.zip`이며 [변경·지원 한계·설치·복구·SHA-256](ISSUE-5-ALPHA1-RELEASE.md)을 게시 설명으로 사용합니다. 이번 행동의 일반·명시 조건 무시·무작위 선택 팝업과 이벤트 예약/99999 보정 제거, 미션·주사위 유지가 범위입니다. 기본 ON 설정·로그 도구 저장·상위 설정, 이벤트만 검색/필터, 공유 입력 해제·중립과 제한된 Lua 지원 경로를 함께 안내합니다.

사전 원격 확인에서 기존 Release·태그는 각각 24개이며 대상 버전은 없습니다. `prerelease=true`·`make_latest=false`로 새 후보만 게시하고 Latest 정식 v1.0.5와 기존 태그·Release·자산을 유지합니다. 소스 커밋은 실제 커밋 후 확정하고 `verify-preview-release.py --scope prd5`의 고정 커밋 전체 재빌드·14개 검사와 검토 DLL/cfg/ZIP 일치, 게시 후 태그·공개 다운로드의 SHA-256·CRC·정확히 두 파일·내부 바이트를 확인합니다. 결과 영수증은 `artifacts/releases/v1.0.6-alpha.1/`에 보존합니다. 이 절은 게시 전 준비 기록이며 성공·게시 시각·원격 ID는 아직 기록하지 않습니다.

검토 DLL SHA-256은 `fc2baea75bf1d1bb856411af49c4485c19f3369e77f573524dd2da56733a789c`, cfg는 `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7`, ZIP은 `f512a1c11f9a39eefd0ae37174b116ed8316c7b33db5ef1fddb49ff2bffac019`입니다. 로컬 전체 빌드·14개 검사와 격리 소스 재빌드는 통과했으며 최종 고정 커밋 검증과 구분합니다. 게임 소스·게임 DLL은 배포하지 않고 기존 cfg를 보존해 DLL만 교체합니다. **새 DLL의 Windows 확인은 대기**이며 v1.0.5 사용자 확인을 승계하지 않습니다. [확인표](ISSUE-5-WINDOWS-VALIDATION.md)에 별도 결과를 남기며 이슈 #5를 종료하지 않습니다.

## v1.0.5 정식 Release·Latest 게시

사용자의 “지금까지 작업한 내용을 release로 반영해” 지시에 따라 **2026-10-19 23:59:15 KST에 정식 [v1.0.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5)를 게시하고 Latest로 지정했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5/DiceMaster-v1.0.5.zip)은 alpha.1~alpha.9에서 반영한 로그 GUI·Xbox/XInput 탐색·한국어 표시·예약 표시/해제를 포함합니다. 소스·태그 `8886309e2a7248fa13544f81546e3c4795ff9611`, Release ID `417427722`·asset ID `656586246`, `draft=false`·`prerelease=false`·`make_latest=true`입니다. [전체 변경·설치·복구·Windows 확인](ISSUE-4-STABLE-RELEASE.md)을 따릅니다.

이번 정식 전환은 문서 6개만 커밋했으며 `.agents/`·`skills-lock.json`은 제외했습니다. Git 스냅샷의 입력 153개·소스 63개와 DLL/cfg/ZIP이 고정 재빌드·12개 검사·익명 다운로드를 검증한 alpha.9와 동일합니다. 재빌드·검사 재실행·재압축 없이 같은 파일을 승격했으며 원래 alpha.9 검사 기록을 새 실행으로 표현하지 않습니다. 독립 감사도 동일 입력·바이트와 기존 검증의 출처를 확인했습니다.

DLL은 585,728바이트·`39e30eb7816e140e8175c9a44da14b5e04fdfefe3398e3b56dafc92ac0e49339`, cfg는 11,175바이트·`bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`, ZIP은 246,335바이트·`98fd7ed0a6a3232ebc6cf6bde29a333cba08ca5ce40b647c4eb3beaba527db04`입니다. 공개 페이지와 ZIP을 인증 없이 확인해 SHA-256·CRC·정확히 두 파일·내부 바이트를 검증했습니다. 기존 23개 Release·23개 태그·자산을 보존했고 이전 정식 v1.0.4는 복구용으로 유지합니다. Latest만 v1.0.5로 바뀌었으며 이슈 #2 closed·#4 open은 유지합니다.

영수증은 `artifacts/releases/v1.0.5/`의 `promotion-verification.json`, `origin-alpha9-*`, `independent-audit.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`, `release-body.md` 등에 보존합니다. 게시 결과 문서의 후속 커밋은 소스 태그·설치 자산을 바꾸지 않습니다. **정식 게시 지시는 Windows 확인 완료를 뜻하지 않으며 에이전트는 게임을 설치하거나 실행하지 않았습니다. 실제 패드·스크롤·버튼·터치와 이슈 #4 완료는 사용자 확인 대기입니다.**

## PRD #4 alpha.9 목록 예약 표시·해제 게시

사용자의 커밋·prerelease 지시에 따라 **2026-10-19 23:23:31 KST에 [v1.0.5-alpha.9](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.9)를 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.9/DiceMaster-v1.0.5-alpha.9.zip)은 목록 `(예약 중)` 표시와 새 A/Enter/Space·선택된 행 재클릭/터치의 예약·해제를 포함합니다. 미션 해제는 확인 가능한 모드 소유 활성 항목만 복원하며 마지막 명시 취소 대상의 즉시 자동 재예약을 같은 문맥에서 억제합니다. 이미 소비된 결과·게임 진행은 되감지 않으며 [설치·복구·Windows 확인표](ISSUE-4-ALPHA9-RELEASE.md)를 따릅니다.

소스·태그는 `7d9c9ec0cfaadbb968c35cfaa5111912c1c68f45`, Release ID `417385047`·asset ID `656496078`, `draft=false`·`prerelease=true`·`make_latest=false`입니다. 구현·검사·문서 16개 파일만 커밋했고 `.agents/`·`skills-lock.json`은 제외했습니다. 고정 커밋의 깨끗한 전체 재빌드·12개 검사·입력 153개·소스 63개와 검토 DLL/cfg/ZIP 바이트 일치를 확인했습니다. UI/입력 3,472개·실제 Bridge 통합 400개·모델 375개·DLL 연결 71개와 기존 분석·갱신·번역·주사위·패드 검사를 포함하며 독립 감사도 통과했습니다.

DLL은 585,728바이트·`39e30eb7816e140e8175c9a44da14b5e04fdfefe3398e3b56dafc92ac0e49339`, cfg는 11,175바이트·`bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`, ZIP은 246,335바이트·`98fd7ed0a6a3232ebc6cf6bde29a333cba08ca5ce40b647c4eb3beaba527db04`입니다. ZIP에는 cfg/새 DLL 두 파일만 있으며 게임 소스·게임 DLL을 포함하지 않습니다. 공개 페이지와 ZIP을 인증 없이 확인해 SHA-256·CRC·두 파일·내부 바이트를 검증했습니다. 기존 22개 Release·22개 태그·자산과 Latest 정식 v1.0.4, 이슈 #2 closed·#4 open을 보존했습니다.

영수증은 `artifacts/releases/v1.0.5-alpha.9/`에 보존합니다. 게시 결과 문서의 후속 커밋은 소스 태그·공개 설치 파일을 바꾸지 않습니다. **게임 설치·실행은 수행하지 않았으며 실제 Windows 패드·스크롤·버튼·터치와 이슈 #4 완료는 사용자 확인 대기입니다.**

## PRD #4 alpha.8 목록에서 바로 예약 게시

사용자의 커밋·prerelease 지시에 따라 **2026-10-19 22:58:37 KST에 [v1.0.5-alpha.8](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.8)을 게시했습니다.** [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.8/DiceMaster-v1.0.5-alpha.8.zip)은 이벤트·미션의 목록 직접 예약과 읽기 전용 상세 상태 표시를 포함합니다. 새 A/Enter/Space와 선택된 행 재클릭·터치로 기존 예약을 한 번 설정하며 상세 버튼·포커스를 제거합니다. 예약은 취소 토글·즉시 실행이 아니며 [설치·복구·Windows 확인표](ISSUE-4-ALPHA8-RELEASE.md)를 따릅니다.

소스·태그는 `ce67f91704533e3db60985ca081b58e238cdd13e`, Release ID `417357418`·asset ID `656434402`, `draft=false`·`prerelease=true`·`make_latest=false`입니다. 구현·검사·문서 9개 파일만 커밋했고 `.agents/`·`skills-lock.json`은 제외했습니다. 고정 커밋의 깨끗한 전체 소스 재빌드·12개 검사·입력 153개·소스 63개와 검토 후보 DLL/cfg/ZIP 바이트 일치를 확인했습니다. UI/입력 3,201개·실제 DLL 연결 64개·패드 열기 35개 시나리오/1,408개와 기존 분석·갱신·주사위·번역 검사를 포함하며 독립 비교도 통과했습니다.

DLL은 582,656바이트·`7657962841ad221fa3c3254fe4fc88d297a524d3f7eb7640a89bd939bb28622f`, cfg는 11,175바이트·`bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`, ZIP은 245,090바이트·`51d23774b16c53b1378d34f735cc461f0a440a8f075ab82825d9c5b3589412ea`입니다. ZIP에는 DLL/cfg 두 파일만 있으며 게임 소스·DLL을 포함하지 않습니다. 공개 페이지·ZIP을 인증 없이 확인해 CRC·두 파일·내부 바이트·SHA-256을 검증했습니다. 기존 21개 Release·21개 태그·자산과 Latest 정식 v1.0.4, 이슈 #2 closed·#4 open을 보존했습니다.

영수증은 `artifacts/releases/v1.0.5-alpha.8/`에 보존합니다. 게시 결과 문서의 후속 커밋은 소스 태그·공개 설치 파일을 바꾸지 않습니다. **게임 설치·실행은 수행하지 않았으며 실제 Windows 패드·스크롤·버튼·터치 확인과 이슈 #4 완료는 사용자 확인 대기입니다.**

## PRD #4 alpha.7 한국어 표시·상태·패드 겹침 열기 게시

사용자의 커밋·prerelease 명시적 지시에 따라 **2026-10-19 06:50:38 KST에 [v1.0.5-alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.7)를 게시했습니다.** [설치 자산](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.7/DiceMaster-v1.0.5-alpha.7.zip)은 한국어 표시 경로 통일·이스케이프 등호/전체 문장 매칭, 정상 조회 문구 생략, 시간 제한 없는 동일 패드 L3/R3 겹침 열기를 포함합니다. 원본 ID·예약 대상과 반복/해제·중립 보호를 유지하며 [설치·복구·Windows 단계표](ISSUE-4-ALPHA7-RELEASE.md)를 따릅니다.

소스·태그 커밋은 `3746dbf0153fe131226bb149d68ba18e43e9fdd9`, Release ID는 `416732905`, asset ID는 `654566578`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 커밋에는 이번에 검토한 24개 파일만 포함했고 기존 `.agents/`·`skills-lock.json`은 제외했습니다. 고정 커밋을 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 12개 검사·입력 153개·생산 소스 63개와 검토 DLL/cfg/ZIP 바이트 일치를 확인했습니다. 번역·서술 로그 3,511개·UI/입력 3,073개·실제 DLL 연결 55개와 기존 패드·분석·갱신 회귀를 포함합니다.

DLL은 583,168바이트·`17044b1c90ce1e65ef1b253b6ebd131a202a787e3eb59e776ed1ac68e8bc18e4`, cfg는 11,175바이트·`bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`, ZIP은 245,281바이트·`922e6601c2c5d929c3458e9ad3d46e1ac690e7e8419ea981ee8f085e448b9d2b`입니다. ZIP에는 DLL/cfg 두 파일뿐이며 게임 소스·DLL은 포함하지 않습니다. 공개 페이지와 ZIP을 인증 없이 접근·다운로드해 CRC·두 파일·내부 바이트·해시를 확인했습니다. 기존 20개 Release·20개 태그·자산과 Latest 정식 v1.0.4, 이슈 #2 closed·#4 open을 보존했습니다.

`artifacts/releases/v1.0.5-alpha.7/`에 `release-verification.json`, `reviewed-candidate-comparison.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`, `release-body.md`와 로그를 보존합니다. 게시 결과를 기록하는 후속 문서 커밋은 태그·공개 설치 자산을 바꾸지 않습니다. **실제 Windows 게임을 설치하거나 실행하지 않았으며 사용자 확인과 이슈 #4 완료는 대기입니다.**

## PRD #4 alpha.6 읽기 화면·조건 분기·패드 열기 게시

2026-10-19 KST에 사용자가 현재 수정의 커밋·prerelease 작성을 명시적으로 지시했습니다. [v1.0.5-alpha.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.6)의 설치 자산은 [DiceMaster-v1.0.5-alpha.6.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.6/DiceMaster-v1.0.5-alpha.6.zip)이며 [변경·SHA-256·설치·복구·Windows 확인표](ISSUE-4-ALPHA6-RELEASE.md)를 사용합니다. alpha.5 이후의 1920×1200 기준 872×960·20px 기본 배치, 선택지 읽기 전용 화면, 조건 분기 블록, 미션 정상 문구 생략·자동 스크롤바와 양쪽 스틱 클릭 열기를 포함합니다.

패드 열기는 사용자 후속 지시로 초기 제외 조건을 대체합니다. 같은 패드의 첫 스틱 클릭을 누른 채 반대쪽 클릭을 300ms 미만에 겹쳐 열며 기존 Tab·설정 키와 B 복귀·닫기를 유지합니다. `[DiceMaster].EnableGamepadLogOpen=true`가 기본이고 주사위 조정 옵션과 독립적입니다. 첫 클릭 중복을 막기 위해 옵션 활성·게임 초점·로그 닫힘에서 단독 L3/R3도 게임에 전달하지 않습니다. 이 제약, `false`와 해제·중립 후 복구, 게임 바인딩 자료 부재와 기존 Tab 진입 첫 입력 문제의 미해결 범위를 게시 본문에 포함합니다.

검토 DLL은 582,656바이트·`4bcce037713407e25378ecbd2113649c9f9e52968659b1b748a6bec795645efa`, 설정은 11,084바이트·`efd4847eb096aad70bfb7c3399534caab6becf71daca75fc5359100b0638cbf8`, ZIP은 245,066바이트·`fe26f96e3c9512c7a31c10226ff3ba13a13f81b5ec8c6916c44d3376184f5cb0`입니다. 로컬 전체 12개 프로그램, 패드 열기 993개 단언·28개 시나리오, UI·입력 2,720·모델 372·실제 DLL 연결 47·helper 통합 203개를 통과했습니다. [로컬 manifest](../artifacts/issue-4/pad-open/build-manifest.json)와 [독립 검증 영수증](../artifacts/issue-4/pad-open-evidence/local-verification.json)에 입력 153개·ZIP CRC·두 파일·내부 바이트·해시를 기록했습니다.

사전 원격 확인에서 main은 로컬 기준 `8aa292970b8f46d5c5007bb29a61124b08577665`와 일치하고 기존 Release·태그는 각각 19개입니다. alpha.6 태그·Release는 없으며 Latest 정식 v1.0.4, 이슈 #2 closed·#4 open을 확인했습니다. 게시 검증기의 필수 검사 목록을 12개로 보완하고 검증기 회귀 8개를 통과했습니다. 이때 빌드 도구 2개의 입력 해시만 바뀌었고 생산 소스 입력 63개는 동일합니다. 앞의 로컬 153개 입력 영수증은 후보 생성 당시 스냅샷이며, 최종 입력 근거는 아래 고정 커밋 재빌드 manifest로 분리합니다.

**2026-10-19 06:21:15 KST에 prerelease를 게시했습니다.** 소스·태그 커밋은 `de0d511e60c99a6b84be33c9e3d7676746986c76`, Release ID는 `416720471`, asset ID는 `654515746`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 해당 커밋을 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 전체 12개 검사와 입력 153개·생산 소스 63개 일치를 확인했습니다. DLL·설정·ZIP은 검토한 후보와 같은 바이트입니다.

인증 없는 공개 페이지 접근·설치 ZIP 다운로드에서 SHA-256·CRC·정확히 두 파일·DLL/설정 내부 바이트를 확인했습니다. 기존 19개 Release·태그 커밋·자산과 Latest 정식 v1.0.4, 이슈 #2 closed·#4 open을 보존했습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.6/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 빌드·검증 로그에 있습니다.

실제 공개 본문은 같은 폴더의 `release-body.md`로 보존합니다. 임시 준비 문구를 검증·설치 안내로 바꾸고 문서 링크를 고정 소스 URL로 풀었으며 로컬 산출물 링크를 제거했습니다. 게시 기록의 후속 문서 커밋은 태그·공개 설치 자산을 바꾸지 않습니다. **Windows 게임 설치·실행은 수행하지 않았으며 실제 패드·스크롤·버튼·터치 확인은 사용자 대기입니다. 이슈 #4를 완료 처리하지 않았습니다.**

## PRD #4 alpha.5 화면·탭·터치·미션 안내 후속

사용자가 현재 수정의 커밋·prerelease 게시를 명시적으로 지시했습니다. 대상은 `v1.0.5-alpha.5`, 설치 자산은 `DiceMaster-v1.0.5-alpha.5.zip`이며 [변경·설치·복구·해시·Windows 확인표](ISSUE-4-ALPHA5-RELEASE.md)를 사용합니다. 화면 60%×70% 첫 표시, 선택지 전체 공개·조건 목록/설명, 활성 예약 버튼 직접 포커스, 패드 모드 터치 복구, 검색 제거와 미션 상태 안내 비활성화를 포함합니다.

검토 DLL `1ce1f0fa8d4d0dba626102c787c5e382cea5414861c050397ab4e506fe2b50d0`·설정 `fce24616e5380004bc87aa992871407386d90bf1b6ec6f30116a29347fe0f046`을 요구하고 고정 커밋을 `verify-preview-release.py --scope prd4`로 재빌드합니다. 로컬 11개 프로그램·UI 2,597·모델 361·실제 DLL 연결 40·Bridge 통합 182 단언을 통과했습니다. 게시 검증 출력은 `artifacts/releases/v1.0.5-alpha.5/`입니다.

사전 원격 확인에서 main은 작업 기준 `21b91e97eeb6bfad10b6bda5a6677f59f4a4af7c`과 일치하며 기존 Release·태그는 각각 18개입니다. Latest 정식 v1.0.4, 이슈 #2 closed·#4 open이고 alpha.5 태그·Release는 없습니다. 새 prerelease는 `prerelease=true`·`make_latest=false`로 게시하며 기존 Release·태그·자산을 보존합니다. 실제 게시·공개 다운로드 결과는 후속 기록합니다. 게임 설치·실행과 이슈 #4 완료는 수행하지 않습니다.

**2026-10-19 05:37:30 KST에 [v1.0.5-alpha.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.5) prerelease를 게시했습니다.** 소스 태그 커밋은 `70e045e17351ffd01ff0e19f5f8ba638b2b062c4`, Release ID는 `416698408`, asset ID는 `654431988`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.5/DiceMaster-v1.0.5-alpha.5.zip)은 고정 커밋의 깨끗한 재빌드·전체 11개 검사와 검토 후보의 DLL·cfg·ZIP 해시 일치를 확인한 파일입니다.

인증 없는 공개 페이지·ZIP 다운로드에서 CRC·정확히 두 파일·DLL/설정 바이트·SHA-256을 확인했습니다. 기존 18개 Release·태그 커밋·자산, Latest 정식 v1.0.4, 이슈 #2 closed·#4 open을 보존했습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.5/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 로그에 있습니다. 게시 본문은 같은 폴더의 `release-body.md`로 보존합니다. 커밋 문서에서 준비 문구를 검증·설치 안내로 바꾸고, 상대 문서 링크를 고정 소스 URL로 풀었으며 조회 실패 시 읽기 가능한 선택 보존 문구를 명확히 했습니다. 이 게시 기록의 후속 문서 커밋은 태그·공개 ZIP을 바꾸지 않습니다. **실제 Windows 게임 확인은 대기이며 이슈 #4를 완료하지 않았습니다.**

## PRD #4 alpha.4 카드 탐색·창 크기·목록 후속

사용자가 현재 수정의 커밋 후 prerelease 게시를 명시적으로 지시했습니다. 대상은 `v1.0.5-alpha.4`, 설치 자산은 `DiceMaster-v1.0.5-alpha.4.zip`이며 [게시 설명·설치·복구·해시](ISSUE-4-ALPHA4-RELEASE.md)를 사용합니다. 선택지·조건의 ← 접기·펼친 상태 ↑↓ 이동과 자동 갱신 추종, 기본 창 1280×960, 미션 목록 행 높이와 상태 문구를 수정합니다. 게임 설치·실행과 이슈 #4 완료는 수행하지 않습니다.

검토 DLL `06485af8929579f6f7d3744f183aa3359ec40545a6d155257c4bfebadc3b7d44`·설정 `b351703e2b0f1b5ea6b4a5894c1f5309a13dece5d6b06ed909330c0771801259`를 요구하고 고정 커밋을 `verify-preview-release.py --scope prd4`로 재빌드합니다. 전체 11개 프로그램·UI 11,996·실제 DLL 연결 23 단언과 ZIP/바이트/해시를 확인합니다. 출력은 `artifacts/releases/v1.0.5-alpha.4/`입니다.

사전 GitHub 확인에서 기존 Release·태그는 각각 17개, Latest 정식은 v1.0.4, 이슈 #2 closed·#4 open이고 alpha.4는 없습니다. 원격 main은 작업 기준 `9aebf796ca7bbf4da7b35899ad7ce107676dd578`과 일치합니다. 새 prerelease는 `prerelease=true`·`make_latest=false`로 게시하고 기존 태그·Release·자산을 보존합니다. 실제 게시와 공개 다운로드 결과는 후속 기록으로 구분합니다.

**2026-10-19 04:46:13 KST에 [v1.0.5-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.4) prerelease를 게시했습니다.** 태그 소스 커밋은 `2bb45aa75b3c863f91d70bf1d78fd413c9a6a45d`, Release ID는 `416665943`, asset ID는 `654324328`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 고정 커밋 재빌드와 전체 11개 검사, UI 11,996/연결 23 단언을 통과했고 검토 후보와 DLL·설정·ZIP 바이트가 일치했습니다. 설치 파일은 [DiceMaster-v1.0.5-alpha.4.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.4/DiceMaster-v1.0.5-alpha.4.zip)입니다.

인증 없는 공개 페이지·ZIP 다운로드에서 CRC·정확히 두 파일·DLL/설정 바이트·SHA-256을 확인했습니다. 기존 17개 Release·태그 커밋·자산, Latest 정식 v1.0.4, 이슈 #2 closed와 #4 open을 보존했습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.4/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 로그에 있습니다. 게시 본문은 태그 문서의 게시 준비 문장 두 곳만 중립적인 제공·검증 안내로 바꿨고 `release-body.md`로 보존합니다. 게시 기록 문서의 후속 커밋은 태그·설치 자산을 바꾸지 않습니다. **실제 Windows 게임 확인은 대기이며 이슈 #4를 완료 처리하지 않았습니다.**

## PRD #4 alpha.3 작은 빈 창 후속

alpha.2에서 좌측 상단의 작은 빈 창만 보인다는 실제 Windows 실패를 확인하고 자동 크기 창을 고정 사각형 창으로 교체합니다. 기존 prerelease 지시의 후속인 `v1.0.5-alpha.3`은 [원인·검사 보강](ISSUE-4-WINDOW-SIZE-REGRESSION.md)과 [설치·복구·해시](ISSUE-4-ALPHA3-RELEASE.md)를 따릅니다. 에이전트는 게임을 실행하지 않았으며 실제 수정 성공과 이슈 #4 완료는 주장하지 않습니다.

검토 DLL `e432c5e118760a3d5d75eba9f0920db1ac7bb73bdc0aada3d91ea74e178b3fb9`, 설정 `2ed5841519cf3ef6719477b0e6b69de989c4b02c5e5fa883b303b786db2faafc`를 요구하고 고정 소스 커밋을 `verify-preview-release.py --scope prd4`로 재빌드합니다. 전체 11개 프로그램·UI 11,840·새 실제 DLL 연결 23 단언, 이전 호출로 되돌리면 실패하는 회귀·공개 alpha.2 DLL 거부를 확인했습니다. 출력은 `artifacts/releases/v1.0.5-alpha.3/`입니다. 소스 커밋 `3b245130a1e6530e4b482d2a4965eb3456d6ec91`의 깨끗한 재빌드·11개 검사와 검토 DLL/설정/ZIP 해시 일치를 확인했습니다. 사용자의 명시적 게시 승인 뒤 커밋·태그를 푸시했고 실제 게시 결과는 아래에 기록합니다. 사전 확인의 기존 16개 Release·태그·자산과 정식 Latest v1.0.4를 보존하고 이슈 #4는 open을 유지합니다. 게시·인증 없는 공개 페이지/ZIP 다운로드 결과는 별도로 기록합니다.

**2026-10-19 04:24:38 KST에 [v1.0.5-alpha.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.3) prerelease를 게시했습니다.** 소스 태그 커밋은 `3b245130a1e6530e4b482d2a4965eb3456d6ec91`, Release ID는 `416651578`, asset ID는 `654282422`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 구현·후보 검증 문서 커밋을 푸시했고 고정 소스 스냅샷의 깨끗한 재빌드·11개 프로그램·UI 11,840/실제 DLL 연결 23 단언과 검토 DLL·설정·ZIP 해시 일치를 확인했습니다. 공개 설치 파일은 [DiceMaster-v1.0.5-alpha.3.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.3/DiceMaster-v1.0.5-alpha.3.zip)입니다.

Release·태그·보존 상태는 인증된 GitHub API로 확인했고 공개 페이지와 ZIP은 인증 없이 접근·다운로드했습니다. ZIP CRC·정확히 두 파일·DLL/설정 바이트·해시가 검증한 패키지와 일치합니다. 기존 16개 Release·태그 커밋·자산, Latest 정식 v1.0.4, 이슈 #2 closed와 #4 open을 보존했습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.3/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 빌드/검증 로그에 있습니다. 게시 결과를 기록하는 후속 문서 커밋은 태그·공개 자산을 바꾸지 않습니다. **실제 Windows 창 표시·패드 탐색·스크롤·버튼·드래그와 회귀 확인은 대기이며 이슈 #4를 완료 처리하지 않았습니다.**

## PRD #4 alpha.2 UI 크기·스크롤·창 이동 후속

사용자가 2026-10-19 KST에 현재 UI 수정의 commit·push·prerelease 게시를 지시했습니다. 대상은 `v1.0.5-alpha.2`, 설치 자산은 `DiceMaster-v1.0.5-alpha.2.zip`이며 [alpha.2 게시 설명](ISSUE-4-ALPHA2-RELEASE.md)을 사용합니다. alpha.1의 작은 UI·좁은 본문·포커스 스크롤 실패 보고와 전체 창 드래그 복원 요청에 대응한 변경입니다. 실제 Windows에서 수정 성공이나 PRD #4 완료를 확인한 것은 아닙니다.

검토 후보의 DLL SHA-256은 `ad9aa4b460382c81ae72e80596b3b12b5d6583976a8bfa5100ab60d68c2a6646`, 설정 SHA-256은 `2ed5841519cf3ef6719477b0e6b69de989c4b02c5e5fa883b303b786db2faafc`입니다. 변경을 커밋한 뒤 `verify-preview-release.py --scope prd4`로 해당 커밋만 추출하여 재빌드하고 11개 검사·ZIP/해시를 확인합니다. 게시 출력은 `artifacts/releases/v1.0.5-alpha.2/`입니다.

사전 공개 API 확인에서 기존 Release는 15개, Latest 정식은 v1.0.4, 이슈 #2는 closed, #4는 open이고 alpha.2 태그/Release는 없습니다. 새 prerelease는 `prerelease=true`·`make_latest=false`로 게시하며 기존 태그·Release·자산을 보존합니다. 실제 게시와 인증 없는 다운로드 결과는 아래 후속 기록으로 구분합니다. 게임 설치·실행과 이슈 종료는 수행하지 않습니다.

**2026-10-19 04:13:07 KST에 [v1.0.5-alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.2) prerelease를 게시했습니다.** 태그 소스 커밋은 `389292a85469c046032d498561a3470387429f61`, Release ID는 `416646044`, asset ID는 `654259429`이며 `draft=false`·`prerelease=true`·`make_latest=false`입니다. 고정 커밋의 깨끗한 재빌드·11개 검사·검토 DLL/설정/ZIP 해시 비교와 검증기 회귀 8개를 통과했습니다. 설치 파일은 [DiceMaster-v1.0.5-alpha.2.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.2/DiceMaster-v1.0.5-alpha.2.zip)입니다.

익명 API 조회 한도 때문에 Release·태그·기존 자산·Latest·이슈 메타데이터는 인증된 GitHub API로 확인했습니다. 공개 페이지와 ZIP은 인증 없이 접근·다운로드했고 ZIP CRC·정확히 두 파일·DLL/설정 바이트·SHA-256이 검증한 결과와 일치했습니다. 기존 15개 Release·태그 커밋·자산을 보존했고 Latest는 정식 v1.0.4, 이슈 #2는 closed, #4는 open을 유지합니다. 영수증은 `artifacts/releases/v1.0.5-alpha.2/`의 `release-verification.json`, `github-publication.json`, `anonymous-verification.json`, `tag-verification.json`과 빌드/검증 로그입니다. 이 게시 기록을 반영하는 후속 문서 커밋은 태그와 공개 설치 자산을 바꾸지 않습니다.

Windows 게임 설치·실행·UI/native 입력 성공이나 PRD #4 완료를 주장하지 않습니다. 실제 패드 탐색·스크롤·버튼·창 이동과 회귀는 사용자가 재검증해야 합니다.

## PRD #4 alpha.1 최초 로그 GUI·Xbox/XInput 탐색

PRD #4는 [로컬 구현·후보 검사](ISSUE-4-IMPLEMENTATION.md)를 마치고 [사용자 Windows 검증](ISSUE-4-WINDOWS-VALIDATION.md)을 기다립니다. 사용자 후속 지시에 따라 **[v1.0.5-alpha.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.1)을 2026-10-19 03:47:14 KST에 prerelease로 게시했습니다.** 고정 커밋 `f9f3b596db03372d219a4244c5c6a6fe90f9cc83`의 깨끗한 재빌드·11개 검사·공개 다운로드의 CRC·두 파일·바이트·해시·태그 확인이 통과했습니다. Release ID `416633648`, asset ID `654210019`입니다. [릴리스 설명·게시 결과](ISSUE-4-ALPHA1-RELEASE.md)는 새 화면·입력/한글 검색·창 설정·예약 의미·입력 경로 제약, Windows 미실행과 이전 DLL/설정 복구를 포함합니다. 정식 Latest v1.0.4와 기존 14개 Release·태그·자산을 보존했고 이슈 #4는 open으로 유지합니다. 영수증은 `artifacts/releases/v1.0.5-alpha.1/`에 보존합니다.

고정 소스 커밋은 `verify-preview-release.py --scope prd4`로 새로 빌드합니다. 기본 PRD #2 검증 계약은 유지하며 PRD #4는 명시적으로 선택해야 합니다. 11개 검사 증거와 Windows 미검증 상태·완료 미주장을 요구하고 검토 DLL SHA-256 `3ea62dd5c8fbee368438dd6e3ec8a58c023fcab5d01608321fe7e97ce80a6553`, 설정 SHA-256 `8d4ce1a8ee7ef173c821cb5c332127fa23b9dab4860bb76786b8a97f2ae5d815`와 비교합니다. prerelease는 `prerelease=true`·`make_latest=false`로 게시하고 공개 다운로드의 CRC·두 파일·커밋·해시를 확인합니다.

**현재 상태 — 2026-10-19 03:24:40 KST:** 사용자가 `v1.0.4-alpha.5` 맥락의 Windows 테스트 완료를 확인해 [이슈 #2](https://github.com/jungpaeng/dicemaster/issues/2)를 `closed` / `completed`로 종료했습니다. [완료 근거와 확인 범위](ISSUE-2-COMPLETION.md)를 따르며 아래 미확인/open 기록은 당시 이력으로 보존합니다.

[사용자 안내](../README.md) · [개발·검증](DEVELOPMENT.md) · [전체 소스 빌드 후보](SOURCE-BUILD.md) · [변경 이력](../CHANGELOG.md) · [검증 기록](ISSUE-1-VALIDATION.md) · [alpha.3 설명](ISSUE-1-RELEASE-DRAFT.md) · [전체 소스 alpha.4 기록](ISSUE-3-RELEASE.md)

## PRD 2 alpha.5 분석 공유·현재 조건 힌트 prerelease

사용자가 2026-10-19 KST에 이번 구현의 commit·push·prerelease 게시를 지시했습니다. 대상은 `v1.0.4-alpha.5`, 설치 자산은 `DiceMaster-v1.0.4-alpha.5.zip`이며 [alpha.5 게시 설명](ISSUE-2-ALPHA5-RELEASE.md)을 사용합니다. 안전한 현재 조건 표시 구현과 실제 자료의 모든 선택지가 미입증 콜백 경계로 중립을 유지하는 범위를 구분합니다.

고정 소스 커밋을 `verify-preview-release.py`로 새로 빌드해 검토한 DLL SHA-256 `6f4044b5b2e6d6b4f83f57937414dc6887c285660074776308084139bed62dba`와 설정 SHA-256 `8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e`를 요구합니다. 출력은 `artifacts/releases/v1.0.4-alpha.5`이며 `prerelease=true`·`make_latest=false`로 게시합니다. 사전 API 확인에서 기존 Release는 12개, Latest 정식은 v1.0.3, 이슈 #2는 open이며 새 태그는 없습니다. 기존 Release·태그·자산을 보존하고 Windows 실행·설치·오류 주입·이슈 종료는 수행하지 않습니다. 실제 게시 결과와 익명 공개 다운로드 검증은 후속 기록으로 구분합니다.

**2026-10-19 03:18:01 KST에 [v1.0.4-alpha.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.5) prerelease를 게시하고 인증 없는 공개 다운로드를 검증했습니다.** 소스 태그 커밋은 `d669c4ba08a0450679e5f5d83a8cd20af23b6a58`, Release ID `416618681`, asset ID `654148833`입니다. 고정 커밋의 깨끗한 재빌드가 검토한 DLL·설정·ZIP과 같은 바이트였고 공개 ZIP의 CRC·정확히 두 파일·해시·태그 커밋이 일치했습니다. 기존 12개 Release·자산과 Latest 정식 v1.0.3, 이슈 #2 open을 유지했습니다. Windows 실행과 PRD #2 전체 완료를 주장하지 않습니다. 영수증은 `artifacts/releases/v1.0.4-alpha.5/`에 보존하며 이 게시 기록의 후속 문서 커밋은 태그·공개 자산을 바꾸지 않습니다.

## PRD 2 alpha.4 실제 Lua 후속 prerelease

사용자가 2026-10-19 KST에 현재 구현의 commit·push·prerelease 게시를 지시했습니다. 대상은 `v1.0.4-alpha.4`, 설치 자산명은 `DiceMaster-v1.0.4-alpha.4.zip`이며 [alpha.4 게시 설명](ISSUE-2-ALPHA4-RELEASE.md)을 사용합니다. 실제 Lua 정적 대조·유한 추적·캐시·할당 개선과 남은 동기 분석 지연·실게임 미검증 범위를 구분합니다.

게시할 소스 커밋을 `verify-preview-release.py`로 새로 빌드해 DLL SHA-256 `c1b192fdd3932dcc2af35f8d5a526f3de0b52e33557f3de0c8a75258ccf99b91`과 설정 SHA-256 `8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e`를 요구합니다. 출력은 `artifacts/releases/v1.0.4-alpha.4`이며 `prerelease=true`·`make_latest=false`로 게시합니다. 기존 11개 Release·태그·자산과 Latest 정식 v1.0.3, 이슈 #2 open 상태를 유지합니다. Windows 설치·게임 실행·런타임 검증을 수행하거나 요청하지 않습니다. 실제 게시 결과는 아래 기록과 구분합니다.

**2026-10-19 02:44:05 KST에 [v1.0.4-alpha.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.4) prerelease를 게시하고 익명 공개 다운로드를 검증했습니다.** 소스 태그 커밋은 `3af4f862c09399b56ea7e127ef67d21290eaf174`, Release ID `416602030`, asset ID `654080605`입니다. 커밋의 깨끗한 새 빌드가 검토한 DLL·설정·ZIP과 같은 해시였고 공개 ZIP의 CRC·정확히 두 파일·DLL/설정 바이트·태그 커밋이 일치했습니다. 실제 Lua 정적 대조의 DLL SHA와도 같습니다. 기존 11개 Release·자산, Latest 정식 v1.0.3과 이슈 #2 open을 유지했습니다. 영수증은 `artifacts/releases/v1.0.4-alpha.4/`에 있으며 이 게시 기록의 후속 문서 커밋은 태그·공개 자산을 바꾸지 않습니다.

## PRD 2 alpha.3 문구 정리 prerelease

사용자가 2026-10-19 KST에 `가능한 변화` / `이후 가능한 변화` 묶음과 중복 `가산` 제거, commit·push·prerelease 게시를 승인했습니다. 대상은 `v1.0.4-alpha.3`, 설치 자산명은 `DiceMaster-v1.0.4-alpha.3.zip`이며 [alpha.3 게시 설명](ISSUE-2-ALPHA3-RELEASE.md)을 사용합니다. 고정 소스 커밋을 `verify-preview-release.py`로 새로 빌드해 검토한 DLL/설정 해시와 대조하고 `prerelease=true`·`make_latest=false`로 게시합니다. 기존 Release·태그·자산, 정식 Latest v1.0.3과 이슈 #2 open 상태를 유지합니다. 실제 Lua·Windows/native 검증과 PRD #2 완료를 새로 주장하지 않습니다.

**2026-10-19 01:14:16 KST에 [v1.0.4-alpha.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.3)를 게시하고 익명 공개 다운로드를 검증했습니다.** 태그 커밋은 `a7393cc1eb0fc092f38a347f099c48d2f3a88eba`, Release ID는 `416556648`, asset ID는 `653893311`입니다. 소스 커밋의 새 빌드와 검토 후보가 같은 해시로 재생성됐고 공개 ZIP의 CRC·두 파일·DLL/설정 바이트와 태그 커밋이 일치했습니다. 이전 10개 Release·자산, 정식 Latest v1.0.3과 이슈 #2 open을 유지했습니다. 검증 영수증은 `artifacts/releases/v1.0.4-alpha.3/`에 있으며 후속 문서 커밋은 태그·공개 파일을 변경하지 않습니다.

## PRD 2 alpha.2 가독성 후속 prerelease

사용자가 2026-10-19 KST에 가독성 후속 변경의 commit·push와 prerelease 게시를 승인했습니다. 대상은 `v1.0.4-alpha.2`, 자산명은 `DiceMaster-v1.0.4-alpha.2.zip`입니다. [alpha.2 게시 설명](ISSUE-2-ALPHA2-RELEASE.md)을 사용하고 `prerelease=true`·`make_latest=false`로 게시합니다. alpha.1을 포함한 기존 Release·태그·자산, 정식 Latest v1.0.3과 이슈 #2 open 상태를 유지합니다.

선택한 커밋을 아래 `verify-preview-release.py`로 새로 빌드하고 검토한 DLL SHA-256 `121f09973f8c37353e78afd341c735c84ca3f49f8ca20319c54b62bfc091b5b2`와 설정 SHA-256 `8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e`를 요구합니다. 버전은 `v1.0.4-alpha.2`, 출력은 `artifacts/releases/v1.0.4-alpha.2`로 지정합니다. 실제 Lua·Windows/native·최종 추적 한도 검증은 여전히 미완료이며 실제 게시·공개 다운로드 결과는 후속 기록으로 구분합니다.

**2026-10-19 01:02:27 KST에 [v1.0.4-alpha.2](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.2)를 게시하고 익명 공개 다운로드 검증을 완료했습니다.** 태그 커밋은 `06f10d81f1e3c0b0fa8f06c974027ab6fcb487c9`, Release ID는 `416550477`, asset ID는 `653865628`입니다. 커밋에서 새로 빌드한 DLL/설정과 공개 ZIP의 해시·CRC·정확히 두 파일 일치를 확인했습니다. 기존 9개 Release·자산과 Latest 정식 v1.0.3, 이슈 #2 open을 유지했습니다. 검증 영수증은 `artifacts/releases/v1.0.4-alpha.2/`에 있으며 후속 문서 커밋은 소스 태그와 공개 파일을 변경하지 않습니다.

## PRD 2 alpha.1 prerelease의 별도 범위

사용자가 2026-10-19 KST에 PRD 2 구현의 commit·push와 새 prerelease 게시를 승인했습니다. 대상은 `v1.0.4-alpha.1`, 설치 자산은 `DiceMaster-v1.0.4-alpha.1.zip`이며 `prerelease=true`·`make_latest=false`로 게시합니다. 기존 정식 Latest `v1.0.3`과 이전 Release·태그·자산을 보존합니다. [게시 설명](ISSUE-2-RELEASE.md)과 [구현·검증 기록](ISSUE-2-IMPLEMENTATION.md)에 범위와 식별값을 구분합니다.

**2026-10-19 00:39:24 KST에 [v1.0.4-alpha.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.1)을 게시하고 인증 없는 공개 다운로드를 검증했습니다.** 태그 커밋 `14ff82914d5d990362fdeebcf1dbd6eaa09bb6ef`, Release ID `416534622`, asset ID `653814237`이며 이전 8개 Release·자산, Latest 정식 v1.0.3과 이슈 #2 open을 보존했습니다. 게시 기록의 후속 문서 커밋은 태그·설치 자산을 변경하지 않습니다.

실제 Lua 대조·실제 자료와 비용에 근거한 최종 추적 제한·이 후보의 사용자 Windows 확인은 남아 있으므로 PRD 2 전체나 기능을 완료로 처리하지 않고 이슈 #2를 open으로 유지합니다. 과거 alpha.4/PRD 3 Windows 확인은 승계하지 않습니다. 이번 승인은 게임 설치·실행·오류 주입을 포함하지 않습니다.

게시할 소스 커밋을 확정한 뒤 prerelease 전용 사전 검증을 실행합니다. 해당 커밋의 소스만 허용 목록으로 추출하여 기존 모드 DLL·Git 데이터·이전 출력 없이 새로 빌드하고 8개 검사 프로그램을 실행합니다. 검토한 DLL/설정 해시·선택 커밋의 빌드 입력·ZIP CRC·정확히 두 파일·각 파일 바이트 일치를 요구합니다.

```sh
python3 build/verify-preview-release.py \
  --references '<읽기 전용 참조 루트>' \
  --source-commit '<게시할 40자리 커밋 SHA>' \
  --version v1.0.4-alpha.1 \
  --expected-dll-sha256 f02b3511760b0054b35319766ae5704bd76282cb48f90ae525231ca1a7ae6da3 \
  --expected-config-sha256 8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e \
  --output-dir artifacts/releases/v1.0.4-alpha.1
```

이 명령은 게시·Git 변경을 하지 않으며 prerelease 버전만 허용합니다. PRD 3의 전체 소스 복원/alpha.4 동등성 검사용 `verify-release.py`는 그대로 보존합니다. PRD 2는 의도한 동작 변경이 있어 과거 DLL과의 전체 동등성을 주장하지 않습니다. 별도의 기존 본문·리소스·번역/주사위·반복/교차 검사와 동일 DLL 해시를 보존 근거로 사용하고, 게시 후 익명 공개 다운로드와 커밋·자산 해시를 다시 확인합니다.

## v1.0.3 정식 전환 — 2026-10-19

사용자는 PRD #3 완료 시 정식 Release 게시와 이슈 종료를 지시했고, 2026-10-19 KST에 **Windows 검증을 완료했으며 공개된 alpha.4를 직접 내려받아 사용했다**고 확인했습니다. 이를 alpha.4 DLL SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`에 대한 사용자 확인으로 기록합니다. 게임·BepInEx 버전, 패드/Steam Input 환경, 항목별 결과·로그·업데이트/롤백 증거는 별도 제공되지 않았습니다. 이 환경을 과거 수신 참조의 환경으로 추정하거나 정식 후보를 직접 Windows에서 실행했다고 기록하지 않습니다.

**[정식 v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)을 2026-10-19 00:10:53 KST에 게시하고 Latest 정식 버전으로 지정했습니다.** 소스 커밋은 `ced38df9f0560a212559e34fd281fddbd2581ff4`, Release ID는 `416518288`, 상태는 `draft=false`·`prerelease=false`입니다. 일반 개발·배포 경로를 전체 소스 빌드로 전환했으며 정책·사용자 확인·비교 근거는 `build/release-policy.json`과 [이슈 #3 완료 근거](ISSUE-3-RELEASE.md)에 연결합니다.

정식 자산은 [DiceMaster-v1.0.3.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3/DiceMaster-v1.0.3.zip), asset ID `653759221`이며 설정과 새 소스 빌드 DLL 두 파일만 포함합니다. 고정 소스 커밋의 새 빌드·일곱 자동 검사·alpha.3 계약 2,750개·변조 거부 12종·번역/주사위 관찰값 4,341개와, alpha.4 대비 전체 IL·메타데이터 동등성 1,313개 항목 및 변조 거부 8종을 통과했습니다. alpha.4 이후 C# 34개·설정과 번역 데이터를 유지하며 번역 리소스 주석 세 줄만 정리해 DLL 전체 해시는 다릅니다. 사용자 확인 대상과 정식 DLL을 연결하는 동등성 비교이며 정식 DLL의 직접 Windows 실행 기록은 아닙니다.

게시 후 익명 공개 다운로드·CRC·정확히 두 파일·빌드 DLL/설정 바이트·SHA-256을 확인했고 [이슈 #3](https://github.com/jungpaeng/dicemaster/issues/3)의 수용 기준 10개를 확인해 2026-10-19 00:11:02 KST에 `closed`·`completed`로 종료했습니다. 이전 일곱 Release의 ID·자산·바이트를 보존했습니다. 이슈 #1은 `closed`, #2는 `open`인 기존 상태를 유지하며 이슈 #1의 과거 수용 기준이나 #2의 기능 확장 완료를 새로 선언하지 않습니다. 최종 DLL은 461,312바이트 / SHA-256 `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1`, ZIP은 191,442바이트 / SHA-256 `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7`입니다. 게시 결과를 기록하는 후속 문서 커밋은 위 소스 태그·설치 자산을 바꾸지 않습니다.

## alpha.4 및 이전 prerelease의 당시 승인 범위

사용자 승인에 따라 C# 프로젝트 구조 변경을 commit·push하고 **[v1.0.3-alpha.4 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)를 2026-10-18 23:42:14 KST에 게시했습니다.** 태그의 소스 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`을 새로 빌드·검사한 `DiceMaster-v1.0.3-alpha.4.zip`이며 `draft=false`, `prerelease=true`, `make_latest=false`로 게시했습니다. Release ID `416503505`, asset ID `653700999`입니다. 익명 공개 API·다운로드 검증을 마쳤고 식별값과 검사 증거는 [이슈 #3 배포 기록](ISSUE-3-RELEASE.md)에 기록했습니다. 당시 기존 여섯 Release·자산과 Latest 정식 `v1.0.2`를 유지했습니다. 당시 승인은 Windows 설치·실행, 기본 빌드 전환, 정식 Release나 이슈 종료를 포함하지 않았습니다.

사용자가 모드 전용 독립 난수의 숫자 갱신과 `v1.0.3-alpha.3` prerelease 게시를 승인했습니다. UI에는 `표시용` 같은 부가 문구를 붙이지 않습니다. 실제 게임 판정의 예측값이 아니라는 의미는 문서·게시 설명에 기록합니다. 최종 자동 검사·파일·로컬 ZIP 검증과 새 prerelease 게시·익명 공개 다운로드 검증을 마쳤습니다. 기존 수치를 승계하지 않습니다.

**[`v1.0.3-alpha.2` prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.2)를 2026-10-18 21:52:10 KST에 게시하고 익명 공개 다운로드 검증을 마쳤습니다.** Release ID는 `416448453`, 상태는 `draft=false`, `prerelease=true`입니다. 이전 버전 수치를 새 버전 결과로 승계하지 않습니다.

**[`v1.0.3-alpha.1` prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.1)를 2026-10-18 21:08:36 KST에 게시하고 공개 다운로드 검증을 마쳤습니다.** GitHub 상태는 `draft=false`, `prerelease=true`입니다. 이 게시로 기능·이슈 완료나 정식 Release로 처리하지 않습니다.

이 prerelease 게시 당시에는 Windows PC의 기존 파일·정보 읽기만 허용됐고 게임 실행·후보 설치·런타임 검증은 승인되지 않았습니다. 이후 사용자 본인의 alpha.4 검증 완료 확인은 위 정식 전환 기록으로 분리합니다. 에이전트가 Windows 설치나 게임 실행을 수행하거나 요청한 것은 아닙니다.

## 이슈 #3 전체 소스 빌드와 Release 검사

전체 소스 복원·비교 기준은 `v1.0.3-alpha.3` / `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`이며 기준 DLL SHA-256은 `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633`입니다. [전체 소스 빌드](SOURCE-BUILD.md)는 별도 후보 DLL과 두 파일 ZIP을 만듭니다. 게시한 alpha.4 prerelease는 고정 태그 커밋의 `src/DiceMaster/DiceMaster.csproj`를 빌드한 DLL과 같은 커밋의 `packaging/config/DiceMaster.cfg`를 묶습니다. 태그의 루트 `plugins/DiceMaster/DiceMaster.dll`은 기존 alpha.3 비교·복구 기준이므로 게시 자산 DLL과 다릅니다. 태그에 기존 DLL을 교체하지 않고 빌드 영수증·해시·공개 다운로드로 소스 커밋과 자산을 연결합니다.

저장소 구성은 루트 솔루션과 `src/`·`tests/`·`build/`·`tools/`·`packaging/`으로 정리했습니다. 일반 컴파일 DLL은 `artifacts/bin/DiceMaster/release/`, 자동 검사한 후보 DLL·ZIP·영수증은 `artifacts/source-candidate/`에 생성합니다. 루트 `config/`·`plugins/`는 제거했으며 ZIP의 설정은 `packaging/config/DiceMaster.cfg`에서 읽습니다. ZIP 내부 설치 경로는 계속 `config/DiceMaster.cfg`와 `plugins/DiceMaster/DiceMaster.dll`입니다. 비교·최초 복원 기준은 `python3 build/baseline.py`로 고정 Git 커밋에서 꺼내 해시 확인 후 `artifacts/baselines/v1.0.3-alpha.3/DiceMaster.dll`에 둡니다. 기준 DLL과 기존 패치 공정은 일반 빌드 입력이 아니며 별도 비교·복구 경로로 유지합니다. 기존 패치의 출력은 `artifacts/legacy-candidate/`로 분리합니다.

Release 후보 사전 검증은 다음 명령으로 수행합니다. 관리 소스만 담은 스냅샷에서 기존 모드 DLL과 `.git/` 없이 빌드·일곱 검사를 실행하고, 고정 기준 비교·변조 거부·ZIP CRC·두 파일·빌드 DLL/패키지 설정 바이트·SHA-256을 확인합니다.

```sh
python3 build/verify-release.py \
  --references '<읽기 전용 참조 루트>' \
  --output-dir artifacts/release-candidate
```

완료 결과는 `release-verification.json`과 빌드·호환성 보고서 및 로그로 기록합니다. 게시할 커밋 확정 후에는 `--source-commit <40자리 전체 커밋 SHA>`로 그 커밋만 검사하고 `--version v1.0.3`으로 ZIP 자산명을 정합니다. 기존 영수증을 덮어쓰지 않도록 출력 폴더는 비어 있거나 없어야 하며 재실행에는 새 경로를 지정합니다. 이 명령은 로컬 검증만 수행합니다. 새 태그·게시·push는 사용자의 정식 게시 지시에 따라 별도로 실행하며 기존 alpha.4 태그나 자산은 바꾸지 않습니다.

기본 개발·배포 경로 전환의 증거는 다음과 같이 구분합니다.

- 기존 모드 DLL 없이 새 소스 빌드·ZIP 생성, 기존 자동 검사 및 기준과의 동작·메타데이터·후킹·native ABI·8개 내장 리소스 비교 통과
- 2026-10-19 사용자가 기록한 공개 alpha.4 Windows 검증 완료 확인과, 그 DLL에서 새 후보로의 동등성 비교. 사용자 미제공 환경·항목별 로그는 미제공으로 명시
- 게임 DLL·게임 소스가 저장소와 ZIP에 없고 ZIP이 설정·새 DLL 두 파일만 포함한다는 확인
- 기본 명령·문서·산출물 생성 경로를 전환하고 이전 기본 빌드·배포 DLL로 복구할 수 있다는 확인

Windows 설치·실행을 에이전트가 수행하거나 요청하지 않습니다. 자동 검사와 사용자 확인은 서로 다른 증거이며, 사용자 확인을 모든 Windows 환경·성능·개별 경로의 검증 기록으로 확대하지 않습니다. 이슈 #1의 알려진 제한과 미완료 조건, 이슈 #2의 기능 확장은 별도로 유지합니다.

비교·복구는 별도 후보 디렉터리에 고정 Git 커밋에서 alpha.3 기준을 재생성하여 기존 패치 공정에 사용하거나 이전 공개 alpha.4 설치 자산을 사용하는 것입니다. 참조 폴더나 기존 게시 파일을 덮어쓰지 않습니다. 설치 뒤 문제가 생기면 게임을 종료하고 보관한 이전 DLL을 복원하며 기존 설정과 세이브를 유지합니다. 실제 Windows 업데이트·롤백의 별도 실행 기록은 제공되지 않았습니다. v1.0.2의 선택지 결과 소실, 제자리/무동작/실패 로드 후 다음 성공 Lua까지 대기하는 alpha.3의 알려진 제한은 소스 복원으로 해결했다고 표시하지 않습니다.

## alpha.3 게시 기록

새 태그는 `v1.0.3-alpha.3`, 파일은 `DiceMaster-v1.0.3-alpha.3.zip`이며 `draft=false`, `prerelease=true`, 최신 정식 버전 지정 없이 게시했습니다. 최종 DLL은 462,336바이트 / SHA-256 `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633`, 로컬 ZIP은 191,369바이트 / SHA-256 `d4a5bb2276a1d1089e02284286456f4ec16f5e41f8a8481ad6c23db0fde7a6b9`입니다. 두 파일·CRC·작업 트리 바이트 일치를 확인했습니다. 관리 동작 3,914개·메타데이터 1,011개·통합 134개·반복 구조 958개를 최초/반복에서, 설치 기준 구조 971개·최종 패드 525,430개도 통과했습니다. 최종 11개 프로젝트 MSBuild 경고·오류는 0입니다. 2026-10-18 22:43:45 KST에 [v1.0.3-alpha.3 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.3)를 배포 태그 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`에서 게시했습니다. 익명 공개 API·다운로드·CRC·정확히 두 파일·각 파일의 배포 커밋 바이트·ZIP/DLL SHA-256 일치를 검증했습니다. Release ID는 `416474001`, asset ID는 `653584113`입니다. 게시 시각은 2026-10-18 13:43:45 UTC / 22:43:45 KST이며 영수증은 `development/releases/v1.0.3-alpha.3/github-publication.json`·`anonymous-verification.json`입니다. 게시 기록 문서 후속 커밋은 배포 태그·커밋·ZIP을 변경하지 않습니다. alpha.1·alpha.2의 태그·Release·파일을 덮어쓰지 않고 Latest 정식 `v1.0.2`를 유지합니다.

alpha.3 ZIP에는 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 두 파일만 넣고 당시 태그 커밋의 루트 배포 파일과 바이트를 일치시켰습니다. alpha.3은 게임 RNG와 분리된 `System.Random`으로 숫자·색상·난수에 의존하는 이벤트 확률을 계산합니다. 개별 `ConditionResultItem` 설명 또는 전체 이벤트 설명(헤더·활성 조건·모든 확률 항목 조건) 안에서 결과를 재사용합니다. 미션·스위치의 서로 다른 조건 항목은 별도 평가하므로 같은 참조도 다시 추첨할 수 있으며, 실제 게임의 다음 판정이나 분포를 예측하지 않습니다. 전체 int 구간과 역순 구간 정규화는 모드 화면의 규칙으로 설명하고, 게임 RNG·조건 실행 카운터 보존과 Windows/native 미검증 범위를 게시 설명에 포함합니다.

## alpha.2 게시 기록 — 불변 이력

최종 소스의 자동 검사·반복 패치·패드·구조 결과와 DLL SHA-256을 확정했습니다. DLL은 `b6f7ae790cde736dcf1a282e9242c46b8ab0204602c7c462d59c571f321740ec` / 462,848바이트이고 로컬 ZIP은 `fcd3111b101efba6ef62b0f79cb918f23e016ccf8e1637a821edb9e2ae0589f1` / 192,018바이트이며 정확히 두 파일·CRC·작업 트리 바이트 일치를 검증했습니다. 배포 태그 커밋은 `015c80c2fd9f905aaece574b3b11da52e501d432`이며 `DiceMaster-v1.0.3-alpha.2.zip`과 같은 고정 기준입니다. 익명 공개 API·다운로드·CRC·정확히 두 파일·게시 커밋 바이트·SHA-256 일치를 확인했습니다. alpha.1의 태그·파일·해시는 덮어쓰지 않고 Latest 정식 `v1.0.2`를 유지합니다. 게시 시각은 2026-10-18 12:52:10 UTC / 21:52:10 KST이며 로컬 영수증은 `development/releases/v1.0.3-alpha.2/github-publication.json`입니다. 문서 후속 커밋은 배포 태그·커밋·ZIP을 변경하지 않습니다.

난수 미리보기의 실제 허용 범위·이론 확률 의미·미지원 표시와 미검증 native·Windows 경로를 게시 설명에 기록합니다. Windows 실행·설치 검증 승인은 추가되지 않았습니다.

## alpha.1 prerelease 게시 기록 — 불변 이력

| 항목 | 계약 |
|---|---|
| 저장소 | `jungpaeng/dicemaster` |
| 태그 | `v1.0.3-alpha.1` |
| ZIP | `DiceMaster-v1.0.3-alpha.1.zip` |
| 배포 기준 커밋 | `2d8936786845b534524b916537e7f1c3e5ab482d` — 태그·ZIP의 고정 기준 |
| GitHub 상태 | Release ID `416428315`, `draft=false`, `prerelease=true`; 기존 Latest `v1.0.2` (ID `416349430`) 유지 |
| 게시 시각 | 2026-10-18 12:08:36 UTC / 21:08:36 KST |
| ZIP 크기·SHA-256 | 188,793바이트, `95c753037a4cf5fe16db37735dbf4833c2ed76af71c7bd941c6f76d791932429` |

설치 ZIP에는 아래 두 파일만 포함합니다. 다른 모드·개발 파일·문서·로그·수신 게임 DLL·`.DS_Store`·`__MACOSX` 등은 포함하지 않습니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

최종 DLL은 SHA-256 `ca9061a39bc514b8996b6f6dce154908bad741aa4815ce0951e5ade37dcc97f7`, 455,680바이트입니다. 익명 공개 API와 익명 다운로드에서 두 파일만 포함됨·CRC 정상·ZIP SHA-256·각 파일의 배포 커밋 바이트 일치를 확인했습니다. 로컬 영수증은 `development/releases/v1.0.3-alpha.1/github-publication.json`입니다. 게시 기록을 보완하는 문서 후속 커밋은 위 태그·배포 커밋을 바꾸지 않습니다.

기존 태그·Release를 덮어쓰지 않습니다. GitHub의 자동 `Source code` 압축 파일은 설치용 ZIP과 별개입니다. 새 설정·원격 기능 플래그·단계별 배포는 도입하지 않습니다. 읽기 전용 원본 게임 참조는 게시 파일에 넣지 않습니다.

## alpha.1에 공개한 검증 범위 — alpha.2로 승계하지 않음

[게시 설명](ISSUE-1-RELEASE-DRAFT.md)에는 자동 검사와 미검증 경로를 함께 기록합니다. 실제 관리 DLL의 정적 API·수명주기 분석, 실제 IL을 native 경계 대역으로 옮긴 macOS 검사, 실제 게임 실행은 서로 다른 증거입니다.

alpha.1 후보는 캐시/보호 19개, 엔진 16개 시나리오·70개 단언, 수명주기 4,023개, 통합 최초·반복 각 94개, 실제 IL 각 152개·메타데이터 각 910개, 최종 구조 각 934개, 실제 설치 기준 구조 961개, 패드 525,430개를 통과했습니다. 코드 오류는 0이나 네트워크 차단에 따른 NuGet 보안 권고 조회 `NU1900` 경고가 있습니다. 상세 식별값과 측정 방법은 [검증 기록](ISSUE-1-VALIDATION.md)에 기록합니다.

alpha.1의 난수 실패 정책과 alpha.2에 반영한 범위·판정 미정·전용 미지원 표시의 차이, 제자리 복원 뒤 다음 성공 Lua까지 대기할 수 있는 동작을 알려야 합니다. native Unity·실제 리소스·Harmony·다른 모드·UI·Windows·실제 게임 성능은 미검증입니다. 모의 엔진의 벤치마크를 실제 게임 성능으로 기재하지 않습니다.

## 이슈 #1의 별도 기능 완료 증거

[이슈 #1 검증 기록](ISSUE-1-VALIDATION.md)의 기능·실제 성능 수용 기준은 이슈 #3의 소스 빌드 전환과 별개입니다. 아래는 이슈 #1 당시 남긴 확인 목록이며, 이번 alpha.4 사용자 확인으로 측정값이나 항목별 결과가 추가됐다고 처리하지 않습니다. 이 목록을 정식 v1.0.3 전체 소스 빌드 전환의 새 요구사항으로 확대하거나 이슈 #1의 완료를 재판정하지 않습니다.

- 실제 파서·공유 캐시·표시·선택지 요약의 연속 갱신과 기존 기능 보존
- 현재 탭·세 상태·5초 재시도·수동 복구·진단 로그 동작
- 실제 IL과 순수 평가의 값 의미·RNG 비소비·카운터 보존·미지원 자료의 명시적 실패
- 실제 로드·새 게임·씬·Lua 경계, null·예외·오래된 완료·제자리 복원 처리
- 향후 별도 승인된 환경에서의 Unity/native·실제 리소스·Harmony·다른 모드·UI·진행 확인
- 별도 승인 후 사용자가 기록한 Windows 검증과 실제 게임 시간·할당량 측정
- 최종 DLL·소스·도구·문서·ZIP 일치, 반복 패치·패드 재빌드·리소스 보존

## 롤백 안내와 확인 범위

기존 설정과 세이브 형식은 변경하지 않습니다. 이전 Release 또는 보관 DLL 복원과 설정 유지 여부의 항목별 Windows 결과는 사용자가 별도 제공하지 않았습니다. 이번 작업에서 Windows에 설치하거나 복원하도록 요청하지 않습니다.

v1.0.2로 돌아가면 선택지 결과 소실 문제가 다시 나타날 수 있습니다. v1.0.1에는 자동 로그 갱신이 없습니다. 실제 업데이트·롤백은 아직 검증하지 않았습니다.
