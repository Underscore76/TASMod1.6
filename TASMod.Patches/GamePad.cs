using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using TASMod.Inputs;
using TASMod.System;

namespace TASMod.Patches
{
    public class GamePad_GetState : IPatch
    {
        public override string Name => "GamePad.PlatformGetState";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GamePad), "PlatformGetState"),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(int index, ref GamePadState __result)
        {
            if (TASInputState.Active && index > 0)
                __result = TASInputState.GetGamePadState(index);
        }
    }
}