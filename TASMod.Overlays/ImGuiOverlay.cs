using Num = System.Numerics;
using ImGuiNET;
using TASMod.ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.System;
using TASMod.Extensions;
using TASMod.Inputs;
using TASMod.Console;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using StardewValley.Minigames;
using StardewValley.Objects;
using ImGuiVector2 = System.Numerics.Vector2;
using TASMod.Overlays.Widgets;

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

        public void MultiplayerControllers()
        {
            for (int i = 0; i < TASInputState.NumControllers - 1; i++)
            {
                ImGui.SetNextWindowPos(new ImGuiVector2(10, 10 + (i - 1) * 150), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new ImGuiVector2(300, 140), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowBgAlpha(0.9f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
                if (ImGui.Begin($"Controller {i}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    TASGamePadState con = TASInputState.GetTASGamePadState(i);
                    ControllerWidget.Draw("Input", ref con);
                    ImGui.End();
                }
                ImGui.PopStyleVar(2);
            }
        }

        public void BuildLayout()
        {
            ImGui.Begin("Engine");
            if (ImGui.CollapsingHeader("Config"))
            {
                if (ImGui.DragFloat("Window Scale", ref FontScale, 0.005f, 1, 2, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                {
                    ImGui.SetWindowFontScale(FontScale);
                }
            }
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
            }
            if (ImGui.CollapsingHeader("State Info"))
            {
                if (TextBoxInput.GetSelected() != null)
                {
                    // var helper = OverlayManager.Get<TextBoxHelper>();
                    string text = TextBoxInput.Text;
                    ImGui.InputTextMultiline("Multiline", ref text, 100000, new Num.Vector2(200, 100));
                    TextBoxInput.Text = text;
                }
                else
                {
                    TextBoxInput.Text = "";
                }
                ImGui.Text($"State Name: {Controller.State.Prefix}");
                ImGui.Text($"Frame: {TASDateTime.CurrentFrame}");
                ImGui.Text($"Player Tile: {Game1.player.Tile.X},{Game1.player.Tile.Y}");
                ImGui.SeparatorText("Last Frame Input");
                if (Controller.State.FrameStates.Count > 0)
                {
                    ImGui.Text("Left Click: " + Controller.LastFrameMouse().LeftMouseClicked);
                    ImGui.Text("Right Click: " + Controller.LastFrameMouse().RightMouseClicked);
                    ImGui.Text($"Mouse Position: {Controller.LastFrameMouse().MouseX},{Controller.LastFrameMouse().MouseY}");
                    string keys = string.Join(",", Controller.State.FrameStates.Last().keyboardState);
                    ImGui.Text("Keyboard: " + keys);
                }
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
            if (Game1.player != null && ImGui.CollapsingHeader("Inventory"))
            {
                for (int i = 0; i < Game1.player.Items.Count; i++)
                {
                    var item = Game1.player.Items[i];
                    if (item == null) continue;
                    if (item is Furniture furniture)
                    {
                        ImGui.Text($"{i}: {furniture.Name} (dir: {furniture.GetSittingDirection()})");
                    }
                    else if (item is Tool tool)
                    {
                        ImGui.Text($"{i}: {tool.Name}");
                    }
                    else
                    {
                        ImGui.Text($"{i}: {item.Name} x{item.Stack}");
                    }
                }
            }
            ImGui.End();
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            GuiRenderer.BeforeLayout(TASDateTime.CurrentGameTime);
            BuildLayout();
            if (ShowControllers)
            {
                MultiplayerControllers();
            }
            // ImGui.ShowDemoWindow();
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