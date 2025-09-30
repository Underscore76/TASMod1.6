using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;

namespace TASMod.Patches
{
    public class SGame_Update : IPatch
    {
        public override string Name => "SGame.Update";
        public static MethodInfo TrueUpdateMethod = null;

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    "StardewModdingAPI.Framework.SGame:Update"
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static bool Prefix()
        {
            // SMAPI uses Game.IsActive to stop running true update which bricks the gamepad update
            if (TrueUpdateMethod == null)
            {
                TrueUpdateMethod = Game1.input.GetType().GetMethod("TrueUpdate");
            }
            TrueUpdateMethod.Invoke(Game1.input, null);
            return true;
        }
    }
}
