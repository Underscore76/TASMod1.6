// public virtual bool answerDialogueAction(string questionAndAnswer, string[] questionParams)

using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using TASMod.Minigames;
using TASMod.System;

namespace TASMod.Patches
{
    public class GameLocation_answerDialogueAction : IPatch
    {
        public override string Name => "GameLocation.answerDialogueAction";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "answerDialogueAction"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(string questionAndAnswer)
        {
            return !questionAndAnswer.StartsWith("MinecartGame_");
        }

        public static void Postfix(string questionAndAnswer, out bool __result)
        {
            switch (questionAndAnswer)
            {
                case "MinecartGame_Endless":
                    Game1.currentMinigame = new SMineCart(
                        whichTheme: SMineCart.brownArea,
                        mode: SMineCart.infiniteMode,
                        Game1.random,
                        TASDateTime.CurrentFrame
                    );
                    break;
                case "MinecartGame_Progress":
                    Game1.currentMinigame = new SMineCart(
                        whichTheme: SMineCart.brownArea,
                        mode: SMineCart.progressMode,
                        Game1.random,
                        TASDateTime.CurrentFrame
                    );
                    break;
            }
            __result = true;
        }

        public static IEnumerator<int> EmptySave()
        {
            foreach (GameLocation location in Game1.locations)
            {
                location.cleanupBeforeSave();
            }
            yield return 100;
            yield break;
        }
    }
}
