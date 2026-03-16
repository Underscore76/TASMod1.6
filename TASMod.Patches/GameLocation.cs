// public virtual bool answerDialogueAction(string questionAndAnswer, string[] questionParams)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
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

    public class GameLocation__TestCornersTiles : IPatch
    {
        public override string Name => "GameLocation._TestCornersTiles";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "_TestCornersTiles"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }

        public static void Postfix(Vector2 top_right, Vector2 top_left, Vector2 bottom_right, Vector2 bottom_left, Vector2 top_mid, Vector2 bottom_mid, Vector2? player_top_right, Vector2? player_top_left, Vector2? player_bottom_right, Vector2? player_bottom_left, Vector2? player_top_mid, Vector2? player_bottom_mid, bool bigger_than_tile, Func<Vector2, bool> action, out bool __result)
        {
            Span<Vector2> visitedCollisionTiles = stackalloc Vector2[6];
            int visitedCount = 0;

            if (TryVisitTile(top_right, player_top_right, visitedCollisionTiles, ref visitedCount, action))
            {
                __result = true;
                return;
            }

            if (TryVisitTile(top_left, player_top_left, visitedCollisionTiles, ref visitedCount, action))
            {
                __result = true;
                return;
            }

            if (TryVisitTile(bottom_left, player_bottom_left, visitedCollisionTiles, ref visitedCount, action))
            {
                __result = true;
                return;
            }

            if (TryVisitTile(bottom_right, player_bottom_right, visitedCollisionTiles, ref visitedCount, action))
            {
                __result = true;
                return;
            }

            if (bigger_than_tile)
            {
                if (TryVisitTile(top_mid, player_top_mid, visitedCollisionTiles, ref visitedCount, action))
                {
                    __result = true;
                    return;
                }

                if (TryVisitTile(bottom_mid, player_bottom_mid, visitedCollisionTiles, ref visitedCount, action))
                {
                    __result = true;
                    return;
                }
            }

            __result = false;
        }

        private static bool TryVisitTile(Vector2 tile, Vector2? playerTile, Span<Vector2> visitedCollisionTiles, ref int visitedCount, Func<Vector2, bool> action)
        {
            if (playerTile == tile)
            {
                return false;
            }

            for (int i = 0; i < visitedCount; i++)
            {
                if (visitedCollisionTiles[i] == tile)
                {
                    return false;
                }
            }

            visitedCollisionTiles[visitedCount++] = tile;
            return action(tile);
        }
    }
}
