using System;
using System.Collections.Generic;

namespace DiceMaster.LogUI
{
    // Local syllable composer: every modern Hangul syllable is reachable without
    // an OS IME, synthetic key events, game callbacks or an external service.
    public sealed class LocalSearchKeyboard
    {
        private const string Initials = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        private const string Medials = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        private static readonly string[] Finals = { "받침 없음", "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ", "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ", "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ" };
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Identifiers = "0123456789_-./:()[]{}',+*=!?;\"";
        private int initial;
        private int medial;
        public int Mode { get; private set; }
        public int Stage { get; private set; }
        public string Draft { get; private set; } = string.Empty;
        public string StageLabel => Stage == 0 ? "초성 선택" : Stage == 1 ? "중성 선택" : "종성 선택";
        public string Composition => Mode != 0 || Stage == 0 ? string.Empty : Stage == 1 ? Initials[initial].ToString() : Syllable(initial, medial, 0).ToString();

        public void Start(string text) { Draft = text ?? string.Empty; Mode = 0; Stage = 0; }
        public void SetMode(int mode) { Mode = Math.Max(0, Math.Min(3, mode)); Stage = 0; }

        public IList<string> Keys()
        {
            if (Mode == 0 && Stage == 2) return Finals;
            string characters = Mode == 0 ? (Stage == 0 ? Initials : Medials) : Mode == 1 ? Lowercase : Mode == 2 ? Uppercase : Identifiers;
            var result = new List<string>(characters.Length);
            foreach (char character in characters) result.Add(character.ToString());
            return result;
        }

        public void PressKey(int index)
        {
            IList<string> keys = Keys();
            if (index < 0 || index >= keys.Count) return;
            if (Mode != 0) { Append(keys[index]); return; }
            if (Stage == 0) { initial = index; Stage = 1; }
            else if (Stage == 1) { medial = index; Stage = 2; }
            else { Append(Syllable(initial, medial, index).ToString()); Stage = 0; }
        }

        public void Space() { FinishComposition(); Append(" "); }
        public void Backspace()
        {
            if (Stage > 0) { Stage--; return; }
            if (Draft.Length > 0) Draft = Draft.Substring(0, Draft.Length - 1);
        }
        public void UndoComposition() { if (Stage > 0) Stage--; }
        public string Commit() { FinishComposition(); return Draft; }
        private void FinishComposition()
        {
            if (Mode == 0 && Stage == 1) Append(Initials[initial].ToString());
            if (Mode == 0 && Stage == 2) Append(Syllable(initial, medial, 0).ToString());
            Stage = 0;
        }
        private void Append(string text)
        {
            // Bound only UI state; no search text is written to logs or config.
            if (Draft.Length + text.Length <= 256) Draft += text;
        }
        public static char Syllable(int initialIndex, int medialIndex, int finalIndex)
        {
            if (initialIndex < 0 || initialIndex >= 19 || medialIndex < 0 || medialIndex >= 21 || finalIndex < 0 || finalIndex >= 28)
                throw new ArgumentOutOfRangeException();
            return (char)(0xAC00 + (initialIndex * 21 + medialIndex) * 28 + finalIndex);
        }
    }
}
