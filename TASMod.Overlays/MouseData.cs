using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.TerrainFeatures;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Inputs;
using XMouse = Microsoft.Xna.Framework.Input.Mouse;

namespace TASMod.Overlays
{
    public class MouseData : IOverlay
    {
        public override string Name => "MouseData";

        public Vector2 offset = new Vector2(-5, 0);
        public Color RectColor = new Color(0, 0, 0, 180);
        public Color TextColor = Color.White;
        public override string Description => "draw data for coord";

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            var location = InstanceCurrentLocation.Get(ActiveInstance.InstanceIndex);
            if (!location.Active)
                return;
            var player = InstanceCurrentPlayer.Get(ActiveInstance.InstanceIndex);
            var viewport = InstanceViewport.Get(ActiveInstance.InstanceIndex);
            var options = InstanceOptions.Get(ActiveInstance.InstanceIndex);
            var screenFade = InstanceScreenFade.Get(ActiveInstance.InstanceIndex);
            var instanceData = InstanceData.Get(ActiveInstance.InstanceIndex);

            MouseState mouseState = RealInputState.mouseState;//XMouse.GetState();
            Vector2 actualCoords = new Vector2(mouseState.X, mouseState.Y);
            Vector2 coords = new Vector2(mouseState.X - viewport.Window.X, mouseState.Y - viewport.Window.Y);
            Vector2 zoomedCoords = coords * (1f / options.zoomLevel);

            int mouseTileX = (int)(zoomedCoords.X + viewport.X) / Game1.tileSize;
            int mouseTileY = (int)(zoomedCoords.Y + viewport.Y) / Game1.tileSize;

            // open the door for extra data to be written down the line
            List<string> data = new List<string>();
            data.Add(string.Format("({0},{1})", mouseTileX, mouseTileY));
            data.Add(string.Format("StepsTaken: {0}", player.Player.stats.StepsTaken));
            if (GameRunner.instance.gameInstances.Count > 1)
            {
                data.Add(string.Format("Tick: {0}", Reflector.GetStaticVar(0, "Game1_gameTimeInterval") ?? 0));
            }
            else
            {
                data.Add(string.Format("Tick: {0}", Game1.gameTimeInterval));
            }
            data.Add(string.Format("RNG: {0}", instanceData.random.get_Index()));
            if (screenFade.FadeToBlack)
            {
                data.Add("FadeToBlack");
            }
            if (!player.CanMove)
            {
                data.Add("PlayerCanMove: false");
            }
            if (location.Active && location.mineRandom != null && location.MineLevel > 120)
            {
                data.Add(
                    string.Format(
                        "shaft: {0:0.000}",
                        location.mineRandom.Copy().NextDouble()
                    )
                );
            }
            if (player.CanShoot)
            {
                data.Add("slingshot");
            }

            if (location.Active)
            {
                Vector2 mouseTile = new Vector2(mouseTileX, mouseTileY);
                if (location.Location.objects.ContainsKey(mouseTile))
                {
                    var obj = location.Location.objects[mouseTile];
                    switch (obj.Name)
                    {
                        default:
                            data.Add(obj.Name);
                            break;
                    }
                }
                else if (location.Location.terrainFeatures.ContainsKey(mouseTile))
                {
                    var tf = location.Location.terrainFeatures[mouseTile];
                    switch (tf.GetType().Name)
                    {
                        case "HoeDirt":
                            bool watered = (tf as HoeDirt).state.Value == 1;
                            data.Add(string.Format("HoeDirt: {0}", watered));
                            if ((tf as HoeDirt).crop is Crop crop)
                            {
                                data.Add(
                                    string.Format(
                                        "  Crop: {0}",
                                        DropInfo.ObjectName(crop.indexOfHarvest.Value)
                                    )
                                );
                            }
                            break;
                        case "Tree":
                            data.Add(string.Format("Tree: [{0}]", (tf as Tree).health.Value));
                            if ((tf as Tree).hasSeed.Value)
                            {
                                data.Add("  HasSeed");
                            }
                            break;
                        case "Grass":
                            data.Add(
                                string.Format("Grass: [{0}]", (tf as Grass).numberOfWeeds.Value)
                            );
                            break;
                        default:
                            data.Add(tf.GetType().Name);
                            break;
                    }
                }
            }
            DrawText(
                ActiveInstance.InstanceIndex,
                spriteBatch,
                data,
                actualCoords + offset,
                TextColor,
                RectColor,
                offsetTopRight: true
            );
        }
    }
}
