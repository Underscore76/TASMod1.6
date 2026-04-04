using System;
using HarmonyLib;
using TASMod.System;

namespace TASMod.Patches
{
    public class GuidHelper_NewGuid : IPatch
    {
        public override string Name => "GuidHelper.GuidHelper";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method("StardewValley.Util.GuidHelper:NewGuid"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            // NOTE: this function gets inlined on mac but not on windows, so need to patch both
            return false;
        }

        public static void Postfix(ref Guid __result)
        {
            // force guidhelper to return a controlled random guid
            __result = TASGuid.NewGuid();
        }
    }
}
