using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using TASMod.Console.Commands;
using TASMod.Inputs;
using TASMod.Monogame.Framework;

namespace TASMod.Console
{
    public class TASConsole
    {
        public readonly Color WarningColor = Color.Goldenrod;
        public bool ShowWarnings = false;
        public bool ShowErrors = false;
        public bool DebugMode = false;
        private TASSpriteBatch spriteBatch;
        public SpriteFont consoleFont;
        public Texture2D solidColor;
        public static ConsoleInputHandler handler;
        public Dictionary<string, IConsoleCommand> Commands;
        public Overlays.Mouse consoleMouse = new(Color.Wheat);
        public string nextFrameCommand = null;

        public List<string> GetCommands()
        {
            return Commands.Keys.ToList();
        }

        public Dictionary<string, string> Aliases;
        public Stack<string> ActiveSubscribers;

        public float fontSize = 1f;
        private const int LEFTPAD = 5;
        private const int RIGHTPAD = 16;
        private const int TABSTOP = 2;
        private char[] SplitTokens = { ' ', '\t', '.', ',', '(', ')', ':', ';', '=' };

        public Color backgroundEntryColor = new Color(40, 40, 40, 220);
        public Color textEntryColor = new Color(100, 180, 180, 255);
        public Color cursorColor = new Color(180, 180, 180, 128);
        public Rectangle entryRect;
        public string entryText = "";
        public int cursorPosition;

        public Color backgroundHistoryColor = new Color(10, 10, 10, 220);
        public Color textHistoryColor = new Color(180, 180, 180, 255);
        private Rectangle historyRect;
        public int historyRectRows;
        public List<ConsoleTextElement> historyLog;
        public int historyTail;
        public bool followLogUpdate;
        public LinkedList<string> entryLog;
        public int entryIndex;

        public float openHeight = 0f;
        public float openHeightTarget = 0f;
        public float openHeightMax = 0.5f;
        public float openRate = 0.04f;
        public bool IsOpen => openHeight > 0;
        public bool IsOpenMax => openHeight == openHeightMax;

        public void Open()
        {
            if (!IsOpenMax)
                openHeightTarget = openHeightMax;
        }

        public void Close()
        {
            if (IsOpen)
            {
                openHeightTarget = 0;
                isScrollbarDragging = false;
                isScrollbarHovering = false;
            }
        }

        public bool IsSelecting;
        public int SelectStart = -1;
        public int SelectEnd = -1;

        // Scrollbar state
        private bool isScrollbarDragging = false;
        private bool isScrollbarHovering = false;
        private int scrollbarDragStartY = 0;
        private int scrollbarDragStartTail = 0;
        private int scrollbarWidth = 16;
        private int scrollbarThumbHeight = 0;
        private Rectangle scrollbarRect;
        private Rectangle scrollbarThumbRect;

        public TASConsole()
        {
            try
            {
                ExternalLogger.Init(Environment.ProcessId);
            }
            catch (Exception ex)
            {
                ModEntry.Console.Log($"Failed to initialize ExternalLogger: {ex}", StardewModdingAPI.LogLevel.Error);
            }

            handler = new ConsoleInputHandler(GameRunner.instance.Window, this);
            solidColor = new Texture2D(
                Game1.graphics.GraphicsDevice,
                1,
                1,
                false,
                SurfaceFormat.Color
            );
            Color[] data = new Color[1] { new Color(255, 255, 255, 255) };
            solidColor.SetData(data);

            consoleFont = Game1.content.Load<SpriteFont>("Fonts/ConsoleFont");
            spriteBatch = new TASSpriteBatch(Game1.graphics.GraphicsDevice);
            spriteBatch.PrintAllChars(consoleFont);

            historyLog = new List<ConsoleTextElement>();

            Commands = new Dictionary<string, IConsoleCommand>();
            foreach (
                var v in Reflector.GetTypesInNamespace(
                    Assembly.GetExecutingAssembly(),
                    "TASMod.Console.Commands"
                )
            )
            {
                if (v.IsAbstract || v.BaseType != typeof(IConsoleCommand))
                    continue;
                IConsoleCommand command = (IConsoleCommand)Activator.CreateInstance(v);
                Commands.Add(command.Name, command);
                ModEntry.Console.Log(
                    string.Format("Command \"{0}\" added to console", command.Name),
                    StardewModdingAPI.LogLevel.Info
                );
            }
            Aliases = new Dictionary<string, string>();
            ActiveSubscribers = new Stack<string>();

            LoadConsoleState();
        }

        public void Update()
        {
            if (IsOpenMax)
            {
                if (RealInputState.ScrollWheelTriggered() && !isScrollbarDragging && TASSpriteBatch.Active)
                {
                    followLogUpdate = false;
                    if (historyLog.Count > historyRectRows)
                    {
                        int dir = RealInputState.ScrollWheelDiff();
                        if (dir < 0)
                        {
                            // up?
                            for (historyTail++; historyTail < historyLog.Count; historyTail++)
                            {
                                if (historyTail >= historyLog.Count) continue;
                                if (historyLog[historyTail].Type == ConsoleTextElementType.Warn && !ShowWarnings) continue;
                                if (historyLog[historyTail].Type == ConsoleTextElementType.Error && !ShowErrors) continue;
                                if (historyLog[historyTail].Type == ConsoleTextElementType.Debug && !DebugMode) continue;

                                break;
                            }
                        }
                        else
                        {
                            // down
                            for (historyTail--; historyTail > 0; historyTail--)
                            {
                                if (historyTail >= historyLog.Count) continue;
                                if (historyLog[historyTail].Type == ConsoleTextElementType.Warn && !ShowWarnings) continue;
                                if (historyLog[historyTail].Type == ConsoleTextElementType.Error && !ShowErrors) continue;
                                if (historyLog[historyTail].Type == ConsoleTextElementType.Debug && !DebugMode) continue;

                                break;
                            }
                        }
                        historyTail = Math.Min(
                            Math.Max(historyRectRows - 1, historyTail),
                            historyLog.Count
                        );
                    }
                    else
                    {
                        historyTail = historyLog.Count;
                    }
                }
                else
                {
                    if (followLogUpdate)
                    {
                        historyTail = historyLog.Count;
                    }
                }
                // add dragging support for clicking on the side to the right of the historyRect.Width

                // Handle scrollbar mouse interactions
                var mouseState = RealInputState.mouseState;
                var oldMouseState = RealInputState.oldMouseState;

                // Check for hover state
                if (historyLog.Count > historyRectRows)
                {
                    isScrollbarHovering = scrollbarThumbRect.Contains(mouseState.X, mouseState.Y);
                }
                else
                {
                    isScrollbarHovering = false;
                }

                if (RealInputState.LeftMouseClicked())
                {
                    if (scrollbarThumbRect.Contains(mouseState.X, mouseState.Y))
                    {
                        isScrollbarDragging = true;
                        scrollbarDragStartY = mouseState.Y;
                        scrollbarDragStartTail = historyTail;
                    }
                }
                else if (RealInputState.LeftMouseReleased())
                {
                    isScrollbarDragging = false;
                }

                if (isScrollbarDragging && historyLog.Count > historyRectRows)
                {
                    int mouseDelta = mouseState.Y - scrollbarDragStartY;
                    float scrollableHeight = scrollbarRect.Height - scrollbarThumbHeight;
                    if (scrollableHeight > 0)
                    {
                        float scrollPercent = (float)mouseDelta / scrollableHeight;
                        int maxScroll = historyLog.Count - historyRectRows;
                        int newTail = scrollbarDragStartTail + (int)(scrollPercent * maxScroll);
                        historyTail = Math.Max(historyRectRows, Math.Min(historyLog.Count, newTail));
                    }
                }
            }
            openHeight += Math.Sign(openHeightTarget - openHeight) * openRate;
            openHeight = Math.Min(openHeightMax, Math.Max(openHeight, 0));

            historyRect.Width = Game1.graphics.GraphicsDevice.Viewport.Width - RIGHTPAD;
            historyRect.Height = (int)(openHeight * Game1.graphics.GraphicsDevice.Viewport.Height);
            historyRectRows = historyRect.Height / consoleFont.LineSpacing;
            entryRect.Width = Game1.graphics.GraphicsDevice.Viewport.Width;
            entryRect.Height = consoleFont.LineSpacing * 3 / 2;
            entryRect.Y = historyRect.Height;

            // Calculate scrollbar dimensions
            scrollbarRect = new Rectangle(
                Game1.graphics.GraphicsDevice.Viewport.Width - scrollbarWidth,
                0,
                scrollbarWidth,
                historyRect.Height
            );

            // Calculate scrollbar thumb dimensions
            if (historyLog.Count > historyRectRows)
            {
                float thumbRatio = (float)historyRectRows / historyLog.Count;
                scrollbarThumbHeight = Math.Max(20, (int)(scrollbarRect.Height * thumbRatio));

                float scrollPosition = (float)(historyTail - historyRectRows) / (historyLog.Count - historyRectRows);
                int thumbY = (int)(scrollPosition * (scrollbarRect.Height - scrollbarThumbHeight));

                scrollbarThumbRect = new Rectangle(
                    scrollbarRect.X,
                    scrollbarRect.Y + thumbY,
                    scrollbarRect.Width,
                    scrollbarThumbHeight
                );
            }
            else
            {
                scrollbarThumbHeight = scrollbarRect.Height;
                scrollbarThumbRect = scrollbarRect;
            }

            if (nextFrameCommand != null)
            {
                string cmd = nextFrameCommand;
                nextFrameCommand = null;
                PushCommand(cmd);
            }
        }

        public void Draw()
        {
            if (!IsOpen)
                return;

            string prefix = " ";
            int lineWidth = (int)(Game1.viewport.Width / consoleFont.MeasureString(" ").X) - 8;
            if (ActiveSubscribers.Count > 0)
            {
                prefix = Commands[ActiveSubscribers.Peek()].SubscriberPrefix;
            }
            Vector2 offset = consoleFont.MeasureString(prefix) * fontSize;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null
            );
            // draw the history
            Vector2 historyLoc = new Vector2(
                LEFTPAD,
                historyRect.Height - 1.25f * consoleFont.LineSpacing
            );
            spriteBatch.Draw(
                solidColor,
                historyRect,
                null,
                backgroundHistoryColor,
                0,
                Vector2.Zero,
                SpriteEffects.None,
                0
            );

            int index = historyTail - 1;
            while (historyLoc.Y + consoleFont.LineSpacing > 0 && index >= 0)
            {
                if (historyLog[index].Visible)
                {
                    if (historyLog[index].Type == ConsoleTextElementType.Warn && !ShowWarnings)
                    {
                        index--;
                        continue;
                    }
                    if (historyLog[index].Type == ConsoleTextElementType.Error && !ShowErrors)
                    {
                        index--;
                        continue;
                    }
                    string text = new string('.', prefix.Length - 1) + " " + historyLog[index].Text;
                    text = Constants.StripUsername(text);
                    text = text.Replace("\t", new string(' ', TABSTOP));
                    spriteBatch.DrawSafeString(
                        consoleFont,
                        text,
                        historyLoc,
                        historyLog[index].Color,
                        0f,
                        Vector2.Zero,
                        fontSize,
                        SpriteEffects.None,
                        0.999999f
                    );
                    historyLoc.Y -= consoleFont.LineSpacing;
                }
                index--;
            }

            // draw the entry block
            Vector2 entryLoc = new Vector2(LEFTPAD, entryRect.Y);
            spriteBatch.Draw(
                solidColor,
                entryRect,
                null,
                backgroundEntryColor,
                0,
                Vector2.Zero,
                SpriteEffects.None,
                0
            );
            if (ActiveSubscribers.Count > 0)
            {
                spriteBatch.DrawString(
                    consoleFont,
                    prefix,
                    entryLoc,
                    textEntryColor,
                    0f,
                    Vector2.Zero,
                    fontSize,
                    SpriteEffects.None,
                    0.999999f
                );
                entryLoc.X += offset.X;
            }

            string renderText = entryText.Replace("\t", new string(' ', TABSTOP));
            spriteBatch.DrawSafeString(
                consoleFont,
                renderText,
                entryLoc,
                textEntryColor,
                0f,
                Vector2.Zero,
                fontSize,
                SpriteEffects.None,
                0.9999999f
            );

            Vector2 characterSize = consoleFont.MeasureString(" ") * fontSize;
            int renderCursorPosition = cursorPosition;
            if (entryText.Length > 0)
            {
                for (int i = 0; i < Math.Min(cursorPosition, entryText.Length); i++)
                {
                    if (entryText[i] == '\t')
                    {
                        renderCursorPosition += (TABSTOP - 1);
                    }
                }
            }
            Rectangle cursorRect = new Rectangle(
                (int)(entryLoc.X + characterSize.X * renderCursorPosition),
                (int)entryLoc.Y,
                (int)characterSize.X,
                (int)characterSize.Y
            );
            spriteBatch.Draw(
                solidColor,
                cursorRect,
                null,
                cursorColor,
                0,
                Vector2.Zero,
                SpriteEffects.None,
                0
            );

            if (SelectStart != -1 && SelectEnd != -1)
            {
                int selectStartPos = SelectStart;
                int selectEndPos = SelectEnd;
                for (int i = 0; i < SelectStart; i++)
                {
                    if (entryText[i] == '\t')
                    {
                        selectStartPos += (TABSTOP - 1);
                    }
                }
                for (int i = 0; i < SelectEnd; i++)
                {
                    if (entryText[i] == '\t')
                    {
                        selectEndPos += (TABSTOP - 1);
                    }
                }
                int minX = (int)(
                    Math.Min(selectStartPos, selectEndPos) * characterSize.X + entryLoc.X
                );
                int maxX = (int)(
                    Math.Max(selectStartPos, selectEndPos) * characterSize.X + entryLoc.X
                );
                Rectangle selectRect = new Rectangle(
                    minX,
                    (int)entryLoc.Y,
                    (int)((maxX - minX) + characterSize.X),
                    (int)characterSize.Y
                );
                spriteBatch.Draw(
                    solidColor,
                    selectRect,
                    null,
                    cursorColor,
                    0,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0
                );
            }

            // Draw scrollbar track
            spriteBatch.Draw(
                solidColor,
                scrollbarRect,
                null,
                new Color(60, 60, 60, 200),
                0,
                Vector2.Zero,
                SpriteEffects.None,
                0.99f
            );
            // Draw scrollbar if needed
            if (historyLog.Count > historyRectRows)
            {
                // Draw entry markers on scrollbar
                Color entryMarkerColor = new Color(180, 180, 100, 200); // Yellow-ish markers
                for (int i = 0; i < historyLog.Count; i++)
                {
                    if (historyLog[i].Entry)
                    {
                        // Calculate the position of this entry on the scrollbar
                        float entryPosition = (float)i / historyLog.Count;
                        int markerY = scrollbarRect.Y + (int)(entryPosition * scrollbarRect.Height);

                        // Draw a small horizontal line
                        Rectangle markerRect = new Rectangle(
                            scrollbarRect.X,
                            markerY,
                            scrollbarRect.Width,
                            2 // 2 pixel height for visibility
                        );

                        spriteBatch.Draw(
                            solidColor,
                            markerRect,
                            null,
                            entryMarkerColor,
                            0,
                            Vector2.Zero,
                            SpriteEffects.None,
                            0.9999f
                        );
                    }
                }

                // Draw scrollbar thumb
                Color thumbColor;
                if (isScrollbarDragging)
                {
                    thumbColor = new Color(140, 140, 140, 255); // Brightest when dragging
                }
                else if (isScrollbarHovering)
                {
                    thumbColor = new Color(120, 120, 120, 255); // Medium when hovering
                }
                else
                {
                    thumbColor = new Color(100, 100, 100, 255); // Default state
                }

                spriteBatch.Draw(
                    solidColor,
                    scrollbarThumbRect,
                    null,
                    thumbColor,
                    0,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.999f
                );
            }

            consoleMouse.ActiveDraw(spriteBatch);
            spriteBatch.End();
        }

        public void ResetSelection()
        {
            IsSelecting = false;
            SelectStart = -1;
            SelectEnd = -1;
        }

        public void UpdateSelection(int start, int end)
        {
            if (!IsSelecting)
            {
                ResetSelection();
                return;
            }
            SelectStart = SelectStart == -1 ? start : SelectStart;
            SelectEnd = end;
        }

        public void SendStop()
        {
            if (ActiveSubscribers.Count == 0)
                return;
            Commands[ActiveSubscribers.Peek()].Stop();
        }

        public bool HandleSubscribers(string command)
        {
            if (ActiveSubscribers.Count > 0)
            {
                string name = ActiveSubscribers.Peek();
                Commands[name].ReceiveInput(command);
                return true;
            }
            return false;
        }

        public void RunOnNextUpdate(string command)
        {
            // Implementation for running a command on the next update
            nextFrameCommand = command;
        }

        public void PushCommand(string command)
        {
            if (HandleSubscribers(command))
                return;

            if (command != "")
            {
                PushEntry(command);
                RunCommand(command);
            }
            else
            {
                PushResult("");
            }
        }

        public void RunCommand(string command)
        {
            ModEntry.Console.Log($"calling RunCommand: {command}");
            if (Aliases.ContainsKey(command))
            {
                RunCommand(Aliases[command]);
                return;
            }
            string[] tokens = command.Trim().Split(' ');
            string func = tokens[0];
            string[] parameters = tokens.Skip(1).ToArray();
            if (Commands.ContainsKey(func))
            {
                Commands[func].Run(parameters);
            }
        }

        public void PushEntry(string entry)
        {
            historyLog.Add(new ConsoleTextElement(entry, true, color: textEntryColor));
            ExternalLogger.TryQueueMessage(entry, "Command");
            if (entryLog.Count == 0 || entryLog.Last.Value != entry)
            {
                entryLog.AddLast(entry);
            }

        }

        public void SaveConsoleState()
        {
            string filePath = Path.Combine(
                Constants.BasePath, "console_history.txt"
            );
            using (StreamWriter file = new StreamWriter(filePath, false))
            {
                foreach (var entry in entryLog)
                {
                    file.WriteLine(entry);
                }
            }
        }

        public void LoadConsoleState()
        {
            string filePath = Path.Combine(
                Constants.BasePath, "console_history.txt"
            );
            if (entryLog == null)
                entryLog = new LinkedList<string>();
            if (!File.Exists(filePath))
                return;
            entryLog.Clear();
            using (StreamReader file = File.OpenText(filePath))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    entryLog.AddLast(line);
                }
            }
        }

        public void PushEntry(string entry, Color color)
        {
            historyLog.Add(new ConsoleTextElement(entry, true, color: color));
            ExternalLogger.TryQueueMessage(entry, "Command");
            if (entryLog.Count == 0 || entryLog.Last.Value != entry)
            {
                entryLog.AddLast(entry);
            }
        }

        public void PushResult(string result)
        {
            // followLogUpdate = historyTail == historyLog.Count;
            if (followLogUpdate)
                historyTail++;
            historyLog.Add(new ConsoleTextElement(result, false, color: textHistoryColor));
            ExternalLogger.TryQueueMessage(result, "Trace");
        }

        public void PushResult(string result, ConsoleTextElementType type, Color color)
        {
            // followLogUpdate = historyTail == historyLog.Count;
            if (followLogUpdate)
                historyTail++;
            historyLog.Add(new ConsoleTextElement(result, false, type: type, color: color));
            switch (type)
            {
                case ConsoleTextElementType.Error:
                    if (ShowErrors)
                    {
                        ExternalLogger.TryQueueMessage(result, type.ToString());
                    }
                    break;
                case ConsoleTextElementType.Debug:
                    if (DebugMode)
                    {
                        ExternalLogger.TryQueueMessage(result, type.ToString());
                    }
                    break;
                case ConsoleTextElementType.Warn:
                    if (ShowWarnings)
                    {
                        ExternalLogger.TryQueueMessage(result, type.ToString());
                    }
                    break;
                default:
                    ExternalLogger.TryQueueMessage(result, "Trace");
                    break;
            }
        }

        public void ResetEntry()
        {
            entryText = "";
            cursorPosition = 0;
        }

        public void ResetHistoryPointers()
        {
            entryIndex = entryLog.Count;
            historyTail = historyLog.Count;
        }

        public void ReceiveCommandInput(char command)
        {
            if (!IsOpenMax)
                return;
            Controller.Console.Debug($"ReceiveCommandInput: {command} {char.IsControl(command)}");
            switch (command)
            {
                case '\u0003': // copy
                    if (SelectStart != -1)
                    {
                        int minX = Math.Min(SelectStart, SelectEnd);
                        int offset = Math.Abs(SelectStart - SelectEnd);
                        DesktopClipboard.SetText(entryText.Substring(minX, offset));
                    }
                    else
                    {
                        ResetEntry();
                        ResetHistoryPointers();
                    }
                    break;
                case '\u0016': //paste
                    HandlePaste();
                    break;
                case '\r':
                    PushCommand(entryText);
                    ResetEntry();
                    ResetHistoryPointers();
                    break;
                case '\t':
                    ReceiveTextInput('\t');
                    break;
                default:
                    //ModEntry.Console.Log($"Pressed Command Key: {command}", LogLevel.Warn);
                    break;
            }
        }

        public void ReceiveTextInput(char character)
        {
            if (!IsOpenMax)
                return;
            Controller.Console.Debug($"ReceiveTextInput: {character} {char.IsControl(character)}");
            switch (character)
            {
                case '~':
                case '`':
                    if (!handler.ControlKeyDown)
                    {
                        entryText = entryText.Insert(cursorPosition++, character.ToString());
                    }
                    break;
                default:
                    entryText = entryText.Insert(cursorPosition++, character.ToString());
                    break;
            }
        }

        public void ReceiveKey(Keys key)
        {
            Controller.Console.Debug($"ReceiveKey: {key} (Control:{handler.ControlKeyDown})");
            IsSelecting = handler.LeftShiftDown;
            int startCursor = cursorPosition;
            switch (key)
            {
                case Keys.OemTilde:
                    if (handler.ControlKeyDown)
                    {
                        Controller.Console.Debug($"Current console: {IsOpen}");
                        if (IsOpen)
                        {
                            Close();
                        }
                        else
                        {
                            Open();
                        }
                    }
                    break;
                case Keys.Escape:
                    if (!IsOpenMax)
                        return;
                    ResetSelection();
                    break;
                case Keys.Up:
                    if (!IsOpenMax)
                        return;
                    BackHistory();
                    ResetSelection();
                    break;
                case Keys.Down:
                    if (!IsOpenMax)
                        return;
                    ForwardHistory();
                    ResetSelection();
                    break;
                case Keys.Left:
                    if (!IsOpenMax)
                        return;
                    if (handler.AltKeyDown || handler.ControlKeyDown)
                    {
                        string[] tokens = entryText.Split(SplitTokens);
                        int count = 0;
                        foreach (string token in tokens)
                        {
                            if (count + token.Length + 1 >= cursorPosition)
                            {
                                break;
                            }
                            count += token.Length + 1;
                        }
                        // clamp
                        cursorPosition = Math.Max(0, count);
                    }
                    else
                    {
                        cursorPosition = Math.Max(0, cursorPosition - 1);
                    }
                    UpdateSelection(startCursor, cursorPosition);
                    break;
                case Keys.Right:
                    if (!IsOpenMax)
                        return;
                    if (handler.AltKeyDown || handler.ControlKeyDown)
                    {
                        string[] tokens = entryText.Split(SplitTokens);
                        int count = 0;
                        foreach (string token in tokens)
                        {
                            if (count > cursorPosition)
                            {
                                break;
                            }
                            count += token.Length + 1;
                        }
                        // clamp
                        cursorPosition = Math.Min(count, entryText.Length);
                    }
                    else
                    {
                        cursorPosition = Math.Min(entryText.Length, cursorPosition + 1);
                    }
                    UpdateSelection(startCursor, cursorPosition);
                    break;
                case Keys.Home:
                    if (!IsOpenMax)
                        return;
                    cursorPosition = 0;
                    UpdateSelection(startCursor, cursorPosition);
                    break;
                case Keys.End:
                    if (!IsOpenMax)
                        return;
                    cursorPosition = entryText.Length;
                    UpdateSelection(startCursor, cursorPosition);
                    break;
                case Keys.Back:
                case Keys.Delete:
                    if (!IsOpenMax)
                        return;
                    if (entryText.Length > 0)
                    {
                        if (SelectStart != -1)
                        {
                            int minX = Math.Min(SelectStart, SelectEnd);
                            int off = Math.Min(
                                Math.Abs(SelectStart - SelectEnd),
                                entryText.Length - 1
                            );
                            entryText = entryText.Remove(minX, off);
                            ResetSelection();
                            cursorPosition = Math.Max(0, minX);
                        }
                        else
                        {
                            if (cursorPosition > 0)
                            {
                                entryText = entryText.Remove(cursorPosition - 1, 1);
                            }
                            cursorPosition = Math.Max(0, cursorPosition - 1);
                        }
                    }
                    break;
            }
        }

        private void HandlePaste()
        {
            string pasteResult = "";
            DesktopClipboard.GetText(ref pasteResult);
            if (pasteResult == null)
                return;
            if (SelectStart != -1)
            {
                SelectEnd++;
                ReceiveKey(Keys.Delete);
            }
            entryText = entryText.Insert(cursorPosition, pasteResult);
            cursorPosition += pasteResult.Length;
        }

        public void BackHistory()
        {
            if (cursorPosition != entryText.Length)
            {
                cursorPosition = entryText.Length;
                return;
            }
            if (--entryIndex >= 0 && entryLog.Count != 0)
            {
                entryText = entryLog.ElementAt(entryIndex);
                cursorPosition = entryText.Length;
                return;
            }
            ResetEntry();
            entryIndex = entryLog.Count;
        }

        public void ForwardHistory()
        {
            if (++entryIndex < entryLog.Count)
            {
                entryText = entryLog.ElementAt(entryIndex);
                cursorPosition = entryText.Length;
                return;
            }
            ResetEntry();
            entryIndex = entryLog.Count;
        }

        public void Clear()
        {
            ResetEntry();
            historyLog.Clear();
            ResetHistoryPointers();
        }

        public void WriteToFile(string name)
        {
            name += ".txt";
            string filePath = Path.Combine(Constants.BasePath, name);
            using (StreamWriter file = File.CreateText(filePath))
            {
                for (int i = 0; i < historyLog.Count; ++i)
                {
                    if (historyLog[i].Visible)
                    {
                        file.WriteLine((historyLog[i].Entry ? ">" : "") + historyLog[i].Text);
                    }
                }
            }
        }

        public void WriteToRandomFile()
        {
            string name = Path.GetRandomFileName().Substring(0, 8) + ".txt";
            string filePath = Path.Combine(Constants.BasePath, name);
            using (StreamWriter file = File.CreateText(filePath))
            {
                for (int i = 0; i < historyLog.Count; ++i)
                {
                    if (historyLog[i].Visible)
                    {
                        file.WriteLine((historyLog[i].Entry ? ">" : "") + historyLog[i].Text);
                    }
                }
            }
        }

        public void Alert(string message)
        {
            PushResult(message, ConsoleTextElementType.Warn, Color.Purple);
        }

        public void Warn(string message)
        {
            PushResult(message, ConsoleTextElementType.Warn, Color.Goldenrod);
        }

        public void Trace(string message)
        {
            PushResult(message, ConsoleTextElementType.Trace, Color.DarkGray);
        }

        public void Error(string message)
        {
            PushResult(message, ConsoleTextElementType.Error, Color.Red);
        }

        public void Debug(string message)
        {
            if (DebugMode)
            {
                PushResult(message, ConsoleTextElementType.Debug, Color.WhiteSmoke);
            }
        }
    }
}
