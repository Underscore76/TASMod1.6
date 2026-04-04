using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace TASMod.Simulators.FishingMinigame;

public class SBeach : SGameLocation
{
    public SBeach(Beach beach)
        : base(beach)
    {
    }
    public SBeach()
    {
    }

    protected SBeach(SBeach beach)
        : base(beach)
    {
    }

    public override SGameLocation Clone()
    {
        return new SBeach(this);
    }

    public override void UpdateWhenCurrentLocation(SGame1 game)
    {
        base.UpdateWhenCurrentLocation(game);
        if (!(game.NextDouble() < 1E-06))
        {
            return;
        }
        Vector2 position = new Vector2(game.Next(15, 47) * 64, game.Next(29, 42) * 64);
        bool draw = true;
        var loc = StardewValley.Game1.currentLocation;
        for (float i = position.Y / 64f; i < (float)loc.map.RequireLayer("Back").LayerHeight; i += 1f)
        {
            if (!loc.isWaterTile((int)position.X / 64, (int)i) || !loc.isWaterTile((int)position.X / 64 - 1, (int)i) || !loc.isWaterTile((int)position.X / 64 + 1, (int)i))
            {
                draw = false;
                break;
            }
        }
        if (draw)
        {
            // adding a sea monster
            game.Next(7);
        }
    }
    public override void checkForMusic(SGame1 game)
    {
        game.NextDouble();
    }

    public override void draw(SGame1 game)
    {
        base.draw(game);
    }
}