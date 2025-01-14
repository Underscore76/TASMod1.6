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

        public void BuildLayout()
        {
            ImGui.Begin("My First Tool");
            if (ImGui.ColorEdit4("Color", ref MouseColor))
            {
                var mouse = OverlayManager.Get<Mouse>();
                if (mouse != null)
                    mouse.MouseColor = MouseColor.ToColor();
            }
            if (TextBoxInput.GetSelected() != null)
            {
                // var helper = OverlayManager.Get<TextBoxHelper>();
                string text = TextBoxInput.Text;
                ImGui.InputText("Text Entry", ref text, 100);
                TextBoxInput.Text = text;
            }
            else
            {
                TextBoxInput.Text = "";
                // OverlayManager.Get<TextBoxHelper>().Text = "";
            }
            unsafe
            {
                // we need a custom handler for copy and paste that lets us use the clipboard
                // and pull the selection start/end
                ImGuiInputTextCallback callback = (data) =>
                {
                    Console.Trace($"{data->SelectionStart} {data->SelectionEnd} {data->CursorPos}");
                    return 0;
                };
                if (ImGui.InputText("Random Value", ref RandomValue, 100, ImGuiInputTextFlags.CallbackAlways, callback))
                {
                    Console.Trace("Random Value: " + RandomValue);
                }
                else if (ImGui.IsItemActive())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Enter a random value");
                    ImGui.EndTooltip();
                }
                else if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Enter a random value");
                    ImGui.EndTooltip();
                }
            }
            ImGui.InputTextMultiline("Multiline", ref RandomValue, 1000, new Num.Vector2(200, 100));
            ImGui.End();
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            GuiRenderer.BeforeLayout(TASDateTime.CurrentGameTime);
            BuildLayout();

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