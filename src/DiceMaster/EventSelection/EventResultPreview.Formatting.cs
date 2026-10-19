using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DiceMaster.EventSelection
{
    // Display-only formatting shared by the result reader and popup. Keep
    // color information without allowing font/size tags to change hit regions.
    public sealed partial class EventResultPreview
    {
        // Pending/Unavailable are constructed by the outer type initializer.
        // A separate holder avoids depending on partial-file field ordering.
        private static class Patterns
        {
            internal static readonly Regex DisplayTags = new Regex(@"</?(?:color|size|b|i|material)(?:=[^<>]*)?>", RegexOptions.IgnoreCase);
            internal static readonly Regex ColorOpen = new Regex(@"^<color=(?:#[0-9a-f]{6}(?:[0-9a-f]{2})?|red|green|blue|yellow|cyan|magenta|white|black|grey|gray)>$", RegexOptions.IgnoreCase);
            internal static readonly Regex ColorTags = new Regex(@"<color=[^<>]+>|</color>", RegexOptions.IgnoreCase);
        }

        // IMGUI rich text does not decode HTML entities. Full-width brackets
        // leave literal comparisons/identifiers readable without parsing them.
        public static string Escape(string literal)
            => (literal ?? "").Replace('<', '＜').Replace('>', '＞');

        internal static string NormalizeRich(string value)
        {
            value = value ?? "";
            var output = new StringBuilder();
            int position = 0, depth = 0;
            foreach (Match tag in Patterns.DisplayTags.Matches(value))
            {
                output.Append(Escape(value.Substring(position, tag.Index - position)));
                if (Patterns.ColorOpen.IsMatch(tag.Value)) { output.Append(tag.Value); depth++; }
                else if (tag.Value.Equals("</color>", StringComparison.OrdinalIgnoreCase) && depth > 0)
                { output.Append("</color>"); depth--; }
                position = tag.Index + tag.Length;
            }
            output.Append(Escape(value.Substring(position)));
            while (depth-- > 0) output.Append("</color>");
            return output.ToString();
        }

        public static string PlainText(string rich)
            => Patterns.ColorTags.Replace(NormalizeRich(rich), "");

        // visibleCharacters counts UTF-16 text units, never markup. Always
        // close retained spans so truncation cannot color the following row.
        public static string FitSummary(string rich, int visibleCharacters)
        {
            rich = NormalizeRich(rich);
            string plain = Patterns.ColorTags.Replace(rich, "");
            int limit = Math.Max(0, Math.Min(visibleCharacters, plain.Length));
            if (limit == plain.Length) return rich;
            if (limit > 0 && char.IsHighSurrogate(plain[limit - 1])) limit--;
            var output = new StringBuilder();
            int position = 0, count = 0, depth = 0;
            foreach (Match tag in Patterns.ColorTags.Matches(rich))
            {
                int take = Math.Min(limit - count, tag.Index - position);
                output.Append(rich, position, take); count += take;
                if (count == limit) break;
                output.Append(tag.Value);
                depth += tag.Value.Equals("</color>", StringComparison.OrdinalIgnoreCase) ? -1 : 1;
                position = tag.Index + tag.Length;
            }
            if (count < limit) output.Append(rich, position, limit - count);
            while (depth-- > 0) output.Append("</color>");
            return output.Append('…').ToString();
        }
    }
}
