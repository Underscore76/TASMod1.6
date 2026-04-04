using Microsoft.Xna.Framework;
using StardewValley;
using TASMod.Networking;
using TASMod.Views;

namespace TASMod
{
    public static class ActiveInstance
    {
        public static int InstanceIndex = 0;
        public static Rectangle Window
        {
            get
            {
                if (Controller.ViewController.CurrentView == TASView.Map)
                    return new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height);
                return Game1.game1.localMultiplayerWindow;
            }
        }
        public static void LoadZero()
        {
            GameRunner.LoadInstance(GameRunner.instance.gameInstances[0]);
        }
    }
}