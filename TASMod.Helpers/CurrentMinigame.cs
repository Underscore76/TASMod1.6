using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace TASMod.Helpers
{
    public class MinigameInfo
    {
        public int index;
        public IMinigame Minigame;
        public bool Active => Minigame != null;
    }

    public class InstanceCurrentMinigame
    {
        public static MinigameInfo Get(int index)
        {
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new MinigameInfo { index = index, Minigame = null };
            var minigame = Reflector.GetStaticVar(index, "Game1__currentMinigame") as IMinigame;
            return new MinigameInfo { index = index, Minigame = minigame };
        }
    }
}
