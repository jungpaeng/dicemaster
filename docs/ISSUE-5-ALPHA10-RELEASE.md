# DiceMaster v1.0.6-alpha.10 — 조건 로그 능력치 이름 표시 개선

회상에 추가되는 DiceMaster 조건 로그와 공통 조건 설명에서 능력치 대신 내부 객체 이름이 표시될 수 있는 경로를 보완한다. 이름을 추정하기 전에 실제 능력치 참조의 현지화 키를 조회하고, 여러 값으로 구성된 식은 전체 식으로 표시한다.

**2026-10-20 04:15:46 KST에 [v1.0.6-alpha.10 prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.6-alpha.10)를 게시했다.** [이 버전 설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.6-alpha.10/DiceMaster-v1.0.6-alpha.10.zip) · [최신 정식 버전](https://github.com/jungpaeng/dicemaster/releases/latest). 실제 Windows 회상 화면 확인과 해당 체크포인트의 능력치 연결 확인은 대기이며, 아래 자동 검사는 게임 실행 결과가 아니다.

## 변경 사항

- `DataHelper.FormatConditionValName`이 실제 `StatValueReference._stat`의 `GameStat.GetLocaleKey()`로 이름을 구한다. `Ch_3_6_4_3_001`을 학문으로 치환하는 규칙이나 ID별 이름 사전은 추가하지 않는다.
- 학문을 참조하면 학문, 다른 능력치를 참조하면 그 이름을 표시한다. 상수·합산·곱셈·최댓값·최솟값·중첩식을 보존하며 일부 항을 해석하지 못한 식을 한 능력치로 축약하지 않는다.
- 이름을 구하기 위해 게임의 `GetValue`·`GetResult`·판정 실행·난수·숫자 능력치 getter를 호출하지 않는다. 중첩의 순환·깊이·확장 작업량을 제한한다.
- 설정·세이브·판정 계산·주사위·팝업 입력 조작은 변경하지 않는다.

## 알려진 한계와 실제 게임 확인

**참조를 해석하지 못하면 원본의 객체 이름 처리로 돌아간다.** 기존 객체 이름이 실제 내용과 다르면 오표시 가능성이 남는다. 이 시험판은 모든 이름의 정확성을 보장하거나 해석 불가 표시로 전면 전환하는 변경이 아니다.

제공된 게임 Lua에서 해당 ID가 분기와 주사위 판정에 쓰인 것은 확인했지만, 그 조건 객체가 실제로 학문 단독인지 복합식인지 연결할 게임 데이터는 확인하지 못했다. 자동 검사에서 같은 구조의 문제를 재현하고 개선했으며, 사용자 게임에서 보고된 바로 그 사례의 해결을 확인한 것은 아니다. [Windows 확인표](ISSUE-5-WINDOWS-VALIDATION.md)에 실제 관찰을 별도로 남긴다.

## 설치·업데이트와 복구

Windows용 활협전과 BepInEx가 포함된 활협전용 모드 베이스가 필요하다.

1. 게임을 종료하고 현재 `BepInEx/plugins/DiceMaster/DiceMaster.dll`과 `BepInEx/config/DiceMaster.cfg`를 백업한다.
2. 이 버전 Release의 `DiceMaster-v1.0.6-alpha.10.zip`을 받는다. GitHub 자동 `Source code` 압축 파일은 설치용이 아니다.
3. 업데이트는 **`plugins/DiceMaster/DiceMaster.dll`만 교체**해 기존 cfg를 보존한다. 최초 설치는 ZIP의 `config`·`plugins`를 기존 BepInEx 아래에 넣는다.
4. 문제 시 게임 종료 후 백업 DLL과 필요한 cfg를 복원한다. DLL 복원으로 이미 진행·저장한 게임 상태가 되돌아가지는 않는다.

설정·기본값 이행은 없다. alpha.9로 복구하면 기존 객체 이름 중심의 조건 표시를 사용한다. ZIP에는 DLL·cfg 두 파일만 포함하며 게임 소스·게임 DLL은 배포하지 않는다.

## 자동 검사 근거

2026-10-20 전체 소스 후보 빌드에서 **14개 검사 프로그램**을 통과했다. SDK 8.0.426으로 모드를 컴파일했으며 오류 0개·기존 경고 7개다. 실제 게임 DLL은 읽기 전용 컴파일 참조로 사용하고 실행 경계는 명시적인 fixture로 구분한다.

후보 DLL의 실제 `FormatConditionValName`·구조 참조 해석·번역·회상 로그 기록 IL을 실행한다. 새 이름 회귀는 27개이며 같은 ID의 학문/다른 능력치, 다른 ID의 같은 능력치, 복합식·부호·중첩·누락 참조·누락 번역·지원 밖 항·순환·깊이·작업량 제한을 포함한다. 학문 데이터에서도 내부 ID를 그대로 출력하는 기존 alpha.9 DLL의 실패를 같은 검사로 재현했고, 수정 DLL에서는 전체 번역/회상/주사위 3,538개 단언이 통과했다. 능력치 getter는 호출 시 예외를 던지는 대역으로 두어 표시 이름 조회가 숫자 계산을 실행하지 않는지 확인한다.

고정 소스 `df627348e3aa099a087389642d04f7ac4b0a845c`를 기존 모드 DLL 0개인 별도 소스 스냅샷에서 재빌드해 14개 검사 프로그램을 다시 통과했다. 검토 후보와 180개 빌드 입력·DLL/cfg/ZIP 바이트·전체 검사 결과가 일치한다. 게시 후 익명 공개 페이지와 ZIP 다운로드의 CRC·정확히 두 파일·내부 바이트·SHA-256도 대조했다. 실제 Windows·Unity 회상 화면·장치·다른 모드 조합의 확인은 대기이며 기존 버전의 사용자 확인을 새 DLL에 승계하지 않는다.

## 파일 확인

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| `DiceMaster-v1.0.6-alpha.10.zip` | 262,639 | `761777d3ba3d50585aa0e0416b189baff7d4903bfe041fcbab95fe3d2820be89` |
| `DiceMaster.dll` | 621,056 | `c2a413032c23e532083143c1bccc4292f62d837caf83e35ac9a27d7de6d8a1c1` |
| `DiceMaster.cfg` | 11,459 | `66b34802ed57b53db7ad977e96e8e17ef4847149e08ac3f244788f0704d9aea7` |

공개 소스는 [alpha.10 태그](https://github.com/jungpaeng/dicemaster/tree/v1.0.6-alpha.10)에서 확인한다. Latest 정식 v1.0.5와 기존 Release·태그·자산 및 이슈 상태를 유지한다. 로컬 검사·배포 영수증은 `artifacts/condition-name-fix/`와 `artifacts/releases/v1.0.6-alpha.10/`에 보존하며 공개 근거 링크로 사용하지 않는다.

## 게시 결과

Release ID는 `417652240`, asset ID는 `657185876`이며 `draft=false`·`prerelease=true`·`make_latest=false`다. 소스·주석 태그는 `df627348e3aa099a087389642d04f7ac4b0a845c`로 일치하며 README·수정·검사를 `origin/main`에 반영했다. 기존 33개 Release·태그·자산, Latest v1.0.5와 이슈 #2 closed·#4 open·#5 open을 보존했다. 업로드 실패로 남은 이번 작업의 빈 초안만 제거하고 공개 Release·태그·자산은 유지했다.

이 게시 결과 보충은 빌드 소스·태그·배포 파일을 바꾸지 않는 별도 문서 커밋이다. 고정 소스의 게시 준비 문구와 검증 영수증의 `published=false`는 게시 전 검사 시점이며 위 실제 게시 결과와 구분한다.
