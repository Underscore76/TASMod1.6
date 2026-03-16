using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.FishingMinigame;

public class SAnimatedSprite
{
    public delegate void endOfAnimationBehavior(SGame1 game, SFarmer who);
    public class SAnimationFrame
    {
        public int frame;
        public int milliseconds;
        public endOfAnimationBehavior frameStartBehavior;
        public endOfAnimationBehavior frameEndBehavior;

        public SAnimationFrame(FarmerSprite.AnimationFrame frame)
        {
            this.frame = frame.frame;
            this.milliseconds = frame.milliseconds;
            //caller handles behaviors so can ignore
        }

        public SAnimationFrame(int frame, int milliseconds, endOfAnimationBehavior frameStartBehavior = null, endOfAnimationBehavior frameEndBehavior = null)
        {
            this.frame = frame;
            this.milliseconds = milliseconds;
            this.frameStartBehavior = frameStartBehavior;
            this.frameEndBehavior = frameEndBehavior;
        }

        public SAnimationFrame(SAnimationFrame frame)
        {
            this.frame = frame.frame;
            this.milliseconds = frame.milliseconds;
            //caller handles behaviors so can ignore
        }

        public SAnimationFrame Clone()
        {
            return new SAnimationFrame(this);
        }
    }

    public float timer;
    public int currentFrame;
    public bool loop = true;
    public int currentAnimationIndex;
    public List<SAnimationFrame> currentAnimation;
    public int oldFrame;

    public SAnimatedSprite(AnimatedSprite sprite)
    {
        timer = sprite.timer;
        currentFrame = sprite.currentFrame;
        loop = sprite.loop;
        currentAnimationIndex = sprite.currentAnimationIndex;
        oldFrame = sprite.oldFrame;
        currentAnimation = new List<SAnimationFrame>();
    }
    public SAnimatedSprite()
    {
        currentAnimation = new List<SAnimationFrame>();
    }

    public SAnimatedSprite(SAnimatedSprite sprite)
    {
        timer = sprite.timer;
        currentFrame = sprite.currentFrame;
        loop = sprite.loop;
        currentAnimationIndex = sprite.currentAnimationIndex;
        oldFrame = sprite.oldFrame;
        currentAnimation = new List<SAnimationFrame>();
    }

    public SAnimatedSprite Clone()
    {
        return new SAnimatedSprite(this);
    }

    public List<SAnimationFrame> CurrentAnimation
    {
        get
        {
            if (currentAnimation.Count == 0)
                return null;
            return currentAnimation;
        }
        set
        {
            currentAnimation.Clear();
            if (value != null)
            {
                currentAnimation.AddRange(value);
            }
        }
    }
    public void setCurrentAnimation(List<SAnimationFrame> animation)
    {
        currentAnimation.Clear();
        currentAnimation.AddRange(animation);
        oldFrame = currentFrame;
        currentAnimationIndex = 0;
        if (CurrentAnimation.Count > 0)
        {
            timer = CurrentAnimation[0].milliseconds;
            currentFrame = CurrentAnimation[0].frame;
        }
    }
    public virtual bool animateOnce(SGame1 game, GameTime gameTime)
    {
        if (CurrentAnimation != null)
        {
            timer -= gameTime.ElapsedGameTime.Milliseconds;
            if (timer <= 0f)
            {
                if (CurrentAnimation[currentAnimationIndex].frameEndBehavior != null)
                {
                    CurrentAnimation[currentAnimationIndex].frameEndBehavior(game, null);
                    if (CurrentAnimation == null)
                    {
                        currentFrame = oldFrame;
                        CurrentAnimation = null;
                        return true;
                    }
                }

                currentAnimationIndex++;
                if (currentAnimationIndex >= CurrentAnimation.Count)
                {
                    if (!loop)
                    {
                        currentFrame = oldFrame;
                        CurrentAnimation = null;
                        return true;
                    }

                    currentAnimationIndex = 0;
                }

                if (CurrentAnimation[currentAnimationIndex].frameStartBehavior != null)
                {
                    CurrentAnimation[currentAnimationIndex].frameStartBehavior(game, null);
                }

                if (CurrentAnimation != null)
                {
                    timer = CurrentAnimation[currentAnimationIndex].milliseconds;
                    currentFrame = CurrentAnimation[currentAnimationIndex].frame;
                }
            }
            return false;
        }
        return true;
    }
}