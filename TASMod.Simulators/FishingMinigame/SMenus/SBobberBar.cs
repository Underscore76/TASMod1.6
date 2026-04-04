using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace TASMod.Simulators.FishingMinigame;

public class SBobberBar
{
    public const float initialDistanceFromCatching = 0.3f;
    public float everythingShakeTimer;
    public float difficulty;
    public int motionType;

    public float bobberTargetPosition;
    public float bobberPosition;
    public float floaterSinkerAcceleration;
    public float bobberAcceleration;
    public float bobberSpeed;
    public float bobberBarPos;
    public bool bobberInBar;
    public int bobberBarHeight;
    public float bobberBarSpeed;
    public List<string> bobbers = new List<string>();

    public bool treasure;
    public float treasureAppearTimer;
    public float treasureScale;
    public bool treasureCaught;
    public float treasurePosition;
    public float treasureCatchLevel;

    public float distanceFromCatching;
    public Vector2 fishShake;
    public Vector2 barShake;
    public bool perfect;
    public int challengeBaitFishes;
    public float distanceFromCatchPenaltyModifier;
    public bool beginnersRod;

    public SBobberBar(BobberBar bar)
    {
        everythingShakeTimer = bar.everythingShakeTimer;
        difficulty = bar.difficulty;
        motionType = bar.motionType;
        bobberTargetPosition = bar.bobberTargetPosition;
        bobberPosition = bar.bobberPosition;
        floaterSinkerAcceleration = bar.floaterSinkerAcceleration;
        bobberAcceleration = bar.bobberAcceleration;
        bobberSpeed = bar.bobberSpeed;
        bobberBarPos = bar.bobberBarPos;
        bobberInBar = bar.bobberInBar;
        bobberBarHeight = bar.bobberBarHeight;
        bobberBarSpeed = bar.bobberBarSpeed;
        treasure = bar.treasure;
        treasureAppearTimer = bar.treasureAppearTimer;
        treasureScale = bar.treasureScale;
        treasureCaught = bar.treasureCaught;
        treasurePosition = bar.treasurePosition;
        treasureCatchLevel = bar.treasureCatchLevel;
        distanceFromCatching = bar.distanceFromCatching;
        fishShake = bar.fishShake;
        barShake = bar.barShake;
        perfect = bar.perfect;
        challengeBaitFishes = bar.challengeBaitFishes;
        distanceFromCatchPenaltyModifier = bar.distanceFromCatchPenaltyModifier;
        beginnersRod = bar.beginnersRod;
        if (bar.bobbers != null)
        {
            bobbers.AddRange(bar.bobbers);
        }
    }

    public SBobberBar()
    {
    }

    public SBobberBar Clone()
    {
        SBobberBar clone = new SBobberBar
        {
            everythingShakeTimer = everythingShakeTimer,
            difficulty = difficulty,
            motionType = motionType,
            bobberTargetPosition = bobberTargetPosition,
            bobberPosition = bobberPosition,
            floaterSinkerAcceleration = floaterSinkerAcceleration,
            bobberAcceleration = bobberAcceleration,
            bobberSpeed = bobberSpeed,
            bobberBarPos = bobberBarPos,
            bobberInBar = bobberInBar,
            bobberBarHeight = bobberBarHeight,
            bobberBarSpeed = bobberBarSpeed,
            treasure = treasure,
            treasureAppearTimer = treasureAppearTimer,
            treasureScale = treasureScale,
            treasureCaught = treasureCaught,
            treasurePosition = treasurePosition,
            treasureCatchLevel = treasureCatchLevel,
            distanceFromCatching = distanceFromCatching,
            fishShake = fishShake,
            barShake = barShake,
            perfect = perfect,
            challengeBaitFishes = challengeBaitFishes,
            distanceFromCatchPenaltyModifier = distanceFromCatchPenaltyModifier,
            beginnersRod = beginnersRod
        };
        if (bobbers != null)
        {
            clone.bobbers.AddRange(bobbers);
        }
        return clone;
    }

    public void update(SGame1 game)
    {
        bool buttonPressed = game.currentFrameButtonPressed;
        if (everythingShakeTimer > 0f)
        {
            everythingShakeTimer -= 16;
            game.Next(-10, 11);
            game.Next(-10, 11);
        }

        if (game.NextDouble() < (double)(difficulty * (float)((motionType != 2) ? 1 : 20) / 4000f) && (motionType != 2 || bobberTargetPosition == -1f))
        {
            float spaceBelow = 548f - bobberPosition;
            float spaceAbove = bobberPosition;
            float percent = Math.Min(99f, difficulty + (float)game.Next(10, 45)) / 100f;
            bobberTargetPosition = bobberPosition + (float)game.Next((int)Math.Min(0f - spaceAbove, spaceBelow), (int)spaceBelow) * percent;
        }
        switch (motionType)
        {
            case 4:
                floaterSinkerAcceleration = Math.Max(floaterSinkerAcceleration - 0.01f, -1.5f);
                break;
            case 3:
                floaterSinkerAcceleration = Math.Min(floaterSinkerAcceleration + 0.01f, 1.5f);
                break;
        }

        if (Math.Abs(bobberPosition - bobberTargetPosition) > 3f && bobberTargetPosition != -1f)
        {
            bobberAcceleration = (bobberTargetPosition - bobberPosition) / ((float)game.Next(10, 30) + (100f - Math.Min(100f, difficulty)));
            bobberSpeed += (bobberAcceleration - bobberSpeed) / 5f;
        }
        else if (motionType != 2 && game.NextDouble() < (double)(difficulty / 2000f))
        {
            bobberTargetPosition = bobberPosition + (float)(game.NextBool() ? game.Next(-100, -51) : game.Next(50, 101));
        }
        else
        {
            bobberTargetPosition = -1f;
        }
        if (motionType == 1 && game.NextDouble() < (double)(difficulty / 1000f))
        {
            bobberTargetPosition = bobberPosition + (float)(game.NextBool() ? SafeNext(game, -100 - (int)difficulty * 2, -51) : SafeNext(game, 50, 101 + (int)difficulty * 2));
        }
        bobberTargetPosition = Math.Max(-1f, Math.Min(bobberTargetPosition, 548f));
        bobberPosition += bobberSpeed + floaterSinkerAcceleration;
        if (bobberPosition > 532f)
        {
            bobberPosition = 532f;
        }
        else if (bobberPosition < 0f)
        {
            bobberPosition = 0f;
        }
        bobberInBar = bobberPosition + 12f <= bobberBarPos - 32f + (float)bobberBarHeight && bobberPosition - 16f >= bobberBarPos - 32f;

        if (bobberPosition >= (float)(548 - bobberBarHeight) && bobberBarPos >= (float)(568 - bobberBarHeight - 4))
        {
            bobberInBar = true;
        }

        float gravity = buttonPressed ? (-0.25f) : 0.25f;
        if (buttonPressed && gravity < 0f && (bobberBarPos == 0f || bobberBarPos == (float)(568 - bobberBarHeight)))
        {
            bobberBarSpeed = 0f;
        }

        if (bobberInBar)
        {
            gravity *= bobbers.Contains("(O)691") ? 0.3f : 0.6f;
            if (bobbers.Contains("(O)691"))
            {
                for (int i = 0; i < StardewValley.Utility.getStringCountInList(bobbers, "(O)691"); i++)
                {
                    if (bobberPosition + 16f < bobberBarPos + (float)(bobberBarHeight / 2))
                    {
                        bobberBarSpeed -= (i > 0) ? 0.05f : 0.2f;
                    }
                    else
                    {
                        bobberBarSpeed += (i > 0) ? 0.05f : 0.2f;
                    }
                    if (i > 0)
                    {
                        gravity *= 0.9f;
                    }
                }
            }
        }

        float oldPos = bobberBarPos;
        bobberBarSpeed += gravity;
        bobberBarPos += bobberBarSpeed;
        if (bobberBarPos + (float)bobberBarHeight > 568f)
        {
            bobberBarPos = 568 - bobberBarHeight;
            bobberBarSpeed = (0f - bobberBarSpeed) * 2f / 3f * (bobbers.Contains("(O)692") ? ((float)StardewValley.Utility.getStringCountInList(bobbers, "(O)692") * 0.1f) : 1f);
        }
        else if (bobberBarPos < 0f)
        {
            bobberBarPos = 0f;
            bobberBarSpeed = (0f - bobberBarSpeed) * 2f / 3f;

        }

        bool treasureInBar = false;
        if (treasure)
        {
            float oldTreasureAppearTimer = treasureAppearTimer;
            treasureAppearTimer -= 16;
            if (treasureAppearTimer <= 0f)
            {
                if (treasureScale < 1f && !treasureCaught)
                {
                    if (oldTreasureAppearTimer > 0f)
                    {
                        if (bobberBarPos > 274f)
                        {
                            treasurePosition = game.Next(8, (int)bobberBarPos - 20);
                        }
                        else
                        {
                            int min = Math.Min(528, (int)bobberBarPos + bobberBarHeight);
                            int max = 500;
                            treasurePosition = ((min > max) ? (max - 1) : game.Next(min, max));
                        }
                    }
                    treasureScale = Math.Min(1f, treasureScale + 0.1f);
                }
                treasureInBar = treasurePosition + 12f <= bobberBarPos - 32f + (float)bobberBarHeight && treasurePosition - 16f >= bobberBarPos - 32f;
                if (treasureInBar && !treasureCaught)
                {
                    treasureCatchLevel += 0.0135f;
                    game.Next(-2, 3);
                    game.Next(-2, 3);
                    if (treasureCatchLevel >= 1f)
                    {
                        treasureCaught = true;
                    }
                }
                else if (treasureCaught)
                {
                    treasureScale = Math.Max(0f, treasureScale - 0.1f);
                }
                else
                {
                    treasureCatchLevel = Math.Max(0f, treasureCatchLevel - 0.01f);
                }
            }
        }
        if (bobberInBar)
        {
            distanceFromCatching += 0.002f;
            fishShake.X = game.Next(-10, 11);
            fishShake.Y = game.Next(-10, 11);
            barShake = Vector2.Zero;
        }
        else if (!treasureInBar || treasureCaught || !bobbers.Contains("(O)693"))
        {
            if (!fishShake.Equals(Vector2.Zero))
            {
                perfect = false;
                if (challengeBaitFishes > 0)
                {
                    challengeBaitFishes--;
                    if (challengeBaitFishes <= 0)
                    {
                        distanceFromCatching = 0f;
                    }
                }
            }
            if ((StardewValley.Game1.player.fishCaught != null && StardewValley.Game1.player.fishCaught.Length != 0) || StardewValley.Game1.currentMinigame != null)
            {
                if (bobbers.Contains("(O)694"))
                {
                    float reduction = 0.003f;
                    float amount = 0.001f;
                    for (int j = 0; j < StardewValley.Utility.getStringCountInList(bobbers, "(O)694"); j++)
                    {
                        reduction -= amount;
                        amount /= 2f;
                    }
                    reduction = Math.Max(0.001f, reduction);
                    distanceFromCatching -= reduction * distanceFromCatchPenaltyModifier;
                }
                else
                {
                    distanceFromCatching -= (beginnersRod ? 0.002f : 0.003f) * distanceFromCatchPenaltyModifier;
                }
            }
            barShake.X = game.Next(-10, 11) / 10f;
            barShake.Y = game.Next(-10, 11) / 10f;
            fishShake = Vector2.Zero;
        }
        distanceFromCatching = Math.Max(0f, Math.Min(1f, distanceFromCatching));

        game.player.fishingRod.tickUpdate(game);
        if (bobberPosition < 0f)
        {
            bobberPosition = 0f;
        }
        if (bobberPosition > 548f)
        {
            bobberPosition = 548f;
        }
    }

    private int SafeNext(SGame1 game, int minValue, int maxValue)
    {
        if (minValue >= maxValue)
        {
            return maxValue;
        }
        return game.Next(minValue, maxValue);
    }
}
