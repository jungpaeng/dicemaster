using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    // Transform only parsed, direct, single-literal calls. Discovery does not
    // create an action: the callback runs immediately before the real call.
    public static class EventLuaGate
    {
        public const string CallbackName = "__dicemaster_wait_event";
        private const string Marker = "-- DiceMaster event execution gate\n";
        internal static string CallbackKey(long token) => CallbackName + "_" + token.ToString(CultureInfo.InvariantCulture);
        internal static string YieldKey(long token) => "__dicemaster_event_yield_" + token.ToString(CultureInfo.InvariantCulture);
        public static string Instrument(string source, long scriptToken, out bool changed)
        { Dictionary<int, EventSwitchSite> ignored; return Instrument(source, scriptToken, out changed, out ignored); }

        internal static string Instrument(string source, long scriptToken, out bool changed, out Dictionary<int, EventSwitchSite> switchSites)
        {
            switchSites = new Dictionary<int, EventSwitchSite>();
            changed = false;
            if (string.IsNullOrEmpty(source) || source.Length > 524288 || source.StartsWith(Marker, StringComparison.Ordinal))
                return source;
            List<PreviewToken> tokens = PreviewSyntax.Tokenize(source);
            if (tokens.Count > 131072) return source;
            var output = new StringBuilder(source.Length + 512);
            string callback = "__dm_event_wait_" + scriptToken.ToString(CultureInfo.InvariantCulture);
            string yield = "__dm_event_yield_" + scriptToken.ToString(CultureInfo.InvariantCulture);
            string invoke = "__dm_event_invoke_" + scriptToken.ToString(CultureInfo.InvariantCulture);
            int copied = 0, site = 0;
            int switchWork = 8388608, switchText = 8388608;
            for (int i = 0; i < tokens.Count; i++)
            {
                string owner, name;
                int open, close;
                if (!PreviewSyntax.TryCall(tokens, i, out owner, out name, out open, out close) ||
                    owner != "checkpointmanager" || (name != "Position" && name != "Switch") || tokens[i + 1].Text != "." ||
                    close != open + 2 || !tokens[open + 1].IsString) continue;
                string literal;
                if (!PreviewSyntax.TryString(tokens[open + 1].Text, out literal) || string.IsNullOrEmpty(literal)) continue;
                // Lua function declarations have the same lexical call shape
                // but are never an invocation boundary.
                if (i > 0 && tokens[i - 1].Text == "function") continue;
                // Each exact-site reader may scan/materialize the remaining
                // source. Bound cumulative work even for deeply nested calls.
                if (name == "Switch")
                {
                    int work = tokens.Count - i, text = source.Length - tokens[i].Start;
                    if (work > switchWork || text > switchText) continue;
                    switchWork -= work; switchText -= text;
                }
                EventSwitchSite definition = name == "Switch" ? EventSwitchSite.Read(source, tokens, i, close) : null;
                if (name == "Switch" && definition == null) continue;
                if (definition != null) switchSites.Add(site + 1, definition);
                string original = source.Substring(tokens[i].Start, tokens[close].End - tokens[i].Start);
                string argument = tokens[open + 1].Text;
                output.Append(source, copied, tokens[i].Start - copied);
                output.Append(invoke).Append("(function() if ")
                    .Append(callback).Append(" then while ").Append(callback).Append('(')
                    .Append(argument).Append(',').Append(scriptToken.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append((++site).ToString(CultureInfo.InvariantCulture))
                    .Append(",checkpointmanager,\"").Append(name).Append("\") do ").Append(yield).Append("() end end return ").Append(original).Append(" end)");
                copied = tokens[close].End;
                i = close;
                changed = true;
            }
            if (!changed) return source;
            output.Append(source, copied, source.Length - copied);
            // Capture mod-owned functions before user locals can shadow the
            // coroutine library or the callback. Main-thread admission is
            // rejected by the typed callback before a yield can occur.
            return Marker + "local " + callback + "=" + CallbackKey(scriptToken) + "; local " + yield + "=" +
                YieldKey(scriptToken) + "; local function " + invoke + "(fn) return fn() end;\n" + output;
        }
    }
}
