using StardewValley;
using StardewValley.Tools;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class AnimationCancel : IAutomatedLogic
    {
        public override string Name => "AnimationCancel";
        public override string Description => "auto advance tool swing to first cancellable frame";
        public override string[] Usage =>
            new string[] { "\tnote: only works with axe, pickaxe, hoe, watering can." };

        public AnimationCancel()
        {
            Active = true;
        }

        public override bool ActiveUpdate(
            int index,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            var playerInfo = InstanceCurrentPlayer.Get(index);
            if (
                playerInfo.UsingTool
                && (
                    playerInfo.CurrentTool is Hoe
                    || playerInfo.CurrentTool is Axe
                    || playerInfo.CurrentTool is Pickaxe
                    || playerInfo.CurrentTool is WateringCan
                )
            )
            {
                kstate = null;
                mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);
                gstate = null;
                if (ShouldCancel(playerInfo))
                {
                    return false;
                }
                return true;
            }
            return base.ActiveUpdate(index, out kstate, out mstate, out gstate);
        }

        public bool ShouldCancel(PlayerInfo playerInfo)
        {
            switch (playerInfo.FarmerSprite.CurrentSingleAnimation)
            {
                case 66: // axe/pickaxe/hoe down
                case 48: // axe/pickaxe/hoe left/right
                case 36: // axe/pickaxe/hoe down
                    return playerInfo.FarmerSprite.currentAnimationIndex >= 2;
                case 54: // watering can down
                case 58: // watering can left/right
                case 62: // watering can up
                    return playerInfo.FarmerSprite.currentAnimationIndex >= 3;
                default:
                    return false;
            }
        }
    }
}
