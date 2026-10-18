# DiceMaster v1.0.4 정식 Release

**2026-10-19 03:43:05 KST에 [정식 v1.0.4](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.4)를 게시하고 Latest로 지정했다.** `draft=false`, `prerelease=false`이며 Release ID `416631625`, asset ID `654200835`다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.4/DiceMaster-v1.0.4.zip)은 인증 없는 공개 다운로드로 검증했다.

## 기준과 Windows 확인

사용자가 `v1.0.4-alpha.5`의 Windows 테스트 완료와 이슈 #2 완료를 확인한 뒤 정식 Release를 지시했다. 사용자 확인은 [완료 기록](ISSUE-2-COMPLETION.md)에 있으며 환경·항목별 로그·Windows 실측 성능은 별도 제공되지 않았다. 에이전트가 Windows 게임을 실행한 결과로 기록하지 않는다.

정식 태그는 완료 커밋 `102cafe086e2210b627644b4ce028ffd2427bc74`을 가리킨다. 이 커밋과 alpha.5 태그 커밋 `d669c4ba08a0450679e5f5d83a8cd20af23b6a58`의 소스·검사·도구·빌드·설정 입력은 같다. 차이는 게시/완료 문서다.

당시 작업 폴더에는 별도 이슈 #4의 미커밋 소스·검사·설정·문서 변경이 있었다. 이를 수정·되돌림·스테이징하거나 배포에 포함하지 않고, 고정 완료 커밋만 별도 경로에 추출해 그 커밋의 빌드 도구로 새로 빌드했다. 역사적인 v1.0.3 복원 동등성 정책이나 prerelease 전용 검증기를 약화하지 않았다.

## 파일 식별값과 검증

| 파일 | 크기 | SHA-256 |
| --- | ---: | --- |
| DLL | 537,088바이트 | `6f4044b5b2e6d6b4f83f57937414dc6887c285660074776308084139bed62dba` |
| `DiceMaster-v1.0.4.zip` | 223,370바이트 | `c0e97431c897df7428e3092893771c042beace31f99310c7db1365147de4d0b3` |
| 설정 | 9,759바이트 | `8fada1d878689e70f1ec510c6438116f7ab6e69e1e7a728bbb0986164e77bc7e` |

새 정식 ZIP의 파일명만 바꾸었으며 내용은 사용자 확인 맥락의 alpha.5 공개 ZIP과 바이트 단위로 같다. ZIP은 `config/DiceMaster.cfg`, `plugins/DiceMaster/DiceMaster.dll` 두 파일만 포함한다. alpha.5 사용자는 DLL·설정을 다시 바꿀 필요가 없다.

- 고정 커밋의 깨끗한 빌드 입력 133개와 전체 8개 검사 묶음, 미리보기 1,453개 단언이 통과했다.
- 새 빌드의 DLL·설정·ZIP이 alpha.5 검토/공개 다운로드와 바이트 동일했다. 같은 DLL SHA의 실제 Lua 대조는 [공유 분석 검증 기록](ISSUE-2-SHARED-ANALYSIS.md)으로 연결한다.
- 정식 공개 다운로드가 새 빌드와 같고 ZIP CRC·정확히 두 파일·DLL/설정 해시·태그 커밋이 일치했다.
- GitHub `/releases/latest`가 v1.0.4를 반환함을 확인했고 기존 13개 Release·prerelease·자산은 보존했다. 이슈 #2는 `closed` / `completed`를 유지했다.

## 지원 범위

PRD #2의 확장 효과·상수식·안전한 상태 의존 설명·다단계 후속·구조화 상태·50자 요약/전체 로그·정적 분석 공유를 정식으로 배포한다. [alpha.5 설명](ISSUE-2-ALPHA5-RELEASE.md)의 기능과 지원 경계는 동일하다.

유한 추적·한도 영향 54개 선택지의 부분 분석·동적/지원 밖 구문·미입증 콜백/조건의 중립 표시는 그대로다. 제공된 실제 Lua의 현재 조건 힌트는 계속 중립이며, Mac 대역에서 약 0.34초의 무거운 동기 추출과 프레임 분할 미구현도 유지한다. 사용자 Windows 확인을 모든 미래 결과·환경·FPS·상주 메모리의 증명으로 확대하지 않는다.

영수증은 `artifacts/releases/v1.0.4/{release-verification,github-publication,anonymous-verification}.json`, 새 빌드 로그와 게시 설명은 `artifacts/stable-v1.0.4/`에 보존한다. 이 문서의 후속 커밋은 정식 소스 태그·DLL·ZIP을 변경하지 않는다.
