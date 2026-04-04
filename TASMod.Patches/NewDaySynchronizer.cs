using HarmonyLib;
using StardewValley;
using TASMod.Extensions;

namespace TASMod.Patches
{
    public class NewDaySynchronizer_processMessages : IPatch
    {
        public override string Name => "NewDaySynchronizer.processMessages";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(NewDaySynchronizer),
                    nameof(NewDaySynchronizer.processMessages)
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }
        public static void Postfix()
        {
            // removing the thread.sleep..
            Game1.Multiplayer.UpdateLate();
            Game1.Multiplayer.UpdateEarly();
        }
    }
}