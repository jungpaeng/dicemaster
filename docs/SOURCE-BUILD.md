# 전체 소스 빌드 — 이슈 #3

2026-10-21 06:25:46 KST에 소스·태그 `1a782f206f1aa7ab9f9e490125fbe916aabbad66`의 [alpha.9](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.9)를 게시했다. 지도 오버레이에서 진행 불가 항목을 제외하고 로그 상세의 조건·사유는 보존한다. 전체·고정 커밋 재빌드 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조와 DLL/cfg/ZIP 동일성·익명 공개 다운로드 대조를 통과했다. 기존 50개 Release·51개 태그·자산과 Latest 정식 v1.0.6을 보존했다. 새 DLL의 실제 Windows·물리 패드·가독성·성능 확인과 #6 전체 완료는 대기다. [필터·파일·게시·Windows 확인](ISSUE-6-ALPHA9-RELEASE.md).

[개발 안내](DEVELOPMENT.md) · [Release·전환 조건](RELEASING.md) · [PRD #3](https://github.com/jungpaeng/dicemaster/issues/3)

전체 소스 빌드를 기본 개발·배포 경로로 전환하고 [정식 v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)을 2026-10-19 00:10:53 KST에 게시했습니다. 소스 태그 커밋은 `ced38df9f0560a212559e34fd281fddbd2581ff4`이며 이슈 #3은 같은 날 00:11:02 KST에 완료로 닫았습니다. 일반 컴파일은 기존 모드 DLL 없이 `src/DiceMaster/`의 C#·모드 리소스와 읽기 전용 외부 게임 참조를 사용합니다. 기존 패치 도구는 비교·복구용으로 보존하며 기준 DLL은 고정 Git 이력에서 해시를 검증해 재생성합니다. 후보 출력 이름 `artifacts/source-candidate/`는 기존 명령 호환성을 위해 유지합니다. 기본 정책·사용자 확인과 정식 전환 근거는 `build/release-policy.json`에 기록합니다.

2026-10-19 사용자가 공개 [v1.0.3-alpha.4 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)을 직접 내려받아 Windows 검증을 완료했다고 확인했습니다. 확인 대상은 아래 alpha.4 해시이며 현재 주석 정리 DLL을 새로 실행한 결과로 간주하지 않습니다. [사용자 확인과 기본 전환](#사용자-windows-확인과-기본-전환)에 근거 범위를 구분하고 [배포 기록](ISSUE-3-RELEASE.md)에 정식 게시·공개 다운로드·이슈 완료 결과를 기록했습니다. 이슈 #1·#2의 기능이나 상태는 추가 변경하지 않습니다.

## 지도 자동 표시 필터 — alpha.9

소스 `1a782f206f1aa7ab9f9e490125fbe916aabbad66`의 전체·고정 커밋 재빌드에서 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조·전체 검사 성공 기록·DLL/cfg/ZIP을 대조했다. 불가 대화·임무·1회성 자동 제외와 상세 보존·가능/불명 유지·필터 후 목록과 개수·빈 안내·1회성 전용 모드를 기존 provider 경계에서 확인했다. Python 배포 검증기 회귀 20개도 통과했다. [파일·검사·새 Windows 확인](ISSUE-6-ALPHA9-RELEASE.md).

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-6/map-overlay-filter-candidate/full-final
python3 build/verify-preview-release.py --scope prd6 --references '<읽기 전용 참조 루트>' --source-commit 1a782f206f1aa7ab9f9e490125fbe916aabbad66 --version v1.0.7-alpha.9 --expected-dll-sha256 b11b1af675cc656e87602e9fbedb98e28669497102708603028aa13813810362 --expected-config-sha256 442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a --output-dir artifacts/releases/v1.0.7-alpha.9
```

출력 폴더는 없거나 비어 있어야 하며 재실행은 새 출력 경로를 사용한다. 선택적 corpus는 고정 재빌드에도 같은 SHA 확인 입력을 제공한다. 실제 Windows·Unity 렌더링·물리 입력·성능 확인은 대기다. 다음 절부터는 이전 게시 기록이다.

## 전체 이야기 조건 조회 — alpha.8

소스 `90cba0b8e754db606d311394f05a7f05aca8efb5`의 전체·고정 커밋 재빌드에서 21개 프로그램·250개 입력·103개 소스/리소스·15개 참조·전체 검사 성공 기록·DLL/cfg/ZIP을 대조했다. 내부 이야기 조건·쓰기 의존성·불명 콜백/풀·누락 경로·현재 메뉴·장소 제한·양성 색과 캐시 회귀를 포함한다. Python 배포 검증기 회귀 20개도 통과했다. [파일·검사·Windows 확인](ISSUE-6-ALPHA8-RELEASE.md).

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-6/story-conditions-candidate/full-final
python3 build/verify-preview-release.py --scope prd6 --references '<읽기 전용 참조 루트>' --source-commit 90cba0b8e754db606d311394f05a7f05aca8efb5 --version v1.0.7-alpha.8 --expected-dll-sha256 da97db93c5a84c7dba5f6531e5d4f4c0f800cd90a00cfa0c2fc12295eb8c26a1 --expected-config-sha256 442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a --output-dir artifacts/releases/v1.0.7-alpha.8
```

출력 폴더는 없거나 비어 있어야 하며 재실행은 새 출력 경로를 사용한다. 선택적 corpus는 고정 재빌드에도 같은 SHA 확인 입력을 제공한다. 실제 Windows·Unity 렌더링·물리 입력·성능 확인은 대기다. 다음 절부터는 이전 게시 기록이다.

## 현재 조건 색 구분 — alpha.7

소스 `4aa5d2c524f63a21197668407f1f70c67a089ab1`의 전체·고정 커밋 재빌드에서 21개 프로그램·249개 입력·전체 검사 성공 기록·DLL/cfg/ZIP을 대조했다. 렌더러 회귀는 이번 실행 5,309개이며 새 값·실패 이전 값·혼합 상태·rich text/배치 캐시를 포함한다. [파일·검사·Windows 확인](ISSUE-6-ALPHA7-RELEASE.md).

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-6/entry-color-candidate/full-final
python3 build/verify-preview-release.py --scope prd6 --references '<읽기 전용 참조 루트>' --source-commit 4aa5d2c524f63a21197668407f1f70c67a089ab1 --version v1.0.7-alpha.7 --expected-dll-sha256 7ec09ad68a786de23616ef5e626cecf387f33f61ab57a1a3078514be502f570d --expected-config-sha256 442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a --output-dir artifacts/releases/v1.0.7-alpha.7
```

출력 폴더는 없거나 비어 있어야 하며 재실행은 새 출력 경로를 사용한다. 선택적 corpus를 썼다면 고정 재빌드에도 같은 SHA 검증 입력을 제공한다. 실제 Windows·Unity 렌더링·물리 입력·성능 확인은 대기다. 다음 절은 이전 게시 기록이다.

## 인물 대화 분류 수정 — alpha.6

전체 소스 후보와 `08b9fb623c43cd62b42fcae8a24fb913f372027c`의 고정 커밋 재빌드에서 21개 프로그램·249개 입력·전체 성공 기록·DLL/cfg/ZIP을 대조했다. MapRouteTests 5,303개·MapGuideRouteIntegrationTests 345개·ChoiceStoryTests 684개는 새 실행 값이며 이전 후보 수치를 승계하지 않는다. [검사·해시·설치·Windows 확인](ISSUE-6-ALPHA6-RELEASE.md).

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-6/dialogue-filter-candidate/full-final
python3 build/verify-preview-release.py --scope prd6 --references '<읽기 전용 참조 루트>' --source-commit 08b9fb623c43cd62b42fcae8a24fb913f372027c --version v1.0.7-alpha.6 --expected-dll-sha256 cbd19644255a43a928b10d3f6f066595af7b3366fce54a5dc46b2ba4c1075b88 --expected-config-sha256 442f360da915e365fb96d2daa33c11b6d8b8be3ecde2db85382e09179ebd005a --output-dir artifacts/releases/v1.0.7-alpha.6
```

출력 폴더는 없거나 비어 있어야 한다. 재실행은 보존된 결과를 덮어쓰지 말고 새 출력 경로를 사용한다. `DICEMASTER_CORPUS_INDEX`를 사용했다면 같은 SHA 검증 입력을 고정 재빌드에도 제공한다. 새 DLL의 실제 Windows·물리 입력·성능 확인은 대기다. 다음 절은 alpha.5 당시 기록이다.

## 이야기·선택 경로 후보 — alpha.5

alpha.5 후속은 `ChoiceStoryTests`를 추가한 21개 검사 프로그램에 실제 경로/이야기/선택 표시와 입력 연결 회귀를 포함한다. 전체·고정 소스 재빌드에서 21개가 모두 통과했다. alpha.4의 20개 프로그램 결과를 새 실행으로 승계하지 않으며 [최종 기록](ISSUE-6-ALPHA5-RELEASE.md)에 이번 영수증을 남겼다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-6/choice-story-candidate/full-final
python3 build/test-verify-preview-release.py
```

생산·검사·빌드·설정 입력이 동결된 상태에서 실행한다. 기존 .NET SDK·NuGet 캐시와 읽기 전용 게임 참조를 사용하고, 선택적 `DICEMASTER_CORPUS_INDEX`를 쓴다면 고정 커밋 재빌드에서도 같은 SHA 검증 자료를 유지한다. 소스 커밋 확정 뒤 아래 명령의 값을 새 manifest에서 채운다. 출력은 없거나 빈 폴더여야 하며 다른 버전 산출물·게시 원문을 지우지 않는다.

```sh
python3 build/verify-preview-release.py --scope prd6 --references '<읽기 전용 참조 루트>' --source-commit a26603bf746bff0332ea2d4fde2642c000008426 --version v1.0.7-alpha.5 --expected-dll-sha256 9fddbe4653aaf058f02d280c7c5d3b3660bd8df318906e4480e5080499d84931 --expected-config-sha256 e7a598ea94d88910d19df5d235a3178b92e1c46959482b79e78e52d8e8db1388 --output-dir artifacts/releases/v1.0.7-alpha.5
```

이 검증은 고정 커밋의 허용 소스만 추출해 관리 모드 DLL 0개에서 빌드·검사를 다시 실행한다. 원래 검토 후보와 SDK·전체 입력/참조·검사 결과·DLL/cfg/ZIP을 비교하고 ZIP의 정확히 두 경로·CRC·내부 바이트를 확인한다. 실제 Windows 실행·물리 입력·프레임 비용은 이 명령의 결과에 포함하지 않는다. 커밋·태그·게시와 익명 다운로드 확인은 [Release 절차](RELEASING.md)에 별도로 남긴다.

## 이전 지도 후속 후보 빌드 — alpha.4·alpha.3

alpha.4의 상점 요약·지도 비용·1회성 자동 문구 수정 후보는 당시 20개 프로그램과 고정 커밋 재빌드·실제 DLL 연결·ZIP 검증을 사용했다. `--output-dir artifacts/issue-6/compact-information-candidate/full-final`의 산출물과 [당시 구현·검사·식별값](ISSUE-6-ALPHA4-RELEASE.md)을 보존한다. 다음 지도 개선 경로·수치는 alpha.3 당시 기록이다.

alpha.3 지도 후속은 기존 전체 소스 빌드에 MapRouteTests와 MapGuideRouteIntegrationTests를 포함한 당시 20개 검사 프로그램을 등록했다. `build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-6/map-guidance-candidate/full-final`로 후보를 만들고, 커밋 확정 뒤 `verify-preview-release.py --scope prd6`로 해당 소스만 추출해 다시 검증한다. 선택적 `DICEMASTER_CORPUS_INDEX`는 개발자가 보유한 검증된 번체 자료 색인을 지정하며 게임 자료를 저장소·설치 ZIP에 포함하지 않는다. [당시 구현·검사·파일·Windows 확인](ISSUE-6-MAP-GUIDANCE-RELEASE.md)에 그 결과를 기록했으며 아래 PRD #4 이전 후보의 수치·확인도 당시 이력이다.

## PRD #4 후보와 prerelease

### v1.0.5 정식 게시·동일 파일 승격 검증

**2026-10-19 23:59:15 KST에 정식 [v1.0.5](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5)를 게시하고 Latest로 지정했습니다.** 정식 태그 `8886309e2a7248fa13544f81546e3c4795ff9611`의 Git 입력 153개·생산 소스 63개가 alpha.9의 고정 재빌드 소스 `7d9c9ec0cfaadbb968c35cfaa5111912c1c68f45`와 동일함을 확인했습니다. 기존 DLL/cfg/ZIP을 바이트 그대로 복사해 ZIP 이름만 `DiceMaster-v1.0.5.zip`으로 바꿨습니다. 재빌드·검사 재실행·재압축은 하지 않았으며 기존 12개 검사 결과는 alpha.9 실행 기록으로 구분합니다.

공개 [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5/DiceMaster-v1.0.5.zip)의 SHA-256은 `98fd7ed0a6a3232ebc6cf6bde29a333cba08ca5ce40b647c4eb3beaba527db04`, DLL은 `39e30eb7816e140e8175c9a44da14b5e04fdfefe3398e3b56dafc92ac0e49339`, cfg는 `bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`입니다. 인증 없는 다운로드의 CRC·정확히 두 파일·내부 바이트와 실제 Latest v1.0.5를 확인했습니다. 원래 검증 영수증을 변경하지 않고 `artifacts/releases/v1.0.5/`의 `promotion-verification.json`, 원본 `origin-alpha9-*` 영수증과 독립 감사·공개 게시 기록으로 동일성을 연결합니다. [정식 변경·설치·해시·복구·Windows 확인](ISSUE-4-STABLE-RELEASE.md)을 따릅니다. 사용자 게시 지시는 Windows 검증 완료 근거로 쓰지 않으며 실제 Windows 확인·이슈 #4 완료는 별도로 대기합니다. 과거 PRD #3 동등성 검증기·정책과 이전 모든 배포는 보존합니다.

### v1.0.5-alpha.9 게시·고정 커밋 재빌드 검증

`--output-dir artifacts/issue-4/reservation-toggle`로 목록 예약 표시·새 확인 입력의 예약/해제와 미션 소유 상태 복구 후속을 전체 소스로 빌드했습니다. 실제 소비·동일 객체 재예약·조회 실패 사이의 오래된 입력을 예약 슬롯·문맥·변경 세대로 거부하며 기존 최신 대상/게임 멤버십·홀드/중립 보호를 유지합니다. [상세 계약·로컬 검토 기록](ISSUE-4-RESERVATION-TOGGLE.md)을 따릅니다.

**2026-10-19 23:23:31 KST에 [v1.0.5-alpha.9](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.9)를 게시했습니다.** 소스·태그 `7d9c9ec0cfaadbb968c35cfaa5111912c1c68f45`을 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 12개 검사·입력 153개·소스 63개와 검토 DLL/cfg/ZIP 바이트 일치를 확인했습니다. UI/입력 3,472개·실제 Bridge 통합 400개·모델 375개·DLL 연결 71개를 포함합니다. 공개 [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.9/DiceMaster-v1.0.5-alpha.9.zip)의 SHA-256은 `98fd7ed0a6a3232ebc6cf6bde29a333cba08ca5ce40b647c4eb3beaba527db04`, DLL은 `39e30eb7816e140e8175c9a44da14b5e04fdfefe3398e3b56dafc92ac0e49339`, cfg는 `bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`입니다. 인증 없는 다운로드의 CRC·두 파일·내부 바이트/해시도 확인했습니다. 결과는 `artifacts/releases/v1.0.5-alpha.9/`에 보존하며 [설치·복구·Windows 확인](ISSUE-4-ALPHA9-RELEASE.md)을 따릅니다. cfg를 보존해 DLL만 교체합니다. 실제 Windows 확인과 이슈 #4 완료는 대기이며 아래 alpha.8 공개 이력은 보존합니다.

### v1.0.5-alpha.8 게시·고정 커밋 재빌드 검증

`--output-dir artifacts/issue-4/list-reservation`으로 전체 소스를 빌드합니다. 이벤트·미션의 선택된 목록에서 새 A·Enter·Space로 일회 예약하고, 상세는 예약 활성 여부와 가능/불가 이유를 표시하는 읽기 전용 영역으로 바꿉니다. 선택된 행의 재클릭·터치는 누름 시점 티켓을 해제 시 다시 검증하며 해제 시 새 권한을 만들지 않습니다. 기존 `LogModel`·`LogBridge`·controller의 대상/문맥·홀드·해제/중립 보호를 유지합니다.

2026-10-19 22:45:14 KST 후보의 전체 12개 검사 프로그램·UI/입력 3,201개·실제 DLL 연결 64개 검사가 통과했습니다. 빌드 입력 153개·생산 소스 63개와 ZIP CRC·정확히 두 파일·내부 바이트/해시도 일치했습니다. **2026-10-19 22:58:37 KST에 [v1.0.5-alpha.8](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.8)을 게시했습니다.** 소스·태그 `ce67f91704533e3db60985ca081b58e238cdd13e`을 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 12개 검사·입력 153개·소스 63개와 검토 DLL/cfg/ZIP 바이트 일치를 확인했습니다. 공개 [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.8/DiceMaster-v1.0.5-alpha.8.zip)의 SHA-256은 `51d23774b16c53b1378d34f735cc461f0a440a8f075ab82825d9c5b3589412ea`, DLL은 `7657962841ad221fa3c3254fe4fc88d297a524d3f7eb7640a89bd939bb28622f`, cfg는 `bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`입니다. 익명 다운로드의 CRC·두 파일·내부 바이트/해시도 확인했습니다. 결과는 `artifacts/releases/v1.0.5-alpha.8/`에 보존하며 [설치·복구·Windows 확인](ISSUE-4-ALPHA8-RELEASE.md)을 따릅니다. cfg는 그대로 두고 DLL만 교체합니다. 실제 Windows 확인과 이슈 #4 완료는 대기입니다. [로컬 후보 검토 기록](ISSUE-4-LIST-RESERVATION.md)과 아래 alpha.7 공개 기록은 당시 파일의 검증 이력입니다.

### v1.0.5-alpha.7 게시·고정 커밋 재빌드 검증

**2026-10-19 06:50:38 KST에 [v1.0.5-alpha.7](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.7)를 게시했습니다.** 소스·태그 커밋 `3746dbf0153fe131226bb149d68ba18e43e9fdd9`을 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 12개 검사·입력 153개·생산 소스 63개와 검토 후보의 DLL/cfg/ZIP 바이트 일치를 확인했습니다. 공개 [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.7/DiceMaster-v1.0.5-alpha.7.zip)의 SHA-256은 `922e6601c2c5d929c3458e9ad3d46e1ac690e7e8419ea981ee8f085e448b9d2b`이고 DLL은 `17044b1c90ce1e65ef1b253b6ebd131a202a787e3eb59e776ed1ac68e8bc18e4`, cfg는 `bec354ddd2663ea7e54053e5f00ed17202016259d6edda84445348c1fdf4c6b0`입니다. 공개 페이지·ZIP은 인증 없이 접근해 CRC·정확히 두 파일·내부 바이트/해시를 확인했습니다. 영수증은 `artifacts/releases/v1.0.5-alpha.7/`에 있으며 [설치·복구·Windows 확인표](ISSUE-4-ALPHA7-RELEASE.md)를 따릅니다. 기존 자산·Latest 정식 v1.0.4와 이슈 #4 open을 유지하고 실제 Windows 확인은 대기입니다.

### alpha.7에 포함한 로컬 후보: 한국어 표시 경로 통일

`--output-dir artifacts/issue-4/translation-coverage`로 목록·상세·선택지 제목, 이벤트 표시 캐시, 게임 서술 로그와 효과 요약의 표시 번역을 통일한 전체 소스 후보를 생성합니다. 이스케이프 등호와 전체 번역 키 매칭을 보완하며 내장 리소스 바이트와 원본 ID·예약 식별은 보존합니다. 2026-10-19 06:44:14 KST 후보의 전체 12개 검사 프로그램·실제 DLL 번역/서술 로그 3,511개·UI/입력 3,073개·DLL 연결 55개 검사가 통과했습니다. 입력 153개·소스 63개와 ZIP CRC·두 파일·내부 바이트·해시를 대조했습니다. [동작·후보·SHA-256·적용·Windows 확인](ISSUE-4-TRANSLATION-COVERAGE.md)을 따르며 DLL만 교체해 cfg를 보존합니다. 앞선 정상 문구 생략·패드 겹침 열기를 포함하고 이전 후보·공개 자산은 보존합니다. 실제 Windows 확인은 대기이며 커밋·게시를 수행하지 않았습니다.

### 이전 로컬 후속: 정상 조회 문구 생략

네 탭의 정상 항목에서 `조회 정상` 표시만 생략하고 읽음 정보·분석 상태·대기·실패·이전 값과 모델/예약 계약을 유지합니다. `--output-dir artifacts/issue-4/status-labels`로 앞선 시간 제한 없는 L3/R3 열기를 포함한 전체 소스를 빌드합니다. 2026-10-19 06:38:12 KST 후보의 전체 12개 검사 프로그램·UI/입력 2,740개·실제 DLL 연결 48개 검사가 통과했습니다. 현재 입력 153개·소스 63개와 ZIP의 CRC·두 파일·내부 바이트·해시를 대조했습니다. [상태 의미·후보·해시·적용·Windows 확인표](ISSUE-4-STATUS-LABELS.md)를 따르며 DLL만 교체해 cfg를 보존합니다. 실제 Windows 확인은 대기이며 커밋·게시를 수행하지 않았습니다. 기존 패드 열기 후보와 공개 자산은 보존합니다.

### 이전 로컬 후속: 패드 열기 시간 제한 제거

같은 소유자 패드의 한쪽 스틱 클릭을 길게 유지한 뒤 반대 클릭을 추가해 겹치면 열도록 300ms 조합 제한을 제거합니다. 양쪽 순서·동시 입력을 허용하고 게임 폴링 신선도·입력 취소·전체 중립·한 번 열기·홀드 차단·기존 옵션은 유지합니다. 1초 폴링 신선도는 첫 클릭 홀드 시간 제한이 아닙니다. [변경·후보·적용·복구·Windows 확인표](ISSUE-4-PAD-OVERLAP.md)를 따릅니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/pad-overlap
```

2026-10-19 06:29:49 KST에 새 DLL·ZIP·manifest를 생성했고 전체 12개 검사 프로그램이 통과했습니다. 패드 열기 1,408개 단언·35개 시나리오, 실제 DLL 연결 48개·UI/입력 2,720개·모델 372개·helper 통합 203개를 포함합니다. DLL은 582,656바이트·SHA-256 `c14dfdff3cc599f09231a85c6ec3ba658cfc95d1c4035248481a08eb66a96825`, ZIP은 245,027바이트·`8a210fa221d92d3ecf42ca7670b1c5447205cfba0f6b3e08fb7438f565e688e6`입니다. 전체 해시·검사표·시간 제한 회귀 실험은 위 후보 문서에 기록하며 이전 alpha.6 수치와 구분합니다. 전체 C# 소스와 읽기 전용 외부 게임 참조를 사용하고 게임 소스·DLL은 배포하지 않습니다. 적용은 DLL만 교체해 cfg를 보존하며 복구는 공개 alpha.6으로 가능합니다. 이번 후속에는 게임 실행·커밋·게시·이슈 #4 완료가 포함되지 않습니다. 아래 공개·로컬 후보 이력은 당시 파일과 결과로 보존합니다.

### v1.0.5-alpha.6 게시·공개 다운로드 검증

**2026-10-19 06:21:15 KST에 [v1.0.5-alpha.6](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.5-alpha.6)을 게시했습니다.** 소스·태그 커밋 `de0d511e60c99a6b84be33c9e3d7676746986c76`을 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 12개 검사와 입력 153개·생산 소스 63개 일치를 확인했습니다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.5-alpha.6/DiceMaster-v1.0.5-alpha.6.zip)은 검토한 DLL `4bcce037713407e25378ecbd2113649c9f9e52968659b1b748a6bec795645efa`·설정 `efd4847eb096aad70bfb7c3399534caab6becf71daca75fc5359100b0638cbf8`·ZIP `fe26f96e3c9512c7a31c10226ff3ba13a13f81b5ec8c6916c44d3376184f5cb0`과 같은 바이트이며 인증 없는 다운로드의 CRC·두 파일·바이트·해시를 확인했습니다.

결과는 `artifacts/releases/v1.0.5-alpha.6/`에 분리하며 Release ID `416720471`·asset ID `654515746`입니다. 기존 19개 Release·태그·자산과 Latest 정식 v1.0.4를 보존했습니다. [게시 설명·검사 범위·DLL만 적용·복구·Windows 확인](ISSUE-4-ALPHA6-RELEASE.md)과 [게시 기록](RELEASING.md)을 따릅니다. 실제 Windows 확인은 대기이고 이슈 #4는 open이며, 아래 개별 로컬 후보의 당시 기록은 보존합니다.

### alpha.6에 포함한 로컬 후보 이력: 패드 로그 열기

사용자 승인에 따라 같은 소유자 XInput 패드에서 첫 스틱 클릭을 유지한 채 반대 클릭이 300ms 미만에 겹치면 닫힌 로그를 여는 경로를 추가합니다. 같은 게임 폴링에서의 동시 클릭도 허용하며, 겹치지 않는 연속 클릭과 정확히 300ms 이상의 간격은 거부합니다. 기존 `LogMenuToggleKey`(기본 Tab)는 유지합니다. 새 `[DiceMaster].EnableGamepadLogOpen=true`는 주사위 조정 옵션과 독립적이며 조합은 열기 전용입니다. 초기 PRD의 패드 열기 제외는 이번 후속 지시로 대체하고 과거 후보·공개 자산은 보존합니다.

첫 조합 입력이 게임으로 새지 않도록 옵션 활성·게임 창 초점·로그 닫힘 동안 L3/R3 두 클릭을 예약합니다. 이때 단독 클릭도 게임으로 전달하지 않는 제약이 있습니다. 초기화·상태 재설정의 native 해제·중립 대기 구간에는 다른 버튼·스틱·트리거의 남은 입력도 소비합니다. `false`에서는 예약을 해제하고 남은 누름은 해제 후 반환합니다. 실제 게임의 직렬화된 바인딩 자료가 없어 스틱 클릭의 미사용을 확정하지 않습니다. 지원·검증된 native XInput 경로와 같은 소유자의 새 입력·전체 해제·중립을 사용하며 게임 scheme와 액션 맵을 변경하지 않습니다.

패드 열기 의도를 로그 입력 상태로 전달해 KM로 읽히거나 scheme이 자동 변경되어도 열린 뒤 첫 새 패드 탐색 입력을 전환용으로 다시 잃지 않게 합니다. 이 진입 의도는 단순 MouseMove·호버로 취소하지 않고 실제 포인터·키 조작이나 초점·준비·연결·닫기 경계에 기존 안전 전환을 적용합니다. 기존 Tab 진입의 모드 전환·첫 입력 소비 문제는 별도이며 이번 패드 열기 경로의 결과로 해결했다고 주장하지 않습니다. [설계·검사 계획·적용·Windows 확인](ISSUE-4-PAD-OPEN.md)을 따릅니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/pad-open
```

2026-10-19 06:14:54 KST에 후보를 생성했고 **전체 12개 검사 프로그램이 통과**했습니다. 새 `LogOpenerIntegrationTests`는 실제 native hook·filter·bridge·controller·action gate 소스를 연결한 관리 대역에서 28개 시나리오·993개 단언을 검증합니다. UI·입력 2,720개·모델 372개·실제 DLL 연결 47개·helper 통합 203개와 기존 분석·주사위 검사를 유지했습니다. 전체 모드 컴파일 오류 0개·기존 경고 7개입니다.

빌드 입력 153개와 ZIP CRC·정확히 두 파일·각 내부 바이트·해시를 독립 확인했습니다. DLL은 582,656바이트·SHA-256 `4bcce037713407e25378ecbd2113649c9f9e52968659b1b748a6bec795645efa`, ZIP은 245,066바이트·`fe26f96e3c9512c7a31c10226ff3ba13a13f81b5ec8c6916c44d3376184f5cb0`입니다. 전체 검사·설정 해시·[최종 검증 영수증](../artifacts/issue-4/pad-open-evidence/local-verification.json)은 [후보 기록](ISSUE-4-PAD-OPEN.md)을 따릅니다.

전체 C# 소스와 읽기 전용 게임 참조를 사용하며 게임 소스·DLL을 배포하지 않습니다. 실제 Windows 테스트는 사용자가 수행하고, 관리 대역에서 조합·소유권을 검증한 결과를 실게임 입력 정상으로 확대하지 않습니다. 후보 생성 당시에는 커밋·공개 게시·이슈 완료를 수행하지 않았고 이후 위 alpha.6으로 게시했습니다. 실제 Windows 확인과 이슈 #4 완료는 대기입니다.

### 직전 로컬 후보: 조건 분기 블록

`DataHelper.DescribeSwitchCheckpoint`가 실제 `ConditionResultItem` 단위의 `LogConditionBranch`와 `LogConditionDetails`를 한 번에 생성합니다. `LogBridge`는 이 구조를 번역·지역화해 모델에 게시하고, 렌더러는 조건 탭 상세에서 분기 제목·검사 맥락·원래 조건 설명을 별도 읽기 전용 블록으로 표시합니다. 기존 문자열 `ParseSwitchCheckpoint`는 같은 캡처의 호환 설명만 반환합니다. 줄바꿈·공백·`|`·번역된 논리 연산자에서 분기를 복원하지 않습니다.

실행 횟수 분기는 조건식으로 꾸미지 않고 중립 안내로 표시합니다. 빈 조건 묶음도 무조건 충족으로 설명하지 않습니다. 실패 시 마지막 정상 구조 전체를 보존하고 회복·새 문맥·항목 소멸에 기존 갱신·선택 무효화 계약을 적용합니다. [동작·검사 경계·적용과 Windows 확인표](ISSUE-4-CONDITION-BLOCKS.md)를 따릅니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/condition-blocks
```

2026-10-19 06:07:06 KST에 `artifacts/issue-4/condition-blocks/`의 새 DLL·ZIP·manifest·로그를 생성했고 **전체 11개 검사 프로그램이 통과**했습니다. UI·입력 2,720·모델 372·실제 DLL 연결 43·통합 203·선택지 통합 1,453·패드 262,831개 단언, 실제 게임 API 관리 동작 3,914개·메타데이터 1,047개를 확인했습니다. 전체 모드 컴파일 오류 0개·기존 경고 7개이며 빌드 입력 151개 일치와 ZIP CRC·정확히 두 파일·내부 바이트를 검증했습니다.

DLL 579,072바이트의 SHA-256은 `285ff419f46e1fdd5b0fc05a6047372cee6a0cefaa333d9ded20a22f1e96c86b`, ZIP 243,066바이트는 `7c726afe65b7f339a76eceb0bba974ab193bf7e397540d4432313d7cc7f9fc34`입니다. 설정·전체 검사표와 [최종 검증 영수증](../artifacts/issue-4/condition-blocks-evidence/local-verification.json)은 [후보 기록](ISSUE-4-CONDITION-BLOCKS.md)에 연결했습니다. 이전 읽기 화면 후보의 결과·해시와 구분합니다.

실제 새 DLL의 helper → Bridge → 모델 연결 검사는 실제 메서드 IL을 사용하되 게임 데이터·Unity·지역화는 명시적인 관리 대역입니다. Windows 확인은 대기 중이며, 로그를 연 직후 첫 패드 입력이 소비되는 문제도 이번 조건 화면 변경에서는 해결하지 않았습니다. 게임 설치·실행·커밋·공개 게시·이슈 완료는 수행하지 않습니다.

### 직전 로컬 후보: 읽기 화면

새 기본 창은 1920×1200의 **872×960px·본문 20px**을 기준으로 창 너비·높이와 글자 배율을 계산합니다. 기존 1080 기준 기본 배치와 60%/70% 배치를 전체 위치·크기·글자 튜플로 판별해 이행하고 사용자 지정 값은 보존합니다. 첫 표시 지연 초기화와 무효 화면 무저장을 유지합니다. [기준·설정 이행](ISSUE-4-VIEWPORT-1200.md)을 따릅니다.

선택지는 버튼 대신 읽기 전용 레이블로 표시하고 ↑↓·오른쪽 스틱을 전체 본문 스크롤로 사용합니다. 항목 선택·접기·예약 진입 없이 A·좌우는 소비하며, 항목 간격은 `max(8, font × 0.6)`입니다. 미션 정상 행의 `조회 정상`만 숨기고 대기·실패·이전 값과 비활성 상태 안내 행은 보존합니다. 공통 `GUILayout.BeginScrollView`의 강제 표시 인수는 가로·세로 모두 `false`로 두어 스크롤바를 필요한 경우에만 표시합니다. [렌더링·입력·Windows 확인 계약](ISSUE-4-READING-LAYOUT.md)을 따릅니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/reading-layout
```

출력 위치는 위 폴더의 `DiceMaster.dll`, `DiceMaster-source-candidate.zip`, `build-manifest.json`, `build.log`입니다. **전체 11개 검사 프로그램이 통과**했습니다. UI·입력 2,674·모델 361·실제 후보 DLL 연결 40·통합 182·패드 262,831개 단언이며 전체 모드 컴파일 오류 0개·기존 경고 7개입니다. 151개 입력이 manifest와 일치하고 ZIP CRC·두 파일·내부 바이트를 확인했습니다. DLL SHA-256은 `aaecbe9f0ef8005375c67da104baada97c288bf1f07caccb804de3c2ee8938b8`, ZIP은 `dcb44b006dfd664da80c6224b841103464d69682b3407a6be82dde5a0c157768`입니다. 이전 alpha.5의 결과를 승계하지 않습니다. 실제 Windows의 스크롤바 표시·글자 배치·터치·패드는 사용자 확인 대상입니다. 기존 cfg를 보존해 DLL만 적용하고 필요할 때 도구 기본값 복구를 사용합니다. 커밋·공개 게시·이슈 #4 완료는 이번 로컬 후속에 포함하지 않습니다.

### 이전 공개 후보: alpha.5

**alpha.5는 미션 상태 안내 행까지 포함한 당시 공개 후속**입니다. Bridge가 미션 탭의 값 없는 게시 행에만 `IsStatusOnly`를 지정하고, 모델은 일반 행과 안내 행 각각의 기존 순서를 유지한 채 안내 행을 끝에 배치합니다. UI와 모델은 모든 입력의 안내 행 선택·예약을 거부하며 상태 요약·원본 ID·진단 행 자체는 보존합니다. 실제 값이 있는 대기·실패 미션과 읽기 전용 선택지·조건은 계속 선택할 수 있습니다. [당시 변경·검사·후보·Windows 확인](ISSUE-4-MISSION-STATUS.md)을 따릅니다.

```sh
python3 build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/mission-status
```

일반 전체 C# 소스 빌드와 11개 자동 검사 프로그램을 통과했습니다. 모델 361·UI/입력 2,597·실제 DLL 연결 40·실제 Bridge 통합 182·패드 262,831 단언을 확인했으며, 컴파일 오류 0개·기존 경고 7개입니다. [후보·해시·실제 DLL 경계와 최종 검사](ISSUE-4-MISSION-STATUS.md)를 따릅니다. 검색 없는 전체 행·17개 도구·터치·예약·설정 계약을 유지하고 ZIP은 cfg와 새 DLL 두 파일입니다. 게임 DLL은 읽기 전용 외부 참조입니다.

사용자 승인에 따라 `v1.0.5-alpha.5`의 고정 소스 커밋을 `verify-preview-release.py --scope prd4`로 재빌드합니다. 검토 DLL `1ce1f0fa8d4d0dba626102c787c5e382cea5414861c050397ab4e506fe2b50d0`과 설정 `fce24616e5380004bc87aa992871407386d90bf1b6ec6f30116a29347fe0f046`을 요구하며 결과는 `artifacts/releases/v1.0.5-alpha.5/`에 분리합니다. [설치·복구·검증 범위](ISSUE-4-ALPHA5-RELEASE.md)를 따르고 실제 게시 결과는 별도 기록합니다. Windows 실행과 이슈 #4 완료는 주장하지 않습니다.

2026-10-19 05:37:30 KST에 [alpha.5 prerelease](ISSUE-4-ALPHA5-RELEASE.md)를 게시했습니다. 소스 태그 `70e045e17351ffd01ff0e19f5f8ba638b2b062c4`의 깨끗한 재빌드·11개 검사·공개 페이지/ZIP의 CRC·두 파일·바이트·해시가 통과했습니다. 검토 DLL·설정뿐 아니라 ZIP `4b15898c0c2eee42c6680839fb884403d93f779429d8452a6aa98e4965c61c8c`도 같으며 게시 영수증은 위 release 출력 폴더에 보존합니다. 기존 18개 Release·태그·자산과 Latest 정식 v1.0.4를 유지하고 이슈 #4는 open입니다.

아래 별도 출력 경로와 게시 정보는 **이전 후보·공개 버전의 재현 기록**입니다. 그 기록의 검색·자판·검사·해시는 당시 상태이며 최신 후보에 승계하지 않습니다.

검색 제거 후보는 `--output-dir artifacts/issue-4/no-search`에 보존합니다. [검색·자판·관련 모델 로직 제거와 당시 검사·해시](ISSUE-4-NO-SEARCH.md)는 그 후보의 기록이며 이번 미션 상태 안내 행 후보의 최종 수치로 재사용하지 않습니다.

패드 모드의 터치 복구 후속은 `--output-dir artifacts/issue-4/touch-input`에 생성합니다. [임시 커서 접근·입력 전환 보호·후보와 Windows 확인](ISSUE-4-TOUCH-INPUT.md)을 따릅니다. 모드 조회는 계속 읽기 전용이며 실제 Windows 터치 확인과 공개 게시를 구분합니다.

상세 제목의 포커스 단계를 없애고 활성 예약 컨트롤에 직접 진입하는 후속은 `--output-dir artifacts/issue-4/action-focus`에 생성합니다. [실행 가능한 컨트롤 진입 보호·조작·검사](ISSUE-4-ACTION-FOCUS.md)를 따릅니다. 앞선 탭 구성·첫 표시 초기화·화면 비율 수정을 함께 빌드하며 이전 후보는 당시 결과로 보존합니다.

선택지 항상 공개·조건 목록/상세 분리 후속은 `--output-dir artifacts/issue-4/tab-layout`에 생성합니다. [탭 성격·변경·후보와 검사](ISSUE-4-TAB-LAYOUT.md)를 따릅니다. 앞선 첫 표시 초기화·화면 비율 로컬 변경도 함께 빌드하고 이전 후보와 공개 자산을 보존합니다.

첫 표시 때 기본값을 적용하고 창을 화면의 약 60%×70%로 조정한 로컬 후보는 `--output-dir artifacts/issue-4/viewport-defaults`에 생성합니다. [초기 해상도·설정 경계와 후보 검사](ISSUE-4-VIEWPORT-DEFAULTS.md)를 따릅니다. 생성자는 설정을 정규화·저장하지 않고 첫 유효한 Draw가 적용하며, 실제 DLL과 관리 코드 회귀 검사·Windows 확인을 구분합니다.

사용자 후속 지시로 카드 탐색·창 확대·목록 행 높이 수정의 `v1.0.5-alpha.4`를 2026-10-19 04:46:13 KST에 게시했습니다. 소스 커밋 `2bb45aa75b3c863f91d70bf1d78fd413c9a6a45d`를 `verify-preview-release.py --scope prd4`로 깨끗하게 재빌드해 검토 DLL `06485af8929579f6f7d3744f183aa3359ec40545a6d155257c4bfebadc3b7d44`·설정 `b351703e2b0f1b5ea6b4a5894c1f5309a13dece5d6b06ed909330c0771801259`와 바이트 일치를 확인했습니다. 11개 검사 프로그램·UI 11,996/연결 23 단언과 공개 페이지·ZIP의 CRC/바이트/해시 검증을 통과했습니다. 출력·게시 영수증은 `artifacts/releases/v1.0.5-alpha.4/`이며 [설치·복구·해시·미확인 범위](ISSUE-4-ALPHA4-RELEASE.md)를 따릅니다. 기존 자산과 Latest 정식 v1.0.4, 이슈 #4 open을 유지하며 Windows 확인은 대기 중입니다.

카드 방향키·기본 창 1280×960·목록 행 높이·상태 안내의 로컬 후속은 `--output-dir artifacts/issue-4/card-navigation`에 생성합니다. [변경·후보 해시·자동 검사와 Windows 검증](ISSUE-4-CARD-NAVIGATION.md)을 따릅니다. 전체 소스와 11개 검사 프로그램을 사용하며 기존 후보·공개 자산은 보존합니다.

alpha.3은 소스 커밋 `3b245130a1e6530e4b482d2a4965eb3456d6ec91`의 깨끗한 재빌드·11개 검사·검토 후보 해시 일치를 확인한 뒤 2026-10-19 04:24:38 KST에 게시했습니다. 인증 없는 공개 페이지·설치 ZIP 검증도 통과했습니다. [게시 결과와 해시](ISSUE-4-ALPHA3-RELEASE.md)를 따르며 실제 Windows 성공 여부는 확인 대기입니다.

alpha.2의 작은 빈 창 회귀는 [별도 원인·검사 기록](ISSUE-4-WINDOW-SIZE-REGRESSION.md)을 따릅니다. 고정 크기 창 API로 바꾼 후보는 `artifacts/issue-4/fixed-window/`, 게시 전 고정 커밋 검증은 `artifacts/releases/v1.0.5-alpha.3/`에 분리합니다. 기존 후보·자산과 설정을 보존하고 실제 Unity/native 검증은 사용자가 수행합니다.

사용자 지시에 따라 [alpha.2 UI 후속](ISSUE-4-ALPHA2-RELEASE.md)을 `389292a85469c046032d498561a3470387429f61`로 커밋했습니다. `verify-preview-release.py --scope prd4`로 해당 커밋만 재빌드한 DLL·설정·ZIP이 검토 후보와 일치했고 11개 검사 프로그램을 통과했습니다. 2026-10-19 04:13:07 KST prerelease 게시와 인증 없는 공개 페이지·ZIP 다운로드 검증을 마쳤습니다. [배포 기록](RELEASING.md)을 따르며 실제 Windows 확인은 별도입니다.

alpha.1 UI 크기·스크롤 후보는 `--output-dir artifacts/issue-4/ui-followup`에 보존하고, 창 전체 드래그 복원을 포함한 후속은 `--output-dir artifacts/issue-4/window-drag`에 생성합니다. 기존 후보·공개 파일을 보존합니다. [UI 후속 기록](ISSUE-4-UI-FOLLOWUP.md)의 새 해시·레이아웃/선택 스크롤 검사를 확인합니다. 전체 소스 프로젝트와 기존11개 검사 프로그램을 사용하며 실제 Windows UI 검증은 별도입니다.

**최초 alpha.1 게시 전 로컬 후보의 재현 기록:** PRD #2 완료 뒤 `102cafe` 기준의 미커밋 소스에서 `build/build-source.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/issue-4/candidate`를 사용합니다. 전체 모드 C# 컴파일·기존 검사에 새 GUI 모델/입력/게시 연결 검사를 포함하고 설정+새 DLL 두 파일 ZIP과 영수증을 생성합니다. [구현·최종 검사·후보 해시](ISSUE-4-IMPLEMENTATION.md)와 [사용자 Windows 검증표](ISSUE-4-WINDOWS-VALIDATION.md)를 따릅니다. 이 후보는 이전 Windows 확인과 동등한 런타임 결과를 주장하지 않으며 공개 배포를 수행하지 않습니다. 아래 고정 alpha.3 기준은 기존 복원/비교용 이력입니다.

## 고정 기준과 최초 복원 시작 상태

| 항목 | 기준 |
|---|---|
| 복원·비교 버전 | `v1.0.3-alpha.3` |
| 기준 태그 커밋 | `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec` |
| 기준 DLL | 고정 커밋의 `plugins/DiceMaster/DiceMaster.dll`, 462,336바이트. 현재 재생성 위치는 `artifacts/baselines/v1.0.3-alpha.3/DiceMaster.dll` |
| 기준 DLL SHA-256 | `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633` |
| 작업 시작 HEAD | `9ca7730` (`docs: record verified v1.0.3-alpha.3 publication`) |
| HEAD와 기준 차이 | 9개 문서만 변경, 34줄 추가·22줄 삭제. 기준 DLL·C# 관리 소스·도구·설정은 동일 |
| 시작 시 기존 미추적 파일 | `.agents/`, `docs/ISSUE-2-START-PROMPT.md`, `docs/ISSUE-2-TECHNICAL-HANDOFF.md`, `skills-lock.json` — 그대로 보존 |

시작 시 HEAD·작업 트리·기준 태그와 실제 DLL 해시를 대조했습니다. HEAD의 게시 확인 문서 변경을 기준 모드 변경으로 취급하지 않았으며 기존 변경사항을 되돌리거나 정리하지 않았습니다. 과거 임시 추출본 대신 고정 해시를 확인한 기준 DLL에서 새로 복원합니다. 원 작성자의 프로젝트 구조·주석 원문까지 복구했다는 뜻은 아닙니다.

## 일반 C# 저장소 구조로 정리한 범위

최초 복원·후보 빌드 변경의 커밋 `2f1b90515ee52bd4b874cd81453f29f82ba0db31`을 바탕으로 디렉터리와 명령 경로를 정리했습니다. 비교 기준은 위 alpha.3 커밋·DLL로 고정하며 과거 임시 추출본이나 이전 후보 출력을 새 소스 입력으로 사용하지 않습니다. 기존 미추적 파일과 과거 생성 결과는 삭제하거나 정리하지 않습니다.

루트 솔루션과 `src/`·`tests/`를 일반 개발 진입점으로 만들고, `build/`에는 실행 가능한 빌드·검증 명령을, `tools/SourceRecovery/`에는 최초 복원 도구·프로젝트 템플릿을, `tools/LegacyPatch/`에는 기존 패치 도구와 payload 프로젝트를 배치합니다. 런타임 타입·네임스페이스·내장 리소스 논리 이름은 폴더 이동 때문에 바꾸지 않습니다. 예전 패드·로그 패치와 복원 명령은 `development/tools/`의 세 호환 진입점에서 새 경로로 전달합니다.

`global.json`은 검증한 SDK를 지정하고 루트 `Directory.Build.props`는 공통 생성 경로와 제외 규칙만 둡니다. 주 모드의 net48과 검사 도구의 net8.0 타깃은 각 프로젝트에 남겨 공통 설정이 검사 대상을 덮어쓰지 않게 합니다. 새 후보 패키지 설정은 `packaging/`에서 관리하고 루트 배포 사본 `config/`·`plugins/`는 제거했습니다. 고정 Git 이력에서 기준을 재생성하므로 구조 정리 때문에 비교 기준이나 과거 배포 자산이 달라지지 않습니다. 상세 트리는 [개발 안내](DEVELOPMENT.md#파일-구성)에 있습니다.

## 소스와 리소스의 소유권

`src/DiceMaster/DiceMaster.csproj`는 선택지 파서·번역·UI·주사위·후킹·어셈블리 속성과 Gamepad·LiveLog를 포함한 전체 모드 프로젝트입니다. 복원 C# 26개와 기존 관리 C# 8개(Gamepad 2개, LiveLog 6개)를 같은 소스 트리에 둡니다. Gamepad·LiveLog는 실제 하위 폴더이며 주 프로젝트의 형제 payload 프로젝트를 링크하지 않습니다. 기준 DLL에서 추출한 같은 타입은 제외하여 한 번만 컴파일하고 기존 후킹을 유지합니다. `DiceMaster.sln`은 주 프로젝트와 검사 프로젝트를 모으며 보존한 legacy payload는 포함하지 않습니다.

UI/번역 리소스 5개와 MinHook x86/x64 DLL·라이선스 3개는 `src/DiceMaster/Resources/`에 둡니다. native 파일은 `Resources/native/`에 있으며 8개 모두 원래 논리 이름으로 내장합니다. 모든 리소스의 이름·속성을 기준과 비교하며, 7개는 크기·바이트 일치를 요구합니다. 번역 리소스 하나의 주석 세 줄만 정리한 차이는 기준·후보의 고정 SHA-256과 모든 비주석 행·빈 행·줄바꿈 비교로 한정합니다. 기준의 에셋 리소스는 `Public`, 나머지 7개는 `Private`이며 이 속성을 그대로 유지합니다. 게임 소스는 복원하지 않으며 게임 DLL은 저장소에 복사하거나 내장하지 않습니다.

새 후보 ZIP의 설정 원본은 `packaging/config/DiceMaster.cfg`입니다. 루트 `config/`·`plugins/`가 없어도 일반 소스 빌드·후보 패키징을 수행합니다. alpha.3 비교·복원 기준은 고정 Git 커밋의 blob에서 해시를 확인하여 재생성하며, 기존 패치 출력도 `artifacts/legacy-candidate/`로 분리합니다. 후보 ZIP의 설치 경로 `config/DiceMaster.cfg`·`plugins/DiceMaster/DiceMaster.dll`과 두 파일 구성은 유지합니다.

현재 소스 프로젝트에 내장하는 모드 리소스의 식별은 다음과 같습니다. 비교 도구는 이 파일 목록만 보는 대신 기준 DLL과 후보 DLL에서 실제 내장된 데이터를 읽어 이름·속성·바이트를 비교합니다.

| 내장 논리 이름 | 바이트 | SHA-256 |
|---|---:|---|
| `DiceMaster.Assets.mortaldice` | 7,066 | `5cb5c10adea77234bbef4f8f348514526c9765e9c45fe2b4e8d3c334ecfc0ff6` |
| `DiceMaster.Gamepad.Native.LICENSE.txt` | 4,365 | `d5c2224982b0b95a16b098a561335fa93f3eaf4d0aa964b7843edb986df78dc8` |
| `DiceMaster.Gamepad.Native.MinHook.x64.dll` | 16,384 | `033e55af8995c3b260e56de73daf99ff4a7ce2fcbe4b3872db2270678d8c6585` |
| `DiceMaster.Gamepad.Native.MinHook.x86.dll` | 12,800 | `1f28d13b8133551c8896cc556db768c94ebfaae0ad38e86719f3151f7b4ce1fc` |
| `DiceMaster.Translations.ModTranslation.txt` | 44,278 | `7441c94d83df9f2ebe6ecdfe0489c752afe7ebeadc7b28ae6d15aca460acf5eb` |
| `DiceMaster.Translations.Pairs` | 147,030 | `172489a3a34fd27d93410b71b23dcfeb6d597723e5d2e0f4878eef464899ec00` |
| `DiceMaster.Translations.TriggerTranslation1.0.5v.txt` | 8,521 | `a528b4740da8fe1fd5218e50d8113720306ea1db21b959b0ab1545c13587dbd4` |
| `DiceMaster.Translations._Substitutions.txt` | 2,514 | `640d5fc2a5fae476b766c5b59481a10e7e7d68a605557485f5bf2277e6569909` |

기준 DLL의 `ModTranslation.txt`는 44,349바이트 / SHA-256 `6fc71ac1780be28b78e5ee193835a38e47d1526cbb9a143d05ff5206eceef1ec`입니다. 별도 모드 자료 정리에 따라 주석 세 줄만 수정했으며 공용 게임 UI·플래그·참고문 번역은 DiceMaster에서 사용하므로 유지합니다. 다른 7개 리소스와 모든 C# 파일은 이 정리로 바꾸지 않습니다. 복원 도구도 새 임시 추출에 같은 주석 보정을 적용하므로 이전 임시 자료 없이 현재 소스를 재생성합니다.

어셈블리 이름 `DiceMaster`, Assembly/FileVersion `1.0.0.0`, InformationalVersion `1.0.0`, BepInPlugin GUID/이름 `DiceMaster`와 버전 `1.0.0`은 기준과 동일합니다. 배포 태그와 어셈블리 내부 버전은 별도 식별입니다. 이미 게시한 소스 빌드의 배포 태그는 `v1.0.3-alpha.4`이며 정식 배포 태그는 `v1.0.3`입니다. 이후 정리 결과와 각 게시 파일은 아래 별도 기록으로 구분합니다. 내부 버전은 기존 계약 보존을 위해 올리지 않습니다. DLL SHA-256으로 실제 산출물을 구분합니다.

기존 UI·한국어·네 탭·50자 선택지 요약/로그 전체 설명·설정 키와 기본값·세이브 형식·주사위/패드·로그/난수·수명주기 계약을 유지합니다. 이슈 #2 효과 분석 확대나 버그 수정은 포함하지 않습니다. 제자리/무동작/실패 로드 후 다음 성공 Lua까지 대기할 수 있는 alpha.3의 기존 제한은 그대로 남습니다. 이번 전환 때문에 이슈 #1·#2의 범위나 상태를 변경하지 않습니다.

## 준비 환경과 읽기 전용 외부 참조

Python 3.9 이상(최초 실행 `3.9.6`), 루트 `global.json`이 지정한 .NET SDK `8.0.426`과 최초 NuGet 패키지 복원이 필요합니다. `rollForward=latestPatch`는 같은 SDK 기능 대역의 최신 패치만 허용합니다. 모드는 .NET Framework 4.8로 컴파일하며 `Microsoft.NETFramework.ReferenceAssemblies.net48` 1.0.3을 사용합니다. 검사·호환성 도구는 .NET 8에서 Mono.Cecil 0.11.6으로 메타데이터를 읽습니다. 빌드 도구는 `NuGetAudit=false`로 네트워크 감사 조회를 제외해 오프라인 반복 실행을 가능하게 합니다. 패키지 보안 감사 통과를 뜻하지 않습니다. 소스 일반 빌드에는 ILSpy나 예전 임시 폴더가 필요하지 않습니다. 표준 SDK 설치는 `dotnet`을 PATH에 두면 됩니다. 이번 로컬 실행은 이미 준비한 SDK·패키지 캐시를 다음과 같이 지정했습니다.

```sh
export DOTNET_ROOT=/tmp/dicemaster-dotnet
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME=/tmp/dicemaster-cli
export NUGET_PACKAGES=/tmp/dicemaster-nuget
export PYTHONDONTWRITEBYTECODE=1
```

이 경로는 도구/패키지 캐시이며 과거 모드 추출본을 빌드 입력으로 쓰는 경로가 아닙니다. 다른 환경에서는 설치한 SDK와 로컬 NuGet 캐시를 사용하면 됩니다. ILSpy 실행 파일이 비표준 SDK 설치를 사용하면 `DOTNET_ROOT`도 지정합니다. 이번 복원 도구는 `/tmp/dicemaster-tools/ilspycmd`의 고정 버전을 사용했습니다.

이번에 사용한 외부 참조 루트는 다음 경로이며 입력으로만 읽습니다.

```text
/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018
```

루트 아래 `Mortal_Data/Managed/`와 `BepInEx/core/`의 게임·Unity·로더 어셈블리를 사용합니다. 프로젝트의 참조는 `Private=false`로 설정해 빌드 출력으로 복사하지 않습니다. 참조의 경로·필요 파일 15개·해시는 `build/reference-contract.json`과 대조하며 누락·잘못된 버전은 빌드 실패입니다. 다른 게임 버전을 사용하려면 명세·API 계약을 별도 검토해야 하며 해시 검사를 우회하지 않습니다.

명세의 필수 파일은 다음과 같습니다. 파일별 크기·SHA-256은 `build/reference-contract.json`에서 확인합니다.

| 폴더 | 필수 DLL |
|---|---|
| `Mortal_Data/Managed/` | `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.AssetBundleModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.TextRenderingModule.dll`, `UnityEngine.InputLegacyModule.dll`, `Mortal.Core.dll`, `Mortal.Story.dll`, `Mortal.Free.dll`, `OBB.Framework.dll`, `Fungus.dll` |
| `BepInEx/core/` | `0Harmony.dll`, `BepInEx.Core.dll`, `BepInEx.Unity.Mono.dll` |

수신 기존 파일·로그가 식별한 환경은 Steam appid `1859910` / buildid `26021712`, Unity `2020.3.49f1`, BepInEx `6.0.0-be.692`, Harmony `2.10.1`입니다. 이 식별은 새 Windows 실행이나 호환성 통과 기록이 아닙니다. 원본 참조의 수신 모드 DLL은 복원 기준으로 사용하지 않습니다.

## 일반 C# 빌드와 검증 후보

저장소 루트의 솔루션을 열거나 다음 명령으로 컴파일합니다. `GameReferences`는 로컬 읽기 전용 외부 참조 루트입니다.

```sh
dotnet build DiceMaster.sln -c Release \
  -p:GameReferences='/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

루트 `Directory.Build.props`가 공통 출력 설정을 적용합니다. 주 DLL은 `artifacts/bin/DiceMaster/release/DiceMaster.dll`, 중간 파일은 `artifacts/obj/`에 생성합니다. 이 명령은 컴파일이며 일곱 실행 검사와 설치 ZIP을 만들려면 다음 후보 빌드 명령을 사용합니다.

```sh
python3 build/build-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

이 일반 소스 빌드는 기존 모드 DLL을 입력으로 읽거나 추출·병합·메서드 교체하지 않습니다. `DiceMaster.csproj`가 관리 소스·모드 리소스만 컴파일하며 Git 데이터·기준 DLL·ILSpy도 필요하지 않습니다. 기본 검증 후보 출력은 `artifacts/source-candidate/`입니다. 사용자 지정 위치에는 `--output-dir artifacts/<별도 후보>`를 사용합니다. 출력은 `artifacts/`의 하위 폴더이며 참조와 겹치지 않는 경로로 제한합니다. `artifacts/` 자체와 일반 컴파일 출력인 `artifacts/bin/`·`artifacts/obj/`는 후보 출력으로 사용할 수 없습니다. 이미 존재하는 대상은 유효한 소스 후보 manifest·DLL 해시가 일치하고 허용된 결과 파일만 있을 때 갱신합니다. 기존 배포 디렉터리나 알 수 없는 파일을 후보 출력으로 덮어쓰지 않습니다.

빌드는 새 임시 작업 폴더에서 컴파일하고 일곱 자동 검사(`LogRefreshTests`, `LiveLogEngineTests`, `LifecycleTests`, `LiveLogIntegrationTests`, `RealGameApiTests`, `GamepadTests`, `SourceBehaviorTests`)를 새 DLL에 실행한 뒤 후보를 생성합니다. 기본 출력은 다음과 같습니다.

```text
artifacts/source-candidate/
├── DiceMaster.dll
├── DiceMaster-source-candidate.zip
├── build-manifest.json
└── build.log
```

`build-manifest.json`에서 SDK·참조·새 DLL·ZIP 식별과 수행한 검사 결과를 확인합니다. `build_input_sha256`은 소스·리소스·설정·검사·빌드 도구의 실행 전후 해시를 기록하며 빌드 도중 입력이 바뀌면 출력을 실패시킵니다. ZIP의 `config/DiceMaster.cfg`는 `packaging/config/DiceMaster.cfg`의 바이트이며 `plugins/DiceMaster/DiceMaster.dll`은 이번 새 빌드 바이트입니다. 게임 DLL·게임 소스·다른 모드·문서·개발 도구를 넣지 않습니다.

검증한 DLL·ZIP·영수증은 한 디렉터리에서 원자적으로 교체합니다. 일반 컴파일·검증 후보를 구분하고 후보 결과를 한곳에 묶는 이유는 실패나 중단 뒤 서로 다른 실행의 DLL·ZIP·성공 기록이 섞이지 않도록 하기 위해서입니다. 기존 `development/releases/`는 과거 결과·게시 영수증의 보존 위치이며 새 빌드 입력이나 최신 후보 위치로 사용하지 않습니다.

후보와 기준 DLL을 비교하는 검사는 일반 빌드와 구분합니다.

```sh
python3 build/verify-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

기본 후보는 `artifacts/source-candidate/DiceMaster.dll`이며 다른 후보에는 `--candidate <후보 DLL>`을 지정합니다. `--baseline`을 생략하면 고정 Git 커밋의 기준 DLL을 자동 재생성합니다. Git 데이터가 없는 소스 압축본에서 비교할 때에는 같은 고정 해시의 파일을 `--baseline <기준 DLL>`로 명시합니다. 비교 결과는 `compatibility.json`, `compatibility.log`에 기록합니다. 메타데이터 계약 외에 기준/후보의 실제 번역·주사위 메서드를 같은 대역 환경에서 실행해 관찰 결과를 비교합니다. 호환성 도구는 정상 후보 통과 후 리소스 속성/바이트, 누락/중복 타입, 중복 후킹, Harmony `__state` 이름, 설정 기본값, P/Invoke 진입점, `PadState` packing, delegate 호출 규약과 번역 데이터 변조·과거 주석 복원을 포함한 12개 후보가 의도한 계약 때문에 실패하는지도 검사합니다. 비교에서만 고정 alpha.3 기준 DLL을 입력으로 읽습니다. 재컴파일한 DLL 전체 바이트 일치는 성공 조건이 아니며 DLL·ZIP 해시는 식별과 추적용입니다.

## Release 후보의 로컬 검증

다음 명령은 현재 관리 소스·검사·도구·패키지 설정만 새 임시 스냅샷에 복사하여 전체 빌드·일곱 검사·기준 비교·변조 거부 검사와 ZIP 검증을 실행합니다. 스냅샷에는 `.git/`, 루트 `config/`·`plugins/`, 예전 모드 DLL·생성 결과가 없으며 컴파일은 기준 DLL을 사용하지 않습니다. 기준 비교는 원래 저장소에서 고정 Git 기준을 재생성한 뒤 수행합니다.

```sh
python3 build/verify-release.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018' \
  --output-dir artifacts/release-candidate
```

출력에는 새 `DiceMaster.dll`, `DiceMaster-release-candidate.zip`, `release-verification.json`, 빌드·호환성 보고서와 로그를 함께 둡니다. 검사는 ZIP CRC·정확히 두 경로·새 DLL 바이트·스냅샷의 패키지 설정 바이트·SHA-256을 확인하고 소스 출처와 입력 해시를 기록합니다. `--version vX.Y.Z...`를 지정하면 ZIP 이름은 `DiceMaster-vX.Y.Z....zip`입니다. 출력은 비어 있거나 존재하지 않는 `artifacts/` 하위 폴더만 허용하며 재실행은 새 `--output-dir`을 사용합니다. 기존 게시 영수증이나 다른 결과를 덮어쓰지 않습니다.

게시할 커밋이 확정된 뒤에는 `--source-commit <40자리 전체 커밋 SHA>`로 해당 Git 커밋의 허용 목록만 추출하여 같은 검증을 재현할 수 있습니다. 인수를 생략하면 현재 작업 트리의 관리 입력을 검사하므로 미커밋 변경도 포함합니다. 이 명령은 로컬 검증만 수행하며 commit·push·태그·GitHub Release 게시를 실행하지 않습니다. Windows 확인과 기본 전환 조건은 계속 별도입니다.

## 기존 모드 DLL 없는 격리 빌드

```sh
python3 build/verify-clean-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

검사는 `src/`·`tests/`·`tools/`·`build/`·`packaging/`의 관리 소스와 루트 솔루션·공통 설정·SDK 명세를 새 임시 소스 스냅샷에 복사합니다. 내장할 MinHook x86/x64 DLL 두 개만 허용하며 다른 DLL이 소스에 있으면 즉시 실패합니다. `plugins/`, `.git/`, `artifacts/`, 예전 출력·도구 캐시와 과거 추출본을 포함하지 않으며 시작 시 `DiceMaster.dll`이 0개임을 확인합니다. 그 상태에서 일반 빌드·일곱 검사·두 파일 ZIP 생성을 그대로 실행합니다. 결과 `clean-build.json`, `clean-build.log`를 기본 후보 디렉터리에 기록하며 `--report-dir artifacts/<별도 후보>`를 사용할 수 있습니다. 원래 저장소 DLL을 이동하거나 삭제해 격리하지 않으므로 기존 작업 트리·배포 파일을 보존합니다.

## 최초 복원 재현과 설계 근거

비교·복원에 사용할 기준 DLL은 루트에 보관하지 않습니다. 다음 명령은 저장소의 고정 커밋에서 blob을 읽고 SHA-256을 검증한 뒤 Git에서 제외한 캐시에 재생성합니다.

```sh
python3 build/baseline.py
```

결과는 `artifacts/baselines/v1.0.3-alpha.3/DiceMaster.dll`입니다. 고정 기준은 브랜치 HEAD나 움직일 수 있는 태그 이름으로 결정하지 않으며 전체 커밋 SHA·저장소 내 blob 경로·DLL SHA-256으로 확인합니다. 정상인 캐시가 있어도 매번 고정 Git blob을 읽어 검증하고 캐시와 대조합니다. 캐시가 삭제되거나 일반 파일의 내용이 달라지면 검증한 blob으로 다시 생성하며 임의 DLL이나 과거 임시 추출본으로 대체하지 않습니다. 로컬 Git 이력에 고정 커밋이 없으면 캐시가 정상이어도 진단과 함께 실패하므로 필요한 Git 이력을 준비해야 합니다. 자동 네트워크 조회는 하지 않습니다. 일반 소스 빌드에는 Git 이력이나 이 캐시가 필요하지 않습니다.

기준 재생성과 실패 진단은 다음 명령으로 검사합니다. 고정 Git blob·해시 확인, 캐시 삭제/변조 후 재생성, 기준 이력 누락, 다른 DLL, 심볼릭 링크·디렉터리 거부와 기존 패치 출력의 기준 캐시 덮어쓰기 차단을 확인합니다. 보고서는 `artifacts/baseline-checks/baseline-checks.json`에 기록합니다.

```sh
python3 build/verify-baseline.py
```

최초 복원은 ILSpy `9.1.0.7988`로 새로 수행합니다. 관리 도구를 준비한 뒤 빈 출력 폴더로 복원합니다.

```sh
dotnet tool install ilspycmd --version 9.1.0.7988 --tool-path /tmp/dicemaster-ilspy
python3 tools/SourceRecovery/recover-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018' \
  --output /tmp/dicemaster-recovered \
  --ilspycmd /tmp/dicemaster-ilspy/ilspycmd
```

관리 소스가 같은 절차로 다시 만들어지는지만 확인하려면 `--output` 대신 `--check`를 사용합니다.

```sh
python3 tools/SourceRecovery/recover-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018' \
  --ilspycmd /tmp/dicemaster-ilspy/ilspycmd \
  --check
```

이 단계는 최초 추출·재현용이며 일반 빌드가 실행하지 않습니다. 고정 기준 SHA-256과 도구 버전을 확인하고 다른 DLL이나 비어 있지 않은 출력 폴더는 거부합니다. `recovery-manifest.json`은 추출 파일·통합한 관리 소스·native 리소스의 해시와 적용한 보정을 기록합니다. 컴포넌트의 `.gitattributes`와 복원 도구의 UTF-8/LF 출력은 Windows checkout에서도 C# 줄바꿈과 내장 리소스 바이트가 달라지지 않도록 관리합니다. 별도 임시 분석 자료가 없어도 저장소·기준 DLL·명시한 도구/참조로 재생성할 수 있게 합니다.

복원 시 보정은 다음으로 제한합니다.

- `Gamepad`·`LiveLog`의 추출본을 제외하고 기존 관리 소스를 새 프로젝트의 `Gamepad/`·`LiveLog/`에 복사해 한 번 컴파일합니다.
- C# 12 컴파일러가 `RefSafetyRules(11)`과 지원 속성을 다시 생성하도록 중복 모듈 속성을 제거합니다.
- `DataHelper.GetMissions`의 `item2 != LiveMissionFilter`를 `(object)item2 != LiveMissionFilter`로 명시하여 기준 IL의 객체 참조 비교 의미를 보존합니다.
- 디컴파일러가 복원한 기존 컴파일러 식별자를 유지하고 컴파일 후 IL을 다시 쓰지 않습니다.
- 작성한 net48 프로젝트 템플릿으로 참조·어셈블리 속성·리소스 이름을 지정합니다.
- `ModTranslation.txt`의 1·38·89행을 일반 게임 UI·DiceMaster 플래그·참고문 주석으로 바꿉니다. 번역 데이터와 CRLF는 보존하며 고정 기준 리소스 SHA-256을 확인한 뒤 적용합니다.

프로젝트를 전체 모드 폴더에 배치한 이유는 게임 경계와 파서·UI·번역의 구현을 한 컴파일 단위에서 검토하기 위해서입니다. Gamepad·LiveLog를 주 프로젝트의 실제 폴더로 통합한 이유는 alpha.3 관리 구현과 단일 컴파일 소유권을 유지하면서 추출본의 중복 타입·후킹을 방지하기 위해서입니다. 기존 패치 payload 프로젝트만 같은 관리 파일을 링크하여 비교·복구를 재현합니다. 후보 디렉터리와 비교 단계를 분리한 이유는 정상 소스 빌드의 모드 DLL 의존성을 제거하고 기존 배포물을 유지하기 위해서입니다. 외부 게임 참조를 명시하고 로컬 경로만 받는 이유는 참조 버전을 식별하면서 게임 코드의 저장소·설치 ZIP 유입과 원본 수정 경로를 막기 위해서입니다.

## 검사 계약과 한계

새 DLL을 기존 관리 코드·실제 파서/표시 통합·게임 API 메타데이터·수명주기·패드 검사에 연결합니다. 파서나 설명 본체를 정답 문자열을 반환하는 대역으로 바꾸지 않습니다. `SourceBehaviorTests`는 후보의 실제 `EmbeddedTranslations.Display`와 전체 내장 번역 쌍·식별자 마스킹/캐시·반복 적용, `DicePatch`의 signed int 경계·prefab 부재·revolution·창 재열기 초기화를 실행합니다. Unity·로더·패드 브리지는 대역이며 별도 비교에서는 같은 관찰값을 기준 DLL과 대조합니다. 게임·Unity·native 경계를 대역으로 옮겨 실행하는 기존 관리 검사는 실제 Windows 실행과 구분합니다.

| 비교 | 보존해야 하는 계약 |
|---|---|
| 어셈블리·설정 | .NET Framework 4.8, 어셈블리/플러그인 식별, 공개 API, 설정 키·기본값 |
| 후킹·리플렉션 | Harmony 대상·패치 서명·속성, 수명주기/Lua 경계, 중복 타입·후킹 부재 |
| native ABI | P/Invoke 이름·호출 규약·구조체 레이아웃, XInput/MinHook 연결 |
| 내장 리소스 | 8개 이름·속성·개수, 7개 바이트 일치, 번역 하나의 주석 세 줄만 고정 해시 차이 허용·비주석 데이터 일치 |
| 동작 | 실제 파서/요약/로그/캐시, 현재 탭·숨김, 실패 격리·이전 값·5초 재시도·수동 복구, 난수·게임 부작용 보존, 주사위·패드 |
| 설치 ZIP | 정확히 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 두 파일, DLL은 이번 새 빌드와 동일 |

컴파일러 최적화·로컬 변수·분기 배치·컴파일러 생성 타입은 IL 형태를 바꿀 수 있습니다. 기존 패치 공정의 메서드 IL 지문 검사는 그 공정의 비교/복구 검사로 유지하고, 소스 후보는 메타데이터·후킹/참조 계약과 실제 메서드 동작 검사로 평가합니다. 필요한 검사를 제거하거나 임의 허용 범위를 늘려 성공 처리하지 않습니다. 기존 `LiveLogArtifactTests`의 패치 전후 정확한 지문 비교는 수정하지 않습니다.

기존 검사 중 `LogRefreshTests`의 표시 보호 IL 검사는 원래 `ldsfld; brfalse; ret`의 장형 명령만 허용했습니다. 새 소스 컴파일러가 만드는 단형 분기 또는 반대 방향 분기도 비영(보호 중) 깊이에서 즉시 반환하는 동일 조건을 검증하도록 바꿉니다. `DisplayRefreshDepth` 필드 소유자·참조 횟수·분기 극성·정확한 반환 대상을 확인합니다. 본문이 비어 있는 예전 Lua Prefix에 한해서는 `ret` 또는 동일 깊이 필드 읽기/`pop`/`ret`도 허용합니다. 실제 새 Lua 후킹과 파서·표시 동작 검사는 별도로 유지합니다. 기능 검사를 생략하기 위한 일반 IL 무시는 추가하지 않습니다. 패드 검사는 같은 전체 조합·입력·ABI 검사를 새 최종 DLL에 1회 실행합니다. 과거 525,430개는 payload와 설치 DLL 두 입력에 각각 262,715개를 실행한 합계였으며, 전체 소스 후보는 별도 payload를 만들지 않으므로 262,715개가 동일 검사 범위의 1회 결과입니다.

별도 `SourceBuildContractTests`는 이름을 가진 타입·필드·메서드의 접근 범위·형식·속성·상수·마샬링·override 연결과 참조 어셈블리 식별을 대조합니다. Harmony 인벤토리는 컴파일러 생성 타입/메서드를 포함한 전체에서 비교해 숨은 중복 후킹도 확인하며 native 진입점·delegate 규약도 대조합니다. 컴파일러 생성 closure/state machine의 식별자, 명령 인코딩·토큰 순서·MVID·PE 바이트는 이 검사 범위 밖입니다. 어셈블리·모듈과 이름을 가진 타입/멤버의 custom attribute를 비교하며 컴파일러·디버그·TargetFramework 속성을 일괄 제외하지 않습니다. 런타임 타깃과 플러그인 식별도 확인합니다. 표준 컴파일러가 기준과 같은 `.NETFramework,Version=v4.8` TargetFrameworkAttribute를 생성합니다.

기준과의 메타데이터 차이는 다음 위치로 한정해 기록합니다. 나머지 named reflection·Harmony·native 계약은 정규화 없이 비교합니다.

- `DiceMaster.EmbeddedTranslations.Display`의 기존 컴파일러 캐시 필드 5개: `CS$<>9__CachedAnonymousMethodDelegate{1,4,6,9}`는 `CS_0024_003C_003E9__CachedAnonymousMethodDelegate{1,4,6,9}`로, `$$method0x600000e-1`은 `_0024_0024method0x600000e_002D1`로 바뀝니다. C# 식별자로 표현하기 위한 ILSpy escape이며 필드의 형식·접근 속성·초기 데이터는 비교합니다. 이 private/internal 컴파일러 필드를 원래 이름으로 직접 reflection하는 외부 코드의 경우는 이 비교의 보장 밖입니다.
- Harmony 대상이 아닌 내부 래퍼 `OptionInfoPatch.PreviewReadGroup`, `PreviewCheckComparison`, `PreviewReadEventRate`, `PositionEventCache.GeneratePreviewDynamicDesc`의 이름 없는 매개변수는 `P_0`로 복원됩니다. 매개변수의 형식·순서·플래그는 동일합니다.
- 기존 payload 복사기가 누락한 C# `params`의 `ParamArrayAttribute`가 연결 소스의 `LogBridge.Call`, `LogBridge.CallDisplayHelper`, `PreviewFormatting.Helper`에 추가됩니다. `CallDisplayHelper`만 public이며 기존 메서드·배열 인수의 형식·이름·플래그와 호출 동작은 동일합니다. 이 속성은 C# 호출자가 배열을 펼친 인수 문법으로 호출할 수 있게 하며 기존 배열 호출의 런타임 연결은 유지합니다. 따라서 public 메타데이터 전체 바이트가 동일하다고 주장하지 않습니다.

내장 번역 데이터·에셋과 native ABI의 자동 비교는 Unity/native 실제 호출·리소스 로드·Harmony 설치·다른 모드·Windows UI/패드·실제 게임 성능의 직접 실행 검사가 아닙니다. alpha.4의 사용자 Windows 완료 확인을 별도로 기록하며 환경 조합·항목별 상세 결과와 성능 측정값은 제공되지 않았습니다.

## 실패 처리·확인 방법

실패 경로를 재현하려면 다음을 실행합니다.

```sh
python3 build/verify-source-failures.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

이 검사는 폐기할 소스 스냅샷에서 SDK 누락/잘못된 SDK, 참조 누락/잘못된 해시, 의도적 컴파일 오류, 의도적 검사 오류에 더해 안전하지 않은 출력 루트·일반 빌드용 `artifacts/bin`/`artifacts/obj` 출력 지정·관리되지 않는 기존 출력·비교 사전 검사 실패의 11가지 경우를 만듭니다. 실제 게임 참조나 작업 소스는 수정하지 않습니다. 각 경우 비정상 종료·기대 진단·기존 후보 sentinel/관리되지 않는 파일 보존·새 ZIP 미생성 및 비교 실패 시 오래된 PASS 제거를 확인하고 `failure-paths.json`과 `failure-<case>.log`에 기록합니다. `--report-dir artifacts/<별도 후보>`를 지정할 수 있습니다.

참조 누락·잘못된 해시, 기준 DLL/복원 도구 불일치, 컴파일 오류, 중복 타입/후킹, 리소스 불일치, 검사 실패는 명시적 비정상 종료입니다. 이전 DLL을 새 성공 결과로 복사하거나 보관 DLL로 조용히 대체하지 않습니다. 실패 진단과 새 결과 식별을 확인한 뒤 수정하고 새 후보 디렉터리에서 다시 실행합니다. 기존 성공 후보는 실패한 실행 때문에 교체되지 않으며, 이전 영수증을 이번 실행의 성공으로 해석하지 않습니다. 후보 승격 중 중단되어 `.pending`/`.previous`가 남으면 내용을 확인한 뒤 복구하고 재시도합니다. 비교·격리 검사는 시작 시 이전 성공 JSON을 지우므로 실패 뒤 과거 PASS 기록을 새 결과로 남기지 않습니다.

문법 복원과 참조 보정은 기능 의미를 보존하는 범위로 제한합니다. 기능 변경이 필요해 보이는 경우 이번 복원에 섞지 않고 별도로 기록합니다. 런타임 진단은 기존 BepInEx 정책을 유지하며 전체 Lua·세이브·비밀정보를 새 로그에 추가하지 않습니다.

## 루트 배포 폴더 제거 후 빌드·Release 검증 — 2026-10-19 당시 기록

사용자 승인에 따라 루트 `config/`·`plugins/`를 제거했습니다. 패키지 설정은 기존과 바이트가 같은 `packaging/config/DiceMaster.cfg`만 유지합니다. 비교·복원·기존 패치의 기본 기준은 고정 Git 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`의 DLL blob에서 매번 SHA-256을 검증하여 재생성합니다. 일반 소스 컴파일은 Git 이력과 기준 DLL을 사용하지 않습니다. 기존 게시 태그·설치 자산은 변경하지 않았습니다.

| 이번 변경 후 실행 | 결과 |
|---|---|
| 루트 솔루션 Release 빌드 | 14개 프로젝트, 오류 0개·기존 경고 7개, DLL 해시는 아래 후보와 동일 |
| `verify-release.py` 현재 소스 | 새 소스 스냅샷에서 전체 컴파일·일곱 자동 검사·기준 계약 2,750개·변조 거부 12종·동작 관찰값 비교 4,341개 통과 |
| Release 설치 ZIP | 정확히 설정·새 DLL 두 파일, CRC·스냅샷 설정/빌드 DLL 바이트·SHA-256 일치 |
| 기존 모드 DLL·Git 데이터 없는 격리 빌드 | 시작 시 기존 `DiceMaster.dll` 0개, 일곱 검사 통과, Release 후보와 DLL·ZIP 해시 일치 |
| 기준 재생성·실패 보호 | 13종 통과: 루트 폴더 없음, 캐시 생성/삭제/오염 복구, Git 이력 누락 시 캐시 우회 거부, 잘못된 해시·symlink·비정상 파일·기준 덮어쓰기 거부 |
| 소스 빌드 실패 경로 | 11종 통과, 이전 후보 보존·새 패키지 미생성·오래된 PASS 제거 |
| Release 출력 보호 | 기존 게시 폴더·`artifacts/` 외부 경로·잘못된 버전·짧은 커밋 SHA 거부 4종, 기존 게시 ZIP 바이트 보존 |
| 전체 소스 복원 재현 | 고정 Git DLL의 새 추출과 현재 통합 프로젝트 44개 파일 `--check` 통과 |
| 기존 패치의 새 기본 경로 | 패드 525,430개 및 로그/엔진/수명주기/통합/API/리소스/반복 패치 검사 통과, 기준 DLL 해시 유지·루트 폴더 재생성 없음 |
| alpha.4 고정 소스 커밋 재현 | `--source-commit 46bbf7486ab424696f1f5ba7d4126f6c12d11184 --version v1.0.3-alpha.4` 통과, 기존 게시 DLL·ZIP 바이트 그대로 재현 |

이 절의 검증 당시 로컬 Release 후보는 `artifacts/release-candidate/DiceMaster.dll`(461,312바이트, SHA-256 `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1`)과 `DiceMaster-release-candidate.zip`(191,442바이트, SHA-256 `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7`)입니다. 이전 주석 정리 후보와 같은 제품 입력이므로 DLL·ZIP 해시도 같습니다. 이 루트 폴더 제거 변경은 기준 파일 보관·도구·문서 경로만 바꿨습니다.

재현한 alpha.4 DLL의 SHA-256은 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`, 설치 ZIP은 `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8`로 기존 게시 영수증과 같습니다. 이 결과는 현재 주석 정리 후보와 구분하며 과거 태그·Release를 덮어쓰지 않습니다.

현재 소스의 출처·입력 해시·SDK·참조·검사·ZIP 영수증은 `artifacts/release-candidate/release-verification.json`과 같은 폴더의 빌드/호환성 보고서에 있습니다. 고정 커밋 재현은 `artifacts/release-alpha4-reproduction/`, 격리/실패/기존 패치/출력 보호 로그는 `artifacts/root-removal-checks/`, 기준 재생성 13종은 `artifacts/baseline-checks/baseline-checks.json`에 기록했습니다. 현재 소스 검증은 미커밋 작업 트리 입력임을 영수증에 명시합니다.

이 절의 검증 당시에는 commit·push·태그·Release 게시를 수행하지 않았습니다. 이후 사용자가 이 변경의 commit·push를 승인하여 커밋 `0c520c5ed177cd1266d92be50b202caf64bbeb7d`를 `origin/main`에 반영했습니다. 이 시점에는 Windows 사용자 확인과 기본 소스 빌드 전환, PRD #3 전체 완료를 보류했으며 아래 2026-10-19 사용자 확인·정식 게시 절과 구분합니다. 루트 사본을 제거해도 고정 Git 이력이 있으면 같은 alpha.3 기준을 재생성할 수 있고, 게임 DLL·게임 소스는 저장소·설치 ZIP에 포함하지 않습니다.

## 별도 모드 자료 정리 후 로컬 검증

별도 모드의 DLL·설정 파일과 현재 문서의 관련 참조를 제거했습니다. C# 34개, 게임 UI·플래그·참고문 번역 데이터, `Pairs`의 2,130개 번역 쌍은 그대로 유지합니다. 빈 `DiceMaster.DebugPatch`는 외부 모드 의존성이 없는 기준의 공개 타입이고 `Awake`에서 참조하므로 메타데이터·기존 초기화 경로를 보존합니다. 번역 리소스의 주석 세 줄을 정리한 차이만 위 리소스 계약과 복원 도구에 명시했습니다.

수정한 입력으로 `build/build-source.py`, `build/verify-source.py`, `build/verify-clean-source.py`, `tools/SourceRecovery/recover-source.py --check`를 다시 실행했습니다. 일곱 자동 검사 묶음이 통과했으며 기준 대비 계약 2,750개·변조 거부 12종·번역/주사위 관찰값 4,341개도 통과했습니다. 기존 DLL 0개인 격리 스냅샷의 새 DLL·ZIP 해시는 일반 후보와 동일합니다. 주 모드 컴파일은 기존 경고 7개·오류 0개입니다. 실패 경로 도구나 기존 패치 공정의 동작은 이번 정리에서 바꾸지 않았으며 과거 검사 기록과 이번 실행을 구분합니다.

| 로컬 후보 식별 | 값 |
|---|---|
| DLL | `artifacts/source-candidate/DiceMaster.dll`, 461,312바이트 |
| DLL SHA-256 | `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1` |
| ZIP | `artifacts/source-candidate/DiceMaster-source-candidate.zip`, 191,442바이트 |
| ZIP SHA-256 | `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7` |
| 당시 보존 기준 | 루트 alpha.3 DLL SHA-256 `485581c2…`, 루트 설정과 `packaging/config/DiceMaster.cfg` 바이트 동일. 이후 루트 사본은 제거하고 고정 Git 기준을 재생성하도록 변경 |
| 검사 영수증 | `artifacts/source-candidate/build-manifest.json`, `compatibility.json`, `artifacts/comment-cleanup-checks/clean-build.json` |

이 정리 당시 후보는 로컬 결과였으며 기존 alpha.4 태그·게시 자산을 교체하지 않았습니다. 당시 기본 빌드 전환·이슈 완료 처리는 보류했고 Windows 설치·게임 실행도 수행하거나 요청하지 않았습니다. 후속 루트 폴더 정리에서는 고정 Git 커밋에서 기준 DLL을 재생성하도록 비교·복원·기존 패치 경로를 함께 변경했습니다.

## alpha.4 최종 소스 커밋 빌드·게시 — 2026-10-18

소스 태그 `v1.0.3-alpha.4`는 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`에 고정했습니다. 이 커밋을 `git archive`로 읽고 소스·리소스·빌드/검사·패키지 설정만 새 스냅샷에 복사했습니다. `plugins/`·기존 산출물·이전 DLL을 포함하지 않아 시작 시 `DiceMaster.dll`은 0개였습니다. SDK `8.0.426`으로 실제 커밋 입력을 다시 컴파일·검사했으며 빌드 도중 입력 변경이 없음을 확인했습니다. 원본 게임 참조는 읽기 전용입니다.

최종 DLL에 일곱 묶음의 검사(`LogRefreshTests` 19개, 엔진 16개 시나리오·70개 단언, 수명주기 4,023개, 통합 134개, 실제 API 관리 동작 3,914개·메타데이터 1,011개, 패드 262,715개, 번역/주사위 2,170개)를 실행해 통과했습니다. alpha.3 대비 계약 2,748개와 변조 후보 10종 거부, 번역/주사위 관찰값 비교 4,341개도 통과했습니다. 구조 정리 단계의 실패 경로 11종·복원 재현과 별도로 최종 커밋의 빌드/검사를 새로 수행한 결과입니다.

게시 DLL은 461,312바이트 / SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`, `DiceMaster-v1.0.3-alpha.4.zip`은 191,482바이트 / SHA-256 `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8`입니다. ZIP은 같은 소스 커밋의 `packaging/config/DiceMaster.cfg`와 이번 빌드 DLL만 포함합니다. 루트 `plugins/DiceMaster/DiceMaster.dll`은 기존 alpha.3 해시 `485581c2…`로 유지하므로 태그의 루트 DLL과 설치 자산 DLL은 다릅니다.

2026-10-18 23:42:14 KST에 prerelease로 게시하고 익명 공개 API·다운로드의 정확히 두 파일·CRC·ZIP/DLL 해시와 빌드 DLL·태그 패키지 설정의 바이트 일치를 확인했습니다. Release ID `416503505`, asset ID `653700999`입니다. 기존 여섯 Release·자산과 Latest 정식 `v1.0.2`는 유지했습니다. 로컬 영수증은 `artifacts/releases/v1.0.3-alpha.4/`에 있으며 [배포 기록](ISSUE-3-RELEASE.md)에 핵심 식별과 경로를 정리했습니다. 게시 기록 문서 후속 커밋은 소스 태그·자산을 변경하지 않습니다. 이 게시 당시에는 Windows 확인·기본 빌드 전환·이슈 #3 전체 완료를 보류했습니다.

## 구조 정리 후 로컬 검증 기록 — 2026-10-18

이 절은 구조 정리 직후, 새 commit·push와 alpha.4 게시 승인 전에 확보한 로컬 검증 이력입니다. 아래 HEAD·미커밋 상태는 당시 기록이며 현재 게시 상태로 해석하지 않습니다. alpha.4의 커밋·최종 빌드·게시 결과는 [배포 기록](ISSUE-3-RELEASE.md)에 별도로 기록합니다.

HEAD `2f1b905`에서 시작한 구조 정리의 새 결과입니다. 루트 `dotnet build DiceMaster.sln -c Release -p:GameReferences='<읽기 전용 참조 루트>'`로 14개 프로젝트를 빌드했습니다. 오류는 0개이며 주 모드의 기존 경고 7개(`CS0219`, `CS0169`, `CS0649`)를 유지했습니다. 일반 빌드의 `artifacts/bin/DiceMaster/release/DiceMaster.dll`과 검증 후보·기존 모드 DLL 없는 격리 빌드의 DLL 해시가 모두 같습니다.

| 새 구조에서 재실행한 검사 | 결과 |
|---|---|
| 검증 후보의 일곱 검사 묶음 | `LogRefreshTests` 19개, `LiveLogEngineTests` 16개 시나리오·70개 단언, `LifecycleTests` 4,023개, `LiveLogIntegrationTests` 134개, `RealGameApiTests` 관리 동작 3,914개·메타데이터 1,011개, `GamepadTests` 262,715개, `SourceBehaviorTests` 2,170개 통과 |
| alpha.3 기준/후보 계약 비교 | 메타데이터·Harmony 후킹·native ABI·내장 리소스 2,748개, 변조 후보 10종 거부, 번역·주사위 관찰값 비교 4,341개 통과 |
| 기존 모드 DLL 없는 격리 빌드 | 시작 시 `DiceMaster.dll` 0개, 일반 소스 빌드·일곱 검사·두 파일 ZIP 재생성 통과. 일반 후보와 DLL·ZIP 해시 일치 |
| 실패 경로 | SDK/참조·컴파일/검사·비교 사전 검사 실패와 출력 보호 11개 통과. 새 `artifacts/bin`·`artifacts/obj` 출력 거부 포함 |
| 최초 복원 재현 | 기준 DLL에서 새로 복원한 통합 프로젝트 44개 파일과 관리 소스·리소스의 `--check` 통과 |
| 기존 패치 명령의 호환 진입점 | 패드 525,430개, 로그 19개·엔진 70개·수명주기 4,023개·통합 134개·구조 958개·실제 API 동작 3,914개/메타데이터 1,011개 통과. 로그 반복 패치도 통과하며 무관한 IL 본문 356개·리소스 8개 유지 |
| 원본·작업 트리 보존 | C# 내용 바이트, 기준 배포 DLL·루트 설정, 시작 시 기존 미추적 7개 파일 유지. HEAD는 `2f1b905`이며 새 commit·push 미수행 |

| 현재 후보 식별 | 값 |
|---|---|
| DLL 경로 | `artifacts/source-candidate/DiceMaster.dll` |
| DLL 크기·SHA-256 | 461,312바이트 / `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1` |
| ZIP 경로 | `artifacts/source-candidate/DiceMaster-source-candidate.zip` |
| ZIP 크기·SHA-256 | 191,482바이트 / `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8` |
| ZIP 내용 | `packaging/config/DiceMaster.cfg`와 이번 DLL의 바이트를 기존 설치 경로의 두 파일로 포함, CRC 정상 |
| 기본 배포 DLL | 462,336바이트 / `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633` 유지 |

DLL 해시는 최초 복원 후보의 `704c967e…`에서 바뀌었습니다. 소스 파일 경로·컴파일 순서를 바꾼 재컴파일 결과의 식별 차이이며 DLL 전체 바이트 일치는 요구하지 않습니다. C# 파일 내용은 유지했고 위 기준/후보 동작·메타데이터·후킹·native ABI·리소스 계약을 새 DLL에 다시 확인했습니다.

새 영수증은 `artifacts/source-candidate/`의 `build-manifest.json`, `build.log`, `compatibility.json`/`.log`, `clean-build.json`/`.log`, `failure-paths.json`, `failure-<case>.log`입니다. 격리/실패 검사의 원래 출력은 `artifacts/layout-checks/`에도 보존하고 기존 패치 회귀 로그는 `artifacts/legacy-layout/gamepad.log`·`log-refresh.log`에 있습니다. 기존 `development/releases/` 기록은 그대로 둡니다. 당시 Windows 설치·게임 실행은 수행하거나 요청하지 않았으며 기본 배포 전환·PRD #3 전체 완료도 보류했습니다.

## 최초 복원 로컬 검증 기록 — 2026-10-18

이 절은 구조 정리 전 `2f1b905`에 기록한 최초 복원 결과이며 경로·해시는 당시 값입니다. 아래 기록을 현재 구조의 새 검사 결과로 승계하지 않습니다.

macOS에서 Python `3.9.6`·.NET SDK `8.0.426`과 위 읽기 전용 참조를 사용했습니다. 새 모드 컴파일은 오류 0개와 경고 7개입니다. 경고는 기준에서 유지한 사용되지 않는 변수/캐시·reflection으로 접근하는 필드(`CS0219`, `CS0169`, `CS0649`)이며 경고를 숨기거나 필드를 제거하지 않았습니다. 메타데이터 도구와 검사 프로젝트는 경고·오류 0개입니다. 이 결과는 기존 alpha.3의 과거 검사 수치를 새 DLL 결과로 승계한 것이 아닙니다.

| 새 후보에 수행한 검사 | 결과 |
|---|---|
| `LogRefreshTests` | 실제 메서드 본문/경계 19개 통과 |
| `LiveLogEngineTests` | 16개 시나리오·70개 단언 통과 |
| `LifecycleTests` | 4,023개 통과 |
| `LiveLogIntegrationTests` | 실제 파서·캐시·표시·요약·후킹 연결 134개 통과 |
| `RealGameApiTests` | 관리 동작 3,914개·실제 API/후킹 메타데이터 1,011개 통과 |
| `GamepadTests` | 새 DLL의 전체 입력 조합·signed int 경계·ABI 262,715개 통과 |
| `SourceBehaviorTests` | 실제 번역/주사위 2,170개 통과 |
| 기준/후보 메타데이터·리소스 | 2,748개 통과. 이름 있는 타입 61개·메서드 459개, Harmony 표시 메서드 30개, native P/Invoke/delegate 메서드 19개, 내장 리소스 8개 |
| 비교 검사 자체의 음성 검사 | 변조 후보 10개를 각각 기대한 계약 오류로 거부 |
| 기준/후보 번역·주사위 동작 | 양쪽 실행과 관찰값 비교 4,341개 통과 |
| 기존 모드 DLL 없는 격리 빌드 | 시작 시 `DiceMaster.dll` 0개, 일반 빌드·일곱 검사·ZIP 재생성 통과. 일반 후보와 DLL/ZIP 해시 모두 일치 |
| 실패 경로 | SDK/참조 누락·버전 불일치, 위험한 출력 루트, 관리되지 않는 출력, 컴파일/검사 실패, 비교 사전 검사 실패 9개 통과. 기존 파일 보존·새 패키지 미생성·오래된 PASS 제거 확인 |
| 읽기 전용 원본 참조 | 수신 인벤토리 159개 파일의 크기·SHA-256 불일치 0개 |

고정 기준의 새 추출과 관리 소스가 일치하는 `recover-source.py --check`도 통과했습니다. 기존 기본 패치 공정은 별도 `development/releases/issue-3-evidence/legacy-log.dll`에 출력하여 회귀 검사했습니다. 최초/반복 패치의 관리·통합·수명주기·실제 API 검사와 기존 정확한 artifact 검사 958개가 통과했습니다. 비교 대상 기존 IL 본문 356개는 모두 동일했고 리소스 8개도 동일합니다. 기본 스크립트·배포 DLL은 전환하지 않았습니다.

| 후보 식별 | 값 |
|---|---|
| DLL 경로 | `development/releases/source-candidate/DiceMaster.dll` |
| DLL 크기·SHA-256 | 461,312바이트 / `704c967ea895f280f2f96c8fc7a6ed818af5c03c73ad75371adb20e900ed9f18` |
| ZIP 경로 | `development/releases/source-candidate/DiceMaster-source-candidate.zip` |
| ZIP 크기·SHA-256 | 191,550바이트 / `ea61959e7d5fb8065df69834755e3712e3b675da33836b5313ce1c270f9c55b3` |
| ZIP 내용 | 설정과 이번 새 DLL 두 파일, CRC·각 파일 바이트 일치 |
| 기본 배포 DLL | 462,336바이트 / `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633` 유지 |

실행 영수증은 `development/releases/source-candidate/`의 `build-manifest.json`, `build.log`, `compatibility.json`, `compatibility.log`, `clean-build.json`, `clean-build.log`, `failure-paths.json`, `failure-<case>.log`에 있습니다. 기존 패치 공정의 별도 회귀 기록은 `development/releases/issue-3-evidence/legacy-log.log`에 있습니다. 이 디렉터리는 생성 파일이므로 Git에서 제외합니다. 기준/후보의 메타데이터 차이 12개는 위 목록과 영수증에 명시되며 DLL 전체 바이트 일치는 성공 기준으로 사용하지 않았습니다.

## 사용자 Windows 확인과 기본 전환

2026-10-19 사용자가 “사용자 Windows 검증은 완료했어”라고 확인하고, 검증에 사용한 파일을 묻는 후속 질문에 “alpha.4를 직접 받아서 했어”라고 답했습니다. 이는 공개 alpha.4 설치 자산에 대한 사용자 완료 확인입니다. 세부 항목별 체크표나 환경 버전은 제공되지 않았으며 기존에 수신한 참조 DLL·로그의 환경을 이 실행 환경으로 옮겨 쓰지 않습니다.

| 근거 | 확인 범위 |
|---|---|
| 확인 주체·일자 | 사용자 직접 확인, 2026-10-19 (Asia/Seoul) |
| Windows 검증 대상 | 공개 `DiceMaster-v1.0.3-alpha.4.zip` 직접 다운로드 |
| 검증 대상 ZIP SHA-256 | `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8` (공개 자산 식별값) |
| 검증 대상 DLL SHA-256 | `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1` (공개 자산 내 DLL 식별값) |
| 원본 소스 커밋 | `46bbf7486ab424696f1f5ba7d4126f6c12d11184` |
| 게임·BepInEx·패드·Steam Input 환경 | 사용자 제공 없음 |
| 항목별 실행 로그·성능 수치 | 사용자 제공 없음. 포괄 완료 확인을 환경 조합별 통과나 측정 결과로 확대하지 않음 |
| 정식 v1.0.3 DLL | `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1`; 이 DLL 자체를 Windows에서 새로 실행했다는 확인은 없음 |
| alpha.4 이후 제품 변경 | 제품 C# 34개·패키지 설정·내장 리소스 7개 유지. `ModTranslation.txt`의 주석 세 줄만 변경하며 비주석 데이터·빈 행·줄바꿈 유지 |
| 기본 경로 | 전체 소스 빌드·검증·패키징. `tools/LegacyPatch/`는 비교·복구용 |

정식 산출물과 alpha.4의 런타임 계약·메서드 IL·내장 번역 비주석 데이터 비교 결과는 정식 검증 영수증과 [배포 기록](ISSUE-3-RELEASE.md)에 기록했습니다. 사용자 Windows 확인, 이 자동 비교, 새 소스 커밋의 격리 빌드·자동 검사·설치 ZIP 검증을 정식 전환 근거로 함께 사용합니다. DLL 전체 바이트 일치나 현재 DLL의 새 Windows 실행 결과를 주장하지 않습니다.

정식 전환 준비 입력의 Release 검사·Git 없는 격리 빌드·실패 경로 11종을 다시 실행해 통과했습니다. DLL SHA-256은 `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1`, ZIP은 `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7`로 격리 빌드와 같습니다. Release 검사는 Windows 확인 대상의 고정 소스 커밋을 새로 컴파일하여 공개 alpha.4 DLL·ZIP 해시를 재현하고, 정식 후보와 설정 바이트 및 전체 런타임 인벤토리를 비교합니다.

`tools/ReleaseAudit/`로 타입 91개·메서드 525개(본문 496개)·메타데이터/IL 1,313항목·Harmony 속성 61개·P/Invoke 3개의 일치와 변조 8종 거부를 확인했습니다. 리소스 7개와 설정은 바이트 일치하며 하나의 리소스는 고정 전후 SHA와 정확한 주석 세 줄 교체만 허용합니다. 이 근거를 `release-verification.json`·`runtime-equivalence.json`에 기록하며 정식 DLL의 직접 Windows 실행으로 확대하지 않습니다.

기본 빌드는 `python3 build/build-source.py --references '<읽기 전용 참조 루트>'`이며 Release 사전 검증은 `python3 build/verify-release.py --references '<읽기 전용 참조 루트>' --output-dir artifacts/<새 검증 폴더> --version v1.0.3`입니다. `build/release-policy.json`에서 기본 소스 빌드와 확인 대상 파일·근거를 관리합니다. 최종 태그·게시·공개 다운로드·이슈 #3 종료 결과는 [배포 기록](ISSUE-3-RELEASE.md)에 기록했습니다.

### 정식 v1.0.3 최종 빌드·게시 — 2026-10-19

태그 커밋 `ced38df9f0560a212559e34fd281fddbd2581ff4`의 입력 102개를 새 소스 스냅샷으로 고정하고 기존 `DiceMaster.dll` 0개에서 새로 컴파일했습니다. 일곱 검사 묶음, alpha.3 기준 계약 2,750개·변조 12종 거부·번역/주사위 4,341개, alpha.4 대비 타입 91개·메서드 525개(본문 496개)·런타임 비교 1,313항목과 변조 8종 거부를 모두 통과했습니다. 이 빌드는 사용자가 확인한 alpha.4 DLL·ZIP도 고정 소스 커밋에서 새로 재현하여 알려진 공개 해시와 대조했습니다.

| 최종 산출물 | 식별값 |
|---|---|
| DLL | 461,312바이트 / SHA-256 `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1` |
| [DiceMaster-v1.0.3.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3/DiceMaster-v1.0.3.zip) | 191,442바이트 / SHA-256 `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7` |
| [Release](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3) | 2026-10-19 00:10:53 KST, ID `416518288`, `draft=false`, `prerelease=false`, Latest `v1.0.3` |
| 설치 asset | ID `653759221` |
| 익명 공개 검증 | API·ZIP CRC·정확히 두 파일·빌드 DLL/커밋 설정 바이트·DLL/ZIP SHA-256 일치 |
| PRD #3 | 완료 조건 10개 확인, 2026-10-19 00:11:02 KST에 `completed`로 종료 |

영수증은 `artifacts/releases/v1.0.3/`의 `release-verification.json`, `build-manifest.json`, `compatibility.json`, `runtime-equivalence.json`, `source-provenance.json`, `github-publication.json`, `anonymous-verification.json`, `issue-closure.json`에 있습니다. 기존 일곱 Release·태그·설치 자산을 보존했고 이슈 #1 closed·#2 open 상태는 변경하지 않았습니다. 이 결과를 기록하는 후속 문서 커밋은 정식 소스 태그와 설치 자산을 바꾸지 않습니다.

기존 패치 공정과 배포 DLL은 고정 Git 커밋·기존 게시 자산으로 보존합니다. 비교·복구가 필요하면 `build/baseline.py`로 같은 alpha.3 기준을 재생성하고 별도 후보 경로를 사용합니다. 게임 종료 후 보관 DLL만 복원하면 설정·세이브를 유지하는 기존 배포 구조를 따릅니다. 더 오래된 v1.0.2로 복원하면 선택지 결과 소실 문제가 다시 나타날 수 있고 v1.0.1에는 자동 갱신이 없습니다. 에이전트는 이 작업에서 Windows 설치·게임 실행을 수행하거나 요청하지 않습니다.
