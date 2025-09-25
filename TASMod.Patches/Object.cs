using HarmonyLib;
using StardewValley;
using TASMod.Extensions;

namespace TASMod.Patches
{
    public class Object_cutWeed : IPatch
    {
        public override string Name => "Object.cutWeed";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Object),
                    nameof(Object.cutWeed)
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static bool Prefix()
        {
            // Controller.Console.Alert($"Object.CutWeed: {Game1.random.get_Index():D4}");
            return true;
        }
    }
}