# v1.0.7-alpha.19 — 잠긴 후속 선택지 안내 제외

<!-- alpha19-publication -->
2026-10-22 01:06:52 KST에 소스·주석 태그 [`0b984ff`](https://github.com/jungpaeng/dicemaster/commit/0b984ff9345b1feca3831c4e25c4c2fa43f6046e)의 [alpha.19](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.19)를 게시했다. 전체·고정 커밋 재빌드 각각 21개 프로그램·271개 입력·112개 소스/리소스·16개 고정 참조와 실제 DLL 연결·설치 ZIP·익명 공개 다운로드 바이트 대조를 통과했다. 기존 60개 Release·61개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **새 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인과 #6 전체 완료는 대기다.** [설치·파일·검증 기록](ISSUE-6-ALPHA19-RELEASE.md).
<!-- /alpha19-publication -->

## 변경 내용

후속 선택지의 해금과 그 뒤 이야기의 발생 조건을 분리한다. 예를 들어 남제자방의 사사형 버튼이 존재해도 `예전에 부탁했던 일`의 해금을 확인하지 못했다면 그 주제는 자동 툴팁과 기본 상세 경로에 나오지 않는다. 다른 열린 주제가 있으면 그 경로를 사용하고, 안내할 경로가 없으면 툴팁을 숨긴다.

- 현재 활성 선택지 다음의 모든 선택 단계가 해금된 것으로 확인된 경로만 표시한다. 잠김·해금 미확정 경로와 추가 개수도 제외한다.
- 해금된 선택지 내부의 난수·미확정 이야기 조건은 별도 확정 거짓이 없으면 `진행 가능성 있음`으로 구분한다.
- 선택 메뉴 마커와 지도 이벤트 경로에도 같은 해금 확인을 추가한다. 이미 엄격했던 현재 대기 임무·1회성 증명·현재 조건·도달 경로 요건을 약화하지 않으며 일반 대화에는 마커를 붙이지 않는다.
- 기본 상세 로그에 아직 선택할 수 없는 주제 경로가 대량으로 남지 않도록 같은 해금 필터를 적용한다.

[설계·지원 범위·검사](ISSUE-6-CHOICE-UNLOCKS.md) · [실제 Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha19--후속-선택지-해금-확인-대기).

선행 상태 변경·동적 메뉴·자료 누락·조회 상한으로 해금을 확정하지 못한 주제는 실제로 가능하더라도 표시가 생략될 수 있다. 실제 하위 메뉴가 열리면 그 화면의 활성 선택지를 새 출발점으로 안내한다. 모든 이야기 탐지를 보장하지 않는다. 기존 입력·설정·캐시·정상 0.25초/실패 5초와 게임 상태 읽기 전용 계약을 유지한다.

## 자동 검사와 파일

검토 후보는 전체 21개 검사 프로그램·271개 입력·112개 소스/리소스·16개 고정 참조, 실제 게임 DLL 연결과 설치 ZIP의 정확히 두 경로·CRC·DLL/cfg 바이트 대조를 통과했다. 고정 커밋 재빌드에서도 21개 프로그램이 모두 통과했으며 전체 입력·참조·DLL/cfg/ZIP이 검토 후보와 일치했다. 익명 공개 ZIP의 두 경로·CRC·내부 바이트·SHA-256도 확인했다. 선택지 3,932개·지도 677개·경로 6,882개·지도/경로 연결 3,347개·UI/입력 5,449개·정보 런타임 4,551개·기존 패드 262,831개 검사를 포함한다. 새 DLL의 실제 Windows·Unity 렌더링·물리 입력·프레임 성능은 확인하지 않았다.

| 파일 | 바이트 | SHA-256 |
| --- | ---: | --- |
| DiceMaster-v1.0.7-alpha.19.zip | 368441 | 9bc6ee042c354d531fbef01ab80e5e21a0c376502a275fd6cf0ae6c83272ac86 |
| DiceMaster.dll | 875520 | 170ec23277a4033bd8dc698c5e4585e312fd9bd4a9023721b9d28550fe38f12e |
| 기존 기본 cfg (변경 없음) | 13078 | a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e |

Release `419769432`·asset `663048250`, draft=false·prerelease=true·make_latest=false다. [설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.19/DiceMaster-v1.0.7-alpha.19.zip)을 공개했다.

지도 통합 검사의 단언 집계는 검토 3,347개/고정 재빌드 3,346개이며 둘 다 통과했다. 초기 준비 완료까지 반복하는 `Read()`가 매번 소유권 단언 한 개를 추가하므로 슬라이스 횟수에 따라 집계가 달라진다. 원본 단언을 바꾸지 않은 임시 계측에서도 8슬라이스/3,348개를 확인했다. 20개 프로그램의 성공 기록은 정확히 같고, 이 프로그램의 한 개 집계 차이만 `reviewed-candidate-comparison.json`·별도 counter-audit.json에 명시했다. 파일이나 코드 입력 차이로 취급하지 않으며 전체 검사 기록이 숫자까지 동일하다고 주장하지 않는다.

로컬 검토 후보는 artifacts/issue-6/choice-unlocks/candidate/의 build-manifest.json·build.log와 상위 verification.json이다. 고정 커밋·공개 파일 증거는 artifacts/releases/v1.0.7-alpha.19/의 release-verification.json·reviewed-candidate-comparison.json·tag-verification.json·anonymous-verification.json에 보존한다. 공개 검증은 이전 릴리스/태그/자산·Latest·이슈 상태도 대조한다.

## 설치와 실제 Windows 확인

게임을 종료하고 DLL/cfg를 백업한 뒤 업데이트는 **plugins/DiceMaster/DiceMaster.dll만 교체해 기존 cfg를 보존**한다. 최초 설치는 설치 ZIP의 config·plugins를 기존 활협전용 BepInEx 모드 베이스 아래에 넣는다. 문제 시 게임 종료 후 백업 DLL/cfg를 복원한다. GitHub 자동 Source code 압축 파일은 설치용이 아니다.

남제자방에서 잠긴 주제 제외·다른 열린 주제 유지·실제 하위 메뉴 진입 후 안내, 해금/완료 뒤 툴팁·상세·개수·마커의 동시 갱신, 정상 대기 임무·1회성 마커, 해금된 선택지 내부의 난수 분기, 패드/키보드/포인터/터치와 게임 창 가림·최초/반복 프레임을 확인한다. 이슈 #6은 종료하지 않는다.
