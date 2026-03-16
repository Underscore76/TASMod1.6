using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace TASMod.Simulators.FishingMinigame;

public class SBirdie : SCritter
{
    public int state;
    public float flightOffset;
    public bool stationary;
    public int characterCheckTimer = 200;
    public int walkTimer;

    public SBirdie(Birdie birdie)
        : base(birdie)
    {
        state = (int)Reflector.GetValue(birdie, "state");
        flightOffset = (float)Reflector.GetValue(birdie, "flightOffset");
        stationary = (bool)Reflector.GetValue(birdie, "stationary");
        characterCheckTimer = (int)Reflector.GetValue(birdie, "characterCheckTimer");
        walkTimer = (int)Reflector.GetValue(birdie, "walkTimer");
        for (int i = 0; i < birdie.sprite.CurrentAnimation?.Count; i++)
        {
            var anim = birdie.sprite.CurrentAnimation[i];
            SAnimatedSprite.SAnimationFrame frame = new SAnimatedSprite.SAnimationFrame(anim);
            if (anim.frameStartBehavior != null)
            {
                switch (anim.frameStartBehavior.Method.Name)
                {
                    case "hop":
                        frame.frameStartBehavior = this.hop;
                        break;
                    case "donePecking":
                        frame.frameStartBehavior = this.donePecking;
                        break;
                    case "playPeck":
                    case "playFlap":
                        // these behaviors only play audio/don't affect state
                        break;
                    default:
                        throw new Exception($"Unexpected frame behavior {anim.frameStartBehavior.Method.Name} in SBirdie constructor");
                }
            }
            sprite.currentAnimation.Add(frame);
        }
    }

    public SBirdie()
    {
    }

    protected SBirdie(SBirdie birdie)
        : base(birdie)
    {
        state = birdie.state;
        flightOffset = birdie.flightOffset;
        stationary = birdie.stationary;
        characterCheckTimer = birdie.characterCheckTimer;
        walkTimer = birdie.walkTimer;
        for (int i = 0; i < birdie.sprite.CurrentAnimation?.Count; i++)
        {
            var anim = birdie.sprite.CurrentAnimation[i];
            SAnimatedSprite.SAnimationFrame frame = new SAnimatedSprite.SAnimationFrame(anim);
            if (anim.frameStartBehavior != null)
            {
                switch (anim.frameStartBehavior.Method.Name)
                {
                    case "hop":
                        frame.frameStartBehavior = this.hop;
                        break;
                    case "donePecking":
                        frame.frameStartBehavior = this.donePecking;
                        break;
                    default:
                        throw new Exception($"Unexpected frame behavior {anim.frameStartBehavior.Method.Name} in SBirdie clone");
                }
            }
            sprite.currentAnimation.Add(frame);
        }
    }

    public override SCritter Clone()
    {
        return new SBirdie(this);
    }

    public void hop(SGame1 game, SFarmer who)
    {
        gravityAffectedDY = -2f;
    }
    public void donePecking(SGame1 game, SFarmer who)
    {
        state = game.Choose(0, 3);
    }
    public override bool update(SGame1 game, SGameLocation location)
    {
        if (yJumpOffset < 0f && state != 1 && !stationary)
        {
            if (!flip && !location.isCollidingPosition(getBoundingBox(-2, 0)))
            {
                position.X -= 2f;
            }
            else if (!location.isCollidingPosition(getBoundingBox(2, 0)))
            {
                position.X += 2f;
            }
        }
        characterCheckTimer -= game.currentGameTime.ElapsedGameTime.Milliseconds;
        if (this.characterCheckTimer < 0)
        {
            Character f = Utility.isThereAFarmerOrCharacterWithinDistance(base.position / 64f, 4, Game1.currentLocation);
            this.characterCheckTimer = 200;
            if (f != null && this.state != 1)
            {
                if (game.NextDouble() < 0.85)
                {
                }
                this.state = 1;
                if (f.Position.X > base.position.X)
                {
                    base.flip = false;
                }
                else
                {
                    base.flip = true;
                }
                base.sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                    {
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 6), 70),
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 7), 60),
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 8), 70),
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 7), 60)
                    });
                base.sprite.loop = true;
            }
        }
        switch (this.state)
        {
            case 0:
                if (base.sprite.CurrentAnimation == null)
                {
                    List<SAnimatedSprite.SAnimationFrame> peckAnim = new List<SAnimatedSprite.SAnimationFrame>
                    {
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 2), 480),
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 3), 170),
                        new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 4), 170),
                    };
                    int pecks = game.Next(1, 5);
                    for (int i = 0; i < pecks; i++)
                    {
                        peckAnim.Add(new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 3), 70));
                        peckAnim.Add(new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 4), 100));
                    }
                    peckAnim.Add(new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 3), 100));
                    peckAnim.Add(new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 2), 70));
                    peckAnim.Add(new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 1), 70));
                    peckAnim.Add(new SAnimatedSprite.SAnimationFrame((short)base.baseFrame, 500, donePecking));
                    base.sprite.loop = false;
                    base.sprite.setCurrentAnimation(peckAnim);
                }
                break;
            case 1:
                if (!base.flip)
                {
                    base.position.X -= 6f;
                }
                else
                {
                    base.position.X += 6f;
                }
                base.yOffset -= 2f + this.flightOffset;
                break;
            case 2:
                if (base.sprite.CurrentAnimation == null)
                {
                    base.sprite.currentFrame = base.baseFrame + 5;
                }
                if (game.NextDouble() < 0.003 && base.sprite.CurrentAnimation == null)
                {
                    this.state = 3;
                }
                break;
            case 4:
                if (!this.stationary)
                {
                    int delta = (base.flip ? 1 : (-1));
                    if (!location.isCollidingPosition(this.getBoundingBox(delta, 0)))
                    {
                        base.position.X += delta;
                    }
                }
                else
                {
                    float delta2 = (base.flip ? 0.5f : (-0.5f));
                    if (Math.Abs(base.position.X + delta2 - base.startingPosition.X) < 8f)
                    {
                        base.position.X += delta2;
                    }
                    else
                    {
                        base.flip = !base.flip;
                    }
                }
                this.walkTimer -= game.currentGameTime.ElapsedGameTime.Milliseconds;
                if (this.walkTimer < 0)
                {
                    this.state = 3;
                    base.sprite.loop = false;
                    base.sprite.CurrentAnimation = null;
                    base.sprite.currentFrame = base.baseFrame;
                }
                break;
            case 3:
                if (game.NextDouble() < 0.008 && base.sprite.CurrentAnimation == null && base.yJumpOffset >= 0f)
                {
                    switch (game.Next(6))
                    {
                        case 0:
                            this.state = 2;
                            break;
                        case 1:
                            this.state = 0;
                            break;
                        case 2:
                            this.hop(game, null);
                            break;
                        case 3:
                            base.flip = !base.flip;
                            this.hop(game, null);
                            break;
                        case 4:
                        case 5:
                            this.state = 4;
                            base.sprite.setCurrentAnimation(new List<SAnimatedSprite.SAnimationFrame>
                        {
                            new SAnimatedSprite.SAnimationFrame((short)base.baseFrame, 100),
                            new SAnimatedSprite.SAnimationFrame((short)(base.baseFrame + 1), 100)
                        });
                            base.sprite.loop = true;
                            if (base.position.X >= base.startingPosition.X)
                            {
                                base.flip = false;
                            }
                            else
                            {
                                base.flip = true;
                            }
                            this.walkTimer = game.Next(5, 15) * 100;
                            break;
                    }
                }
                else if (base.sprite.CurrentAnimation == null)
                {
                    base.sprite.currentFrame = base.baseFrame;
                }
                break;
        }
        return base.update(game, location);
    }
}
