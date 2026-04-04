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
            for (int index = 0; index < GameRunner.instance.gameInstances.Count; index++)
            {
                var screen = GameRunner.instance.gameInstances[index].screen;
                var uiScreen = GameRunner.instance.gameInstances[index].uiScreen;
                Game1.graphics.GraphicsDevice.SetRenderTarget(screen);
                Game1.graphics.GraphicsDevice.Clear(Color.Black);
                Game1.graphics.GraphicsDevice.SetRenderTarget(uiScreen);
                Game1.graphics.GraphicsDevice.Clear(Color.Black);
                Game1.graphics.GraphicsDevice.SetRenderTarget(null);
            }
        }
    }
}
