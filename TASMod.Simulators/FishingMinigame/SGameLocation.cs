using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace TASMod.Simulators.FishingMinigame;

public class SObject
{
    public int shakeTimer;
    public SObject(StardewValley.Object obj)
    {
        shakeTimer = obj.shakeTimer;
    }
    public SObject()
    {
    }
    public SObject(SObject obj)
    {
        shakeTimer = obj.shakeTimer;
    }
    public virtual SObject Clone()
    {
        return new SObject(this);
    }

    public virtual void update(SGame1 game, SGameLocation location)
    {
        if (this.shakeTimer > 0)
        {
            this.shakeTimer -= 16;
        }
        if (game.NextDouble() < 0.01)
        {
            this.shakeTimer = 100;
        }
    }

    public virtual void draw(SGame1 game)
    {
        if (shakeTimer > 0)
        {
            shakeTimer--;
            game.Next();
            game.Next();
        }
    }
}

public class SGameLocation
{
    public int DisplayWidth;
    public int DisplayHeight;
    public List<SCritter> critters = new List<SCritter>();
    public List<STemporaryAnimatedSprite> TemporarySprites = new List<STemporaryAnimatedSprite>();
    public List<SObject> objects = new List<SObject>();
    public STemporaryAnimatedSprite fishSplashAnimation;

    public SGameLocation(GameLocation location)
    {
        DisplayWidth = location.map.DisplayWidth;
        DisplayHeight = location.map.DisplayHeight;
        critters = new List<SCritter>();
        if (location.critters != null)
        {
            foreach (Critter critter in location.critters)
            {
                switch (critter)
                {
                    case Birdie birdie:
                        critters.Add(new SBirdie(birdie));
                        break;
                    case Seagull seagull:
                        critters.Add(new SSeagull(seagull));
                        break;
                    case Butterfly butterfly:
                        critters.Add(new SButterfly(butterfly));
                        break;
                    default:
                        critters.Add(new SCritter(critter));
                        break;
                }
            }
        }

        TemporarySprites = new List<STemporaryAnimatedSprite>();
        if (location.TemporarySprites != null)
        {
            foreach (TemporaryAnimatedSprite sprite in location.TemporarySprites)
            {
                TemporarySprites.Add(new STemporaryAnimatedSprite(sprite));
                if (sprite == location.fishSplashAnimation)
                {
                    fishSplashAnimation = new STemporaryAnimatedSprite(sprite);
                }
            }
        }
        if (location.objects != null)
        {
            foreach (var pair in location.objects.Pairs)
            {
                if (pair.Value.QualifiedItemId == "(O)590" || pair.Value.QualifiedItemId == "(O)SeedSpot")
                {
                    objects.Add(new SObject(pair.Value));
                }
            }
        }
    }
    public SGameLocation()
    {
    }

    protected SGameLocation(SGameLocation location)
    {
        DisplayWidth = location.DisplayWidth;
        DisplayHeight = location.DisplayHeight;
        critters = new List<SCritter>();
        if (location.critters != null)
        {
            foreach (SCritter critter in location.critters)
            {
                critters.Add(critter?.Clone());
            }
        }

        TemporarySprites = new List<STemporaryAnimatedSprite>();
        if (location.TemporarySprites != null)
        {
            foreach (STemporaryAnimatedSprite sprite in location.TemporarySprites)
            {
                TemporarySprites.Add(sprite?.Clone());
            }
        }
        objects = new List<SObject>();
        if (location.objects != null)
        {
            foreach (SObject obj in location.objects)
            {
                objects.Add(obj?.Clone());
            }
        }
    }

    public virtual SGameLocation Clone()
    {
        return new SGameLocation(this);
    }

    public virtual void UpdateWhenCurrentLocation(SGame1 game)
    {
        List<SCritter> critterList = this.critters;
        if (critterList != null && critterList.Count > 0)
        {
            Span<SCritter> critterSpan = CollectionsMarshal.AsSpan(critterList);
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < critterSpan.Length; readIndex++)
            {
                SCritter critter = critterSpan[readIndex];
                if (!critter.update(game, this))
                {
                    critterSpan[writeIndex++] = critter;
                }
            }

            if (writeIndex < critterSpan.Length)
            {
                critterList.RemoveRange(writeIndex, critterSpan.Length - writeIndex);
            }
        }

        GameLocation location = Game1.currentLocation;
        if (location.fishSplashAnimation != null)
        {
            string frenzyFish = location.fishFrenzyFish.Value;
            bool flag = !string.IsNullOrEmpty(frenzyFish);
            double num = (flag ? 0.1 : 0.02);
            if (game.NextDouble() < num)
            {
                game.Next(-32, 32);
                game.Next(-32, 32);
                if (flag)
                {
                    game.Next(-64, 64);
                    game.Next(-64, 64);
                }
                if (game.NextDouble() < 0.1)
                {
                }
            }
            if (flag && game.NextDouble() < 0.005)
            {
                game.Next(-32, 32);
                game.Next(-32, 32);
                int spriteID = 982648 + game.Next(99999);
                bool flag2 = game.NextDouble() < 0.5;
                float num2 = (float)game.Next(10, 20) / 10f;
                if (game.NextDouble() < 0.9)
                {
                    num2 *= 0.75f;
                }
                game.Next(11); game.Next(30, 41);
                game.Next(5, 10);
            }
        }
        // todo: add ore pan animation bs
        foreach (SObject obj in objects)
        {
            obj.update(game, this);
        }
    }
    public virtual void updateEvenIfFarmerIsntHere(SGame1 game)
    {
        for (int i = this.TemporarySprites.Count - 1; i >= 0; i--)
        {
            STemporaryAnimatedSprite sprite = ((i < this.TemporarySprites.Count) ? this.TemporarySprites[i] : null);
            if (i < this.TemporarySprites.Count && sprite != null && sprite.update(game) && i < this.TemporarySprites.Count)
            {
                this.TemporarySprites.RemoveAt(i);
            }
        }
    }
    public virtual void checkForMusic(SGame1 game) { }
    public virtual void draw(SGame1 game)
    {
        drawFarmers(game);
        //todo: check other places between drawfarmer and ts drawing
        foreach (SObject obj in objects)
        {
            obj.draw(game);
        }
        if (this.TemporarySprites.Count > 0)
        {
            foreach (STemporaryAnimatedSprite s in this.TemporarySprites)
            {
                if (!s.drawAboveAlwaysFront)
                {
                    s.draw(game);
                }
            }
        }
    }
    public virtual void drawFarmers(SGame1 game)
    {
        game.player.draw(game);
    }

    // getBoundingBox(delta, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
    public bool isCollidingPosition(Rectangle position)
    {
        if (this.IsOutOfBounds(position))
        {
            return false;
        }

        GameLocation location = Game1.currentLocation;
        Vector2 nextTopRight = new(position.Right / 64, position.Top / 64);
        Vector2 nextTopLeft = new(position.Left / 64, position.Top / 64);
        Vector2 nextBottomRight = new(position.Right / 64, position.Bottom / 64);
        Vector2 nextBottomLeft = new(position.Left / 64, position.Bottom / 64);
        bool nextLargerThanTile = position.Width > 64;
        Vector2 nextBottomMid = new(position.Center.X / 64, position.Bottom / 64);
        Vector2 nextTopMid = new(position.Center.X / 64, position.Top / 64);

        Span<Vector2> corners = stackalloc Vector2[6];
        int cornerCount = 0;
        AddUniqueTile(corners, ref cornerCount, nextTopRight);
        AddUniqueTile(corners, ref cornerCount, nextTopLeft);
        AddUniqueTile(corners, ref cornerCount, nextBottomLeft);
        AddUniqueTile(corners, ref cornerCount, nextBottomRight);
        if (nextLargerThanTile)
        {
            AddUniqueTile(corners, ref cornerCount, nextTopMid);
            AddUniqueTile(corners, ref cornerCount, nextBottomMid);
        }

        if (location.buildings.Count > 0)
        {
            foreach (Building b in location.buildings)
            {
                if (!b.intersects(position))
                {
                    continue;
                }
                return true;
            }
        }
        if (location.resourceClumps.Count > 0)
        {
            foreach (ResourceClump resourceClump in location.resourceClumps)
            {
                Rectangle bounds = resourceClump.getBoundingBox();
                if (bounds.Intersects(position))
                {
                    return true;
                }
            }
        }
        if (location.furniture.Count > 0)
        {
            foreach (Furniture f in location.furniture)
            {
                if (f.furniture_type.Value != 12 && f.IntersectsForCollision(position))
                {
                    return true;
                }
            }
        }
        NetCollection<LargeTerrainFeature> netCollection = location.largeTerrainFeatures;
        if (netCollection != null && netCollection.Count > 0)
        {
            foreach (LargeTerrainFeature largeTerrainFeature in netCollection)
            {
                Rectangle bounds2 = largeTerrainFeature.getBoundingBox();
                if (bounds2.Intersects(position))
                {
                    return true;
                }
            }
        }

        for (int i = 0; i < cornerCount; i++)
        {
            Vector2 corner = corners[i];
            if (location.objects.TryGetValue(corner, out var value) && value != null)
            {
                if (value.isPassable())
                {
                    continue;
                }
                Rectangle boundingBox = value.GetBoundingBox();
                if (boundingBox.Intersects(position))
                {
                    return true;
                }
            }
        }

        for (int i = 0; i < cornerCount; i++)
        {
            Vector2 corner = corners[i];
            if (location.terrainFeatures.TryGetValue(corner, out var value3)
                && value3 != null
                && value3.getBoundingBox().Intersects(position)
                && !value3.isPassable(null))
            {
                return true;
            }
        }

        foreach (Farmer otherFarmer in location.farmers)
        {
            if (position.Intersects(otherFarmer.GetBoundingBox()))
            {
                return true;
            }
        }

        xTile.Layers.Layer back_layer = location.map.RequireLayer("Back");
        for (int i = 0; i < cornerCount; i++)
        {
            Vector2 tile = corners[i];
            xTile.Tiles.Tile t2 = back_layer.Tiles[(int)tile.X, (int)tile.Y];
            if (t2 != null && t2.Properties.ContainsKey("TemporaryBarrier"))
            {
                return true;
            }
        }

        for (int i = 0; i < cornerCount; i++)
        {
            Vector2 tile = corners[i];
            xTile.Tiles.Tile tile2 = back_layer.Tiles[(int)tile.X, (int)tile.Y];
            if (tile2 != null && (tile2.TileIndexProperties.ContainsKey("Passable") || tile2.Properties.ContainsKey("Passable")))
            {
                return true;
            }
        }

        xTile.Layers.Layer buildings_layer = location.map.RequireLayer("Buildings");
        for (int i = 0; i < cornerCount; i++)
        {
            Vector2 tile = corners[i];
            xTile.Tiles.Tile tmp = buildings_layer.Tiles[(int)tile.X, (int)tile.Y];
            if (tmp != null)
            {

                if (!(tmp.TileIndexProperties.ContainsKey("Shadow") || tmp.TileIndexProperties.ContainsKey("Passable") || tmp.Properties.ContainsKey("Passable")))
                {
                    if (!(tmp.TileIndexProperties.ContainsKey("NPCPassable") || tmp.Properties.ContainsKey("NPCPassable")))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static void AddUniqueTile(Span<Vector2> tiles, ref int count, Vector2 tile)
    {
        for (int i = 0; i < count; i++)
        {
            if (tiles[i] == tile)
            {
                return;
            }
        }

        tiles[count++] = tile;
    }

    public virtual bool IsOutOfBounds(Rectangle pixelPosition)
    {
        if (pixelPosition.Right < 0 || pixelPosition.Bottom < 0)
        {
            return true;
        }
        xTile.Layers.Layer layer = Game1.currentLocation.map.Layers[0];
        if (pixelPosition.X <= layer.DisplayWidth)
        {
            return pixelPosition.Top > layer.DisplayHeight;
        }
        return true;
    }
}