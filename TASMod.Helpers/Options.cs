using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Helpers
{
    public class InstanceOptions
    {
        public static Options Get(int index)
        {
            if (GameRunner.instance.gameInstances.Count == 1)
            {
                return Game1.options;
            }
            var options = GameRunner.instance.gameInstances[index].instanceOptions;
            return options;
        }
    }

    public class InstanceData
    {
        public int index;
        public Random random;
        public DayTimeMoneyBox dayTimeMoneyBox;

        public static InstanceData Get(int index)
        {
            InstanceData data = new InstanceData();
            data.index = index;
            if (GameRunner.instance.gameInstances.Count == 1)
            {
                data.random = Game1.random.Copy();
                data.dayTimeMoneyBox = Game1.dayTimeMoneyBox;
                return data;
            }
            var random = (Reflector.GetStaticVar(index, "Game1_random") as Random);
            if (random != null)
            {
                data.random = random.Copy();
            }
            data.dayTimeMoneyBox = Reflector.GetStaticVar(index, "Game1_dayTimeMoneyBox") as DayTimeMoneyBox;

            return data;
        }

        public static long getNewID(int index)
        {
            var player = InstanceCurrentPlayer.Get(index).Player;
            var latestID = (Reflector.GetStaticVar(index, "Game1_multiplayer") as Multiplayer).latestID;
            ulong seqNum = ((latestID & 0xFF) + 1) & 0xFF;
            ulong nodeID = (ulong)player.UniqueMultiplayerID;
            nodeID = (nodeID >> 32) ^ (nodeID & 0xFFFFFFFFu);
            nodeID = ((nodeID >> 16) ^ (nodeID & 0xFFFF)) & 0xFFFF;
            ulong timestamp = (ulong)(TASDateTime.NextFrameUtcNow.Ticks / 10000);
            latestID = (timestamp << 24) | (nodeID << 8) | seqNum;
            return (long)latestID;
        }
    }
}