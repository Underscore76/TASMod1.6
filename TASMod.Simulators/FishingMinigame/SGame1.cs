
using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Menus;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Simulators.FishingMinigame;

public class SGame1
{
    public SGameLocation loc;
    public SDayTimeMoneyBox dayTimeMoneyBox;
    public SFarmer player;
    public SBobberBar bobberBar;
    public Random random;
    public GameTime currentGameTime;
    public bool previousFrameButtonPressed;
    public bool currentFrameButtonPressed;

    public SGame1()
    {
        switch (Game1.currentLocation?.Name)
        {
            case "Beach":
                loc = Game1.currentLocation != null ? new SBeach(Game1.currentLocation as Beach) : null;
                break;
            case "Town":
                loc = Game1.currentLocation != null ? new STown(Game1.currentLocation as Town) : null;
                break;
            default:
                throw new NotImplementedException($"Location {Game1.currentLocation?.Name} not implemented in SGame1");
        }
        dayTimeMoneyBox = Game1.dayTimeMoneyBox != null ? new SDayTimeMoneyBox(Game1.dayTimeMoneyBox) : null;
        player = Game1.player != null ? new SFarmer(Game1.player) : null;
        bobberBar = Game1.activeClickableMenu is BobberBar ? new SBobberBar(Game1.activeClickableMenu as BobberBar) : null;
        random = Game1.random.Copy();
        currentGameTime = Game1.currentGameTime != null ? new GameTime(Game1.currentGameTime.TotalGameTime, Game1.currentGameTime.ElapsedGameTime, Game1.currentGameTime.IsRunningSlowly) : null;
        previousFrameButtonPressed =
            Game1.oldMouseState.LeftButton == ButtonState.Pressed
            || Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton)
            || (Game1.options.gamepadControls
                && (Game1.oldPadState.IsButtonDown(Buttons.X) || Game1.oldPadState.IsButtonDown(Buttons.A)));
    }

    public SGame1(SGame1 game)
    {
        loc = game.loc?.Clone();
        dayTimeMoneyBox = game.dayTimeMoneyBox?.Clone();
        player = game.player?.Clone();
        bobberBar = game.bobberBar?.Clone();
        random = game.random.Copy();
        if (game.currentGameTime != null)
        {
            currentGameTime = new GameTime(game.currentGameTime.TotalGameTime, game.currentGameTime.ElapsedGameTime, game.currentGameTime.IsRunningSlowly);
        }
        previousFrameButtonPressed = game.previousFrameButtonPressed;
    }

    public int Next(int maxValue)
    {
        // Controller.Console.Trace(Environment.StackTrace);
        return random.Next(maxValue);
    }
    public int Next(int minValue, int maxValue)
    {
        // Controller.Console.Trace(Environment.StackTrace);
        return random.Next(minValue, maxValue);
    }
    public int Next()
    {
        // Controller.Console.Trace(Environment.StackTrace);
        return random.Next();
    }
    public bool NextBool()
    {
        // Controller.Console.Trace(Environment.StackTrace);
        return random.NextBool();
    }

    public double NextDouble()
    {
        // Controller.Console.Trace(Environment.StackTrace);
        return random.NextDouble();
    }

    public int Choose(int optionA, int optionB)
    {
        // Controller.Console.Trace(Environment.StackTrace);
        return random.Choose(optionA, optionB);
    }

    public SGame1 Clone()
    {
        return new SGame1(this);
    }
    public void Press()
    {
        _update(true);
        _draw();
    }
    public void Release()
    {
        _update(false);
        _draw();
    }

    public void _update(bool pressed)
    {
        currentFrameButtonPressed = previousFrameButtonPressed;
        previousFrameButtonPressed = pressed;

        updateActiveMenu();
        UpdateCharacters();
        UpdateLocations();
        UpdateOther();
    }
    public void UpdateCharacters()
    {
        player.Update(this);
    }
    public void UpdateLocations()
    {
        _UpdateLocation(loc);
    }
    public void _UpdateLocation(SGameLocation location)
    {
        location.UpdateWhenCurrentLocation(this);
        //TODO: check if should call location.updateEvenIfFarmerIsntHere() here for all locations
        //Depends on if shouldTimePass is true for updateCharacters and the temp sprite nonsense
        //definitely needed for Town fishing due to clint machine mostly
        location.updateEvenIfFarmerIsntHere(this);
    }
    public void UpdateOther()
    {
        loc.checkForMusic(this);
    }
    public void updateActiveMenu()
    {
        bobberBar.update(this);
    }

    public void Draw() { _draw(); }
    public void _draw()
    {
        DrawWorld();
        drawHud();

        currentGameTime.TotalGameTime += TASDateTime.FrameTimeSpan;
    }
    public void DrawWorld()
    {
        loc.draw(this);
    }
    public void drawTool(SFishingRod fishingRod)
    {
        fishingRod.draw(this);
    }
    public void drawHud()
    {
        dayTimeMoneyBox.draw(this);
    }
}