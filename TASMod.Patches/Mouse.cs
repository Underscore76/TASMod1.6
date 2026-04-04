using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using TASMod.Inputs;
using TASMod.System;

namespace TASMod.Patches
{
    public class Mouse_PlatformSetPosition : IPatch
    {
        public override string Name => "Mouse.PlatformSetPosition";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Mouse), "PlatformSetPosition"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}