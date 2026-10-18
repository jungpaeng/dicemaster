# 전체 소스 빌드 후보 — 이슈 #3

[개발 안내](DEVELOPMENT.md) · [Release·전환 조건](RELEASING.md) · [PRD #3](https://github.com/jungpaeng/dicemaster/issues/3)

현재 상태는 별도 로컬 후보입니다. 기존 기본 패치 빌드와 `plugins/DiceMaster/DiceMaster.dll`을 보존합니다. 자동 검사와 사용자 Windows 확인이 모두 완료되기 전에는 기본 빌드 전환·이슈 #3 완료로 처리하지 않습니다. 이번 작업은 로컬 소스 복원·빌드·자동 검사·문서 갱신이며 Windows 설치·게임 실행을 수행하거나 요청하지 않습니다. 최초 요청 이후 별도 사용자 지시로 이번 소스 변경의 commit·push를 승인받았습니다. 태그·Prerelease/Release 게시·이슈 종료는 승인 범위에 포함하지 않습니다.

## 고정 기준과 시작 상태

| 항목 | 기준 |
|---|---|
| 복원·비교 버전 | `v1.0.3-alpha.3` |
| 기준 태그 커밋 | `f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec` |
| 기준 DLL | `plugins/DiceMaster/DiceMaster.dll`, 462,336바이트 |
| 기준 DLL SHA-256 | `485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633` |
| 작업 시작 HEAD | `9ca7730` (`docs: record verified v1.0.3-alpha.3 publication`) |
| HEAD와 기준 차이 | 9개 문서만 변경, 34줄 추가·22줄 삭제. 기준 DLL·C# 관리 소스·도구·설정은 동일 |
| 시작 시 기존 미추적 파일 | `.agents/`, `docs/ISSUE-2-START-PROMPT.md`, `docs/ISSUE-2-TECHNICAL-HANDOFF.md`, `skills-lock.json` — 그대로 보존 |

시작 시 HEAD·작업 트리·기준 태그와 실제 DLL 해시를 대조했습니다. HEAD의 게시 확인 문서 변경을 기준 모드 변경으로 취급하지 않았으며 기존 변경사항을 되돌리거나 정리하지 않았습니다. 과거 임시 추출본 대신 고정 해시를 확인한 기준 DLL에서 새로 복원합니다. 원 작성자의 프로젝트 구조·주석 원문까지 복구했다는 뜻은 아닙니다.

## 소스와 리소스의 소유권

`development/src/DiceMaster/`는 선택지 파서·번역·UI·주사위·기존 후킹과 어셈블리 속성을 포함한 전체 모드 프로젝트입니다. 복원 C# 26개와 모드 리소스 5개를 보관합니다. 기존 `development/src/DiceMaster.Gamepad/`와 `development/src/DiceMaster.LiveLog/`의 C# 8개(Gamepad 2개, LiveLog 6개)를 링크하여 같은 어셈블리에 한 번만 컴파일합니다. 기준 DLL에서 추출한 동일 Gamepad·LiveLog 타입은 제외합니다. 관리 중인 소스가 이후 수정의 기준이며 추출본을 별도 실행 구현으로 병행하지 않습니다.

UI/번역 리소스 5개는 원래 논리 이름으로 복원합니다. MinHook x86/x64 DLL과 라이선스 3개는 기존 `DiceMaster.Gamepad/native/` 파일을 바이트 일치 확인 후 링크합니다. 모든 8개 내장 리소스의 이름·속성·크기·SHA-256을 기준과 비교합니다. 기준의 에셋 리소스는 `Public`, 나머지 7개는 `Private`이며 이 속성을 그대로 유지합니다. 게임 소스는 복원하지 않으며 게임 DLL은 저장소에 복사하거나 내장하지 않습니다.

기준에서 복원한 모드 리소스의 식별은 다음과 같습니다. 비교 도구는 이 파일 목록만 보는 대신 기준 DLL과 후보 DLL에서 실제 내장된 데이터를 읽어 이름·속성·바이트를 비교합니다.

| 내장 논리 이름 | 바이트 | SHA-256 |
|---|---:|---|
| `DiceMaster.Assets.mortaldice` | 7,066 | `5cb5c10adea77234bbef4f8f348514526c9765e9c45fe2b4e8d3c334ecfc0ff6` |
| `DiceMaster.Gamepad.Native.LICENSE.txt` | 4,365 | `d5c2224982b0b95a16b098a561335fa93f3eaf4d0aa964b7843edb986df78dc8` |
| `DiceMaster.Gamepad.Native.MinHook.x64.dll` | 16,384 | `033e55af8995c3b260e56de73daf99ff4a7ce2fcbe4b3872db2270678d8c6585` |
| `DiceMaster.Gamepad.Native.MinHook.x86.dll` | 12,800 | `1f28d13b8133551c8896cc556db768c94ebfaae0ad38e86719f3151f7b4ce1fc` |
| `DiceMaster.Translations.ModTranslation.txt` | 44,349 | `6fc71ac1780be28b78e5ee193835a38e47d1526cbb9a143d05ff5206eceef1ec` |
| `DiceMaster.Translations.Pairs` | 147,030 | `172489a3a34fd27d93410b71b23dcfeb6d597723e5d2e0f4878eef464899ec00` |
| `DiceMaster.Translations.TriggerTranslation1.0.5v.txt` | 8,521 | `a528b4740da8fe1fd5218e50d8113720306ea1db21b959b0ab1545c13587dbd4` |
| `DiceMaster.Translations._Substitutions.txt` | 2,514 | `640d5fc2a5fae476b766c5b59481a10e7e7d68a605557485f5bf2277e6569909` |

어셈블리 이름 `DiceMaster`, Assembly/FileVersion `1.0.0.0`, InformationalVersion `1.0.0`, BepInPlugin GUID/이름 `DiceMaster`와 버전 `1.0.0`은 기준과 동일합니다. 배포 태그 alpha.3과 어셈블리 내부 버전은 별도 식별이며 이번 후보에 새 배포 버전을 부여하지 않습니다.

기존 UI·한국어·네 탭·50자 선택지 요약/로그 전체 설명·설정 키와 기본값·세이브 형식·주사위/패드·로그/난수·수명주기 계약을 유지합니다. 이슈 #2 효과 분석 확대나 버그 수정은 포함하지 않습니다. 제자리/무동작/실패 로드 후 다음 성공 Lua까지 대기할 수 있는 alpha.3의 제한과 이슈 #1의 미완료 수용 기준은 그대로 남습니다.

## 준비 환경과 읽기 전용 외부 참조

Python 3.9 이상(이번 실행 `3.9.6`), .NET 8.x SDK와 최초 NuGet 패키지 복원이 필요합니다. 이번 실행에 사용한 SDK는 `8.0.426`입니다. 모드는 .NET Framework 4.8로 컴파일하며 `Microsoft.NETFramework.ReferenceAssemblies.net48` 1.0.3을 사용합니다. 검사·호환성 도구는 .NET 8에서 Mono.Cecil 0.11.6으로 메타데이터를 읽습니다. 빌드 도구는 `NuGetAudit=false`로 네트워크 감사 조회를 제외해 오프라인 반복 실행을 가능하게 합니다. 패키지 보안 감사 통과를 뜻하지 않습니다. 소스 일반 빌드에는 ILSpy나 예전 임시 폴더가 필요하지 않습니다. 표준 SDK 설치는 `dotnet`을 PATH에 두면 됩니다. 이번 로컬 실행은 이미 준비한 SDK·패키지 캐시를 다음과 같이 지정했습니다.

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

루트 아래 `Mortal_Data/Managed/`와 `BepInEx/core/`의 게임·Unity·로더 어셈블리를 사용합니다. 프로젝트의 참조는 `Private=false`로 설정해 빌드 출력으로 복사하지 않습니다. 참조의 경로·필요 파일 15개·해시는 `development/reference-contract.json`과 대조하며 누락·잘못된 버전은 빌드 실패입니다. 다른 게임 버전을 사용하려면 명세·API 계약을 별도 검토해야 하며 해시 검사를 우회하지 않습니다.

명세의 필수 파일은 다음과 같습니다. 파일별 크기·SHA-256은 `development/reference-contract.json`에서 확인합니다.

| 폴더 | 필수 DLL |
|---|---|
| `Mortal_Data/Managed/` | `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.AssetBundleModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.TextRenderingModule.dll`, `UnityEngine.InputLegacyModule.dll`, `Mortal.Core.dll`, `Mortal.Story.dll`, `Mortal.Free.dll`, `OBB.Framework.dll`, `Fungus.dll` |
| `BepInEx/core/` | `0Harmony.dll`, `BepInEx.Core.dll`, `BepInEx.Unity.Mono.dll` |

수신 기존 파일·로그가 식별한 환경은 Steam appid `1859910` / buildid `26021712`, Unity `2020.3.49f1`, BepInEx `6.0.0-be.692`, Harmony `2.10.1`입니다. 이 식별은 새 Windows 실행이나 호환성 통과 기록이 아닙니다. 원본 참조의 수신 모드 DLL은 복원 기준으로 사용하지 않습니다.

## 일반 빌드와 별도 비교

저장소 루트에서 읽기 전용 참조 위치를 지정합니다.

```sh
python3 development/tools/build-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

이 일반 빌드는 기존 모드 DLL을 입력으로 읽거나 추출·병합·메서드 교체하지 않습니다. `DiceMaster.csproj`가 관리 소스·모드 리소스만 컴파일하며 기본 출력은 `development/releases/source-candidate/`입니다. 기존 `plugins/DiceMaster/DiceMaster.dll`과 기본 패치 스크립트는 변경하지 않습니다. 사용자 지정 후보 위치에는 `--output-dir development/releases/<별도 후보>`를 사용합니다. 배포 DLL과 읽기 전용 참조를 보호하기 위해 출력은 `development/releases/`의 하위 폴더이며 참조와 겹치지 않는 경로로 제한합니다. `development/releases/` 자체는 출력할 수 없습니다. 이미 존재하는 대상은 유효한 소스 후보 manifest·DLL 해시가 일치하고 허용된 결과 파일만 있을 때 갱신합니다. 기존 배포 디렉터리나 알 수 없는 파일을 후보 출력으로 덮어쓰지 않습니다.

빌드는 새 임시 작업 폴더에서 컴파일하고 일곱 자동 검사(`LogRefreshTests`, `LiveLogEngineTests`, `LifecycleTests`, `LiveLogIntegrationTests`, `RealGameApiTests`, `GamepadTests`, `SourceBehaviorTests`)를 새 DLL에 실행한 뒤 후보를 생성합니다. 기본 출력은 다음과 같습니다.

```text
development/releases/source-candidate/
├── DiceMaster.dll
├── DiceMaster-source-candidate.zip
├── build-manifest.json
└── build.log
```

`build-manifest.json`에서 SDK·참조·새 DLL·ZIP 식별과 수행한 검사 결과를 확인합니다. `build_input_sha256`은 소스·리소스·설정·검사·빌드 도구의 실행 전후 해시를 기록하며 빌드 도중 입력이 바뀌면 출력을 실패시킵니다. ZIP의 `config/DiceMaster.cfg`는 저장소 설정이며 `plugins/DiceMaster/DiceMaster.dll`은 이번 새 빌드 바이트입니다. 게임 DLL·게임 소스·다른 모드·문서·개발 도구를 넣지 않습니다.

후보와 기준 DLL을 비교하는 검사는 일반 빌드와 구분합니다.

```sh
python3 development/tools/verify-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018' \
  --baseline plugins/DiceMaster/DiceMaster.dll
```

기본 후보는 `development/releases/source-candidate/DiceMaster.dll`이며 다른 후보에는 `--candidate <후보 DLL>`을 지정합니다. 비교 결과는 `compatibility.json`, `compatibility.log`에 기록합니다. 메타데이터 계약 외에 기준/후보의 실제 번역·주사위 메서드를 같은 대역 환경에서 실행해 관찰 결과를 비교합니다. 호환성 도구는 정상 후보 통과 후 리소스 속성/바이트, 누락/중복 타입, 중복 후킹, Harmony `__state` 이름, 설정 기본값, P/Invoke 진입점, `PadState` packing, delegate 호출 규약을 각각 변조한 10개 후보가 의도한 계약 때문에 실패하는지도 검사합니다. 비교에서만 고정 alpha.3 기준 DLL을 입력으로 읽습니다. 재컴파일한 DLL 전체 바이트 일치는 성공 조건이 아니며 DLL·ZIP 해시는 식별과 추적용입니다.

## 기존 모드 DLL 없는 격리 빌드

```sh
python3 development/tools/verify-clean-source.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

검사는 `development/`의 소스·도구·명세와 `config/`만 새 임시 소스 스냅샷에 복사합니다. `plugins/`, `.git/`, 기존 `bin/`, `obj/`, `releases/`와 과거 추출본을 포함하지 않으며 시작 시 `DiceMaster.dll`이 0개임을 확인합니다. 그 상태에서 일반 빌드·일곱 검사·두 파일 ZIP 생성을 그대로 실행합니다. 결과 `clean-build.json`, `clean-build.log`를 기본 후보 디렉터리에 기록하며 `--report-dir development/releases/<별도 후보>`를 사용할 수 있습니다. 원래 저장소 DLL을 이동하거나 삭제해 격리하지 않으므로 기존 작업 트리·배포 파일을 보존합니다.

## 최초 복원 재현과 설계 근거

기준 DLL이 작업 트리에서 달라진 경우에도 저장소의 고정 커밋에서 별도 파일로 다시 꺼낼 수 있습니다. 이 명령은 작업 트리 DLL을 바꾸지 않습니다.

```sh
git show f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec:plugins/DiceMaster/DiceMaster.dll > /tmp/DiceMaster-alpha3-reference.dll
shasum -a 256 /tmp/DiceMaster-alpha3-reference.dll
```

결과가 위 기준 해시와 일치하는지 확인하고 복원/비교 명령의 `--baseline`에 이 별도 경로를 지정합니다.

최초 복원은 ILSpy `9.1.0.7988`로 새로 수행합니다. 관리 도구를 준비한 뒤 빈 출력 폴더로 복원합니다.

```sh
dotnet tool install ilspycmd --version 9.1.0.7988 --tool-path /tmp/dicemaster-ilspy
python3 development/tools/recover-source.py \
  --baseline plugins/DiceMaster/DiceMaster.dll \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018' \
  --output /tmp/dicemaster-recovered \
  --ilspycmd /tmp/dicemaster-ilspy/ilspycmd
```

관리 소스가 같은 절차로 다시 만들어지는지만 확인하려면 `--output` 대신 `--check`를 사용합니다.

```sh
python3 development/tools/recover-source.py \
  --baseline plugins/DiceMaster/DiceMaster.dll \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018' \
  --ilspycmd /tmp/dicemaster-ilspy/ilspycmd \
  --check
```

이 단계는 최초 추출·재현용이며 일반 빌드가 실행하지 않습니다. 고정 기준 SHA-256과 도구 버전을 확인하고 다른 DLL이나 비어 있지 않은 출력 폴더는 거부합니다. `recovery-manifest.json`은 추출 파일·링크한 관리 소스·native 리소스의 해시와 적용한 보정을 기록합니다. 컴포넌트의 `.gitattributes`와 복원 도구의 UTF-8/LF 출력은 Windows checkout에서도 C# 줄바꿈과 내장 리소스 바이트가 달라지지 않도록 관리합니다. 별도 임시 분석 자료가 없어도 저장소·기준 DLL·명시한 도구/참조로 재생성할 수 있게 합니다.

복원 시 보정은 다음으로 제한합니다.

- `Gamepad`·`LiveLog`의 추출본을 제외하고 기존 관리 소스를 한 번 컴파일합니다.
- C# 12 컴파일러가 `RefSafetyRules(11)`과 지원 속성을 다시 생성하도록 중복 모듈 속성을 제거합니다.
- `DataHelper.GetMissions`의 `item2 != LiveMissionFilter`를 `(object)item2 != LiveMissionFilter`로 명시하여 기준 IL의 객체 참조 비교 의미를 보존합니다.
- 디컴파일러가 복원한 기존 컴파일러 식별자를 유지하고 컴파일 후 IL을 다시 쓰지 않습니다.
- 작성한 net48 프로젝트 템플릿으로 참조·어셈블리 속성·리소스 이름을 지정합니다.

프로젝트를 전체 모드 폴더에 배치한 이유는 게임 경계와 파서·UI·번역의 구현을 한 컴파일 단위에서 검토하기 위해서입니다. 기존 Gamepad·LiveLog를 파일 링크로 통합한 이유는 alpha.3 관리 구현을 유지하면서 추출본의 중복 타입·후킹을 방지하기 위해서입니다. 후보 디렉터리와 비교 단계를 분리한 이유는 정상 소스 빌드의 모드 DLL 의존성을 제거하고 기존 배포물을 유지하기 위해서입니다. 외부 게임 참조를 명시하고 로컬 경로만 받는 이유는 참조 버전을 식별하면서 게임 코드의 저장소·설치 ZIP 유입과 원본 수정 경로를 막기 위해서입니다.

## 검사 계약과 한계

새 DLL을 기존 관리 코드·실제 파서/표시 통합·게임 API 메타데이터·수명주기·패드 검사에 연결합니다. 파서나 설명 본체를 정답 문자열을 반환하는 대역으로 바꾸지 않습니다. `SourceBehaviorTests`는 후보의 실제 `EmbeddedTranslations.Display`와 전체 내장 번역 쌍·식별자 마스킹/캐시·반복 적용, `DicePatch`의 signed int 경계·prefab 부재·revolution·창 재열기 초기화를 실행합니다. Unity·로더·패드 브리지는 대역이며 별도 비교에서는 같은 관찰값을 기준 DLL과 대조합니다. 게임·Unity·native 경계를 대역으로 옮겨 실행하는 기존 관리 검사는 실제 Windows 실행과 구분합니다.

| 비교 | 보존해야 하는 계약 |
|---|---|
| 어셈블리·설정 | .NET Framework 4.8, 어셈블리/플러그인 식별, 공개 API, 설정 키·기본값 |
| 후킹·리플렉션 | Harmony 대상·패치 서명·속성, 수명주기/Lua 경계, 중복 타입·후킹 부재 |
| native ABI | P/Invoke 이름·호출 규약·구조체 레이아웃, XInput/MinHook 연결 |
| 내장 리소스 | 8개 이름·속성·개수·크기·바이트 |
| 동작 | 실제 파서/요약/로그/캐시, 현재 탭·숨김, 실패 격리·이전 값·5초 재시도·수동 복구, 난수·게임 부작용 보존, 주사위·패드 |
| 설치 ZIP | 정확히 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 두 파일, DLL은 이번 새 빌드와 동일 |

컴파일러 최적화·로컬 변수·분기 배치·컴파일러 생성 타입은 IL 형태를 바꿀 수 있습니다. 기존 패치 공정의 메서드 IL 지문 검사는 그 공정의 비교/복구 검사로 유지하고, 소스 후보는 메타데이터·후킹/참조 계약과 실제 메서드 동작 검사로 평가합니다. 필요한 검사를 제거하거나 임의 허용 범위를 늘려 성공 처리하지 않습니다. 기존 `LiveLogArtifactTests`의 패치 전후 정확한 지문 비교는 수정하지 않습니다.

기존 검사 중 `LogRefreshTests`의 표시 보호 IL 검사는 원래 `ldsfld; brfalse; ret`의 장형 명령만 허용했습니다. 새 소스 컴파일러가 만드는 단형 분기 또는 반대 방향 분기도 비영(보호 중) 깊이에서 즉시 반환하는 동일 조건을 검증하도록 바꿉니다. `DisplayRefreshDepth` 필드 소유자·참조 횟수·분기 극성·정확한 반환 대상을 확인합니다. 본문이 비어 있는 예전 Lua Prefix에 한해서는 `ret` 또는 동일 깊이 필드 읽기/`pop`/`ret`도 허용합니다. 실제 새 Lua 후킹과 파서·표시 동작 검사는 별도로 유지합니다. 기능 검사를 생략하기 위한 일반 IL 무시는 추가하지 않습니다. 패드 검사는 같은 전체 조합·입력·ABI 검사를 새 최종 DLL에 1회 실행합니다. 과거 525,430개는 payload와 설치 DLL 두 입력에 각각 262,715개를 실행한 합계였으며, 전체 소스 후보는 별도 payload를 만들지 않으므로 262,715개가 동일 검사 범위의 1회 결과입니다.

별도 `SourceBuildContractTests`는 이름을 가진 타입·필드·메서드의 접근 범위·형식·속성·상수·마샬링·override 연결과 참조 어셈블리 식별을 대조합니다. Harmony 인벤토리는 컴파일러 생성 타입/메서드를 포함한 전체에서 비교해 숨은 중복 후킹도 확인하며 native 진입점·delegate 규약도 대조합니다. 컴파일러 생성 closure/state machine의 식별자, 명령 인코딩·토큰 순서·MVID·PE 바이트는 이 검사 범위 밖입니다. 어셈블리·모듈과 이름을 가진 타입/멤버의 custom attribute를 비교하며 컴파일러·디버그·TargetFramework 속성을 일괄 제외하지 않습니다. 런타임 타깃과 플러그인 식별도 확인합니다. 표준 컴파일러가 기준과 같은 `.NETFramework,Version=v4.8` TargetFrameworkAttribute를 생성합니다.

기준과의 메타데이터 차이는 다음 위치로 한정해 기록합니다. 나머지 named reflection·Harmony·native 계약은 정규화 없이 비교합니다.

- `DiceMaster.EmbeddedTranslations.Display`의 기존 컴파일러 캐시 필드 5개: `CS$<>9__CachedAnonymousMethodDelegate{1,4,6,9}`는 `CS_0024_003C_003E9__CachedAnonymousMethodDelegate{1,4,6,9}`로, `$$method0x600000e-1`은 `_0024_0024method0x600000e_002D1`로 바뀝니다. C# 식별자로 표현하기 위한 ILSpy escape이며 필드의 형식·접근 속성·초기 데이터는 비교합니다. 이 private/internal 컴파일러 필드를 원래 이름으로 직접 reflection하는 외부 코드의 경우는 이 비교의 보장 밖입니다.
- Harmony 대상이 아닌 내부 래퍼 `OptionInfoPatch.PreviewReadGroup`, `PreviewCheckComparison`, `PreviewReadEventRate`, `PositionEventCache.GeneratePreviewDynamicDesc`의 이름 없는 매개변수는 `P_0`로 복원됩니다. 매개변수의 형식·순서·플래그는 동일합니다.
- 기존 payload 복사기가 누락한 C# `params`의 `ParamArrayAttribute`가 연결 소스의 `LogBridge.Call`, `LogBridge.CallDisplayHelper`, `PreviewFormatting.Helper`에 추가됩니다. `CallDisplayHelper`만 public이며 기존 메서드·배열 인수의 형식·이름·플래그와 호출 동작은 동일합니다. 이 속성은 C# 호출자가 배열을 펼친 인수 문법으로 호출할 수 있게 하며 기존 배열 호출의 런타임 연결은 유지합니다. 따라서 public 메타데이터 전체 바이트가 동일하다고 주장하지 않습니다.

내장 번역/에셋 바이트와 native ABI가 같아도 Unity/native 실제 호출·리소스 로드·Harmony 설치·다른 모드·Windows UI/패드·실제 게임 성능을 검증한 것은 아닙니다. 이런 경로는 향후 별도 승인 후 사용자 확인이 필요합니다.

## 실패 처리·확인 방법

실패 경로를 재현하려면 다음을 실행합니다.

```sh
python3 development/tools/verify-source-failures.py \
  --references '/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018'
```

이 검사는 폐기할 소스 스냅샷에서 SDK 누락/잘못된 SDK, 참조 누락/잘못된 해시, 의도적 컴파일 오류, 의도적 검사 오류에 더해 안전하지 않은 출력 루트·관리되지 않는 기존 출력·비교 사전 검사 실패의 아홉 경우를 만듭니다. 실제 게임 참조나 작업 소스는 수정하지 않습니다. 각 경우 비정상 종료·기대 진단·기존 후보 sentinel/관리되지 않는 파일 보존·새 ZIP 미생성 및 비교 실패 시 오래된 PASS 제거를 확인하고 `failure-paths.json`과 `failure-<case>.log`에 기록합니다. `--report-dir development/releases/<별도 후보>`를 지정할 수 있습니다.

참조 누락·잘못된 해시, 기준 DLL/복원 도구 불일치, 컴파일 오류, 중복 타입/후킹, 리소스 불일치, 검사 실패는 명시적 비정상 종료입니다. 이전 DLL을 새 성공 결과로 복사하거나 보관 DLL로 조용히 대체하지 않습니다. 실패 진단과 새 결과 식별을 확인한 뒤 수정하고 새 후보 디렉터리에서 다시 실행합니다. 기존 성공 후보는 실패한 실행 때문에 교체되지 않으며, 이전 영수증을 이번 실행의 성공으로 해석하지 않습니다. 후보 승격 중 중단되어 `.pending`/`.previous`가 남으면 내용을 확인한 뒤 복구하고 재시도합니다. 비교·격리 검사는 시작 시 이전 성공 JSON을 지우므로 실패 뒤 과거 PASS 기록을 새 결과로 남기지 않습니다.

문법 복원과 참조 보정은 기능 의미를 보존하는 범위로 제한합니다. 기능 변경이 필요해 보이는 경우 이번 복원에 섞지 않고 별도로 기록합니다. 런타임 진단은 기존 BepInEx 정책을 유지하며 전체 Lua·세이브·비밀정보를 새 로그에 추가하지 않습니다.

## 이번 로컬 검증 기록 — 2026-10-18

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

## Windows 확인과 기본 전환

Windows 설치·실행은 수행하지 않았고 이번 요청에서 요청하지도 않습니다. 향후 별도 승인 후 같은 후보 DLL의 SHA-256, 게임/로더/패드/Steam Input 환경과 사용자 결과를 기록해야 합니다. 기준 대비 UI·번역·네 탭·선택지 요약/전체 설명·설정 유지·세이브·로그/난수/수명주기·음수/0/100 초과 주사위·키보드/마우스/패드·MinHook/XInput·Harmony 및 설치/롤백 경로가 대상입니다.

자동 검사와 이 사용자 확인을 모두 통과하고 기본 명령·문서·배포 후보 경로를 전환한 뒤에만 PRD #3의 전환 조건을 완료로 처리합니다. 후보 상태에서는 기존 기본 빌드·배포 DLL을 보존하므로 로컬 롤백은 기존 경로를 계속 사용하는 것입니다. 향후 승인된 Windows 설치 뒤 문제 발생 시 게임 종료 후 보관 alpha.3 DLL을 복원하고 기존 설정·세이브를 유지합니다. 실제 Windows 업데이트/롤백은 아직 확인하지 않았습니다. 더 오래된 v1.0.2로 복원하면 선택지 결과 소실 문제가 다시 나타날 수 있고 v1.0.1에는 자동 갱신이 없습니다.
