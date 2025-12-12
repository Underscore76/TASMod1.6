using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Microsoft.Xna.Framework;

namespace TASMod.Helpers
{
    public class ViewportInfo
    {
        public int index;
        public xTile.Dimensions.Rectangle Viewport;
        public int X => Viewport.X;
        public int Y => Viewport.Y;
        public Vector2 Center => new Vector2(Viewport.X + Viewport.Width / 2, Viewport.Y + Viewport.Height / 2);
        public int Width => Viewport.Width;
        public int Height => Viewport.Height;
        public Rectangle Window
        {
            get
            {
                if (Controller.ViewController.CurrentView == TASMod.Views.TASView.Map)
                    return new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height);
                // if (GameRunner.instance.gameInstances.Count == 1)
                //     return new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height);
                return GameRunner.instance.gameInstances[index].localMultiplayerWindow;
            }
        }
    }
    public class InstanceViewport
    {
        public static ViewportInfo Get(int index)
        {
            // if (GameRunner.instance.gameInstances.Count == 1)
            // {
            //     return new ViewportInfo { index = index, Viewport = Game1.viewport };
            // }
            var viewport = (xTile.Dimensions.Rectangle)Reflector.GetStaticVar(index, "Game1_viewport");
            return new ViewportInfo { index = index, Viewport = viewport };
        }
    }
}