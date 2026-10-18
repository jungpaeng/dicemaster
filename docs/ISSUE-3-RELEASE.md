# 전체 소스 빌드 alpha.4 prerelease 기록

[소스 빌드·비교 계약](SOURCE-BUILD.md) · [Release·전환 조건](RELEASING.md) · [변경 이력](../CHANGELOG.md)

## 승인과 현재 상태

사용자가 현재 C# 프로젝트 구조 변경의 commit·push와 이 프로젝트에서 빌드한 DLL의 prerelease 게시를 승인했습니다. 배포 태그는 `v1.0.3-alpha.4`, 설치 자산은 `DiceMaster-v1.0.3-alpha.4.zip`입니다. 현재 이 문서는 게시 준비 기록이며 게시 완료·익명 다운로드 검증 완료를 뜻하지 않습니다. 최종 소스 커밋에서 새 빌드·자동 검사 후 게시하고 확인된 결과를 추가합니다.

기존 태그·Release·자산을 덮어쓰지 않으며 Latest 정식 `v1.0.2`를 유지합니다. alpha.4는 `draft=false`, `prerelease=true`, `make_latest=false`로 게시합니다. Windows 설치·게임 실행은 수행하거나 요청하지 않습니다. 기본 빌드 전환·PRD #3 전체 완료·이슈 종료·정식 Release는 보류합니다. 이슈 #2의 기능 확장은 포함하지 않습니다.

## 태그와 설치 자산의 대응

복원·비교 기준은 `v1.0.3-alpha.3`의 커밋 `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec`, DLL SHA-256 `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633`으로 고정합니다.

alpha.4의 DLL은 alpha.4 태그 커밋에 있는 `src/DiceMaster/DiceMaster.csproj`의 전체 C# 소스·모드 리소스에서 컴파일합니다. 기존 `DiceMaster.dll`을 일반 빌드 입력으로 사용하지 않습니다. 같은 태그 커밋의 `packaging/config/DiceMaster.cfg`와 함께 다음 두 경로만 설치 ZIP에 포함합니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

태그 커밋의 루트 `plugins/DiceMaster/DiceMaster.dll`은 alpha.3 기준 DLL로 보존합니다. 따라서 alpha.4 설치 자산의 DLL과 루트 보존 DLL은 서로 다르며, ZIP이 루트 DLL과 같다는 검사를 적용하지 않습니다. 대신 태그 커밋의 프로젝트 빌드 결과와 자산 DLL 바이트 일치, 같은 커밋의 패키지 설정 바이트 일치, ZIP CRC·파일 수·경로·SHA-256을 확인합니다. GitHub의 자동 `Source code` 압축 파일에는 저장소의 보존 DLL도 포함되므로 설치용 Assets ZIP과 구분합니다.

Assembly/FileVersion `1.0.0.0`, InformationalVersion `1.0.0`, BepInPlugin 버전 `1.0.0`은 기준의 메타데이터 계약을 보존합니다. 배포 태그 alpha.4와 내부 버전은 별도이며 실제 산출물은 DLL SHA-256으로 식별합니다. 게임 DLL·게임 소스·외부 참조·DebugMenu·개발 도구는 설치 ZIP에 포함하지 않습니다.

## 재현 명령과 검사

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

최종 DLL에는 일곱 검사 묶음, alpha.3 대비 동작·메타데이터·Harmony 후킹·native ABI·리소스 비교와 변조 후보 10종 거부를 적용합니다. 기존 모드 DLL이 없는 격리 빌드와 실패 경로 11종도 확인합니다. 재컴파일한 DLL 전체의 기준 바이트 일치는 요구하지 않습니다. 구조 정리 직후 확보한 실행 수치와 후보 해시는 [당시 검증 기록](SOURCE-BUILD.md#구조-정리-후-로컬-검증-기록--2026-10-18)이며 최종 게시 결과를 대신하지 않습니다.

## 게시 후 기록할 증거

최종 소스 커밋·태그 대상, DLL·ZIP 크기와 SHA-256, 빌드 입력 해시, 자동 검사 결과, GitHub Release·asset ID와 게시 시각, 익명 공개 API·다운로드의 CRC·두 파일·DLL/설정 바이트 일치 결과를 기록합니다. 게시 기록 문서 후속 커밋은 소스 태그와 자산을 바꾸지 않습니다.

로컬 영수증 위치는 `artifacts/releases/v1.0.3-alpha.4/`입니다. `source-provenance.json`으로 고정 소스 커밋에서 만든 새 소스 스냅샷과 일반 빌드 시작 시 기존 모드 DLL 0개를 기록합니다. `build-manifest.json`, `compatibility.json`/`.log`, `commit-build.log`는 빌드·검사를, `release-manifest.json`, `github-publication.json`, `anonymous-verification.json`은 자산·게시·공개 다운로드를 추적합니다. 이 생성 파일은 Git에서 제외하며 이 문서에 핵심 결과를 남깁니다.

native Unity·실제 리소스 로드·Harmony 설치·다른 모드·Windows UI·XInput·사용자 설정 유지 업데이트/롤백·실제 게임 성능은 미검증입니다. 별도 승인 후 같은 DLL 식별값으로 사용자 Windows 확인을 마쳐야 기본 빌드·배포 전환을 검토할 수 있습니다. alpha.3의 다음 성공 Lua까지 대기하는 알려진 제한과 이슈 #1 미완료 조건도 유지합니다.
