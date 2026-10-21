using System;
using System.Globalization;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    // Closed structural evidence for the fixed response of a paid activity,
    // and its mechanical result receipt. Names of people, flags, localized
    // choices and checkpoint keys do not enter the classification.
    internal sealed class MapStoryProfile
    {
        private readonly MapRouteSyntax syntax;
        private readonly int end;
        private int cursor, firstSpeech = -1, switchAt = -1, switchCount, activityBranch = -1, refusalBranch = -1, paymentBranch = -1;
        private string switchVariable;
        private bool failed, flags, resources, receiptPrelude;
        private MapRouteSyntax.Control branch;
        internal readonly HashSet<string> FlagWrites = new HashSet<string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> LiteralSets = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> nonIdempotentWrites = new HashSet<string>(StringComparer.Ordinal);
        internal bool Ready { get; private set; }
        internal bool ActivityReceipt { get; private set; }
        internal MapStoryKind Kind { get; private set; }
        internal MapStoryProfile(MapRouteSyntax value, int start = 0, int stop = -1)
        {
            syntax = value; cursor = start; end = stop < 0 ? value.Tokens.Count : stop;
            failed = value.Limited || value.Malformed || end - start > 8192;
            Kind = MapStoryKind.Unclassified; Ready = failed;
        }
        internal int Step(int budget)
        {
            int spent = 0;
            while (!Ready && spent < budget)
            {
                if (cursor >= end)
                {
                    bool closed = !failed && firstSpeech >= 0 && switchCount == 1 && branch != null && Headers();
                    if (closed && !flags && firstSpeech < switchAt && activityBranch >= 0 && refusalBranch >= 0 &&
                        activityBranch != refusalBranch && paymentBranch == activityBranch && !receiptPrelude)
                        Kind = MapStoryKind.ActivityResponse;
                    ActivityReceipt = closed && switchAt < firstSpeech && resources && activityBranch < 0 && refusalBranch == -2;
                    Ready = true; break;
                }
                Inspect(); spent++;
                if (failed) Ready = true;
            }
            return spent;
        }
        private bool Headers()
        {
            if (branch.Branches.Count != 2 || switchVariable == null) return false;
            for (int i = 0; i < 2; i++)
            {
                var b = branch.Branches[i];
                if (b.Else || b.HeaderEnd - b.Header != 3 || syntax.Tokens[b.Header].Text != switchVariable ||
                    syntax.Tokens[b.Header + 1].Text != "==" || syntax.Tokens[b.Header + 2].Text != (i + 1).ToString(CultureInfo.InvariantCulture)) return false;
            }
            return true;
        }
        private int BranchAt(int at)
        {
            if (branch == null) return -1;
            for (int i = 0; i < branch.Branches.Count; i++)
                if (at >= branch.Branches[i].Start && at < branch.Branches[i].End) return i;
            return -1;
        }
        private void Inspect()
        {
            var t = syntax.Tokens; int at = cursor;
            if (t[at].Text == ";") { cursor++; return; }
            if (branch != null)
            {
                for (int i = 0; i < branch.Branches.Count; i++)
                    if (at == branch.Branches[i].End)
                    { cursor = i + 1 < branch.Branches.Count ? branch.Branches[i + 1].Start : branch.End + 1; return; }
            }
            if (syntax.Controls.TryGetValue(at, out MapRouteSyntax.Control control))
            {
                if (control.Kind != "if" || branch != null || control.End >= end || control.Branches.Count != 2) { failed = true; cursor++; return; }
                branch = control; cursor = control.Branches[0].Start; return;
            }
            if (MapRouteDefinitions.TryPresentationSyntax(syntax, at, out int displayEnd)) { cursor = displayEnd; return; }
            bool local = t[at].Text == "local";
            if (local && at + 3 < end && t[at + 1].IsIdentifier && t[at + 2].Text == "=" &&
                Call(at + 3, out string bindingOwner, out string bindingName, out int bindingOpen, out int bindingClose) &&
                bindingOwner == "checkpointmanager" && bindingName == "Switch" && LiteralArguments(bindingOpen, bindingClose, 1))
            {
                switchVariable = t[at + 1].Text;
                if (Reserved(switchVariable)) { failed = true; cursor = bindingClose + 1; return; }
                switchAt = at; switchCount++; cursor = bindingClose + 1; return;
            }
            if (!Call(at, out string owner, out string name, out int open, out int close))
            { failed = true; cursor++; return; }
            cursor = close + 1;
            if (owner.Length == 0 && name == "say")
            {
                if (!Call(open + 1, out string innerOwner, out string innerName, out int innerOpen, out int innerClose) ||
                    innerOwner != "luamanager" || innerName != "GetStoryText" || innerClose != close - 1 || !LiteralArguments(innerOpen, innerClose, 1)) failed = true;
                if (firstSpeech < 0) firstSpeech = at; return;
            }
            if (owner == "statmodifymanager" && name == "Player")
            {
                int p = open + 1;
                bool literal = p < close && t[p++].IsString && p < close && t[p++].Text == ",";
                bool negative = p < close && t[p].Text == "-"; if (negative) p++;
                literal &= p < close && int.TryParse(t[p++].Text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) &&
                    p < close && t[p++].Text == "," && p < close && t[p++].IsString && p < close && t[p++].Text == "," &&
                    p < close && int.TryParse(t[p++].Text, out int mode) && p == close;
                if (!literal || BranchAt(at) < 0) failed = true;
                else { resources = true; if (negative) paymentBranch = BranchAt(at); }
                return;
            }
            if (owner == "statmodifymanager" && (name == "SetFlag" || name == "AddFlag") && LiteralFlag(open, close, out string flag, out int flagValue))
            {
                flags = true; if (BranchAt(at) < 0 || FlagWrites.Count >= 32 && !FlagWrites.Contains(flag)) failed = true;
                FlagWrites.Add(flag); int previous;
                if (name != "SetFlag" || nonIdempotentWrites.Contains(flag) || LiteralSets.TryGetValue(flag, out previous) && previous != flagValue)
                { LiteralSets.Remove(flag); nonIdempotentWrites.Add(flag); }
                else LiteralSets[flag] = flagValue;
                return;
            }
            if (owner == "luamanager" && name == "SetCurrentStoryScript" && LiteralArguments(open, close, 1)) return;
            if (owner == "luamanager" && name == "PlayMusic" && LiteralArguments(open, close, 1) && branch == null)
            { receiptPrelude = true; return; }
            if (owner.Length == 0 && name == "runblock" && close == open + 6 && t[open + 1].Text == "flowcharts" && t[open + 2].Text == "." && t[open + 3].Text == "view" && t[open + 4].Text == "," && t[open + 5].IsString && branch == null)
            { receiptPrelude = true; return; }
            if (owner == "luamanager" && name == "ChangeScene" && LiteralArguments(open, close, 3) && PreviewSyntax.TryString(t[open + 1].Text, out string scene))
            {
                int arm = BranchAt(at);
                if (arm >= 0)
                {
                    if (scene == "Combat" || scene == "Pinball") activityBranch = arm;
                    else if (scene == "Free") refusalBranch = arm;
                    else failed = true;
                    if (!EmptyTail(close + 1, branch.Branches[arm].End)) failed = true;
                }
                else if (scene == "Free" && branch != null && at > branch.End)
                {
                    refusalBranch = -2;
                    if (!EmptyTail(close + 1, end)) failed = true;
                }
                else failed = true;
                return;
            }
            failed = true;
        }
        private static bool Reserved(string name)
        {
            foreach (string value in new[] { "checkpointmanager", "statmodifymanager", "luamanager", "characters", "stage", "say", "setcharacter", "setsaydialog", "sayoptions", "runwait", "wait", "transitionblack", "flowcharts", "getvar", "runblock" })
                if (name == value) return true;
            return false;
        }
        private bool EmptyTail(int start, int stop)
        {
            if (stop - start > MapRouteLimits.MaxHeaderTokens) return false;
            for (int i = start; i < stop; i++) if (syntax.Tokens[i].Text != ";") return false;
            return true;
        }
        private bool LiteralFlag(int open, int close, out string flag, out int number)
        {
            flag = null; number = 0; var t = syntax.Tokens; int p = open + 1;
            if (p >= close || !t[p].IsString || !PreviewSyntax.TryString(t[p++].Text, out flag) || p >= close || t[p++].Text != ",") return false;
            bool negative = p < close && t[p].Text == "-"; if (negative) p++;
            bool valid = p < close && int.TryParse(t[p++].Text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && p == close;
            if (negative) number = -number; return valid;
        }
        private bool LiteralArguments(int open, int close, int count)
        {
            var t = syntax.Tokens; int p = open + 1;
            for (int i = 0; i < count; i++)
            { if (p >= close || !t[p++].IsString || i + 1 < count && (p >= close || t[p++].Text != ",")) return false; }
            return p == close;
        }
        private bool Call(int at, out string owner, out string name, out int open, out int close)
        {
            owner = name = ""; open = close = -1; var t = syntax.Tokens;
            if (at >= end || !t[at].IsIdentifier || at > 0 && (t[at - 1].Text == "." || t[at - 1].Text == ":")) return false;
            int p = at;
            if (p + 3 < end && t[p + 1].Text == "." && t[p + 2].IsIdentifier) { owner = t[p].Text; name = t[p + 2].Text; p += 3; }
            else { name = t[p].Text; p++; }
            if (p >= end || t[p].Text != "(" || !syntax.Closes.TryGetValue(p, out close) || close >= end || close - p > MapRouteLimits.MaxHeaderTokens) return false;
            open = p; return true;
        }
    }
}
