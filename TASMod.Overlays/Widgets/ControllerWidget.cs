using System;
using System.Collections;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TASMod.Inputs;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;

namespace TASMod.Overlays.Widgets
{
    // Using alias to avoid conflicts with MPTest.System namespace

    public class ControllerWidget
    {
        public static void Draw(string label, ref TASGamePadState state)
        {
            if (ImGui.CollapsingHeader(label))
            {
                var triggerSize = new ImGuiVector2(20.0f, 16.0f);
                var triggerSpacing = 5.0f;
                var analogWidth = 50.0f;
                var availableRegion = ImGui.GetContentRegionAvail();
                var controllerHeight = Math.Min(2 * analogWidth + 10f, 130);
                var controllerWidth = Math.Min(2 * analogWidth + 4 * triggerSize.X + 3 * triggerSpacing + 20, availableRegion.X);

                var elemSize = new ImGuiVector2(controllerWidth, controllerHeight) + new ImGuiVector2(20, 20);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new ImGuiVector4(0f, 0f, 0f, 1f)); // Semi-transparent dark background
                // ImGui.BeginChild(label + "child", elemSize + new ImGuiVector2(0, 60));
                ImGui.BeginChild(label + "child", elemSize + new ImGuiVector2(0, 60));
                // Reserve the space for our controller widget
                ImGui.Dummy(elemSize);

                var drawList = ImGui.GetWindowDrawList();
                var startPos = ImGui.GetItemRectMin();
                var centerX = startPos.X + controllerWidth * 0.5f;
                var leftColumnX = centerX - 2 * triggerSize.X - 1.5f * triggerSpacing;
                var row1Y = startPos.Y + 30f;

                // Col1: [ZL/L/R/ZR], [DPAD, ABXY], [START/SELECT]
                // Row1 - ZL/L/R/ZR triggers + the neutral clear button
                bool[] triggerStates = { state.ButtonZL, state.ButtonL, false, state.ButtonR, state.ButtonZR };
                DrawButtonGroup(drawList, new ImGuiVector2(leftColumnX, row1Y), new string[] { "ZL", "L", "C", "R", "ZR" }, ref triggerStates, triggerSize, triggerSpacing);
                state.ButtonZL = triggerStates[0];
                state.ButtonL = triggerStates[1];
                state.ButtonR = triggerStates[3];
                state.ButtonZR = triggerStates[4];
                if (triggerStates[2])
                    state.Clear();

                // Row2 - DPad (3x3 grid) and ABXY (diamond)
                var dpadSize = 14.0f;
                var dpadSpacing = 16.0f;
                var dpadCenter = new ImGuiVector2(leftColumnX - dpadSize - dpadSpacing, row1Y + triggerSize.Y + dpadSize + dpadSpacing);
                DrawDPad(drawList, state, dpadCenter, dpadSize, dpadSpacing);

                var faceButtonRadius = 10.0f;
                var faceSpacing = 16.0f;
                dpadCenter.X += 2f * (dpadSize + dpadSpacing);
                DrawFaceButtons(drawList, state, dpadCenter, faceButtonRadius, faceSpacing);

                // Row3 - Start/Select
                var buttonSize = new ImGuiVector2(45.0f, 16.0f);
                var buttonStates = new bool[] { state.ButtonStart, state.ButtonSelect };
                dpadCenter.Y += 1.5f * (dpadSize + dpadSpacing);
                DrawButtonGroup(drawList, new ImGuiVector2(leftColumnX, dpadCenter.Y), new string[] { "Start", "Select" }, ref buttonStates, buttonSize, 25.0f);
                state.ButtonStart = buttonStates[0];
                state.ButtonSelect = buttonStates[1];

                // Col2 - Analog Stick
                var analogCenter = new ImGuiVector2(centerX + analogWidth + 10f, row1Y + analogWidth);
                DrawAnalogStick(drawList, state, analogCenter, analogWidth);
                ImGui.DragFloat("Analog X", ref state.AnalogX, 0.01f, -1f, 1f);
                ImGui.DragFloat("Analog Y", ref state.AnalogY, 0.01f, -1f, 1f);
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }
        private static bool IsPointClicked(ImGuiVector2 pos, float radius)
        {
            var mousePos = ImGui.GetMousePos();
            var distance = (mousePos - pos).Length();
            return distance <= radius && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered();
        }
        private static bool IsRectClicked(ImGuiVector2 pos, ImGuiVector2 size)
        {
            var mousePos = ImGui.GetMousePos();
            var min = pos - size * 0.5f;
            var max = pos + size * 0.5f;
            return mousePos.X >= min.X && mousePos.X <= max.X &&
                    mousePos.Y >= min.Y && mousePos.Y <= max.Y &&
                    ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered();
        }

        private static void DrawButtonGroup(ImDrawListPtr drawList, ImGuiVector2 center, string[] labels, ref bool[] states, ImGuiVector2 buttonSize, float spacing)
        {
            int count = labels.Length;
            var totalWidth = count * buttonSize.X + (count - 1) * spacing;
            var startX = center.X - totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var pos = new ImGuiVector2(startX + i * (buttonSize.X + spacing) + buttonSize.X * 0.5f, center.Y);
                var color = states[i] ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : ImGui.GetColorU32(ImGuiCol.Button);
                drawList.AddRectFilled(pos - buttonSize * 0.5f, pos + buttonSize * 0.5f, color, 3.0f);
                drawList.AddRect(pos - buttonSize * 0.5f, pos + buttonSize * 0.5f, ImGui.GetColorU32(ImGuiCol.Border), 3.0f, 0, 1.0f);
                var textSize = ImGui.CalcTextSize(labels[i]);
                drawList.AddText(pos - textSize * 0.5f, ImGui.GetColorU32(ImGuiCol.Text), labels[i]);
                if (IsRectClicked(pos, buttonSize)) states[i] = !states[i];
            }
        }

        private static void DrawDPad(ImDrawListPtr drawList, TASGamePadState state, ImGuiVector2 dpadCenter, float dpadSize, float dpadSpacing)
        {
            var dpadPositions = new ImGuiVector2[9] {
                    new ImGuiVector2(dpadCenter.X - dpadSpacing, dpadCenter.Y - dpadSpacing), // UL
                    new ImGuiVector2(dpadCenter.X, dpadCenter.Y - dpadSpacing),              // U
                    new ImGuiVector2(dpadCenter.X + dpadSpacing, dpadCenter.Y - dpadSpacing), // UR
                    new ImGuiVector2(dpadCenter.X - dpadSpacing, dpadCenter.Y),              // L
                    new ImGuiVector2(dpadCenter.X, dpadCenter.Y),                            // N (center)
                    new ImGuiVector2(dpadCenter.X + dpadSpacing, dpadCenter.Y),              // R
                    new ImGuiVector2(dpadCenter.X - dpadSpacing, dpadCenter.Y + dpadSpacing), // DL
                    new ImGuiVector2(dpadCenter.X, dpadCenter.Y + dpadSpacing),              // D
                    new ImGuiVector2(dpadCenter.X + dpadSpacing, dpadCenter.Y + dpadSpacing)  // DR
                };

            var dpadStates = new bool[9] {
                    state.DPadUp && state.DPadLeft,                                           // UL
                    state.DPadUp && !state.DPadLeft && !state.DPadRight,                    // U
                    state.DPadUp && state.DPadRight,                                         // UR
                    state.DPadLeft && !state.DPadUp && !state.DPadDown,                     // L
                    !state.DPadUp && !state.DPadDown && !state.DPadLeft && !state.DPadRight, // N
                    state.DPadRight && !state.DPadUp && !state.DPadDown,                    // R
                    state.DPadDown && state.DPadLeft,                                        // DL
                    state.DPadDown && !state.DPadLeft && !state.DPadRight,                  // D
                    state.DPadDown && state.DPadRight                                        // DR
                };

            // Draw D-pad
            for (int i = 0; i < 9; i++)
            {
                var color = dpadStates[i] ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : ImGui.GetColorU32(ImGuiCol.Button);
                var pos = dpadPositions[i];
                drawList.AddRectFilled(pos - new ImGuiVector2(dpadSize * 0.5f), pos + new ImGuiVector2(dpadSize * 0.5f), color, 2.0f);
                drawList.AddRect(pos - new ImGuiVector2(dpadSize * 0.5f), pos + new ImGuiVector2(dpadSize * 0.5f), ImGui.GetColorU32(ImGuiCol.Border), 2.0f, 0, 0.8f);
            }

            // Handle D-pad clicks
            for (int i = 0; i < 9; i++)
            {
                if (IsRectClicked(dpadPositions[i], new ImGuiVector2(dpadSize)))
                {
                    switch (i)
                    {
                        case 0: state.SetDPad(true, false, true, false); break;   // UL
                        case 1: state.SetDPad(true, false, false, false); break;  // U
                        case 2: state.SetDPad(true, false, false, true); break;   // UR
                        case 3: state.SetDPad(false, false, true, false); break;  // L
                        case 4: state.SetDPad(false, false, false, false); break; // N
                        case 5: state.SetDPad(false, false, false, true); break;  // R
                        case 6: state.SetDPad(false, true, true, false); break;   // DL
                        case 7: state.SetDPad(false, true, false, false); break;  // D
                        case 8: state.SetDPad(false, true, false, true); break;   // DR
                    }
                    break;
                }
            }
        }
        private static void DrawFaceButton(ImDrawListPtr drawList, ImGuiVector2 pos, string btnLabel, float faceButtonRadius, ref bool isPressed)
        {
            var color = isPressed ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : ImGui.GetColorU32(ImGuiCol.Button);
            drawList.AddCircleFilled(pos, faceButtonRadius, color);
            drawList.AddCircle(pos, faceButtonRadius, ImGui.GetColorU32(ImGuiCol.Border), 16, 1.0f);
            var textSize = ImGui.CalcTextSize(btnLabel);
            drawList.AddText(pos - textSize * 0.5f, ImGui.GetColorU32(ImGuiCol.Text), btnLabel);
            if (IsPointClicked(pos, faceButtonRadius)) isPressed = !isPressed;
        }

        public static void DrawFaceButtons(ImDrawListPtr drawList, TASGamePadState state, ImGuiVector2 faceCenter, float faceButtonRadius, float faceSpacing)
        {
            var yPos = new ImGuiVector2(faceCenter.X, faceCenter.Y - faceSpacing);
            var xPos = new ImGuiVector2(faceCenter.X - faceSpacing, faceCenter.Y);
            var bPos = new ImGuiVector2(faceCenter.X + faceSpacing, faceCenter.Y);
            var aPos = new ImGuiVector2(faceCenter.X, faceCenter.Y + faceSpacing);

            DrawFaceButton(drawList, yPos, "Y", faceButtonRadius, ref state.ButtonY);
            DrawFaceButton(drawList, xPos, "X", faceButtonRadius, ref state.ButtonX);
            DrawFaceButton(drawList, bPos, "B", faceButtonRadius, ref state.ButtonB);
            DrawFaceButton(drawList, aPos, "A", faceButtonRadius, ref state.ButtonA);
        }

        public static void DrawAnalogStick(ImDrawListPtr drawList, TASGamePadState state, ImGuiVector2 analogCenter, float analogWidth, float stickRadius = 3.0f)
        {
            drawList.AddRect(
                analogCenter - new ImGuiVector2(analogWidth, analogWidth),
                analogCenter + new ImGuiVector2(analogWidth, analogWidth),
                ImGui.GetColorU32(ImGuiCol.Border), 0
            );

            // Draw X and Y axis lines
            var axisColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
            drawList.AddLine(
                new ImGuiVector2(analogCenter.X - analogWidth, analogCenter.Y),
                new ImGuiVector2(analogCenter.X + analogWidth, analogCenter.Y),
                axisColor, 0.8f
            );
            drawList.AddLine(
                new ImGuiVector2(analogCenter.X, analogCenter.Y - analogWidth),
                new ImGuiVector2(analogCenter.X, analogCenter.Y + analogWidth),
                axisColor, 0.8f
            );

            // Calculate stick position
            var stickPos = new ImGuiVector2(
                analogCenter.X + state.AnalogX * analogWidth,
                analogCenter.Y - state.AnalogY * analogWidth
            );

            var stickColor = ImGui.GetColorU32(ImGuiCol.ButtonActive);
            var stickBorderColor = ImGui.GetColorU32(ImGuiCol.Border);

            drawList.AddCircleFilled(stickPos, stickRadius, stickColor);
            drawList.AddCircle(stickPos, stickRadius, stickBorderColor, 12, 1.0f);

            // Handle analog stick input with invisible button to capture mouse properly
            ImGui.SetCursorScreenPos(analogCenter - new ImGuiVector2(analogWidth, analogWidth));
            ImGui.InvisibleButton("analog_stick", new ImGuiVector2(analogWidth * 2, analogWidth * 2));

            // Track mouse position for analog stick - continue tracking even when dragging outside the area
            if ((ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) ||
                (ImGui.IsItemActive() && ImGui.IsMouseDown(ImGuiMouseButton.Left)))
            {
                var mousePos = ImGui.GetMousePos();
                var delta = (mousePos - analogCenter) / analogWidth;
                state.AnalogX = Math.Clamp(delta.X, -1f, 1f);
                state.AnalogY = Math.Clamp(-delta.Y, -1f, 1f);
            }
        }
    }
}