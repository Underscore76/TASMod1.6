using Num = System.Numerics;
using ImGuiNET;
using TASMod.ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.System;
using TASMod.Extensions;
using TASMod.Inputs;
using StardewValley.Objects;
using TASMod.Overlays.Widgets;
using TASMod.Patches;
using TASMod.Recording;
using TASMod.Networking;
using System.Linq;

namespace TASMod.Overlays
{

    public class ImGuiOverlay : IOverlay
    {
        public static bool ShowControllers = true;
        public static ImGuiRenderer GuiRenderer;
        public static RenderTarget2D imguiTarget;
        public Num.Vector4 MouseColor = Color.Black.ToVector4().ToNumerics();
        public string RandomValue = "";
        public float FontScale = 1.0f;

        public override string Name => "imgui";
        public override string Description => "draw a generic gui window to screen";
        public ImGuiOverlay() : base()
        {
            GuiRenderer = new ImGuiRenderer(GameRunner.instance);
            GuiRenderer.RebuildFontAtlas();
            // var io = ImGui.GetIO()
            // io.setc
            // ImGui.SetClipboardText = DesktopClipboard.SetText;
            // ImGui.GetClipboardText = DesktopClipboard.GetText;
            Priority = 100;
        }
        public override void ActiveUpdate()
        {
        }

        public void EngineLayout()
        {
            ImGui.Begin("Engine");
            if (ImGui.CollapsingHeader("Config"))
            {
                if (ImGui.DragFloat("Window Scale", ref FontScale, 0.005f, 1, 2, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                {
                    ImGui.SetWindowFontScale(FontScale);
                }
            }

            FileSelection.Draw();

            if (ImGui.CollapsingHeader("Overlays"))
            {
                foreach (var overlay in OverlayManager.Overlays.Values)
                {
                    if (overlay is ImGuiOverlay) continue;
                    ImGui.Checkbox(overlay.Name, ref overlay.Active);
                }
            }

            if (ImGui.CollapsingHeader("Logic"))
            {
                foreach (var logic in AutomationManager.Automation.Values)
                {
                    ImGui.Checkbox(logic.Name, ref logic.Active);
                }
            }

            if (ImGui.CollapsingHeader("Multiplayer"))
            {
                ImGui.Checkbox("Show Controllers", ref ShowControllers);
                ImGui.SliderInt("Total Players", ref TASInputState.NumControllers, 2, 4);
                ImGui.SliderInt("Active Instance", ref ActiveInstance.InstanceIndex, 0, GameRunner.instance.gameInstances.Count - 1);
            }

            if (ImGui.CollapsingHeader("State Info"))
            {
                ImGui.Text($"State Name: {Controller.State.Prefix}");
                ImGui.Text($"Frame: {TASDateTime.CurrentFrame}");
            }

            foreach (var overlay in OverlayManager.Items)
            {
                if (overlay.Active)
                {
                    ImGui.PushID(overlay.Name + "##overlay");
                    overlay.RenderImGui();
                    ImGui.PopID();
                }
            }

            ImGui.End();
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            GuiRenderer.BeforeLayout(TASDateTime.CurrentGameTime);
            EngineLayout();

            KeyboardMouseWindow.Draw(0);
            if (ShowControllers)
            {
                for (int i = 1; i < TASInputState.NumControllers; i++)
                {
                    ControllerWindow.Draw(i);
                }
            }

            DrawPlayerStatusWindow();

            if (imguiTarget == null || imguiTarget.Width != Game1.graphics.PreferredBackBufferWidth || imguiTarget.Height != Game1.graphics.PreferredBackBufferHeight)
            {
                imguiTarget?.Dispose();
                imguiTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice, Game1.graphics.PreferredBackBufferWidth, Game1.graphics.PreferredBackBufferHeight);
            }
            var oldTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            Game1.graphics.GraphicsDevice.SetRenderTarget(imguiTarget);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
            GuiRenderer.AfterLayout();
            Game1.graphics.GraphicsDevice.SetRenderTargets(oldTargets);
            spriteBatch.Draw(imguiTarget, Vector2.Zero, Color.White);
        }

        private static void DrawPlayerStatusWindow()
        {
            ImGui.SetNextWindowPos(new Num.Vector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Num.Vector2(200, 60), ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5.0f);

            if (ImGui.Begin("Player Status", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                var drawList = ImGui.GetWindowDrawList();
                var pos = ImGui.GetCursorScreenPos();
                float squareSize = 20.0f;
                float spacing = 5.0f;

                int serverMessages = 0;
                if (NetworkState.IncomingMessages.ContainsKey("SERVER"))
                {
                    serverMessages = NetworkState.IncomingMessages["SERVER"].Count;
                }

                float serverWidth = 40.0f;
                Num.Vector4 serverBgColor = serverMessages > 0
                    ? new Num.Vector4(1, 0, 0, 1)
                    : new Num.Vector4(0.3f, 0.3f, 0.3f, 1);

                drawList.AddRectFilled(
                    new Num.Vector2(pos.X, pos.Y),
                    new Num.Vector2(pos.X + serverWidth, pos.Y + squareSize),
                    ImGui.ColorConvertFloat4ToU32(serverBgColor)
                );

                drawList.AddRect(
                    new Num.Vector2(pos.X, pos.Y),
                    new Num.Vector2(pos.X + serverWidth, pos.Y + squareSize),
                    ImGui.ColorConvertFloat4ToU32(new Num.Vector4(0, 0, 0, 1))
                );

                string serverText = serverMessages.ToString();
                var textSize = ImGui.CalcTextSize(serverText);
                float textX = pos.X + (serverWidth - textSize.X) / 2;
                float textY = pos.Y + (squareSize - textSize.Y) / 2;
                drawList.AddText(
                    new Num.Vector2(textX, textY),
                    ImGui.ColorConvertFloat4ToU32(new Num.Vector4(1, 1, 1, 1)),
                    serverText
                );

                float playerStartX = pos.X + serverWidth + spacing;

                for (int i = 0; i < 4; i++)
                {
                    bool hasActivity = false;

                    if (i == 0)
                    {
                        hasActivity = AutomationManager.AppliedLogic != null;
                    }
                    else if (i < TASInputState.NumControllers)
                    {
                        hasActivity = GamePadInputQueue.HasInput(i) || GamePadInputQueue.HasFrameFunction(i);
                    }

                    float x = playerStartX + i * (squareSize + spacing);
                    float y = pos.Y;

                    Num.Vector4 color = hasActivity
                        ? new Num.Vector4(0, 1, 0, 1)
                        : new Num.Vector4(0.5f, 0.5f, 0.5f, 1);

                    drawList.AddRectFilled(
                        new Num.Vector2(x, y),
                        new Num.Vector2(x + squareSize, y + squareSize),
                        ImGui.ColorConvertFloat4ToU32(color)
                    );

                    drawList.AddRect(
                        new Num.Vector2(x, y),
                        new Num.Vector2(x + squareSize, y + squareSize),
                        ImGui.ColorConvertFloat4ToU32(new Num.Vector4(0, 0, 0, 1))
                    );
                }

                float totalWidth = serverWidth + spacing + 4 * (squareSize + spacing) - spacing;
                float totalHeight = squareSize + 5 + ImGui.GetTextLineHeight();

                ImGui.Dummy(new Num.Vector2(totalWidth, totalHeight));

                float labelY = pos.Y + squareSize + 5;

                string srvLabel = "SRV";
                var srvTextSize = ImGui.CalcTextSize(srvLabel);
                float srvLabelX = pos.X + (serverWidth - srvTextSize.X) / 2;
                drawList.AddText(
                    new Num.Vector2(srvLabelX, labelY),
                    ImGui.ColorConvertFloat4ToU32(new Num.Vector4(1, 1, 1, 1)),
                    srvLabel
                );

                for (int i = 0; i < 4; i++)
                {
                    string playerLabel = $"P{i}";
                    var playerTextSize = ImGui.CalcTextSize(playerLabel);
                    float playerLabelX = playerStartX + i * (squareSize + spacing) + (squareSize - playerTextSize.X) / 2;

                    drawList.AddText(
                        new Num.Vector2(playerLabelX, labelY),
                        ImGui.ColorConvertFloat4ToU32(new Num.Vector4(1, 1, 1, 1)),
                        playerLabel
                    );
                }
            }
            ImGui.End();
            ImGui.PopStyleVar();
        }

        public static bool ColorEdit4(string label, ref Color color)
        {
            var col = color.ToVector4().ToNumerics();
            if (ImGui.ColorEdit4(label, ref col))
            {
                color = col.ToColor();
                return true;
            }
            return false;
        }
    }
}