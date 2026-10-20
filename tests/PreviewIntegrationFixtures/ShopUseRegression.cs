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
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "special", "food", options, false);
            detail = (string)Member(result, "Detail");
            Check(detail.Contains("12") && detail.Contains("O_drink") && !detail.Contains("O_food"), "same ID with different stock type cannot leak another effect");
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "absent", options, false);
            Check((string)Member(result,"Status") == "사용 경로 미연결" && !(bool)Member(result,"Known"), "missing use is unsupported rather than confirmed no effect");
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "food", options, true);
            Check(!(bool)Member(result,"Known") && ((string)Member(result,"Detail")).Contains("분석 한계"), "partial option discovery remains explicit");
            string conditional = "--index:O_conditional_food\nif statcomparemanager.Check(\"gate\") then\nstatmodifymanager.RemoveMisc(\"food\")\nstatmodifymanager.SetPlayer(\"money\",5)\nend\n";
            options = Invoke(Type("LuaParse.LuaParser"), Parser, "ExtractOptionActionsFromLua", "shop-conditional-parser-regression", conditional);
            result = Invoke(Type("Information.ShopUseEffects"), null, "Find", "misc", "food", options, false);
            detail = (string)Member(result,"Detail");
            Check(detail.Contains("조건") || detail.Contains("미확정") || detail.Contains("확인"), "conditional use retains actual formatter uncertainty");
            Check(Mortal.Core.StatGroupVariable.OriginalReads == 0 && UnityEngine.Random.Calls == 0, "shop integration never executes original game checks or RNG");
        }
    }
}
