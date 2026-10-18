# PRD 2 로컬 구현·검증 기록

[PRD 2](https://github.com/jungpaeng/dicemaster/issues/2) · [개발 안내](DEVELOPMENT.md) · [기술 인계](ISSUE-2-TECHNICAL-HANDOFF.md)

2026-10-19 KST. PRD 2 로컬 소스·검사·별도 후보의 구현 기록이다. 이후 사용자 승인으로 commit·push하고 `v1.0.4-alpha.1` prerelease를 게시했다. 실제 Lua와 사용자 Windows 검증, 실제 자료를 사용한 추적 제한 검증은 미완료이며 기능·이슈 완료로 처리하지 않는다. 과거 alpha.4 사용자 Windows 확인을 이 후보에 승계하지 않으며 이슈 #2는 open으로 유지한다. 게시 설명은 [prerelease 안내](ISSUE-2-RELEASE.md)에 있다.

## v1.0.4-alpha.1 게시 기록

2026-10-19 **00:39:24 KST**에 [v1.0.4-alpha.1 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4-alpha.1)를 `draft=false`·`prerelease=true`·`make_latest=false`로 게시했다. 소스 태그 커밋은 `14ff82914d5d990362fdeebcf1dbd6eaa09bb6ef`, Release ID는 `416534622`, 설치 자산 ID는 `653814237`이다. 기존 정식 Latest `v1.0.3`과 이전 8개 Release·자산을 보존하고 이슈 #2 open을 다시 확인했다.

커밋에서 소스 115개 입력만 추출한 별도 폴더에 기존 모드 DLL·Git 데이터·이전 출력 없이 새로 빌드하고 8개 검사 프로그램을 통과했다. DLL `f02b3511760b0054b35319766ae5704bd76282cb48f90ae525231ca1a7ae6da3`과 설정 해시는 기존 검토 후보와 동일했다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.4-alpha.1/DiceMaster-v1.0.4-alpha.1.zip)은 208,944바이트 / SHA-256 `1f619d9a5c6a757425d6172fdbba18127690135926c623ad7e43377f493fc86d`다. 인증 없는 공개 API·다운로드에서 태그 커밋·CRC·정확히 두 파일·DLL/설정 바이트·해시 일치를 확인했다.

영수증은 `artifacts/releases/v1.0.4-alpha.1/release-verification.json`, `github-publication.json`, `anonymous-verification.json`과 빌드 로그에 있다. 이 게시 기록의 후속 문서 커밋은 소스 태그·공개 ZIP·DLL을 바꾸지 않는다. 실제 Lua·최종 추적 한도·이번 DLL의 Windows/native·실게임 성능 확인은 게시 후에도 미완료다.

## 시작 기준의 차이

시작 HEAD는 `9209a39fae31d4b7beedbd5fd93c53fe948324d4`이다. 인계 HEAD `6eb52e6b5cc7a3b3684d2ea2b7c1a555dcf60f00` 이후 PRD 3에서 전체 C# 소스 빌드로 전환했고 루트 배포 DLL을 제거했다. 최신 checkout을 되돌리지 않고 `src/DiceMaster/`를 구현 기준으로 사용한다. 이후 사용자도 최신 코드베이스 사용을 확인했다. 추적 파일 변경은 없었고 `.agents/`, `skills-lock.json`, 두 인계 문서는 기존 미추적 파일 그대로 보존한다.

과거 실패 대조용 alpha.2는 Git 커밋 `015c80c2fd9f905aaece574b3b11da52e501d432`의 `plugins/DiceMaster/DiceMaster.dll`을 `artifacts/issue-2/baseline/DiceMaster.alpha2.dll`로 복원했다. 462,848바이트 / SHA-256 `b6f7ae790cde736dcf1a282e9242c46b8ab0204602c7c462d59c571f321740ec`로 인계 기록과 일치한다. 과거 `working.dll`은 사용하지 않는다. 일반 소스 컴파일은 이 DLL을 입력으로 읽지 않는다.

GitHub API로 PRD 본문을 다시 읽어 등록 시점 스냅샷과 일치함을 확인했다. 본문 SHA-256은 `ebf23951caab985ff0a2bbca40bba475844494a6b83a6fc379840d372f8904a4`이다. 원본 게임 참조의 모든 파일은 작업 전 크기·SHA-256을 기록하고 후보 검사 후 대조한다. 게임 소스·DLL은 저장소에 추가하지 않는다.

## 분석과 표시의 경계

고정 구문 분석·후속 연결을 문맥 메모리에 캐시하고 표시 때 현재값만 읽는다. 문자열·주석을 건너뛰는 토큰 경계에서 호출·괄호·쉼표·순서·분기를 유지한다. 구조화한 효과와 이유를 선택지 요약과 로그가 공유한다. 미지원은 읽기 실패로 재시도하지 않는다. 일부/전체 미지원 안내는 정상 효과 앞에 배치하고 로그는 전체 설명을 유지한다. 효과가 발견되지 않으면 `확인된 효과 없음`으로 표시한다.

상수식은 숫자·사칙연산·괄호·단항 부호만 처리한다. 정수가 아닌 결과, 0 나누기, 비유한 값·범위 초과·지원 밖 연산은 미지원이다. 변수를 실행·추론하지 않는다. 현재값 예상은 읽기 전용으로 검증할 수 있는 경로에 한하며 선행 변경·조건·부가 능력치·제한을 확정할 수 없으면 의미 설명을 사용한다.

`SetNextScript`는 예약 대상 등록으로 설명한다. 연결된 효과에는 `후속 스크립트`와 `조건부`를 붙이고 다음 사용자 선택에서 멈춘다. 덮어쓰기·순환·동적 흐름을 즉시 실행 결과로 합치지 않는다. 실제 리소스 읽기는 기존 Resources의 `story/chinesetraditional/` 안에서만 수행한다. 경로 탐색·URL·파일 읽기 인터페이스를 추가하지 않는다. 정상 빈 대상, 누락, 미준비, 읽기 실패, 순환, 한도 도달을 구분한다.

실제 읽기 오류는 기존 열린 현재 탭의 5초 재시도·수동 복구·이전 값 정책을 따른다. 첫 실패에도 이미 해석한 효과는 유지한다. 요약 표시는 실패 상태와 부분 설명을 사용하며 스스로 재시도하지 않는다. 미지원 진단은 같은 문맥·스크립트·선택지·함수·원인마다 최초 1회이며 Lua 원문·세이브·인자 원문을 기록하지 않는다.

## 지원 API 의미

정확한 서명·게임 DLL token·하위 호출과 설명 경계는 [API 의미 대조](ISSUE-2-API-SEMANTICS.md)에 기록한다. 직접 설정은 가산과 구분하며 제거 함수는 1개 감소다. `UpdateSetPlayerStat`·`GetModifyString`·`GetDisplayTime`은 상태 변경 효과로 표시하지 않는다. `SetPlayerBattleSkill`의 index와 `SetMissionTime`의 year가 무시되는 동작, 활성값 1, 장비 종류별 백업·음수 레벨 유지, 시간 진행의 하위 효과를 설명한다.

## 추적 제한과 실측

현재 실제 Lua 리소스는 없다. 아래 유한 개발 안전 한도는 최종 검증값이 아니다. 실제 Lua의 크기·연결·분기 분포와 실게임 비용을 확보하기 전 이 항목은 완료하지 않는다.

| 개발 안전 경계 | 현재 값·계수 방식 |
| --- | --- |
| 후속 깊이 | 루트를 0으로 한 8단계 |
| 고유 스크립트 | 루트 포함 32개, 대소문자 구분 없이 재사용 |
| 누적 원문 | UTF-16 문자 262,144개 |
| 누적 토큰 | 65,536개 |
| 결정적 처리 예산 | 토큰·분기 순회 작업 131,072회 |
| 구문 분기 중첩 | 64단계 |
| 산술 재귀·정밀도 | 128단계, 중간 절댓값 2^53−1 이하 |

문자·스크립트·깊이 한도는 다음 대상을 확장하기 전에 검사하며 이미 해석한 상위 효과를 유지한다. 토큰·작업 한도는 처리 가능한 접두 부분을 유지하고 원인별 미지원 상태를 남긴다. 순환은 별도 이유다. 시간 타이머로 출력이 달라지는 대신 결정적 순회 예산을 사용한다. 이것은 실제 경과 시간 제한이나 Unity Resources.Load 비용 보장이 아니다. 각 값의 직전·도달·초과를 대역 검사하고 합성 분석의 시간·할당량을 아래 기록한다.

선택지 식별 자체도 같은 문자·토큰·작업 안전 경계에서 멈춘다. 보존한 선택지는 부분 미지원이 되고 루트 진단을 남긴다. 잘린 뒤의 미발견 O_ 키는 요약에서 전체 미지원으로 안내한다. 안전하게 식별하지 못한 임의 choose 키를 모든 내러티브 문자열에 임의로 대응시키지 않으며 이 경우의 실제 자료 대조는 남아 있다. 루트/대상 원문은 문맥 메모리에만 보관하고 로그에 출력하지 않는다.

## 자동 검사와 후보 식별

다음은 2026-10-19 KST에 최종 소스·후보로 새로 실행한 결과다. 실제 후보의 파서·Action·분석기·설명·선택지 요약·이벤트 캐시·bridge IL을 대역에 복사해 실행했으며 기대 문자열을 반환하는 가짜 파서를 사용하지 않는다. Unity·리소스·게임 상태·번역의 경계 대역과 실제 게임 실행은 구분한다.

| 검사 | 이번 결과 |
| --- | --- |
| alpha.2 실제 IL 실패 재현 | 8사례·24단언, 지정 SHA guard 포함 |
| 새 미리보기 통합 | 373단언. 16+12 함수·인자 검증·상수식·문자열/주석·순서/분기/반환·축약 호출·manager 재바인딩·다단계/덮어쓰기/순환/다음 선택·5종 추적 예산 경계·상태 예상량·실패/부분 보존·캐시·진단·5초/숨김/수동 복구 포함 |
| 기존 파서·로그·선택지 통합 | 134단언 |
| 표시 엔진 / 수명주기 | 16시나리오·70단언 / 4,023단언 |
| 캐시·보호 IL | 19단언 |
| 실제 게임 IL 동작 / 메타데이터 | 3,914 / 1,021단언. 게임의 조회 IL과 부작용 보호를 대역으로 검사, 진행용 효과 API 실행 아님 |
| 패드·주사위 범위 / 번역·주사위 | 소스 후보 패드 262,715단언, 최신 HEAD DLL과 번역·주사위 대조 4,341단언 |
| 기존 로그 injector 최초 / 반복 | 1,035 / 1,066 구조 단언. 기존 본문 318 / 331개 불변, 리소스 8개 바이트·속성 동일. 각각 새 미리보기 373·기존 통합 134·실제 IL 3,914+메타데이터 1,021 통과 |
| 패드→로그 교차·반복 | 패드 빌드 525,430단언과 소스/교차 최종 DLL 패드 재검사 525,430단언. 로그 최초 1,054 / 반복 1,051 구조 단언과 동일 통합·실제 IL 검사 통과. 처음 한 본문은 검증한 표시 가드 정규화만 허용 |
| 소스 후보의 실제 리소스·읽기 경계 | 최신 HEAD DLL과 내장 리소스 8개 바이트·속성 일치. 새 reader IL은 고정 Resources 접두 경로·Load 1회이며 파일·네트워크·Lua 실행·게임 변경/RNG 진입 호출 없음. native Resources 동작은 미검증 |
| 읽기 전용 원본 | 작업 전후 163개 파일의 크기·SHA-256 동일 |
| 빌드·포장 | 14프로젝트 오류 0. 주 프로젝트의 기존 미사용 코드 경고 7개는 시작 HEAD 빌드와 동일. 두 경계 대역 빌드에는 의도적으로 null인 ItemDatabase._instance 필드 경고가 각 1개 있다. ZIP CRC·정확히 두 파일·새 DLL/설정 바이트 일치 |

추가 검토에서 중첩 choose 뒤 효과, 재할당된 manager를 게임 API로 오인하는 경우, 실패 복구 때 원문 잘림 경고 소실, 이벤트 첫 실패의 부분 결과/중복 읽기, 이름 조회의 singleton 탐색을 발견해 수정하고 회귀 검사에 포함했다. 이벤트도 별도 파서에 실패 분석을 캐시하고 해당 열린 행의 재시도에서만 복구한다. 표시 이름은 검증한 기존 필드만 읽고 없으면 ID로 폴백한다. 실제 수치 조회의 미준비·오류는 이 폴백으로 숨기지 않는다.

합성 측정은 macOS Darwin 24.5.0 ARM64, .NET 8.0.32 / SDK 8.0.426의 관리 경계 대역에서 10,399자·200효과를 한 번 워밍업한 뒤 분석+설명을 100회 반복했다. 최종 소스 실행의 총 시간은 **131.319ms**, 현재 스레드 할당량은 **149,796,064바이트**다. 대략 1회 1.313ms·1.50MB지만 하나의 합성 형태에 대한 수치이며 실제 Lua·Unity Resources·게임 UI·Windows 성능이나 최종 예산 근거로 확대하지 않는다. 정상 주기에서 고정 분석·후속 리소스 읽기 0회는 별도 횟수 검사로 확인한다. 실제 실패 재시도에는 저장한 루트부터 제한 내 연결 분석을 다시 수행하므로 정상 주기와 비용을 구분한다.

최신 전체 소스 후보는 `artifacts/issue-2/candidate/DiceMaster.dll`, **502,272바이트 / SHA-256 `f02b3511760b0054b35319766ae5704bd76282cb48f90ae525231ca1a7ae6da3`**이다. 로컬 두 파일 ZIP은 208,944바이트 / SHA-256 `1f619d9a5c6a757425d6172fdbba18127690135926c623ad7e43377f493fc86d`다. 후보 생성 시각은 2026-10-19 00:30:06 KST이며 어셈블리 내부 버전 1.0.0.0은 기존 계약대로 유지한다. `build-manifest.json`은 114개 빌드 입력과 게임 참조·DLL·ZIP·설정·검사를 연결하고 이 후보의 Windows/실제 Lua 확인이 미실행임을 명시한다. 별도 legacy/cross DLL은 반복/교차 검사 산출물이며 위 소스 후보와 바이트가 같다는 주장은 하지 않는다.

로그·영수증은 `artifacts/issue-2/`의 `source-build-final.log`, `legacy-build-final.log`, `pad-build-final.log`, `cross-log-final.log`, `behavior-compare.log`, `final-pad-contract.log`, `source-resource-contract.json`, `reference-verification.json`과 후보의 `build-manifest.json`, 최종 `final-verification.json`에 있다. alpha.2 재현은 `baseline/runtime-results.txt`다. 이들은 Git 제외 생성 파일이며 게임 설치 경로가 아니다.

전체 소스의 컴파일러 생성 IL까지 과거 DLL과 동일하다고 주장하지 않는다. 기존 전체 동등성 검사는 PRD 3의 소스 복원용이며 의도한 PRD 2 동작 변경을 거부할 수 있다. 이번 보존 증거는 동일 입력의 번역/주사위 관찰값, 실제 게임·후킹 계약, 리소스 비교와 이름이 지정된 변경 경계만 허용하는 기존 패치 구조 검사다. 검사를 끄거나 무관한 본문을 모두 허용하지 않았다.

재현 명령은 저장소 루트에서 실행한다. SDK·캐시는 인계문의 환경 변수를 사용하고 `<참조 루트>`는 읽기 전용 게임 참조다.

```sh
python3 build/build-source.py --references '<참조 루트>' --output-dir artifacts/issue-2/candidate
python3 tools/LegacyPatch/build-log-refresh.py --input artifacts/issue-2/current-baseline/DiceMaster.dll --output artifacts/issue-2/legacy/DiceMaster.dll --game-managed '<참조 루트>/Mortal_Data/Managed'
python3 tools/LegacyPatch/build-gamepad.py --input artifacts/issue-2/candidate/DiceMaster.dll --output artifacts/issue-2/cross/DiceMaster.pad.dll
python3 tools/LegacyPatch/build-log-refresh.py --input artifacts/issue-2/cross/DiceMaster.pad.dll --output artifacts/issue-2/cross/DiceMaster.pad-log.dll --game-managed '<참조 루트>/Mortal_Data/Managed'
```

`current-baseline/DiceMaster.dll`은 시작 HEAD의 `src/`와 루트 빌드 파일을 `git archive`로 별도 폴더에 추출하고 같은 읽기 전용 참조로 다시 컴파일한 것으로 공개 v1.0.3의 SHA-256 `db10926e620c0297dfd80a86b5a5dbcab209dab39f691704e45f83206587cba1`과 일치했다. 기존 게시 DLL·원본 게임 참조를 덮어쓰지 않았다.

## 남은 완료 조건

- 실제 Lua의 선택지·후속 연결·상수식과 게임 로더 식별 규칙 대조.
- 실제 Lua와 비용 측정에 따른 최종 추적 깊이·고유 스크립트·원문량·처리 비용 제한 확정.
- Unity/native·실제 Harmony·다른 모드·실제 UI/게임 진행과 시간·할당량 확인.
- 해당 후보 DLL에 대한 사용자 Windows 확인. 이번 작업에서 후보 설치·게임 실행·오류 주입·런타임 검증을 수행하거나 요청하지 않는다.

기존 로그·난수·수명주기·주사위·패드·번역·설정·세이브 계약을 보존한다. 새 설정·통신·계정·영구 저장은 추가하지 않는다. 사용자가 승인한 배포는 `v1.0.4-alpha.1` prerelease이며, 문제가 있으면 이전 DLL 복원과 기존 설정·세이브 유지가 롤백 경로다. v1.0.2의 선택지 캐시 소실, 기존 로드 후 다음 성공 Lua까지 대기하는 제한은 해당 버전의 알려진 문제로 구분한다.
