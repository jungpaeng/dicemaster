# 전체 소스 빌드 alpha.4 prerelease 기록

[소스 빌드·비교 계약](SOURCE-BUILD.md) · [Release·전환 조건](RELEASING.md) · [변경 이력](../CHANGELOG.md)

## 승인과 현재 상태

사용자 승인에 따라 C# 프로젝트 구조 변경을 commit·push하고 **[v1.0.3-alpha.4 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.4)를 2026-10-18 23:42:14 KST에 게시했습니다.** 고정 태그 커밋 `46bbf7486ab424696f1f5ba7d4126f6c12d11184`에서 프로젝트를 다시 빌드·자동 검사했습니다. 설치 자산은 `DiceMaster-v1.0.3-alpha.4.zip`이며 익명 공개 API와 실제 자산 다운로드 검증을 마쳤습니다. 이 문서는 확인된 게시 결과를 기록하는 후속 문서 변경이며 소스 태그·자산을 바꾸지 않습니다.

기존 여섯 태그·Release·자산을 덮어쓰지 않았으며 Latest 정식 `v1.0.2` (Release ID `416349430`)를 유지했습니다. alpha.4는 `draft=false`, `prerelease=true`, `make_latest=false`로 게시했습니다. Windows 설치·게임 실행은 수행하거나 요청하지 않습니다. 기본 빌드 전환·PRD #3 전체 완료·이슈 종료·정식 Release는 보류합니다. 이슈 #2의 기능 확장은 포함하지 않습니다.

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

native Unity·실제 리소스 로드·Harmony 설치·다른 모드·Windows UI·XInput·사용자 설정 유지 업데이트/롤백·실제 게임 성능은 미검증입니다. 별도 승인 후 같은 DLL 식별값으로 사용자 Windows 확인을 마쳐야 기본 빌드·배포 전환을 검토할 수 있습니다. alpha.3의 다음 성공 Lua까지 대기하는 알려진 제한과 이슈 #1 미완료 조건도 유지합니다.
