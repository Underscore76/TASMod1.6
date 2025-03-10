using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TASMod.Overlays
{
    public class TileTextElement
    {
        public string Text;
        public Vector2 Tile;
        public Color BgColor;

        public TileTextElement(Vector2 tile, string text)
        {
            Tile = tile;
            BgColor = new Color(128, 0, 128, 196);
            Text = text;
        }

        public TileTextElement(Vector2 tile, Color col, string text)
        {
            Tile = tile;
            BgColor = col;
            Text = text;
        }
    }

    public class TileText : IOverlay
    {
        public override string Name => "TileText";
        public static List<TileTextElement> States = new List<TileTextElement>();
        public static HashSet<Vector2> Tiles = new HashSet<Vector2>();
        public Color HighlightColor = new Color(128, 0, 128, 196);
        public override string Description => "draw text on tiles";

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < States.Count; ++i)
            {
                DrawFilledTile(spriteBatch, States[i].Tile, States[i].BgColor);
                float scale = FitTextInTile(States[i].Text);

                DrawCenteredTextInTile(
                    spriteBatch,
                    States[i].Tile,
                    States[i].Text,
                    Color.White,
                    scale
                );
            }
        }

        public override void RenderImGui()
        {
            if (ImGui.CollapsingHeader("TileText"))
            {
                if (ImGui.Button("Clear"))
                {
                    Clear();
                }
                if (ImGui.CollapsingHeader("Tiles"))
                {
                    foreach (var tile in Tiles)
                    {
                        bool isChecked = false;
                        if (ImGui.Checkbox(tile.ToString(), ref isChecked))
                        {
                            Remove(tile);
                        }
                    }
                }
            }
            base.RenderImGui();
        }

        public static void Add(Vector2 tile, string text)
        {
            if (Tiles.Contains(tile))
            {
                for (int i = 0; i < States.Count; ++i)
                {
                    if (States[i].Tile == tile)
                    {
                        States[i].Text += "," + text;
                        return;
                    }
                }
                return;
            }
            Tiles.Add(tile);
            States.Add(new TileTextElement(tile, text));
        }

        public static void Add(Vector2 tile, Color col, string text)
        {
            if (Tiles.Contains(tile))
            {
                for (int i = 0; i < States.Count; ++i)
                {
                    if (States[i].Tile == tile)
                    {
                        States[i].BgColor = col;
                        States[i].Text += "," + text;
                        return;
                    }
                }
                return;
            }

            Tiles.Add(tile);
            States.Add(new TileTextElement(tile, col, text));
        }

        public static bool Contains(Vector2 tile)
        {
            return Tiles.Contains(tile);
        }

        public static void Remove(Vector2 tile)
        {
            if (!Tiles.Contains(tile))
                return;
            Tiles.Remove(tile);
            States = States.Where((o) => o.Tile != tile).ToList();
        }

        public static void Clear()
        {
            Tiles.Clear();
            States.Clear();
        }
    }
}
