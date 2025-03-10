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
    public class HighlightState
    {
        public Vector2 Tile;
        public Color BgColor;

        public HighlightState(Vector2 tile)
        {
            Tile = tile;
            BgColor = new Color(128, 0, 128, 196);
        }

        public HighlightState(Vector2 tile, Color col)
        {
            Tile = tile;
            BgColor = col;
        }
    }

    public class TileHighlight : IOverlay
    {
        public override string Name => "TileHighlight";
        public static List<HighlightState> States = new List<HighlightState>();
        public static HashSet<Vector2> Tiles = new HashSet<Vector2>();
        public static bool DrawOrder = false;
        public Color HighlightColor = new Color(128, 0, 128, 196);
        public override string Description => "highlight set tiles";

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < States.Count; ++i)
            {
                DrawFilledTile(spriteBatch, States[i].Tile, States[i].BgColor);
                if (DrawOrder)
                {
                    DrawCenteredTextInTile(
                        spriteBatch,
                        States[i].Tile,
                        (i + 1).ToString(),
                        Color.White,
                        2
                    );
                }
            }
        }

        public override void RenderImGui()
        {
            if (ImGui.CollapsingHeader("TileHighlight"))
            {
                ImGui.Checkbox("Toggle Order", ref DrawOrder);
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

        public static void Add(Vector2 tile)
        {
            if (Tiles.Contains(tile))
                return;

            Tiles.Add(tile);
            States.Add(new HighlightState(tile));
        }

        public static void Add(Vector2 tile, Color col)
        {
            if (Tiles.Contains(tile))
            {
                for (int i = 0; i < States.Count; ++i)
                {
                    if (States[i].Tile == tile)
                    {
                        States[i].BgColor = col;
                        return;
                    }
                }
                return;
            }

            Tiles.Add(tile);
            States.Add(new HighlightState(tile, col));
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
