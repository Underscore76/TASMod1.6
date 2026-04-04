using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace TASMod.Simulators.FishingMinigame;

public class SCritter
{
    public Vector2 position;
    public Vector2 startingPosition;
    public SAnimatedSprite sprite;
    public int baseFrame;
    public float gravityAffectedDY;
    public float yOffset;
    public float yJumpOffset;
    public bool flip;

    public SCritter(Critter critter)
    {
        position = critter.position;
        startingPosition = critter.startingPosition;
        sprite = new SAnimatedSprite(critter.sprite);
        baseFrame = critter.baseFrame;
        gravityAffectedDY = critter.gravityAffectedDY;
        yOffset = critter.yOffset;
        yJumpOffset = critter.yJumpOffset;
        flip = critter.flip;
    }

    public SCritter()
    {
    }

    protected SCritter(SCritter critter)
    {
        position = critter.position;
        startingPosition = critter.startingPosition;
        sprite = critter.sprite?.Clone();
        baseFrame = critter.baseFrame;
        gravityAffectedDY = critter.gravityAffectedDY;
        yOffset = critter.yOffset;
        yJumpOffset = critter.yJumpOffset;
        flip = critter.flip;
    }

    public virtual SCritter Clone()
    {
        return new SCritter(this);
    }

    public virtual bool update(SGame1 game, SGameLocation environment)
    {
        sprite.animateOnce(game, game.currentGameTime);
        if (gravityAffectedDY < 0f || yJumpOffset < 0f)
        {
            yJumpOffset += gravityAffectedDY;
            gravityAffectedDY += 0.25f;
        }
        if (position.X < -128f || position.Y < -128f || position.X > (float)environment.DisplayWidth || position.Y > (float)environment.DisplayHeight)
        {
            return true;
        }
        return false;
    }
    public virtual Rectangle getBoundingBox(int xOffset, int yOffset)
    {
        return new Rectangle((int)position.X - 32 + xOffset, (int)position.Y - 16 + yOffset, 64, 32);
    }
}