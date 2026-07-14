using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class Hitbox : IOverlay
    {
        public override string Name => "Hitbox";
        public override string Description => "displays hitboxes on all characters";

        public static Color PlayerColor = Color.Blue;
        public static Color PlayerInvincibleColor = Color.Aqua;
        public static Color NPCColor = Color.Green;
        public static Color MonsterColor = Color.Red;
        public static Color MonsterInvincibleColor = Color.Purple;
        public static Color LineColor = Color.White;

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawHitboxesForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log($"Hitbox2 ActiveDraw Exception: {e}", StardewModdingAPI.LogLevel.Error);
                }
            }
        }
        public void DrawHitboxesForInstance(int i, SpriteBatch spriteBatch)
        {
            var currentLocation = InstanceCurrentLocation.Get(i);
            var playerInfo = InstanceCurrentPlayer.Get(i);
            if (currentLocation.Active)
            {
                if (currentLocation.Location is Farm farm)
                {
                    foreach (var animal in farm.animals.Values)
                    {
                        DrawRectGlobal(i, spriteBatch, animal.GetBoundingBox(), NPCColor, LineColor);
                    }
                }
                foreach (NPC current in currentLocation.Characters)
                {
                    DrawRectGlobal(i, spriteBatch, current.GetBoundingBox(), NPCColor, LineColor);
                }
                foreach (NPC current in currentLocation.Monsters)
                {
                    if ((current as Monster).isInvincible())
                        DrawRectGlobal(i, spriteBatch, current.GetBoundingBox(), MonsterInvincibleColor, LineColor);
                    else
                        DrawRectGlobal(i, spriteBatch, current.GetBoundingBox(), MonsterColor, LineColor);
                }
                if (!playerInfo.Player.temporarilyInvincible && playerInfo.CanMove)
                    DrawRectGlobal(i, spriteBatch, playerInfo.BoundingBox, PlayerColor, LineColor);
                else
                    DrawRectGlobal(i, spriteBatch, playerInfo.BoundingBox, PlayerInvincibleColor, LineColor);
            }
        }
    }

    public class WeaponGuide : IOverlay
    {
        public override string Name => "WeaponGuide";

        public override string Description => "displays weapon swing position";

        public override string[] HelpText()
        {
            return new string[] { string.Format("{0}: display the weapon swing position", Name) };
        }
        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log($"Hitbox2 ActiveDraw Exception: {e}", StardewModdingAPI.LogLevel.Error);
                }
            }
        }

        public void DrawForInstance(int index, SpriteBatch spriteBatch)
        {
            var playerInfo = InstanceCurrentPlayer.Get(index);
            if (playerInfo.CurrentTool is MeleeWeapon weapon && weapon.type.Value != MeleeWeapon.dagger)
            {
                // draw the current arc
                if (playerInfo.IsSwingingSword)
                {
                    Color col = Color.Gray;
                    for (int animIndex = playerInfo.FarmerSprite.currentAnimationIndex;
                        animIndex < playerInfo.FarmerSprite.CurrentAnimation.Count; animIndex++)
                    {

                        if (playerInfo.FarmerSprite.CurrentAnimation[animIndex].frameStartBehavior != null &&
                            playerInfo.FarmerSprite.CurrentAnimation[animIndex].frameStartBehavior.Method.Name == "showSwordSwipe")
                        {
                            DrawAnimation(playerInfo, spriteBatch, weapon, playerInfo.FacingDirection, animIndex, col, Color.Black);
                            col.A = (byte)(col.A * 0.8);
                        }
                    }
                }
                else
                {
                    int currDir = playerInfo.GetLastMouseFacingDirection();
                    Color currCol = new Color(64, 220, 64, 128);
                    DrawAnimation(playerInfo, spriteBatch, weapon, currDir, 0, currCol);

                    int newDir = playerInfo.GetMouseFacingDirection();
                    Color newCol = new Color(220, 64, 64, 128);
                    DrawAnimation(playerInfo, spriteBatch, weapon, newDir, 0, newCol);

                }
            }
        }
        public void DrawAnimation(PlayerInfo info, SpriteBatch spriteBatch, MeleeWeapon weapon, int facingDirection, int index, Color rectColor)
        {
            Vector2 toolLoc = info.GetToolLocation(facingDirection);
            Vector2 tileLoc = Vector2.Zero;
            Vector2 tileLoc2 = Vector2.Zero;
            Rectangle areaOfEffect = WeaponInfo.GetAreaOfEffect(weapon, (int)toolLoc.X, (int)toolLoc.Y, facingDirection, ref tileLoc, ref tileLoc2, info.BoundingBox, index);

            DrawRectGlobal(info.index, spriteBatch, areaOfEffect, rectColor);
        }
        public void DrawAnimation(PlayerInfo info, SpriteBatch spriteBatch, MeleeWeapon weapon, int facingDirection, int index, Color rectColor, Color textColor)
        {
            Vector2 toolLoc = info.GetToolLocation();
            Vector2 tileLoc = Vector2.Zero;
            Vector2 tileLoc2 = Vector2.Zero;
            Rectangle areaOfEffect = WeaponInfo.GetAreaOfEffect(weapon, (int)toolLoc.X, (int)toolLoc.Y, facingDirection, ref tileLoc, ref tileLoc2, info.BoundingBox, index);

            int numFrames = WeaponInfo.GetNumberOfSwingFrames(info, weapon, index);
            DrawRectGlobal(info.index, spriteBatch, areaOfEffect, rectColor);
            DrawCenteredTextInRectGlobal(info.index, spriteBatch, areaOfEffect, numFrames.ToString(), textColor, 1.5f, 1);
        }
    }
}

