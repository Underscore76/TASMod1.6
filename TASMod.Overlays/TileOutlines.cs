using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using TASMod.Helpers;
using Object = StardewValley.Object;

namespace TASMod.Overlays
{
    public class TileOutlines : IOverlay
    {
        public override string Name => "ObjectTiles";

        public override string Description => "draw object/clump outlines";

        public static Color ObjectColor = Color.Aquamarine;
        public static Color TerrainFeatureColor = Color.Fuchsia;
        public static Color LargeTerrainFeatureColor = Color.Brown;
        public static Color ResourceClumpColor = Color.BlanchedAlmond;

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log($"TileOutlines ActiveDraw Exception: {e}", StardewModdingAPI.LogLevel.Error);
                }
            }
        }
        public void DrawForInstance(int index, SpriteBatch spriteBatch)
        {
            var location = InstanceCurrentLocation.Get(index);
            if (location.Active)
            {
                foreach (
                    KeyValuePair<Vector2, Object> current in location.Location.Objects.Pairs
                )
                {
                    DrawTileOutline(index, spriteBatch, current.Key, ObjectColor);
                }
                foreach (
                    KeyValuePair<Vector2, TerrainFeature> current in location.Location
                        .terrainFeatures
                        .Pairs
                )
                {
                    DrawTileOutline(index, spriteBatch, current.Key, TerrainFeatureColor);
                }
                foreach (LargeTerrainFeature current in location.Location.largeTerrainFeatures)
                {
                    if (current is Bush bush)
                    {
                        Vector2 scale;
                        switch ((int)bush.size.Value)
                        {
                            case 0:
                            case 3:
                                scale = new Vector2(1, 1);
                                break;
                            case 1:
                                scale = new Vector2(2, 1);
                                break;
                            case 2:
                                scale = new Vector2(3, 1);
                                break;
                            default:
                                scale = new Vector2(1, 1);
                                break;
                        }
                        DrawTileOutline(index, spriteBatch, current.Tile, LargeTerrainFeatureColor, scale);
                    }
                }

                if (location.Location is MineShaft mineShaft)
                {
                    foreach (ResourceClump current in mineShaft.resourceClumps)
                    {
                        DrawTileOutline(index, spriteBatch, current.Tile, ResourceClumpColor, 2f);
                    }
                }
                if (location.Location is Farm farm)
                {
                    foreach (ResourceClump current in farm.resourceClumps)
                    {
                        DrawTileOutline(index, spriteBatch, current.Tile, ResourceClumpColor, 2f);
                    }
                }
            }
        }
    }
}
