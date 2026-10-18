# 전체 소스 빌드 전환과 Release 기록

[소스 빌드·비교 계약](SOURCE-BUILD.md) · [Release·전환 조건](RELEASING.md) · [변경 이력](../CHANGELOG.md)

## v1.0.3 정식 전환 근거 — 2026-10-19

사용자는 PRD #3가 완료되면 정식 Release를 발행하고 이슈 #3을 닫으라고 지시했으며, 뒤이어 **사용자 Windows 검증을 완료했고 alpha.4를 직접 내려받아 확인했다**고 답했습니다. 확인 대상은 공개 `DiceMaster-v1.0.3-alpha.4.zip`의 DLL SHA-256 `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1`입니다. 사용자 확인 날짜는 2026-10-19 KST이며 실제 실행 날짜·게임/BepInEx 버전·패드/Steam Input 환경·항목별 로그는 미제공입니다. 과거 읽기 전용 참조 자료의 버전 정보를 이 Windows 실행 환경으로 대입하지 않습니다.

**[정식 v1.0.3](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3)을 2026-10-19 00:10:53 KST에 게시했고 [이슈 #3](https://github.com/jungpaeng/dicemaster/issues/3)을 00:11:02 KST에 `completed`로 종료했습니다.** 정식 태그의 소스 커밋 `ced38df9f0560a212559e34fd281fddbd2581ff4`에서 전체 프로젝트를 새로 빌드하고 검사했습니다. alpha.4 이후 제품 C# 34개, 설정과 번역 데이터는 유지했고 번역 리소스의 주석 세 줄만 정리했습니다. 이 때문에 정식 DLL의 전체 바이트와 해시는 alpha.4와 다릅니다. 사용자 확인 대상과 정식 DLL을 연결하는 아래 정적 동등성 비교를 정식 DLL의 직접 Windows 실행으로 표현하지 않습니다.

기본 개발·배포를 전체 소스 빌드로 전환했으며 `build/release-policy.json`에 전환 상태와 검증 출처를 기록합니다. 기존 패치 명령은 별도 비교·복구 도구로 남기고, alpha.3 기준은 고정 Git 커밋에서 해시를 검증해 재생성합니다. 고정 커밋 사전 검증과 게시 후 익명 공개 다운로드 검증을 통과한 뒤 수용 기준 10개를 확인해 이슈 #3을 닫았습니다. 기존 일곱 태그·Release·자산은 보존했고 이슈 #1 `closed`·#2 `open` 상태는 변경하지 않았습니다. 이 게시 결과를 기록하는 후속 문서 커밋은 아래 소스 태그·설치 자산을 변경하지 않습니다.

### 정식 게시 식별과 최종 검증

| 항목 | 확인한 값 |
|---|---|
| 태그·소스 커밋 | `v1.0.3` / `ced38df9f0560a212559e34fd281fddbd2581ff4` |
| GitHub 상태 | Release ID `416518288`, `draft=false`·`prerelease=false`, Latest 정식 `v1.0.3` |
| 게시 시각 | 2026-10-18 15:10:53 UTC / 2026-10-19 00:10:53 KST |
| 설치 자산 | [DiceMaster-v1.0.3.zip](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3/DiceMaster-v1.0.3.zip), asset ID `653759221` |
| ZIP 크기·SHA-256 | 191,442바이트 / `72c14f2743d0d65f668825ceec9d557e31d48302730f5afaa293c781190829a7` |
| DLL 크기·SHA-256 | 461,312바이트 / `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1` |
| 설정 크기·SHA-256 | 9,759바이트 / `8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e` |
| 이슈 #3 종료 | 2026-10-18 15:11:02 UTC / 2026-10-19 00:11:02 KST, `state=closed`·`state_reason=completed`, 수용 기준 10개 확인 |

정식 커밋의 관리 빌드 입력 102개를 새 스냅샷으로 추출했으며 시작 시 기존 `DiceMaster.dll`은 0개였습니다. 일곱 자동 검사, alpha.3 기준 계약 2,750개·변조 거부 12종·번역/주사위 관찰값 4,341개가 통과했습니다. 사용자 확인 대상인 공개 alpha.4의 DLL·설정·ZIP 해시를 고정 소스 커밋 재빌드로 재현한 뒤 정식 DLL과 비교했습니다. 컴파일러 생성 항목을 포함한 91개 타입·525개 메서드·496개 IL 본문을 포함해 메타데이터/IL 1,313개 항목이 일치했고, 7개 리소스는 바이트가 같으며 나머지 번역 리소스는 검토한 주석 세 줄 이외 모든 바이트가 같습니다. 설정도 사용자 확인 alpha.4 ZIP과 같습니다. 관리 IL·Harmony 속성·필드 레이아웃·native 진입점·어셈블리 참조·native 리소스·번역 데이터·허용하지 않은 주석 변조 8종은 각각 거부했습니다. 이 비교는 MVID·PE 위치/시간·메타데이터 토큰/RVA/힙 위치·디버그 컨테이너를 비교 대상에서 제외하며 동작에 쓰는 사용자 속성과 메타데이터는 비교합니다.

공개 API와 인증 없는 실제 ZIP 다운로드에서 상태·자산 ID·크기·SHA-256, ZIP CRC·정확히 두 파일, 고정 커밋에서 새로 빌드한 DLL과 패키지 설정 바이트를 확인했습니다. 이전 일곱 Release의 ID·자산·바이트를 보존했습니다. `artifacts/releases/v1.0.3/release-verification.json`은 게시 전 고정 커밋 검사 결과이므로 `published=false`를 유지하며, 실제 게시·익명 검증·이슈 종료는 각각 같은 폴더의 `github-publication.json`·`anonymous-verification.json`·`issue-closure.json`에 기록합니다.

### PRD #3 수용 기준과 증거

아래 번호는 [PRD #3의 Acceptance Criteria](https://github.com/jungpaeng/dicemaster/issues/3) 순서입니다. 자동 검사에서 사용하는 native/Unity 경계 대역, 사용자 확인, 별도 미제공 환경·측정 기록을 구분합니다.

| 번호 | 수용 기준 | 구현·검증 근거 |
|---|---|---|
| 1 | alpha.3 기준과 전체 C#·모드 리소스 관리 | 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`, 기준 DLL `485581c2…`, `src/DiceMaster/` C# 34개·리소스 8개·`recovery-manifest.json`. 고정 DLL 새 추출의 `recover-source.py --check` 44개 파일 일치 |
| 2 | 깨끗한 checkout에서 기존 DLL 없이 전체 빌드·설치 파일 생성 | `build/verify-release.py --source-commit <전체 SHA>`의 허용 목록 스냅샷은 기존 `DiceMaster.dll` 0개, `.git/`·옛 출력·외부 게임 자료 제외. 새 DLL과 정확히 두 파일 ZIP 생성 |
| 3 | 정상 빌드의 추출·병합·IL 교체 의존성 제거와 중복 방지 | `src/DiceMaster/DiceMaster.csproj`가 26개 복원 소스와 8개 Gamepad/LiveLog 관리 소스를 한 번만 컴파일. 비교기의 타입·메서드·Harmony 수와 변조 거부 검사로 중복 타입·후킹 검출 |
| 4 | 설정·식별·후킹/리플렉션·native ABI·리소스 계약 | `SourceBuildContractTests`와 `compatibility.json`의 alpha.3 계약 2,750개, 변조 거부 12종. 12개 소스 표현 차이와 번역 주석 세 줄의 의도한 정리는 [비교 계약](SOURCE-BUILD.md#검사-계약과-한계)에 명시 |
| 5 | 실제 기능 검사를 새 빌드에 연결하고 변경 근거 기록 | 일곱 자동 검사: 로그 19, 엔진 16개 시나리오·70개 단언, 수명주기 4,023, 실제 파서/표시 통합 134, 게임 API 동작 3,914·메타데이터 1,011, 패드 262,715, 번역/주사위 2,170. 기준/후보 관찰값 4,341개 비교. 기존 IL 지문 검사와 동작 계약의 차이를 문서화 |
| 6 | 참조·컴파일·검사·리소스 오류의 명확한 실패와 오래된 산출물 방지 | `verify-source-failures.py` 11종, 기준 재생성 13종, Release 출력 보호 4종 및 비교기 변조 거부 12종. 기존 성공 후보 보존·새 ZIP 미생성·오래된 PASS 제거 확인 |
| 7 | 게임 DLL/소스 제외와 두 파일 설치 ZIP·새 DLL 일치 | 소스 허용 목록과 명시적 참조 15개 계약, ZIP CRC·파일 수·경로·설정/빌드 DLL 바이트·SHA-256 검사. 허용한 내장 MinHook 두 DLL 외 게임 DLL·게임 소스는 포함하지 않음 |
| 8 | 사용자 Windows에서 기존 동작·설치·설정·native/Harmony/UI/패드 확인과 식별 기록 | 2026-10-19 사용자 본인의 포괄적 Windows 검증 완료 확인, 직접 다운로드한 공개 alpha.4와 위 DLL 식별. 게임/BepInEx·패드 환경·세부 경로별 로그는 미제공으로 명시. 정식 후보는 alpha.4 대비 별도 동등성 비교로 연결하며 직접 Windows 실행을 주장하지 않음 |
| 9 | 조건 충족 후 기본 빌드 전환·문서·복구 절차 갱신 | `build/release-policy.json`, README·개발·소스 빌드·Release·변경 이력의 기본 경로를 소스 빌드로 전환. [Release·복구 안내](RELEASING.md)에 이전 공개 자산 또는 고정 Git 기준 DLL을 이용한 복구와 설정/세이브 유지, 실제 롤백 기록 미제공을 구분 |
| 10 | #2 이전 빌드 기반 전환과 기존 이슈/미확인 범위 분리 | #2 효과 분석 확장은 구현하지 않음. alpha.3의 알려진 대기 제한·독립 난수 설명·#1의 별도 수용 기준을 유지. 다른 환경 호환성·실제 게임 성능·개별 Windows 실행 로그를 새로 검증했다고 표현하지 않음 |

### 정식 자산 재현 절차

정식 준비 입력의 새 검증에서 alpha.4 고정 커밋의 DLL·ZIP 공개 해시 재현과 설정 바이트 일치를 확인했습니다. 전체 타입 91개·메서드 525개(본문 496개)·메타데이터/IL 1,313항목·Harmony 속성 61개·P/Invoke 3개가 동일하며 변조 8종을 거부했습니다. 리소스 7개는 바이트 일치하고 하나의 정확한 주석 세 줄 변경만 허용합니다. 새 일곱 검사·alpha.3 기준 계약 2,750개/변조 12종/동작 4,341개·Git 없는 격리 빌드·실패 경로 11종도 통과했습니다. 같은 검사를 고정 커밋으로 다시 수행하여 최종 영수증을 게시 결과와 연결했습니다.

```sh
python3 build/verify-release.py \
  --references '<읽기 전용 참조 루트>' \
  --source-commit ced38df9f0560a212559e34fd281fddbd2581ff4 \
  --version v1.0.3 \
  --output-dir artifacts/reproduce-v1.0.3
```

출력 폴더는 비어 있거나 없어야 합니다. `release-verification.json`의 커밋·입력 해시·SDK·참조·DLL·ZIP·검사 결과를 위 게시 식별값과 대조합니다. 게시 영수증이 보존된 `artifacts/releases/v1.0.3/`를 재현 출력으로 덮어쓰지 않습니다. 이 로컬 검증 명령은 새 태그·게시·이슈 종료를 실행하지 않습니다.

## alpha.4 prerelease 당시 승인과 상태

사용자 승인에 따라 C# 프로젝트 구조 변경을 commit·push하고 **[v1.0.3-alpha.4 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)를 2026-10-18 23:42:14 KST에 게시했습니다.** 고정 태그 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`에서 프로젝트를 다시 빌드·자동 검사했습니다. 설치 자산은 `DiceMaster-v1.0.3-alpha.4.zip`이며 익명 공개 API와 실제 자산 다운로드 검증을 마쳤습니다. 이 문서는 확인된 게시 결과를 기록하는 후속 문서 변경이며 소스 태그·자산을 바꾸지 않습니다.

기존 여섯 태그·Release·자산을 덮어쓰지 않았으며 당시 Latest 정식 `v1.0.2` (Release ID `416349430`)를 유지했습니다. alpha.4는 `draft=false`, `prerelease=true`, `make_latest=false`로 게시했습니다. 이 게시 당시에는 Windows 설치·게임 실행을 수행하거나 요청하지 않았고 기본 빌드 전환·PRD #3 전체 완료·이슈 종료·정식 Release를 보류했습니다. 이슈 #2의 기능 확장은 포함하지 않았습니다.

## 태그와 설치 자산의 대응

복원·비교 기준은 `v1.0.3-alpha.3`의 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`, DLL SHA-256 `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633`으로 고정합니다.

alpha.4의 DLL은 alpha.4 태그 커밋에 있는 `src/DiceMaster/DiceMaster.csproj`의 전체 C# 소스·모드 리소스에서 컴파일합니다. 기존 `DiceMaster.dll`을 일반 빌드 입력으로 사용하지 않습니다. 같은 태그 커밋의 `packaging/config/DiceMaster.cfg`와 함께 다음 두 경로만 설치 ZIP에 포함합니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

태그 커밋의 루트 `plugins/DiceMaster/DiceMaster.dll`은 alpha.3 기준 DLL로 보존합니다. 따라서 alpha.4 설치 자산의 DLL과 루트 보존 DLL은 서로 다르며, ZIP이 루트 DLL과 같다는 검사를 적용하지 않습니다. 대신 태그 커밋의 프로젝트 빌드 결과와 자산 DLL 바이트 일치, 같은 커밋의 패키지 설정 바이트 일치, ZIP CRC·파일 수·경로·SHA-256을 확인합니다. GitHub의 자동 `Source code` 압축 파일에는 저장소의 보존 DLL도 포함되므로 설치용 Assets ZIP과 구분합니다.

Assembly/FileVersion `1.0.0.0`, InformationalVersion `1.0.0`, BepInPlugin 버전 `1.0.0`은 기준의 메타데이터 계약을 보존합니다. 배포 태그 alpha.4와 내부 버전은 별도이며 실제 산출물은 DLL SHA-256으로 식별합니다. 게임 DLL·게임 소스·외부 참조·다른 모드·개발 도구는 설치 ZIP에 포함하지 않습니다.

## 재현 명령과 검사

아래 명령과 루트 DLL 경로는 고정 alpha.4 태그의 당시 구조를 재현하는 기록입니다. 현재 작업 트리는 루트 `config/`·`plugins/`를 제거했고 기준 DLL은 고정 alpha.3 Git 커밋에서 자동 재생성합니다. 현재 소스로 Release 후보를 검사하는 명령은 [소스 빌드 안내](SOURCE-BUILD.md#release-후보의-로컬-검증)를 따릅니다. 기존 alpha.4 태그·자산은 변경하지 않습니다.

Python 3와 `global.json`에 명시한 .NET SDK `8.0.426`을 준비합니다. 태그 커밋의 저장소 루트에서 다음 명령을 실행합니다. 읽기 전용 게임 참조는 저장소 밖에 별도로 준비하며 정확한 파일·버전·해시는 `build/reference-contract.json`으로 검증합니다.

```sh
python3 build/build-source.py \
  --references '<읽기 전용 참조 루트>' \
  --output-dir artifacts/reproduction-alpha4
python3 build/verify-source.py \
  --references '<읽기 전용 참조 루트>' \
  --baseline plugins/DiceMaster/DiceMaster.dll \
  --candidate artifacts/reproduction-alpha4/DiceMaster.dll
```

재현 출력은 비어 있거나 빌드 도구가 관리하는 후보 폴더를 사용합니다. 최종 게시 영수증이 추가된 `artifacts/releases/v1.0.3-alpha.4/`를 재현 출력으로 덮어쓰지 않습니다.

빌드가 생성하는 `DiceMaster-source-candidate.zip`을 바이트 변경 없이 배포 자산명 `DiceMaster-v1.0.3-alpha.4.zip`으로 복사합니다. 일반 빌드는 기존 모드 DLL을 읽지 않으며 `verify-source.py`만 고정 기준과의 계약 비교에 이를 사용합니다. 다른 출력 디렉터리와 보고서 경로 등 전체 명령은 [소스 빌드 안내](SOURCE-BUILD.md)를 따릅니다.

최종 소스 커밋을 `git archive`로 추출하고 소스·리소스·빌드/검사·패키지 설정의 허용 목록만 새 스냅샷에 복사했습니다. 기존 모드 DLL·루트 `plugins/`·기존 산출물이 없는 상태에서 시작 시 `DiceMaster.dll` 0개를 확인했습니다. 새 빌드의 입력 해시가 커밋과 같고 실행 중 바뀌지 않음을 검증했습니다. SDK는 `8.0.426`입니다. 구조 정리 단계의 기존 모드 DLL 없는 격리 빌드·실패 경로 11종 기록은 [당시 검증 기록](SOURCE-BUILD.md#구조-정리-후-로컬-검증-기록--2026-10-18)에 보존하며 아래는 최종 커밋에서 다시 실행한 결과입니다.

| 최종 소스 커밋의 새 DLL 검사 | 결과 |
|---|---|
| `LogRefreshTests` | 19개 통과 |
| `LiveLogEngineTests` | 16개 시나리오·70개 단언 통과 |
| `LifecycleTests` | 4,023개 통과 |
| `LiveLogIntegrationTests` | 실제 파서·캐시·표시·요약·후킹 연결 134개 통과 |
| `RealGameApiTests` | 관리 동작 3,914개·메타데이터 1,011개 통과 |
| `GamepadTests` | 전체 입력 조합·signed int 경계·ABI 262,715개 통과 |
| `SourceBehaviorTests` | 실제 번역/주사위 2,170개 통과 |
| alpha.3 기준/후보 계약 | 메타데이터·Harmony 후킹·native ABI·내장 리소스 2,748개 통과 |
| 비교 검사 변조 후보 | 10종을 각각 기대한 계약 오류로 거부 |
| 기준/후보 번역·주사위 관찰값 비교 | 4,341개 통과 |

재컴파일한 DLL 전체의 기준 바이트 일치는 요구하지 않습니다. 소스 표현에 따른 메타데이터 차이 12건은 [소스 빌드 계약](SOURCE-BUILD.md#검사-계약과-한계)에 기록한 범위로 유지합니다. Unity·로더·입력 경계 대역을 사용하는 관리 검사는 Windows 게임 런타임 확인과 구분합니다.

## 게시 결과와 공개 다운로드 검증

| 항목 | 확인한 값 |
|---|---|
| 소스 태그 | `v1.0.3-alpha.4` |
| 소스 커밋 | `46bbf7486ab424696f1f5ba7d4126f6c12d11184` |
| GitHub 상태 | Release ID `416503505`, `draft=false`, `prerelease=true`; `make_latest=false`로 게시 |
| 게시 시각 | 2026-10-18 14:42:14 UTC / 23:42:14 KST |
| 자산 | `DiceMaster-v1.0.3-alpha.4.zip`, asset ID `653700999` |
| ZIP 크기·SHA-256 | 191,482바이트 / `ddd32abbaa886cea4e0975bd522addbb1339e71a59223137fbbe23114d9556b8` |
| DLL 크기·SHA-256 | 461,312바이트 / `24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1` |
| 패키지 설정 SHA-256 | `8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e` |
| 보존한 루트 배포 DLL SHA-256 | `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633` |
| Latest 정식 | `v1.0.2`, Release ID `416349430` 유지 |

익명 공개 API로 게시 상태·자산 ID·크기·digest를 확인했습니다. [공개 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.3-alpha.4/DiceMaster-v1.0.3-alpha.4.zip)을 인증 없이 내려받아 정확히 두 파일과 경로·CRC 정상, 로컬 게시 ZIP SHA-256, 새 빌드 DLL 바이트, 태그 소스 커밋의 `packaging/config/DiceMaster.cfg` 바이트 일치를 확인했습니다. 이전 `v1.0.3-alpha.3`, `alpha.2`, `alpha.1`, `v1.0.2`, `v1.0.1`, `v1.0.0`의 Release·자산 ID와 digest는 유지됐습니다. 게시 기록 문서 후속 커밋은 위 소스 태그·자산을 바꾸지 않습니다.

로컬 영수증 위치는 `artifacts/releases/v1.0.3-alpha.4/`입니다. `source-provenance.json`으로 고정 소스 커밋에서 만든 새 소스 스냅샷과 일반 빌드 시작 시 기존 모드 DLL 0개를 기록합니다. `build-manifest.json`, `compatibility.json`/`.log`, `commit-build.log`는 빌드·검사를, `release-manifest.json`, `github-publication.json`, `anonymous-verification.json`은 자산·게시·공개 다운로드를 추적합니다. 이 생성 파일은 Git에서 제외하며 이 문서에 핵심 결과를 남깁니다.

alpha.4 게시 당시에는 native Unity·실제 리소스 로드·Harmony 설치·다른 모드·Windows UI·XInput·사용자 설정 유지 업데이트/롤백·실제 게임 성능을 Windows에서 검증하지 않았습니다. 이후 사용자 본인이 공개 alpha.4의 Windows 검증 완료를 확인한 내용과 미제공 환경·실행 기록은 이 문서의 정식 전환 근거로 분리합니다. alpha.3의 다음 성공 Lua까지 대기하는 알려진 제한과 이슈 #1의 별도 수용 기준은 유지합니다.
