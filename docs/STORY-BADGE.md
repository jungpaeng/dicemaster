# 금색 이야기 배지 이미지

## alpha.29 — 아이보리 안내 변형

<!-- alpha29-publication -->
2026-10-22 15:04:05 KST에 소스 [`0bee1e2`](https://github.com/jungpaeng/dicemaster/commit/0bee1e26dcc857ca87b7da0b92967befde8eafd4)의 [alpha.29](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.29)를 게시했다. 전체·고정 커밋 자동 검사21개·실제 후보 DLL 연결·설치 ZIP과 익명 공개 다운로드 검증을 통과했다. **새 DLL의 실제 Windows·가독성·물리 입력·프레임 확인과 PRD #6 전체 완료는 대기다.**
<!-- /alpha29-publication -->

[StoryGuideBadge.svg](../src/DiceMaster/Resources/StoryGuideBadge.svg)는 같은 Lucide 두루마리·원형 틀에 아이보리 바탕과 짙은 갈색 선을 적용한 안내용 변형이다. 기존 StoryBadge 파일은 바꾸지 않는다. 지도/메뉴 위치·크기를 공유하고 기회 배지를 우선한다. 두 RGBA/라이선스 SVG를 DLL에 내장하며 각 텍스처를 필요할 때 한 번 업로드하고 종료 때 둘 다 해제한다. 개발용 생성기는 두 파일을 기본 처리하며 `StoryGuideBadge` 인자로 새 변형만 생성할 수 있다. [표시 의미·검증 상태](ISSUE-6-GUIDANCE-BADGES.md).

아래는 기존 어두운 금색 배지의 게시 이력이다.

alpha.16의 금색 원형 테두리·어두운 바탕·크기·지도 핀/메뉴 모서리 배치는 유지한다. 안쪽 그림만 사용자가 제공한 Lucide `scroll-text` SVG의 네 path로 교체한다. 24×24 원본을 배지 중앙에 맞춰 확대하고 기존 금색 선을 사용한다. 판정·입력·위치·캐시 코드는 변경하지 않는다. 기존 공개 alpha.16의 이미지/해시를 소급해서 바꾸지 않는다.

원본: [StoryBadge.svg](../src/DiceMaster/Resources/StoryBadge.svg). 개발용 tools/create-story-badge.cjs로 PNG·Unity 하단 행 우선 RGBA를 갱신한다. 런타임은 기존과 같이 RGBA 이미지를 한 번 업로드해 재사용한다.

안쪽 그림 출처: [Lucide scroll-text](https://lucide.dev/icons/scroll-text). [ISC 라이선스](https://lucide.dev/license)의 저작권·허가·보증 부인 전문을 원본 SVG의 metadata에 보존하고, 그 SVG도 DLL에 내장해 배포 사본에 포함한다. 추가 디코더나 외부 파일 설치는 필요 없다. 테두리와 바탕은 DiceMaster 자체 그림이다.

원본과 비교한 테두리/외곽 바탕 픽셀 차이는 0이며, 새 PNG와 내장 RGBA의 모든 행이 일치한다. 실제 참조 DLL을 연결한 소스 빌드(기존 경고 7개·오류 0개), 갱신 연결 23개·GUI/리소스 바이트 연결 121개 검사와 git diff --check를 통과했다. 검사 기록은 artifacts/issue-6/lucide-story-icon/에 보존한다. 이어 전체 21개 검사 프로그램·실제 DLL 연결·설치 ZIP 검사를 통과했다. [alpha.17 검증/게시 기록](ISSUE-6-ALPHA17-RELEASE.md)에 전체 결과를 기록한다. 실제 Windows 실행은 별도다. 새 이미지의 실제 Windows 가독성 확인은 대기다. [alpha.17](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.17)로 게시했다. 설치 파일·소스·해시는 [게시 기록](ISSUE-6-ALPHA17-RELEASE.md)을 따른다.

표시 크기와 원래 게임 창의 가림 처리는 [후속 alpha.18](ISSUE-6-ALPHA18-RELEASE.md)를 따른다. 아이콘 그림은 alpha.17과 같다.
