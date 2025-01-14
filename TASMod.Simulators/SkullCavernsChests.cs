/*
Assumes that you are entering an SC floor using a ladder and that you only care about getting SC treasure chests.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;
using TASMod.Extensions;
using TASMod.Simulators.SkullCaverns;
using xTile;

namespace TASMod.Simulators
{
    // public class SkullCavernsChests
    // {
    //     public string Name => "SkullCavernsChests";

    //     public static string Evaluate()
    //     {
    //         MineShaft mineShaft = Game1.currentLocation as MineShaft;
    //         if (mineShaft == null)
    //         {
    //             return "";
    //         }

    //         Random sharedRandom = Extensions.RandomExtensions.SharedRandom.Copy();
    //         SMineShaft shaft = new SMineShaft(
    //             mineShaft.mineLevel + 1,
    //             mineShaft.loadedMapNumber,
    //             sharedRandom
    //         );
    //         Random Game1_random = Game1.random.Copy();
    //         // Controller.Console.Warn(
    //         //     $"\test: b:generateContents: {Game1_random.get_Index():D4} {shaft.mineRandom.get_Index():D4}"
    //         // );
    //         shaft.generateContents(Game1_random);
    //         // Controller.Console.Warn(
    //         //     $"\test: a:generateContents: {Game1_random.get_Index():D4} {shaft.mineRandom.get_Index():D4}"
    //         // );
    //         if (!shaft.isTreasureRoom)
    //         {
    //             return "";
    //         }
    //         // foreach (var objs in shaft.Objects)
    //         // {
    //         //     Controller.Console.Warn($"\test: {objs.Key} {objs.Value}");
    //         // }

    //         int blinkTimer = Game1.player.blinkTimer;
    //         bool doDrip =
    //             Game1.isMusicContextActiveButNotPlaying()
    //             || Game1.getMusicTrackName().Contains("Ambient");

    //         for (int i = 0; i < 38; i++)
    //         {
    //             blinkTimer += 16;
    //             if (blinkTimer > 2200 && Game1_random.NextDouble() < 0.01)
    //             {
    //                 blinkTimer = -150;
    //             }
    //             if (doDrip)
    //             {
    //                 Game1_random.NextDouble();
    //             }
    //             sharedRandom.Next();
    //         }
    //         // Controller.Console.Warn(
    //         //     $"\test: b:addLevelChests: {Game1_random.get_Index():D4} {shaft.mineRandom.get_Index():D4}"
    //         // );
    //         string items = shaft.addLevelChests(Game1_random);
    //         // Controller.Console.Warn(
    //         //     $"\test: a:addLevelChests: {Game1_random.get_Index():D4} {shaft.mineRandom.get_Index():D4}"
    //         // );
    //         return items;
    //     }
    // }
}
