using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace TASMod.Simulators.FishingMinigame;

public class SButterfly : SCritter
{
    public int flapTimer;
    public int flapSpeed = 50;
    public float motionMultiplier = 1f;
    public Vector2 motion;
    public bool stayInbounds;
    public bool summerButterfly;


    public SButterfly(Butterfly butterfly)
        : base(butterfly)
    {
        flapTimer = (int)Reflector.GetValue(butterfly, "flapTimer");
        flapSpeed = (int)Reflector.GetValue(butterfly, "flapSpeed");
        motionMultiplier = (float)Reflector.GetValue(butterfly, "motionMultiplier");
        motion = (Vector2)Reflector.GetValue(butterfly, "motion");
        stayInbounds = butterfly.stayInbounds;
        summerButterfly = (bool)Reflector.GetValue(butterfly, "summerButterfly");
        for (int i = 0; i < butterfly.sprite.CurrentAnimation?.Count; i++)
        {
            var anim = butterfly.sprite.CurrentAnimation[i];
            SAnimatedSprite.SAnimationFrame frame = new SAnimatedSprite.SAnimationFrame(anim);
            if (anim.frameStartBehavior != null)
            {
                frame.frameStartBehavior = this.doneWithFlap;
            }
            sprite.currentAnimation.Add(frame);
        }

    }

    public SButterfly()
    { }

    public SButterfly(SButterfly butterfly)
        : base(butterfly)
    {
        flapTimer = butterfly.flapTimer;
        flapSpeed = butterfly.flapSpeed;
        motionMultiplier = butterfly.motionMultiplier;
        motion = butterfly.motion;
        stayInbounds = butterfly.stayInbounds;
        summerButterfly = butterfly.summerButterfly;
        for (int i = 0; i < butterfly.sprite.CurrentAnimation?.Count; i++)
        {
            var anim = butterfly.sprite.CurrentAnimation[i];
            SAnimatedSprite.SAnimationFrame frame = new SAnimatedSprite.SAnimationFrame(anim);
            if (anim.frameStartBehavior != null)
            {
                frame.frameStartBehavior = this.doneWithFlap;
            }
            sprite.currentAnimation.Add(frame);
        }
    }

    public void doneWithFlap(SGame1 game, SFarmer who)
    {
        flapTimer = 200 + game.Next(-5, 6);
    }

    public override bool update(SGame1 game, SGameLocation environment)
    {
        flapTimer -= game.currentGameTime.ElapsedGameTime.Milliseconds;
        if (flapTimer <= 0 && sprite.CurrentAnimation == null)
        {
            motionMultiplier = 1f;
            motion.X += (float)game.Next(-80, 81) / 100f;
            motion.Y = (float)(game.NextDouble() + 0.25) * -3f / 2f;
            if (Math.Abs(motion.X) > 1.5f)
            {
                motion.X = 3f * (float)Math.Sign(motion.X) / 2f;
            }

            if (Math.Abs(motion.Y) > 3f)
            {
                motion.Y = 3f * (float)Math.Sign(motion.Y);
            }

            if (stayInbounds)
            {
                if (position.X < 128f)
                {
                    motion.X = 0.8f;
                }

                if (position.Y < 192f)
                {
                    motion.Y /= 2f;
                    flapTimer = 1000;
                }

                if (position.X > (float)(environment.DisplayWidth - 128))
                {
                    motion.X = -0.8f;
                }

                if (position.Y > (float)(environment.DisplayHeight - 128))
                {
                    motion.Y = -1f;
                    flapTimer = 100;
                }
            }

            if (summerButterfly)
            {
                sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                {
                    new (baseFrame + 1, flapSpeed),
                    new (baseFrame + 2, flapSpeed),
                    new (baseFrame + 3, flapSpeed),
                    new (baseFrame + 2, flapSpeed),
                    new (baseFrame + 1, flapSpeed),
                    new (baseFrame, flapSpeed,  doneWithFlap)
                });
            }
            else
            {
                sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                {
                    new (baseFrame + 1, flapSpeed),
                    new (baseFrame + 2, flapSpeed),
                    new (baseFrame + 1, flapSpeed),
                    new (baseFrame, flapSpeed,  doneWithFlap)
                });
            }
        }

        position += motion * motionMultiplier;
        motion.Y += 0.005f * (float)game.currentGameTime.ElapsedGameTime.Milliseconds;
        motionMultiplier -= 0.0005f * (float)game.currentGameTime.ElapsedGameTime.Milliseconds;
        if (motionMultiplier <= 0f)
        {
            motionMultiplier = 0f;
        }
        return base.update(game, environment);
    }
}