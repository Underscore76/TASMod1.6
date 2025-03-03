using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Internal;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Patches
{
    public class FishingRod_startMinigameEndFunction : IPatch
    {
        public override string Name => "FishingRod.startMinigameEndFunction";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(FishingRod), "startMinigameEndFunction"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static void Test()
        {
            Random random = Game1.random.Copy();
            // update farmer
            // update current location
            // update when not current location

            // do the normal blink test
            // do update when current location on area
        }

        public static bool Prefix()
        {
            // 
            // exclamation bar is a temporary animated sprite, will always call 2
            Controller.Console.Alert(
                $"startMinigameEndFunction: {Game1.random.get_Index():D4}"
            );
            return true;
        }
    }

    public class GameLocation_GetFishFromLocationData : IPatch
    {
        public override string Name => "GameLocation.GetFishFromLocationData";

        public override void Patch(Harmony harmony)
        {
            // GetFishFromLocationData(string locationName, Vector2 bobberTile, int waterDepth, Farmer player, bool isTutorialCatch, bool isInherited, GameLocation location, ItemQueryContext itemQueryContext)
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(GameLocation), "GetFishFromLocationData", new Type[] {
            //         typeof(string),
            //         typeof(Vector2),
            //         typeof(int),
            //         typeof(Farmer),
            //         typeof(bool),
            //         typeof(bool),
            //         typeof(GameLocation),
            //         typeof(ItemQueryContext)
            //     }),
            //     prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
            //     postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            // );
        }

        public static bool Prefix()
        {
            Controller.Console.Alert($"PRE:GetFishFromLocationData: {Game1.random.get_Index():D4}");
            return true;
        }

        public static void Postfix()
        {
            Controller.Console.Alert($"POST:GetFishFromLocationData: {Game1.random.get_Index():D4}");
        }
    }

    public class GameLocation_CheckGenericFishRequirements : IPatch
    {
        public override string Name => "GameLocation.CheckGenericFishRequirements";

        public override void Patch(Harmony harmony)
        {
            // CheckGenericFishRequirements(string fishId, Vector2 bobberTile, int waterDepth, Farmer player, bool isTutorialCatch, bool isInherited, GameLocation location, ItemQueryContext itemQueryContext)
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(GameLocation), "CheckGenericFishRequirements"),
            //     prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
            //     postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            // );
        }

        public static bool Prefix(Item fish)
        {
            Controller.Console.Alert($"PRE:CheckGenericFishRequirements: {Game1.random.get_Index():D4} {fish.ItemId} {fish.HasTypeObject()}");
            return true;
        }

        public static void Postfix()
        {
            Controller.Console.Alert($"POST:CheckGenericFishRequirements: {Game1.random.get_Index():D4}");
        }
    }

    public class ItemQueryResolver_TryResolve : IPatch
    {
        public override string Name => "ItemQueryResolver.TryResolve";

        public override void Patch(Harmony harmony)
        {
            // public static IList<ItemQueryResult> TryResolve(ISpawnItemData data, ItemQueryContext context, ItemQuerySearchMode filter = ItemQuerySearchMode.All, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Func<string, string> formatItemId = null, Action<string, string> logError = null, Item inputItem = null)

            // harmony.Patch(
            //     original: AccessTools.Method(typeof(ItemQueryResolver), "TryResolve", new Type[] {
            //         typeof(ISpawnItemData),
            //         typeof(ItemQueryContext),
            //         typeof(ItemQuerySearchMode),
            //         typeof(bool),
            //         typeof(HashSet<string>),
            //         typeof(Func<string, string>),
            //         typeof(Action<string, string>),
            //         typeof(Item)
            //     }),
            //     prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
            //     postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            // );
        }

        public static bool Prefix(ISpawnItemData data)
        {
            Controller.Console.Alert($"PRE:ItemQueryResolve.TryResolve: {Game1.random.get_Index():D4} {data.ItemId}");
            return true;
        }

        public static void Postfix(ref IList<ItemQueryResult> __result)
        {
            Controller.Console.Alert($"POST:ItemQueryResolve.TryResolve: {Game1.random.get_Index():D4}");
            if (__result != null)
            {
                foreach (ItemQueryResult result in __result)
                {
                    Controller.Console.Alert($"\tPOST:ItemQueryResolve.TryResolve: {Game1.random.get_Index():D4} {result.Item.Name}");
                }
            }
        }
    }
}