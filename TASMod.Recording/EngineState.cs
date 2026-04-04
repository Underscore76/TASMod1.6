using System;
using System.Collections.Generic;
using TASMod.Console;
using TASMod.Overlays;

namespace TASMod.Recording
{
    public class EngineState
    {
        public Dictionary<string, string> Aliases;
        public Dictionary<string, bool> OverlayState;
        public Dictionary<string, bool> LogicState;
        public bool MultiplayerShowControllers;
        public bool ShowObjectViewer;
        public bool LogExternalMessages;

        public EngineState()
        {
            Aliases = new Dictionary<string, string>(Controller.Console.Aliases, StringComparer.OrdinalIgnoreCase);
            OverlayState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var overlay in OverlayManager.Items)
            {
                OverlayState.Add(overlay.Name, overlay.Active);
            }
            LogicState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var logic in AutomationManager.Pairs)
            {
                LogicState.Add(logic.Key, logic.Value.Active);
            }
            MultiplayerShowControllers = ImGuiOverlay.ShowControllers;
            ShowObjectViewer = ImGuiOverlay.ShowObjectViewer;
            LogExternalMessages = ExternalLogger.LogExternalMessages;
        }

        public void UpdateGame()
        {
            Controller.Console.Aliases = new Dictionary<string, string>(Aliases, StringComparer.OrdinalIgnoreCase);
            foreach (var overlay in OverlayState)
            {
                if (OverlayManager.ContainsKey(overlay.Key))
                    OverlayManager.Get(overlay.Key).Active = overlay.Value;
            }
            foreach (var logic in LogicState)
            {
                if (AutomationManager.ContainsKey(logic.Key))
                    AutomationManager.Get(logic.Key).Active = logic.Value;
            }
            ImGuiOverlay.ShowControllers = MultiplayerShowControllers;
            ImGuiOverlay.ShowObjectViewer = ShowObjectViewer;
            ExternalLogger.LogExternalMessages = LogExternalMessages;
        }
    }
}