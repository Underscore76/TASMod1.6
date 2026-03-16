using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Tools;

namespace TASMod.Simulators.FishingMinigame;

public class SFishingRod
{
    public float bobberTimeAccumulator;
    public float timePerBobberBob;
    public int bobberBob;

    public SFishingRod(FishingRod rod)
    {
        bobberTimeAccumulator = rod.bobberTimeAccumulator;
        timePerBobberBob = rod.timePerBobberBob;
        bobberBob = rod.bobberBob;
    }

    public SFishingRod()
    {
    }

    public SFishingRod(SFishingRod rod)
    {
        bobberTimeAccumulator = rod.bobberTimeAccumulator;
        timePerBobberBob = rod.timePerBobberBob;
        bobberBob = rod.bobberBob;
    }

    public SFishingRod Clone()
    {
        return new SFishingRod(this);
    }

    public void tickUpdate(SGame1 game)
    {
        // we are in the reeling block here
        if (!game.currentFrameButtonPressed && game.previousFrameButtonPressed)
        {
            // equiv of Game1.didPlayerJustClickAtAll()
            game.player.jitterStrength = 1f;
        }
        else
        {
            game.player.jitterStrength = 0f;
            game.player.jitter = Vector2.Zero;
        }
        game.Next(-10, 11);
        game.Next(-10, 11);
        bobberTimeAccumulator += 16;
    }
    public void draw(SGame1 game)
    {
        if (bobberTimeAccumulator > timePerBobberBob)
        {
            if (game.NextDouble() < 0.05)
            {
                game.NextBool();
            }
            timePerBobberBob = (bobberBob == 0) ? game.Next(1500, 3500) : game.Next(350, 750);
            bobberTimeAccumulator = 0f;
            // isReeling
            timePerBobberBob = game.Next(25, 75);
            game.Next(-5, 5);
            game.Next(-5, 5);
        }
    }
}