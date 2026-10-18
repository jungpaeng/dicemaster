# prerelease 게시와 정식 Release 조건

[사용자 안내](../README.md) · [개발·검증](DEVELOPMENT.md) · [변경 이력](../CHANGELOG.md) · [검증 기록](ISSUE-1-VALIDATION.md) · [prerelease 설명](ISSUE-1-RELEASE-DRAFT.md)

**현재는 난수 표시 수정과 `v1.0.3-alpha.2` prerelease 재발행이 승인된 준비 단계입니다.** 필요한 commit·push·태그·게시 권한이 있으며 alpha.2 최종 패드→로그·반복 패치·직접 패드와 로컬 ZIP 검증은 통과했으며 게시 커밋 기준 증명·공개 다운로드는 준비 중입니다. 이전 버전 수치를 새 버전 결과로 승계하지 않습니다.

**[`v1.0.3-alpha.1` prerelease](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.3-alpha.1)를 2026-10-18 21:08:36 KST에 게시하고 공개 다운로드 검증을 마쳤습니다.** GitHub 상태는 `draft=false`, `prerelease=true`입니다. 이 게시로 기능·이슈 완료나 정식 Release로 처리하지 않습니다.

현재 Windows PC는 기존 파일·정보 읽기만 허용되어 있으며 게임 실행·후보 설치·런타임 검증은 승인되지 않았습니다. prerelease 게시 승인은 이 PC에서의 게임 실행 승인이 아닙니다. 실행을 요청하거나 수행하지 않습니다.

## alpha.2 게시 전 확인할 기록

최종 소스의 자동 검사·반복 패치·패드·구조 결과와 DLL SHA-256을 확정했습니다. DLL은 `b6f7ae790cde736dcf1a282e9242c46b8ab0204602c7c462d59c571f321740ec` / 462,848바이트이고 로컬 ZIP은 `fcd3111b101efba6ef62b0f79cb918f23e016ccf8e1637a821edb9e2ae0589f1` / 192,018바이트이며 정확히 두 파일·CRC·작업 트리 바이트 일치를 검증했습니다. 게시 단계에서는 같은 게시 커밋에서 `v1.0.3-alpha.2` 태그와 `DiceMaster-v1.0.3-alpha.2.zip`을 만듭니다. ZIP은 기존 두 파일 계약을 유지하며 공개 다운로드·CRC·커밋 바이트·SHA-256을 별도로 검증합니다. alpha.1의 태그·파일·해시는 덮어쓰지 않고 Latest 정식 `v1.0.2`를 유지합니다. 현재 문서는 alpha.2 게시 완료를 뜻하지 않습니다.

난수 미리보기의 실제 허용 범위·이론 확률 의미·미지원 표시와 미검증 native·Windows 경로를 게시 설명에 기록합니다. Windows 실행·설치 검증 승인은 추가되지 않았습니다.

## alpha.1 prerelease 게시 기록 — 불변 이력

| 항목 | 계약 |
|---|---|
| 저장소 | `jungpaeng/dicemaster` |
| 태그 | `v1.0.3-alpha.1` |
| ZIP | `DiceMaster-v1.0.3-alpha.1.zip` |
| 배포 기준 커밋 | `2d8936786845b534524b916537e7f1c3e5ab482d` — 태그·ZIP의 고정 기준 |
| GitHub 상태 | Release ID `416428315`, `draft=false`, `prerelease=true`; 기존 Latest `v1.0.2` (ID `416349430`) 유지 |
| 게시 시각 | 2026-10-18 12:08:36 UTC / 21:08:36 KST |
| ZIP 크기·SHA-256 | 188,793바이트, `95c753037a4cf5fe16db37735dbf4833c2ed76af71c7bd941c6f76d791932429` |

설치 ZIP에는 아래 두 파일만 포함합니다. DebugMenu·개발 파일·문서·로그·수신 게임 DLL·`.DS_Store`·`__MACOSX` 등은 포함하지 않습니다.

```text
config/DiceMaster.cfg
plugins/DiceMaster/DiceMaster.dll
```

최종 DLL은 SHA-256 `ca9061a39bc514b8996b6f6dce154908bad741aa4815ce0951e5ade37dcc97f7`, 455,680바이트입니다. 익명 공개 API와 익명 다운로드에서 두 파일만 포함됨·CRC 정상·ZIP SHA-256·각 파일의 배포 커밋 바이트 일치를 확인했습니다. 로컬 영수증은 `development/releases/v1.0.3-alpha.1/github-publication.json`입니다. 게시 기록을 보완하는 문서 후속 커밋은 위 태그·배포 커밋을 바꾸지 않습니다.

기존 태그·Release를 덮어쓰지 않습니다. GitHub의 자동 `Source code` 압축 파일은 설치용 ZIP과 별개입니다. 새 설정·원격 기능 플래그·단계별 배포는 도입하지 않습니다. 읽기 전용 원본 게임 참조는 게시 파일에 넣지 않습니다.

## alpha.1에 공개한 검증 범위 — alpha.2로 승계하지 않음

[게시 설명](ISSUE-1-RELEASE-DRAFT.md)에는 자동 검사와 미검증 경로를 함께 기록합니다. 실제 관리 DLL의 정적 API·수명주기 분석, 실제 IL을 native 경계 대역으로 옮긴 macOS 검사, 실제 게임 실행은 서로 다른 증거입니다.

alpha.1 후보는 캐시/보호 19개, 엔진 16개 시나리오·70개 단언, 수명주기 4,023개, 통합 최초·반복 각 94개, 실제 IL 각 152개·메타데이터 각 910개, 최종 구조 각 934개, 실제 설치 기준 구조 961개, 패드 525,430개를 통과했습니다. 코드 오류는 0이나 네트워크 차단에 따른 NuGet 보안 권고 조회 `NU1900` 경고가 있습니다. 상세 식별값과 측정 방법은 [검증 기록](ISSUE-1-VALIDATION.md)에 기록합니다.

alpha.1의 난수 실패 정책과 alpha.2에 반영한 범위·판정 미정·전용 미지원 표시의 차이, 제자리 복원 뒤 다음 성공 Lua까지 대기할 수 있는 동작을 알려야 합니다. native Unity·실제 리소스·Harmony·다른 모드·UI·Windows·실제 게임 성능은 미검증입니다. 모의 엔진의 벤치마크를 실제 게임 성능으로 기재하지 않습니다.

## 기능 완료·정식 Release 전에 필요한 증거

[검증 기록](ISSUE-1-VALIDATION.md)의 모든 수용 기준을 실제 증거에 연결해야 합니다. 미확인·실패가 남아 있으면 기능 완료·이슈 완료·정식 Release로 처리하지 않습니다. 이 조건은 이번에 승인된 prerelease 게시를 막는 조건과 구분합니다.

- 실제 파서·공유 캐시·표시·선택지 요약의 연속 갱신과 기존 기능 보존
- 현재 탭·세 상태·5초 재시도·수동 복구·진단 로그 동작
- 실제 IL과 순수 평가의 값 의미·RNG 비소비·카운터 보존·미지원 자료의 명시적 실패
- 실제 로드·새 게임·씬·Lua 경계, null·예외·오래된 완료·제자리 복원 처리
- 향후 별도 승인된 환경에서의 Unity/native·실제 리소스·Harmony·다른 모드·UI·진행 확인
- 별도 승인 후 사용자가 기록한 Windows 검증과 실제 게임 시간·할당량 측정
- 최종 DLL·소스·도구·문서·ZIP 일치, 반복 패치·패드 재빌드·리소스 보존

## 향후 롤백 검증

기존 설정과 세이브 형식은 변경하지 않습니다. 이전 Release 또는 보관 DLL 복원과 설정 유지 여부는 향후 별도 승인된 Windows 검증 대상입니다. 현재 Windows에 후보를 설치하거나 복원하도록 요청하지 않습니다.

v1.0.2로 돌아가면 선택지 결과 소실 문제가 다시 나타날 수 있습니다. v1.0.1에는 자동 로그 갱신이 없습니다. 실제 업데이트·롤백은 아직 검증하지 않았습니다.
