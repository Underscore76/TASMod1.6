// TOWN FIRST SPRITE IS THE DOOR which has shake intensity
// the rest are likely
using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.FishingMinigame;

public class STemporaryAnimatedSprite
{
    public int currentParentTileIndex;
    public int oldCurrentParentTileIndex;
    public bool drawAboveAlwaysFront;
    public float shakeIntensity;
    public float shakeIntensityChange;
    public bool local;
    public bool hasText;
    public bool hasTexture;
    public int TextureWidth;
    public bool bigCraftable;

    public bool hasAttachedCharacter;
    public bool hasEndFunction;
    public int delayBeforeAnimationStart;
    public int ticksBeforeAnimationStart;
    public int animationLength;
    public float timer;
    public float totalTimer;
    public float interval = 200f;
    public int currentNumberOfLoops;
    public int totalNumberOfLoops;

    public bool destroyable;
    public Rectangle sourceRect;
    public Vector2 sourceRectStartingPos;
    public bool paused;
    public bool overrideLocationDestroy;
    public float originalScale;
    public float scale = 1f;
    public float scaleChange;
    public float scaleChangeChange;
    public bool timeBasedMotion;

    public bool flicker;
    public bool flash;
    public bool pingPong;
    public int pingPongMotion = 1;
    public bool pulse;
    public float pulseTime;
    public float pulseTimer;
    public float pulseAmount = 1.1f;
    public float alpha = 1f;
    public float alphaFade;

    public float alphaFadeFade;
    public int initialParentTileIndex;
    public bool holdLastFrame;

    public STemporaryAnimatedSprite(TemporaryAnimatedSprite sprite)
    {
        if (sprite.parentSprite != null)
        {
            throw new NotImplementedException("parentSprite is not null, which we don't support");
        }
        if (sprite.endFunction != null)
        {
            throw new NotImplementedException("endFunction is not null, which we don't support");
        }

        currentParentTileIndex = sprite.currentParentTileIndex;
        oldCurrentParentTileIndex = sprite.oldCurrentParentTileIndex;
        drawAboveAlwaysFront = sprite.drawAboveAlwaysFront;
        shakeIntensity = sprite.shakeIntensity;
        shakeIntensityChange = sprite.shakeIntensityChange;
        local = sprite.local;
        hasText = sprite.text != null;
        hasTexture = sprite.texture != null;
        TextureWidth = hasTexture ? sprite.Texture.Width : 0;
        bigCraftable = sprite.bigCraftable;

        hasAttachedCharacter = sprite.attachedCharacter != null;
        hasEndFunction = sprite.endFunction != null;
        delayBeforeAnimationStart = sprite.delayBeforeAnimationStart;
        ticksBeforeAnimationStart = sprite.ticksBeforeAnimationStart;
        animationLength = sprite.animationLength;
        timer = sprite.timer;
        totalTimer = (float)Reflector.GetValue(sprite, "totalTimer");
        interval = sprite.interval;
        currentNumberOfLoops = sprite.currentNumberOfLoops;
        totalNumberOfLoops = sprite.totalNumberOfLoops;

        destroyable = sprite.destroyable;
        sourceRect = sprite.sourceRect;
        sourceRectStartingPos = sprite.sourceRectStartingPos;
        paused = sprite.paused;
        overrideLocationDestroy = sprite.overrideLocationDestroy;
        originalScale = (float)Reflector.GetValue(sprite, "originalScale");
        scale = sprite.scale;
        scaleChange = sprite.scaleChange;
        scaleChangeChange = sprite.scaleChangeChange;
        timeBasedMotion = sprite.timeBasedMotion;

        flicker = sprite.flicker;
        flash = sprite.flash;
        pingPong = sprite.pingPong;
        pingPongMotion = sprite.pingPongMotion;
        pulse = sprite.pulse;
        pulseTime = sprite.pulseTime;
        pulseTimer = (float)Reflector.GetValue(sprite, "pulseTimer");
        pulseAmount = sprite.pulseAmount;
        alpha = sprite.alpha;
        alphaFade = sprite.alphaFade;

        alphaFadeFade = sprite.alphaFadeFade;
        initialParentTileIndex = sprite.initialParentTileIndex;
        holdLastFrame = sprite.holdLastFrame;
    }
    public STemporaryAnimatedSprite()
    {
    }

    public STemporaryAnimatedSprite(STemporaryAnimatedSprite sprite)
    {
        currentParentTileIndex = sprite.currentParentTileIndex;
        oldCurrentParentTileIndex = sprite.oldCurrentParentTileIndex;
        drawAboveAlwaysFront = sprite.drawAboveAlwaysFront;
        shakeIntensity = sprite.shakeIntensity;
        shakeIntensityChange = sprite.shakeIntensityChange;
        local = sprite.local;
        hasText = sprite.hasText;
        hasTexture = sprite.hasTexture;
        TextureWidth = sprite.TextureWidth;
        bigCraftable = sprite.bigCraftable;

        hasAttachedCharacter = sprite.hasAttachedCharacter;
        hasEndFunction = sprite.hasEndFunction;
        delayBeforeAnimationStart = sprite.delayBeforeAnimationStart;
        ticksBeforeAnimationStart = sprite.ticksBeforeAnimationStart;
        animationLength = sprite.animationLength;
        timer = sprite.timer;
        totalTimer = sprite.totalTimer;
        interval = sprite.interval;
        currentNumberOfLoops = sprite.currentNumberOfLoops;
        totalNumberOfLoops = sprite.totalNumberOfLoops;

        destroyable = sprite.destroyable;
        sourceRect = sprite.sourceRect;
        sourceRectStartingPos = sprite.sourceRectStartingPos;
        paused = sprite.paused;
        overrideLocationDestroy = sprite.overrideLocationDestroy;
        originalScale = sprite.originalScale;
        scale = sprite.scale;
        scaleChange = sprite.scaleChange;
        scaleChangeChange = sprite.scaleChangeChange;
        timeBasedMotion = sprite.timeBasedMotion;

        flicker = sprite.flicker;
        flash = sprite.flash;
        pingPong = sprite.pingPong;
        pingPongMotion = sprite.pingPongMotion;
        pulse = sprite.pulse;
        pulseTime = sprite.pulseTime;
        pulseTimer = sprite.pulseTimer;
        pulseAmount = sprite.pulseAmount;
        alpha = sprite.alpha;
        alphaFade = sprite.alphaFade;

        alphaFadeFade = sprite.alphaFadeFade;
        initialParentTileIndex = sprite.initialParentTileIndex;
        holdLastFrame = sprite.holdLastFrame;
    }

    public STemporaryAnimatedSprite Clone()
    {
        return new STemporaryAnimatedSprite(this);
    }

    public bool update(SGame1 game)
    {
        if (this.paused)
        {
            return false;
        }
        int elapsedMs = (int)game.currentGameTime.ElapsedGameTime.TotalMilliseconds;
        if (this.ticksBeforeAnimationStart > 0)
        {
            this.ticksBeforeAnimationStart--;
            return false;
        }
        if (this.delayBeforeAnimationStart > 0)
        {
            this.delayBeforeAnimationStart -= elapsedMs;
            if (this.delayBeforeAnimationStart <= 0)
            {
                this.timer = -this.delayBeforeAnimationStart;
            }
            return false;
        }
        this.timer += elapsedMs;
        this.totalTimer += elapsedMs;
        this.alpha -= this.alphaFade * (float)((!this.timeBasedMotion) ? 1 : elapsedMs);
        this.alphaFade -= this.alphaFadeFade * (float)((!this.timeBasedMotion) ? 1 : elapsedMs);
        this.shakeIntensity += this.shakeIntensityChange * (float)elapsedMs;
        this.scale += this.scaleChange * (float)((!this.timeBasedMotion) ? 1 : elapsedMs);
        this.scaleChange += this.scaleChangeChange * (float)((!this.timeBasedMotion) ? 1 : elapsedMs);

        if (!this.pingPong)
        {
            this.pingPongMotion = 1;
        }
        if (this.pulse)
        {
            this.pulseTimer -= elapsedMs;
            if (this.originalScale == 0f)
            {
                this.originalScale = this.scale;
            }
            if (this.pulseTimer <= 0f)
            {
                this.pulseTimer = this.pulseTime;
                this.scale = this.originalScale * this.pulseAmount;
            }
            if (this.scale > this.originalScale)
            {
                this.scale -= this.pulseAmount / 100f * (float)elapsedMs;
            }
        }
        if (this.alpha <= 0f || this.scale <= 0f)
        {
            return this.destroyable;
        }
        if (this.timer > this.interval)
        {
            this.currentParentTileIndex += this.pingPongMotion;
            this.sourceRect.X += this.sourceRect.Width * this.pingPongMotion;
            if (hasTexture)
            {
                if (!this.pingPong && this.sourceRect.X >= TextureWidth)
                {
                    this.sourceRect.Y += this.sourceRect.Height;
                }
                if (!this.pingPong)
                {
                    this.sourceRect.X %= TextureWidth;
                }
                if (this.pingPong)
                {
                    if ((float)this.sourceRect.X + ((float)this.sourceRect.Y - this.sourceRectStartingPos.Y) / (float)this.sourceRect.Height * (float)TextureWidth >= this.sourceRectStartingPos.X + (float)(this.sourceRect.Width * this.animationLength))
                    {
                        this.pingPongMotion = -1;
                        this.sourceRect.X -= this.sourceRect.Width * 2;
                        this.currentParentTileIndex--;
                        if (this.sourceRect.X < 0)
                        {
                            this.sourceRect.X = TextureWidth + this.sourceRect.X;
                        }
                    }
                    else if ((float)this.sourceRect.X < this.sourceRectStartingPos.X && (float)this.sourceRect.Y == this.sourceRectStartingPos.Y)
                    {
                        this.pingPongMotion = 1;
                        this.sourceRect.X = (int)this.sourceRectStartingPos.X + this.sourceRect.Width;
                        this.currentParentTileIndex++;
                        this.currentNumberOfLoops++;
                        if (hasEndFunction)
                        {
                            // warning: we don't handle this case
                        }
                        if (this.currentNumberOfLoops >= this.totalNumberOfLoops)
                        {
                            // run the end function if it exists
                            return this.destroyable;
                        }
                    }
                }
                else if (this.totalNumberOfLoops >= 1 && (float)this.sourceRect.X + ((float)this.sourceRect.Y - this.sourceRectStartingPos.Y) / (float)this.sourceRect.Height * (float)TextureWidth >= this.sourceRectStartingPos.X + (float)(this.sourceRect.Width * this.animationLength))
                {
                    this.sourceRect.X = (int)this.sourceRectStartingPos.X;
                    this.sourceRect.Y = (int)this.sourceRectStartingPos.Y;
                }
            }
            this.timer -= this.interval;
            if (this.flicker)
            {
                if (this.currentParentTileIndex < 0 || this.flash)
                {
                    this.currentParentTileIndex = this.oldCurrentParentTileIndex;
                    this.flash = false;
                }
                else
                {
                    this.oldCurrentParentTileIndex = this.currentParentTileIndex;
                    this.currentParentTileIndex = -100;
                }
            }
            if (this.currentParentTileIndex - this.initialParentTileIndex >= this.animationLength)
            {
                this.currentNumberOfLoops++;
                if (this.holdLastFrame)
                {
                    this.currentParentTileIndex = this.initialParentTileIndex + this.animationLength - 1;
                    if (hasTexture)
                    {
                        // setSourceRectToCurrentTileIndex
                        this.sourceRect.X = (int)(this.sourceRectStartingPos.X + (float)(this.currentParentTileIndex * this.sourceRect.Width)) % TextureWidth;
                        if (this.sourceRect.X < 0)
                        {
                            this.sourceRect.X = 0;
                        }
                        this.sourceRect.Y = (int)this.sourceRectStartingPos.Y;
                    }
                    if (hasEndFunction)
                    {
                    }
                    return false;
                }
                this.currentParentTileIndex = this.initialParentTileIndex;
                if (this.currentNumberOfLoops >= this.totalNumberOfLoops)
                {
                    return this.destroyable;
                }
            }
        }
        return false;
    }
    public void draw(SGame1 game)
    {
        if (this.currentParentTileIndex < 0 || this.delayBeforeAnimationStart > 0 || this.ticksBeforeAnimationStart > 0)
        {
            return;
        }
        if (hasText)
        {
        }
        else if (hasTexture)
        {
            if (shakeIntensity > 0)
            {
                game.Next(-(int)shakeIntensity, (int)shakeIntensity + 1);
                game.Next(-(int)shakeIntensity, (int)shakeIntensity + 1);
            }
        }
        else if (this.bigCraftable)
        {
        }
        else
        {
            if (this.hasAttachedCharacter)
            {
            }
            else
            {
                if (shakeIntensity > 0)
                {
                    game.Next(-(int)shakeIntensity, (int)shakeIntensity + 1);
                    game.Next(-(int)shakeIntensity, (int)shakeIntensity + 1);
                }
            }
        }
    }
}
