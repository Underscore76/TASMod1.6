using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Network;
using TASMod.Inputs;

namespace TASMod.Simulators.Fishing
{
    public class SGame
    {
        public int gameModeTicks;
        public GameTime gameTime;
        public GameTime currentGameTime;
        public int gameTimeInterval;
        public float _cursorUpdateElapsedSec;
        public bool _cursorSpeedDirty;
        public int FarmAnimal_NumPathfindingThisTick;
        public int ticks;
        public float pauseTime;
        public bool freezeControls;

        public NetRoot<NetWorldState> netWorldState;
        public Random random;
        public SFarmer player;
        public SMultiplayer multiplayer;
        public SGameLocation currentLocation;
        public List<SGameLocation> locations;
        public SClickableMenu activeClickableMenu;
        public bool globalFade;
        public SScreenFade screenFade;
        public SAudioEngine audioEngine;
        public xTile.Dimensions.Rectangle viewport;
        public Vector2 previousViewportPosition;
        public RainDrop[] rainDrops;
        public List<SWeatherDebris> debrisWeather;

        public byte _gameMode;
        public byte gameMode
        {
            get { return _gameMode; }
            set
            {
                if (_gameMode != value)
                {
                    _gameMode = value;
                    gameModeTicks = 0;
                }
            }
        }

        public SGame() { }

        private void SetFreeCursorElapsed(float elapsedSec)
        {
            if (elapsedSec != _cursorUpdateElapsedSec)
            {
                _cursorUpdateElapsedSec = elapsedSec;
                _cursorSpeedDirty = true;
            }
        }

        public void checkForEscapeKeys() { }

        public void updateMusic() { }

        public bool IsRainingHere(SGameLocation location = null)
        {
            if (location == null)
            {
                location = currentLocation;
            }
            if (location != null && netWorldState.Value != null)
            {
                return location.IsRainingHere();
            }
            return false;
        }
        public void updateDebrisWeatherForMovement(List<SWeatherDebris> debrisWeather) { }
        public void updateRaindropPosition()
        {
            if (IsRainingHere())
            {
                int xOffset = viewport.X - (int)previousViewportPosition.X;
                int yOffset = viewport.Y - (int)previousViewportPosition.Y;
                for (int i = 0; i < rainDrops.Length; i++)
                {
                    rainDrops[i].position.X -= (float)xOffset * 1f;
                    rainDrops[i].position.Y -= (float)yOffset * 1f;
                    if (rainDrops[i].position.Y > (float)(viewport.Height + 64))
                    {
                        rainDrops[i].position.Y = -64f;
                    }
                    else if (rainDrops[i].position.X < -64f)
                    {
                        rainDrops[i].position.X = viewport.Width;
                    }
                    else if (rainDrops[i].position.Y < -64f)
                    {
                        rainDrops[i].position.Y = viewport.Height;
                    }
                    else if (rainDrops[i].position.X > (float)(viewport.Width + 64))
                    {
                        rainDrops[i].position.X = -64f;
                    }
                }
            }
            else
            {
                updateDebrisWeatherForMovement(debrisWeather);
            }
        }

        public void updateActiveMenu(GameTime gameTime) { }

        public void updatePause(GameTime gameTime)
        {
            pauseTime -= gameTime.ElapsedGameTime.Milliseconds;
            if (player.isCrafting && random.NextDouble() < 0.007)
            {
                // player sound crafting;
            }
            if (!(pauseTime <= 0f))
            {
                return;
            }
        }

        public void UpdateControlInput(GameTime gameTime) { }

        public void UpdateGameClock(GameTime gameTime) { }

        public void UpdateCharacters(GameTime gameTime)
        {
            player.Update(gameTime, currentLocation, random);
        }

        public void UpdateLocations(GameTime gameTime)
        {
            foreach (var location in locations)
            {
                _UpdateLocation(location, gameTime);
            }
        }
        public void _UpdateLocation(SGameLocation location, GameTime gameTime)
        {
            if (player.currentLocation == location)
            {
                location.UpdateWhenCurrentLocation(gameTime, random);
            }
            location.updateEvenIfFarmerIsntHere(gameTime, random);
        }
        public SGameLocation getLocationFromName(string name)
        {
            foreach (var location in locations)
            {
                if (location.Name.EqualsIgnoreCase(name))
                {
                    return location;
                }
            }
            return null;
        }


        public void UpdateViewPort(bool overrideFreeze, Vector2 center) { }

        public Vector2 getViewportCenter()
        {
            return Vector2.Zero;
        }

        public void UpdateOther(GameTime gameTime) { }

        public void Update(TASKeyboardState kstate, TASMouseState mstate)
        {
            gameModeTicks++;
            FarmAnimal_NumPathfindingThisTick = 0;

            // input.Update();

            // newdaytask skipped
            // skipping save
            // skipping exit
            SetFreeCursorElapsed((float)gameTime.ElapsedGameTime.TotalSeconds);
            // Program.sdk.Update();
            // skipping keyboard focus
            // skipping display setting save
            // skipping keyboard dispatch
            // skipping loading mode
            // if (gameMode == 3)
            // {
            //     multiplayer.UpdateEarly();
            //     if (player?.team != null)
            //     {
            //         player.team.Update();
            //     }
            // }

            currentGameTime = gameTime;
            if (gameMode != 11)
            {
                ticks++;
                checkForEscapeKeys();
                updateMusic();
                updateRaindropPosition();
                if (globalFade)
                {
                    screenFade.UpdateGlobalFade();
                }

                if (gameMode == 3 || gameMode == 2)
                {
                    player.millisecondsPlayed += (uint)gameTime.ElapsedGameTime.Milliseconds;
                    bool doMainGameUpdates = true;

                    if (doMainGameUpdates)
                    {
                        if (currentLocation != null)
                        {
                            if (activeClickableMenu != null)
                            {
                                updateActiveMenu(gameTime);
                            }
                            else
                            {
                                if (pauseTime > 0f)
                                {
                                    updatePause(gameTime);
                                }
                                if (!globalFade && !freezeControls && activeClickableMenu == null)
                                {
                                    UpdateControlInput(gameTime);
                                }
                            }
                        }

                        if (currentLocation != null)
                        {
                            if (activeClickableMenu == null)
                            {
                                UpdateGameClock(gameTime);
                            }
                            UpdateCharacters(gameTime);
                            UpdateLocations(gameTime);
                            UpdateViewPort(overrideFreeze: false, getViewportCenter());
                            UpdateOther(gameTime);
                        }
                    }
                }

                audioEngine?.Update();
                if (gameMode != 6)
                {
                    multiplayer.UpdateLate();
                }
            }
            // skipping on day started
        }

        public void DrawWorld(GameTime gameTime) { }

        public void drawHUD() { }

        public void DrawGlobalFade(GameTime gameTime) { }

        public void DrawScreenOverlaySprites(GameTime gameTime) { }

        public void DrawMenu(GameTime gameTime) { }

        public void DrawOverlays(GameTime gameTime) { }

        public void Draw()
        {
            DrawWorld(gameTime);
            drawHUD();
            DrawGlobalFade(gameTime);
            DrawScreenOverlaySprites(gameTime);
            DrawMenu(gameTime);
            DrawOverlays(gameTime);
        }

        public SGame Copy()
        {
            return null;
        }

        public static SGame CloneGame()
        {
            return null;
        }
    }
}
