# alpha.37 — 카드 선택지의 실제 이름과 시각 영역

## 원인과 근거

사용자 스크린샷 IMG_2914.PNG에서 준비 스피너가 휴식 카드 위에 떠 있고, 사사형 안내가 휴식 설명을 덮었다. 기존 InformationLayout.Badge는 모든 선택지에 루트 버튼 우상단을 사용했고 Near는 선택한 루트만 피했다.

제공받은 읽기 전용 게임 복제본 Mortal_Data/level2의 PositionOptionButton0(MonoBehaviour pathID26403, RectTransform18858)은431×141이지만 제목 변형의 Title은240×80이고 부모 변형 기준(453,-133)에 있다. 제목이 루트의 오른쪽 밖으로 나간다. 그림·설명도 별도 참조다. BreakOptionButton(pathID25537, RectTransform18635)은450×55이며 제목 부모 변형에(127,0) 오프셋이 있다. 두 유형에 루트 모서리 규칙을 공통 적용하면 실제 이름과 표시 위치가 어긋난다.

Mortal.Core.PositionOptionButton/BreakOptionButton의 _titleText와 그림/설명 필드, MenuToggleButton의 _normal/_enable/_hover/_disable은 표시 변형 GameObject.SetActive로 전환된다. UnityEngine.UI.Text.OnPopulateMesh는 이미 생성한 m_TextCache.verts를 pixelsPerUnit과 PixelAdjustPoint로 보정해 그린다. 이 구현을 따라 현재 활성 글자의 잉크 경계를 읽으며 이름 글자 수/RectTransform 폭으로 위치를 추측하지 않는다. 조사/측정/실패·최종 검사 자료는 ignored artifacts/issue-6/card-anchors/에 남겼다. 게임 자산/소스/DLL은 배포하지 않는다.

## 구현과 표시 규칙

- InformationCardAnchors는 두 카드 타입에만 적용한다. 활성 _titleText의 기존 m_TextCache 정점(최대1024개/표시 변형12개)에서 공백/종료용 퇴화 사각형을 제외하고 실제 글자 경계를 화면 좌표로 투영한다. 기존 캐시가 없으면 이름이 렌더링될 때까지 마커/스피너를 숨긴다. cachedTextGenerator getter·Populate·preferredWidth/Height·SetVerticesDirty/SetLayoutDirty는 호출하지 않는다.
- 이름 왼쪽8px 간격·글자 세로 중심에 기존32 기준 배지를 놓는다. 좌측 여백이 없으면 오른쪽을 사용하고 화면 안으로 제한한다. 아이보리/금색 마커, 지연 준비 스피너, 마커 위 마우스 판독이 같은 TryBadgeRect를 사용한다. 스피너는 그 공통 사각형의65% 크기로 중심을 맞춘다. 당문 지도 핀과 일반 선택지는 기존 규칙이다.
- ChoiceStorySnapshot.Anchors는 기존 예약 조회에서 관찰한 실제 버튼을 수집한다. 이야기/마커가 없거나 조작 불가여도 보이는 카드는 장애물이다. 숨겨진 카드/다른 메뉴는 포함하지 않는다. 렌더러가 버튼을 발견하기 위해 씬을 검색하거나 이야기 계산을 추가하지 않는다.
- 카드 장애물은 루트와 활성 이름/종류/그림/설명·행동력/기여 표시의 경계를 합친다. 카드 툴팁에 실제 선택 이름을 넣고, 선택 카드 아래→전체 카드 아래/외곽 등 고정8개 후보를 검사한다. 최대64개 보이는 카드와12px 간격을 지키며 전부 막히면 툴팁을 숨긴다. 기존 상세 로그는 유지한다. 지도 툴팁·분기 알림은 기존 배치다.
- 같은 Draw의 카드 기하를 재사용하며 finally에서 캐시를 비운다. 이전 프레임의 포커스 표시·해상도/Transform을 재사용하지 않는다. 원천 조회의 visibility 검사에는 이 단기 캐시가 개입하지 않는다. 클릭 가능한 새 UI·레이캐스트·포커스 이동·입력 소비를 추가하지 않는다.

## 검증과 한계

LogUiKeyboardTests는 실제 표시 소스와 명시적 Unity/game 경계로 스크린샷 배치(1920×1200), 루트 밖 제목/그림/설명, 활성 변형·글자 배율·퇴화 정점, 이름 옆 스피너/마커 공통 중심, 안내 없는 옆 카드 회피, 공간 없음/장애물 상한 숨김, 해상도별 배치, 웜1000회 측정/업로드 불변·입력 불변을 검사한다. ChoiceStoryTests는 빈/조작 불가/숨김 버튼 수집을 확인한다. 실제 DLL IL 검사는 공통 기하 연결·기존 텍스트 캐시 읽기·생성/레이아웃 호출 없음·finally 수명·게임/입력 쓰기 없음을 확인한다.

고정 참조와 게임 복제본 DLL 연결의 전체21개 검사 모두 통과하고 동일한 DLL을 생성했다. alpha.36에서 수행한 관리 기하 참고 측정은3/16/64카드 각각10,000프레임, p95 0.011458/0.095750/0.045833ms, 웜 할당0byte/프레임이었다. JIT/스케줄링과 모형의 영향이 있으므로 실제 Unity 텍스트/Transform/카메라/GPU 비용이나 Windows FPS를 보증하지 않는다. 주기·Lua 파싱·현재 조건 조회·캐시 정책을 변경할 이유를 이 측정에서 찾지 못했다. 실제 게임의 시각 배치와 성능은 Windows 확인이 필요하다.

안내 대상/금색·아이보리 의미/반복 활동 제외/해금·완료·랜덤 AND의 거짓 우선 처리와 안전한1회성 증명은 바꾸지 않는다. 기존 설정·cfg·0.25초/5초·읽기 전용·숨김 조회 중단·웜 Lua 재분석 없음·#5와 물리 입력 보호를 유지한다. [후보·게시](ISSUE-6-ALPHA37-RELEASE.md) · [Windows 확인표](ISSUE-6-WINDOWS-VALIDATION.md).


## alpha.37 — 같은 그리기 안의 루트 좌표 재사용

alpha.36 게시 후 검토에서 DrawBadge와 Focused가 기존 TryRect 결과를 얻은 뒤 공통 TryBadgeRect를 통해 같은 루트를 다시 투영하는 것을 확인했다. TryRect의 실제 계산을 ComputeRect로 분리하고, 기존 카드 기하와 같은 BeginPaint/EndPaint 수명 안에서 성공/실패 루트 사각형도 재사용한다. finally에서 두 캐시를 비우므로 다음 그리기·해상도/포커스/좌표 변경과 원천의 예약 visibility 조회는 새 기하를 읽는다. 캐시는 조건/해금/마커 판정 결과를 보관하지 않는다.

자동 검사에서 같은 루트의20회 요청은 GetWorldCorners1회였고, 그리기 밖2회 조회는 각각 다시 읽었다. 다음 그리기에서도 이동한 좌표를 새로 읽었다. 같은 생산 코드의 비캐시/그리기 재사용 경로를 대조한10,000프레임 관리 모형은3루트 p95 0.026708→0.006417ms,64루트 0.075916→0.045833ms였으며 웜 추가 할당은 모두0byte/프레임이었다. 이는 반복 요청을 강조한 대조 모형이며 실제 게임의 일반 호출 횟수나 Unity/native/FPS 개선 폭을 뜻하지 않는다. 기록은 artifacts/issue-6/card-anchor-reuse/root-benchmark.json에 있다.

alpha.37에서도 고정 참조/게임 복제본 DLL 연결의 전체21개 검사가 통과하고 같은 DLL을 생성했다. 설정/cfg·0.25초/5초·준비 수명·표시 범위/판정·카드 이름 옆 배치·형제 카드 회피·입력 보호는 그대로다. [최종 후보·게시](ISSUE-6-ALPHA37-RELEASE.md)의 새 DLL은 실제 Windows 확인 대기다. alpha.36과 이전 배포는 보존한다.
