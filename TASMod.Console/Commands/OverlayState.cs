using System;
using System.Collections.Generic;
using TASMod.Overlays;

namespace TASMod.Console.Commands
{
    public class OverlayState : IConsoleCommand
    {
        public override string Name => "overlay";
        public override string Description => "print or modify the status of TAS overlays";
        public override string[] Usage =>
            new string[]
            {
                $"\"{Name}\" :See current state",
                $"\"{Name}\" on[|off]: See all on/off",
                $"\"{Name}\" on[|off] <name> [..<name>]: Toggle items on/off",
                $"\"{Name}\" on[|off] all: Toggle ALL overlays on/off",
            };

        public override void Run(string[] tokens)
        {
            if (tokens.Length == 0)
            {
                Write(HelpText());
                foreach (IOverlay overlay in OverlayManager.Items)
                {
                    Write("{0}: {1}", overlay.Name, overlay.Active);
                }
                return;
            }

            if (tokens[0] != "on" && tokens[0] != "off")
            {
                Write(HelpText());
                return;
            }

            bool type = tokens[0] == "on";

            if (tokens.Length == 1)
            {
                Write(string.Join("\r", GetOverlaysByStatus(type)));
                return;
            }

            List<string> rem;
            if (tokens.Length == 2 && tokens[1] == "all")
            {
                rem = new List<string>(OverlayManager.Names);
                Write(string.Join("\r", SetOverlaysToStatus(type, rem)));
                return;
            }
            rem = new List<string>(tokens);
            rem.RemoveAt(0);
            Write(string.Join("\r", SetOverlaysToStatus(type, rem)));
        }

        private List<string> GetOverlaysByStatus(bool active)
        {
            List<string> overlays = new List<string>();
            foreach (var overlay in OverlayManager.Items)
            {
                if (overlay.Active == active)
                {
                    overlays.Add(overlay.Name);
                }
            }
            return overlays;
        }

        private List<string> SetOverlaysToStatus(bool active, List<string> overlays)
        {
            List<string> result = new List<string>();
            foreach (string overlay in overlays)
            {
                if (OverlayManager.ContainsKey(overlay))
                {
                    var ov = OverlayManager.Get(overlay);
                    ov.Active = active;
                    result.Add(
                        string.Format("{0}: {1}", ov.Name, active)
                    );
                }
                else
                {
                    result.Add(string.Format("**{0}: overlay not found", overlay));
                }
            }
            return result;
        }
    }
}
