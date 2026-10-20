using System;
using System.Collections;
using System.Linq;

namespace DiceMaster
{
    public static partial class PreviewScenario
    {
        private static void ShopUseRegression()
        {
            string source = "--index:O_food\nstatmodifymanager.RemoveMisc(\"food\")\nstatmodifymanager.SetPlayer(\"money\",8)\n" +
                "--index:O_drink\nstatmodifymanager.RemoveSpecial(\"food\")\nstatmodifymanager.SetPlayer(\"money\",12)\n" +
                "--index:O_other\nstatmodifymanager.RemoveMisc(\"other\")\nstatmodifymanager.SetPlayer(\"money\",99)\n";
            object options = Invoke(Type("LuaParse.LuaParser"), Parser, "ExtractOptionActionsFromLua", "shop-real-parser-regression", source);
            object result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "food", options, false);
            string detail = (string)Member(result, "Detail");
            Check(detail.Contains("8") && detail.Contains("O_food") && !detail.Contains("O_drink") && !detail.Contains("O_other"), "actual shop association and parser preserve selected item identity: " + detail);
            string inline = (string)Member(result, "InlineText");
            Check(inline.StartsWith("사용: ") && inline.Contains("8") && !inline.Contains("O_food") && !inline.Contains("\n") && inline.Length <= 180, "compact shop hint exposes actual effects in a short line without raw option identity: " + inline);
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "special", "food", options, false);
            detail = (string)Member(result, "Detail");
            Check(detail.Contains("12") && detail.Contains("O_drink") && !detail.Contains("O_food"), "same ID with different stock type cannot leak another effect");
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "absent", options, false);
            Check((string)Member(result,"Status") == "사용 경로 미연결" && !(bool)Member(result,"Known"), "missing use is unsupported rather than confirmed no effect");
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "food", options, true);
            Check(!(bool)Member(result,"Known") && ((string)Member(result,"Detail")).Contains("분석 한계"), "partial option discovery remains explicit");
            inline = (string)Member(result,"InlineText");
            detail = (string)Member(result,"Detail");
            Check(inline.Contains("8") && !inline.Contains("분석 한계") && detail.Contains("분석 한계"), "partial discovery keeps the numeric summary and full route diagnostics in detail");
            string conditional = "--index:O_conditional_food\nif statcomparemanager.Check(\"gate\") then\nstatmodifymanager.RemoveMisc(\"food\")\nstatmodifymanager.SetPlayer(\"money\",5)\nend\n";
            options = Invoke(Type("LuaParse.LuaParser"), Parser, "ExtractOptionActionsFromLua", "shop-conditional-parser-regression", conditional);
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "food", options, false);
            detail = (string)Member(result,"Detail");
            Check(detail.Contains("조건") || detail.Contains("미확정") || detail.Contains("확인"), "conditional use retains actual formatter uncertainty");
            inline = (string)Member(result,"InlineText");
            Check(inline.Contains("5") && inline.Contains("가능:"), "compact conditional shop effects keep a short possibility marker");
            // A small authored example with the same API shape as an actual
            // food-use option. This is not the game's dialogue or full script.
            string sugarcane = "--index:O_test_sugarcane\n" +
                "stage.hide{character=characters.Get(\"fixture-player\"),duration=0.5}\nwait(1)\n" +
                "statmodifymanager.Player(\"mental\",50,\"\",1)\nwait(0.5)\n" +
                "statmodifymanager.RemoveMisc(\"5001\")\nsay(\"fixture speech\")\n";
            options = Invoke(Type("LuaParse.LuaParser"), Parser, "ExtractOptionActionsFromLua", "shop-sugarcane-shaped-regression", sugarcane);
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "5001", options, false);
            detail = (string)Member(result,"Detail"); inline = (string)Member(result,"InlineText");
            Check(!(bool)Member(result,"Known") && (string)Member(result,"Status") == "사용 효과 일부 확인", "presentation syntax does not falsely turn the whole option fully known");
            Check(detail.Contains("O_test_sugarcane") && detail.Contains(Status("PartialStatus")) && detail.Contains("50"), "full log keeps partial diagnostic, route identity and the supported sugarcane-shaped effect");
            Check(inline.StartsWith("사용: ") && inline.Contains("50") && inline.Contains("가능:"), "partial sugarcane-shaped option shows the numeric effect and its possibility directly: " + inline);
            Check(!inline.Contains("O_test_sugarcane") && !inline.Contains("\n" + Status("PartialStatus") + "\n") && !inline.Contains("\n" + Status("UnsupportedStatus") + "\n"), "direct effect does not stop at analysis status or expose raw option identifiers");
            Check(!inline.Contains("\n") && inline.Length <= 180 && detail.Contains(Status("PartialStatus")) && detail.Contains("확정하지 않음"), "compact hints retain short uncertainty while the complete explanation stays in detail");
            Check(Mortal.Core.StatGroupVariable.OriginalReads == 0 && UnityEngine.Random.Calls == 0, "shop integration never executes original game checks or RNG");
        }
    }
}
