namespace DiceMaster.Information
{
    // Published display data. Contains no executable action or reservation target.
    public sealed class InformationRow
    {
        public readonly string Id, Title, Kind, Status, Detail, AutoText;
        public readonly object Anchor;
        public readonly bool Available;
        public readonly bool Opportunity;
        public InformationRow(string id, string title, string kind, string status, string detail,
            string autoText = null, object anchor = null, bool available = false, bool opportunity = false)
        {
            Id = id ?? ""; Title = title ?? Id; Kind = kind ?? ""; Status = status ?? "";
            Detail = detail ?? ""; AutoText = autoText ?? (Title + " · " + Kind + " · " + Status);
            Anchor = anchor; Available = available; Opportunity = opportunity;
        }
    }
}
