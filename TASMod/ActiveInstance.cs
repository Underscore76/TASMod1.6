//TODO: I am incredibly aggressive about doing a stash/pop instead of letting it ride
// I should test if I can start easing these back
using Microsoft.Xna.Framework;
using StardewValley;
using TASMod.Networking;
using TASMod.Views;

namespace TASMod
{
    public static class ActiveInstance
    {
        public static int InstanceIndex = 0;
        public static int StashedIndex = -1;

        public static Rectangle Window
        {
            get
            {
                if (Controller.ViewController.CurrentView == TASView.Map)
                    return new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height);
                return Game1.game1.localMultiplayerWindow;
            }
        }
        public static void Stash(int index)
        {
            if (StashedIndex != -1)
            {
                Controller.Console.PushResult("Already have a stashed index!");
                return;
            }
            StashedIndex = InstanceIndex;
            InstanceIndex = index;
            TryLoad();
        }
        public static void Pop()
        {
            if (StashedIndex == -1)
            {
                Controller.Console.PushResult("No stashed index to pop!");
                return;
            }
            InstanceIndex = StashedIndex;
            StashedIndex = -1;
            TryLoad();
        }

        public static void TryLoad()
        {
            if (
                GameRunner.instance != null
                && GameRunner.instance.gameInstances.Count > InstanceIndex
                && Game1.game1.instanceIndex != InstanceIndex
            )
            {
                GameRunner.LoadInstance(GameRunner.instance.gameInstances[InstanceIndex]);
            }
        }

        public static void LoadLast()
        {
            if (
                GameRunner.instance != null
                && NetworkState.NumConnections > 0
                && NetworkState.NumConnections < GameRunner.instance.gameInstances.Count)
            {
                GameRunner.LoadInstance(GameRunner.instance.gameInstances[NetworkState.NumConnections]);
            }
        }
    }
}