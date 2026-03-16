using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace TASMod.Simulators.FishingMinigame;

public class SSeagull : SCritter
{
    public int state;
    public int characterCheckTimer;
    public bool moveLeft;

    public SSeagull(Seagull seagull)
        : base(seagull)
    {
        state = (int)Reflector.GetValue(seagull, "state");
        characterCheckTimer = (int)Reflector.GetValue(seagull, "characterCheckTimer");
        moveLeft = (bool)Reflector.GetValue(seagull, "moveLeft");
        for (int i = 0; i < seagull.sprite.CurrentAnimation?.Count; i++)
        {
            var anim = seagull.sprite.CurrentAnimation[i];
            SAnimatedSprite.SAnimationFrame frame = new SAnimatedSprite.SAnimationFrame(anim);
            if (anim.frameStartBehavior != null)
            {
                frame.frameStartBehavior = this.hop;
            }
            sprite.currentAnimation.Add(frame);
        }
    }

    public SSeagull()
    {
    }

    protected SSeagull(SSeagull seagull)
        : base(seagull)
    {
        state = seagull.state;
        characterCheckTimer = seagull.characterCheckTimer;
        moveLeft = seagull.moveLeft;
        for (int i = 0; i < seagull.sprite.CurrentAnimation?.Count; i++)
        {
            var anim = seagull.sprite.CurrentAnimation[i];
            SAnimatedSprite.SAnimationFrame frame = new SAnimatedSprite.SAnimationFrame(anim);
            if (anim.frameStartBehavior != null)
            {
                frame.frameStartBehavior = this.hop;
            }
            sprite.currentAnimation.Add(frame);
        }
    }

    public override SCritter Clone()
    {
        return new SSeagull(this);
    }

    public void hop(SGame1 game, SFarmer who)
    {
        gravityAffectedDY = -4f;
    }

    public override bool update(SGame1 game, SGameLocation environment)
    {
        characterCheckTimer -= 16;
        if (characterCheckTimer < 0)
        {
            Character f = Utility.isThereAFarmerOrCharacterWithinDistance(base.position / 64f, 4, Game1.currentLocation);
            this.characterCheckTimer = 200;
            if (f != null && this.state != 1)
            {
                if (game.NextDouble() < 0.25)
                {
                }
                this.state = 1;
                if (f.Position.X > base.position.X)
                {
                    this.moveLeft = true;
                }
                else
                {
                    this.moveLeft = false;
                }
                sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                {
                    new SAnimatedSprite.SAnimationFrame(baseFrame + 10, 80),
                    new SAnimatedSprite.SAnimationFrame(baseFrame + 11, 80),
                    new SAnimatedSprite.SAnimationFrame(baseFrame + 12, 80),
                    new SAnimatedSprite.SAnimationFrame(baseFrame + 13, 100)
                });
                sprite.loop = true;
            }
        }
        switch (state)
        {
            case 0:
                int delta = (this.moveLeft ? (-2) : 2);
                if (!environment.isCollidingPosition(getBoundingBox(delta, 0)))
                {
                    base.position.X += delta;
                }
                if (game.NextDouble() < 0.005)
                {
                    this.state = 3;
                    sprite.loop = false;
                    sprite.CurrentAnimation = null;
                    sprite.currentFrame = 0;
                }
                break;
            case 2:
                sprite.currentFrame = baseFrame + 9;
                float tmpY = yOffset;
                if ((game.currentGameTime.TotalGameTime.TotalMilliseconds + (double)((int)base.position.X * 4)) % 2000.0 < 1000.0)
                {
                    yOffset = 2f;
                }
                else
                {
                    yOffset = 0f;
                }
                if (yOffset > tmpY)
                {
                    game.NextBool();
                    // added a temporary sprite...
                }
                break;
            case 1:
                if (this.moveLeft)
                {
                    base.position.X -= 4f;
                }
                else
                {
                    base.position.X += 4f;
                }
                base.yOffset -= 2f;
                break;
            case 3:
                if (game.NextDouble() < 0.003 && sprite.CurrentAnimation == null)
                {
                    sprite.loop = false;
                    int extra;
                    List<SAnimatedSprite.SAnimationFrame> frames;
                    switch (game.Next(4))
                    {
                        case 0:
                            frames = new List<SAnimatedSprite.SAnimationFrame>
                            {
                                new(baseFrame + 2, 100),
                                new(baseFrame + 3, 100),
                                new(baseFrame + 4, 200),
                                new(baseFrame + 5, 200)
                            };
                            extra = game.Next(5);
                            for (int j = 0; j < extra; j++)
                            {
                                frames.Add(new(baseFrame + 4, 200));
                                frames.Add(new(baseFrame + 5, 200));
                            }
                            sprite.setCurrentAnimation(frames);
                            break;
                        case 1:
                            sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                            {
                                new(6, game.Next(500, 4000))
                            });
                            break;
                        case 2:
                            frames = new List<SAnimatedSprite.SAnimationFrame>
                            {
                                new(baseFrame + 6, 500),
                                new(baseFrame + 7, 100, frameStartBehavior: hop),
                                new(baseFrame + 8, 100)
                            };
                            extra = game.Next(3);
                            for (int i = 0; i < extra; i++)
                            {
                                frames.Add(new(baseFrame + 7, 100));
                                frames.Add(new(baseFrame + 8, 100));
                            }
                            sprite.setCurrentAnimation(frames);
                            break;
                        case 3:
                            state = 0;
                            sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                            {
                                new(baseFrame, 200),
                                new(baseFrame + 1, 200)
                            });
                            sprite.loop = true;
                            moveLeft = game.NextBool();
                            if (game.NextDouble() < 0.33)
                            {
                                if (base.position.X > base.startingPosition.X)
                                {
                                    moveLeft = true;
                                }
                                else
                                {
                                    moveLeft = false;
                                }
                            }
                            break;
                    }
                }
                else if (sprite.CurrentAnimation == null)
                {
                    sprite.currentFrame = baseFrame;
                }
                break;
        }
        flip = !moveLeft;
        return base.update(game, environment);
    }
}
