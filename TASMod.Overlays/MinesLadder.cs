using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class MinesLadder : IOverlay
    {
        public override string Name => "MinesLadder";

        public override string Description => "Shows the best rock to break in the mines";

        private int last_mineLevel = -1;
        private int last_miningLevel = -1;
        private int last_luckLevel = -1;
        private int last_stonesLeftOnThisLevel = -1;
        private int last_characterCount = -1;
        private Dictionary<Vector2, int> rockCounters;

        public int lineThickness = 2;
        public bool hasLadder;
        public int minRockCount;
        public Vector2 minLocation;
        public Vector2 ladderLocation;

        public MinesLadder()
        {
            Active = true;
            Reset();
        }

        public override void ActiveUpdate()
        {
            if (ShouldUpdate())
            {
                rockCounters = new Dictionary<Vector2, int>();
                last_mineLevel = CurrentLocation.MineLevel;
                last_miningLevel = Game1.player.MiningLevel;
                last_luckLevel = Game1.player.LuckLevel;
                last_stonesLeftOnThisLevel = CurrentLocation.StonesLeftOnThisLevel();
                last_characterCount = CurrentLocation.EnemyCount;

                foreach (
                    KeyValuePair<Vector2, StardewValley.Object> current in Game1
                        .currentLocation
                        .Objects
                        .Pairs
                )
                {
                    if (current.Value.Name == "Stone")
                    {
                        rockCounters.Add(
                            current.Key,
                            EvalTile(Game1.currentLocation as MineShaft, current.Key)
                        );
                    }
                }
            }
            if (CurrentLocation.IsMines)
            {
                hasLadder = CurrentLocation.HasLadder(out ladderLocation);
                Vector2 baseTile = Game1.player.Tile;
                if (Game1.currentLocation.Name != Game1.player.currentLocation.Name)
                {
                    baseTile = (Game1.currentLocation as MineShaft).tileBeneathLadder;
                }
                if (!hasLadder)
                {
                    float minDistance = float.MaxValue;
                    int minCount = Int32.MaxValue;
                    foreach (var rock in rockCounters)
                    {
                        if (rock.Value < minCount)
                        {
                            minDistance = (rock.Key - baseTile).Length();
                            minCount = rock.Value;
                            minLocation = rock.Key;
                        }
                        else if (rock.Value == minCount)
                        {
                            float distance = (rock.Key - baseTile).Length();
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                minLocation = rock.Key;
                            }
                        }
                    }
                    minRockCount = minCount;
                }
            }
        }

        private Color GetRockColor(int value)
        {
            if (value < 0)
                return Color.DarkGray;
            if (value < 2)
                return Color.Gold;
            if (value < 5)
                return Color.Silver;
            if (value < 8)
                return Color.Green;
            if (value < 11)
                return Color.Orange;
            if (value < 14)
                return Color.Red;
            return Color.DarkGray;
        }

        public override void ActiveDraw(SpriteBatch b)
        {
            if (!CurrentLocation.IsMines)
                return;

            Vector2 baseTile = Game1.player.Tile;
            if (Game1.currentLocation.Name != Game1.player.currentLocation.Name)
            {
                baseTile = (Game1.currentLocation as MineShaft).tileBeneathLadder;
            }
            // draw best line
            if (hasLadder)
            {
                DrawLineBetweenTiles(b, baseTile, ladderLocation, Color.LightCyan, lineThickness);
            }
            else if (minRockCount != Int32.MaxValue)
            {
                foreach (KeyValuePair<Vector2, int> current in rockCounters)
                {
                    if (current.Value <= minRockCount + 10)
                        DrawDepth(b, current.Key, current.Value);
                }
                DrawLineBetweenTiles(
                    b,
                    baseTile,
                    minLocation,
                    GetRockColor(minRockCount),
                    lineThickness
                );
            }
        }

        private void DrawDepth(SpriteBatch spriteBatch, Vector2 tile, int depth)
        {
            DrawCenteredTextInTile(spriteBatch, tile, depth.ToString(), GetRockColor(depth));
        }

        public int EvalTile(MineShaft mine, Vector2 tile)
        {
            if (mine.ladderHasSpawned || mine.stonesLeftOnThisLevel == 0)
            {
                return -1;
            }
            int farmerLuckLevel = Game1.player.LuckLevel;
            double chanceForLadderDown =
                0.02 + (double)farmerLuckLevel / 100.0 + Game1.player.DailyLuck / 5.0;
            if (mine.EnemyCount == 0)
            {
                chanceForLadderDown += 0.04;
            }
            for (int i = 0; i < mine.stonesLeftOnThisLevel; i++)
            {
                Random r = Utility.CreateDaySaveRandom(tile.X * 1000, tile.Y, mine.mineLevel);
                r.NextDouble();
                if (
                    r.NextDouble()
                    < chanceForLadderDown
                        + 1.0 / (double)Math.Max(1, mine.stonesLeftOnThisLevel - i)
                )
                {
                    return i;
                }
            }
            return -1;
        }

        public bool ShouldUpdate()
        {
            if (CurrentLocation.IsMines)
            {
                return (last_mineLevel != CurrentLocation.MineLevel)
                    || (last_miningLevel != Game1.player.MiningLevel)
                    || (last_luckLevel != Game1.player.LuckLevel)
                    || (last_stonesLeftOnThisLevel != CurrentLocation.StonesLeftOnThisLevel())
                    || (last_characterCount != CurrentLocation.EnemyCount);
            }
            return false;
        }

        public override void Reset()
        {
            hasLadder = false;
            last_mineLevel = -1;
            last_miningLevel = -1;
            last_luckLevel = -1;
            last_stonesLeftOnThisLevel = -1;
            last_characterCount = -1;
            rockCounters = new Dictionary<Vector2, int>();
            hasLadder = false;
            minLocation = Vector2.Zero;
            ladderLocation = Vector2.Zero;
            minRockCount = int.MaxValue;
        }
    }
}
