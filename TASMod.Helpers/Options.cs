using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using TASMod.Extensions;

namespace TASMod.Helpers
{
    public class InstanceOptions
    {
        public static Options Get(int index)
        {
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
            data.random = (Reflector.GetStaticVar(index, "Game1_random") as Random).Copy();
            data.dayTimeMoneyBox = Reflector.GetStaticVar(index, "Game1_dayTimeMoneyBox") as DayTimeMoneyBox;
            return data;
        }
    }
}