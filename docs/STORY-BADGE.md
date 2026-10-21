# 금색 이야기 배지 이미지

alpha.16의 금색 원형 테두리·어두운 바탕·크기·지도 핀/메뉴 모서리 배치는 유지한다. 안쪽 그림만 사용자가 제공한 Lucide `scroll-text` SVG의 네 path로 교체한다. 24×24 원본을 배지 중앙에 맞춰 확대하고 기존 금색 선을 사용한다. 판정·입력·위치·캐시 코드는 변경하지 않는다. 기존 공개 alpha.16의 이미지/해시를 소급해서 바꾸지 않는다.

원본: [StoryBadge.svg](../src/DiceMaster/Resources/StoryBadge.svg). 개발용 tools/create-story-badge.cjs로 PNG·Unity 하단 행 우선 RGBA를 갱신한다. 런타임은 기존과 같이 RGBA 이미지를 한 번 업로드해 재사용한다.

안쪽 그림 출처: [Lucide scroll-text](https://lucide.dev/icons/scroll-text). [ISC 라이선스](https://lucide.dev/license)의 저작권·허가·보증 부인 전문을 원본 SVG의 metadata에 보존하고, 그 SVG도 DLL에 내장해 배포 사본에 포함한다. 추가 디코더나 외부 파일 설치는 필요 없다. 테두리와 바탕은 DiceMaster 자체 그림이다.

원본과 비교한 테두리/외곽 바탕 픽셀 차이는 0이며, 새 PNG와 내장 RGBA의 모든 행이 일치한다. 실제 참조 DLL을 연결한 소스 빌드(기존 경고 7개·오류 0개), 갱신 연결 23개·GUI/리소스 바이트 연결 121개 검사와 git diff --check를 통과했다. 검사 기록은 artifacts/issue-6/lucide-story-icon/에 보존한다. 이어 전체 21개 검사 프로그램·실제 DLL 연결·설치 ZIP 검사를 통과했다. [alpha.17 검증/게시 기록](ISSUE-6-ALPHA17-RELEASE.md)에 전체 결과를 기록한다. 실제 Windows 실행은 별도다. 새 이미지의 실제 Windows 가독성 확인은 대기다. [alpha.17](https://github.com/jungpaeng/dicemaster/releases/tag/v1.0.7-alpha.17)로 게시했다. 설치 파일·소스·해시는 [게시 기록](ISSUE-6-ALPHA17-RELEASE.md)을 따른다.
