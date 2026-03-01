using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Constants;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.System;
using Netcode;
using TASMod.Simulators.Books;
using BF = StardewValley.BellsAndWhistles.Butterfly;
using StardewValley.Menus;

namespace TASMod.Simulators.TreeShake
{
    public class GemNodeHit
    {
        public Vector2 Tile;
        public List<string> CurrentGems = new List<string>();
        public List<string> LadderGems = new List<string>();
        public override string ToString()
        {
            return $"Tile: {Tile}, CurrentGems: [{string.Join(",", CurrentGems)}], LadderGems: [{string.Join(",", LadderGems)}]";
        }
    }

    public class GemNode
    {
    }
}