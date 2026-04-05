using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;
using TASMod.Helpers;
using TASMod.Scripting;

namespace TASMod.Overlays
{
    public class CropQuality : IOverlay
    {
        public enum Quality
        {
            BASE = 0,
            SILVER = 1,
            GOLD = 2,
            IRIDIUM = 4
        }
        public class TileQuality
        {
            public Vector2 tile;
            public int firstGoldDay;
            public TileQuality(int farmingLevel, GameLocation location, Vector2 tile)
            {
                this.tile = tile;
                HoeDirt soil = location.isTileHoeDirt(tile) ? (HoeDirt)location.terrainFeatures[tile] : null;
                for (int i = Game1.dayOfMonth; i <= 28; ++i)
                {
                    int day = (int)Game1.stats.DaysPlayed + (i - Game1.dayOfMonth);
                    Quality quality = getDayQuality(day, farmingLevel, location, soil);
                    if (quality == Quality.GOLD || quality == Quality.IRIDIUM)
                    {
                        firstGoldDay = day;
                        return;
                    }
                }
                firstGoldDay = -1;
            }

            private Quality getDayQuality(int day, int farmingLevel, GameLocation location, HoeDirt soil)
            {
                Random r2 = Utility.CreateRandom((double)tile.X * 7.0, (double)tile.Y * 11.0, day, Game1.uniqueIDForThisGame);
                int fertilizerQualityLevel = soil?.GetFertilizerQualityBoostLevel() ?? 0;
                double chanceForGoldQuality = 0.2 * ((double)farmingLevel / 10.0) + 0.2 * (double)fertilizerQualityLevel * (((double)farmingLevel + 2.0) / 12.0) + 0.01;
                double chanceForSilverQuality = Math.Min(0.75, chanceForGoldQuality * 2.0);
                if (fertilizerQualityLevel >= 3 && r2.NextDouble() < chanceForGoldQuality / 2.0)
                {
                    return Quality.IRIDIUM;
                }
                else if (r2.NextDouble() < chanceForGoldQuality)
                {
                    return Quality.GOLD;
                }
                else if (r2.NextDouble() < chanceForSilverQuality || fertilizerQualityLevel >= 3)
                {
                    return Quality.SILVER;
                }
                return Quality.BASE;
            }
        }
        public override string Name => "CropQuality";
        public override string Description => "displays crop quality on the map";
        public Color TextColor = Color.Gold;
        public float TextScale = 2;

        private string last_LocationName;
        private int last_Day;
        private int last_FarmingLevel;
        public List<TileQuality> CropQualities = new List<TileQuality>();

        public CropQuality()
        {
            Reset();
        }

        public override void Reset()
        {
            last_LocationName = "";
            last_Day = -1;
            last_FarmingLevel = -1;
            CropQualities = new List<TileQuality>();
        }

        public override void ActiveUpdate()
        {
            var currentLocation = InstanceCurrentLocation.Get(ActiveInstance.InstanceIndex).Location;
            var player = InstanceCurrentPlayer.Get(ActiveInstance.InstanceIndex).Player;
            if (currentLocation == null || !Active) return;
            if (!currentLocation.IsFarm) return;

            if (currentLocation.Name != last_LocationName || Game1.dayOfMonth != last_Day || player.FarmingLevel != last_FarmingLevel)
            {
                last_LocationName = currentLocation.Name;
                last_Day = Game1.dayOfMonth;
                last_FarmingLevel = player.FarmingLevel;
                CropQualities = new List<TileQuality>();
                for (int i = 0; i < currentLocation.map.Layers[0].LayerSize.Width; ++i)
                {
                    for (int j = 0; j < currentLocation.map.Layers[0].LayerSize.Height; ++j)
                    {
                        Vector2 tile = new Vector2(i, j);
                        if (!TileInfo.IsOccupied(currentLocation, tile, false) && TileInfo.IsTillable(currentLocation, tile))
                        {
                            CropQualities.Add(new TileQuality(last_FarmingLevel, currentLocation, tile));
                        }
                    }
                }
            }
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            var currentLocation = InstanceCurrentLocation.Get(ActiveInstance.InstanceIndex).Location;
            if (currentLocation == null || !currentLocation.IsFarm || !Active) return;

            foreach (var tileQuality in CropQualities)
            {
                if (tileQuality.firstGoldDay == -1)
                    continue;
                string text = tileQuality.firstGoldDay.ToString();
                DrawCenteredTextInTile(ActiveInstance.InstanceIndex, spriteBatch, tileQuality.tile, text, TextColor, TextScale);
            }
        }
    }
}