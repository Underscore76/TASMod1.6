using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using TASMod.Inputs;
using TASMod.System;

namespace TASMod.Patches
{
    public class SoundEffect_GetPooledInstance : IPatch
    {
        public override string Name => "SoundEffect.GetPooledInstance";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SoundEffect), "GetPooledInstance"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static bool Prefix(ref SoundEffectInstance __result)
        {
            __result = null;
            return false;
        }
    }
}