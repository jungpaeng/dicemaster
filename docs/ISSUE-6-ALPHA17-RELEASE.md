# PRD #6 alpha.17 — 두루마리 아이콘 변경

<!-- alpha17-publication -->
2026-10-21 22:54:40 KST에 소스·주석 태그 [`d55a999`](https://github.com/jungpaeng/dicemaster/commit/d55a9998e65270626b66a76f30cd013c08881257)의 [alpha.17](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.17)를 게시했다. 전체·고정 커밋 재빌드 21개 프로그램·267개 입력·111개 소스/리소스·16개 참조·실제 DLL 연결·공개 ZIP 바이트 대조를 통과했다. 기존 58개 Release·59개 태그·자산·Latest v1.0.6·#6 open을 보존했다. **이번 DLL의 실제 Windows·물리 입력·가독성·프레임 성능 확인은 대기다.**
<!-- /alpha17-publication -->

## 변경 범위

alpha.16 배지의 금색 테두리·어두운 바탕·크기·장소 상단 중앙 핀/연결선·메뉴 모서리 배치를 유지하고, 내부 그림만 사용자가 제공한 Lucide `scroll-text`의 네 SVG path로 교체한다. 금색 선과 기존 중앙 정렬을 적용한다. 원본 SVG/PNG/RGBA를 갱신하고 ISC 저작권·허가·보증 부인 전문을 SVG metadata에 보존한다. 그 SVG도 DLL에 내장하므로 배포 사본에서 출처/라이선스를 확인할 수 있다. [이미지·출처 기록](STORY-BADGE.md).

표시 대상·현재 조건·대기 임무 우선순위·1회성 증명·불확실한 유학 경로 구분·설정·입력·배치·캐시 코드와 cfg는 변경하지 않는다. 실제 등록된 대기 임무의 현재 실행 연결 또는 엄격한 1회성 증명/현재 조건/해당 선택 경로만 배지의 근거다. 일반 대화 설명의 초록 상태는 유지하며, 배지 부재를 전체 이야기 없음으로 해석하지 않는다. [기능·지원 한계](ISSUE-6-ALPHA15-RELEASE.md), [배치·입력·이미지 재사용](ISSUE-6-ALPHA16-RELEASE.md)을 따른다.

런타임은 기존 RGBA 이미지를 한 번 업로드해 재사용한다. 라이선스용 원본 SVG는 렌더링/파싱하지 않는다. 추가 디코더·외부 DLL·사용자 설치 파일은 없다. 게임 판정/RNG/세이브/능력치/아이템을 바꾸지 않으며 정상 0.25초·실패 5초·숨김/비포커스/OFF 중단·정상 주기 Lua 재분석 없음과 패드 소유권/해제·중립 계약은 그대로다.

## 자동 검사와 실제 Windows 확인

테두리/외곽 바탕의 기존 픽셀 차이 0, 내부 그림만 변경, PNG와 Unity 하단 행 우선 RGBA의 모든 행 일치를 확인했다. 기존 8개 내장 리소스의 바이트와 새 이미지/라이선스 원본의 정확한 이름·해시를 검사한다. 전체 21개 검사 프로그램·267개 입력·111개 소스/리소스·16개 고정 참조와 실제 DLL 연결·설치 ZIP의 정확한 두 경로/CRC/바이트 검사를 통과했다. UI 5,382개·GUI/리소스 연결 121개·정보 런타임/연결 4,424개·메뉴 2,424개·지도 677개·패드 262,831개 검사를 포함한다. 검증 도구 자체 20개 검사와 git diff --check도 통과했다. 기존 컴파일 경고 7개·오류 0개다.

로컬 검토 증거는 artifacts/issue-6/lucide-story-icon/validated/build-manifest.json 및 build-run.log·image-check.json, 고정 커밋/공개 검증 증거: artifacts/releases/v1.0.7-alpha.17/release-verification.json 및 anonymous-verification.json. 모든 빌드 입력·21개 프로그램 성공 기록·DLL/cfg/ZIP은 검토 후보와 일치한다. 공개 ZIP의 두 경로·CRC·바이트·SHA-256도 익명 다운로드로 대조했다. 자동 검사와 실제 Unity/Windows 실행을 구분한다.

[Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md#alpha17--두루마리-아이콘-확인-대기). 지도 핀과 선택 메뉴 배지의 새 문양·축소 가독성·투명 테두리/바탕, 해상도/글자 크기, 패드/정지 마우스 소유권, 기존 입력과 프레임 성능은 새 후보로 확인해야 한다. 이전 후보/v1.0.6의 확인을 이 DLL의 Windows 통과로 바꾸지 않는다.

업데이트는 게임 종료 후 DLL만 교체해 cfg를 보존한다. 최초 설치는 ZIP의 config·plugins를 활협전용 BepInEx 모드 베이스 아래에 넣는다. 별도 이미지 설치는 없다. 문제 시 게임 종료 후 백업 DLL/cfg로 복구한다. 기존 alpha.16의 태그/릴리스/해시를 소급해서 바꾸지 않는다.

[설치 ZIP](https://github.com/jungpaeng/dicemaster/releases/download/v1.0.7-alpha.17/DiceMaster-v1.0.7-alpha.17.zip)의 파일 식별값(검토·고정 커밋·공개 다운로드 일치):

| 파일 | bytes | SHA-256 |
| --- | ---: | --- |
| DiceMaster.dll | 871424 | `5685ac42948ee4cd8a58dcdfbfd9543eab5a80e19c173fc958d3de7c3daf7ec7` |
| DiceMaster-v1.0.7-alpha.17.zip | 367039 | `718109b2cefcb89e08a20b43a75ca15bb2fec228c3dac0664cea85ede82815a9` |
| DiceMaster.cfg | 13078 | `a7d0d90884e032c69f8177b8193751a5543fe7b5fda7a66ab97dcffc01d6658e` |
