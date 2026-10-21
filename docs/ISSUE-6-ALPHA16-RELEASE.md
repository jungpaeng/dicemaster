# PRD #6 alpha.16 — 금색 이야기 배지와 지도 핀

<!-- alpha16-publication -->
2026-10-21 22:40:23 KST에 소스·주석 태그 [`f6f10d5`](https://github.com/jungpaeng/dicemaster/commit/f6f10d5aca59aaee04b1a7fd2b5f0f1858a1c06c)의 [alpha.16](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.16)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·267개 입력·111개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 57개 Release·58개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.**
<!-- /alpha16-publication -->

## 표시와 입력

기존 붉은 글자 `●`를 DiceMaster 자체 금색 두루마리 이미지로 교체한다. 당문 등 PositionController 장소 지도는 실제 선택 영역 상단 중앙에 작은 핀과 최대 7px 연결선을, 유학 등 실제 선택 메뉴는 버튼 우측 상단에 같은 배지를 표시한다. 크기는 기존 로그 글자 크기 12–24에 따라 20–28px로 제한하며 화면 경계 안에 둔다. 상단 경계에서 연결선이 뒤집혀 장소를 가로지르지 않도록 숨긴다. 설명창은 장소/버튼과 그 배지를 합한 영역 옆에 배치하며 내용/글자 크기/화면 변경 때만 다시 측정한다. 인접 장소나 원래 게임 텍스트와의 실제 겹침은 Windows에서 확인한다. 지도 전체의 장애물 탐색/배지 충돌 회피는 추가하지 않는다.

장소당 배지는 하나이며 여러 기회는 기존 설명의 최대 세 행과 나머지 수로 읽는다. 배지는 별도 버튼·포커스·입력 소비 대상이 아니다. 원래 패드/키보드 포커스 또는 움직인 마우스의 장소/버튼/배지 호버가 설명창 하나를 정한다. 정지 마우스보다 새 native 포커스를 우선하고 A/Enter/클릭/터치는 원래 게임 동작이다. L3/R3·해제/중립·Y 도구·오른쪽 스틱·B 복귀와 네 로그 탭/예약/이벤트 선택/주사위는 유지한다.

## 표시 기준·설정·읽기 전용 성능

alpha.15의 기준을 유지한다. 실제 등록된 대기 임무의 현재 실행 연결, 또는 엄격한 1회성 증명과 현재 조건·해당 선택의 도달 경로를 모두 확인한 항목만 배지를 붙인다. 일반 대화의 초록 상태·경로 설명은 남기되 배지의 근거로 쓰지 않는다. 선행 쓰기·미지원/불명 유학 경로를 양성으로 올리지 않으며, 표식 부재는 게임 전체의 기회 없음이 아니다. [alpha.15의 상세 지원 한계](ISSUE-6-ALPHA15-RELEASE.md)를 유지한다.

원본 SVG와 PNG는 src/DiceMaster/Resources/StoryBadge.*에 보존한다. tools/create-story-badge.cjs는 개발용 sharp로 64×64 PNG와 Unity용 하단 행 우선 RGBA 이미지를 만든다. 일반 소스 빌드는 이미 준비된 RGBA 파일을 DLL의 DiceMaster.StoryBadge.rgba로 내장한다. 게임에서 PNG를 디코딩하거나 새 Unity 모듈을 참조하지 않는다. 사용자 설치에 이미지/외부 DLL을 추가하지 않는다.

처음 유효한 Repaint에서 16,384바이트 이미지를 한 번 읽어 Texture2D에 올린 뒤 CPU 사본을 해제하고 같은 텍스처를 재사용한다. 글꼴·아이콘 생성·디스크 읽기·애니메이션은 정상 반복 표시에서 수행하지 않는다. 코드로 그리는 연결선은 Unity의 기존 whiteTexture를 쓴다. GUI.color를 복원하고 모드 종료 시 자체 텍스처를 해제한다. 이미지 읽기/업로드 실패는 한 번 보고하고 해당 모드 수명 동안 배지만 숨기며 독립 텍스트 설명을 유지한다.

새 설정·cfg·단축키는 없다. EnableMapGuide/EnableOneShotHints의 독립 동작, 상위 EnableOptionInfo, 정상 0.25초/실패 5초·숨김/비포커스/OFF 중단·정상 주기 Lua 재분석 없음·미선택 장소의 새 Lua 분석 없음 계약을 유지한다. 게임 조건/RNG/세이브/능력치/아이템/선택 상태를 바꾸지 않는다. UI 도형/자체 텍스처 변경은 게임 상태 변경이 아니다.

## 자동 검사·설치·Windows 확인

전체 21개 검사 프로그램·267개 입력·111개 소스/리소스·16개 참조와 실제 DLL 연결·설치 ZIP의 정확한 두 경로/CRC/바이트 검사를 통과했다. UI 5,382개·정보 런타임/연결 4,424개·메뉴 2,424개·지도 677개·패드 262,831개 검사를 포함한다. 검증 도구 자체 20개 검사와 git diff --check도 통과했다. 기존 컴파일 경고 7개·오류 0개이며 기존 8개 내장 리소스의 바이트를 보존했다. 공유 UI 검사 대체 코드와 기존 리소스 계약을 새 이미지에 연결하기 전 실패는 artifacts/issue-6/gold-story-pins/*failure.log에 따로 보존한다. 기존 8개 리소스의 이름/바이트를 보존하고 새 이미지까지 이름/해시로 확인하며, 최종 성공 기록과 혼용하지 않는다. 로컬 검토 증거: artifacts/issue-6/gold-story-pins/reviewed/build-manifest.json 및 build-run.log. 고정 커밋/공개 검증 증거: artifacts/releases/v1.0.7-alpha.16/release-verification.json 및 anonymous-verification.json. 모든 빌드 입력·21개 프로그램 성공 기록·DLL/cfg/ZIP은 검토 후보와 일치한다. 공개 ZIP의 두 경로·CRC·바이트·SHA-256도 익명 다운로드로 대조했다.

새 검사는 지도 상단 중앙/메뉴 모서리 분리·화면 경계/연결선·배지를 포함한 설명 회피·새 배지 호버·패드/정지 마우스 소유권·천 회 표시의 이미지 업로드 한 번·GUI 색 복원·종료 해제·실패 백 회의 시도 한 번·실제 DLL의 내장 리소스/이미지 렌더러 호출을 확인한다. 관리 코드 경계 검사는 Unity 렌더링·실제 Windows FPS 확인을 대신하지 않는다.

[Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha16--금색-지도-핀선택지-배지-확인-대기). 업데이트는 게임 종료 후 DLL만 교체해 기존 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins를 BepInEx 아래에 넣는다. 이전 후보/v1.0.6의 사용자 확인을 이번 DLL의 확인으로 대체하지 않는다.

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.16/DiceMaster-v1.0.7-alpha.16.zip)의 파일 식별값(검토·고정 커밋·공개 다운로드 일치):

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 869376 | `3a2824f4fcb130f0e3b00b07be916e706bf4ebf49e0c4d22e6bb4dff9d9b951b` |
| DiceMaster-v1.0.7-alpha.16.zip | 366156 | `b23cfb1ebc3d4412e1d16acb222f9ed730119cf8be7ed1909d1f2f9d17c3d505` |
| DiceMaster.cfg | 13078 | `a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e` |
