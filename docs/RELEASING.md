# 소스 기반 Release와 게시 이력

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
