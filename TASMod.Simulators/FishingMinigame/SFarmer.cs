using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;

namespace TASMod.Simulators.FishingMinigame;

public class SFarmer
{
    public Vector2 jitter;
    public float jitterStrength;
    public int blinkTimer;
    public SFishingRod fishingRod;
    public bool hasVisibleQuests;
    public bool hasNewQuestActivity;

    public SFarmer(Farmer farmer)
    {
        jitter = farmer.jitter;
        jitterStrength = farmer.jitterStrength;
        blinkTimer = farmer.blinkTimer;
        hasVisibleQuests = farmer.hasVisibleQuests;
        hasNewQuestActivity = farmer.hasNewQuestActivity();
        fishingRod = new SFishingRod(farmer.CurrentTool as FishingRod);
    }
    public SFarmer()
    {
    }

    public SFarmer(SFarmer farmer)
    {
        jitter = farmer.jitter;
        jitterStrength = farmer.jitterStrength;
        blinkTimer = farmer.blinkTimer;
        hasVisibleQuests = farmer.hasVisibleQuests;
        hasNewQuestActivity = farmer.hasNewQuestActivity;
        fishingRod = new SFishingRod(farmer.fishingRod);
    }

    public SFarmer Clone()
    {
        return new SFarmer(this);
    }

    public void Update(SGame1 game)
    {
        updateCommon(game);
    }
    public void updateCommon(SGame1 game)
    {
        if (jitterStrength > 0f)
        {
            jitter = new Vector2((float)game.Next(-(int)(jitterStrength * 100f), (int)((jitterStrength + 1f) * 100f)) / 100f, (float)game.Next(-(int)(jitterStrength * 100f), (int)((jitterStrength + 1f) * 100f)) / 100f);
        }
        blinkTimer += 16;
        if (this.blinkTimer > 2200 && game.NextDouble() < 0.01)
        {
            this.blinkTimer = -150;
        }
        else if (this.blinkTimer > -100)
        {
            if (this.blinkTimer < -50)
            {
            }
            else if (this.blinkTimer < 0)
            {
            }
            else
            {
            }
        }
        fishingRod.tickUpdate(game);

    }

    public void draw(SGame1 game)
    {
        game.drawTool(fishingRod);
    }
}