using System;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static void MapLogDiagnosticCases()
    {
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); PreviewConditions.ClearTalkContext();
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        CheckPointManager.Instance = new CheckPointManager(); LuaManager.Instance = null;
        var flag = new FlagData { name = "log_gate", State = 0 };
        CheckPointManager.Instance._condition._list.Add(new ConditionResultData { name = "log_gate",
            _items = new System.Collections.Generic.List<ConditionResultItem> { IsZero(flag) } });
        DataHelper.Scripts["log_room"] = "local a={};a[1]=checkpointmanager.Condition('log_gate') and '사사형' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_log_intro')) end";
        var place = new PositionController { _positionData = new FreePositionData { name = "LogRoom", _defaultScript = "log_room" } };
        MapGuideSource.Register(place);
        int resources = DataHelper.Reads, searches = Resources.Searches;
        var snapshot = Read(true);
        Check(snapshot.Entries.Any(row => row.Title == "지도 조회 성능" && row.Detail.Contains("상세 조회 대상: 없음", StringComparison.Ordinal)) && DataHelper.Reads == resources,
            "registered no-focus maps expose diagnostics without starting a place's Lua preparation");
        MapGuideSource.Select(place);
        for (int at = 0; at < 40; at++) snapshot = Read(true);
        Check(snapshot.Entries.Any(row => row.Title == "사사형") && snapshot.Entries.Any(row => row.Title == "지도 조회 성능"),
            "focused original provider exposes both the story and its diagnostic row");
        resources = DataHelper.Reads;
        MapGuideSource.Deselect(place);
        Check(!MapGuideSource.Visible && MapGuideSource.SelectedAnchor == null, "remembered log targets never remain an automatic focus or overlay anchor");
        snapshot = Read(false);
        Check(!snapshot.HasSelection && snapshot.Summary == null && !snapshot.Entries.Any(row => row.Title == "사사형"),
            "passive no-focus queries retain the original global scope");
        snapshot = Read(true);
        Check(snapshot.Entries.Any(row => row.Title == "사사형") && snapshot.Entries.Any(row => row.Title == "지도 조회 성능" && row.Detail.Contains("마지막 선택 장소", StringComparison.Ordinal)),
            "opening explicit detail after deselection still queries the last live place and identifies that target");
        Check(!MapGuideSource.IsCurrent(snapshot.Summary) && DataHelper.Reads == resources && Resources.Searches == searches,
            "remembered detail is not a current overlay and adds neither warm Lua reads nor native asset scans");
        flag.State = 1; snapshot = Read(true);
        Check(!snapshot.Summary.Guidance && !snapshot.Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal),
            "last-place detail resamples today's conditions rather than retaining the old eligible story");
        flag.State = 0;
        var pending = MapGuideSource.Discover(true, true, true)[0];
        MapGuideSource.ClearSnapshots();
        try { pending.Read(); Check(false, "a pre-load detail request must be rejected"); } catch (DataNotReadyException) { checks++; }
        snapshot = Read(true);
        Check(!snapshot.HasSelection && !snapshot.Entries.Any(row => row.Title == "사사형") &&
            snapshot.Entries.Any(row => row.Title == "지도 조회 성능" && row.Detail.Contains("상세 조회 대상: 없음", StringComparison.Ordinal)),
            "load/scene/OFF cleanup forgets the previous log target while retaining no-focus diagnostics");
        MapGuideSource.Select(place); MapGuideSource.Deselect(place);
        place._positionData = null;
        try { Read(true); Check(false, "metadata read failure must remain a failed query"); } catch (DataNotReadyException) { checks++; }
        var diagnostic = MapGuideSource.DiagnosticRow();
        Check(diagnostic.Detail.Contains("마지막 상세 조회 실패: DiceMaster.LiveLog.DataNotReadyException", StringComparison.Ordinal) &&
            diagnostic.Detail.Contains("로드된 설정 자산", StringComparison.Ordinal),
            "cache diagnostics survive a metadata exception without converting the failed place read to healthy");
        place.gameObject.activeInHierarchy = false;
        Check(MapGuideSource.Discover(true, true, true).Count == 0 && MapGuideSource.DiagnosticRow().Detail.Contains("상세 조회 대상: 없음", StringComparison.Ordinal),
            "inactive remembered places are not revived for detail; the diagnostic row can still report no target");
        MapGuideSource.Clear(); diagnostic = MapGuideSource.DiagnosticRow();
        Check(diagnostic.Detail.Contains("등록된 장소: 0개", StringComparison.Ordinal) && !diagnostic.Detail.Contains("마지막 상세 조회 실패", StringComparison.Ordinal),
            "full cleanup removes both remembered target and failure context");
        Check(!diagnostic.HasBadge && !diagnostic.Available && diagnostic.Anchor == null && diagnostic.AutoText == "" && Counters.GameCalls == 0 && Resources.Searches == searches,
            "diagnostics carry no automatic marker/input authority and call neither gameplay nor native asset discovery");
        GC.KeepAlive(place); MapGuideSource.MarkerVisible = null;
    }
}
