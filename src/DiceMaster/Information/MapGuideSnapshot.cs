using System.Collections.Generic;

namespace DiceMaster.Information
{
    // One scheduled read owns both surfaces; rendering never discovers entries.
    public sealed class MapGuideSnapshot
    {
        public InformationRow Summary;
        public readonly List<InformationRow> Entries = new List<InformationRow>();
        public bool HasSelection;
        public string ContextKey;
    }
}
