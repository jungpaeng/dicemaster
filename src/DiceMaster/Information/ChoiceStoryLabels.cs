using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    // Resolve the same data keys as the native button classes. In particular,
    // GetStoryText is not called: DiceMaster's postfix would format effects.
    public static class ChoiceStoryLabels
    {
        private static readonly Regex markup = new Regex("<[^>]*>");
        private static readonly Regex space = new Regex(@"\s+");
        public static string Plain(string text)
        { return space.Replace(markup.Replace(text ?? "", ""), " ").Trim(); }
        public static string Rendered(string text)
        {
            // This is the exact trailing wrapper emitted by OptionInfoPatch,
            // not part of the native choice's story title.
            const string suffix = " <size=18>";
            int at = (text ?? "").LastIndexOf(suffix, StringComparison.Ordinal);
            if (at >= 0 && text.EndsWith("</size>", StringComparison.Ordinal))
            {
                string annotation = text.Substring(at + suffix.Length, text.Length - at - suffix.Length - "</size>".Length);
                if (annotation.Length >= 2 && (annotation[0] == '(' && annotation[annotation.Length - 1] == ')' ||
                    annotation[0] == '（' && annotation[annotation.Length - 1] == '）')) text = text.Substring(0, at);
            }
            return Plain(text);
        }
        public static string Raw(string text)
        { return !string.IsNullOrEmpty(text) && text[0] == '~' ? text.Substring(1) : text ?? ""; }

        public static bool TryResolve(string raw, out string label)
        {
            raw = Raw(raw); label = "";
            if (raw.Length == 0 || raw.Length > 2048) return false;
            string[] parts = raw.Split('+');
            string key;
            if (parts.Length == 3) key = "Position/title/" + parts[2];
            else if (parts.Length == 4) key = "Story/" + parts[1];
            else if (parts.Length == 1 && (raw.StartsWith("O_", StringComparison.Ordinal) || raw.StartsWith("D_", StringComparison.Ordinal))) key = "Story/" + raw;
            else if (parts.Length == 1 && raw.IndexOf('_') < 0 && raw.IndexOf('{') < 0 && raw.IndexOf('}') < 0)
            { label = Plain(DisplayNames.Text(raw)); return label.Length > 0; }
            else return false;
            string value = DisplayNames.Locale(key, "");
            if (string.IsNullOrWhiteSpace(value) || value == key) return false;
            label = Plain(DisplayNames.Text(value));
            return label.Length > 0;
        }
        public static string Resolve(string raw)
        {
            string label;
            return TryResolve(raw, out label) ? label : "선택지 이름 미확인 (" + Raw(raw) + ")";
        }
        public static string Group(string raw)
        {
            string label;
            return (TryResolve(raw, out label) ? label : "이름 미확인 선택지") + " 관련 이야기";
        }
        public static string DialogueTarget(string character)
        {
            if (string.IsNullOrEmpty(character)) return "인물 대화";
            string key = "Position/title/" + character;
            string value = DisplayNames.Locale(key, "");
            if (string.IsNullOrWhiteSpace(value) || value == key) return "인물 대화";
            string name = Plain(DisplayNames.Text(value));
            return name.Length == 0 ? "인물 대화" : ShortName(name) + " 대화";
        }
        public static string DialogueName(string raw, string character)
        {
            string name = Resolve(raw);
            string[] parts = Raw(raw).Split('+');
            // The native position label already names this exact person.
            if (parts.Length == 3 && parts[0] == "talk" && parts[1] == "talk" && parts[2] == character) return name;
            return ShortName(name) + " → " + DialogueTarget(character);
        }
        public static string ShortName(string label)
        { label = Plain(label); return label.Length <= 28 ? label : label.Substring(0, 27) + "…"; }
        public static string Path(IList<MapChoiceStep> steps)
        {
            var labels = new List<string>();
            if (steps != null) for (int index = 0; index < Math.Min(16, steps.Count); index++) labels.Add(Resolve(steps[index].RawLabel));
            if (steps != null && steps.Count > 16) labels.Add("이후 경로는 상세 확인");
            return string.Join(" → ", labels);
        }
    }
}
