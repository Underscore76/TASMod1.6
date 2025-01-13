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
    public class CurrentLocation
    {
        public static bool Active
        {
            get { return Game1.currentLocation != null; }
        }
        public static string Name
        {
            get { return Game1.currentLocation?.Name; }
        }
        public static IEnumerable<NPC> Characters
        {
            get { return Game1.currentLocation?.characters.Where((n) => (!(n is Monster))); }
        }

        public static IEnumerable<NPC> Monsters
        {
            get { return (Game1.currentLocation?.characters.Where((n) => (n is Monster))); }
        }

        public static IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> Forage
        {
            get { return LocationForage(Game1.currentLocation); }
        }

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

        public static bool IsMines
        {
            get { return Game1.currentLocation is MineShaft; }
        }
        public static int MineLevel
        {
            get { return (Game1.currentLocation as MineShaft).mineLevel; }
        }

        public static int StonesLeftOnThisLevel()
        {
            if (Game1.currentLocation is MineShaft mine)
            {
                return Reflector.GetValue<MineShaft, int>(mine, "stonesLeftOnThisLevel");
            }
            return 0;
        }

        public static bool LadderHasSpawned()
        {
            if (Game1.currentLocation is MineShaft mine)
            {
                //if (mine.getMineArea() != 121 && (mine.mineLevel % 10 == 0 || mine.mineLevel % 40 == 12))
                //    return true;
                return mine.ladderHasSpawned;
            }
            return false;
        }

        public static int EnemyCount
        {
            get
            {
                if (Game1.currentLocation is MineShaft mine)
                    return mine.EnemyCount;
                return 0;
            }
        }

        public static bool FogActive
        {
            get
            {
                if (Game1.currentLocation is MineShaft mine)
                    return ((NetBool)Reflector.GetValue(mine, "isFogUp")).Value;
                return false;
            }
        }

        public static Random mineRandom
        {
            get
            {
                if (Game1.currentLocation is MineShaft mine)
                {
                    return (Random)Reflector.GetValue(mine, "mineRandom");
                }
                return null;
            }
        }

        public static bool MustKillAllMonstersToAdvance()
        {
            if (Game1.currentLocation is MineShaft mine)
            {
                return mine.mustKillAllMonstersToAdvance();
            }
            return false;
        }

        public static int StonesRemaining(MineShaft mine, Vector2 loc)
        {
            int stonesLeftOnThisLevel = CurrentLocation.StonesLeftOnThisLevel();
            if (CurrentLocation.LadderHasSpawned() || (stonesLeftOnThisLevel == 0))
            {
                return -1;
            }

            double rockChance =
                (mine.EnemyCount == 0 ? 0.06 : 0.02)
                + (double)Game1.player.LuckLevel / 100.0
                + Game1.player.DailyLuck / 5.0;
            for (int i = 0; i < stonesLeftOnThisLevel; i++)
            {
                Random random = new Random(
                    (int)loc.X * 1000
                        + (int)loc.Y
                        + mine.mineLevel
                        + (int)Game1.uniqueIDForThisGame / 2
                );
                random.NextDouble();
                if (
                    random.NextDouble()
                    < rockChance + 1.0 / (double)Math.Max(1, stonesLeftOnThisLevel - i)
                )
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool HasLadder(out Vector2 location)
        {
            location = Vector2.Zero;
            if (Game1.currentLocation is MineShaft mine && mine.ladderHasSpawned)
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

        public static Vector2 NearestGrass()
        {
            float minDist = float.MaxValue;
            Vector2 nearest = Vector2.Zero;
            Vector2 player = Game1.player.Tile;
            foreach (var tf in Game1.currentLocation.terrainFeatures.Pairs)
            {
                if (tf.Value is Grass grass)
                {
                    float diff = Vector2.DistanceSquared(tf.Key, player);
                    if (diff < minDist)
                    {
                        minDist = diff;
                        nearest = tf.Key;
                    }
                }
            }
            return nearest;
        }
    }
}
