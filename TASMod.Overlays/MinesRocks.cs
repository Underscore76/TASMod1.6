// TODO: work by instance so multiple floors can be traced
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.Locations;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class MinesRocks : IOverlay
    {
        public override string Name => "MinesRocks";
        public override string Description => "Show rocks in mines that have drops";

        public Color RectColor = new Color(0, 0, 0, 180);
        public Color TextColor = Color.White;

        public MinesFloor minesFloor;

        public Dictionary<Vector2, List<Tuple<string, int>>> GetSpecificItems(List<string> items)
        {
            return minesFloor.GetStoneContents().Where(kv => kv.Value.Any(i => items.Contains(i.Item1)))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public MinesRocks()
        {
            Active = true;
            minesFloor = new MinesFloor(0);
        }
        public override void ActiveUpdate()
        {
            int index = ActiveInstance.InstanceIndex;
            minesFloor.SetIndex(index);
            minesFloor.UpdateStoneContents();
        }

        public override void ActiveDraw(SpriteBatch b)
        {
            DrawForInstance(ActiveInstance.InstanceIndex, b);
        }
        public void DrawForInstance(int index, SpriteBatch spriteBatch)
        {
            var location = InstanceCurrentLocation.Get(index);
            if (!location.IsMines)
                return;

            foreach (KeyValuePair<Vector2, List<Tuple<string, int>>> current in minesFloor.GetStoneContents())
            {
                DrawTextAtTile(
                    index,
                    spriteBatch,
                    current.Value
                        .Where((o) => o.Item1 != "Stone")
                        .Select(
                            (o) => $"{(o.Item1.StartsWith("(O)") ? Game1.objectData[o.Item1[3..]].Name : o.Item1)} x{o.Item2}"
                        ),
                    current.Key,
                    TextColor,
                    RectColor
                );
            }
        }
    }
}
