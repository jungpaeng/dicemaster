# PRD 2 구현 시작 프롬프트

아래 본문을 DiceMaster 저장소에 연결한 새 채팅에 붙여 넣는다. 별도 worktree를 쓰면 이 파일과 `ISSUE-2-TECHNICAL-HANDOFF.md`가 그 checkout에도 있는지 먼저 확인한다. 새 채팅은 이 두 문서만 작성한 기존 세션과 달리 PRD 2 구현을 시작하는 요청을 받는다.

---

DiceMaster의 PRD 2 구현을 시작해줘.

PRD: https://github.com/jungpaeng/dicemaster/issues/2

먼저 선택한 checkout의 `docs/ISSUE-2-TECHNICAL-HANDOFF.md`를 읽고, PRD 본문과 `docs/DEVELOPMENT.md`, `docs/REAL-GAME-API-FINDINGS.md`, `docs/GAME-LIFECYCLE-ANALYSIS.md`, `docs/RANDOM-PREVIEW-FOLLOWUP.md`, `docs/RELEASING.md`를 확인해줘. 문서가 이 checkout에 없다면 원래 저장소 `/Users/im-yongbeen/dev/jungpaeng/활협전 DiceMaster/docs/`에서 두 인계 문서를 읽어줘.

인계 기준 HEAD는 `6eb52e6b5cc7a3b3684d2ea2b7c1a555dcf60f00`이고, 구현 기준 DLL은 alpha.2의 `plugins/DiceMaster/DiceMaster.dll`이다. SHA-256은 `b6f7ae790cde736dcf1a282e9242c46b8ab0204602c7c462d59c571f321740ec`다. 시작할 때 실제 HEAD·작업 트리·DLL 해시를 확인하고 차이를 대조해줘. 기존 변경사항을 되돌리거나 정리하지 마. 과거 임시 폴더의 `working.dll`은 alpha.1 사본이므로 현재 입력으로 사용하지 마.

확정된 요구사항을 다시 질문하지 말고, 구현·검사로 진행해줘. 현재 DLL의 실제 파서와 설명 경계를 확인하고, 실패 사례를 실제 경로로 재현한 뒤 구조화된 분석 상태와 구문 경계, 16개·12개 함수 지원, 상수 사칙연산, 안전한 상태별 예상량, 다단계 후속 추적을 순서대로 구현해줘. 선택지 요약·로그·injector·검사를 함께 연결하고 기존 로그·난수·수명주기·캐시·주사위·패드 계약을 보존해줘. 구체 구현 방식은 판단해서 진행하되, PRD가 위임한 추적 제한은 실제 자료와 비용 측정에 근거해 정하고 문서·경계 검사에 반영해줘.

원본 게임 참조는 `/Users/im-yongbeen/Documents/Codex/2026-10-18/task/references/LegendOfMortal-mac-references-20261018`에 있고 읽기 전용이다. 실제 Lua 리소스는 아직 없으므로, 합성 Lua·읽기 경계 대역으로 가능한 구현과 검사는 계속하고 실제 자료가 필요한 대조·최종 제한값 검증은 미완료로 구분해줘. 도구와 자료의 정확한 경로·빌드 명령은 기술 인계문에 있다. 임시 자료가 없어도 현재 DLL·저장소·원본 참조에서 필요한 분석을 재생성해줘.

표시를 위해 게임 Lua·상태 변경 API·RNG·조건 실행 사슬을 실행하지 마. Windows PC에서는 기존 파일 읽기만 허용되며 후보 설치·게임 실행·오류 주입·런타임 검증을 수행하거나 요청하지 마. 전체 Lua 원문과 세이브 내용을 진단 로그에 남기지 마.

로컬 소스·검사·문서와 별도 후보 DLL을 검토 가능한 상태로 만들고, commit·push·태그·Release 게시·이슈 종료는 별도 지시 전까지 하지 마. 탐색·검사 중 빌드 출력은 별도 후보 경로에 두고 원본 게임 참조를 수정하지 마. 과거 alpha.2 게시 승인을 이 작업에 전용하지 마.

첫 답변에서 현재 기준 확인 결과와 구현 순서를 짧게 알린 다음 실제 작업을 시작해줘. 진행하면서 필요한 구현 선택은 자율적으로 처리하고, 실제 접근 차단이나 상충하는 제품 요구가 있을 때만 질문해줘. 최종 보고에는 구현 범위·실행한 검사·후보 DLL 식별값·남은 실제 Lua/Windows 검증을 구분해줘. 미확인 완료 조건이 있으면 PRD 전체나 기능을 완료로 처리하지 마.
