# alpha.32 — 저장 지도 직행의 이야기 안내

<!-- alpha32-publication -->
게시 준비. main의 구현·자동 검증 후보이며 실제 Windows 확인은 대기다. PRD #6 전체 완료/이슈 종료로 처리하지 않는다.
<!-- /alpha32-publication -->

기준 main은 `d42be54449bf2a3e8f75365af24d1262a812fd02`다. 소스는 커밋 후 annotated tag `v1.0.7-alpha.32`로 고정하고 같은 소스를 새로 빌드해 아래 검토 후보와 대조한다. [구현·원인·지원 지문](ISSUE-6-FILE-DEFINITIONS.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha32--저장-지도-직행의-파일-정의-확인-대기).

## 변화와 지원 범위

Story를 거치지 않고 지도 저장을 바로 불러오는 경우를 위해 설치 파일의 조건·분기·초기화 정의를 확보한다. 파일의 현재 State/실행 횟수/시간/능력치를 쓰지 않고 현재 게임의 기존 참조와 연결한다. 다른 분기의 풀 실패로 직접 인사 등 독립적으로 확인한 경로까지 숨기던 전역 차단을 제거한다. 해금·완료·반복 활동·진입 보호는 유지하고 금색 기회/1회성 판정에는 파일 정의를 빌려주지 않는다.

디스크 대체 조회는 제공받은 게임 복제본의 검증된 자산/관리 DLL 지문을 지원한다. 파일이 다르면 지원 자료 불일치로 보류하며 기존 loaded-only 조회는 유지한다. 다른 게임 빌드나 모드 베이스 지원을 추정하지 않는다. 풀 자체가 필요한 경로와 불명 입력은 계속 미확정일 수 있다.

파일 검증/해석은 별도 스레드이며 캐시한다. 현재 값은 새로 읽고 정상0.25초/실패5초·warm Lua 재사용, 숨김/가림/포커스/OFF 취소·로드/장면 정리를 유지한다. 기존 설정 키·기본값과 cfg 바이트는 그대로다. Xbox/XInput·키보드·마우스·터치·소유권/해제/중립·#5 이벤트/예약·네 로그 탭·주사위·상점·실제 분기는 유지한다.

## 자동 검증

SDK 8.0.426으로 전체21개 프로그램을 통과했다. 고정 compiler 참조16개와 제공 복제본 DLL16개를 각각 입력으로 검사했다. 복제본 빌드는 같은 DLL 바이트를 생성했다. 파일 fallback 회귀는 실제 원본 파일과 SHA 확인한 번체 Lua2,199개를 사용하되 native 로딩·현재 세이브·UI/입력은 관리 경계다. 실제 Windows/Harmony/native 게임 실행을 하지 않았다.

빌드 입력 302개, 생산 소스/리소스 123개의 SHA-256 불변을 확인했다. ZIP은 cfg와 DLL 두 경로·CRC·내부 바이트를 확인했다. 배포 검증기 Python 회귀20개와 git diff --check도 통과했다.

- RealGameApiTests: METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed. / PASS: 4265 managed behavior assertions and 1280 metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.
- MapGuideRouteIntegrationTests: PASS: MapGuideRouteIntegrationTests 3764 production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.
- InformationRuntimeTests: PASS: InformationRuntimeTests: 6672 scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.
- ChoiceStoryTests: PASS: ChoiceStoryTests 7719 production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.
- GamepadTests: PASS: 262831 assertions.

FileDefinitionCases는 Story owner/config/pool이 없는 지도에서 실제 파일로 마커 복구, 완료·확정 false 제거, warm 재사용, 엄격한 reader/횟수 보류, 누락 초기화·인덱스/지문 오류·취소·늦은 결과 폐기를 포함한다. 별도 정적 초기자를 가진 가짜 카탈로그가 있어도 초기화하지 않는 경계도 검사한다. 게임 파일·DLL의 내용 불변과 원문 payload 미배포를 확인했다.

관리 경계의 8/32/64장소, 짧은/긴 Lua 표본을 이전 main과 비교했다. 모두 전체 배지 수·cold 한 번에 Lua1개 이하·warm Lua 읽기0을 유지했다. 아래는 p95 ms 범위이며 Windows 프레임 성능을 뜻하지 않는다. JIT/실행 순서·OS 영향을 포함하므로 속도 개선이나 실게임 동등성을 단정하지 않는다. 정상0.25초 간격을 늘리지 않았다.

| 표본 | 이전 main p95 ms | 현재 p95 ms |
| --- | ---: | ---: |
| 미포커스 | 0.086–1.563 | 0.085–1.786 |
| 포커스+배경 | 0.101–1.792 | 0.095–1.637 |

처음 전체 검사에서 DTO test source link, 다음 검사에서 새 index 리소스의 배포 allowlist 연결을 보완했다. 이후 owner 재조회 재사용과 정적 초기화 보호를 정리한 최종 코드로 전체/복제본 검사를 다시 통과했다. 앞선 로그도 보존한다. 세부 receipt·실행 로그는 로컬 artifacts/issue-6/file-definitions/에 있으며 공개 근거는 이 문서와 고정 소스다.

## 설치 후보

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 1088000 | `cfd046c5b46805d2a74d26f8e0edb17e1906b84d2ac779155a05b1598fe59a56` |
| DiceMaster-v1.0.7-alpha.32.zip | 429128 | `9aedcecbdc188a5c03aad5a60827cecb13198f7b22c6396d06c59cd370de1e50` |
| 기존 DiceMaster.cfg (바이트 유지) | 13083 | `deda801f4c061f3436732c1da6d4a3a2195dce79822dccb74846468faf9f99fd` |

[alpha.32 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.32/DiceMaster-v1.0.7-alpha.32.zip). 게임을 종료하고 DLL/cfg를 백업한 뒤 DLL만 교체해 기존 cfg를 보존한다. 문제 시 게임 종료 후 백업 DLL을 복구한다. 원본 게임 파일이나 추가 도구 설치는 필요하지 않다. 자동 Source code 압축은 설치 파일이 아니다. 이전 alpha.31과 Latest 정식 v1.0.6은 별도로 보존한다.

## 남은 실제 확인

Windows에서 프로세스를 완전히 다시 켜 지도 저장을 바로 불러온 후, 미포커스·남제자방 포커스·방 안 사사형·초회 전후·잠김/후속 주제를 비교한다. `파일 정의: 확인` 또는 지원 불일치 원인, 가림/OFF/다른 저장 뒤 정리, 초기/정상 지연·FPS/GC와 기존 입력·게임 상태 불변을 확인해야 한다. 사용자 확인 전 전체 완료나 #6 종료로 처리하지 않는다.
