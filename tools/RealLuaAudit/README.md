# 실제 Lua 오프라인 대조

게임 설치 폴더를 실행하지 않고 전달된 `resources.assets`와 `globalgamemanagers`를 읽는다. 전체 Lua·세이브·인자·미리보기 전문을 진단이나 저장소에 기록하지 않는다. 원문 사본은 무시되는 `artifacts/` 안의 로컬 corpus에만 둔다. 원본에 출력하거나 덮어쓰지 않는다.

## 추출과 리소스 경로

별도 Python 환경에 `requirements.txt`의 UnityPy 1.25.4를 준비한다. 입력 SHA를 고정하고 새 출력 폴더를 지정한다.

```sh
python tools/RealLuaAudit/extract-resources.py \
  --resources '<읽기 전용 resources.assets>' \
  --manager '<읽기 전용 globalgamemanagers>' \
  --resources-sha256 '<검증한 SHA-256>' \
  --manager-sha256 '<검증한 SHA-256>' \
  --output-dir artifacts/real-lua-local/corpus
```

도구는 게임 폴더·dependency resolver 대신 두 파일의 바이트만 UnityPy에 전달한다. TextAsset의 인라인 필드와 ResourceManager 컨테이너를 raw 필드 디코더로 다시 비교한다. 각 출력 본문의 SHA를 확인하며 경로는 `m_Container`의 외부 fileID·pathID로 연결한다. 같은 이름이나 다른 언어에서 추측하지 않는다. 다른 타입의 미해결 객체는 Lua 본문 누락과 구분한다.

## 실제 후보 IL 대조

.NET 8과 저장소의 Mono.Cecil 0.11.6을 사용한다. 게임 DLL·Lua 인터프리터를 로드하지 않는다. 후보 DLL의 LuaParse/LiveLog IL을 기존 테스트 경계에 복사하고 현재 상태 조회를 비활성화한다. 모든 읽기 입력은 본문 SHA를 먼저 확인한다. 출력은 `artifacts/` 아래에 둔다.

```sh
dotnet build tools/RealLuaAudit/Fixture/CorpusFixture.csproj -p:NuGetAudit=false
dotnet build tools/RealLuaAudit/Runner/CorpusRunner.csproj -p:NuGetAudit=false
DICEMASTER_CORPUS_RESOURCE_MAP=1 dotnet \
  artifacts/real-lua-audit-tool/build-output/bin/CorpusRunner/debug/CorpusRunner.dll \
  '<후보 DiceMaster.dll>' \
  artifacts/real-lua-audit-tool/build-output/bin/CorpusFixture/debug/CorpusFixture.dll \
  artifacts/real-lua-local/corpus/story-chinesetraditional-index.json \
  artifacts/real-lua-local/mapped-report.json \
  '<후보 SHA-256>'
python tools/RealLuaAudit/independent_mapped_scan.py \
  artifacts/real-lua-local/corpus/story-chinesetraditional-index.json \
  artifacts/real-lua-local/mapped-report.json \
  artifacts/real-lua-local/mapped-comparison.json
```

`DICEMASTER_CORPUS_RESOURCE_MAP`를 생략하면 후속 읽기를 전부 Waiting으로 처리하는 과거 root-only 대조다. 이때 `lua-candidate-index.json`과 `independent_scan.py`를 사용한다. 키가 없는 상황을 Missing으로 추측하지 않는다. 매핑 모드의 Missing은 검증된 번체 리소스 집합에 해당 정적 대상이 없다는 뜻이며 게임 런타임에서의 누락과 동일시하지 않는다.

별도 Python 스캐너는 제품 파서를 import하지 않는다. 원본 주석 인덱스의 발견 여부와 실제 리소스 본문 안의 owner/name/인자 해시 근거를 대조한다. 조건 평가, 빠진 모든 효과, 동적 값, 정확한 실행 순서와 결과 수치까지 증명하는 오라클은 아니다. 미확인 항목을 완료로 간주하지 않는다. 시간·할당량은 macOS .NET 대역·리플렉션·JIT 비용을 포함하며 Unity 프레임 예산이나 최종 추적 한도 검증이 아니다.

`compare_reports.py <기준 보고서> <후보 보고서> <출력 보고서>`는 표시·분기 메타데이터가 바뀐 레코드와 실제 원본 호출 identity의 추가·누락을 구분한다. 호출 identity는 `source/owner/name/argument_sha256/followup`의 중복 횟수를 보존하며 공통 선택지의 루트·후속 증감과 영향을 받은 선택지 수를 별도로 집계한다. 새로 생기거나 사라진 선택지의 호출 수는 별도 항목이다. 조건·실행 순서·분기 의미는 이 identity 비교에 포함되지 않으며, 호출 수가 보존돼도 검증 전체나 보상 완전성을 증명하지 않는다.

이번 입력·실행 결과·남은 범위는 [실제 Lua 검증 기록](../../docs/ISSUE-2-REAL-LUA-VALIDATION.md)에 기록한다. 이 도구는 Windows 수집·설치·게임 실행·배포를 하지 않는다.
