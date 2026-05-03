using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class MinesLadder : IOverlay
    {
        public override string Name => "MinesLadder";
        public override string Description => "Shows the best rock to break in the mines";

        public MinesFloor minesFloor;
        public int lineThickness = 2;
        public MinesLadder()
        {
            Active = true;
            minesFloor = new MinesFloor(0);
        }

        public Vector2 GetLadder(int index)
        {
            var locationInfo = InstanceCurrentLocation.Get(index);
            locationInfo.HasLadder(out var ladder);
            return ladder;
        }

        public override void ActiveUpdate()
        {
            int index = ActiveInstance.InstanceIndex;
            minesFloor.SetIndex(index);
            minesFloor.UpdateLadders();
        }

        private Color GetRockColor(int value)
        {
            if (value < 0)
                return Color.DarkGray;
            if (value < 2)
                return Color.Gold;
            if (value < 5)
                return Color.Silver;
            if (value < 8)
                return Color.Green;
            if (value < 11)
                return Color.Orange;
            if (value < 14)
                return Color.Red;
            return Color.DarkGray;
        }

        public override void ActiveDraw(SpriteBatch b)
        {
            int index = ActiveInstance.InstanceIndex;
            var locationInfo = InstanceCurrentLocation.Get(index);
            var playerInfo = InstanceCurrentPlayer.Get(index);
            if (!locationInfo.IsMines)
                return;

            Vector2 baseTile = playerInfo.CurrentTile;
            if (locationInfo.Name != playerInfo.Player.currentLocation.Name)
            {
                baseTile = (locationInfo.Location as MineShaft).tileBeneathLadder;
            }
            // draw best line
            if (minesFloor.HasLadder())
            {
                DrawLineBetweenTiles(index, b, baseTile, minesFloor.GetLadderTile(), Color.LightCyan, lineThickness);
            }
            else if (minesFloor.GetMinLadderCount() != Int32.MaxValue)
            {
                foreach (KeyValuePair<Vector2, int> current in minesFloor.GetLadderCounts())
                {
                    if (current.Value <= minesFloor.GetMinLadderCount() + 10)
                        DrawDepth(index, b, current.Key, current.Value);
                }
                DrawLineBetweenTiles(
                    index,
                    b,
                    baseTile,
                    minesFloor.GetLadderTile(),
                    GetRockColor(minesFloor.GetMinLadderCount()),
                    lineThickness
                );
            }
        }

        private void DrawDepth(int index, SpriteBatch spriteBatch, Vector2 tile, int depth)
        {
            DrawCenteredTextInTile(index, spriteBatch, tile, depth.ToString(), GetRockColor(depth));
        }
    }
}
