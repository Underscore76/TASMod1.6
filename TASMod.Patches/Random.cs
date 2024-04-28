using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using TASMod.Extensions;
using TASMod.System;
using Object = System.Object;

namespace TASMod.Patches
{
    public class Random_Constructor : IPatch
    {
        public override string Name => "Random.Constructor";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: typeof(Random).GetConstructor(new Type[] { }),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
            harmony.Patch(
                original: typeof(Random).GetConstructor(new Type[] { typeof(int) }),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix_Seeded))
            );
        }

        public static void Postfix(ref Random __instance)
        {
            __instance.InitData();
            if (__instance.IsNet6())
            {
                __instance.LoadFromShared();
            }
        }

        public static void Postfix_Seeded(Random __instance, int Seed)
        {
            __instance.InitData(Seed);
        }
    }

    // make randoms actually parseable
    public class Random_ToString : IPatch
    {
        public override string Name => "Random.ToString";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), "ToString"),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(Object __instance, ref string __result)
        {
            if (__instance is Random random)
            {
                if (random.IsNet6())
                {
                    __result =
                        $"{{state: [{random.get_S0()}, {random.get_S1()}, {random.get_S2()}, {random.get_S3()}], index:{random.get_Index()}}}";
                }
                else
                {
                    __result = $"{{seed: {random.get_Seed()}, index:{random.get_Index()}}}";
                }
            }
        }
    }

    public class Random_Next : IPatch
    {
        public override string Name => "Random.Next";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Random), "Next", new Type[] { }),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Random), "Next", new Type[] { typeof(int) }),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Random),
                    "Next",
                    new Type[] { typeof(int), typeof(int) }
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(Random __instance)
        {
            __instance.IncrementCounter();
        }

        public static bool Prefix(Random __instance)
        {
            if (__instance == Game1.random)
            {
                if (!RandomExtensions.StackTraces.ContainsKey((int)TASDateTime.CurrentFrame))
                {
                    RandomExtensions.StackTraces.Add(
                        (int)TASDateTime.CurrentFrame,
                        new List<string>()
                    );
                }
                RandomExtensions
                    .StackTraces[(int)TASDateTime.CurrentFrame]
                    .Add(Environment.StackTrace);
            }
            return true;
        }
    }

    public class Random_NextDouble : IPatch
    {
        public override string Name => "Random.NextDouble";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Random), "NextDouble", new Type[] { }),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(Random __instance)
        {
            if (__instance == Game1.random)
            {
                if (!RandomExtensions.StackTraces.ContainsKey((int)TASDateTime.CurrentFrame))
                {
                    RandomExtensions.StackTraces.Add(
                        (int)TASDateTime.CurrentFrame,
                        new List<string>()
                    );
                }
                RandomExtensions
                    .StackTraces[(int)TASDateTime.CurrentFrame]
                    .Add(Environment.StackTrace);
            }
            return true;
        }

        public static void Postfix(Random __instance)
        {
            __instance.IncrementCounter();
        }
    }

    public class Random_NextBytes : IPatch
    {
        public override string Name => "Random.NextBytes";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Random),
                    "NextBytes",
                    new Type[] { typeof(byte[]) }
                ),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(Random __instance, byte[] buffer)
        {
            if (__instance.IsNet6())
            {
                __instance.IncrementCounter((buffer.Length + 7) / 8);
            }
            else
            {
                __instance.IncrementCounter(buffer.Length);
            }
        }
    }
}
