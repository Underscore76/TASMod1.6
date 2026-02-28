using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Layers;
using xTile.ObjectModel;

namespace TASMod.Helpers
{
    public class LocationHelpers
    {
        public static Dictionary<
            string,
            IEnumerable<KeyValuePair<Vector2, StardewValley.Object>>
        > AllForage
        {
            get
            {
                Dictionary<
                    string,
                    IEnumerable<KeyValuePair<Vector2, StardewValley.Object>>
                > forage =
                    new Dictionary<
                        string,
                        IEnumerable<KeyValuePair<Vector2, StardewValley.Object>>
                    >();
                foreach (GameLocation location in Game1.locations)
                {
                    if (location.Name == "Desert" && !Game1.player.hasOrWillReceiveMail("ccVault"))
                        continue;
                    forage.Add(location.Name, LocationForage(location));
                }
                return forage;
            }
        }

        public static IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> LocationForage(
            GameLocation location
        )
        {
            if (location == null)
                return null;
            return location.Objects.Pairs.Where(
                (pair) =>
                {
                    return IsForage(location, pair.Value.Category, pair.Value.ParentSheetIndex);
                }
            );
        }

        private static bool IsForage(GameLocation location, int category, int parentSheetIndex)
        {
            if (
                category != -79
                && category != -81
                && category != -80
                && category != -75
                && !(location is Beach)
            )
            {
                return (int)parentSheetIndex == 430 || parentSheetIndex == 590;
            }
            return true;
        }
    }


    public class LocationInfo
    {
        public int index;
        public GameLocation Location;
        public bool Active => Location != null;
        public string Name => Location?.Name;
        public IEnumerable<NPC> Characters => Location?.characters.Where((n) => (!(n is Monster)));
        public IEnumerable<NPC> Monsters => Location?.characters.Where((n) => (n is Monster));
        public IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> Forage => LocationHelpers.LocationForage(Location);
        public bool IsMines => Location is MineShaft;
        public int MineLevel => (Location as MineShaft).mineLevel;
        public int StonesLeftOnThisLevel()
        {
            if (Location is MineShaft mine)
            {
                return mine.stonesLeftOnThisLevel;
            }
            return 0;
        }
        public bool LadderHasSpawned()
        {
            if (Location is MineShaft mine)
            {
                return mine.ladderHasSpawned;
            }
            return false;
        }
        public int EnemyCount
        {
            get
            {
                if (Location is MineShaft mine)
                    return mine.EnemyCount;
                return 0;
            }
        }
        public bool FogActive
        {
            get
            {
                if (Location is MineShaft mine)
                    return mine.isFogUp.Value;
                return false;
            }
        }
        public Random mineRandom
        {
            get
            {
                if (Location is MineShaft mine)
                {
                    return mine.mineRandom;
                }
                return null;
            }
        }
        public bool MustKillAllMonstersToAdvance()
        {
            if (Location is MineShaft mine)
            {
                return mine.mustKillAllMonstersToAdvance();
            }
            return false;
        }

        public bool HasLadder(out Vector2 location)
        {
            location = Vector2.Zero;
            if (Location is MineShaft mine && mine.ladderHasSpawned)
            {
                // have to find it...
                xTile.Dimensions.Size mapDims = mine.map.Layers[0].LayerSize;
                for (int i = 0; i < mapDims.Width; i++)
                {
                    for (int j = 0; j < mapDims.Height; j++)
                    {
                        int index = mine.getTileIndexAt(i, j, "Buildings");
                        if (index == 173 || index == 174)
                        {
                            location = new Vector2(i, j);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public class InstanceCurrentLocation
    {
        public static LocationInfo Get(int index)
        {
            if (GameRunner.instance.gameInstances.Count == 1)
            {
                return new LocationInfo { index = index, Location = Game1.currentLocation };
            }
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new LocationInfo { index = index, Location = null };
            var location = GameRunner.instance.gameInstances[index]?.instanceGameLocation;
            return new LocationInfo { index = index, Location = location };
        }
    }
}
