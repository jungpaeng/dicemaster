# PRD #6 — 단계별 성능 개선과 검증

2026-10-23 작업. 기준은 `main`의 `b0e591a37ad8e99f6d84f5dc4765ca6bb1bda77b`이다. 주사위 자동 이야기 마커·요약을 되살리지 않는다. 별도 브랜치·worktree 없이 원래 작업 폴더에서 진행했다. 이 변경의 실제 Windows·Unity 프레임·물리 패드 확인과 PRD #6 전체 완료는 대기다. 후속 커밋·prerelease 상태는 [alpha.41 기록](ISSUE-6-ALPHA41-RELEASE.md)을 따른다.

## 구현과 수명

1. **상태 조회와 로그 발행 분리** — 정상 0.25초/실패 5초 조회는 그대로 수행한다. 정보 행의 이름·종류·상태·상세·자동 문구·앵커·가능/기회/경로 비트와 목록 순서가 같으면 로그 모델 투영과 발행만 생략한다. 렌더링에는 항상 최신 provider 결과를 전달한다. 수동 갱신, 문맥 변경, 실패·복구, 다른 발행자의 덮어쓰기, 로그의 발행 실패는 재발행한다. 지도 상세가 열리면 진단은 계속 갱신한다. 범용 로그 엔진이나 기존 네 탭의 발행 계약은 바꾸지 않는다.
2. **설치 정의와 현재 연결 분리** — `MapDefinitionFileReader`는 현재 객체가 없는 순수 템플릿 한 개를 보관한다. 설치 절대 경로와 내장 인덱스 SHA-256이 같아도 새로운 준비 때 승인된 여섯 파일 전체의 크기·SHA-256을 검증한다. 경로·mtime만으로 검증을 대체하지 않는다. 스키마·오프셋·그래프 상한도 유지한다. 현재 바인딩마다 별도 `DisplayDefinition` 뷰를 만들고 필요한 필드·목록의 연결만 지연 생성한다. 상수·이름 등 불변 리터럴은 공유하며 현재 입력 연결만 뷰에 따로 보관한다. 실제 현재 값은 계속 현재 입력에서 읽는다. 세이브/장면/OFF 무효화는 이전 뷰·소유자·작업을 버린다. 정적 템플릿에는 Unity 객체·현재 값·바인딩 사전·델리게이트가 없다. 직렬화된 State/count/현재 시간을 복원하지 않으며 경로 전용 자료를 1회성/기회 판정으로 승격하지 않는다.
3. **반복 메타데이터 검색 축소 실험 — 미채택** — 실제 게임에서 확인한 `Mortal.Core.StatValueReference`의 필드 메타데이터를 미리 연결하고 ReadStamp를 보존하는 안을 비교했다. 60만 회의 작은 반복 검사에서는 이득이 있었으나 교차 재측정에서는 폭이 불안정했고 실제 마커 provider의 정상 비용을 일관되게 줄이지 못했다. 전체 경로의 분기 비용과 복잡도를 늘리지 않도록 최종 소스는 기준의 엄격한 계층 검색으로 되돌렸다. 캐시 프로토타입과 제외 사유는 `03-metadata/prototype/`에 남긴다.
4. **짧은 경로의 준비 대기 단축** — 배경 준비에서 진전이 있는 장소는 최대 네 번의 조회까지 이어 처리한 뒤 동료 장소에 순서를 넘긴다. 막힌 작업은 즉시 양보한다. 선택 장소 우선과 이미 확인한 근거의 현재 입력 재검증을 유지한다. 한 화면의 원문/풀 읽기·8192 작업 슬롯과 협력식 약 5ms 예산은 늘리지 않는다. 정상 갱신 간격도 늘리지 않는다. 전체 자료 처리량을 늘리는 변경이 아니라 첫 유효 안내의 대기를 줄이는 변경이다.

최초 템플릿 시도는 현재 연결을 전부 복사해 초기 비용이 늘었다. 해당 방식은 채택하지 않고 지연 연결로 바꿨다. `02-definitions/definitions`는 폐기한 전체 복사 시도의 측정이며 최종 성능으로 인용하지 않는다. 지연 연결은 작업을 뒤로 미루므로 최종 정의 측정은 준비 비용과 전체 정의를 모두 실제로 읽은 비용을 따로 기록한다. `07-pure-nodes`의 추가 노드 분류 시도도 최초 할당량 증가와 복잡도에 비해 이득이 부족해 제외했다. 최종 구현에는 해당 분류 비트/Seal 처리가 없다.

## 비교 방법

모든 숫자는 macOS 호스트 .NET 8 Release의 자동 측정이다. Unity·게임 Lua·Windows 입력을 실행한 FPS 수치가 아니다. 마커와 현재 조건은 운영 소스, 게임/UI 경계는 기존 managed fixture를 사용한다. 설치 정의 검사에서는 사용자 게임 복제본의 실제 파일과 승인된 인덱스를 읽는다.

- 로그 발행: 같은 provider 결과로 2,000회 조회, 별도 프로세스 다섯 번. 조회 횟수·발행 횟수·틱당 할당량·p50/p95를 비교한다.
- 마커: 8/32/64개 장소, 무관한 조건 0/4096개, 무포커스/포커스/3단계 연결. 준비 300회와 준비 후 200회. 같은 probe를 기준 Git archive와 현재 소스에 연결한다. 첫/전체 마커 조회 번호, 원문 읽기, 준비/정상 비용, 할당량, 표시 내용 SHA-256을 기록한다. 기준/변경을 순서대로 세 쌍 비교하며 동시에 빌드나 다른 측정을 돌리지 않는다. 최초 비교는 `comparison/`, 리터럴 공유 뒤의 비교는 `final-comparison/`, 필드 캐시를 제외한 최종 채택 소스는 `accepted-comparison/`에 구분한다.
- 정의: 프로세스당 최초 한 번과 재준비 다섯 번. 준비 비용과 모든 필드의 지연 연결까지 강제한 비용을 별도로 기록한다. 후자의 전체 탐색은 일반 지도보다 훨씬 넓은 보수적 작업이다.
- 필드 접근: 실제 Snapshot.Field 메서드에 연결한 동일 delegate로 60만 회씩 여섯 번. 값 변경 직후 새 값이 보이는지 함께 확인한다. 메타데이터 캐시가 할당량까지 줄인다고 가정하지 않는다.

```sh
python3 tools/benchmark-information-publications.py --baseline b0e591a --output artifacts/issue-6/performance-stages/recheck/baseline-publications
python3 tools/benchmark-information-publications.py --output artifacts/issue-6/performance-stages/recheck/final-publications
python3 tools/benchmark-guidance-markers.py --baseline b0e591a --output artifacts/issue-6/performance-stages/recheck/baseline-markers
python3 tools/benchmark-guidance-markers.py --output artifacts/issue-6/performance-stages/recheck/final-markers
```

.NET 8 SDK를 PATH에 두거나 두 도구의 `--dotnet`을 사용한다. 실제 정의를 포함하려면 `DICEMASTER_MAP_GAME_DATA`에 읽기 전용 `Mortal_Data` 경로를 지정하고 `--modes unselected selected deep definitions metadata`를 준다. 오프라인 전체 빌드는 기존 NuGet 패키지 캐시를 `NUGET_PACKAGES`로 지정할 수 있다. 검사용 Lua corpus는 `DICEMASTER_CORPUS_INDEX`에 `name/path/sha256` 형식의 검증된 인덱스를 지정한다. 추출 도구의 일반 TextAsset 인덱스와 혼동하지 않는다. 이전 측정은 덮어쓰지 말고 별도 출력 폴더를 사용한다.

## 최종 채택 소스의 측정 결과

기준/변경 세 쌍의 중앙값이다. 로그 발행은 각각 별도 프로세스 다섯 번의 중앙값이다. 원본 수치는 `artifacts/issue-6/performance-stages/comparison-summary.json`과 `accepted-comparison/`에 보존한다.

| 항목 | 기준 | 변경 | 해석 |
| --- | ---: | ---: | --- |
| 동일 상점 2,000회 조회의 재발행 | 2,000회 | 0회 | 현재 상태 조회는 양쪽 모두 2,000회 |
| 상점 틱당 할당량 | 3,224 B | 2,656 B | 약 18% 감소 |
| 동일 지도 2,000회 조회의 재발행 | 2,000회 | 0회 | 현재 상태 조회는 양쪽 모두 2,000회 |
| 지도 틱당 할당량 | 5,392 B | 4,616 B | 약 14% 감소 |
| 설치 정의 재준비 | 46.34 ms / 15.61 MB | 30.74 ms / 1.72 MB | 검증은 매번 69,272,604바이트 모두 해시 |
| 재준비 후 17,829개 정의 전체 조회 포함 | 53.33 ms / 17.84 MB | 43.79 ms / 11.30 MB | 지연한 작업도 포함한 보수적 전체 탐색 |
| 최초 설치 정의 준비 | 51.86 ms / 15.62 MB | 52.37 ms / 16.09 MB | 처음에는 템플릿 생성이 필요해 이득 없음 |
| 최초 준비 + 모든 정의 전체 조회 | 59.79 ms / 17.86 MB | 75.47 ms / 26.11 MB | 역효과가 남는 최악의 일괄 조회. 개선으로 보고하지 않음 |
| 64곳·3단계 경로의 첫 마커 | 129번째 조회 | 3번째 조회 | 짧은 경로를 먼저 마무리 |
| 같은 사례의 전체 마커 | 192번째 조회 | 192번째 조회 | 전체 준비 처리량은 증가하지 않음 |

54개 마커 사례의 최종 이름·종류·상태·자동 문구·가능/기회/경로 비트 SHA-256이 기준과 일치했다. 준비 후 추가 원문 읽기는 모두 0회, 한 조회의 cold 원문 읽기는 최대 1회였다. 준비 후 마커 provider의 시간은 조건/실행에 따라 변동해 일반적인 FPS 개선 근거로 사용하지 않는다. 필드 캐시도 해당 이유로 최종에서 제외했다.

**정의 캐시를 채택한 범위는 필요한 일부만 조회하고 같은 설치 자료를 재준비하는 현재 지도 경로다.** 모든 정의를 최초에 일괄 조회하는 용도로 확대하면 위 역효과가 있으므로 별도 검증 없이 확대하지 않는다. 템플릿을 한 개 보관하므로 준비 중/후의 전체 프로세스 상주 메모리 감소를 이 할당량 숫자로 주장하지 않는다. 첫 자동 지도 조회에서의 Unity 프레임 지연은 Windows에서 별도로 확인해야 한다.

## 자동 검사와 Windows 경계

회귀는 동일 내용의 재발행 억제와 현재 조회 유지, 모든 정보 필드 변경, 수동 갱신, 실패/복구·5초 대기, 덮어쓴 모델 복원, 숨김 중단, 현재 참조 재연결, 템플릿의 현재 객체 부재, 캐시 적중 후 전체 지문 검사, 잘못된 파일/인덱스 거부, 취소·늦은 완료 배제, 현재 필드 읽기의 ReadStamp·누락 필드 처리를 포함한다. 기존 실제 경로·완료·반복 활동·불명 RNG와 거짓 AND·선택 해금·1회성·입력 회귀도 유지한다.

실제 Windows에서는 새 게임과 완전 재시작 후 불러오기를 각각 확인해야 한다. 남제자방·연단방의 첫 마커/스피너, 초회 대화 완료 후 제거, 저장 불러오기 후 현재 값 반영, 다른 창/로그/OFF 뒤 복귀, 키보드·마우스·터치·Xbox/XInput 소유권을 확인한다. 게임 프레임 p95, GC 정지, 전체 배경 준비 시간을 별도로 측정해야 하며 자동 조회 번호를 게임의 실제 초 단위 지연으로 바꾸어 보고하지 않는다.

## 외부 근거와 적용 한계

- [Home Assistant: 조회와 변경/복구 알림 분리](https://github.com/home-assistant/core/blob/2026.8.0/homeassistant/helpers/update_coordinator.py#L390-L536).
- [Roslyn: 같은 입력/결과의 이전 상태 재사용](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/SourceGeneration/Nodes/TransformNode.cs#L39-L93), [동등성 모델과 오래된 객체 보유에 대한 지침](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#L73-L92).
- [Harmony: 반복 사용을 위한 필드 접근 메타데이터](https://github.com/pardeike/Harmony/blob/v2.4.2.0/Harmony/Tools/AccessTools.cs#L1176-L1231).
- [Dapper: 스키마에 따른 변환 계획 재사용](https://github.com/DapperLib/Dapper/blob/2.1.66/Dapper/SqlMapper.TypeDeserializerCache.cs#L110-L153).
- [VS Code: 중단하고 이어 가는 예산 기반 준비](https://github.com/microsoft/vscode/blob/main/src/vs/editor/common/model/textModelTokens.ts#L420-L550).

이 사례들은 분리·재사용의 방향을 뒷받침한다. DiceMaster의 구체적인 시간 상한·메모리 비용·Windows 성능 향상을 증명하지는 않는다. Roslyn과 VS Code는 같은 조직 계열로 세며 모드/프레임워크의 포크를 독립 증거로 더하지 않는다.

## 최종 후보와 검사 영수증

최종 채택 소스에서 전체 21개 검사 프로그램이 통과했다. 실제 설치 파일/원문을 제공한 MapGuideRouteIntegrationTests 4,218개, 실제 후보 DLL 연결을 포함한 InformationRuntimeTests 7,241개, ChoiceStoryTests 7,819개를 포함한다. 소스/리소스/검사/빌드 입력은 검증 중 바뀌지 않았다. 사용자 게임 복제본을 참조해 별도 DLL을 새로 컴파일하고 API 검사 4,265개 managed behavior / 1,288개 metadata contract를 통과했다. 해당 검사기/fixture 소스는 변경하지 않아 앞선 빌드의 같은 검사 실행 파일을 재사용했고 최종 후보 DLL을 새로 검사했다. 게임 참조 파일의 검증 전후 해시도 일치했다. 게임/Unity/Harmony 실행 결과가 아니다.

- 전체 후보: `artifacts/issue-6/performance-stages/verification/accepted-pinned-candidate/`
- 실제 게임 DLL/API 연결: `artifacts/issue-6/performance-stages/verification/accepted-clone-api/`
- 통합 영수증: `artifacts/issue-6/performance-stages/verification/summary.json`
- 최종 비교: `artifacts/issue-6/performance-stages/comparison-summary.json` 및 `accepted-comparison/`
- DLL SHA-256: `ea6e09e98e2817aa08d2dbe05ddf3adc1e08ef64e493e61d56a95e0b74b47bdb`
- 설치 ZIP SHA-256: `4ca87e1033014405440b7b207126270883298a88a5af4016edf1bc39ce858c39`

설치 ZIP은 cfg와 DLL 두 경로만 포함하고 CRC·각 파일 바이트를 새 출력과 대조했다. 기존 cfg를 보존한다. 위 단계별 검증 당시의 로컬 후보이며, 후속 커밋/태그/prerelease와 공개 파일 대조는 [alpha.41 기록](ISSUE-6-ALPHA41-RELEASE.md)에 확정한다. 실제 Windows 확인과 #6 전체 완료는 대기다.
