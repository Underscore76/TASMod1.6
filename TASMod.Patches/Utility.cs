using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Patches
{
    public class Utility_NewUniqueIdForThisGame : IPatch
    {
        public override string Name => "Utility.NewUniqueIdForThisGame";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), "NewUniqueIdForThisGame"),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(ref ulong __result)
        {
            __result = TASDateTime.uniqueIdForThisGame;
        }
    }

    public class Utility_trySpawnRareObject : IPatch
    {
        public override string Name => "Utility.trySpawnRareObject";
        public static bool IsEnabled = true;

        public static Dictionary<string, double> ChanceModifier = new Dictionary<string, double>
        {
            { "Tree", 0.33 }, // Tree performToolAction
            { "MonsterLoot", 1.5 } // MonsterLoot monsterDrop
        };
        public static double chanceModifier = ChanceModifier["MonsterLoot"];

        public static void SetModifier(string key)
        {
            if (ChanceModifier.TryGetValue(key, out double value))
            {
                chanceModifier = value;
                Controller.Console.Alert($"Set chanceModifier to {chanceModifier} for {key}");
            }
            else
            {
                Controller.Console.Alert($"No chanceModifier found for {key}");
            }
        }

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), "trySpawnRareObject"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static bool Prefix(Random random)
        {
            if (!IsEnabled) return true;

            if (random == null)
            {
                Random r = Game1.random.Copy();
                double a = r.NextDouble(),
                    b = r.NextDouble();
                int i = Game1.random.get_Index();

                double luck = 1.0 + Game1.player.team.AverageDailyLuck();
                double threshold = 0.0006 * chanceModifier;

                for (; b > threshold; i++)
                {
                    // Controller.Console.Alert($"${i:D4} cosmetic:{a} SkillBook:{b}");
                    a = b;
                    b = r.NextDouble();
                }
                Controller.Console.Alert(
                    $"trySpawnRareObject: {Game1.random.get_Index():D4} {i:D4}"
                );
            }
            return true;
        }
    }
}
