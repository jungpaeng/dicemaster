using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DiceMaster.LiveLog
{
    // Presentation only. Never expand GetFlag/GetDevelop expressions or invoke
    // gameplay singleton getters to resolve a decorative name.
    public static class DisplayNames
    {
        public static int PendingNames { get; private set; }
        private static object Field(object value, string name)
        { try { return value == null ? null : LogBridge.ReadFieldStrict(value, name); } catch (Exception) { return null; } }
        private static string String(object value) { return Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; }
        private static string Name(object value) { return String(Field(value, "name")); }
        private static string Helper(string method, params object[] args)
        { return (string)LogBridge.CallDisplayHelper("DiceMaster.DataHelper", method, args); }

        public static string Text(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            try { return (string)LogBridge.CallDisplayHelper("DiceMaster.UiTextMapper", "TranslateLiteral", value) ?? value; }
            catch (Exception) { return value; }
        }
        public static void RefreshMappings()
        {
            try { LogBridge.CallDisplayHelper("DiceMaster.UiTextMapper", "RefreshNameMappings"); }
            catch (Exception) { } // Names cannot invalidate a judgment or an action.
        }
        public static string Locale(string key, string fallback)
        {
            try { return Helper("GetLocalizedSystemText", key, fallback, "표시 이름", key) ?? fallback; }
            catch (Exception) { return fallback; }
        }
        public static string LookupName(string key, string id)
        {
            string value = Locale(key, id);
            if (string.IsNullOrEmpty(value) || value == id)
            { PendingNames = unchecked(PendingNames + 1); return Text(id); }
            return Text(value);
        }
        private static string EnumKey(object value)
        {
            if (value == null || !value.GetType().IsEnum) return "";
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null)
                foreach (CustomAttributeData attribute in CustomAttributeData.GetCustomAttributes(field))
                    if (attribute.AttributeType.FullName == "OBB.Framework.Attributes.StringValueAttribute" &&
                        attribute.ConstructorArguments.Count == 1)
                        return attribute.ConstructorArguments[0].Value as string ?? "";
            return "";
        }
        private static string Flag(object flag, string id, int? state)
        {
            string translated = state.HasValue ? Locale("Flag/Desc/" + id + "_" + state.Value, "") : "";
            if (string.IsNullOrWhiteSpace(translated)) translated = Locale("Flag/Name/" + id, "");
            if (string.IsNullOrWhiteSpace(translated))
            {
                string note = String(Field(flag, "_devNote"));
                if (string.IsNullOrEmpty(note)) note = PreviewState.FlagDevNote(id) ?? "";
                string literal = Text(note);
                // A frequent literal flag note is a verb prefix plus a story
                // title. Translate its subject without evaluating expressions.
                if (note.StartsWith("觸發", StringComparison.Ordinal) && note.Length > 2 && literal.StartsWith("觸發", StringComparison.Ordinal))
                    literal = Text(note.Substring(2).Trim()) + " 발동 여부";
                if (!string.IsNullOrWhiteSpace(literal) && literal != note) translated = literal;
            }
            if (string.IsNullOrWhiteSpace(translated))
            { if (PreviewState.LooksLikeFlagId(id)) PendingNames = unchecked(PendingNames + 1); return "플래그 (" + id + ")"; }
            translated = Text(translated);
            return string.IsNullOrEmpty(id) || translated.EndsWith("(" + id + ")", StringComparison.Ordinal)
                ? translated : translated + "(" + id + ")";
        }
        public static string FlagName(string id, int state) { return Flag(null, id, state); }

        public static string ReferenceName(object reference, int type)
        {
            string[] fields = { "", "_stat", "_relationship", "_flag", "_book", "_misc", "_specialItem", "", "", "_gameTime",
                "_talentData", "_book", "_upgradeItemData", "_upgradeItemData", "_bookCollection", "_libraryAchieve" };
            object target = type > 0 && type < fields.Length ? Field(reference, fields[type]) : null;
            string raw = Name(target), id = String(Field(target, "_id"));
            if (type == 1)
            {
                string key = EnumKey(Field(target, "_type"));
                if (!string.IsNullOrEmpty(key)) return LookupName("PlayerStat/" + key, key);
            }
            if (type == 2)
            {
                string key = EnumKey(Field(target, "_type"));
                if (string.IsNullOrEmpty(key))
                {
                    Match match = Regex.Match(raw, @"^Relationship_([^_]+)(?:_|$)");
                    if (match.Success) key = match.Groups[1].Value.ToLowerInvariant();
                }
                string note = String(Field(target, "_note"));
                string who = string.IsNullOrEmpty(key) ? Text(note) : Text(Locale("Character/" + key, string.IsNullOrEmpty(note) ? key : note));
                if (!string.IsNullOrEmpty(who)) return who + " 호감도";
            }
            if (type == 3) return Flag(target, raw, null);
            if (type == 4 || type == 5 || type == 6 || type == 11)
            {
                string prefix = type == 5 ? "Misc/Name/" : type == 6 ? "Special/Name/" : "Book/Name/";
                string item = string.IsNullOrEmpty(id) ? Text(raw) : LookupName(prefix + id, id);
                return item + (type == 11 ? " 무학 레벨" : " 보유 수");
            }
            if (type == 9)
            {
                if (raw == "PlayerGameTime") return "현재 시간";
                object time = Field(target, "GameTime");
                object year = Field(time, "Year"), month = Field(time, "Month"), stage = Field(time, "Stage");
                if (year != null && month != null && stage != null)
                {
                    int part = Convert.ToInt32(stage, CultureInfo.InvariantCulture);
                    if (part >= 1 && part <= 3) return year + "년차 " + month + "월 " + new[] { "", "상순", "중순", "하순" }[part];
                }
                return "게임 시간";
            }
            if (type == 10) return (string.IsNullOrEmpty(id) ? Text(raw) : LookupName("PlayerTalent/Name/" + id, id)) + " 레벨";
            if (type == 12 || type == 13)
            {
                string key = String(Field(target, "_key"));
                string title = string.IsNullOrEmpty(key) ? Text(String(Field(target, "_note"))) : LookupName("Equip/Name/" + key, key);
                if (string.IsNullOrEmpty(title)) title = "개발 항목";
                return title + (type == 12 ? " 개발 여부" : " 개발 레벨");
            }
            if (type == 14) return "비급 최대 레벨 달성 수";
            if (type == 15) return LookupName("Library/Title/" + raw, raw) + " 기록 수";
            return Text(raw);
        }
    }
}
