using System;
using System.Collections.Generic;
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
        public bool Active { get; set; } = true;
        public Dictionary<string, IOverlay> Overlays;
        public IEnumerable<string> Keys => Overlays.Keys;
        public IEnumerable<IOverlay> Values => Overlays.Values;

        public bool ContainsKey(string overlayName) => Overlays.ContainsKey(overlayName);

        public Dictionary<string, IOverlay>.Enumerator GetEnumerator() => Overlays.GetEnumerator();

        public IOverlay this[string overlayName]
        {
            get
            {
                if (Overlays.ContainsKey(overlayName))
                    return Overlays[overlayName];
                return null;
            }
        }

        public OverlayManager()
        {
            Overlays = new Dictionary<string, IOverlay>();
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
