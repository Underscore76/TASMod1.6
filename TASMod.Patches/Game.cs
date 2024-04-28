using HarmonyLib;

namespace TASMod.Patches
{
    public class Game_IsActive : IPatch
    {
        public override string Name => "Game.IsActive";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.PropertyGetter(
                    "Microsoft.Xna.Framework.GamePlatform:IsActive"
                ),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(ref bool __result)
        {
            // ensure that the game always thinks it is active
            __result = true;
        }
    }
}
