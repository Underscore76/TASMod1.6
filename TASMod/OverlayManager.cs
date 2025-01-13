using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TASMod.Automation;
using TASMod.Console;
using TASMod.Inputs;
using TASMod.Monogame.Framework;
using TASMod.Overlays;

namespace TASMod
{
    public class OverlayManager
    {
        public static OverlayManager Instance { get; private set; }
        public bool Active { get; set; } = true;
        public static Dictionary<string, IOverlay> Overlays;
        public static IEnumerable<string> Names => Overlays.Keys;
        public static IEnumerable<IOverlay> Items => Overlays.Values.OrderBy((o) => o.Priority);
        public static IEnumerable<KeyValuePair<string, IOverlay>> Pairs => Overlays.OrderBy((o) => o.Value.Priority);

        public static bool ContainsKey(string overlayName) => Overlays.ContainsKey(overlayName);

        public static IOverlay Get(string overlayName)
        {
            if (Overlays.ContainsKey(overlayName))
                return Overlays[overlayName];
            return null;
        }

        public static T Get<T>(string overlayName) where T : IOverlay
        {
            if (Overlays.ContainsKey(overlayName))
                return Overlays[overlayName] as T;
            return null;
        }

        public static T Get<T>() where T : IOverlay
        {
            var overlayName = typeof(T).Name;
            if (Overlays.ContainsKey(overlayName))
                return Overlays[overlayName] as T;
            foreach (var v in Overlays)
            {
                if (v.Value is T)
                    return v.Value as T;
            }
            return null;
        }

        public OverlayManager()
        {
            Instance = this;
            Overlays = new Dictionary<string, IOverlay>(StringComparer.OrdinalIgnoreCase);
            foreach (
                var v in Reflector.GetTypesInNamespace(
                    Assembly.GetExecutingAssembly(),
                    "TASMod.Overlays"
                )
            )
            {
                if (v.IsAbstract || v.BaseType != typeof(IOverlay))
                    continue;
                IOverlay overlay = (IOverlay)Activator.CreateInstance(v);
                Overlays.Add(overlay.Name, overlay);
                ModEntry.Console.Log(
                    string.Format("Overlay \"{0}\" added to overlays list", overlay.Name),
                    StardewModdingAPI.LogLevel.Info
                );
            }
        }

        public void Update()
        {
            if (!Active)
            {
                return;
            }
            foreach (var overlay in Overlays)
            {
                overlay.Value.Update();
            }
        }

        public bool HandleInput(TASMouseState realMouse, TASKeyboardState realKeyboard)
        {
            if (!Active)
            {
                return false;
            }
            foreach (var overlay in Overlays)
            {
                if (overlay.Value.HandleInput(realMouse, realKeyboard))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
