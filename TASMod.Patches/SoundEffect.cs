using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using TASMod.Inputs;
using TASMod.System;

namespace TASMod.Patches
{
    // public class SoundEffect_GetPooledInstance : IPatch
    // {
    //     public override string Name => "SoundEffect.GetPooledInstance";

    //     public override void Patch(Harmony harmony)
    //     {
    //         harmony.Patch(
    //             original: AccessTools.Method(typeof(SoundEffect), "GetPooledInstance"),
    //             prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
    //         );
    //     }

    //     public static bool Prefix(ref SoundEffectInstance __result)
    //     {
    //         // __result = null;
    //         return true;
    //     }
    // }

    // public class SoundEffect_Constructor : IPatch
    // {
    //     public override string Name => "SoundEffect.Constructor";
    //     public static List<WeakReference<SoundEffect>> soundEffects = new List<WeakReference<SoundEffect>>();
    //     public override void Patch(Harmony harmony)
    //     {
    //         Assembly assembly = typeof(SoundEffect).Assembly;
    //         Type type = assembly.GetType("Microsoft.Xna.Framework.Audio.MiniFormatTag");
    //         harmony.Patch(
    //             original: AccessTools.Constructor(typeof(SoundEffect), new Type[] { }),
    //             postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
    //         );
    //         harmony.Patch(
    //             original: AccessTools.Constructor(typeof(SoundEffect), new Type[] { typeof(Stream), typeof(bool) }),
    //             postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
    //         );
    //         harmony.Patch(
    //             original: AccessTools.Constructor(typeof(SoundEffect), new Type[] { typeof(byte[]), typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }),
    //             postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
    //         );
    //         harmony.Patch(
    //             original: AccessTools.Constructor(typeof(SoundEffect), new Type[] { type, typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) }),
    //             postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
    //         );
    //     }

    //     public static void Postfix(ref SoundEffect __instance)
    //     {
    //         Trace($"SoundEffect created: {__instance.GetHashCode()}");
    //         soundEffects.Add(new WeakReference<SoundEffect>(__instance));
    //     }

    //     public static void Reset()
    //     {
    //         for (int i = soundEffects.Count - 1; i >= 0; i--)
    //         {
    //             if (soundEffects[i].TryGetTarget(out SoundEffect se) && se != null)
    //             {
    //                 int c = 0;
    //                 while (!se.ShouldBeRemoved() && c++ < 10)
    //                 {
    //                     se.RemoveDependency();
    //                 }
    //             }
    //         }
    //         soundEffects.Clear();
    //     }
    // }
}