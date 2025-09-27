using System;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using StardewValley.SDKs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using TASMod.Patches;
using Microsoft.Xna.Framework;

namespace TASMod
{
    public class ModEntry : Mod
    {
        public static ModConfig Config;
        public static ModEntry Instance;
        public static IReflectionHelper Reflection => Instance.Helper.Reflection;
        public static IMonitor Console => Instance.Monitor;
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = this.Helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            helper.Events.Display.Rendered += Display_Rendered;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.Content.AssetRequested += ContentManager.OnAssetRequested;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            PatchAll(harmony);
            ForceOnLoad();
        }

        private void ForceOnLoad()
        {
            // need to trigger before GameRunner gets fully launched
            (GameRunner.instance as Game).Window.AllowUserResizing = false;
            // foreach (var mode in Game1.graphics.GraphicsDevice.Adapter.SupportedDisplayModes)
            // {
            //     ModEntry.Console.Log($"{mode.Width}x{mode.Height} ({mode.AspectRatio}) {mode.TitleSafeArea}");
            // }

            try
            {
                FieldInfo field = typeof(StardewValley.Program).GetField("_sdk", BindingFlags.NonPublic | BindingFlags.Static);
                SDKHelper sdk = field?.GetValue(null) as SDKHelper;
                sdk?.Shutdown();
                field.SetValue(null, new NullSDKHelper());
            }
            catch
            {
                // well, at least we tried
            }
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Controller.LateInit();
        }

        private void Display_Rendered(object sender, RenderedEventArgs e)
        {
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
        }

        private void GameLoop_UpdateTicking(object sender, UpdateTickingEventArgs e)
        {
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
        }

        public void PatchAll(Harmony harmony)
        {
            foreach (var v in Reflector.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "TASMod.Patches"))
            {
                if (v.IsAbstract || v.BaseType != typeof(IPatch))
                    continue;
                IPatch patch = (IPatch)Activator.CreateInstance(v);
                patch.Patch(harmony);
                Monitor.Log(string.Format("Patch \"{0}\" applied", patch.Name), LogLevel.Info);
            }
        }
    }
}


