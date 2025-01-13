using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using TASMod.Extensions;
using xTile.Layers;

namespace TASMod.Patches
{
#if true
    public class MineShaft_addLevelChests : IPatch
    {
        public override string Name => "MineShaft.addLevelChests";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "addLevelChests"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"b:addLevelChests: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft __instance)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"a:addLevelChests: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
        }
    }

    public class MineShaft_populateLevel : IPatch
    {
        public override string Name => "MineShaft.populateLevel";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "populateLevel"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"b:populateLevel: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft __instance)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"a:populateLevel: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
        }
    }

    public class MineShaft_isTileClearForMineObjects : IPatch
    {
        public override string Name => "MineShaft.isTileClearForMineObjects";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(MineShaft),
                    "isTileClearForMineObjects",
                    new Type[] { typeof(Vector2) }
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance, ref Vector2 v)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Error(
                    $"\tb:isTileClearForMineObjects: ({(int)v.X:D2},{(int)v.Y:D2}) {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4} {__instance.mineRandom.Copy().NextDouble()}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft __instance, ref bool __result, ref Vector2 v)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Error(
                    $"\ta:isTileClearForMineObjects: ({(int)v.X:D2},{(int)v.Y:D2}) {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4} {__result}"
                );
            }
        }
    }

    public class MineShaft_chooseLevelType : IPatch
    {
        public override string Name => "MineShaft.chooseLevelType";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "chooseLevelType"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"b:chooseLevelType: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft __instance)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"a:chooseLevelType: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
        }
    }

    public class MineShaft_createLitterObject : IPatch
    {
        public override string Name => "MineShaft.createLitterObject";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "createLitterObject"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance, Vector2 tile)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"b:createLitterObject({tile}): {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft __instance, Vector2 tile)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"a:createLitterObject({tile}): {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
        }
    }

    public class MineShaft_getMonsterForThisLevel : IPatch
    {
        public override string Name => "MineShaft.getMonsterForThisLevel";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "getMonsterForThisLevel"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance, int xTile, int yTile)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"b:getMonsterForThisLevel({xTile},{yTile}): {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft __instance, int xTile, int yTile)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(__instance, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"a:getMonsterForThisLevel({xTile},{yTile}): {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
                );
            }
        }
    }

    public class BreakableContainer_GetBarrelForMines : IPatch
    {
        public override string Name => "BreakableContainer.GetBarrelForMines";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(BreakableContainer), "GetBarrelForMines"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft mine, Vector2 tile)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(mine, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"b:GetBarrelForMines({tile}): {Game1.random.get_Index():D4} {mine.mineRandom.get_Index():D4}"
                );
            }
            return true;
        }

        public static void Postfix(ref MineShaft mine, Vector2 tile)
        {
            if (Reflector.GetValue<MineShaft, NetBool>(mine, "netIsTreasureRoom").Value)
            {
                Controller.Console.Alert(
                    $"a:GetBarrelForMines({tile}): {Game1.random.get_Index():D4} {mine.mineRandom.get_Index():D4}"
                );
            }
        }
    }

    public class MineShaft_generateContents : IPatch
    {
        public override string Name => "MineShaft.generateContents";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "generateContents"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref MineShaft __instance)
        {
            Controller.Console.Alert(
                $"b:generateContents: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
            );
            return true;
        }

        public static void Postfix(ref MineShaft __instance)
        {
            Controller.Console.Alert(
                $"a:generateContents: {Game1.random.get_Index():D4} {__instance.mineRandom.get_Index():D4}"
            );
        }
    }
#endif
}
