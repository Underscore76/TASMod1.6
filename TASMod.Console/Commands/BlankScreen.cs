using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Console.Commands
{
    public class BlankScreen : IConsoleCommand
    {
        public override string Name => "blankscreen";

        public override string Description => "blank screen to black";

        public override void Run(string[] tokens)
        {
            Game1.graphics.GraphicsDevice.SetRenderTarget(Game1.game1.screen);
            Game1.graphics.GraphicsDevice.Clear(Color.Black);
            Game1.graphics.GraphicsDevice.SetRenderTarget(Game1.game1.uiScreen);
            Game1.graphics.GraphicsDevice.Clear(Color.Black);
            Game1.graphics.GraphicsDevice.SetRenderTarget(null);
        }
    }
}
