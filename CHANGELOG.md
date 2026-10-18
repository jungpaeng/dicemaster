# 변경 이력

설치용 ZIP은 [GitHub Releases](https://github.com/jungpaeng/dicemaster/releases)에서 배포합니다.
이 문서의 버전은 이 저장소에서 배포하는 ZIP의 버전입니다.

## 미배포 변경

- README에 설치·조작·설정 안내를 정리하고 개발·Release 상세 문서를 분리했습니다.
- `development/`의 소스·테스트·도구를 버전 관리 대상으로 전환하고 빌드·릴리스 산출물만 제외하도록 정리했습니다.

## [v1.0.1](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.1) — 2026-10-18

### 변경

- 패드 조정의 최솟값 0 제한을 제거해 십자키와 왼쪽 스틱으로 음수를 지정할 수 있습니다.
- 0에서 왼쪽은 −1, 1에서 아래는 −9처럼 조정량을 그대로 적용합니다.
- LB+B의 자동 주사위(0) 복귀와 정수 오버플로 방지 처리를 유지합니다.

기준 커밋: [`9f778fc`](https://github.com/jungpaeng/dicemaster/commit/9f778fc90c0ca12219ae869096b2e1cdb4a232b4)

## [v1.0.0](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.0) — 2026-10-17

### 첫 배포

- 기존 DiceMaster를 기반으로 한국어 번역 처리를 DLL에 통합했습니다.
- LB와 십자키·왼쪽 스틱을 이용한 수치 조정, 반복 입력, LB+B 자동 복귀를 추가했습니다.
- A·X 등 기존 입력을 유지하면서 조정용 입력의 중복 실행을 차단합니다.
- 게임 시작 시 로그 창을 숨기고 Tab 토글을 유지합니다.
- 설정과 DLL 두 파일만 담은 DiceMaster 단독 설치 ZIP을 배포합니다.

이 버전의 패드 수치 조정은 최솟값이 0입니다. 음수 조정은 v1.0.1부터 지원합니다.

기준 커밋: [`b047482`](https://github.com/jungpaeng/dicemaster/commit/b04748267a673da29f6445e662a895720895f39b)
