# PRD #6 alpha.18 — 툴팁 조건 수정·배지 표시 개선

<!-- alpha18-publication -->
2026-10-22 00:14:28 KST에 소스·주석 태그 [`0d7d03d`](https://github.com/jungpaeng/dicemaster/commit/0d7d03d51b4e4cbcd7aadce2269e8570994ef3b3)의 [alpha.18](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.18)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·270개 입력·112개 소스/리소스·16개 고정 참조·실제 DLL 연결·익명 공개 ZIP 바이트 대조를 통과했다. 기존 59개 Release·60개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.**
<!-- /alpha18-publication -->

## 변경과 사용

- 안전하게 확인한 현재 필수 조건이 거짓이면 선택지의 인물 대화 자동 툴팁을 숨긴다. 난수·무관한 플래그 변경·미완료 경로가 거짓을 `진행 가능성 있음`으로 덮지 않게 한다. 불가 사유와 관찰한 경로는 로그 상세에 남고 실제 단련/대화 선택은 유지된다.
- 표시 인물·선택 경로·상태·추가 이야기 수를 같은 판정 근거에서 고른다. 다른 인물의 가능한 대화를 진행 불가 인물 이름에 붙이지 않는다. 같은 상태 안의 후속 선택 경로 안내는 유지한다.
- 배지를 로그 글자 크기와 분리해 1920×1200 기준 지도 40px·선택 메뉴 32px로 확대하고 화면에 맞춰 조정한다. 금색 테두리·어두운 바탕·요청한 Lucide 두루마리 문양은 유지한다.
- 게임의 상태·설정·저장·대사 로그 창이 열리면 뒤에 있는 지도/선택 배지·설명과 관련 조회를 멈추고, 닫으면 현재 자료로 갱신한다. 정상 행동 진행 중의 선택 메뉴는 허용한다.

실제 대기 임무·엄격한 1회성의 현재 실행 연결만 기회 표식의 근거다. 일반 대화 경로만으로 표식을 부여하지 않으며 표식 부재가 전체 이야기 없음을 뜻하지 않는다. 조건에 영향을 주는 선행 변경·임의 콜백·현재 메뉴 위치 미확정은 보수적으로 남긴다. 인물/플래그 예외 목록이나 새 설정 키는 없다. [툴팁 설계와 한계](ISSUE-6-TOOLTIP-PREREQUISITES.md), [배지·창 가림 설계](ISSUE-6-BADGE-VISIBILITY.md).

정의 색인의 공통 AND 필수 조건과 기존 캐시를 재사용하고 32개 개별 경로·64셀 색인 예산을 유지한다. 정상 0.25초/실패 5초·숨김/OFF 중단·정상 주기 Lua 재분석 없음과 패드/키보드/마우스/터치·소유권/해제/중립 계약을 유지한다. 게임 판정/RNG/세이브/플래그/능력치/아이템을 바꾸지 않는다.

## 자동 검사와 Windows 확인

검토 후보의 전체 소스 빌드는 21개 검사 프로그램·270개 입력·112개 소스/리소스·16개 고정 참조 및 실제 DLL 연결·읽기 전용 경계를 통과했다. 선택지 3,578개·UI/입력 5,449개·정보 런타임/연결 4,509개·지도 677개·경로 6,882개·패드 262,831개 검사를 포함한다. 기존 경고 7개·오류 0개다. 설치 ZIP의 정확한 두 경로·CRC·DLL/cfg 바이트도 확인했다.

원본 연무장 Lua와 SHA를 확인한 후속 자료에 합성 조건을 연결해 거짓 안내를 검사했다. 이는 사용자의 세이브·실제 직렬화된 조건 설정 또는 Windows/Unity 실행을 대신하지 않는다. [새 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha18--툴팁배지게임-창-확인-대기)의 항목은 모두 대기다. 우소매 부재/존재, 다른 대화의 인물/경로 일치, 패드·포인터·로그 복귀, 배지 크기·게임 창 가림, 최초/반복 프레임을 확인해야 한다. 이슈 #6은 종료하지 않는다.

로컬 검토 기록: artifacts/issue-6/tooltip-prerequisites/validated/build-manifest.json 및 build-run.log·verification.json. 고정 커밋/공개 증거는 artifacts/releases/v1.0.7-alpha.18/release-verification.json·reviewed-candidate-comparison.json·anonymous-verification.json에 보존한다. 모든 빌드 입력·21개 성공 기록·DLL/cfg/ZIP은 검토 후보와 일치하며 공개 ZIP의 두 경로·CRC·바이트·SHA-256을 익명 다운로드로 대조했다.

## 설치 파일

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.18/DiceMaster-v1.0.7-alpha.18.zip)의 식별값(검토·고정 커밋·공개 다운로드 일치):

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.18.zip | 367975 | 4f3544a5a582da4eeaaca256573822234bec9a70512d6d665fc53756a4d709d8 |
| DiceMaster.dll | 873984 | 5ef65685234ae50b4b0a98364391a4c0ead05a3229e640ffd8ef4e8c76bf93a2 |
| 기본 DiceMaster.cfg | 13078 | a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e |

게임을 종료하고 DLL/cfg를 백업한 뒤 업데이트는 **plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 ZIP의 config·plugins를 활협전용 BepInEx 모드 베이스 아래에 넣는다. 문제 시 게임 종료 후 백업한 DLL/cfg로 복구한다. GitHub 자동 Source code 압축은 설치용이 아니다. 실제 Windows 확인이 끝나기 전 이 후보를 정식 안정판 또는 PRD #6 완료로 취급하지 않는다.
