using System;
using StardewValley;
using StardewValley.Menus;

namespace TASMod.Simulators.FishingMinigame;

public class SDayTimeMoneyBox
{
    public int timeShakeTimer;
    public int questPulseTimer;
    public int whenToPulseTimer;

    public SDayTimeMoneyBox(DayTimeMoneyBox dayTimeMoneyBox)
    {
        this.timeShakeTimer = dayTimeMoneyBox.timeShakeTimer;
        this.questPulseTimer = dayTimeMoneyBox.questPulseTimer;
        this.whenToPulseTimer = dayTimeMoneyBox.whenToPulseTimer;
    }

    public SDayTimeMoneyBox()
    {
    }

    public SDayTimeMoneyBox(SDayTimeMoneyBox dayTimeMoneyBox)
    {
        timeShakeTimer = dayTimeMoneyBox.timeShakeTimer;
        questPulseTimer = dayTimeMoneyBox.questPulseTimer;
        whenToPulseTimer = dayTimeMoneyBox.whenToPulseTimer;
    }

    public SDayTimeMoneyBox Clone()
    {
        return new SDayTimeMoneyBox(this);
    }

    public void draw(SGame1 game)
    {
        if (timeShakeTimer > 0)
        {
            timeShakeTimer -= 16;
        }
        if (questPulseTimer > 0)
        {
            questPulseTimer -= 16; ;
        }
        if (whenToPulseTimer > 0)
        {
            whenToPulseTimer -= 16;
            if (whenToPulseTimer <= 0)
            {
                this.whenToPulseTimer = 3000;
                if (game.player.hasNewQuestActivity)
                {
                    this.questPulseTimer = 1000;
                }
            }
        }

        // timePosition
        if (timeShakeTimer > 0)
        {
            game.Next(-2, 3);
            game.Next(-2, 3);
        }
        if (game.player.hasVisibleQuests)
        {
            float scaleMult = 1f / (Math.Max(300f, Math.Abs(this.questPulseTimer % 1000 - 500)) / 500f);
            if (scaleMult > 1f)
            {
                game.Next(-1, 2);
                game.Next(-1, 2);
            }
        }
    }
}