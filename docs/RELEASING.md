# Release 등록 안내

[사용자 안내](../README.md) · [개발·검증](DEVELOPMENT.md) · [변경 이력](../CHANGELOG.md)

## 배포 규칙

| 항목 | 규칙 |
|---|---|
| 저장소 | `jungpaeng/dicemaster` |
| 태그 | `vX.Y.Z` |
| ZIP 이름 | `DiceMaster-vX.Y.Z.zip` |
| 기준 | 원격에 푸시한 커밋 하나를 ZIP과 태그에 동일하게 사용 |
| 공개 순서 | 초안 생성 → ZIP 업로드 → 다운로드 검증 → 공개 |

ZIP에는 아래 **두 파일만** 폴더 구조를 유지하여 포함합니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

`config/`나 `plugins/` 전체를 압축하지 않습니다. DebugMenu, 개발 파일, 문서, 로그, `.DS_Store`, `__MACOSX` 등은 포함하지 않습니다.
태그와 ZIP 이름의 버전은 일치해야 하며, 이미 게시한 버전은 유지하고 새 버전으로 등록합니다.
GitHub가 자동 제공하는 `Source code` 압축 파일은 이 설치용 ZIP과 별개입니다.

## 준비 사항

- Git, Python 3, 최신 GitHub CLI
- 저장소에 릴리스를 등록할 수 있는 계정
- 배포할 DLL과 설정의 검증 완료
- 변경 이력과 릴리스 설명에 기록할 확인 결과

명령은 저장소 루트의 Bash/zsh에서 실행하는 예시입니다. 환경에 따라 `python3`는 `python`으로 바꿉니다.
각 단계가 성공했는지 확인한 뒤 다음 단계로 진행하세요.

```sh
gh auth login --hostname github.com
gh auth status --hostname github.com
```

## 1. 변경 사항 검증 및 푸시

소스·테스트·문서와 갱신된 DLL·설정을 함께 검토하고 커밋한 뒤 `main`에 푸시합니다.
문서와 테스트가 실제 배포 DLL의 동작을 설명하는지 확인합니다.

```sh
git fetch origin main
git status --short
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
```

작업 디렉토리에 미커밋 변경이 없고, 현재 브랜치가 `main`이며, `HEAD`와 `origin/main`이 같은 커밋인지 확인합니다.

## 2. 버전과 기준 커밋 지정

아래 버전은 예시입니다. 사용하지 않은 버전으로 변경합니다.

```sh
release_tag="v1.0.2"
release_commit="$(git rev-parse HEAD)"
release_dir="development/releases/${release_tag}"
release_asset="DiceMaster-${release_tag}.zip"
release_zip="${release_dir}/${release_asset}"

gh release list --repo jungpaeng/dicemaster
git ls-remote --tags origin "refs/tags/${release_tag}"
```

같은 태그의 릴리스나 원격 태그가 이미 있다면 새 버전을 선택합니다.
`development/releases/`는 Git에서 제외되어 있으므로 ZIP과 임시 검증 파일을 이곳에 보관합니다.

## 3. 커밋 기준 ZIP 생성

```sh
mkdir -p "$release_dir"

git archive --format=zip --output="$release_zip" \
  "$release_commit" -- \
  config/DiceMaster.cfg \
  plugins/DiceMaster/DiceMaster.dll
```

[`git archive`](https://git-scm.com/docs/git-archive)를 사용해 작업 중인 파일이 아닌 지정한 커밋의 두 파일을 압축합니다.

## 4. ZIP 내용과 커밋 일치 검증

다음 코드는 파일 수·경로·압축 손상 여부와 각 파일이 기준 커밋의 내용과 동일한지 확인하고 ZIP의 SHA-256을 출력합니다.
Git이 추가한 디렉토리 항목은 파일 수에서 제외합니다.

```sh
python3 - "$release_zip" "$release_commit" <<'PY'
from pathlib import Path
import hashlib
import subprocess
import sys
import zipfile

zip_path = Path(sys.argv[1])
commit = sys.argv[2]
expected = ["config/DiceMaster.cfg", "plugins/DiceMaster/DiceMaster.dll"]

with zipfile.ZipFile(zip_path) as archive:
    files = [entry.filename for entry in archive.infolist() if not entry.is_dir()]
    assert sorted(files) == sorted(expected), f"Unexpected ZIP files: {files}"
    assert archive.testzip() is None, "ZIP integrity check failed"
    for name in expected:
        committed = subprocess.check_output(["git", "show", f"{commit}:{name}"])
        assert archive.read(name) == committed, f"Commit mismatch: {name}"

print("ZIP files and commit contents verified.")
print("SHA-256:", hashlib.sha256(zip_path.read_bytes()).hexdigest())
PY
```

검증에 실패하면 업로드를 진행하지 않고 대상 커밋과 ZIP을 확인합니다.

## 5. 릴리스 설명 작성

`${release_dir}/notes.md`에 변경 사항, 설치·업데이트 방법, 기준 커밋, 검증 결과를 작성합니다.
자동 검사 결과와 실제 게임에서 확인한 범위를 구분하고, 설정 변경이 필요한 경우 명시합니다.

다음은 파일에 복사하여 채워 넣을 수 있는 템플릿입니다.

```markdown
## 변경 사항

- 이번 버전의 변경 내용

## 설치 및 업데이트

Assets의 DiceMaster-vX.Y.Z.zip을 기존 BepInEx 폴더에 압축 해제합니다.
기존 설정을 유지하려면 DLL만 교체합니다.
설정 변경이 필요한 경우 해당 내용을 안내합니다.

## 기준 및 검증

- 기준 커밋: 전체 커밋 SHA
- 자동 검사: 수행한 검사와 결과
- 실제 게임 확인: 게임·모드 베이스 버전, 패드 환경, 확인한 동작
- ZIP SHA-256: 검증 단계에서 출력된 값
```

## 6. 초안 릴리스 생성 및 업로드

[`gh release create`](https://cli.github.com/manual/gh_release_create)로 ZIP을 첨부한 초안 릴리스를 만듭니다.

```sh
gh release create "$release_tag" "$release_zip" \
  --repo jungpaeng/dicemaster \
  --target "$release_commit" \
  --title "DiceMaster ${release_tag}" \
  --notes-file "${release_dir}/notes.md" \
  --draft
```

아직 존재하지 않는 태그를 지정하면 `--target`의 커밋을 기준으로 태그가 생성됩니다.
업로드가 중단되면 기존 초안 상태부터 확인하고, 다른 내용의 릴리스를 같은 태그로 덮어쓰지 않습니다.

## 7. 업로드 파일 다운로드 및 비교

[`gh release download`](https://cli.github.com/manual/gh_release_download)로 초안의 ZIP을 다시 내려받습니다.

```sh
gh release download "$release_tag" \
  --repo jungpaeng/dicemaster \
  --pattern "$release_asset" \
  --dir "${release_dir}/verify"

cmp "$release_zip" "${release_dir}/verify/${release_asset}"
```

다운로드 명령과 `cmp`가 모두 성공해야 합니다. `cmp`는 파일이 같으면 출력 없이 종료 코드 0을 반환합니다.
이는 업로드한 ZIP이 앞에서 검사한 ZIP과 바이트 단위로 동일한지 확인하는 단계입니다.

## 8. 공개 및 최종 확인

검증한 초안을 [`gh release edit`](https://cli.github.com/manual/gh_release_edit)으로 공개하고 최신 릴리스로 지정합니다.

```sh
gh release edit "$release_tag" \
  --repo jungpaeng/dicemaster \
  --draft=false \
  --latest

gh release view "$release_tag" \
  --repo jungpaeng/dicemaster \
  --json tagName,isDraft,isPrerelease,targetCommitish,assets,url

git ls-remote --tags origin "refs/tags/${release_tag}"
```

다음 항목을 확인하고 Release 등록을 마칩니다.

- 공개 상태이며 정식 배포라면 `isPrerelease`가 `false`인지
- 릴리스 버전과 ZIP 이름이 일치하는지
- 기준 커밋과 자동 생성된 원격 태그의 SHA가 일치하는지
- 첨부 ZIP이 하나이며 배포 규칙의 두 파일만 포함하는지
- 릴리스 페이지의 다운로드 링크가 올바른지

태그를 로컬에도 가져오려면 공개 후 `git fetch origin --tags`를 실행합니다.
