using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using TASMod.Extensions;
using TASMod.Inputs;
using TASMod.Monogame.Framework;
using TASMod.Overlays;
using TASMod.System;
using xTile.Layers;
using Mouse = TASMod.Overlays.Mouse;

namespace TASMod.Views
{
    public class MapView : IView
    {
        public GameLocation stashedLocation;
        public xTile.Dimensions.Rectangle OldViewport;
        public xTile.Dimensions.Rectangle CurrentViewport;
        public Rectangle ScreenRect;
        public bool NeedsReset;
        public bool ScaleUp;
        public float Scale;
        public float MaxScale;
        public float OldZoomLevel;
        public int scrollSpeed = 16;
        public ulong lastFrame;
        public RenderTarget2D target;

        // Overlays for this view
        public TileGrid GridOverlay;
        public Mouse Mouse;
        public TileHighlight Highlights;
        public MinesLadder MinesLadder;
        public MinesRocks MinesRocks;

        public MapView()
        {
            GridOverlay = new();
            Mouse = new();
            Highlights = new();
            MinesLadder = new();
            MinesRocks = new();

            ScreenRect = new Rectangle(
                0,
                0,
                ModEntry.Config.ScreenWidth,
                ModEntry.Config.ScreenHeight
            );
            ModEntry.Console.Log(
                $"initializing MapView: {ScreenRect}",
                StardewModdingAPI.LogLevel.Error
            );
            Scale = 1;
        }

        public void Draw()
        {
            bool inBeginEndPair = Game1.spriteBatch.inBeginEndPair();
            if (!inBeginEndPair)
            {
                //Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                Game1.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.Default,
                    RasterizerState.CullNone
                );
            }
            Game1.game1.GraphicsDevice.SetRenderTarget(null);
            Game1.game1.GraphicsDevice.Clear(Color.Black);
            if (Game1.gameMode == 3 && target != null)
            {
                Mouse.DrawViewport(
                    Game1.spriteBatch,
                    target,
                    CurrentViewport,
                    ScreenRect,
                    Color.White
                );
                GridOverlay.Draw();
                MinesLadder.Draw();
                MinesRocks.Draw();
                Highlights.Draw();
                Mouse.Draw();
            }
            if (!inBeginEndPair)
            {
                Game1.spriteBatch.End();
            }
        }

        public void SetLocation(GameLocation location)
        {
            if (Game1.currentLocation == location)
                return;
            if (stashedLocation == null)
            {
                stashedLocation = Game1.currentLocation;
            }
            Game1.currentLocation = location;
            Reset();
        }

        public void Enter()
        {
            OldZoomLevel = Game1.options.baseZoomLevel;
            Game1.game1.zoomModifier = Game1.options.baseZoomLevel;
            OldViewport = Game1.viewport;
            if (Game1.gameMode != 3)
                return;
            Game1.options.baseZoomLevel = 1;
            Reset();
        }

        public void Exit()
        {
            Game1.viewport = OldViewport;
            Game1.options.baseZoomLevel = OldZoomLevel;
            Game1.game1.zoomModifier = 1;
            if (stashedLocation != null)
            {
                Game1.currentLocation = stashedLocation;
                stashedLocation = null;
            }
            MinesRocks.Reset();
            MinesLadder.Reset();
        }

        public void Reset()
        {
            lastFrame = TASDateTime.CurrentFrame;

            int width = Game1.currentLocation.map.DisplayWidth;
            int height = Game1.currentLocation.map.DisplayHeight;
            CurrentViewport = new(0, 0, width, height);
            Scale = Math.Max(width / (float)OldViewport.Width, height / (float)OldViewport.Height);
            MaxScale = Scale;

            RenderTarget2D cached_lightmap = Game1.lightmap;
            SetLightMap(null); //Game1._lightmap = null;
            Game1.game1.takingMapScreenshot = true;
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, width, height);

            target = new RenderTarget2D(
                Game1.graphics.GraphicsDevice,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.DiscardContents
            );
            DrawLocation(target);

            SetLightMap(cached_lightmap); //Game1._lightmap = cached_lightmap;
            Game1.game1.takingMapScreenshot = false;
            Game1.viewport = OldViewport;
        }

        private FieldInfo lightmapInfo = null;

        private void SetLightMap(RenderTarget2D target)
        {
            if (lightmapInfo == null)
            {
                lightmapInfo = typeof(Game1).GetField(
                    "_lightmap",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
            }
            if (lightmapInfo != null)
            {
                lightmapInfo.SetValue(null, target);
            }
            // ModEntry.Console.Log($"{lightmapInfo}", StardewModdingAPI.LogLevel.Warn);
        }

        public static Vector2 MouseTile
        {
            get
            {
                MouseState mouseState = RealInputState.mouseState;
                Vector2 coords = new Vector2(mouseState.X, mouseState.Y);
                Vector2 zoomedCoords = coords * (1f / Game1.options.zoomLevel);

                int mouseTileX = (int)(zoomedCoords.X + Game1.viewport.X) / Game1.tileSize;
                int mouseTileY = (int)(zoomedCoords.Y + Game1.viewport.Y) / Game1.tileSize;
                return new Vector2(mouseTileX, mouseTileY);
            }
        }

        public static void DrawLocation(RenderTarget2D target)
        {
            var spriteBatch = Game1.spriteBatch;
            var currentLocation = Game1.currentLocation;
            var mapDisplayDevice = Game1.mapDisplayDevice;
            var GraphicsDevice = Game1.game1.GraphicsDevice;
            Game1.SetRenderTarget(target);
            GraphicsDevice.Clear(Color.Black);
            // draw background
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            currentLocation.drawBackground(spriteBatch);
            mapDisplayDevice.BeginScene(spriteBatch);
            currentLocation
                .Map.GetLayer("Back")
                .Draw(
                    mapDisplayDevice,
                    Game1.viewport,
                    xTile.Dimensions.Location.Origin,
                    wrapAround: false,
                    4
                );
            currentLocation.drawWater(spriteBatch);
            spriteBatch.End();

            // flooring
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            currentLocation.drawFloorDecorations(spriteBatch);
            spriteBatch.End();

            // shadows

            // building layer
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            Layer building_layer = currentLocation.Map.GetLayer("Buildings");
            if (TASSpriteBatch.Active)
            {
                building_layer.Draw(
                    mapDisplayDevice,
                    Game1.viewport,
                    xTile.Dimensions.Location.Origin,
                    wrapAround: false,
                    4
                );
                mapDisplayDevice.EndScene();
            }
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            // draw shadows again?
            // currentLocation.draw
            {
                foreach (ResourceClump r in currentLocation.resourceClumps)
                {
                    r.draw(spriteBatch);
                }
                Reflector.InvokeMethod(
                    currentLocation,
                    "drawCharacters",
                    new object[] { spriteBatch }
                );
                Reflector.InvokeMethod(
                    currentLocation,
                    "drawFarmers",
                    new object[] { spriteBatch }
                );
                Reflector.InvokeMethod(currentLocation, "drawDebris", new object[] { spriteBatch });
                foreach (var obj in currentLocation.objects.Pairs)
                {
                    obj.Value.draw(spriteBatch, (int)obj.Key.X, (int)obj.Key.Y);
                }

                currentLocation.interiorDoors.Draw(spriteBatch);
                if (currentLocation.largeTerrainFeatures.Count > 0)
                {
                    foreach (
                        LargeTerrainFeature largeTerrainFeature in currentLocation.largeTerrainFeatures
                    )
                    {
                        largeTerrainFeature.draw(spriteBatch);
                    }
                }

                int border_buffer = 1;
                Microsoft.Xna.Framework.Rectangle viewport_rect =
                    new Microsoft.Xna.Framework.Rectangle(
                        Game1.viewport.X / 64 - border_buffer,
                        Game1.viewport.Y / 64 - border_buffer,
                        (int)Math.Ceiling((float)Game1.viewport.Width / 64f) + 2 * border_buffer,
                        (int)Math.Ceiling((float)Game1.viewport.Height / 64f)
                            + 3
                            + 2 * border_buffer
                    );
                Microsoft.Xna.Framework.Rectangle object_rectangle =
                    default(Microsoft.Xna.Framework.Rectangle);
                foreach (Building building in currentLocation.buildings)
                {
                    int additional_radius = building.GetAdditionalTilePropertyRadius();
                    object_rectangle.X = (int)building.tileX.Value - additional_radius;
                    object_rectangle.Width = (int)building.tilesWide.Value + additional_radius * 2;
                    int bottom_y =
                        (int)building.tileY.Value
                        + (int)building.tilesHigh.Value
                        + additional_radius;
                    object_rectangle.Height =
                        bottom_y
                        - (
                            object_rectangle.Y =
                                bottom_y
                                - (int)
                                    Math.Ceiling((float)building.getSourceRect().Height * 4f / 64f)
                                - additional_radius
                        );
                    if (object_rectangle.Intersects(viewport_rect))
                    {
                        building.draw(spriteBatch);
                    }
                }
                if (currentLocation is StardewValley.Locations.BusStop busStop)
                {
                    Vector2 busPosition = (Vector2)Reflector.GetValue(busStop, "busPosition");
                    Rectangle busSource = (Rectangle)Reflector.GetValue(busStop, "busSource");
                    spriteBatch.Draw(
                        Game1.mouseCursors,
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2((int)busPosition.X, (int)busPosition.Y)
                        ),
                        busSource,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        (busPosition.Y + 192f) / 10000f
                    );
                }
            }
            // crabpot tiles
            // draw tool
            // draw farm buildings
            if (currentLocation.Name.Equals("Farm"))
            {
                Reflector.InvokeMethod(Game1.game1, "drawFarmBuildings");
            }
            // front
            mapDisplayDevice.BeginScene(spriteBatch);
            currentLocation
                .Map.GetLayer("Front")
                .Draw(
                    mapDisplayDevice,
                    Game1.viewport,
                    xTile.Dimensions.Location.Origin,
                    wrapAround: false,
                    4
                );
            mapDisplayDevice.EndScene();
            {
                //currentLocation.drawAboveFrontLayer(spriteBatch);
                foreach (var tf in currentLocation.terrainFeatures.Values)
                {
                    tf.draw(spriteBatch);
                }
            }
            spriteBatch.End();
            // always front
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            if (currentLocation.Map.GetLayer("AlwaysFront") != null)
            {
                mapDisplayDevice.BeginScene(spriteBatch);
                currentLocation
                    .Map.GetLayer("AlwaysFront")
                    .Draw(
                        mapDisplayDevice,
                        Game1.viewport,
                        xTile.Dimensions.Location.Origin,
                        wrapAround: false,
                        4
                    );
                mapDisplayDevice.EndScene();
            }
            // random stuff
            //
            //currentLocation.drawAboveAlwaysFrontLayer(spriteBatch);
            spriteBatch.End();
        }

        public void Update()
        {
            if (!Controller.Console.IsOpen)
            {
                if (RealInputState.IsKeyDown(Keys.A))
                {
                    CurrentViewport.X = Math.Max(0, CurrentViewport.X - scrollSpeed);
                }
                if (RealInputState.IsKeyDown(Keys.D))
                {
                    CurrentViewport.X =
                        Math.Min(
                            target.Width,
                            CurrentViewport.X + CurrentViewport.Width + scrollSpeed
                        ) - Math.Min(target.Width, CurrentViewport.Width);
                }
                if (RealInputState.IsKeyDown(Keys.W))
                {
                    CurrentViewport.Y = Math.Max(0, CurrentViewport.Y - scrollSpeed);
                }
                if (RealInputState.IsKeyDown(Keys.S))
                {
                    CurrentViewport.Y =
                        Math.Min(
                            target.Height,
                            CurrentViewport.Y + CurrentViewport.Height + scrollSpeed
                        ) - Math.Min(target.Height, CurrentViewport.Height);
                }
                if (RealInputState.KeyTriggered(Keys.R))
                {
                    Scale = Math.Min(
                        target.Width / (float)OldViewport.Width,
                        target.Height / (float)OldViewport.Height
                    );
                    CurrentViewport = new xTile.Dimensions.Rectangle(
                        0,
                        0,
                        target.Width,
                        target.Height
                    );
                }
                if (RealInputState.KeyTriggered(Keys.C))
                {
                    Overlays.TileHighlight.Clear();
                }
                if (RealInputState.KeyTriggered(Keys.O))
                {
                    Overlays.TileHighlight.DrawOrder = !Overlays.TileHighlight.DrawOrder;
                }
                if (RealInputState.KeyTriggered(Keys.Escape))
                {
                    Controller.ViewController.SetView(TASView.Base);
                    return;
                }
                if (RealInputState.scrollWheelDiff > 0)
                {
                    Scale = Math.Min(MaxScale, Scale + 0.1f);
                }
                else if (RealInputState.scrollWheelDiff < 0)
                {
                    Scale -= 0.1f;
                }
            }
            CurrentViewport.Width = (int)(OldViewport.Width * Scale);
            CurrentViewport.Height = (int)(OldViewport.Height * Scale);
            Game1.viewport = CurrentViewport;
            Game1.options.baseZoomLevel = 1 / Scale;

            if (!Controller.Console.IsOpen && Game1.gameMode == 3)
            {
                if (RealInputState.LeftMouseClicked())
                {
                    Vector2 tile = MouseTile;
                    if (
                        0 <= tile.X
                        && tile.X < Game1.currentLocation.map.Layers[0].LayerWidth
                        && 0 <= tile.Y
                        && tile.Y < Game1.currentLocation.map.Layers[0].LayerHeight
                    )
                    {
                        TileHighlight.Add(tile);
                    }
                }
                else if (RealInputState.RightMouseClicked())
                {
                    TileHighlight.Remove(MouseTile);
                }
            }

            MinesLadder.ActiveUpdate();
            MinesRocks.ActiveUpdate();
            if (lastFrame != TASDateTime.CurrentFrame)
                Reset();
        }
    }
}
