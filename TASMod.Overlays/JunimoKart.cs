using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Minecarts;
using StardewValley.Menus;
using StardewValley.Minigames;
using TASMod.Extensions;
using TASMod.Minigames;
using TASMod.Monogame.Framework;
using TASMod.Patches;
using TASMod.Recording;
using TASMod.System;
using static TASMod.Minigames.SMineCart;

namespace TASMod.Overlays
{
    public enum KartState
    {
        Grounded,
        Coyote,
        Jumping,
        Falling,
        Dead,
        Finished,
    }

    public struct KartPath
    {
        public Vector2 Start;
        public Vector2 End;
        public KartState State;

        public KartPath(Vector2 start, Vector2 end, KartState state)
        {
            Start = start;
            End = end;
            State = state;
        }
    }

    public class JunimoKartState
    {
        public List<KartPath> Paths { get; set; }
        public int PathLength => Paths != null ? Paths.Count : 0;
        public float ScreenLeftBound { get; set; }
        public int TileSize { get; set; }
        public Vector2 ShakeOffset { get; set; }
        public float PixelScale { get; set; }
        public Vector2 UpperLeft { get; set; }
        public float LastGroundedX { get; set; }

        public int Score
        {
            get { return Game.coinCount * 30 + Game._collectedFruit.Count * 1000; }
        }

        // public List<bool> CanJump { get; set; }
        public SMineCart Game { get; set; }
        public const float delta_time = 0.01666667f;

        public JunimoKartState(SMineCart game)
        {
            Paths = new List<KartPath>();
            Game = game.Clone();
            Game.shouldDraw = false;
            Game.shouldPlaySound = false;

            ScreenLeftBound = Game.screenLeftBound;
            TileSize = Game.tileSize;
            ShakeOffset = Game._shakeOffset;
            PixelScale = Game.pixelScale;
            UpperLeft = Game.upperLeft;
        }

        public JunimoKartState(JunimoKartState other)
            : this(other.Game)
        {
            Paths = new List<KartPath>(other.Paths);
        }

        public void AddPath(Vector2 start, Vector2 end, KartState state)
        {
            Paths.Add(new KartPath(start, end, state));
        }

        public void Release()
        {
            Vector2 current = Game.player.position;
            Game.Simulate(false);

            if (Game.gameOver)
            {
                Game.player.position = current;
            }
            else if (Game.reachedFinish)
            {
                AddPath(current, Game.player.position, KartState.Finished);
            }
            else if (Game.player.IsGrounded())
            {
                AddPath(current, Game.player.position, KartState.Grounded);
            }
            else if (Game.player.IsJumping())
            {
                AddPath(current, Game.player.position, KartState.Jumping);
            }
            else
            {
                AddPath(current, Game.player.position, KartState.Falling);
            }
        }

        public void Click()
        {
            Vector2 current = Game.player.position;
            Game.Simulate(true);

            if (Game.gameOver)
            {
                Game.player.position = current;
            }
            else if (Game.reachedFinish)
            {
                AddPath(current, Game.player.position, KartState.Finished);
            }
            else if (Game.player.IsGrounded())
            {
                AddPath(current, Game.player.position, KartState.Grounded);
            }
            else if (Game.player.IsJumping())
            {
                AddPath(current, Game.player.position, KartState.Jumping);
            }
            else
            {
                AddPath(current, Game.player.position, KartState.Falling);
            }
        }

        public JunimoKartState Rollout()
        {
            JunimoKartState cloneState = new JunimoKartState(this);
            SMineCart clone = cloneState.Game;
            clone.shouldDraw = false;
            clone.shouldPlaySound = false;
            Vector2 current = clone.player.position;
            while (!clone.gameOver && !clone.reachedFinish)
            {
                while (clone.player.IsGrounded() && !clone.gameOver && !clone.reachedFinish)
                {
                    clone.Simulate(false);
                    Track track = clone.player.GetTrack();
                    if (track == null)
                    {
                        track = clone.player.GetTrack(new Vector2(0f, 2f));
                    }
                    if (track == null)
                    {
                        continue;
                    }
                    cloneState.AddPath(current, clone.player.position, KartState.Grounded);
                    current = clone.player.position;
                }
                if (clone.gameOver)
                {
                    // cloneState.AddPath(current, clone.player.position, KartState.Dead);
                    clone.player.position = current;
                    break;
                }
                while (
                    !clone.player.IsGrounded()
                    && clone.player.jumpGracePeriod > delta_time
                    && !clone.gameOver
                )
                {
                    cloneState.AddPath(current, clone.player.position, KartState.Coyote);
                    current = clone.player.position;

                    clone.Simulate(false);
                }
                while (!clone.player.IsGrounded() && !clone.gameOver && !clone.reachedFinish)
                {
                    cloneState.AddPath(current, clone.player.position, KartState.Falling);
                    current = clone.player.position;
                    clone.Simulate(false);
                }

                if (clone.gameOver)
                {
                    // AddPath(current, clone.player.position, KartState.Dead);
                    clone.player.position = current;
                    break;
                }
                if (clone.reachedFinish)
                {
                    cloneState.AddPath(current, clone.player.position, KartState.Finished);
                    break;
                }
            }

            return cloneState;
        }

        public JunimoKartState RolloutWhileGrounded(int steps)
        {
            JunimoKartState cloneState = new JunimoKartState(this);
            SMineCart clone = cloneState.Game;
            clone.shouldDraw = false;
            clone.shouldPlaySound = false;
            Vector2 current = clone.player.position;

            // simulate until grounded/dead/finished
            while (clone.player.IsGrounded() && !clone.gameOver && !clone.reachedFinish)
            {
                cloneState.AddPath(current, clone.player.position, KartState.Grounded);
                current = clone.player.position;
                for (int i = 0; i < steps; i++)
                {
                    clone.Simulate(false);
                    if (!clone.player.IsGrounded() || clone.gameOver || clone.reachedFinish)
                    {
                        break;
                    }
                }
            }
            if (clone.gameOver)
            {
                clone.player.position = current;
            }
            else if (clone.reachedFinish)
            {
                cloneState.AddPath(current, clone.player.position, KartState.Finished);
            }
            else if (!clone.player.IsGrounded())
            {
                cloneState.AddPath(current, clone.player.position, KartState.Falling);
            }

            return cloneState;
        }

        public JunimoKartState RolloutUntilGrounded(int steps)
        {
            JunimoKartState cloneState = new JunimoKartState(this);
            SMineCart clone = cloneState.Game;
            clone.shouldDraw = false;
            clone.shouldPlaySound = false;
            Vector2 current = clone.player.position;

            // simulate until grounded/dead/finished
            while (!clone.player.IsGrounded() && !clone.gameOver && !clone.reachedFinish)
            {
                cloneState.AddPath(current, clone.player.position, KartState.Falling);
                current = clone.player.position;
                for (int i = 0; i < steps; i++)
                {
                    clone.Simulate(false);
                    if (clone.player.IsGrounded() || clone.gameOver || clone.reachedFinish)
                    {
                        break;
                    }
                }
            }
            if (clone.gameOver)
            {
                clone.player.position = current;
            }
            else if (clone.reachedFinish)
            {
                cloneState.AddPath(current, clone.player.position, KartState.Finished);
            }
            else if (clone.player.IsGrounded())
            {
                cloneState.AddPath(current, clone.player.position, KartState.Grounded);
            }

            return cloneState;
        }

        public Rectangle TransformDraw(Rectangle dest)
        {
            dest.X =
                (int)Math.Round((dest.X + ShakeOffset.X - ScreenLeftBound) * PixelScale)
                + (int)UpperLeft.X;
            dest.Y = (int)Math.Round((dest.Y + ShakeOffset.Y) * PixelScale) + (int)UpperLeft.Y;
            dest.Width = (int)(dest.Width * PixelScale);
            dest.Height = (int)(dest.Height * PixelScale);
            return dest;
        }

        public Vector2 TransformDraw(Vector2 dest)
        {
            dest.X =
                (int)Math.Round((dest.X + ShakeOffset.X - ScreenLeftBound) * PixelScale)
                + (int)UpperLeft.X;
            dest.Y = (int)Math.Round((dest.Y + ShakeOffset.Y) * PixelScale) + (int)UpperLeft.Y;
            return dest;
        }

        public static JunimoKartState GetBestState(SMineCart mineCart, int Steps = 1)
        {
            JunimoKartState state = new JunimoKartState(mineCart);
            List<JunimoKartState> states = new List<JunimoKartState>();

            while (state.Game.player.IsGrounded() && !state.Game.gameOver && !state.Game.reachedFinish)
            {
                state.Click
            }
            // basic sim to state change
            if (state.Game.player.IsGrounded())
            {
                JunimoKartState rollout = state.RolloutWhileGrounded(Steps);
                if (!rollout.Game.gameOver)
                {
                    rollout.ScreenLeftBound = mineCart.screenLeftBound;
                    states.Add(rollout);
                }
            }
            else
            {
                JunimoKartState rollout = state.RolloutUntilGrounded(Steps);
                if (!rollout.Game.gameOver)
                {
                    rollout.ScreenLeftBound = mineCart.screenLeftBound;
                    states.Add(rollout);
                }
            }

            // sim a jump
            do
            {
                state.Click();
                JunimoKartState rollout = state.RolloutUntilGrounded(Steps);
                rollout.ScreenLeftBound = mineCart.screenLeftBound;
                if (rollout.Game.gameOver)
                {
                    continue;
                }
                states.Add(rollout);
            } while (
                state.Game.player.IsJumping() && !state.Game.gameOver && !state.Game.reachedFinish
            );

            if (states.Count == 0)
            {
                return null;
            }

            int index = 0;
            float maxScore = -1;
            for (int i = 0; i < states.Count; i++)
            {
                float score = states[i].Score;
                if (score > maxScore)
                {
                    maxScore = score;
                    index = i;
                }
                else if (score == maxScore)
                {
                    if (states[i].Game.player.position.X > states[index].Game.player.position.X)
                    {
                        index = i;
                    }
                    else if (
                        states[i].Game.player.position.X == states[index].Game.player.position.X
                    )
                    {
                        if (states[i].Game.player.position.Y < states[index].Game.player.position.Y)
                        {
                            index = i;
                        }
                    }
                }
            }
            JunimoKartState bestState = states[index];
            return bestState;
        }
    }

    public class JunimoKart : IOverlay
    {
        public int NumberOfPaths;
        public int StepsToSimulate = 1;
        public bool DoDraw = true;
        public override string Name => "JunimoKart";

        public override string Description => "shows the fall path of the player";

        public override string[] HelpText()
        {
            return new[] { $"{Name}: shows the fall path of the player " };
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            NumberOfPaths = 0;
            if (Game1.currentMinigame != null && Game1.currentMinigame is SMineCart mineCart)
            {
                if (mineCart.gameState != GameStates.Ingame)
                {
                    return;
                }

                // current frame state
                JunimoKartState bestState = JunimoKartState.GetBestState(mineCart);
                if (bestState != null)
                {
                    DrawState(spriteBatch, bestState, Color.SeaGreen);
                }
            }
        }

        public void DrawState(SpriteBatch spriteBatch, JunimoKartState state, Color color)
        {
            if (!DoDraw)
            {
                return;
            }
            foreach (KartPath path in state.Paths)
            {
                DrawLineLocal(
                    spriteBatch,
                    state.TransformDraw(path.Start),
                    state.TransformDraw(path.End),
                    color,
                    4
                );
                NumberOfPaths++;
            }
            if (state.Game.player is PlayerMineCartCharacter player)
            {
                Rectangle rect = player.GetBounds();
                DrawRectLocal(spriteBatch, state.TransformDraw(rect), color, 4);
            }
        }

        public void DrawState(SpriteBatch spriteBatch, JunimoKartState state)
        {
            if (!DoDraw)
            {
                return;
            }
            foreach (KartPath path in state.Paths)
            {
                Color color = Color.White;
                switch (path.State)
                {
                    case KartState.Dead:
                        color = Color.Gray;
                        break;
                    case KartState.Finished:
                        color = Color.Blue;
                        break;
                    case KartState.Grounded:
                        color = Color.Green;
                        break;
                    case KartState.Coyote:
                        color = Color.Yellow;
                        break;
                    case KartState.Jumping:
                        color = Color.Purple;
                        break;
                    case KartState.Falling:
                        color = Color.Red;
                        break;
                }
                DrawLineLocal(
                    spriteBatch,
                    state.TransformDraw(path.Start),
                    state.TransformDraw(path.End),
                    color,
                    4
                );
                NumberOfPaths++;
            }

            if (state.Game.player is PlayerMineCartCharacter player)
            {
                Rectangle rect = player.GetBounds();
                DrawRectLocal(spriteBatch, state.TransformDraw(rect), Color.Blue, 4);
            }
        }
    }
}
