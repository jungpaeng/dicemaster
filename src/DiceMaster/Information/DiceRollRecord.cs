using System;
using System.Text;

namespace DiceMaster.Information
{
    // Copies of actual UI data only. No game objects, evaluators or save data.
    public sealed class DiceRollRecord
    {
        public readonly string Name, Checkpoint, Choice, ResultName, RawResult;
        public readonly int Index;
        public DiceRollRecord(string name, string checkpoint, string choice, string resultName, string rawResult, int index)
        {
            Name = Clean(name, 160); if (Name.Length == 0) Name = "주사위 판정";
            Checkpoint = Clean(checkpoint, 256); Choice = Clean(choice, 160);
            ResultName = Clean(resultName, 160); Index = index;
            if (ResultName.Length == 0) ResultName = "결과 " + index;
            RawResult = Clean((rawResult ?? "").Replace("<", "＜").Replace(">", "＞"), 512);
        }
        internal static string Clean(string text, int limit)
        {
            var result = new StringBuilder(); bool tag = false, space = false;
            text = text ?? "";
            for (int at = 0; at < Math.Min(4096, text.Length) && result.Length < limit; at++)
            {
                char c = text[at];
                if (c == '<') { tag = true; continue; }
                if (tag) { if (c == '>') tag = false; continue; }
                if (char.IsWhiteSpace(c) || char.IsControl(c)) { space = result.Length > 0; continue; }
                if (space && result.Length < limit) result.Append(' ');
                space = false; if (result.Length < limit) result.Append(c);
            }
            return result.ToString();
        }
        internal static string Short(string value, int length = 28)
        { return value.Length <= length ? value : value.Substring(0, length - 1) + "…"; }
        internal static string FitNotice(string text, Func<string, float> height, float available)
        {
            string[] lines = text.Split('\n');
            foreach (int limit in new[] { 40, 28, 20, 12, 8 })
            {
                var fitted = new string[lines.Length];
                for (int at = 0; at < lines.Length; at++) fitted[at] = Short(lines[at], limit);
                string value = string.Join("\n", fitted);
                if (height(value) <= available) return value;
            }
            string single = Short(lines[0], 12);
            return height(single) <= available ? single : "";
        }
        internal string Notice { get { return "주사위 판정 · 결과 확인\n" + Short(Name) + " → " + Short(ResultName); } }
        internal string Detail { get { return "주사위 판정: " + Name + "\n실제 확정 결과: " + Index + " · " + ResultName +
            "\n진입 선택: " + (Choice.Length == 0 ? "연결 미확인" : Choice) + "\n판정 키: " + Checkpoint +
            "\n원래 결과 항목: " + (RawResult.Length == 0 ? "미확인" : RawResult) +
            "\n게임이 진행을 확정하고 주사위 메뉴를 정상 정리한 결과입니다. 재굴림 중간 결과는 기록하지 않습니다."; } }
    }
}
