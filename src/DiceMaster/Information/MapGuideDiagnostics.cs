using System;
using System.Text;

namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        // The remembered target is for explicit detail only. Deselect still
        // removes the live overlay; load/scene/OFF invalidation forgets it.
        private static Place DetailPlace()
        {
            if (Selected != null) return selectedPlace;
            var controller = detailPlace == null ? null : Live(detailPlace);
            return OnMap(controller) ? detailPlace : null;
        }
        private static string DiagnosticPlaceName(Place place)
        {
            if (place == null) return "없음";
            return string.IsNullOrEmpty(place.Key) ? "장소 #" + place.Id + " · 이름 미조회" : Plain(LocationTranslations.TranslateLocation(place.Key.Replace("養成_", "")));
        }
        // Cache/handle inspection only: no Discover/GetSource/reader, resource
        // lookup, Lua preparation or gameplay call. This survives a failed
        // place query without retrying that query outside RefreshEngine.
        public static InformationRow DiagnosticRow()
        {
            var text = new StringBuilder();
            try
            {
                Place target = DetailPlace();
                text.Append("현재 지도 포커스: ").Append(DiagnosticPlaceName(Selected == null ? null : selectedPlace));
                text.Append("\n상세 조회 대상: ").Append(DiagnosticPlaceName(target));
                if (target != null && Selected == null) text.Append(" · 마지막 선택 장소");
                if (target == null) text.Append("\n장소를 포커스한 뒤 로그를 열면 마지막 선택 장소의 상세를 조회합니다.");
                if (detailFailure != null) text.Append("\n마지막 상세 조회 실패: ").Append(detailFailure).Append(" · 실패 행과 재시도 상태를 함께 확인하세요.");
                text.Append("\n등록된 장소: ").Append(places.Count).Append("개 · 장소 목록을 새로 검색하지 않습니다.");
                Source source = target == null ? null : target.Definition;
                text.Append("\n대상 경로 자료: ").Append(target == null ? "상세 대상 없음" : source == null ? "미준비 또는 메타데이터 조회 전" :
                    source.Failure != null ? "읽기 실패 · " + source.Failure.GetType().FullName : source.Routes == null ? "경로 정의 없음" :
                    source.Routes.IsPreparing ? "준비 중" : source.Routes.HasReadFailure ? "경로 읽기 실패" : source.Routes.IsPartial ? "일부 확인 불가" : "준비 완료");
                text.Append("\n최근 장소 조회: 경로 현재값 ").Append(routeReads).Append("회 · 효과 분석 시작 ").Append(effectStarts)
                    .Append("회 · 정적 구문 재사용 ").Append(source == null || source.Routes == null ? 0 : source.Routes.SharedSyntaxHits).Append("회(대상 경로 누적)");
            }
            catch (Exception error) { text.Append("\n등록된 지도 상태 조회 실패: ").Append(error.GetType().FullName); }
            text.Append('\n').Append(MapDefinitionAssets.Describe());
            text.Append('\n').Append(timings[0].Describe("자동 · 대기 임무"));
            text.Append('\n').Append(timings[1].Describe("자동 · 1회성 포함"));
            text.Append('\n').Append(timings[2].Describe("명시적 상세"));
            text.Append("\n상태·실패 이유는 별도 행도 함께 확인합니다. 미조회는 자료 없음의 확정이 아닙니다. 등록된 장소 조회 표본이며 전체 배경·프레임 시간은 아닙니다. 메타데이터·진단문·UI 비용은 시간 표본에 포함하지 않습니다. 준비는 협력식 약5ms 예산이며 단일 동기 작업 시간을 보장하지 않습니다.");
            return new InformationRow("map/performance/" + ContextKey, "지도 조회 성능", "자료 상태", "현재 자료 상태·최근 실행 표본", text.ToString(), "");
        }
    }
}
