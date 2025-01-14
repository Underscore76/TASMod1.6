using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Objects.Trinkets;
using TASMod.Extensions;
using xTile;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace TASMod.Simulators.SkullCaverns
{
    public class SMineShaft : SSGameLocation
    {
        public static LocalizedContentManager content = null;
        public static Map _map = null;

        public static Map Map
        {
            get
            {
                if (_map == null)
                {
                    _map = GetMap("Maps\\Mines\\10");
                }
                return _map;
            }
        }

        public static Map GetMap(string path)
        {
            if (content == null)
            {
                content = new LocalizedContentManager(
                    Game1.content.ServiceProvider,
                    Game1.content.RootDirectory
                );
            }
            var map = content.Load<Map>(path);
            if (map.GetLayer("Back") == null)
            {
                content.Unload();
                content = null;
                return GetMap(path);
            }
            return map;
        }

        public bool hasAddedDesertFestivalStatue;
        public int stonesLeftOnThisLevel;
        public int previousMapNumber;
        public Vector2 tileBeneathLadder;
        public Vector2 tileBeneathElevator;
        public Point calicoStatueSpot;
        public int mineLevel;
        public bool ladderHasSpawned;
        public bool isTreasureRoom;
        public bool loadedDarkArea;
        public Random mineRandom;
        public List<SItem> ChestItems = new List<SItem>();

        public SMineShaft(int level, int loadedMapNumber, Random random)
        {
            mineLevel = level;
            previousMapNumber = loadedMapNumber;

            Random sharedRandom = random.Copy();
            mineRandom = sharedRandom.SampleNet6Random();
            tileBeneathLadder = new Vector2(6, 6);
            tileBeneathElevator = new Vector2(12, 6);
        }

        public bool generateContents(Random Game1_random)
        {
            loadLevel(Game1_random);
            if (!isTreasureRoom && mineLevel != 420 && mineLevel != 320 && mineLevel != 220)
            {
                return false;
            }
            chooseLevelType(Game1_random);
            Controller.Console.Warn(
                $"\test: a:chooseLevelType: {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
            );
            findLadder(Game1_random);
            Controller.Console.Warn(
                $"\test: b:populateLevel: {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
            );
            populateLevel(Game1_random);
            return true;
        }

        public void loadLevel(Random Game1_random)
        {
            int num;
            for (num = mineRandom.Next(40); num == previousMapNumber; num = mineRandom.Next(40)) { }

            while (num % 5 == 0)
            {
                num = mineRandom.Next(40);
            }
            if (mineLevel == 420 || mineLevel == 320 || mineLevel == 220)
            {
                num = 10;
            }
            else if (mineLevel >= 130)
            {
                double num2 = Game1.player.DailyLuck / 10.0 + Game1.player.LuckLevel / 100.0;
                if (Game1_random.NextDouble() < num2)
                {
                    isTreasureRoom = true;
                    num = 10;
                }
            }
            if (num != 10)
                return;

            map = Map;
            if (GetAdditionalDifficulty() > 0)
            {
                if (mineLevel % 40 >= 30)
                {
                    loadedDarkArea = true;
                }
            }
        }

        public void chooseLevelType(Random Game1_random) { }

        public void findLadder(Random Game1_random) { }

        public bool isDarkArea()
        {
            return loadedDarkArea || mineLevel % 40 > 30;
        }

        public bool isTileClearForMineObjects(int x, int y)
        {
            return isTileClearForMineObjects(new Vector2(x, y));
        }

        public bool isTileClearForMineObjects(Vector2 v)
        {
            if (tileBeneathLadder.Equals(v) || tileBeneathElevator.Equals(v))
            {
                return false;
            }

            if (
                !CanItemBePlacedHere(
                    v,
                    itemIsPassable: false,
                    CollisionMask.All,
                    CollisionMask.None
                )
            )
            {
                return false;
            }

            if (IsTileOccupiedBy(v, CollisionMask.Characters))
            {
                return false;
            }

            if (IsTileOccupiedBy(v, CollisionMask.Flooring | CollisionMask.TerrainFeatures))
            {
                return false;
            }

            string text = doesTileHaveProperty((int)v.X, (int)v.Y, "Type", "Back");
            if (text == null || !text.Equals("Stone"))
            {
                return false;
            }

            if (!isTileOnClearAndSolidGround(v))
            {
                return false;
            }

            if (Objects.ContainsKey(v))
            {
                return false;
            }

            if (Utility.PointToVector2(calicoStatueSpot).Equals(v))
            {
                return false;
            }

            return true;
        }

        public override bool IsLocationSpecificOccupantOnTile(Vector2 tileLocation)
        {
            if (tileBeneathLadder.Equals(tileLocation))
            {
                return true;
            }
            if (tileBeneathElevator != Vector2.Zero && tileBeneathElevator.Equals(tileLocation))
            {
                return true;
            }

            return base.IsLocationSpecificOccupantOnTile(tileLocation);
        }

        public bool isTileOnClearAndSolidGround(Vector2 v)
        {
            if (
                hasTileAt((int)v.X, (int)v.Y, "Back")
                && !hasTileAt((int)v.X, (int)v.Y, "Front")
                && !hasTileAt((int)v.X, (int)v.Y, "Buildings")
            )
            {
                return getTileIndexAt((int)v.X, (int)v.Y, "Back", "mine") != 77;
            }

            return false;
        }

        public bool isTileOnClearAndSolidGround(int x, int y)
        {
            if (hasTileAt(x, y, "Back") && !hasTileAt(x, y, "Front"))
            {
                return getTileIndexAt(x, y, "Back", "mine") != 77;
            }

            return false;
        }

        public float getDistanceFromStart(int xTile, int yTile)
        {
            float num = Utility.distance(xTile, tileBeneathLadder.X, yTile, tileBeneathLadder.Y);
            if (tileBeneathElevator != Vector2.Zero)
            {
                num = Math.Min(
                    num,
                    Utility.distance(xTile, tileBeneathElevator.X, yTile, tileBeneathElevator.Y)
                );
            }
            return num;
        }

        public void populateLevel(Random Game1_random)
        {
            Point calicoStatueSpot;
            stonesLeftOnThisLevel = 0;
            double stoneChance = (double)mineRandom.Next(10, 30) / 100.0;
            double monsterChance = 0.002 + (double)mineRandom.Next(200) / 10000.0;
            double itemChance = 0.0025;
            double gemStoneChance = 0.003;
            adjustLevelChances(
                ref stoneChance,
                ref monsterChance,
                ref itemChance,
                ref gemStoneChance
            );
            Controller.Console.Warn(
                $"\test: populateLevel ({stonesLeftOnThisLevel}) {stoneChance} {monsterChance} {itemChance} {gemStoneChance}"
            );
            float num2 = 0f;
            if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && mineLevel > 131)
            {
                num2 += 1f - 130f / (float)mineLevel;
            }

            if (mineRandom.NextBool())
            {
                Layer layer = map.RequireLayer("Back");
                int num3 = mineRandom.Next(5) + (int)(Game1.player.DailyLuck * 20.0);

                for (int i = 0; i < num3; i++)
                {
                    Point value;
                    Point point;
                    if (mineRandom.NextDouble() < 0.33 + (double)(num2 / 2f))
                    {
                        value = new Point(mineRandom.Next(layer.LayerWidth), 0);
                        point = new Point(0, 1);
                    }
                    else if (mineRandom.NextBool())
                    {
                        value = new Point(0, mineRandom.Next(layer.LayerHeight));
                        point = new Point(1, 0);
                    }
                    else
                    {
                        value = new Point(layer.LayerWidth - 1, mineRandom.Next(layer.LayerHeight));
                        point = new Point(-1, 0);
                    }

                    while (isTileOnMap(value.X, value.Y))
                    {
                        value.X += point.X;
                        value.Y += point.Y;
                        if (!isTileClearForMineObjects(value.X, value.Y))
                        {
                            continue;
                        }

                        Vector2 vector = new Vector2(value.X, value.Y);

                        if (
                            Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                            && !hasAddedDesertFestivalStatue
                            && hasTileAt((int)vector.X, (int)vector.Y - 1, "Buildings")
                        )
                        {
                            calicoStatueSpot = value;
                            hasAddedDesertFestivalStatue = true;
                        }
                        else
                        {
                            // objects.Add(vector, BreakableContainer.GetBarrelForMines(vector, this));
                            // breakableContainer = new BreakableContainer(vector, text, this);
                            string text =
                                (GetAdditionalDifficulty() > 0) ? "Container_118" : "Container_124";
                            Controller.Console.Warn(
                                $"\test: before barrel ({stonesLeftOnThisLevel}) {vector}, {text} {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
                            );
                            if (Game1_random.NextBool())
                            {
                                // breakableContainer.showNextIndex.Value = true;
                            }
                            Controller.Console.Warn(
                                $"\test: adding barrel ({stonesLeftOnThisLevel}) {vector}, {text} {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
                            );
                            Objects.Add(vector, text);
                        }

                        break;
                    }
                }
            }

            bool flag2 = false;
            if (mineLevel % 10 != 0)
            {
                Layer layer2 = map.RequireLayer("Back");
                for (int j = 0; j < layer2.LayerWidth; j++)
                {
                    for (int k = 0; k < layer2.LayerHeight; k++)
                    {
                        if (isTileClearForMineObjects(j, k))
                        {
                            Controller.Console.Error(
                                $"\t{j},{k} {Game1_random.get_Index():D4} {mineRandom.get_Index():D4} {mineRandom.Copy().NextDouble()} {stoneChance}"
                            );
                            if (mineRandom.NextDouble() <= stoneChance)
                            {
                                Vector2 vector2 = new Vector2(j, k);
                                if (base.Objects.ContainsKey(vector2))
                                {
                                    continue;
                                }

                                string object2 = createLitterObject(
                                    0.001,
                                    5E-05,
                                    gemStoneChance,
                                    vector2,
                                    Game1_random
                                );
                                if (object2 != null)
                                {
                                    bool test = CanItemBePlacedHere(
                                        vector2,
                                        itemIsPassable: false,
                                        CollisionMask.All,
                                        CollisionMask.None
                                    );
                                    Controller.Console.Warn(
                                        $"\test: adding stone ({stonesLeftOnThisLevel}) {vector2}, {object2} {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}\t{test}"
                                    );
                                    base.Objects.Add(vector2, object2);
                                    if (object2.Contains("Stone"))
                                    {
                                        stonesLeftOnThisLevel++;
                                    }
                                }
                            }
                            else if (
                                mineRandom.NextDouble() <= monsterChance
                                && getDistanceFromStart(j, k) > 5f
                            )
                            {
                                SMonster monster = null;
                                if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0)
                                {
                                    int[] calicoStatueInvasionIds =
                                        DesertFestival.CalicoStatueInvasionIds;
                                    foreach (int num5 in calicoStatueInvasionIds)
                                    {
                                        if (
                                            !Game1.player.team.calicoStatueEffects.TryGetValue(
                                                num5,
                                                out var value2
                                            )
                                        )
                                        {
                                            continue;
                                        }

                                        for (int m = 0; m < value2; m++)
                                        {
                                            if (mineRandom.NextBool(0.15))
                                            {
                                                Vector2 position = new Vector2(j, k) * 64f;
                                                switch (num5)
                                                {
                                                    case 3:
                                                        monster = new SBat(
                                                            position,
                                                            mineLevel,
                                                            Game1_random
                                                        );
                                                        break;
                                                    case 0:
                                                        monster = new SGhost(
                                                            position,
                                                            "Carbon Ghost",
                                                            Game1_random
                                                        );
                                                        break;
                                                    case 1:
                                                        monster = new SSerpent(
                                                            position,
                                                            Game1_random
                                                        );
                                                        break;
                                                    case 2:
                                                        monster = (
                                                            (!(mineRandom.NextDouble() < 0.33))
                                                                ? (
                                                                    (SMonster)
                                                                        new SSkeleton(
                                                                            position,
                                                                            mineRandom.NextBool(),
                                                                            Game1_random
                                                                        )
                                                                )
                                                                : (
                                                                    (SMonster)
                                                                        new SBat(
                                                                            position,
                                                                            77377,
                                                                            Game1_random
                                                                        )
                                                                )
                                                        );
                                                        monster.BuffForAdditionalDifficulty(
                                                            1,
                                                            Game1_random
                                                        );
                                                        break;
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }

                                if (monster == null)
                                {
                                    Controller.Console.Warn(
                                        $"\test: b:getMonsterForThisLevel({j},{k}): {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
                                    );
                                    monster = BuffMonsterIfNecessary(
                                        getMonsterForThisLevel(mineLevel, j, k, Game1_random),
                                        Game1_random
                                    );
                                    Controller.Console.Warn(
                                        $"\test: a:getMonsterForThisLevel({j},{k}): {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
                                    );
                                }

                                if (!(monster is SGreenSlime greenSlime))
                                {
                                    if (!(monster is SLeaper))
                                    {
                                        if (!(monster is SGrub))
                                        {
                                            if (monster is SDustSpirit)
                                            {
                                                if (mineRandom.NextDouble() < 0.6)
                                                {
                                                    tryToAddMonster(
                                                        BuffMonsterIfNecessary(
                                                            new SDustSpirit(
                                                                Vector2.Zero,
                                                                Game1_random
                                                            ),
                                                            Game1_random
                                                        ),
                                                        j - 1,
                                                        k
                                                    );
                                                }

                                                if (mineRandom.NextDouble() < 0.6)
                                                {
                                                    tryToAddMonster(
                                                        BuffMonsterIfNecessary(
                                                            new SDustSpirit(
                                                                Vector2.Zero,
                                                                Game1_random
                                                            ),
                                                            Game1_random
                                                        ),
                                                        j + 1,
                                                        k
                                                    );
                                                }

                                                if (mineRandom.NextDouble() < 0.6)
                                                {
                                                    tryToAddMonster(
                                                        BuffMonsterIfNecessary(
                                                            new SDustSpirit(
                                                                Vector2.Zero,
                                                                Game1_random
                                                            ),
                                                            Game1_random
                                                        ),
                                                        j,
                                                        k - 1
                                                    );
                                                }

                                                if (mineRandom.NextDouble() < 0.6)
                                                {
                                                    tryToAddMonster(
                                                        BuffMonsterIfNecessary(
                                                            new SDustSpirit(
                                                                Vector2.Zero,
                                                                Game1_random
                                                            ),
                                                            Game1_random
                                                        ),
                                                        j,
                                                        k + 1
                                                    );
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (mineRandom.NextDouble() < 0.4)
                                            {
                                                tryToAddMonster(
                                                    BuffMonsterIfNecessary(
                                                        new SGrub(Vector2.Zero, Game1_random),
                                                        Game1_random
                                                    ),
                                                    j - 1,
                                                    k
                                                );
                                            }

                                            if (mineRandom.NextDouble() < 0.4)
                                            {
                                                tryToAddMonster(
                                                    BuffMonsterIfNecessary(
                                                        new SGrub(Vector2.Zero, Game1_random),
                                                        Game1_random
                                                    ),
                                                    j + 1,
                                                    k
                                                );
                                            }

                                            if (mineRandom.NextDouble() < 0.4)
                                            {
                                                tryToAddMonster(
                                                    BuffMonsterIfNecessary(
                                                        new SGrub(Vector2.Zero, Game1_random),
                                                        Game1_random
                                                    ),
                                                    j,
                                                    k - 1
                                                );
                                            }

                                            if (mineRandom.NextDouble() < 0.4)
                                            {
                                                tryToAddMonster(
                                                    BuffMonsterIfNecessary(
                                                        new SGrub(Vector2.Zero, Game1_random),
                                                        Game1_random
                                                    ),
                                                    j,
                                                    k + 1
                                                );
                                            }
                                        }
                                    }
                                    else
                                    {
                                        float num6 = (float)(GetAdditionalDifficulty() + 1) * 0.3f;
                                        if (mineRandom.NextDouble() < (double)num6)
                                        {
                                            tryToAddMonster(
                                                BuffMonsterIfNecessary(
                                                    new SLeaper(Vector2.Zero, Game1_random),
                                                    Game1_random
                                                ),
                                                j - 1,
                                                k
                                            );
                                        }

                                        if (mineRandom.NextDouble() < (double)num6)
                                        {
                                            tryToAddMonster(
                                                BuffMonsterIfNecessary(
                                                    new SLeaper(Vector2.Zero, Game1_random),
                                                    Game1_random
                                                ),
                                                j + 1,
                                                k
                                            );
                                        }

                                        if (mineRandom.NextDouble() < (double)num6)
                                        {
                                            tryToAddMonster(
                                                BuffMonsterIfNecessary(
                                                    new SLeaper(Vector2.Zero, Game1_random),
                                                    Game1_random
                                                ),
                                                j,
                                                k - 1
                                            );
                                        }

                                        if (mineRandom.NextDouble() < (double)num6)
                                        {
                                            tryToAddMonster(
                                                BuffMonsterIfNecessary(
                                                    new SLeaper(Vector2.Zero, Game1_random),
                                                    Game1_random
                                                ),
                                                j,
                                                k + 1
                                            );
                                        }
                                    }
                                }
                                else
                                {
                                    if (
                                        !flag2
                                        && Game1_random.NextDouble()
                                            <= Math.Max(0.01, 0.012 + Game1.player.DailyLuck / 10.0)
                                        && Game1.player.team.SpecialOrderActive("Wizard2")
                                    )
                                    {
                                        greenSlime.makePrismatic();
                                        flag2 = true;
                                    }

                                    if (
                                        GetAdditionalDifficulty() > 0
                                        && mineRandom.NextDouble()
                                            < (double)
                                                Math.Min(
                                                    (float)GetAdditionalDifficulty() * 0.1f,
                                                    0.5f
                                                )
                                    )
                                    {
                                        if (mineRandom.NextDouble() < 0.009999999776482582)
                                        {
                                            // greenSlime.stackedSlimes.Value = 4;
                                        }
                                        else
                                        {
                                            // greenSlime.stackedSlimes.Value = 2;
                                        }
                                    }
                                }

                                if (mineRandom.NextDouble() < 0.00175)
                                {
                                    // monster.hasSpecialItem.Value = true;
                                }

                                if (
                                    monster.GetBoundingBox().Width <= 64
                                    || isTileClearForMineObjects(j + 1, k)
                                )
                                {
                                    Characters.Add(monster);
                                }
                            }
                            else if (mineRandom.NextDouble() <= itemChance)
                            {
                                Vector2 vector3 = new Vector2(j, k);
                                base.Objects.Add(
                                    vector3,
                                    getRandomItemForThisLevel(mineLevel, vector3, Game1_random)
                                );
                            }
                            else if (
                                mineRandom.NextDouble() <= 0.005
                                && GetAdditionalDifficulty() <= 0
                            )
                            {
                                if (
                                    !isTileClearForMineObjects(j + 1, k)
                                    || !isTileClearForMineObjects(j, k + 1)
                                    || !isTileClearForMineObjects(j + 1, k + 1)
                                )
                                {
                                    continue;
                                }

                                // resource clumps can't be added
                            }
                            else if (GetAdditionalDifficulty() > 0) { }
                        }
                        // else if (
                        //     isContainerPlatform(j, k)
                        //     && CanItemBePlacedHere(new Vector2(j, k))
                        //     && mineRandom.NextDouble() < 0.4
                        //     && (flag || canAdd(0, num))
                        // )
                        // {
                        //     Vector2 vector4 = new Vector2(j, k);
                        //     objects.Add(
                        //         vector4,
                        //         BreakableContainer.GetBarrelForMines(vector4, this)
                        //     );
                        //     num++;
                        //     if (flag)
                        //     {
                        //         updateMineLevelData(0);
                        //     }
                        // }
                        else
                        {
                            if (
                                !(mineRandom.NextDouble() <= monsterChance)
                                || !CanSpawnCharacterHere(new Vector2(j, k))
                                || !isTileOnClearAndSolidGround(j, k)
                                || !(getDistanceFromStart(j, k) > 5f)
                            )
                            {
                                continue;
                            }
                            Controller.Console.Warn(
                                $"\test: b:getMonsterForThisLevel2({j},{k}): {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
                            );
                            SMonster monster2 = BuffMonsterIfNecessary(
                                getMonsterForThisLevel(mineLevel, j, k, Game1_random),
                                Game1_random
                            );
                            Controller.Console.Warn(
                                $"\test: a:getMonsterForThisLevel2({j},{k}): {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
                            );
                            if (
                                monster2.GetBoundingBox().Width <= 64
                                || isTileClearForMineObjects(j + 1, k)
                            )
                            {
                                if (mineRandom.NextDouble() < 0.01)
                                {
                                    // monster2.hasSpecialItem.Value = true;
                                }

                                Characters.Add(monster2);
                            }
                        }
                    }
                }

                if (stonesLeftOnThisLevel > 35)
                {
                    int num7 = stonesLeftOnThisLevel / 35;
                    for (int n = 0; n < num7; n++)
                    {
                        if (
                            !Utility.TryGetRandom(
                                Objects,
                                out var key,
                                out var value3,
                                random: Game1_random
                            )
                            || value3 != "Stone"
                        )
                        {
                            continue;
                        }

                        int num8 = mineRandom.Next(3, 8);
                        bool flag3 = mineRandom.NextDouble() < 0.1;
                        for (
                            int num9 = (int)key.X - num8 / 2;
                            (float)num9 < key.X + (float)(num8 / 2);
                            num9++
                        )
                        {
                            for (
                                int num10 = (int)key.Y - num8 / 2;
                                (float)num10 < key.Y + (float)(num8 / 2);
                                num10++
                            )
                            {
                                Vector2 key2 = new Vector2(num9, num10);
                                if (!Objects.TryGetValue(key2, out var value4) || value4 != "Stone")
                                {
                                    continue;
                                }

                                Objects.Remove(key2);
                                stonesLeftOnThisLevel--;
                                if (
                                    getDistanceFromStart(num9, num10) > 5f
                                    && flag3
                                    && mineRandom.NextDouble() < 0.12
                                )
                                {
                                    SMonster monster3 = BuffMonsterIfNecessary(
                                        getMonsterForThisLevel(
                                            mineLevel,
                                            num9,
                                            num10,
                                            Game1_random
                                        ),
                                        Game1_random
                                    );
                                    if (
                                        monster3.GetBoundingBox().Width <= 64
                                        || isTileClearForMineObjects(num9 + 1, num10)
                                    )
                                    {
                                        Characters.Add(monster3);
                                    }
                                }
                            }
                        }
                    }
                }

                // tryToAddAreaUniques();
                if (
                    mineRandom.NextDouble() < 0.95
                    && mineLevel > 1
                    && mineLevel % 5 != 0
                    && shouldCreateLadderOnThisLevel()
                )
                {
                    Vector2 v = new Vector2(
                        mineRandom.Next(layer2.LayerWidth),
                        mineRandom.Next(layer2.LayerHeight)
                    );
                    if (isTileClearForMineObjects(v))
                    {
                        // createLadderDown((int)v.X, (int)v.Y);
                    }
                }
            }
        }

        public StaticTile setMapTile(
            int tileX,
            int tileY,
            int index,
            string layer,
            string tileSheetId,
            string action = null,
            bool copyProperties = true
        )
        {
            Layer layer2 = map.RequireLayer(layer);
            Tile tile = layer2.Tiles[tileX, tileY];
            StaticTile staticTile = tile as StaticTile;
            if (staticTile != null && staticTile.TileSheet.Id == tileSheetId)
            {
                staticTile.TileIndex = index;
            }
            else
            {
                staticTile = (StaticTile)(
                    layer2.Tiles[tileX, tileY] = new StaticTile(
                        layer2,
                        map.RequireTileSheet(tileSheetId),
                        BlendMode.Alpha,
                        index
                    )
                );
                if (copyProperties && tile != null)
                {
                    foreach (KeyValuePair<string, PropertyValue> property in tile.Properties)
                    {
                        staticTile.Properties[property.Key] = property.Value;
                    }
                }
            }

            if (action != null && layer == "Buildings")
            {
                staticTile.Properties["Action"] = action;
            }

            return staticTile;
        }

        public bool tryToAddMonster(SMonster m, int tileX, int tileY)
        {
            if (
                isTileClearForMineObjects(tileX, tileY)
                && !IsTileOccupiedBy(new Vector2(tileX, tileY))
            )
            {
                m.setTilePosition(tileX, tileY);
                Characters.Add(m);
                return true;
            }

            return false;
        }

        public SMonster BuffMonsterIfNecessary(SMonster monster, Random Game1_random)
        {
            if (monster != null && monster.GetBaseDifficultyLevel() < GetAdditionalDifficulty())
            {
                monster.BuffForAdditionalDifficulty(
                    GetAdditionalDifficulty() - monster.GetBaseDifficultyLevel(),
                    Game1_random
                );
                if (monster is SGreenSlime greenSlime)
                {
                    Game1_random.Next(120, 180);
                }

                // setMonsterTextureToDangerousVersion(monster);
            }

            return monster;
        }

        public string getRandomItemForThisLevel(int level, Vector2 tile, Random Game1_random)
        {
            string itemId = "80";
            if (mineRandom.NextDouble() < 0.05 && level > 80)
            {
                itemId = "422";
            }
            else if (mineRandom.NextDouble() < 0.1 && level > 20)
            {
                itemId = "420";
            }
            else if (mineRandom.NextDouble() < 0.25 || GetAdditionalDifficulty() > 0)
            {
                itemId = (
                    (mineRandom.NextDouble() < 0.3)
                        ? "86"
                        : ((mineRandom.NextDouble() < 0.3) ? "84" : "82")
                );
            }
            else
            {
                itemId = "80";
            }
            Game1_random.NextBool(); // object ctor flipped
            return itemId;
        }

        public bool shouldCreateLadderOnThisLevel()
        {
            if (mineLevel != 77377)
            {
                return mineLevel != 120;
            }

            return false;
        }

        private void adjustLevelChances(
            ref double stoneChance,
            ref double monsterChance,
            ref double itemChance,
            ref double gemStoneChance
        )
        {
            monsterChance += 0.02 * (double)GetAdditionalDifficulty();
            gemStoneChance /= 2.0;
            if (Utility.GetDayOfPassiveFestival("DesertFestival") <= 0)
            {
                return;
            }
            double num2 = 1.0;
            int[] calicoStatueInvasionIds = DesertFestival.CalicoStatueInvasionIds;
            foreach (int key in calicoStatueInvasionIds)
            {
                if (Game1.player.team.calicoStatueEffects.TryGetValue(key, out var value))
                {
                    monsterChance += (double)value * 0.01;
                }
            }

            if (Game1.player.team.calicoStatueEffects.TryGetValue(7, out var value2))
            {
                num2 += (double)value2 * 0.2;
            }

            monsterChance *= num2;
        }

        public int GetAdditionalDifficulty()
        {
            if (mineLevel == 77377)
            {
                return 0;
            }

            if (mineLevel > 120)
            {
                return Game1.netWorldState.Value.SkullCavesDifficulty;
            }

            return Game1.netWorldState.Value.MinesDifficulty;
        }

        public SMonster getMonsterForThisLevel(int level, int xTile, int yTile, Random Game1_random)
        {
            Controller.Console.Warn(
                $"\test: b:getMonsterForThisLevel({xTile},{yTile}): {Game1_random.get_Index():D4} {mineRandom.get_Index():D4}"
            );
            Vector2 vector = new Vector2(xTile, yTile) * 64f;
            float distanceFromStart = getDistanceFromStart(xTile, yTile);

            {
                if (loadedDarkArea)
                {
                    if (mineRandom.NextDouble() < 0.18 && distanceFromStart > 8f)
                    {
                        return new SGhost(vector, "Carbon Ghost", Game1_random);
                    }

                    SMummy mummy = new SMummy(vector, Game1_random);
                    // if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && Game1.player.team.calicoStatueEffects.ContainsKey(9))
                    // {
                    //     mummy.BuffForAdditionalDifficulty(2);
                    //     mummy.speed *= 2;
                    // }

                    return mummy;
                }
                if (mineLevel % 20 == 0 && distanceFromStart > 10f)
                {
                    return new SBat(vector, mineLevel, Game1_random);
                }

                if (mineLevel % 16 == 0)
                {
                    if (
                        Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                        && Game1.player.team.calicoStatueEffects.ContainsKey(4)
                    )
                    {
                        return new SBug(vector, mineRandom.Next(4), "Assassin Bug", Game1_random);
                    }

                    return new SBug(vector, mineRandom.Next(4), 121, Game1_random);
                }

                if (mineRandom.NextDouble() < 0.33 && distanceFromStart > 10f)
                {
                    if (GetAdditionalDifficulty() <= 0)
                    {
                        return new SSerpent(vector, Game1_random);
                    }

                    return new SSerpent(vector, "Royal Serpent", Game1_random);
                }

                if (mineRandom.NextDouble() < 0.33 && distanceFromStart > 10f && mineLevel >= 171)
                {
                    return new SBat(vector, mineLevel, Game1_random);
                }

                if (mineLevel >= 126 && distanceFromStart > 10f && mineRandom.NextDouble() < 0.04)
                {
                    return new SDinoMonster(vector, Game1_random);
                }

                if (mineRandom.NextDouble() < 0.33)
                {
                    if (
                        Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                        && Game1.player.team.calicoStatueEffects.ContainsKey(4)
                    )
                    {
                        return new SBug(vector, mineRandom.Next(4), "Assassin Bug", Game1_random);
                    }

                    return new SBug(vector, mineRandom.Next(4), 121, Game1_random);
                }

                if (mineRandom.NextDouble() < 0.25)
                {
                    return new SGreenSlime(vector, level, Game1_random);
                }

                if (mineLevel >= 146 && mineRandom.NextDouble() < 0.25)
                {
                    return new SRockCrab(vector, "Iridium Crab", Game1_random);
                }

                if (
                    GetAdditionalDifficulty() > 0
                    && mineRandom.NextDouble() < 0.2
                    && distanceFromStart > 8f
                    && hasTileAt(xTile, yTile, "Back")
                    && !hasTileAt(xTile, yTile, "Front")
                )
                {
                    return new SSquidKid(vector, Game1_random);
                }

                return new SBigSlime(vector, 121, Game1_random);
            }
        }

        private string createLitterObject(
            double chanceForPurpleStone,
            double chanceForMysticStone,
            double gemStoneChance,
            Vector2 tile,
            Random Game1_random
        )
        {
            Color color = Color.White;
            if (
                GetAdditionalDifficulty() > 0
                && mineLevel % 5 != 0
                && mineRandom.NextDouble()
                    < (double)GetAdditionalDifficulty() * 0.001
                        + (double)((float)mineLevel / 100000f)
                        + Game1.player.DailyLuck / 13.0
                        + Game1.player.LuckLevel * 0.0001500000071246177
            )
            {
                // object ctor flipped
                Game1_random.NextBool();
                return "Stone";
            }

            int num = (
                mineRandom.NextBool()
                    ? ((!mineRandom.NextBool()) ? 32 : 38)
                    : ((!mineRandom.NextBool()) ? 42 : 40)
            );
            int num5 = mineLevel - 120;
            double num6 = 0.02 + (double)num5 * 0.0005;
            if (mineLevel >= 130)
            {
                num6 += 0.01 * (double)((float)(Math.Min(100, num5) - 10) / 10f);
            }

            double num7 = 0.0;
            if (mineLevel >= 130)
            {
                num7 += 0.001 * (double)((float)(num5 - 10) / 10f);
            }

            num7 = Math.Min(num7, 0.004);
            if (num5 > 100)
            {
                num7 += (double)num5 / 1000000.0;
            }

            if (!isTreasureRoom && mineRandom.NextDouble() < num6)
            {
                double num8 = (double)Math.Min(100, num5) * (0.0003 + num7);
                double num9 = 0.01 + (double)(mineLevel - Math.Min(150, num5)) * 0.0005;
                double num10 = Math.Min(
                    0.5,
                    0.1 + (double)(mineLevel - Math.Min(200, num5)) * 0.005
                );
                if (
                    Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                    && mineRandom.NextBool(
                        0.13
                            + (double)(
                                (float)(Game1.player.team.calicoEggSkullCavernRating.Value * 5)
                                / 1000f
                            )
                    )
                )
                {
                    string text = "CalicoEggStone_" + mineRandom.Next(3);
                    // object ctor flipped
                    Game1_random.NextBool();
                    return text;
                }

                if (mineRandom.NextDouble() < num8)
                {
                    Game1_random.NextBool(); // object ctor flipped
                    return "IridiumStone";
                }

                if (mineRandom.NextDouble() < num9)
                {
                    Game1_random.NextBool(); // object ctor flipped
                    return "GoldStone";
                }

                if (mineRandom.NextDouble() < num10)
                {
                    Game1_random.NextBool(); // object ctor flipped
                    return "IronStone";
                }

                Game1_random.NextBool(); // object ctor flipped
                return "CopperStone";
            }

            double num11 = Game1.player.DailyLuck;
            double num12 = Game1.player.GetSkillLevel(3);
            double num13 = num11 + num12 * 0.005;
            if (
                mineLevel > 50
                && mineRandom.NextDouble()
                    < 0.00025 + (double)mineLevel / 120000.0 + 0.0005 * num13 / 2.0
            )
            {
                num = 2;
            }
            else if (
                gemStoneChance != 0.0
                && mineRandom.NextDouble()
                    < gemStoneChance + gemStoneChance * num13 + (double)mineLevel / 24000.0
            )
            {
                string text = getRandomGemRichStoneForThisLevel(mineLevel);
                // object ctor flipped
                Game1_random.NextBool();
                return text;
            }

            if (
                mineRandom.NextDouble()
                < chanceForPurpleStone / 2.0
                    + chanceForPurpleStone * num12 * 0.008
                    + chanceForPurpleStone * (num11 / 2.0)
            )
            {
                num = 44;
            }

            if (
                mineLevel > 100
                && mineRandom.NextDouble()
                    < chanceForMysticStone
                        + chanceForMysticStone * num12 * 0.008
                        + chanceForMysticStone * (num11 / 2.0)
            )
            {
                num = 46;
            }

            num += num % 2;
            if (mineRandom.NextDouble() < 0.1)
            {
                // object ctor flipped
                mineRandom.Choose("668", "670");
                mineRandom.NextBool();
                Game1_random.NextBool();
                return "Stone";
                // return new Object(mineRandom.Choose("668", "670"), 1)
                // {
                //     MinutesUntilReady = 2,
                //     Flipped = mineRandom.NextBool();
                // };
            }
            // object ctor flipped
            Game1_random.NextBool();
            switch (num)
            {
                case 32:
                    return "Stone";
                case 38:
                    return "Stone";
                case 42:
                    return "Stone";
                case 40:
                    return "Stone";
                case 44:
                    return "GemStone";
                case 46:
                    return "MysticStone";
                default:
                    return "Stone";
            }
        }

        public string getRandomGemRichStoneForThisLevel(int level)
        {
            int num = mineRandom.Next(59, 70);
            num += num % 2;
            if (Game1.player.timesReachedMineBottom == 0)
            {
                if (level < 40 && num != 66 && num != 68)
                {
                    num = mineRandom.Choose(66, 68);
                }
                else if (level < 80 && (num == 64 || num == 60))
                {
                    num = mineRandom.Choose(66, 70, 68, 62);
                }
            }

            return num switch
            {
                66 => "8",
                68 => "10",
                60 => "12",
                70 => "6",
                64 => "4",
                62 => "14",
                _ => 40.ToString(),
            };
        }

        public void addLevelChests(Random Game1_random)
        {
            ChestItems.Clear();
            ChestItems.Add(getTreasureRoomItem(Game1_random));
            if (mineLevel == 320 || mineLevel == 420)
            {
                ChestItems.Add(getTreasureRoomItem(Game1_random));
            }
            if (mineLevel == 420)
            {
                ChestItems.Add(getTreasureRoomItem(Game1_random));
            }
        }

        public static SItem getTreasureRoomItem(Random Game1_random)
        {
            if (
                Game1.player.stats.Get(StatKeys.Mastery(0)) != 0
                && Game1_random.NextDouble() < 0.02
            )
            {
                // object ctor flipped
                Game1_random.NextBool();
                return new SItem("(O)GoldenAnimalCracker", 1);
            }

            if (Trinket.CanSpawnTrinket(Game1.player) && Game1_random.NextDouble() < 0.045)
            {
                return new SItem(RandomTrinket(Game1_random), 1);
            }
            int stack = 1;
            string text;
            switch (Game1_random.Next(26))
            {
                case 0:
                    stack = 5;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)288", stack);
                // return ItemRegistry.Create("(O)288", 5);
                case 1:
                    stack = 10;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)287", stack);
                // return ItemRegistry.Create("(O)287", 10);
                case 2:
                    if (
                        !Game1.MasterPlayer.hasOrWillReceiveMail("volcanoShortcutUnlocked")
                        || !(Game1_random.NextDouble() < 0.66)
                    )
                    {
                        stack = 5;
                        Game1_random.NextBool(); // object ctor flipped
                        return new SItem("(O)275", stack);
                        // return ItemRegistry.Create("(O)275", 5);
                    }
                    stack = 5 + Game1_random.Next(1, 4) * 5;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)848", stack);
                // return ItemRegistry.Create("(O)848", 5 + Game1.random.Next(1, 4) * 5);
                case 3:
                    stack = Game1_random.Next(2, 5);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)773", stack);
                // return ItemRegistry.Create("(O)773", Game1.random.Next(2, 5));
                case 4:
                    stack = 5 + ((Game1_random.NextDouble() < 0.25) ? 5 : 0);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)749", stack);
                // return ItemRegistry.Create(
                //     "(O)749",
                //     5 + ((Game1.random.NextDouble() < 0.25) ? 5 : 0)
                // );
                case 5:
                    stack = 5;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)688", stack);
                // return ItemRegistry.Create("(O)688", 5);
                case 6:
                    stack = Game1_random.Next(1, 4);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)681", stack);
                // return ItemRegistry.Create("(O)681", Game1.random.Next(1, 4));
                case 7:
                    text = "(O)" + Game1_random.Next(628, 634);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem(text, stack);
                // return ItemRegistry.Create("(O)" + Game1.random.Next(628, 634));
                case 8:
                    stack = Game1_random.Next(1, 3);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)645", stack);
                // return ItemRegistry.Create("(O)645", Game1.random.Next(1, 3));
                case 9:
                    stack = 4;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)621", stack);
                // return ItemRegistry.Create("(O)621", 4);
                case 10:
                    if (!(Game1_random.NextDouble() < 0.33))
                    {
                        text = "(O)" + Game1_random.Next(472, 499);
                        stack = Game1_random.Next(1, 5) * 5;
                        Game1_random.NextBool(); // object ctor flipped
                        return new SItem(text, stack);
                        // return ItemRegistry.Create(
                        //     "(O)" + Game1.random.Next(472, 499),
                        //     Game1.random.Next(1, 5) * 5
                        // );
                    }
                    stack = 15;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)802", stack);
                // return ItemRegistry.Create("(O)802", 15);
                case 11:
                    stack = 15;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)286", stack);
                // return ItemRegistry.Create("(O)286", 15);
                case 12:
                    if (!(Game1_random.NextDouble() < 0.5))
                    {
                        Game1_random.NextBool(); // object ctor flipped
                        return new SItem("(O)437", stack);
                        // return ItemRegistry.Create("(O)437");
                    }
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)265", stack);
                // return ItemRegistry.Create("(O)265");
                case 13:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)439", stack);
                // return ItemRegistry.Create("(O)439");
                case 14:
                    if (!(Game1_random.NextDouble() < 0.33))
                    {
                        stack = Game1_random.Next(2, 5);
                        Game1_random.NextBool(); // object ctor flipped
                        return new SItem("(O)349", stack);
                        // return ItemRegistry.Create("(O)349", Game1.random.Next(2, 5));
                    }
                    text = "(O)" + ((Game1_random.NextDouble() < 0.5) ? 226 : 732);
                    stack = 5;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem(text, stack);
                // return ItemRegistry.Create(
                //     "(O)" + ((Game1.random.NextDouble() < 0.5) ? 226 : 732),
                //     5
                // );
                case 15:
                    stack = Game1_random.Next(2, 4);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)337", stack);
                // return ItemRegistry.Create("(O)337", Game1.random.Next(2, 4));
                case 16:
                    if (!(Game1_random.NextDouble() < 0.33))
                    {
                        text = "(O)" + Game1_random.Next(235, 245);
                        stack = 5;
                        Game1_random.NextBool(); // object ctor flipped
                        return new SItem(text, stack);
                        // return ItemRegistry.Create("(O)" + Game1.random.Next(235, 245), 5);
                    }
                    text = "(O)" + ((Game1_random.NextDouble() < 0.5) ? 226 : 732);
                    stack = 5;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem(text, stack);
                // return ItemRegistry.Create(
                //     "(O)" + ((Game1.random.NextDouble() < 0.5) ? 226 : 732),
                //     5
                // );
                case 17:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)74", stack);
                // return ItemRegistry.Create("(O)74");
                case 18:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(BC)21", stack);
                // return ItemRegistry.Create("(BC)21");
                case 19:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(BC)25", stack);
                // return ItemRegistry.Create("(BC)25");
                case 20:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(BC)165", stack);
                // return ItemRegistry.Create("(BC)165");
                case 21:
                    text = (Game1_random.NextDouble() < 0.5) ? "(H)38" : "(H)37";
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem(text, stack);
                // return ItemRegistry.Create(Game1.random.NextBool() ? "(H)38" : "(H)37");
                case 22:
                    if (Game1.player.mailReceived.Contains("sawQiPlane"))
                    {
                        text =
                            (Game1.player.stats.Get(StatKeys.Mastery(2)) != 0)
                                ? "(O)GoldenMysteryBox"
                                : "(O)MysteryBox";
                        stack = 5;
                        Game1_random.NextBool(); // object ctor flipped
                        return new SItem(text, stack);
                        // return ItemRegistry.Create(
                        //     (Game1.player.stats.Get(StatKeys.Mastery(2)) != 0)
                        //         ? "(O)GoldenMysteryBox"
                        //         : "(O)MysteryBox",
                        //     5
                        // );
                    }
                    stack = 5 + ((Game1_random.NextDouble() < 0.25) ? 5 : 0);
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)749", stack);
                // return ItemRegistry.Create(
                //     "(O)749",
                //     5 + ((Game1.random.NextDouble() < 0.25) ? 5 : 0)
                // );
                case 23:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(H)65", stack);
                // return ItemRegistry.Create("(H)65");
                case 24:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(BC)272", stack);
                // return ItemRegistry.Create("(BC)272");
                case 25:
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(H)83", stack);
                // return ItemRegistry.Create("(H)83");
                default:
                    stack = 5;
                    Game1_random.NextBool(); // object ctor flipped
                    return new SItem("(O)288", stack);
                    // return ItemRegistry.Create("(O)288", 5);
            }
        }


        public static string RandomTrinket(Random Game1_random)
        {
            Dictionary<string, TrinketData> data_sheet = DataLoader.Trinkets(Game1.content);
            string t = null;
            while (t == null)
            {
                int which = Game1_random.Next(data_sheet.Count);
                int i = 0;
                foreach (KeyValuePair<string, TrinketData> pair in data_sheet)
                {
                    if (which == i && pair.Value.DropsNaturally)
                    {
                        t = "(TR)" + pair.Key;
                        break;
                    }
                    i++;
                }
            }
            return t;
        }
    }
}
