using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TASMod.Inputs;

namespace TASMod.Simulators
{
    public class SFarmerTeam
    {
        public void Update() { }
    }

    public class SFarmer
    {
        public ulong millisecondsPlayed;
        public SFarmerTeam team;
    }

    public class SMultiplayer
    {
        public void UpdateEarly() { }

        public void UpdateLate() { }
    }

    public class SScreenFade
    {
        public void UpdateGlobalFade() { }
    }

    public class SGameLocation
    {
        public void Update() { }
    }

    public class SClickableMenu
    {
        public void Update(GameTime gameTime) { }
    }

    public class SAudioEngine
    {
        public void Update() { }
    }

    public class SGame
    {
        public int gameModeTicks;
        public GameTime gameTime;
        public GameTime currentGameTime;
        public float _cursorUpdateElapsedSec;
        public bool _cursorSpeedDirty;
        public int FarmAnimal_NumPathfindingThisTick;
        public int ticks;
        public float pauseTime;
        public bool freezeControls;

        public SFarmer player;
        public SMultiplayer multiplayer;
        public SGameLocation currentLocation;
        public SClickableMenu activeClickableMenu;
        public bool globalFade;
        public SScreenFade screenFade;
        public SAudioEngine audioEngine;

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

        public void updateRaindropPosition() { }

        public void updateActiveMenu(GameTime gameTime) { }

        public void updatePause(GameTime gameTime) { }

        public void UpdateControlInput(GameTime gameTime) { }

        public void UpdateGameClock(GameTime gameTime) { }

        public void UpdateCharacters(GameTime gameTime) { }

        public void UpdateLocations(GameTime gameTime) { }

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
            if (gameMode == 3)
            {
                multiplayer.UpdateEarly();
                if (player?.team != null)
                {
                    player.team.Update();
                }
            }

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
