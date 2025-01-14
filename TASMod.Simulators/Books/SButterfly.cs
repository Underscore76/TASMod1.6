using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using TASMod.System;
using BF = StardewValley.BellsAndWhistles.Butterfly;

namespace TASMod.Simulators.Books
{

    public class SButterfly : SCritter
    {
        public int flapTimer;
        public int flapSpeed;
        public Vector2 motion;
        public float motionMultiplier;
        public bool summerButterfly;
        public bool stayInbounds;
        public bool isPrismatic;

        public void doneWithFlap(Farmer who)
        {
            flapTimer = 200 + random.Next(-5, 6);
        }

        public SButterfly(Random r, BF other)
        : base(r)
        {
            flapTimer = Reflector.GetValue<BF, int>(other, "flapTimer");
            flapSpeed = Reflector.GetValue<BF, int>(other, "flapSpeed");
            motion = Reflector.GetValue<BF, Vector2>(other, "motion");
            motionMultiplier = Reflector.GetValue<BF, float>(other, "motionMultiplier");
            summerButterfly = Reflector.GetValue<BF, bool>(other, "summerButterfly");
            stayInbounds = Reflector.GetValue<BF, bool>(other, "stayInbounds");
            isPrismatic = Reflector.GetValue<BF, bool>(other, "isPrismatic");
        }

        public SButterfly(Random r, Vector2 position)
            : base(r)
        {
            base.position = position * 64f;
            startingPosition = base.position;
        }

        public override bool update(SGameLocation loc)
        {
            GameTime time = TASDateTime.CurrentGameTime;
            flapTimer -= 16;
            if (flapTimer <= 0 && sprite.CurrentAnimation == null)
            {
                motionMultiplier = 1f;
                motion.X += (float)random.Next(-80, 81) / 100f;
                motion.Y = (float)(random.NextDouble() + 0.25) * -3f / 2f;
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

                    if (position.X > (float)(loc.DisplayWidth - 128))
                    {
                        motion.X = -0.8f;
                    }

                    if (position.Y > (float)(loc.DisplayHeight - 128))
                    {
                        motion.Y = -1f;
                        flapTimer = 100;
                    }
                }

                if (summerButterfly)
                {
                    sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                    {
                        new FarmerSprite.AnimationFrame(baseFrame + 1, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame + 2, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame + 3, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame + 2, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame + 1, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame, flapSpeed, secondaryArm: false, flip: false, doneWithFlap)
                    });
                }
                else
                {
                    sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                    {
                        new FarmerSprite.AnimationFrame(baseFrame + 1, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame + 2, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame + 1, flapSpeed),
                        new FarmerSprite.AnimationFrame(baseFrame, flapSpeed, secondaryArm: false, flip: false, doneWithFlap)
                    });
                }
                position += motion * motionMultiplier;
                motion.Y += 0.005f * (float)time.ElapsedGameTime.Milliseconds;
                motionMultiplier -= 0.0005f * (float)time.ElapsedGameTime.Milliseconds;
                if (motionMultiplier <= 0f)
                {
                    motionMultiplier = 0f;
                }
            }
            return base.update(loc);
        }
    }
}