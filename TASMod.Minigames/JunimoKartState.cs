using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using static TASMod.Minigames.SMineCart;

namespace TASMod.Minigames
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
        public static int Clones = 0;
        public static int Simulates = 0;

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
            Clones++;
        }

        public JunimoKartState(JunimoKartState other)
            : this(other.Game)
        {
            Paths = new List<KartPath>(other.Paths);
        }

        public bool PastFruit()
        {
            foreach (Fruit fruit in GetFruits())
            {
                if (fruit.GetBounds().Right < Game.player.position.X && fruit.IsActive())
                {
                    return true;
                }
            }
            return false;
        }

        public void AddPath(Vector2 start, Vector2 end, KartState state)
        {
            Paths.Add(new KartPath(start, end, state));
        }

        public JunimoKartState JumpClone(int nclicks)
        {
            JunimoKartState clone = new JunimoKartState(this);
            for (int i = 0; i < nclicks; i++)
            {
                clone.Click();
            }
            return clone;
        }

        public JunimoKartState ReleaseClone()
        {
            JunimoKartState clone = new JunimoKartState(this);
            clone.Game.Simulate(false);
            return clone;
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

        public JunimoKartState ClickClone()
        {
            JunimoKartState clone = new JunimoKartState(this);
            clone.Game.Simulate(true);
            return clone;
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

        public List<Fruit> GetFruits()
        {
            List<Fruit> fruits = new List<Fruit>();
            foreach (Entity entity in Game._entities)
            {
                if (entity is Fruit fruit && fruit.IsActive())
                {
                    fruits.Add(fruit);
                }
            }
            return fruits;
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
            // ModEntry.Console.Log("RolloutUntilGrounded", LogLevel.Trace);
            JunimoKartState cloneState = new JunimoKartState(this);
            SMineCart clone = cloneState.Game;
            clone.shouldDraw = false;
            clone.shouldPlaySound = false;
            Vector2 current = clone.player.position;

            // simulate until grounded/dead/finished
            // ModEntry.Console.Log("\tStarting up loop", LogLevel.Trace);
            while (!clone.player.IsGrounded() && !clone.gameOver && !clone.reachedFinish)
            {
                cloneState.AddPath(current, clone.player.position, KartState.Falling);
                current = clone.player.position;

                clone.Simulate(false);
            }
            // ModEntry.Console.Log("\tFinished loop", LogLevel.Trace);
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

        public List<JunimoKartState> ValidJumps(JunimoKartState state, int Steps = 1)
        {
            List<JunimoKartState> states = new List<JunimoKartState>();
            JunimoKartState clone = new JunimoKartState(state);
            do
            {
                clone.Click();
                JunimoKartState rollout = clone.RolloutUntilGrounded(Steps);
                if (rollout.Game.gameOver)
                {
                    continue;
                }
                rollout.Release();
                rollout.Release();
                rollout.Release();
                rollout.Release();
                rollout.Release();
                if (rollout.Game.gameOver)
                {
                    continue;
                }
                states.Add(rollout);
            } while (
                clone.Game.player.IsJumping() && !clone.Game.gameOver && !clone.Game.reachedFinish
            );
            return states;
        }

        public static JunimoKartState GetBestState(
            SMineCart mineCart,
            int Steps,
            out int NumberOfPaths
        )
        {
            List<JunimoKartState> states = new List<JunimoKartState>();

            // basic sim to state change
            if (mineCart.player.IsGrounded())
            {
                int steps = 0;
                JunimoKartState grounded = new JunimoKartState(mineCart);
                while (
                    grounded.Game.player.IsGrounded()
                    && !grounded.Game.gameOver
                    && !grounded.Game.reachedFinish
                )
                {
                    List<JunimoKartState> jumps = grounded.ValidJumps(grounded, Steps);
                    states.AddRange(jumps);
                    grounded.Release();
                    if (steps++ > 30)
                    {
                        break;
                    }
                }
                if (!grounded.Game.gameOver)
                {
                    states.Add(grounded);
                }
            }
            else
            {
                JunimoKartState ungrounded = new JunimoKartState(mineCart);
                while (
                    ungrounded.Game.player.IsJumping()
                    && !ungrounded.Game.gameOver
                    && !ungrounded.Game.reachedFinish
                )
                {
                    JunimoKartState release = ungrounded.RolloutUntilGrounded(Steps);
                    if (!release.Game.gameOver)
                    {
                        states.Add(release);
                    }
                    ungrounded.Click();
                }
                while (
                    !ungrounded.Game.player.IsGrounded()
                    && !ungrounded.Game.gameOver
                    && !ungrounded.Game.reachedFinish
                )
                {
                    ungrounded.Release();
                }
                if (!ungrounded.Game.gameOver)
                {
                    states.Add(ungrounded);
                }
            }

            // ensure a valid state exists
            if (states.Count == 0)
            {
                NumberOfPaths = 0;
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
            bestState.ScreenLeftBound = mineCart.screenLeftBound;
            NumberOfPaths = states.Count;
            return bestState;
        }
    }
}
