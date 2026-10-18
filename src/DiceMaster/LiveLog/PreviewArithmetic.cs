using System;
using System.Globalization;
using System.Text;

namespace DiceMaster.LiveLog
{
    // A deliberately small constant grammar. It never asks Lua to evaluate an
    // expression, resolve a variable, or call a function. Operations use the
    // Lua numeric (double) boundary; only a finite, exact Int32 result is used.
    public static class PreviewArithmetic
    {
        private sealed class Parser
        {
            private readonly string text;
            private int at, depth;
            internal Parser(string value) { text = value ?? ""; }
            private void Space() { while (at < text.Length && char.IsWhiteSpace(text[at])) at++; }
            private bool Take(char value) { Space(); if (at == text.Length || text[at] != value) return false; at++; return true; }
            private static double Safe(double value)
            {
                // Above 2^53 consecutive integers cease to be representable.
                // Reject that loss even if later cancellation produces Int32.
                if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 9007199254740991d)
                    throw new FormatException("유한한 정밀도 범위를 벗어난 상수식");
                return value;
            }
            private double Add()
            {
                double value = Multiply();
                while (true)
                {
                    if (Take('+')) value = Safe(value + Multiply());
                    else if (Take('-')) value = Safe(value - Multiply());
                    else return value;
                }
            }
            private double Multiply()
            {
                double value = Unary();
                while (true)
                {
                    if (Take('*')) value = Safe(value * Unary());
                    else if (Take('/'))
                    {
                        double divisor = Unary();
                        if (divisor == 0) throw new FormatException("0으로 나누는 상수식");
                        value = Safe(value / divisor);
                    }
                    else return value;
                }
            }
            private double Unary()
            {
                if (++depth > 128) throw new FormatException("상수식 중첩 안전 한도");
                try
                {
                    if (Take('+')) return Unary();
                    if (Take('-')) return Safe(-Unary());
                    if (Take('('))
                    {
                        double value = Add();
                        if (!Take(')')) throw new FormatException("상수식 괄호 불일치");
                        return value;
                    }
                    Space();
                    int start = at;
                    while (at < text.Length && char.IsDigit(text[at])) at++;
                    if (at < text.Length && text[at] == '.')
                    {
                        at++;
                        while (at < text.Length && char.IsDigit(text[at])) at++;
                    }
                    if (at < text.Length && (text[at] == 'e' || text[at] == 'E'))
                    {
                        at++;
                        if (at < text.Length && (text[at] == '+' || text[at] == '-')) at++;
                        while (at < text.Length && char.IsDigit(text[at])) at++;
                    }
                    double number;
                    if (start == at || !double.TryParse(text.Substring(start, at - start), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out number)) throw new FormatException("숫자 상수 외 인자 또는 지원 밖 문법");
                    return Safe(number);
                }
                finally { depth--; }
            }
            internal int Integer()
            {
                double value = Add();
                Space();
                if (at != text.Length) throw new FormatException("숫자 상수 외 인자 또는 지원 밖 연산");
                if (value != Math.Truncate(value)) throw new FormatException("정수가 아닌 상수식 결과");
                if (value < int.MinValue || value > int.MaxValue) throw new FormatException("정수 인자 범위 초과");
                return checked((int)value);
            }
        }

        public static bool TryInteger(string expression, out int value, out string reason)
        {
            try { value = new Parser(WithoutComments(expression)).Integer(); reason = null; return true; }
            catch (FormatException error) { value = 0; reason = error.Message; return false; }
            catch (OverflowException) { value = 0; reason = "정수 인자 범위 초과"; return false; }
        }

        private static string WithoutComments(string expression)
        {
            StringBuilder text = new StringBuilder();
            int end = 0;
            foreach (PreviewToken token in PreviewSyntax.Tokenize(expression))
            {
                if (token.Start > end) text.Append(' ');
                text.Append(token.Text);
                end = token.End;
            }
            return text.ToString();
        }

        public static bool TryStringLiteral(string expression, out string value)
        {
            // Share the lexical literal boundary with calls and trace targets.
            return PreviewSyntax.TryString(expression, out value);
        }

    }
}
