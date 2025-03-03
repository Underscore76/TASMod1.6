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

namespace TASMod.Overlays
{

    public class ImGuiOverlay : IOverlay
    {
        public static ImGuiRenderer GuiRenderer;
        public static RenderTarget2D imguiTarget;
        public Num.Vector4 MouseColor = Color.Black.ToVector4().ToNumerics();
        public string RandomValue = "";

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

        public void BuildLayout()
        {
            ImGui.Begin("Engine");
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
            if (ImGui.CollapsingHeader("State Info"))
            {
                ImGui.Text($"State Name: {Controller.State.Prefix}");
                ImGui.Text($"Frame: {TASDateTime.CurrentFrame}");
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
            // if (ImGui.ColorEdit4("Color", ref MouseColor))
            // {
            //     var mouse = OverlayManager.Get<Mouse>();
            //     if (mouse != null)
            //         mouse.MouseColor = MouseColor.ToColor();
            // }
            // if (TextBoxInput.GetSelected() != null)
            // {
            //     // var helper = OverlayManager.Get<TextBoxHelper>();
            //     string text = TextBoxInput.Text;
            //     ImGui.InputTextMultiline("Multiline", ref text, 100000, new Num.Vector2(200, 100));
            //     TextBoxInput.Text = text;
            // }
            // else
            // {
            //     TextBoxInput.Text = "";
            // }
            ImGui.End();
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            GuiRenderer.BeforeLayout(TASDateTime.CurrentGameTime);
            BuildLayout();
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
    }
}