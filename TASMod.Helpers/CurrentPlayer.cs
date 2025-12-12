using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Network;
using StardewValley.Tools;
using TASMod.Inputs;

namespace TASMod.Helpers
{
    public class PlayerInfo
    {
        public int index;
        public Farmer Player;
        public FarmerSprite FarmerSprite => Player?.FarmerSprite;
        public ViewportInfo Viewport => InstanceViewport.Get(index);
        public Options Options
        {
            get
            {
                if (index >= GameRunner.instance.gameInstances.Count)
                    return null;
                return GameRunner.instance.gameInstances[index]?.instanceOptions;
            }
        }
        public Rectangle Window
        {
            get
            {
                if (index >= GameRunner.instance.gameInstances.Count)
                    return Rectangle.Empty;
                return GameRunner.instance.gameInstances[index].localMultiplayerWindow;
            }
        }
        public bool Active => Player != null;
        public bool CanMove => Player.CanMove;
        public bool FreezePause => Player.freezePause > 0;
        public bool IsEmoting => Player.isEmoting;
        public bool UsingTool => Player.UsingTool;
        public Tool CurrentTool => Player?.CurrentTool;
        public Vector2 CurrentTile => Player.Tile;
        public Rectangle BoundingBox => Player.GetBoundingBox();
        public NetStringDictionary<Friendship, NetRef<Friendship>> Friendships => Player?.friendshipData;

        public string CurrentAnimationStartBehavior
        {
            get
            {
                string behavior = null;
                if (Player != null && Player.FarmerSprite != null)
                {
                    int animIndex = Player.FarmerSprite.currentAnimationIndex;
                    if (animIndex < Player.FarmerSprite.CurrentAnimation.Count)
                    {
                        if (
                            Player.FarmerSprite.CurrentAnimation[animIndex].frameStartBehavior
                            != null
                        )
                        {
                            behavior = Player
                                .FarmerSprite
                                .CurrentAnimation[animIndex]
                                .frameStartBehavior
                                .Method
                                .Name;
                        }
                    }
                }
                return behavior;
            }
        }
        public string LastAnimationEndBehavior
        {
            get
            {
                string behavior = null;
                if (Player != null && Player.FarmerSprite != null)
                {
                    int animIndex = Player.FarmerSprite.currentAnimationIndex;
                    if (
                        animIndex < Player.FarmerSprite.CurrentAnimation.Count
                        && animIndex > 0
                    )
                    {
                        if (
                            Player
                                .FarmerSprite
                                .CurrentAnimation[animIndex - 1]
                                .frameEndBehavior != null
                        )
                        {
                            behavior = Player
                                .FarmerSprite
                                .CurrentAnimation[animIndex - 1]
                                .frameEndBehavior
                                .Method
                                .Name;
                        }
                    }
                }
                return behavior;
            }
        }

        public int CurrentAnimationLength
        {
            get
            {
                double interval = Math.Round(
                    Player.FarmerSprite.interval * Player.FarmerSprite.intervalModifier,
                    1
                );
                return (int)Math.Ceiling(interval / 16);
            }
        }
        public int CurrentAnimationElapsed
        {
            get { return (int)(Player.FarmerSprite.timer / 16); }
        }
        public bool IsHarvestingItem
        {
            get
            {
                if (Player != null && Player.FarmerSprite != null)
                {
                    int animationType = Player.FarmerSprite.currentSingleAnimation;
                    switch (animationType)
                    {
                        case FarmerSprite.harvestItemUp:
                        case FarmerSprite.harvestItemDown:
                        case FarmerSprite.harvestItemLeft:
                        case FarmerSprite.harvestItemRight:
                            return true;
                        default:
                            return false;
                    }
                }
                return false;
            }
        }

        public bool IsSwingingSword
        {
            get
            {
                if (Player != null && Player.FarmerSprite != null)
                {
                    int animationType = Player.FarmerSprite.currentSingleAnimation;
                    switch (animationType)
                    {
                        case FarmerSprite.swordswipeDown:
                        case FarmerSprite.swordswipeUp:
                        case FarmerSprite.swordswipeLeft:
                        case FarmerSprite.swordswipeRight:
                            return true;
                        default:
                            return false;
                    }
                }
                return false;
            }
        }

        public Vector2 GetToolLocation()
        {
            return Player.GetToolLocation();
        }

        public Vector2 GetToolLocation(int dir)
        {
            Rectangle boundingBox = BoundingBox;
            if (
                CurrentTool != null
                && CurrentTool.Name.Equals("Fishing Rod")
            )
            {
                switch (dir)
                {
                    case 0:
                        return new Vector2(boundingBox.X - 16, boundingBox.Y - 102);
                    case 1:
                        return new Vector2(boundingBox.X + boundingBox.Width + 64, boundingBox.Y);
                    case 2:
                        return new Vector2(
                            boundingBox.X - 16,
                            boundingBox.Y + boundingBox.Height + 64
                        );
                    case 3:
                        return new Vector2(boundingBox.X - 112, boundingBox.Y);
                }
            }
            else
            {
                switch (dir)
                {
                    case 0:
                        return new Vector2(
                            boundingBox.X + boundingBox.Width / 2,
                            boundingBox.Y - 48
                        );
                    case 1:
                        return new Vector2(
                            boundingBox.X + boundingBox.Width + 48,
                            boundingBox.Y + boundingBox.Height / 2
                        );
                    case 2:
                        return new Vector2(
                            boundingBox.X + boundingBox.Width / 2,
                            boundingBox.Y + boundingBox.Height + 48
                        );
                    case 3:
                        return new Vector2(
                            boundingBox.X - 48,
                            boundingBox.Y + boundingBox.Height / 2
                        );
                }
            }
            return new Vector2(Player.StandingPixel.X, Player.StandingPixel.Y);
        }

        public int FacingDirection
        {
            get { return Player.FacingDirection; }
        }

        public int GetLastMouseFacingDirection()
        {
            Vector2 position = new Vector2(
                TASInputState.mState.MouseX + Viewport.X,
                TASInputState.mState.MouseY + Viewport.Y
            );
            if (
                Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, Player)
                && (
                    Math.Abs(position.X - (float)Player.StandingPixel.X) >= 32f
                    || Math.Abs(position.Y - (float)Player.StandingPixel.Y) >= 32f
                )
            )
            {
                return Player.getGeneralDirectionTowards(position, 0, false);
            }
            return FacingDirection;
        }

        public int GetMouseFacingDirection()
        {
            Vector2 position = new Vector2(
                RealInputState.mouseState.X + Viewport.X,
                RealInputState.mouseState.Y + Viewport.Y
            );
            if (
                Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, Player)
                && (
                    Math.Abs(position.X - (float)Player.StandingPixel.X) >= 32f
                    || Math.Abs(position.Y - (float)Player.StandingPixel.Y) >= 32f
                )
            )
            {
                return Player.getGeneralDirectionTowards(position, 0, false);
            }
            return FacingDirection;
        }

        public int GetProposedFacingDirection(int mouseX, int mouseY)
        {
            Vector2 position = new Vector2(mouseX + Viewport.X, mouseY + Viewport.Y);
            if (
                Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, Player)
                && (
                    Math.Abs(position.X - (float)Player.StandingPixel.X) >= 32f
                    || Math.Abs(position.Y - (float)Player.StandingPixel.Y) >= 32f
                )
            )
            {
                return Player.getGeneralDirectionTowards(position, 0, false);
            }
            return FacingDirection;
        }

        public bool CanShoot
        {
            get
            {
                if (CurrentTool != null && CurrentTool is Slingshot slingshot)
                {
                    return slingshot.GetBackArmDistance(Player) > 4;
                }
                return false;
            }
        }
        public Vector2 PlayerCenter
        {
            get
            {
                if (Player != null)
                {
                    Rectangle bb = BoundingBox;
                    return new Vector2(bb.X + 24, bb.Y + 16);
                }
                return Vector2.Zero;
            }
        }
        public Vector2 PlayerInTile
        {
            get { return new Vector2(PlayerCenter.X % 64f, PlayerCenter.Y % 64f); }
        }
        public int Direction
        {
            get { return Player.FacingDirection; }
        }
        public int CurrentSingleAnimation
        {
            get { return Player.FarmerSprite.CurrentSingleAnimation; }
        }
        public int CurrentAnimationIndex
        {
            get { return Player.FarmerSprite.currentAnimationIndex; }
        }
    }

    public class InstanceCurrentPlayer
    {
        public static PlayerInfo Get(int index)
        {
            // if (GameRunner.instance.gameInstances.Count == 1)
            // {
            //     return new PlayerInfo { index = index, Player = Game1.player };
            // }
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new PlayerInfo { index = index, Player = null };
            var farmer = Reflector.GetStaticVar(index, "Game1__player") as Farmer;
            return new PlayerInfo { index = index, Player = farmer };
        }
    }
}
