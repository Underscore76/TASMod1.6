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
    public class JunimoKart : IOverlay
    {
        public ulong LastComputedFrame;
        public List<JunimoKartState> States = new List<JunimoKartState>();
        public JunimoKartState BestState = null;
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
                    BestState = null;
                    States.Clear();
                    return;
                }

                // current frame state
                if (LastComputedFrame != TASDateTime.CurrentFrame)
                {
                    LastComputedFrame = TASDateTime.CurrentFrame;
                    // States = BestFirstSearch.GetNeighbors(new JunimoKartState(mineCart));
                    // BestState = (new JunimoKartState(mineCart)).Rollout();
                    // BestState = JunimoKartState.GetBestState(
                    //     mineCart,
                    //     StepsToSimulate,
                    //     out NumberOfPaths
                    // );
                }
                // for (int i = 0; i < States.Count; i++)
                // {
                //     States[i].ScreenLeftBound = mineCart.screenLeftBound;
                //     DrawState(spriteBatch, States[i]);
                // }
                if (BestState != null)
                {
                    // DrawState(spriteBatch, BestState);
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
