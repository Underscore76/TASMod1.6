using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace TASMod.Simulators.FishingMinigame;

public class STown : SGameLocation
{
    public STown(Town town)
        : base(town)
    {
    }
    public STown()
    {
    }

    protected STown(STown town)
        : base(town)
    {
    }

    public override SGameLocation Clone()
    {
        return new STown(this);
    }
}