using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Minigames;
using StardewValley.SpecialOrders;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Minigames
{
    public class SMineCart : IMinigame
    {
        public bool Simulating;
        public bool ShouldPressButton;
        public ulong StartFrame;
        public ulong CurrentFrame;
        public Random random;
        public bool shouldDraw = true;
        public bool shouldPlaySound = true;
        public List<bool> buttonPresses = new List<bool>();

        public enum GameStates
        {
            Title,
            Ingame,
            FruitsSummary,
            Map,
            Cutscene
        }

        public class LevelTransition
        {
            public int startLevel;

            public int destinationLevel;

            public Point startGridCoordinates;

            public string pathString = "";

            public Func<bool> shouldTakePath;

            public LevelTransition(
                int start_level,
                int destination_level,
                int start_grid_x,
                int start_grid_y,
                string path_string,
                Func<bool> should_take_path = null
            )
            {
                startLevel = start_level;
                destinationLevel = destination_level;
                startGridCoordinates = new Point(start_grid_x, start_grid_y);
                pathString = path_string;
                shouldTakePath = should_take_path;
            }
        }

        public enum CollectableFruits
        {
            Cherry,
            Orange,
            Grape,
            MAX
        }

        public enum ObstacleTypes
        {
            Normal,
            Air,
            Difficult
        }

        public class GeneratorRoll
        {
            public float chance;

            public BaseTrackGenerator generator;

            public Func<bool> additionalGenerationCondition;

            public BaseTrackGenerator forcedNextGenerator;

            public GeneratorRoll(
                float generator_chance,
                BaseTrackGenerator track_generator,
                Func<bool> additional_generation_condition = null,
                BaseTrackGenerator forced_next_generator = null
            )
            {
                chance = generator_chance;
                generator = track_generator;
                forcedNextGenerator = forced_next_generator;
                additionalGenerationCondition = additional_generation_condition;
            }

            public GeneratorRoll Clone(SMineCart game)
            {
                // Controller.Console.Alert("Cloning a generator roll");
                return new GeneratorRoll(
                    chance,
                    generator?.Clone(game),
                    additionalGenerationCondition,
                    forcedNextGenerator?.Clone(game)
                );
            }
        }

        public class MapJunimo : Entity
        {
            public enum MoveState
            {
                Idle,
                Moving,
                Finished
            }

            public int direction = 2;

            public string moveString = "";

            public float moveSpeed = 60f;

            public float pixelsToMove;

            public MoveState moveState;

            public float nextBump;

            public float bumpHeight;

            private bool isOnWater;

            public MapJunimo(SMineCart game)
                : base(game) { }

            public void StartMoving()
            {
                moveState = MoveState.Moving;
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                int desired_direction = direction;
                isOnWater = false;
                if (position.X > 194f && position.X < 251f && position.Y > 165f)
                {
                    isOnWater = true;
                    _game.minecartLoop.Pause();
                }
                if (moveString.Length > 0)
                {
                    if (moveString[0] == 'u')
                    {
                        desired_direction = 0;
                    }
                    else if (moveString[0] == 'd')
                    {
                        desired_direction = 2;
                    }
                    else if (moveString[0] == 'l')
                    {
                        desired_direction = 3;
                    }
                    else if (moveString[0] == 'r')
                    {
                        desired_direction = 1;
                    }
                }
                if (moveState == MoveState.Idle && !_game.minecartLoop.IsPaused)
                {
                    _game.minecartLoop.Pause();
                }
                if (moveState == MoveState.Moving)
                {
                    nextBump -= time;
                    bumpHeight = Utility.MoveTowards(bumpHeight, 0f, time * 5f);
                    if (nextBump <= 0f)
                    {
                        nextBump = Utility.RandomFloat(0.1f, 0.3f, random);
                        bumpHeight = -2f;
                    }
                    if (!isOnWater && _game.minecartLoop.IsPaused)
                    {
                        _game.minecartLoop.Resume();
                    }
                    if (pixelsToMove <= 0f)
                    {
                        if (desired_direction != direction)
                        {
                            direction = desired_direction;
                            if (!isOnWater)
                            {
                                SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
                                _game.createSparkShower(position);
                            }
                            else
                            {
                                SMineCartGlobal.PlaySound(shouldPlaySound, "waterSlosh");
                            }
                        }
                        if (moveString.Length > 0)
                        {
                            pixelsToMove = 16f;
                            moveString = moveString.Substring(1);
                        }
                        else
                        {
                            moveState = MoveState.Finished;
                            direction = 2;
                            if (position.X < 368f)
                            {
                                if (!isOnWater)
                                {
                                    SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
                                    _game.createSparkShower(position);
                                }
                                else
                                {
                                    SMineCartGlobal.PlaySound(shouldPlaySound, "waterSlosh");
                                }
                            }
                        }
                    }
                    if (pixelsToMove > 0f)
                    {
                        float pixels_to_move_now = Math.Min(pixelsToMove, moveSpeed * time);
                        Vector2 direction_to_move = Vector2.Zero;
                        if (direction == 1)
                        {
                            direction_to_move.X = 1f;
                        }
                        else if (direction == 3)
                        {
                            direction_to_move.X = -1f;
                        }
                        if (direction == 0)
                        {
                            direction_to_move.Y = -1f;
                        }
                        if (direction == 2)
                        {
                            direction_to_move.Y = 1f;
                        }
                        position += direction_to_move * pixels_to_move_now;
                        pixelsToMove -= pixels_to_move_now;
                    }
                }
                else
                {
                    bumpHeight = -2f;
                }
                if (moveState == MoveState.Finished && !_game.minecartLoop.IsPaused)
                {
                    _game.minecartLoop.Pause();
                }
                base._Update(time, random, shouldPlaySound);
            }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                Rectangle source_rect = new Rectangle(400, 512, 16, 16);
                if (direction == 0)
                {
                    source_rect.Y = 544;
                }
                else if (direction == 2)
                {
                    source_rect.Y = 512;
                }
                else
                {
                    source_rect.Y = 528;
                    if (direction == 3)
                    {
                        effect = SpriteEffects.FlipHorizontally;
                    }
                }
                if (isOnWater)
                {
                    source_rect.Height -= 3;
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            base.drawnPosition
                                + new Vector2(0f, -1f)
                                + new Vector2(0f, 1f) * bumpHeight
                        ),
                        source_rect,
                        Color.White,
                        0f,
                        new Vector2(8f, 8f),
                        _game.GetPixelScale(),
                        effect,
                        0.45f
                    );
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            base.drawnPosition
                                + new Vector2(2f, 10f)
                                + new Vector2(0f, 1f) * bumpHeight
                        ),
                        new Rectangle(414, 624, 13, 5),
                        Color.White,
                        0f,
                        new Vector2(8f, 8f),
                        _game.GetPixelScale(),
                        effect,
                        0.44f
                    );
                }
                else
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            base.drawnPosition
                                + new Vector2(0f, -1f)
                                + new Vector2(0f, 1f) * bumpHeight
                        ),
                        source_rect,
                        Color.White,
                        0f,
                        new Vector2(8f, 8f),
                        _game.GetPixelScale(),
                        effect,
                        0.45f
                    );
                }
            }

            public override MapJunimo Clone(SMineCart game)
            {
                MapJunimo mapJunimo = new MapJunimo(game);
                mapJunimo.CloneOver(this);
                return mapJunimo;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is MapJunimo mapJunimo)
                {
                    mapJunimo.direction = direction;
                    mapJunimo.moveString = moveString;
                    mapJunimo.moveSpeed = moveSpeed;
                    mapJunimo.pixelsToMove = pixelsToMove;
                    mapJunimo.moveState = moveState;
                    mapJunimo.nextBump = nextBump;
                    mapJunimo.bumpHeight = bumpHeight;
                    mapJunimo.isOnWater = isOnWater;
                }
            }
        }

        public class LakeDecor
        {
            public Point _position;

            public int spriteIndex;

            protected SMineCart _game;

            public int _lastCycle = -1;

            public bool _bgDecor;

            private int _animationFrames = 1;

            public LakeDecor(SMineCart game)
            {
                _game = game;
            }

            public LakeDecor(
                SMineCart game,
                int theme = -1,
                bool bgDecor = false,
                int forceXPosition = -1,
                Random random = null
            )
            {
                _game = game;
                _position = new Point(
                    random.Next(0, _game.screenWidth),
                    random.Next(160, _game.screenHeight)
                );
                if (forceXPosition != -1)
                {
                    _position.X =
                        forceXPosition * (_game.screenWidth / 16)
                        + random.Next(0, _game.screenWidth / 16);
                }
                _bgDecor = bgDecor;
                spriteIndex = random.Next(2);
                switch (theme)
                {
                    case 2:
                        spriteIndex = 2;
                        break;
                    case 1:
                        spriteIndex += 3;
                        break;
                    case 5:
                        spriteIndex += 5;
                        break;
                    case 4:
                        spriteIndex = 14;
                        _animationFrames = 6;
                        break;
                    case 9:
                        spriteIndex += 7;
                        break;
                    case 6:
                        spriteIndex = 1;
                        break;
                }
                if (!bgDecor)
                {
                    return;
                }
                spriteIndex += 7;
                _position.Y = random.Next(0, _game.screenHeight / 3);
                if (theme == 2 && forceXPosition % 5 == 0)
                {
                    spriteIndex++;
                    _animationFrames = 4;
                    return;
                }
                switch (theme)
                {
                    case 3:
                        spriteIndex = 24;
                        _animationFrames = 4;
                        break;
                    case 6:
                        spriteIndex = 20;
                        _position.Y = random.Next(0, _game.screenHeight / 5);
                        _animationFrames = 4;
                        break;
                    case 9:
                        spriteIndex = 28;
                        _animationFrames = 4;
                        break;
                }
            }

            public void SpeedDraw(Random random)
            {
                float side_buffer_space = 32f;
                float y_position_in_lake =
                    (float)(_position.Y - 160) / (float)(_game.screenHeight - 160);
                float scroll_speed = Utility.Lerp(-0.4f, -0.75f, y_position_in_lake);
                int current_cycle = (int)
                    Math.Floor(
                        ((float)_position.X + _game.screenLeftBound * scroll_speed)
                            / ((float)_game.screenWidth + side_buffer_space * 2f)
                    );
                if (current_cycle != _lastCycle)
                {
                    _lastCycle = current_cycle;
                    if (spriteIndex < 2)
                    {
                        spriteIndex = random.Next(2);
                        if (_game.currentTheme == 6)
                        {
                            spriteIndex = 1;
                        }
                    }
                }
            }

            public void Draw(SpriteBatch b, Random random)
            {
                Vector2 draw_position = default(Vector2);
                float side_buffer_space = 32f;
                float y_position_in_lake =
                    (float)(_position.Y - 160) / (float)(_game.screenHeight - 160);
                float scroll_speed = Utility.Lerp(-0.4f, -0.75f, y_position_in_lake);
                int current_cycle = (int)
                    Math.Floor(
                        ((float)_position.X + _game.screenLeftBound * scroll_speed)
                            / ((float)_game.screenWidth + side_buffer_space * 2f)
                    );
                if (current_cycle != _lastCycle)
                {
                    _lastCycle = current_cycle;
                    if (spriteIndex < 2)
                    {
                        spriteIndex = random.Next(2);
                        if (_game.currentTheme == 6)
                        {
                            spriteIndex = 1;
                        }
                    }
                }
                float drawY = _position.Y;
                if (_bgDecor)
                {
                    scroll_speed = Utility.Lerp(
                        -0.15f,
                        -0.25f,
                        (float)_position.Y / (float)(_game.screenHeight / 3)
                    );
                    if (_game.currentTheme == 3)
                    {
                        drawY += (float)
                            (int)(
                                Math.Sin(
                                    Utility.Lerp(
                                        0f,
                                        (float)Math.PI * 2f,
                                        (float)(
                                            (
                                                _game.totalTimeMS
                                                + (double)(_position.X * 7)
                                                + (double)(_position.Y * 2)
                                            )
                                            / 2.0
                                            % 1000.0
                                        ) / 1000f
                                    )
                                ) * 3.0
                            );
                    }
                }
                draw_position.X =
                    (float)Mod(
                        (int)((float)_position.X + _game.screenLeftBound * scroll_speed),
                        (int)((float)_game.screenWidth + side_buffer_space * 2f)
                    ) - side_buffer_space;
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(new Vector2(draw_position.X, drawY)),
                    new Rectangle(
                        96
                            + spriteIndex % 14 * _game.tileSize
                            + (int)(
                                (_game.totalTimeMS + (double)(_position.X * 10))
                                % 1000.0
                                / (double)(1000 / _animationFrames)
                            )
                                % 14
                                * _game.tileSize,
                        848 + spriteIndex / 14 * _game.tileSize,
                        16,
                        16
                    ),
                    (spriteIndex == 0)
                        ? _game.midBGTint
                        : ((spriteIndex == 1) ? _game.lakeTint : Color.White),
                    0f,
                    Vector2.Zero,
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    _bgDecor ? 0.65f : (0.8f + y_position_in_lake * -0.001f)
                );
            }

            public LakeDecor Clone(SMineCart game)
            {
                return new LakeDecor(game)
                {
                    _position = _position,
                    spriteIndex = spriteIndex,
                    _lastCycle = _lastCycle,
                    _bgDecor = _bgDecor,
                    _animationFrames = _animationFrames
                };
            }
        }

        public class StraightAwayGenerator : BaseTrackGenerator
        {
            public int straightAwayLength = 10;

            public List<int> staggerPattern;

            public int minLength = 3;

            public int maxLength = 5;

            public float staggerChance = 0.25f;

            public int minimuimDistanceBetweenStaggers = 1;

            public int currentStaggerDistance;

            public bool generateCheckpoint = true;

            protected bool _generatedCheckpoint = true;

            public StraightAwayGenerator SetMinimumDistanceBetweenStaggers(int min)
            {
                minimuimDistanceBetweenStaggers = min;
                return this;
            }

            public StraightAwayGenerator SetLength(int min, int max)
            {
                minLength = min;
                maxLength = max;
                return this;
            }

            public StraightAwayGenerator SetCheckpoint(bool checkpoint)
            {
                generateCheckpoint = checkpoint;
                return this;
            }

            public StraightAwayGenerator SetStaggerChance(float chance)
            {
                staggerChance = chance;
                return this;
            }

            public StraightAwayGenerator SetStaggerValues(params int[] args)
            {
                staggerPattern = new List<int>();
                for (int i = 0; i < args.Length; i++)
                {
                    staggerPattern.Add(args[i]);
                }
                return this;
            }

            public StraightAwayGenerator SetStaggerValueRange(int min, int max)
            {
                staggerPattern = new List<int>();
                for (int i = min; i <= max; i++)
                {
                    staggerPattern.Add(i);
                }
                return this;
            }

            public StraightAwayGenerator(SMineCart game)
                : base(game) { }

            public override void Initialize(Random random)
            {
                straightAwayLength = random.Next(minLength, maxLength + 1);
                _generatedCheckpoint = false;
                if (straightAwayLength <= 3)
                {
                    _generatedCheckpoint = true;
                }
                base.Initialize(random);
            }

            protected override void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                if (_game.generatorPosition.X >= _game.distanceToTravel)
                {
                    return;
                }
                for (int i = 0; i < straightAwayLength; i++)
                {
                    if (_game.generatorPosition.X >= _game.distanceToTravel)
                    {
                        return;
                    }
                    int last_y = _game.generatorPosition.Y;
                    if (currentStaggerDistance <= 0)
                    {
                        if (random.NextDouble() < (double)staggerChance)
                        {
                            _game.generatorPosition.Y += random.ChooseFrom(staggerPattern);
                        }
                        currentStaggerDistance = minimuimDistanceBetweenStaggers;
                    }
                    else
                    {
                        currentStaggerDistance--;
                    }
                    if (!_game.IsTileInBounds(_game.generatorPosition.Y))
                    {
                        _game.generatorPosition.Y = last_y;
                        straightAwayLength = 0;
                        break;
                    }
                    _game.generatorPosition.Y = _game.KeepTileInBounds(_game.generatorPosition.Y);
                    Track.TrackType tile_type = Track.TrackType.Straight;
                    if (_game.generatorPosition.Y < last_y)
                    {
                        tile_type = Track.TrackType.UpSlope;
                    }
                    else if (_game.generatorPosition.Y > last_y)
                    {
                        tile_type = Track.TrackType.DownSlope;
                    }
                    if (tile_type == Track.TrackType.DownSlope && _game.currentTheme == 1)
                    {
                        tile_type = Track.TrackType.IceDownSlope;
                    }
                    if (tile_type == Track.TrackType.UpSlope && _game.currentTheme == 5)
                    {
                        tile_type = Track.TrackType.SlimeUpSlope;
                    }
                    AddPickupTrack(_game.generatorPosition.X, _game.generatorPosition.Y, tile_type);
                    _game.generatorPosition.X++;
                }
                if (
                    _generatedTracks != null
                    && _generatedTracks.Count > 0
                    && generateCheckpoint
                    && !_generatedCheckpoint
                )
                {
                    _generatedCheckpoint = true;
                    _generatedTracks.OrderBy((Track o) => o.position.X);
                    _game.AddCheckpoint(
                        (int)(_generatedTracks[0].position.X / (float)_game.tileSize)
                    );
                }
            }

            public override StraightAwayGenerator Clone(SMineCart game)
            {
                StraightAwayGenerator clone = new StraightAwayGenerator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(BaseTrackGenerator clone)
            {
                base.CloneOver(clone);
                if (clone is StraightAwayGenerator straightAwayGenerator)
                {
                    straightAwayGenerator.straightAwayLength = straightAwayLength;
                    straightAwayGenerator.staggerPattern = new List<int>(staggerPattern);
                    straightAwayGenerator.minLength = minLength;
                    straightAwayGenerator.maxLength = maxLength;
                    straightAwayGenerator.staggerChance = staggerChance;
                    straightAwayGenerator.minimuimDistanceBetweenStaggers =
                        minimuimDistanceBetweenStaggers;
                    straightAwayGenerator.currentStaggerDistance = currentStaggerDistance;
                    straightAwayGenerator.generateCheckpoint = generateCheckpoint;
                    straightAwayGenerator._generatedCheckpoint = _generatedCheckpoint;
                }
            }
        }

        public class SmallGapGenerator : BaseTrackGenerator
        {
            public int minLength = 3;

            public int maxLength = 5;

            public int minDepth = 5;

            public int maxDepth = 5;

            public SmallGapGenerator SetLength(int min, int max)
            {
                minLength = min;
                maxLength = max;
                return this;
            }

            public SmallGapGenerator SetDepth(int min, int max)
            {
                minDepth = min;
                maxDepth = max;
                return this;
            }

            public SmallGapGenerator(SMineCart game)
                : base(game) { }

            protected override void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                if (_game.generatorPosition.X >= _game.distanceToTravel)
                {
                    return;
                }
                int depth = random.Next(minDepth, maxDepth + 1);
                int length = random.Next(minLength, maxLength + 1);
                AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
                _game.generatorPosition.X++;
                _game.generatorPosition.Y += depth;
                for (int i = 0; i < length; i++)
                {
                    if (_game.generatorPosition.X >= _game.distanceToTravel)
                    {
                        _game.generatorPosition.Y -= depth;
                        return;
                    }
                    AddPickupTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
                    _game.generatorPosition.X++;
                }
                _game.generatorPosition.Y -= depth;
                if (_game.generatorPosition.X < _game.distanceToTravel)
                {
                    AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
                    _game.generatorPosition.X++;
                }
            }

            public override SmallGapGenerator Clone(SMineCart game)
            {
                SmallGapGenerator clone = new SmallGapGenerator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(BaseTrackGenerator clone)
            {
                base.CloneOver(clone);
                if (clone is SmallGapGenerator generator)
                {
                    generator.minLength = minLength;
                    generator.maxLength = maxLength;
                    generator.minDepth = minDepth;
                    generator.maxDepth = maxDepth;
                }
            }
        }

        public class RapidHopsGenerator : BaseTrackGenerator
        {
            public int minLength = 3;

            public int maxLength = 5;

            private int startY;

            public int yStep;

            public bool chaotic;

            public RapidHopsGenerator SetLength(int min, int max)
            {
                minLength = min;
                maxLength = max;
                return this;
            }

            public RapidHopsGenerator SetYStep(int yStep)
            {
                this.yStep = yStep;
                return this;
            }

            public RapidHopsGenerator SetChaotic(bool chaotic)
            {
                this.chaotic = chaotic;
                return this;
            }

            public RapidHopsGenerator(SMineCart game)
                : base(game) { }

            protected override void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                if (_game.generatorPosition.X >= _game.distanceToTravel)
                {
                    return;
                }
                if (startY == 0)
                {
                    startY = _game.generatorPosition.Y;
                }
                int length = random.Next(minLength, maxLength + 1);
                AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
                _game.generatorPosition.X++;
                _game.generatorPosition.Y += yStep;
                for (int i = 0; i < length; i++)
                {
                    if (
                        _game.generatorPosition.Y < 3
                        || _game.generatorPosition.Y > _game.screenHeight / _game.tileSize - 2
                    )
                    {
                        _game.generatorPosition.Y = _game.screenHeight / _game.tileSize - 2;
                        startY = _game.generatorPosition.Y;
                    }
                    if (_game.generatorPosition.X >= _game.distanceToTravel)
                    {
                        _game.generatorPosition.Y -= yStep;
                        return;
                    }
                    AddPickupTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
                    _game.generatorPosition.X += random.Next(2, 4);
                    if (random.NextDouble() < 0.33)
                    {
                        AddTrack(
                            _game.generatorPosition.X - 1,
                            Math.Min(
                                _game.screenHeight / _game.tileSize - 2,
                                _game.generatorPosition.Y + random.Next(5)
                            )
                        );
                    }
                    if (chaotic)
                    {
                        _game.generatorPosition.Y =
                            startY + random.Next(-Math.Abs(yStep), Math.Abs(yStep) + 1);
                    }
                    else
                    {
                        _game.generatorPosition.Y += yStep;
                    }
                }
                if (_game.generatorPosition.X < _game.distanceToTravel)
                {
                    _game.generatorPosition.Y -= yStep;
                    AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
                    _game.generatorPosition.X++;
                }
            }

            public override RapidHopsGenerator Clone(SMineCart game)
            {
                RapidHopsGenerator clone = new RapidHopsGenerator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(BaseTrackGenerator clone)
            {
                base.CloneOver(clone);
                if (clone is RapidHopsGenerator generator)
                {
                    generator.minLength = minLength;
                    generator.maxLength = maxLength;
                    generator.startY = startY;
                    generator.yStep = yStep;
                    generator.chaotic = chaotic;
                }
            }
        }

        public class NoxiousMushroom : Obstacle
        {
            public float nextFire;

            public float firePeriod = 1.75f;

            //protected
            public Track _track;

            public Rectangle[] frames = new Rectangle[3]
            {
                new Rectangle(288, 736, 16, 16),
                new Rectangle(288, 752, 16, 16),
                new Rectangle(288, 768, 16, 16)
            };

            public int currentFrame;

            public float frameDuration = 0.05f;

            public float frameTimer;

            public NoxiousMushroom() { }

            public NoxiousMushroom(SMineCart game)
                : base(game) { }

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(-4, -12, 8, 12);
            }

            public override void InitializeObstacle(Track track, Random random)
            {
                nextFire = Utility.RandomFloat(0f, firePeriod, random);
                _track = track;
                base.InitializeObstacle(track, random);
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                nextFire -= time;
                if (nextFire <= 0f)
                {
                    if (IsOnScreen() && _game.deathTimer <= 0f && (float)_game.respawnCounter <= 0f)
                    {
                        NoxiousGas noxiousGas = _game.AddEntity(new NoxiousGas(_game), random);
                        noxiousGas.position = position;
                        noxiousGas.position.Y = GetBounds().Top;
                        noxiousGas.InitializeObstacle(_track, random);
                        SMineCartGlobal.PlaySound(shouldPlaySound, "sandyStep");
                        currentFrame = 1;
                        frameTimer = frameDuration;
                    }
                    nextFire = 1.5f;
                }
                if (currentFrame <= 0)
                {
                    return;
                }
                frameTimer -= time;
                if (frameTimer <= 0f)
                {
                    frameTimer = frameDuration;
                    currentFrame++;
                    if (currentFrame >= frames.Length)
                    {
                        currentFrame = 0;
                        frameTimer = 0f;
                    }
                }
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    frames[currentFrame],
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.45f
                );
            }

            public override bool CanSpawnHere(Track track)
            {
                if (track == null)
                {
                    return false;
                }
                if (track.trackType != 0)
                {
                    return false;
                }
                return true;
            }

            public override NoxiousMushroom Clone(SMineCart game)
            {
                NoxiousMushroom clone = new NoxiousMushroom(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is NoxiousMushroom o)
                {
                    o.nextFire = nextFire;
                    o.firePeriod = firePeriod;
                    o._track = _track;
                    o.frames = frames.DeepClone();
                    o.currentFrame = currentFrame;
                    o.frameDuration = frameDuration;
                    o.frameTimer = frameTimer;
                }
            }
        }

        public class MushroomSpring : Obstacle
        {
            protected HashSet<MineCartCharacter> _bouncedPlayers;

            public Rectangle[] frames = new Rectangle[3]
            {
                new Rectangle(400, 736, 16, 16),
                new Rectangle(400, 752, 16, 16),
                new Rectangle(400, 768, 16, 16)
            };

            public int currentFrame;

            public float frameDuration = 0.05f;

            public float frameTimer;

            public MushroomSpring() { }

            public MushroomSpring(SMineCart game)
                : base(game) { }

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(-4, -12, 8, 12);
            }

            public override void InitializeObstacle(Track track, Random random)
            {
                base.InitializeObstacle(track, random);
                _bouncedPlayers = new HashSet<MineCartCharacter>();
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (currentFrame <= 0)
                {
                    return;
                }
                frameTimer -= time;
                if (frameTimer <= 0f)
                {
                    frameTimer = frameDuration;
                    currentFrame++;
                    if (currentFrame >= frames.Length)
                    {
                        currentFrame = 0;
                        frameTimer = 0f;
                    }
                }
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    frames[currentFrame],
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.45f
                );
            }

            public override bool CanSpawnHere(Track track)
            {
                if (track == null)
                {
                    return false;
                }
                if (track.trackType != 0)
                {
                    return false;
                }
                return true;
            }

            public override bool OnBounce(
                MineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                BouncePlayer(player, random, shouldPlaySound);
                return true;
            }

            public override bool OnBump(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                BouncePlayer(player, random, shouldPlaySound);
                return true;
            }

            public void BouncePlayer(MineCartCharacter player, Random random, bool shouldPlaySound)
            {
                if (!_bouncedPlayers.Contains(player))
                {
                    _bouncedPlayers.Add(player);
                    if (player is PlayerMineCartCharacter)
                    {
                        currentFrame = 1;
                        frameTimer = frameDuration;
                        ShootDebris(random.Next(-10, -4), random.Next(-60, -19), random);
                        ShootDebris(random.Next(5, 11), random.Next(-60, -19), random);
                        ShootDebris(random.Next(-20, -9), random.Next(-40, 0), random);
                        ShootDebris(random.Next(10, 21), random.Next(-40, 0), random);
                        SMineCartGlobal.PlaySound(shouldPlaySound, "hitEnemy");
                    }
                    player.Bounce(0.15f);
                }
            }

            public void ShootDebris(int x, int y, Random random)
            {
                _game.AddEntity(
                    new MineDebris(
                        _game,
                        new Rectangle(368, 784, 16, 16),
                        Utility.PointToVector2(GetBounds().Center),
                        x,
                        y,
                        0.25f,
                        0f,
                        0.9f,
                        1f,
                        3,
                        0.3f
                    ),
                    random
                );
            }

            public override void OnPlayerReset()
            {
                _bouncedPlayers.Clear();
                base.OnPlayerReset();
            }

            public override MushroomSpring Clone(SMineCart game)
            {
                MushroomSpring clone = new MushroomSpring(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is MushroomSpring o)
                {
                    o._bouncedPlayers = new HashSet<MineCartCharacter>(_bouncedPlayers);
                    o.frames = new List<Rectangle>(frames).ToArray();
                    o.currentFrame = currentFrame;
                    o.frameDuration = frameDuration;
                    o.frameTimer = frameTimer;
                }
            }
        }

        public class MushroomBalanceTrackGenerator : BaseTrackGenerator
        {
            protected int minHopSize = 1;

            protected int maxHopSize = 1;

            protected float releaseJumpChance;

            protected List<int> staggerPattern;

            protected Track.TrackType trackType;

            public MushroomBalanceTrackGenerator SetTrackType(Track.TrackType track_type)
            {
                trackType = track_type;
                return this;
            }

            public MushroomBalanceTrackGenerator SetStaggerValues(params int[] args)
            {
                staggerPattern = new List<int>();
                for (int i = 0; i < args.Length; i++)
                {
                    staggerPattern.Add(args[i]);
                }
                return this;
            }

            public MushroomBalanceTrackGenerator SetReleaseJumpChance(float chance)
            {
                releaseJumpChance = chance;
                return this;
            }

            public MushroomBalanceTrackGenerator SetHopSize(int min, int max)
            {
                minHopSize = min;
                maxHopSize = max;
                return this;
            }

            public MushroomBalanceTrackGenerator(SMineCart game)
                : base(game)
            {
                staggerPattern = new List<int>();
            }

            protected override void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                if (_game.generatorPosition.X >= _game.distanceToTravel)
                {
                    return;
                }
                _game.trackBuilderCharacter.enabled = true;
                List<BalanceTrack> balance_tracks = new List<BalanceTrack>();
                for (int i = 0; i < 4; i++)
                {
                    if (i == 1 && random.NextBool())
                    {
                        continue;
                    }
                    _game.trackBuilderCharacter.position.X =
                        ((float)_game.generatorPosition.X - 1f + 0.5f) * (float)_game.tileSize;
                    _game.trackBuilderCharacter.position.Y =
                        _game.generatorPosition.Y * _game.tileSize;
                    _game.trackBuilderCharacter.ForceGrounded();
                    _game.trackBuilderCharacter.Jump(shouldPlaySound);
                    _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                    int target_y = _game.generatorPosition.Y;
                    if (i != 1)
                    {
                        if (i == 3 && random.NextBool())
                        {
                            target_y -= 4;
                        }
                        else if (staggerPattern != null && staggerPattern.Count > 0)
                        {
                            target_y += random.ChooseFrom(staggerPattern);
                        }
                    }
                    target_y = _game.KeepTileInBounds(target_y);
                    bool has_landed = false;
                    while (!has_landed)
                    {
                        if (
                            _game.trackBuilderCharacter.position.Y
                                < (float)(target_y * _game.tileSize)
                            && Math.Abs(
                                Math.Round(
                                    _game.trackBuilderCharacter.position.X / (float)_game.tileSize
                                ) - (double)_game.generatorPosition.X
                            ) > 0.0
                            && _game.trackBuilderCharacter.IsJumping()
                            && random.NextDouble() < (double)releaseJumpChance
                        )
                        {
                            _game.trackBuilderCharacter.ReleaseJump();
                        }
                        Vector2 old_position = _game.trackBuilderCharacter.position;
                        _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                        if (
                            old_position.Y < (float)(target_y * _game.tileSize)
                            && _game.trackBuilderCharacter.position.Y
                                >= (float)(target_y * _game.tileSize)
                        )
                        {
                            has_landed = true;
                        }
                        if (
                            _game.trackBuilderCharacter.IsGrounded()
                            || _game.trackBuilderCharacter.position.Y / (float)_game.tileSize
                                > (float)_game.bottomTile
                        )
                        {
                            _game.trackBuilderCharacter.position = old_position;
                            if (!_game.IsTileInBounds(target_y))
                            {
                                return;
                            }
                            target_y = _game.KeepTileInBounds(
                                (int)(old_position.Y / (float)_game.tileSize)
                            );
                            break;
                        }
                    }
                    _game.generatorPosition.Y = target_y;
                    if (i == 0 || i == 2)
                    {
                        List<BalanceTrack> current_balance_tracks = new List<BalanceTrack>();
                        _game.generatorPosition.X = (int)(
                            _game.trackBuilderCharacter.position.X / (float)_game.tileSize
                        );
                        float y_offset = 0f;
                        if (i == 2 && balance_tracks.Count > 0)
                        {
                            y_offset = balance_tracks[0].position.Y - balance_tracks[0].startY;
                        }
                        BalanceTrack track = new BalanceTrack(
                            _game,
                            Track.TrackType.MushroomLeft,
                            showSecondTile: false
                        );
                        track.position.X = _game.generatorPosition.X * _game.tileSize;
                        track.position.Y = _game.trackBuilderCharacter.position.Y + y_offset;
                        track.startY = track.position.Y;
                        AddTrack(track);
                        current_balance_tracks.Add(track);
                        _game.generatorPosition.X++;
                        track = new BalanceTrack(
                            _game,
                            Track.TrackType.MushroomMiddle,
                            showSecondTile: false
                        );
                        track.position.X = _game.generatorPosition.X * _game.tileSize;
                        track.position.Y = _game.trackBuilderCharacter.position.Y + y_offset;
                        track.startY = track.position.Y;
                        AddTrack(track);
                        current_balance_tracks.Add(track);
                        _game.generatorPosition.X++;
                        track = new BalanceTrack(
                            _game,
                            Track.TrackType.MushroomRight,
                            showSecondTile: false
                        );
                        track.position.X = _game.generatorPosition.X * _game.tileSize;
                        track.position.Y = _game.trackBuilderCharacter.position.Y + y_offset;
                        track.startY = track.position.Y;
                        AddTrack(track);
                        current_balance_tracks.Add(track);
                        _game.generatorPosition.X++;
                        foreach (BalanceTrack item in current_balance_tracks)
                        {
                            item.connectedTracks = new List<BalanceTrack>(current_balance_tracks);
                        }
                        if (i == 2)
                        {
                            foreach (BalanceTrack item2 in balance_tracks)
                            {
                                item2.counterBalancedTracks = new List<BalanceTrack>(
                                    current_balance_tracks
                                );
                            }
                            foreach (BalanceTrack item3 in current_balance_tracks)
                            {
                                item3.counterBalancedTracks = new List<BalanceTrack>(
                                    balance_tracks
                                );
                            }
                        }
                        _game.trackBuilderCharacter.SnapToFloor();
                        while (_game.trackBuilderCharacter.IsGrounded())
                        {
                            float old_x = _game.trackBuilderCharacter.position.X;
                            _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                            if (!_game.trackBuilderCharacter.IsGrounded())
                            {
                                _game.trackBuilderCharacter.position.X = old_x;
                            }
                            if (random.NextDouble() < 0.33000001311302185)
                            {
                                break;
                            }
                        }
                        balance_tracks.AddRange(current_balance_tracks);
                        continue;
                    }
                    int hop_width = random.Next(minHopSize, maxHopSize + 1);
                    for (int width = 0; width < hop_width; width++)
                    {
                        _game.generatorPosition.X =
                            (int)(_game.trackBuilderCharacter.position.X / (float)_game.tileSize)
                            + width;
                        if (_game.generatorPosition.X >= _game.distanceToTravel)
                        {
                            return;
                        }
                        AddPickupTrack(
                            _game.generatorPosition.X,
                            _game.generatorPosition.Y,
                            trackType
                        );
                    }
                }
                foreach (BalanceTrack balance_track in balance_tracks)
                {
                    balance_track.position.Y = balance_track.startY;
                }
                _game.generatorPosition.X++;
            }

            public override MushroomBalanceTrackGenerator Clone(SMineCart game)
            {
                MushroomBalanceTrackGenerator clone = new MushroomBalanceTrackGenerator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(BaseTrackGenerator clone)
            {
                base.CloneOver(clone);
                if (clone is MushroomBalanceTrackGenerator o)
                {
                    o.minHopSize = minHopSize;
                    o.maxHopSize = maxHopSize;
                    o.releaseJumpChance = releaseJumpChance;
                    o.staggerPattern = new List<int>(staggerPattern);
                    o.trackType = trackType;
                }
            }
        }

        public class MushroomBunnyHopGenerator : BaseTrackGenerator
        {
            protected int numberOfHops;

            protected int minHops = 1;

            protected int maxHops = 5;

            protected int minHopSize = 1;

            protected int maxHopSize = 1;

            protected float releaseJumpChance;

            protected List<int> staggerPattern;

            protected Track.TrackType trackType;

            public MushroomBunnyHopGenerator SetStaggerValues(params int[] args)
            {
                staggerPattern = new List<int>();
                for (int i = 0; i < args.Length; i++)
                {
                    staggerPattern.Add(args[i]);
                }
                return this;
            }

            public MushroomBunnyHopGenerator SetReleaseJumpChance(float chance)
            {
                releaseJumpChance = chance;
                return this;
            }

            public MushroomBunnyHopGenerator SetHopSize(int min, int max)
            {
                minHopSize = min;
                maxHopSize = max;
                return this;
            }

            public MushroomBunnyHopGenerator SetNumberOfHops(int min, int max)
            {
                minHops = min;
                maxHops = max;
                return this;
            }

            public MushroomBunnyHopGenerator(SMineCart game)
                : base(game)
            {
                minHopSize = 1;
                maxHopSize = 1;
                staggerPattern = new List<int>();
            }

            public override void Initialize(Random random)
            {
                numberOfHops = random.Next(minHops, maxHops + 1);
                base.Initialize(random);
            }

            protected override void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                if (_game.generatorPosition.X >= _game.distanceToTravel)
                {
                    return;
                }
                _game.trackBuilderCharacter.enabled = true;
                MushroomSpring spring = null;
                for (int i = 0; i < numberOfHops; i++)
                {
                    _game.trackBuilderCharacter.position.X =
                        ((float)_game.generatorPosition.X - 1f + 0.5f) * (float)_game.tileSize;
                    _game.trackBuilderCharacter.position.Y =
                        _game.generatorPosition.Y * _game.tileSize;
                    _game.trackBuilderCharacter.ForceGrounded();
                    _game.trackBuilderCharacter.Jump(shouldPlaySound);
                    spring?.BouncePlayer(_game.trackBuilderCharacter, random, shouldPlaySound);
                    _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                    int target_y = _game.generatorPosition.Y;
                    if (staggerPattern != null && staggerPattern.Count > 0)
                    {
                        target_y += random.ChooseFrom(staggerPattern);
                    }
                    target_y = _game.KeepTileInBounds(target_y);
                    bool has_landed = false;
                    while (!has_landed)
                    {
                        if (
                            _game.trackBuilderCharacter.position.Y
                                < (float)(target_y * _game.tileSize)
                            && Math.Abs(
                                Math.Round(
                                    _game.trackBuilderCharacter.position.X / (float)_game.tileSize
                                ) - (double)_game.generatorPosition.X
                            ) > 1.0
                            && _game.trackBuilderCharacter.IsJumping()
                            && random.NextDouble() < (double)releaseJumpChance
                        )
                        {
                            _game.trackBuilderCharacter.ReleaseJump();
                        }
                        Vector2 old_position = _game.trackBuilderCharacter.position;
                        float y = _game.trackBuilderCharacter.velocity.Y;
                        _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                        if (y < 0f && _game.trackBuilderCharacter.velocity.Y >= 0f)
                        {
                            _game.CreatePickup(
                                _game.trackBuilderCharacter.position + new Vector2(0f, 8f)
                            );
                        }
                        if (
                            old_position.Y < (float)(target_y * _game.tileSize)
                            && _game.trackBuilderCharacter.position.Y
                                >= (float)(target_y * _game.tileSize)
                        )
                        {
                            has_landed = true;
                        }
                        if (
                            _game.trackBuilderCharacter.IsGrounded()
                            || _game.trackBuilderCharacter.position.Y / (float)_game.tileSize
                                > (float)_game.bottomTile
                        )
                        {
                            _game.trackBuilderCharacter.position = old_position;
                            if (!_game.IsTileInBounds(target_y))
                            {
                                return;
                            }
                            target_y = _game.KeepTileInBounds(
                                (int)(old_position.Y / (float)_game.tileSize)
                            );
                            break;
                        }
                    }
                    _game.generatorPosition.Y = target_y;
                    int hop_width = random.Next(minHopSize, maxHopSize + 1);
                    Track.TrackType track_type = trackType;
                    if (i >= numberOfHops - 1)
                    {
                        track_type = Track.TrackType.Straight;
                    }
                    spring = null;
                    for (int width = 0; width < hop_width; width++)
                    {
                        _game.generatorPosition.X =
                            (int)(_game.trackBuilderCharacter.position.X / (float)_game.tileSize)
                            + width;
                        if (_game.generatorPosition.X >= _game.distanceToTravel)
                        {
                            return;
                        }
                        if (track_type == Track.TrackType.MushroomMiddle)
                        {
                            AddTrack(
                                _game.generatorPosition.X - 1,
                                _game.generatorPosition.Y,
                                Track.TrackType.MushroomLeft
                            );
                            AddTrack(
                                _game.generatorPosition.X + 1,
                                _game.generatorPosition.Y,
                                Track.TrackType.MushroomRight
                            );
                        }
                        Track track = AddTrack(
                            _game.generatorPosition.X,
                            _game.generatorPosition.Y,
                            track_type
                        );
                        if (
                            width == hop_width - 1
                            && i < numberOfHops - 1
                            && _game.generatorPosition.Y > 4
                        )
                        {
                            spring = _game.AddEntity(new MushroomSpring(_game), random);
                            spring.InitializeObstacle(track, random);
                            spring.position.X = track.position.X + (float)(_game.tileSize / 2);
                            spring.position.Y = track.GetYAtPoint(spring.position.X);
                        }
                    }
                }
                _game.generatorPosition.X++;
            }

            public override MushroomBunnyHopGenerator Clone(SMineCart game)
            {
                MushroomBunnyHopGenerator clone = new MushroomBunnyHopGenerator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(BaseTrackGenerator clone)
            {
                base.CloneOver(clone);
                if (clone is MushroomBunnyHopGenerator o)
                {
                    o.numberOfHops = numberOfHops;
                    o.minHops = minHops;
                    o.maxHops = maxHops;
                    o.minHopSize = minHopSize;
                    o.maxHopSize = maxHopSize;
                    o.releaseJumpChance = releaseJumpChance;
                    o.staggerPattern = new List<int>(staggerPattern);
                    o.trackType = trackType;
                }
            }
        }

        public class BunnyHopGenerator : BaseTrackGenerator
        {
            protected int numberOfHops;

            protected int minHops = 1;

            protected int maxHops = 5;

            protected int minHopSize = 1;

            protected int maxHopSize = 1;

            protected float releaseJumpChance;

            protected List<int> staggerPattern;

            protected Track.TrackType trackType;

            public BunnyHopGenerator SetTrackType(Track.TrackType track_type)
            {
                trackType = track_type;
                return this;
            }

            public BunnyHopGenerator SetStaggerValues(params int[] args)
            {
                staggerPattern = new List<int>();
                for (int i = 0; i < args.Length; i++)
                {
                    staggerPattern.Add(args[i]);
                }
                return this;
            }

            public BunnyHopGenerator SetReleaseJumpChance(float chance)
            {
                releaseJumpChance = chance;
                return this;
            }

            public BunnyHopGenerator SetHopSize(int min, int max)
            {
                minHopSize = min;
                maxHopSize = max;
                return this;
            }

            public BunnyHopGenerator SetNumberOfHops(int min, int max)
            {
                minHops = min;
                maxHops = max;
                return this;
            }

            public BunnyHopGenerator(SMineCart game)
                : base(game)
            {
                minHopSize = 1;
                maxHopSize = 1;
                staggerPattern = new List<int>();
            }

            public override void Initialize(Random random)
            {
                numberOfHops = random.Next(minHops, maxHops + 1);
                base.Initialize(random);
            }

            protected override void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                if (_game.generatorPosition.X >= _game.distanceToTravel)
                {
                    return;
                }
                _game.trackBuilderCharacter.enabled = true;
                for (int i = 0; i < numberOfHops; i++)
                {
                    _game.trackBuilderCharacter.position.X =
                        ((float)_game.generatorPosition.X - 1f + 0.5f) * (float)_game.tileSize;
                    _game.trackBuilderCharacter.position.Y =
                        _game.generatorPosition.Y * _game.tileSize;
                    _game.trackBuilderCharacter.ForceGrounded();
                    _game.trackBuilderCharacter.Jump(shouldPlaySound);
                    _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                    int target_y = _game.generatorPosition.Y;
                    if (staggerPattern != null && staggerPattern.Count > 0)
                    {
                        target_y += random.ChooseFrom(staggerPattern);
                    }
                    target_y = _game.KeepTileInBounds(target_y);
                    bool has_landed = false;
                    while (!has_landed)
                    {
                        if (
                            _game.trackBuilderCharacter.position.Y
                                < (float)(target_y * _game.tileSize)
                            && Math.Abs(
                                Math.Round(
                                    _game.trackBuilderCharacter.position.X / (float)_game.tileSize
                                ) - (double)_game.generatorPosition.X
                            ) > 1.0
                            && _game.trackBuilderCharacter.IsJumping()
                            && random.NextDouble() < (double)releaseJumpChance
                        )
                        {
                            _game.trackBuilderCharacter.ReleaseJump();
                        }
                        Vector2 old_position = _game.trackBuilderCharacter.position;
                        float y = _game.trackBuilderCharacter.velocity.Y;
                        _game.trackBuilderCharacter.Update(0.03f, random, shouldPlaySound);
                        if (y < 0f && _game.trackBuilderCharacter.velocity.Y >= 0f)
                        {
                            _game.CreatePickup(
                                _game.trackBuilderCharacter.position + new Vector2(0f, 8f)
                            );
                        }
                        if (
                            old_position.Y < (float)(target_y * _game.tileSize)
                            && _game.trackBuilderCharacter.position.Y
                                >= (float)(target_y * _game.tileSize)
                        )
                        {
                            has_landed = true;
                        }
                        if (
                            _game.trackBuilderCharacter.IsGrounded()
                            || _game.trackBuilderCharacter.position.Y / (float)_game.tileSize
                                > (float)_game.bottomTile
                        )
                        {
                            _game.trackBuilderCharacter.position = old_position;
                            if (!_game.IsTileInBounds(target_y))
                            {
                                return;
                            }
                            target_y = _game.KeepTileInBounds(
                                (int)(old_position.Y / (float)_game.tileSize)
                            );
                            break;
                        }
                    }
                    _game.generatorPosition.Y = target_y;
                    int hop_width = random.Next(minHopSize, maxHopSize + 1);
                    Track.TrackType track_type = trackType;
                    if (i >= numberOfHops - 1)
                    {
                        track_type = Track.TrackType.Straight;
                    }
                    for (int width = 0; width < hop_width; width++)
                    {
                        _game.generatorPosition.X =
                            (int)(_game.trackBuilderCharacter.position.X / (float)_game.tileSize)
                            + width;
                        if (_game.generatorPosition.X >= _game.distanceToTravel)
                        {
                            return;
                        }
                        if (track_type == Track.TrackType.MushroomMiddle)
                        {
                            AddTrack(
                                _game.generatorPosition.X - 1,
                                _game.generatorPosition.Y,
                                Track.TrackType.MushroomLeft
                            );
                            AddTrack(
                                _game.generatorPosition.X + 1,
                                _game.generatorPosition.Y,
                                Track.TrackType.MushroomRight
                            );
                        }
                        AddPickupTrack(
                            _game.generatorPosition.X,
                            _game.generatorPosition.Y,
                            track_type
                        );
                    }
                }
                _game.generatorPosition.X++;
            }

            public override BunnyHopGenerator Clone(SMineCart game)
            {
                BunnyHopGenerator clone = new BunnyHopGenerator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(BaseTrackGenerator clone)
            {
                base.CloneOver(clone);
                if (clone is BunnyHopGenerator o)
                {
                    o.numberOfHops = numberOfHops;
                    o.minHops = minHops;
                    o.maxHops = maxHops;
                    o.minHopSize = minHopSize;
                    o.maxHopSize = maxHopSize;
                    o.releaseJumpChance = releaseJumpChance;
                    o.staggerPattern = new List<int>(staggerPattern);
                    o.trackType = trackType;
                }
            }
        }

        public class BaseTrackGenerator
        {
            public const int OBSTACLE_NONE = -10;

            public const int OBSTACLE_MIDDLE = -10;

            public const int OBSTACLE_FRONT = -11;

            public const int OBSTACLE_BACK = -12;

            public const int OBSTACLE_RANDOM = -13;

            protected List<Track> _generatedTracks;

            protected SMineCart _game;

            protected Dictionary<int, KeyValuePair<ObstacleTypes, float>> _obstacleIndices =
                new Dictionary<int, KeyValuePair<ObstacleTypes, float>>();

            protected Func<Track, BaseTrackGenerator, bool> _pickupFunction;

            public static bool FlatsOnly(Track track, BaseTrackGenerator generator)
            {
                return track.trackType == Track.TrackType.None;
            }

            public static bool UpSlopesOnly(Track track, BaseTrackGenerator generator)
            {
                return track.trackType == Track.TrackType.UpSlope;
            }

            public static bool DownSlopesOnly(Track track, BaseTrackGenerator generator)
            {
                return track.trackType == Track.TrackType.DownSlope;
            }

            public static bool IceDownSlopesOnly(Track track, BaseTrackGenerator generator)
            {
                return track.trackType == Track.TrackType.IceDownSlope;
            }

            public static bool Always(Track track, BaseTrackGenerator generator)
            {
                return true;
            }

            public static bool EveryOtherTile(Track track, BaseTrackGenerator generator)
            {
                if ((int)(track.position.X / 16f) % 2 == 0)
                {
                    return true;
                }
                return false;
            }

            public T AddObstacle<T>(
                ObstacleTypes obstacle_type,
                int position,
                float obstacle_chance = 1f
            )
                where T : BaseTrackGenerator
            {
                _obstacleIndices.Add(
                    position,
                    new KeyValuePair<ObstacleTypes, float>(obstacle_type, obstacle_chance)
                );
                return this as T;
            }

            public T AddPickupFunction<T>(
                Func<Track, BaseTrackGenerator, bool> pickup_spawn_function
            )
                where T : BaseTrackGenerator
            {
                _pickupFunction =
                    (Func<Track, BaseTrackGenerator, bool>)
                        Delegate.Combine(_pickupFunction, pickup_spawn_function);
                return this as T;
            }

            public BaseTrackGenerator(SMineCart game)
            {
                _game = game;
            }

            public Track AddTrack(
                int x,
                int y,
                Track.TrackType track_type = Track.TrackType.Straight
            )
            {
                Track track = _game.AddTrack(x, y, track_type);
                _generatedTracks.Add(track);
                return track;
            }

            public Track AddTrack(Track track)
            {
                _game.AddTrack(track);
                _generatedTracks.Add(track);
                return track;
            }

            public Track AddPickupTrack(
                int x,
                int y,
                Track.TrackType track_type = Track.TrackType.Straight
            )
            {
                Track track = AddTrack(x, y, track_type);
                if (_pickupFunction == null)
                {
                    return track;
                }
                Delegate[] invocationList = _pickupFunction.GetInvocationList();
                for (int i = 0; i < invocationList.Length; i++)
                {
                    if (!((Func<Track, BaseTrackGenerator, bool>)invocationList[i])(track, this))
                    {
                        return track;
                    }
                }
                Pickup pickup = _game.CreatePickup(
                    track.position + new Vector2(8f, -_game.tileSize)
                );
                if (
                    pickup != null
                    && (
                        track.trackType == Track.TrackType.DownSlope
                        || track.trackType == Track.TrackType.UpSlope
                        || track.trackType == Track.TrackType.IceDownSlope
                        || track.trackType == Track.TrackType.SlimeUpSlope
                    )
                )
                {
                    pickup.position += new Vector2(0f, (float)(-_game.tileSize) * 0.75f);
                }
                return track;
            }

            public virtual void Initialize(Random random)
            {
                _generatedTracks = new List<Track>();
            }

            public void GenerateTrack(Random random, bool shouldPlaySound)
            {
                _GenerateTrack(random, shouldPlaySound);
                PopulateObstacles(random, shouldPlaySound);
            }

            public void PopulateObstacles(Random random, bool shouldPlaySound)
            {
                if (
                    _game.generatorPosition.X >= _game.distanceToTravel
                    || _generatedTracks.Count == 0
                )
                {
                    return;
                }
                _generatedTracks.OrderBy((Track o) => o.position.X);
                if (_obstacleIndices == null || _obstacleIndices.Count == 0)
                {
                    return;
                }
                foreach (int index in _obstacleIndices.Keys)
                {
                    if (random.NextBool(_obstacleIndices[index].Value))
                    {
                        int track_index = index switch
                        {
                            -12 => _generatedTracks.Count - 1,
                            -11 => 0,
                            -10 => (_generatedTracks.Count - 1) / 2,
                            -13 => random.Next(_generatedTracks.Count),
                            _ => index,
                        };
                        Track track = _generatedTracks[track_index];
                        if (
                            track != null
                            && (int)(track.position.X / (float)_game.tileSize)
                                < _game.distanceToTravel
                        )
                        {
                            _game.AddObstacle(track, _obstacleIndices[index].Key);
                        }
                    }
                }
            }

            protected virtual void _GenerateTrack(Random random, bool shouldPlaySound)
            {
                _game.generatorPosition.X++;
            }

            public virtual BaseTrackGenerator Clone(SMineCart game)
            {
                BaseTrackGenerator baseTrackGenerator = new BaseTrackGenerator(game);
                CloneOver(baseTrackGenerator);
                return baseTrackGenerator;
            }

            public virtual void CloneOver(BaseTrackGenerator clone)
            {
                // Controller.Console.Alert("\tCloning a BaseTrackGenerator CloneOver");
                if (_generatedTracks != null)
                {
                    clone._generatedTracks = new List<Track>();
                    foreach (Track generatedTrack in _generatedTracks)
                    {
                        clone._generatedTracks.Add(generatedTrack.Clone(clone._game));
                    }
                }
                if (_obstacleIndices != null)
                {
                    clone._obstacleIndices = new Dictionary<
                        int,
                        KeyValuePair<ObstacleTypes, float>
                    >(_obstacleIndices);
                }
                clone._pickupFunction = _pickupFunction;
            }
        }

        public class Spark
        {
            public float x;

            public float y;

            public Color c;

            public float dx;

            public float dy;

            public Spark(float x, float y, float dx, float dy)
            {
                this.x = x;
                this.y = y;
                this.dx = dx;
                this.dy = dy;
                c = Color.Yellow;
            }

            public Spark Clone()
            {
                return new Spark(x, y, dx, dy) { c = c };
            }
        }

        public class Entity
        {
            public Vector2 position;

            protected SMineCart _game;

            public bool visible = true;

            public bool enabled = true;

            protected bool _destroyed;

            public Vector2 drawnPosition => position - new Vector2(_game.screenLeftBound, 0f);

            public virtual void OnPlayerReset() { }

            public Entity() { }

            public Entity(SMineCart game)
            {
                _game = game;
            }

            public bool IsOnScreen()
            {
                if (position.X < _game.screenLeftBound - (float)(_game.tileSize * 4))
                {
                    return false;
                }
                if (
                    position.X
                    > _game.screenLeftBound + (float)_game.screenWidth + (float)(_game.tileSize * 4)
                )
                {
                    return false;
                }
                return true;
            }

            public bool IsActive()
            {
                if (_destroyed)
                {
                    return false;
                }
                if (!enabled)
                {
                    return false;
                }
                return true;
            }

            public void Initialize(SMineCart game, Random random)
            {
                _game = game;
                _Initialize(random);
            }

            public void Destroy()
            {
                _destroyed = true;
            }

            protected virtual void _Initialize(Random random) { }

            public virtual bool ShouldReap()
            {
                return _destroyed;
            }

            public void Draw(SpriteBatch b)
            {
                if (!_destroyed && visible && enabled)
                {
                    _Draw(b);
                }
            }

            public void SpeedDraw()
            {
                if (!_destroyed && visible && enabled)
                {
                    _SpeedDraw();
                }
            }

            public virtual void _SpeedDraw() { }

            public virtual void _Draw(SpriteBatch b) { }

            public void Update(float time, Random random, bool shouldPlaySound)
            {
                if (!_destroyed && enabled)
                {
                    _Update(time, random, shouldPlaySound);
                }
            }

            protected virtual void _Update(float time, Random random, bool shouldPlaySound) { }

            public virtual Entity Clone(SMineCart game)
            {
                Entity entity = new Entity(game);
                CloneOver(entity);
                return entity;
            }

            public virtual void CloneOver(Entity clone)
            {
                clone.position = position;
                clone.visible = visible;
                clone.enabled = enabled;
                clone._destroyed = _destroyed;
            }
        }

        public class BaseCharacter : Entity
        {
            public Vector2 velocity;

            public BaseCharacter(SMineCart game)
                : base(game) { }

            public override BaseCharacter Clone(SMineCart game)
            {
                BaseCharacter baseCharacter = new BaseCharacter(game);
                CloneOver(baseCharacter);
                return baseCharacter;
            }

            public virtual void CloneOver(BaseCharacter clone)
            {
                base.CloneOver(clone);
                clone.velocity = velocity;
            }
        }

        public interface ICollideable
        {
            Rectangle GetLocalBounds();

            Rectangle GetBounds();
        }

        public class Bubble : Obstacle
        {
            public Vector2 _normalizedVelocity;

            public float moveSpeed = 8f;

            protected float _age;

            protected int _currentFrame;

            protected float _timePerFrame = 0.5f;

            protected int[] _frames = new int[6] { 0, 1, 2, 3, 3, 2 };

            protected int _repeatedFrameCount = 4;

            protected float _lifeTime = 3f;

            public Vector2 bubbleOffset = Vector2.Zero;

            public Bubble() { }

            public override void OnPlayerReset()
            {
                Destroy();
            }

            public override Rectangle GetBounds()
            {
                Rectangle bounds = base.GetBounds();
                bounds.X += (int)bubbleOffset.X;
                bounds.Y += (int)bubbleOffset.Y;
                return base.GetBounds();
            }

            public Bubble(SMineCart game)
                : base(game) { }

            public Bubble(float angle, float speed, SMineCart game)
                : base(game)
            {
                _normalizedVelocity.X = (float)Math.Cos(angle * (float)Math.PI / 180f);
                _normalizedVelocity.Y = 0f - (float)Math.Sin(angle * (float)Math.PI / 180f);
                moveSpeed = speed;
                _age = 0f;
            }

            public override bool OnBump(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                Pop(random, shouldPlaySound: shouldPlaySound);
                return base.OnBump(player, random, shouldPlaySound);
            }

            public override bool OnBounce(
                MineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                if (!(player is PlayerMineCartCharacter))
                {
                    return false;
                }
                player.Bounce();
                Pop(random: random, shouldPlaySound: shouldPlaySound);
                return true;
            }

            public void Pop(Random random, bool play_sound = true, bool shouldPlaySound = false)
            {
                if (play_sound)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "dropItemInWater");
                }
                Destroy();
                _game.AddEntity(
                    new MineDebris(
                        _game,
                        new Rectangle(32, 240, 16, 16),
                        new Vector2(GetBounds().Center.X, GetBounds().Center.Y),
                        0f,
                        0f,
                        0f,
                        0f,
                        0.4f,
                        1f,
                        2,
                        0.2f
                    ),
                    random
                );
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                position += moveSpeed * _normalizedVelocity * time;
                _age += time;
                _currentFrame = (int)(_age / _timePerFrame);
                if (_currentFrame >= _frames.Length)
                {
                    _currentFrame -= _frames.Length;
                    _currentFrame %= _repeatedFrameCount;
                    _currentFrame += _frames.Length - _repeatedFrameCount;
                }
                bubbleOffset.X = (float)Math.Cos(_age * 10f) * 4f;
                bubbleOffset.Y = (float)Math.Sin(_age * 10f) * 4f;
                if (_age >= _lifeTime)
                {
                    Pop(play_sound: false, random: random, shouldPlaySound: shouldPlaySound);
                }
                base._Update(time, random, shouldPlaySound);
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition + bubbleOffset),
                    new Rectangle(_frames[_currentFrame] * 16, 256, 16, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.27f
                );
            }

            public override Bubble Clone(SMineCart game)
            {
                Bubble clone = new Bubble(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is Bubble bubble)
                {
                    bubble._normalizedVelocity = _normalizedVelocity;
                    bubble.moveSpeed = moveSpeed;
                    bubble._age = _age;
                    bubble._currentFrame = _currentFrame;
                    bubble._timePerFrame = _timePerFrame;
                    bubble._frames = new List<int>(_frames).ToArray();
                    bubble._repeatedFrameCount = _repeatedFrameCount;
                    bubble._lifeTime = _lifeTime;
                    bubble.bubbleOffset = bubbleOffset;
                }
            }
        }

        public class PlayerBubbleSpawner : Entity
        {
            public int bubbleCount;

            public float timer;

            public PlayerBubbleSpawner(SMineCart game)
                : base(game) { }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                position = _game.player.position;
                timer -= time;
                if (_game.player.velocity.Y > 0f && bubbleCount == 0)
                {
                    bubbleCount = 1;
                    timer = Utility.Lerp(0.05f, 0.25f, (float)random.NextDouble());
                }
                if (timer <= 0f && bubbleCount <= 0)
                {
                    bubbleCount = random.Next(1, 4);
                    timer = Utility.Lerp(0.15f, 0.25f, (float)random.NextDouble());
                }
                else if (timer <= 0f)
                {
                    bubbleCount--;
                    _game.AddEntity(
                        new MineDebris(
                            _game,
                            new Rectangle(0, 256, 16, 16),
                            position
                                + new Vector2(0f - _game.player.characterExtraHeight - 16f) / 2f,
                            -10f,
                            10f,
                            0f,
                            -1f,
                            1.5f,
                            0.5f,
                            4,
                            0.1f,
                            0.45f,
                            holdLastFrame: true
                        ),
                        random
                    );
                    if (bubbleCount == 0)
                    {
                        timer = Utility.Lerp(1f, 1.5f, (float)random.NextDouble());
                    }
                    else
                    {
                        timer = Utility.Lerp(0.15f, 0.25f, (float)random.NextDouble());
                    }
                }
            }

            public override PlayerBubbleSpawner Clone(SMineCart game)
            {
                PlayerBubbleSpawner clone = new PlayerBubbleSpawner(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is PlayerBubbleSpawner o)
                {
                    o.bubbleCount = bubbleCount;
                    o.timer = timer;
                }
            }
        }

        public class Whale : Entity
        {
            public enum CurrentState
            {
                Idle,
                OpenMouth,
                FireBubbles,
                CloseMouth
            }

            protected CurrentState _currentState;

            protected float _stateTimer;

            public float mouthCloseTime = 1f;

            protected float _nextFire;

            protected int _currentFrame;

            protected Vector2 _basePosition;

            public Whale(SMineCart game)
                : base(game) { }

            public void SetState(CurrentState new_state, float state_timer = 1f)
            {
                _currentState = new_state;
                _stateTimer = state_timer;
            }

            public override void OnPlayerReset()
            {
                _currentState = CurrentState.Idle;
                _stateTimer = 2f;
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                base._Update(time, random, shouldPlaySound);
                _basePosition.Y = Utility.MoveTowards(
                    _basePosition.Y,
                    _game.player.position.Y + 32f,
                    48f * time
                );
                position.X =
                    _game.screenLeftBound
                    - 128f
                    + (float)_game.screenWidth
                    + (float)Math.Cos(_game.totalTime * Math.PI / 2.299999952316284) * 24f;
                position.Y =
                    _basePosition.Y + (float)Math.Sin(_game.totalTime * Math.PI / 3.0) * 32f;
                if (position.Y > (float)_game.screenHeight)
                {
                    position.Y = _game.screenHeight;
                }
                if (position.Y < 120f)
                {
                    position.Y = 120f;
                }
                _stateTimer -= time;
                if (_currentState == CurrentState.Idle)
                {
                    _currentFrame = 0;
                    if (_stateTimer < 0f && _game.gameState != GameStates.Cutscene)
                    {
                        _currentState = CurrentState.OpenMouth;
                        _stateTimer = mouthCloseTime;
                        SMineCartGlobal.PlaySound(shouldPlaySound, "croak");
                    }
                }
                else if (_currentState == CurrentState.OpenMouth)
                {
                    _currentFrame = (int)Utility.Lerp(3f, 0f, _stateTimer / mouthCloseTime);
                    if (_stateTimer < 0f)
                    {
                        _currentState = CurrentState.FireBubbles;
                        _stateTimer = 4f;
                    }
                    _nextFire = 0f;
                }
                else if (_currentState == CurrentState.FireBubbles)
                {
                    _currentFrame = 3;
                    _nextFire -= time;
                    if (_nextFire <= 0f)
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, "dwop");
                        _nextFire = 1f;
                        float shoot_speed = 32f;
                        float shoot_spread = 45f;
                        if ((float)_game.generatorPosition.X >= (float)_game.distanceToTravel / 2f)
                        {
                            shoot_speed = Utility.Lerp(32f, 64f, (float)random.NextDouble());
                            shoot_spread = 60f;
                        }
                        _game
                            .AddEntity(
                                new Bubble(
                                    180f
                                        + Utility.Lerp(
                                            0f - shoot_spread,
                                            shoot_spread,
                                            (float)random.NextDouble()
                                        ),
                                    shoot_speed,
                                    _game
                                ),
                                random
                            )
                            .position = position + new Vector2(48f, -40f);
                        _game.AddEntity(
                            new MineDebris(
                                _game,
                                new Rectangle(0, 256, 16, 16),
                                position + new Vector2(96f, -100f),
                                -10f,
                                10f,
                                0f,
                                -1f,
                                1f,
                                0.5f,
                                4,
                                0.25f
                            ),
                            random
                        );
                    }
                    if (_stateTimer < 0f)
                    {
                        _currentState = CurrentState.CloseMouth;
                        _stateTimer = mouthCloseTime;
                    }
                }
                else if (_currentState == CurrentState.CloseMouth)
                {
                    _currentFrame = (int)Utility.Lerp(0f, 3f, _stateTimer / mouthCloseTime);
                    if (_stateTimer < 0f)
                    {
                        _currentState = CurrentState.Idle;
                        _stateTimer = 2f;
                    }
                }
            }

            protected override void _Initialize(Random random)
            {
                _currentState = CurrentState.Idle;
                _stateTimer = Utility.Lerp(1f, 2f, (float)random.NextDouble());
                _basePosition.Y = _game.screenHeight / 2 + 56;
                base._Initialize(random);
            }

            public override void _Draw(SpriteBatch b)
            {
                Point source_rect_offset = default(Point);
                Point draw_offset = default(Point);
                if (_currentFrame > 0)
                {
                    source_rect_offset.X = 85 * (_currentFrame - 1) + 1;
                    source_rect_offset.Y = 112;
                    draw_offset.X = 3;
                    draw_offset.Y = -3;
                }
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition + new Vector2(85f, 0f)),
                    new Rectangle(86, 288, 75, 112),
                    Color.White,
                    0f,
                    new Vector2(0f, 112f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.29f
                );
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition + Utility.PointToVector2(draw_offset)),
                    new Rectangle(source_rect_offset.X, 288 + source_rect_offset.Y, 85, 112),
                    Color.White,
                    0f,
                    new Vector2(0f, 112f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.28f
                );
            }

            public override Whale Clone(SMineCart game)
            {
                Whale clone = new Whale(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is Whale o)
                {
                    o._currentState = _currentState;
                    o._stateTimer = _stateTimer;
                    o._nextFire = _nextFire;
                    o._currentFrame = _currentFrame;
                    o._basePosition = _basePosition;
                    o.mouthCloseTime = mouthCloseTime;
                }
            }
        }

        public class EndingJunimo : Entity
        {
            protected Color _color;

            protected Vector2 _velocity;

            private bool _special;

            public EndingJunimo(SMineCart game)
                : base(game) { }

            public EndingJunimo(SMineCart game, bool special = false)
                : base(game)
            {
                _special = special;
            }

            protected override void _Initialize(Random random)
            {
                if (_special || random.NextDouble() < 0.01)
                {
                    switch (random.Next(8))
                    {
                        case 0:
                            _color = Color.Red;
                            break;
                        case 1:
                            _color = Color.Goldenrod;
                            break;
                        case 2:
                            _color = Color.Yellow;
                            break;
                        case 3:
                            _color = Color.Lime;
                            break;
                        case 4:
                            _color = new Color(0, 255, 180);
                            break;
                        case 5:
                            _color = new Color(0, 100, 255);
                            break;
                        case 6:
                            _color = Color.MediumPurple;
                            break;
                        case 7:
                            _color = Color.Salmon;
                            break;
                    }
                    if (random.NextDouble() < 0.01)
                    {
                        _color = Color.White;
                    }
                }
                else
                {
                    switch (random.Next(8))
                    {
                        case 0:
                            _color = Color.LimeGreen;
                            break;
                        case 1:
                            _color = Color.Orange;
                            break;
                        case 2:
                            _color = Color.LightGreen;
                            break;
                        case 3:
                            _color = Color.Tan;
                            break;
                        case 4:
                            _color = Color.GreenYellow;
                            break;
                        case 5:
                            _color = Color.LawnGreen;
                            break;
                        case 6:
                            _color = Color.PaleGreen;
                            break;
                        case 7:
                            _color = Color.Turquoise;
                            break;
                    }
                }
                _velocity.X = Utility.RandomFloat(-10f, -40f, random);
                _velocity.Y = Utility.RandomFloat(-20f, -60f, random);
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                position += time * _velocity;
                _velocity.Y += 210f * time;
                float floor_y = _game.GetTrackForXPosition(position.X).position.Y;
                if (position.Y >= floor_y)
                {
                    if (random.NextDouble() < 0.10000000149011612)
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, "junimoMeep1");
                    }
                    position.Y = floor_y;
                    _velocity.Y = Utility.RandomFloat(-50f, -90f, random);
                    if (position.X < _game.player.position.X)
                    {
                        _velocity.X = Utility.RandomFloat(10f, 40f, random);
                    }
                    if (position.X > _game.player.position.X)
                    {
                        _velocity.X = Utility.RandomFloat(10f, 40f, random) * -1f;
                    }
                }
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    Game1.mouseCursors,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(294 + (int)(_game.totalTimeMS % 400.0) / 100 * 16, 1432, 16, 16),
                    _color,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale() * 2f / 3f,
                    SpriteEffects.None,
                    0.25f
                );
            }

            public override EndingJunimo Clone(SMineCart game)
            {
                EndingJunimo clone = new EndingJunimo(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is EndingJunimo o)
                {
                    o._color = _color;
                    o._velocity = _velocity;
                    o._special = _special;
                }
            }
        }

        public class FallingBoulderSpawner : Obstacle
        {
            public float period = 2.33f;

            public float currentTime;

            //protected
            public Track _track;

            public FallingBoulderSpawner() { }

            public FallingBoulderSpawner(SMineCart game)
                : base(game) { }

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(0, 0, 0, 0);
            }

            public override Rectangle GetBounds()
            {
                return new Rectangle(0, 0, 0, 0);
            }

            public override void InitializeObstacle(Track track, Random random)
            {
                _track = track;
                currentTime = (float)random.NextDouble() * period;
                position.Y = -32f;
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                base._Update(time, random, shouldPlaySound);
                currentTime += time;
                if (currentTime >= period)
                {
                    currentTime = 0f;
                    FallingBoulder fallingBoulder = _game.AddEntity(
                        new FallingBoulder(_game),
                        random
                    );
                    fallingBoulder.position = position;
                    fallingBoulder.InitializeObstacle(_track, random);
                }
            }

            public override FallingBoulderSpawner Clone(SMineCart game)
            {
                FallingBoulderSpawner clone = new FallingBoulderSpawner(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is FallingBoulderSpawner o)
                {
                    o.period = period;
                    o.currentTime = currentTime;
                }
            }
        }

        public class WillOWisp : Obstacle
        {
            protected float _age;

            protected Vector2 offset;

            public float tailRotation;

            public float tailLength;

            public float scale = 1f;

            public float nextDebris = 0.1f;

            public WillOWisp() { }

            public WillOWisp(SMineCart game)
                : base(game) { }

            public override Rectangle GetBounds()
            {
                Rectangle bounds = base.GetBounds();
                bounds.X += (int)offset.X;
                bounds.Y += (int)offset.Y;
                return bounds;
            }

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(-5, -5, 10, 10);
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                _age += time;
                Vector2 old_offset = offset;
                float interval = 15f;
                offset.Y = (float)(Math.Sin(_age * interval * (float)Math.PI / 180f) - 1.0) * 32f;
                offset.X = (float)Math.Cos(_age * interval * 3f * (float)Math.PI / 180f) * 64f;
                offset.Y += (float)Math.Sin(_age * interval * 6f * (float)Math.PI / 180f) * 16f;
                Vector2 delta = offset - old_offset;
                tailRotation = (float)Math.Atan2(delta.Y, delta.X);
                tailLength = delta.Length();
                scale = Utility.Lerp(
                    0.5f,
                    0.6f,
                    (float)Math.Sin(_age * 200f * (float)Math.PI / 180f) + 0.5f
                );
                nextDebris -= time;
                if (nextDebris <= 0f)
                {
                    nextDebris = 0.1f;
                    _game
                        .AddEntity(
                            new MineDebris(
                                _game,
                                new Rectangle(192, 96, 16, 16),
                                new Vector2(GetBounds().Center.X, GetBounds().Bottom)
                                    + new Vector2(random.Next(-4, 5), random.Next(-4, 5)),
                                random.Next(-30, 31),
                                random.Next(-30, -19),
                                0.25f,
                                -0.15f,
                                1f,
                                1f,
                                4,
                                0.25f,
                                0.46f
                            ),
                            random
                        )
                        .visible = visible;
                }
            }

            public override bool OnBump(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                Destroy();
                SMineCartGlobal.PlaySound(shouldPlaySound, "ghost");
                for (int i = 0; i < 8; i++)
                {
                    _game.AddEntity(
                        new MineDebris(
                            _game,
                            new Rectangle(192, 96, 16, 16),
                            new Vector2(GetBounds().Center.X, GetBounds().Bottom)
                                + new Vector2(random.Next(-4, 5), random.Next(-4, 5)),
                            random.Next(-50, 51),
                            random.Next(-50, 51),
                            0.25f,
                            -0.15f,
                            1f,
                            1f,
                            4,
                            0.25f,
                            0.28f
                        ),
                        random
                    );
                }
                return base.OnBump(player, random, shouldPlaySound);
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition + offset),
                    new Rectangle(192, 80, 16, 16),
                    Color.White,
                    _age * 200f * ((float)Math.PI / 180f),
                    new Vector2(8f, 8f),
                    _game.GetPixelScale() * scale,
                    SpriteEffects.None,
                    0.27f
                );
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition + offset),
                    new Rectangle(160, 112, 32, 32),
                    Color.White,
                    _age * 60f * ((float)Math.PI / 180f),
                    new Vector2(16f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.29f
                );
                if (_age > 0.25f)
                {
                    Vector2 tail_scale = new Vector2(tailLength, scale);
                    if (tailLength > 0.5f)
                    {
                        SMineCartGlobal.Draw(
                            _game.shouldDraw,
                            b,
                            _game.texture,
                            _game.TransformDraw(base.drawnPosition + offset),
                            new Rectangle(208 + (int)(_age / 0.1f) % 3 * 16, 80, 16, 16),
                            Color.White,
                            tailRotation,
                            new Vector2(16f, 8f),
                            tail_scale * _game.GetPixelScale(),
                            SpriteEffects.None,
                            0.44f
                        );
                    }
                }
            }

            public override WillOWisp Clone(SMineCart game)
            {
                WillOWisp clone = new WillOWisp(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is WillOWisp o)
                {
                    o._age = _age;
                    o.offset = offset;
                    o.tailRotation = tailRotation;
                    o.tailLength = tailLength;
                    o.scale = scale;
                    o.nextDebris = nextDebris;
                }
            }
        }

        public class CosmeticFallingBoulder : FallingBoulder
        {
            private float yBreakPosition;

            private float delayBeforeAppear;

            private Color color;

            public CosmeticFallingBoulder(SMineCart game)
                : base(game) { }

            public CosmeticFallingBoulder(
                SMineCart game,
                float yBreakPosition,
                Color color,
                float fallSpeed = 96f,
                float delayBeforeAppear = 0f
            )
                : base(game)
            {
                this.yBreakPosition = yBreakPosition;
                this.color = color;
                _fallSpeed = fallSpeed;
                this.delayBeforeAppear = delayBeforeAppear;
                if (delayBeforeAppear > 0f)
                {
                    visible = false;
                }
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (delayBeforeAppear > 0f)
                {
                    delayBeforeAppear -= time;
                    if (!(delayBeforeAppear <= 0f))
                    {
                        return;
                    }
                    visible = true;
                }
                _age += time;
                if (position.Y >= yBreakPosition)
                {
                    _currentFallSpeed = -30f;
                    if (IsOnScreen())
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, "hammer");
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        _game
                            .AddEntity(
                                new MineDebris(
                                    _game,
                                    new Rectangle(16, 80, 16, 16),
                                    new Vector2(GetBounds().Center.X, GetBounds().Bottom),
                                    random.Next(-30, 31),
                                    random.Next(-30, -19),
                                    0.25f
                                ),
                                random
                            )
                            .SetColor(_game.caveTint);
                    }
                    _destroyed = true;
                }
                if (_currentFallSpeed < _fallSpeed)
                {
                    _currentFallSpeed += 210f * time;
                    if (_currentFallSpeed > _fallSpeed)
                    {
                        _currentFallSpeed = _fallSpeed;
                    }
                }
                position.Y += time * _currentFallSpeed;
            }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                if (Math.Floor(_age / 0.5f) % 2.0 == 0.0)
                {
                    effect = SpriteEffects.FlipHorizontally;
                }
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(0, 32, 16, 16),
                    color,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    effect,
                    0.15f
                );
            }

            public override CosmeticFallingBoulder Clone(SMineCart game)
            {
                CosmeticFallingBoulder clone = new CosmeticFallingBoulder(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is CosmeticFallingBoulder o)
                {
                    o.yBreakPosition = yBreakPosition;
                    o.delayBeforeAppear = delayBeforeAppear;
                    o.color = color;
                }
            }
        }

        public class NoxiousGas : Obstacle
        {
            protected float _age;

            protected float _currentRiseSpeed;

            protected float _riseSpeed = -90f;

            public NoxiousGas() { }

            public NoxiousGas(SMineCart game)
                : base(game) { }

            public override void OnPlayerReset()
            {
                Destroy();
            }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                if (Math.Floor(_age / 0.5f) % 2.0 == 0.0)
                {
                    effect = SpriteEffects.FlipHorizontally;
                }
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(368, 784, 16, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale() * Utility.Clamp(_age / 0.5f, 0f, 1f),
                    effect,
                    0.44f
                );
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                _age += time;
                if (_currentRiseSpeed > _riseSpeed)
                {
                    _currentRiseSpeed -= 40f * time;
                    if (_currentRiseSpeed < _riseSpeed)
                    {
                        _currentRiseSpeed = _riseSpeed;
                    }
                }
                position.Y += time * _currentRiseSpeed;
            }

            public override bool OnBounce(
                MineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                return false;
            }

            public override bool ShouldReap()
            {
                if (position.Y < -32f)
                {
                    return true;
                }
                return base.ShouldReap();
            }

            public override NoxiousGas Clone(SMineCart game)
            {
                NoxiousGas clone = new NoxiousGas(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is NoxiousGas o)
                {
                    o._age = _age;
                    o._currentRiseSpeed = _currentRiseSpeed;
                    o._riseSpeed = _riseSpeed;
                }
            }
        }

        public class FallingBoulder : Obstacle
        {
            protected float _age;

            protected List<Track> _tracks;

            protected float _currentFallSpeed;

            protected float _fallSpeed = 96f;

            protected bool _wasBouncedOn;

            public FallingBoulder() { }

            public FallingBoulder(SMineCart game)
                : base(game) { }

            public override void OnPlayerReset()
            {
                Destroy();
            }

            public override void InitializeObstacle(Track track, Random random)
            {
                base.InitializeObstacle(track, random);
                List<Track> tracks = _game.GetTracksForXPosition(position.X);
                if (tracks != null)
                {
                    _tracks = new List<Track>(tracks);
                }
            }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                if (Math.Floor(_age / 0.5f) % 2.0 == 0.0)
                {
                    effect = SpriteEffects.FlipHorizontally;
                }
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(0, 32, 16, 16),
                    _game.caveTint,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    effect,
                    0.45f
                );
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                _age += time;
                if (_tracks != null && _tracks.Count > 0)
                {
                    if (_tracks[0] == null)
                    {
                        _tracks.RemoveAt(0);
                    }
                    else if (position.Y >= (float)_tracks[0].GetYAtPoint(position.X))
                    {
                        _currentFallSpeed = -30f;
                        _tracks.RemoveAt(0);
                        if (IsOnScreen())
                        {
                            SMineCartGlobal.PlaySound(shouldPlaySound, "hammer");
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            _game
                                .AddEntity(
                                    new MineDebris(
                                        _game,
                                        new Rectangle(16, 80, 16, 16),
                                        new Vector2(GetBounds().Center.X, GetBounds().Bottom),
                                        random.Next(-30, 31),
                                        random.Next(-30, -19),
                                        0.25f
                                    ),
                                    random
                                )
                                .SetColor(_game.caveTint);
                        }
                    }
                }
                if (_currentFallSpeed < _fallSpeed)
                {
                    _currentFallSpeed += 210f * time;
                    if (_currentFallSpeed > _fallSpeed)
                    {
                        _currentFallSpeed = _fallSpeed;
                    }
                }
                position.Y += time * _currentFallSpeed;
            }

            public override bool OnBounce(
                MineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                if (!(player is PlayerMineCartCharacter))
                {
                    return false;
                }
                _wasBouncedOn = true;
                player.Bounce();
                SMineCartGlobal.PlaySound(shouldPlaySound, "hammer");
                for (int i = 0; i < 3; i++)
                {
                    _game
                        .AddEntity(
                            new MineDebris(
                                _game,
                                new Rectangle(16, 80, 16, 16),
                                new Vector2(GetBounds().Center.X, GetBounds().Top),
                                random.Next(-30, 31),
                                random.Next(-30, -19),
                                0.25f
                            ),
                            random
                        )
                        .SetColor(_game.caveTint);
                }
                return true;
            }

            public override bool OnBump(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                if (_wasBouncedOn)
                {
                    return true;
                }
                return base.OnBump(player, random, shouldPlaySound);
            }

            public override bool ShouldReap()
            {
                if (position.Y > (float)(_game.screenHeight + 32))
                {
                    return true;
                }
                return base.ShouldReap();
            }

            public override FallingBoulder Clone(SMineCart game)
            {
                FallingBoulder clone = new FallingBoulder(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is FallingBoulder o)
                {
                    o._age = _age;
                    o._tracks = _tracks;
                    o._currentFallSpeed = _currentFallSpeed;
                    o._fallSpeed = _fallSpeed;
                    o._wasBouncedOn = _wasBouncedOn;
                }
            }
        }

        public class MineCartSlime : Obstacle
        {
            public MineCartSlime() { }

            public MineCartSlime(SMineCart game)
                : base(game) { }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(0, 32, 16, 16),
                    _game.caveTint,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    effect,
                    0.45f
                );
            }

            public override bool ShouldReap()
            {
                return false;
            }

            public override MineCartSlime Clone(SMineCart game)
            {
                MineCartSlime clone = new MineCartSlime(game);
                CloneOver(clone);
                return clone;
            }
        }

        public class SlimeTrack : Obstacle
        {
            public SlimeTrack() { }

            public SlimeTrack(SMineCart game)
                : base(game) { }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(0, 192, 32, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    effect,
                    0.45f
                );
            }

            public override bool ShouldReap()
            {
                return false;
            }

            public override SlimeTrack Clone(SMineCart game)
            {
                SlimeTrack clone = new SlimeTrack(game);
                CloneOver(clone);
                return clone;
            }
        }

        public class HugeSlime : Obstacle
        {
            protected float _timeUntilHop = 30f;

            protected float _yVelocity;

            protected bool _grounded;

            protected float _lastTrackY = 300f;

            public Vector2 spriteScale = new Vector2(1f, 1f);

            protected int _currentFrame;

            protected Vector2 _desiredScale = new Vector2(1f, 1f);

            protected float _scaleSpeed = 4f;

            protected float _jumpStrength = -200f;

            private bool _hasPeparedToJump;

            public HugeSlime() { }

            public HugeSlime(SMineCart game)
                : base(game) { }

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(-40, -60, 80, 60);
            }

            public override void OnPlayerReset()
            {
                _game.slimeBossPosition =
                    _game.checkpointPosition + (float)_game.slimeResetPosition;
            }

            protected override void _Initialize(Random random)
            {
                base._Initialize(random);
                _game.slimeBossPosition = _game.slimeResetPosition;
                _grounded = false;
            }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                Rectangle source_rect = new Rectangle(160, 176, 96, 80);
                if (_currentFrame == 0)
                {
                    source_rect = new Rectangle(160, 176, 96, 80);
                }
                else if (_currentFrame == 1)
                {
                    source_rect = new Rectangle(160, 256, 96, 80);
                }
                else if (_currentFrame == 2)
                {
                    source_rect = new Rectangle(160, 336, 96, 64);
                }
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    source_rect,
                    Color.White,
                    0f,
                    new Vector2((float)source_rect.Width * 0.5f, source_rect.Height),
                    _game.GetPixelScale() * spriteScale,
                    effect,
                    0.45f
                );
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                Track track = _game.GetTrackForXPosition(position.X);
                float track_height = _game.screenHeight + 32;
                if (track != null)
                {
                    _lastTrackY = track.GetYAtPoint(position.X);
                    track_height = _lastTrackY;
                }
                _game.slimeBossPosition += _game.slimeBossSpeed * time;
                if (_grounded)
                {
                    _timeUntilHop -= time;
                    if (_timeUntilHop <= 0f)
                    {
                        _grounded = false;
                        spriteScale = new Vector2(1.1f, 0.75f);
                        _desiredScale = new Vector2(1f, 1f);
                        _scaleSpeed = 1f;
                        _yVelocity = _jumpStrength;
                        SMineCartGlobal.PlaySound(shouldPlaySound, "dwoop");
                        for (int j = 0; j < 8; j++)
                        {
                            _game.AddEntity(
                                new MineDebris(
                                    _game,
                                    new Rectangle(192, 112, 16, 16),
                                    new Vector2(GetBounds().Center.X, GetBounds().Bottom)
                                        + new Vector2(random.Next(-32, 33), random.Next(-32, 0)),
                                    random.Next(-10, 11),
                                    random.Next(-50, -29),
                                    0.25f,
                                    0.25f,
                                    1f,
                                    1f,
                                    4,
                                    0.25f,
                                    0.46f
                                ),
                                random
                            );
                        }
                    }
                    else if (_timeUntilHop <= 0.25f)
                    {
                        if (!_hasPeparedToJump)
                        {
                            spriteScale = new Vector2(0.9f, 1.1f);
                            _desiredScale = new Vector2(1f, 1f);
                            _scaleSpeed = 1f;
                            _currentFrame = 2;
                            _hasPeparedToJump = true;
                        }
                    }
                    else
                    {
                        _desiredScale = new Vector2(1f, 1f);
                        _scaleSpeed = 4f;
                    }
                }
                else
                {
                    _currentFrame = 1;
                    if (position.X > _game.slimeBossPosition)
                    {
                        position.X = Utility.MoveTowards(
                            position.X,
                            _game.slimeBossPosition,
                            _game.slimeBossSpeed * time * 8f
                        );
                    }
                    else
                    {
                        position.X = Utility.MoveTowards(
                            position.X,
                            _game.slimeBossPosition,
                            _game.slimeBossSpeed * time * 2f
                        );
                    }
                    _yVelocity += 200f * time;
                    position.Y += _yVelocity * time;
                    if (position.Y > _lastTrackY && _yVelocity < 0f)
                    {
                        _yVelocity = _jumpStrength;
                    }
                    if (_yVelocity < 0f)
                    {
                        _desiredScale = new Vector2(0.9f, 1.1f);
                        _scaleSpeed = 5f;
                    }
                    else if (_yVelocity > 0f)
                    {
                        _desiredScale = new Vector2(1f, 1f);
                        _scaleSpeed = 0.25f;
                    }
                    if (position.Y > track_height && _yVelocity > 0f)
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, "slimedead");
                        SMineCartGlobal.PlaySound(shouldPlaySound, "breakingGlass");
                        for (int i = 0; i < 8; i++)
                        {
                            _game.AddEntity(
                                new MineDebris(
                                    _game,
                                    new Rectangle(192, 112, 16, 16),
                                    new Vector2(GetBounds().Center.X, GetBounds().Bottom)
                                        + new Vector2(random.Next(-32, 33), random.Next(-32, 0)),
                                    random.Next(-80, 81),
                                    random.Next(-10, 1),
                                    0.25f,
                                    0.25f,
                                    1f,
                                    1f,
                                    4,
                                    0.25f,
                                    0.46f
                                ),
                                random
                            );
                        }
                        _game.shakeMagnitude = 1.5f;
                        position.Y = track_height;
                        _grounded = true;
                        _timeUntilHop = 0.5f;
                        _currentFrame = 2;
                        _hasPeparedToJump = false;
                        spriteScale = new Vector2(1.1f, 0.75f);
                    }
                }
                spriteScale.X = Utility.MoveTowards(
                    spriteScale.X,
                    _desiredScale.X,
                    _scaleSpeed * time
                );
                spriteScale.Y = Utility.MoveTowards(
                    spriteScale.Y,
                    _desiredScale.Y,
                    _scaleSpeed * time
                );
            }

            public override bool ShouldReap()
            {
                return false;
            }

            public override HugeSlime Clone(SMineCart game)
            {
                HugeSlime slime = new HugeSlime(game);
                CloneOver(slime);
                return slime;
            }

            public override void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
                if (clone is HugeSlime o)
                {
                    o._timeUntilHop = _timeUntilHop;
                    o._yVelocity = _yVelocity;
                    o._grounded = _grounded;
                    o._lastTrackY = _lastTrackY;
                    o.spriteScale = spriteScale;
                    o._currentFrame = _currentFrame;
                    o._desiredScale = _desiredScale;
                    o._scaleSpeed = _scaleSpeed;
                    o._jumpStrength = _jumpStrength;
                    o._hasPeparedToJump = _hasPeparedToJump;
                }
            }
        }

        public class Roadblock : Obstacle
        {
            public Roadblock() { }

            public Roadblock(SMineCart game)
                : base(game) { }

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(-4, -12, 8, 12);
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound) { }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(16, 0, 16, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.45f
                );
            }

            public override bool CanSpawnHere(Track track)
            {
                if (track == null)
                {
                    return false;
                }
                if (track.trackType != 0)
                {
                    return false;
                }
                return true;
            }

            public override bool OnBounce(
                MineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                if (!(player is PlayerMineCartCharacter))
                {
                    return false;
                }
                ShootDebris(random.Next(-10, -4), random.Next(-60, -19), random);
                ShootDebris(random.Next(5, 11), random.Next(-60, -19), random);
                ShootDebris(random.Next(-20, -9), random.Next(-40, 0), random);
                ShootDebris(random.Next(10, 21), random.Next(-40, 0), random);
                SMineCartGlobal.PlaySound(shouldPlaySound, "woodWhack");
                player.velocity.Y = 0f;
                player.velocity.Y = 0f;
                Destroy();
                return true;
            }

            public override bool OnBump(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                ShootDebris(random.Next(10, 41), random.Next(-40, 0), random);
                ShootDebris(random.Next(10, 41), random.Next(-40, 0), random);
                ShootDebris(random.Next(5, 31), random.Next(-60, -19), random);
                ShootDebris(random.Next(5, 31), random.Next(-60, -19), random);
                SMineCartGlobal.PlaySound(shouldPlaySound, "woodWhack");
                Destroy();
                return false;
            }

            public void ShootDebris(int x, int y, Random random)
            {
                _game.AddEntity(
                    new MineDebris(
                        _game,
                        new Rectangle(48, 48, 16, 16),
                        Utility.PointToVector2(GetBounds().Center),
                        x,
                        y,
                        0.25f,
                        1f,
                        1f
                    ),
                    random
                );
            }

            public override Roadblock Clone(SMineCart game)
            {
                Roadblock clone = new Roadblock(game);
                CloneOver(clone);
                return clone;
            }
        }

        public class MineDebris : Entity
        {
            protected Rectangle _sourceRect;

            protected float _dX;

            protected float _dY;

            protected float _age;

            protected float _lifeTime;

            protected float _gravityMultiplier;

            protected float _scale = 1f;

            protected Color _color = Color.White;

            protected int _numAnimationFrames;

            protected bool _holdLastFrame;

            protected float _animationInterval;

            protected int _currentAnimationFrame;

            protected float _animationTimer;

            public float ySinWaveMagnitude;

            public float flipRate;

            public float depth = 0.45f;

            private float timeBeforeDisplay;

            private string destroySound;

            private string startSound;

            public MineDebris(SMineCart game)
                : base(game) { }

            public MineDebris(
                SMineCart game,
                Rectangle source_rect,
                Vector2 spawn_position,
                float dx,
                float dy,
                float flip_rate = 0f,
                float gravity_multiplier = 1f,
                float life_time = 0.5f,
                float scale = 1f,
                int num_animation_frames = 1,
                float animation_interval = 0.1f,
                float draw_depth = 0.45f,
                bool holdLastFrame = false,
                float timeBeforeDisplay = 0f
            )
                : base(game)
            {
                reset(
                    source_rect,
                    spawn_position,
                    dx,
                    dy,
                    flip_rate,
                    gravity_multiplier,
                    life_time,
                    scale,
                    num_animation_frames,
                    animation_interval,
                    draw_depth,
                    holdLastFrame,
                    timeBeforeDisplay
                );
            }

            public void reset(
                Rectangle source_rect,
                Vector2 spawn_position,
                float dx,
                float dy,
                float flip_rate = 0f,
                float gravity_multiplier = 1f,
                float life_time = 0.5f,
                float scale = 1f,
                int num_animation_frames = 1,
                float animation_interval = 0.1f,
                float draw_depth = 0.45f,
                bool holdLastFrame = false,
                float timeBeforeDisplay = 0f
            )
            {
                _sourceRect = source_rect;
                _dX = dx;
                _dY = dy;
                _lifeTime = life_time;
                flipRate = flip_rate;
                position = spawn_position;
                _gravityMultiplier = gravity_multiplier;
                _scale = scale;
                _numAnimationFrames = num_animation_frames;
                _animationInterval = animation_interval;
                depth = draw_depth;
                _holdLastFrame = holdLastFrame;
                _currentAnimationFrame = 0;
                this.timeBeforeDisplay = timeBeforeDisplay;
                if (timeBeforeDisplay > 0f)
                {
                    visible = false;
                }
            }

            public void SetColor(Color color)
            {
                _color = color;
            }

            public void SetDestroySound(string sound)
            {
                destroySound = sound;
            }

            public void SetStartSound(string sound)
            {
                startSound = sound;
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (timeBeforeDisplay > 0f)
                {
                    timeBeforeDisplay -= time;
                    if (!(timeBeforeDisplay <= 0f))
                    {
                        return;
                    }
                    visible = true;
                    if (startSound != null)
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, startSound);
                    }
                }
                position.X += _dX * time;
                position.Y += _dY * time;
                _dY += 210f * time * _gravityMultiplier;
                _age += time;
                if (_age >= _lifeTime)
                {
                    if (destroySound != null)
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, destroySound);
                    }
                    Destroy();
                    return;
                }
                _animationTimer += time;
                if (_animationTimer >= _animationInterval)
                {
                    _animationTimer = 0f;
                    _currentAnimationFrame++;
                    if (_holdLastFrame && _currentAnimationFrame >= _numAnimationFrames - 1)
                    {
                        _currentAnimationFrame = _numAnimationFrames - 1;
                    }
                    else
                    {
                        _currentAnimationFrame %= _numAnimationFrames;
                    }
                }
                base._Update(time, random, shouldPlaySound);
            }

            private Rectangle _GetSourceRect()
            {
                return new Rectangle(
                    _sourceRect.X + _currentAnimationFrame * _sourceRect.Width,
                    _sourceRect.Y,
                    _sourceRect.Width,
                    _sourceRect.Height
                );
            }

            public override void _Draw(SpriteBatch b)
            {
                SpriteEffects effect = SpriteEffects.None;
                if (flipRate > 0f && Math.Floor(_age / flipRate) % 2.0 == 0.0)
                {
                    effect = SpriteEffects.FlipHorizontally;
                }
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(
                        base.drawnPosition
                            + new Vector2(
                                0f,
                                (float)Math.Sin(_game.totalTime + (double)position.X)
                                    * ySinWaveMagnitude
                            )
                    ),
                    _GetSourceRect(),
                    _color,
                    0f,
                    new Vector2((float)_sourceRect.Width / 2f, (float)_sourceRect.Height / 2f),
                    _game.GetPixelScale() * _scale,
                    effect,
                    depth
                );
            }

            public override MineDebris Clone(SMineCart game)
            {
                MineDebris clone = new MineDebris(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is MineDebris debris)
                {
                    debris._sourceRect = _sourceRect;
                    debris._dX = _dX;
                    debris._dY = _dY;
                    debris._age = _age;
                    debris._lifeTime = _lifeTime;
                    debris._gravityMultiplier = _gravityMultiplier;
                    debris._scale = _scale;
                    debris._color = _color;
                    debris._numAnimationFrames = _numAnimationFrames;
                    debris._holdLastFrame = _holdLastFrame;
                    debris._animationInterval = _animationInterval;
                    debris._currentAnimationFrame = _currentAnimationFrame;
                    debris._animationTimer = _animationTimer;
                    debris.ySinWaveMagnitude = ySinWaveMagnitude;
                    debris.flipRate = flipRate;
                    debris.depth = depth;
                    debris.timeBeforeDisplay = timeBeforeDisplay;
                    debris.destroySound = destroySound;
                    debris.startSound = startSound;
                }
            }
        }

        public class Obstacle : Entity, ICollideable
        {
            public Obstacle() { }

            public Obstacle(SMineCart game)
                : base(game) { }

            public virtual void InitializeObstacle(Track track, Random random) { }

            public virtual bool OnBounce(
                MineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                return false;
            }

            public virtual bool OnBump(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                return false;
            }

            public virtual Rectangle GetLocalBounds()
            {
                return new Rectangle(-4, -12, 8, 12);
            }

            public virtual Rectangle GetBounds()
            {
                Rectangle bounds = GetLocalBounds();
                bounds.X += (int)position.X;
                bounds.Y += (int)position.Y;
                return bounds;
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(16, 0, 16, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.45f
                );
            }

            public virtual bool CanSpawnHere(Track track)
            {
                return true;
            }

            public override Obstacle Clone(SMineCart game)
            {
                Obstacle obst = new Obstacle(game);
                base.CloneOver(obst);
                return obst;
            }

            public virtual void CloneOver(Obstacle clone)
            {
                base.CloneOver(clone);
            }
        }

        public class Fruit : Pickup
        {
            protected CollectableFruits _fruitType;

            public override Rectangle GetLocalBounds()
            {
                return new Rectangle(-6, -6, 12, 12);
            }

            public Fruit(SMineCart game)
                : base(game) { }

            public Fruit(SMineCart game, CollectableFruits fruit_type)
                : base(game)
            {
                _fruitType = fruit_type;
            }

            public override void Collect(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                _game.CollectFruit(_fruitType);
                _game.AddEntity(
                    new MineDebris(
                        _game,
                        new Rectangle(0, 250, 5, 5),
                        position,
                        0f,
                        0f,
                        0f,
                        0f,
                        0.6f,
                        1f,
                        6
                    ),
                    random
                );
                for (int i = 0; i < 4; i++)
                {
                    float interval = Utility.Lerp(0.1f, 0.2f, (float)random.NextDouble());
                    _game.AddEntity(
                        new MineDebris(
                            _game,
                            new Rectangle(0, 250, 5, 5),
                            position + new Vector2(random.Next(-8, 9), random.Next(-8, 9)),
                            0f,
                            0f,
                            0f,
                            0f,
                            interval * 6f,
                            1f,
                            6,
                            interval
                        ),
                        random
                    );
                }
                SMineCartGlobal.PlaySound(shouldPlaySound, "eat");
                Destroy();
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(160 + 16 * (int)_fruitType, 0, 16, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 8f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.43f
                );
            }

            public override Fruit Clone(SMineCart game)
            {
                Fruit clone = new Fruit(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Pickup clone)
            {
                base.CloneOver(clone);
                if (clone is Fruit o)
                {
                    o._fruitType = _fruitType;
                }
            }
        }

        public class Coin : Pickup
        {
            public float age;

            public float afterCollectionTimer;

            public bool collected;

            public float flashSpeed = 0.25f;

            public float flashDelay = 0.5f;

            public float collectYDelta;

            public Coin(SMineCart game)
                : base(game) { }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                age += time;
                if (age > flashDelay + flashSpeed * 3f)
                {
                    age = 0f;
                }
                if (collected)
                {
                    afterCollectionTimer += time;
                    if (time > 0f)
                    {
                        position.Y -= 3f - afterCollectionTimer * 8f * time;
                    }
                    if (afterCollectionTimer > 0.4f)
                    {
                        Destroy();
                    }
                }
                base._Update(time, random, shouldPlaySound);
            }

            public override void _Draw(SpriteBatch b)
            {
                int time = (collected ? 450 : 900);
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(9 * ((int)_game.totalTimeMS % time / (time / 12)), 273, 9, 9),
                    Color.White * (1f - afterCollectionTimer / 0.4f),
                    0f,
                    new Vector2(4f, 4f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.45f
                );
            }

            public override void Collect(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                if (!collected)
                {
                    _game.CollectCoin(1);
                    SMineCartGlobal.PlaySound(shouldPlaySound, "junimoKart_coin");
                    _game.AddEntity(
                        new MineDebris(
                            _game,
                            new Rectangle(0, 250, 5, 5),
                            position,
                            0f,
                            0f,
                            0f,
                            0f,
                            0.6f,
                            1f,
                            6
                        ),
                        random
                    );
                    for (int i = 0; i < 4; i++)
                    {
                        float interval = Utility.Lerp(0.1f, 0.2f, (float)random.NextDouble());
                        _game.AddEntity(
                            new MineDebris(
                                _game,
                                new Rectangle(0, 250, 5, 5),
                                position + new Vector2(random.Next(-8, 9), random.Next(-8, 9)),
                                0f,
                                0f,
                                0f,
                                0f,
                                interval * 6f,
                                1f,
                                6,
                                interval
                            ),
                            random
                        );
                    }
                    collectYDelta = -3f;
                    collected = true;
                }
            }

            public override Coin Clone(SMineCart game)
            {
                Coin clone = new Coin(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Pickup clone)
            {
                base.CloneOver(clone);
                if (clone is Coin o)
                {
                    o.age = age;
                    o.afterCollectionTimer = afterCollectionTimer;
                    o.collected = collected;
                    o.flashSpeed = flashSpeed;
                    o.flashDelay = flashDelay;
                    o.collectYDelta = collectYDelta;
                }
            }
        }

        public class Pickup : Entity, ICollideable
        {
            public Pickup(SMineCart game)
                : base(game) { }

            public virtual Rectangle GetLocalBounds()
            {
                return new Rectangle(-4, -4, 8, 8);
            }

            public virtual Rectangle GetBounds()
            {
                Rectangle bounds = GetLocalBounds();
                bounds.X += (int)position.X;
                bounds.Y += (int)position.Y;
                return bounds;
            }

            public override void _Draw(SpriteBatch b)
            {
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(16, 16, 16, 16),
                    Color.White,
                    0f,
                    new Vector2(8f, 8f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.45f
                );
            }

            public virtual void Collect(
                PlayerMineCartCharacter player,
                Random random,
                bool shouldPlaySound
            )
            {
                SMineCartGlobal.PlaySound(shouldPlaySound, "Pickup_Coin15");
                Destroy();
            }

            public virtual void CloneOver(Pickup clone)
            {
                base.CloneOver(clone);
            }
        }

        public class BalanceTrack : Track
        {
            public List<BalanceTrack> connectedTracks;

            public List<BalanceTrack> counterBalancedTracks;

            public float startY;

            public float moveSpeed = 128f;

            public BalanceTrack(SMineCart game, TrackType type, bool showSecondTile)
                : base(game, type, showSecondTile)
            {
                connectedTracks = new List<BalanceTrack>();
                counterBalancedTracks = new List<BalanceTrack>();
            }

            public override void OnPlayerReset()
            {
                position.Y = startY;
            }

            public override void WhileCartGrounded(MineCartCharacter character, float time)
            {
                foreach (BalanceTrack connectedTrack in connectedTracks)
                {
                    connectedTrack.position.Y += moveSpeed * time;
                }
                foreach (BalanceTrack counterBalancedTrack in counterBalancedTracks)
                {
                    counterBalancedTrack.position.Y -= moveSpeed * time;
                }
            }

            public override BalanceTrack Clone(SMineCart game)
            {
                BalanceTrack clone = new BalanceTrack(game, trackType, _showSecondTile);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Track clone)
            {
                base.CloneOver(clone);
                if (clone is BalanceTrack o)
                {
                    o.connectedTracks = new List<BalanceTrack>();
                    for (int i = 0; i < connectedTracks.Count; i++)
                    {
                        o.connectedTracks.Add(connectedTracks[i].Clone(o._game));
                    }
                    o.counterBalancedTracks = new List<BalanceTrack>();
                    for (int j = 0; j < counterBalancedTracks.Count; j++)
                    {
                        o.counterBalancedTracks.Add(counterBalancedTracks[j].Clone(o._game));
                    }
                    o.startY = startY;
                    o.moveSpeed = moveSpeed;
                }
            }
        }

        public class Track : Entity
        {
            public enum TrackType
            {
                None = -1,
                Straight = 0,
                UpSlope = 2,
                DownSlope = 3,
                IceDownSlope = 4,
                SlimeUpSlope = 5,
                MushroomLeft = 6,
                MushroomMiddle = 7,
                MushroomRight = 8
            }

            public Obstacle obstacle;

            protected bool _showSecondTile;

            public TrackType trackType;

            public Track(SMineCart game)
                : base(game) { }

            public Track(SMineCart game, TrackType type, bool showSecondTile)
                : base(game)
            {
                trackType = type;
                _showSecondTile = showSecondTile;
            }

            public virtual void WhileCartGrounded(MineCartCharacter character, float time) { }

            public override void _Draw(SpriteBatch b)
            {
                if (trackType == TrackType.SlimeUpSlope)
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            new Vector2(base.drawnPosition.X, base.drawnPosition.Y - 32f)
                        ),
                        new Rectangle(192, 144, 16, 32),
                        _game.trackTint,
                        0f,
                        Vector2.Zero,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.5f + base.drawnPosition.Y * 1E-05f
                    );
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            new Vector2(base.drawnPosition.X, base.drawnPosition.Y - 32f)
                        ),
                        new Rectangle(160 + (int)trackType * 16, 144, 16, 32),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.5f + base.drawnPosition.Y * 1E-05f - 0.0001f
                    );
                }
                else if (
                    trackType >= TrackType.MushroomLeft
                    && trackType <= TrackType.MushroomRight
                )
                {
                    if (GetType() == typeof(Track))
                    {
                        SMineCartGlobal.Draw(
                            _game.shouldDraw,
                            b,
                            _game.texture,
                            _game.TransformDraw(
                                new Vector2(base.drawnPosition.X, base.drawnPosition.Y - 32f)
                            ),
                            new Rectangle(304 + (int)(trackType - 6) * 16, 736, 16, 48),
                            Color.White,
                            0f,
                            Vector2.Zero,
                            _game.GetPixelScale(),
                            SpriteEffects.None,
                            0.5f + base.drawnPosition.Y * 1E-05f
                        );
                    }
                    else
                    {
                        SMineCartGlobal.Draw(
                            _game.shouldDraw,
                            b,
                            _game.texture,
                            _game.TransformDraw(
                                new Vector2(base.drawnPosition.X, base.drawnPosition.Y - 32f)
                            ),
                            new Rectangle(352 + (int)(trackType - 6) * 16, 736, 16, 48),
                            Color.White,
                            0f,
                            Vector2.Zero,
                            _game.GetPixelScale(),
                            SpriteEffects.None,
                            0.5f + base.drawnPosition.Y * 1E-05f
                        );
                    }
                }
                else if (
                    _game.currentTheme == 4
                    && (trackType == TrackType.UpSlope || trackType == TrackType.DownSlope)
                )
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            new Vector2(base.drawnPosition.X, base.drawnPosition.Y - 32f)
                        ),
                        new Rectangle(256 + (int)(trackType - 2) * 16, 144, 16, 32),
                        _game.trackTint,
                        0f,
                        Vector2.Zero,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.5f + base.drawnPosition.Y * 1E-05f
                    );
                }
                else
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            new Vector2(base.drawnPosition.X, base.drawnPosition.Y - 32f)
                        ),
                        new Rectangle(160 + (int)trackType * 16, 144, 16, 32),
                        _game.trackTint,
                        0f,
                        Vector2.Zero,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.5f + base.drawnPosition.Y * 1E-05f
                    );
                }
                if (trackType == TrackType.MushroomLeft || trackType == TrackType.MushroomRight)
                {
                    return;
                }
                float darkness = 0f;
                if (trackType == TrackType.MushroomMiddle)
                {
                    for (
                        float y2 = base.drawnPosition.Y;
                        y2 < (float)_game.screenHeight;
                        y2 += (float)(_game.tileSize * 4)
                    )
                    {
                        SMineCartGlobal.Draw(
                            _game.shouldDraw,
                            b,
                            _game.texture,
                            _game.TransformDraw(new Vector2(base.drawnPosition.X, y2 + 16f)),
                            new Rectangle(320, 784, 16, 64),
                            Color.White,
                            0f,
                            Vector2.Zero,
                            _game.GetPixelScale(),
                            SpriteEffects.None,
                            0.5f + base.drawnPosition.Y * 1E-05f + 0.01f
                        );
                        SMineCartGlobal.Draw(
                            _game.shouldDraw,
                            b,
                            _game.texture,
                            _game.TransformDraw(new Vector2(base.drawnPosition.X, y2 + 16f)),
                            new Rectangle(368, 784, 16, 64),
                            _game.trackShadowTint * darkness,
                            0f,
                            Vector2.Zero,
                            _game.GetPixelScale(),
                            SpriteEffects.None,
                            0.5f + base.drawnPosition.Y * 1E-05f + 0.005f
                        );
                        darkness += 0.1f;
                    }
                    return;
                }
                bool flipper = _showSecondTile;
                for (
                    float y = base.drawnPosition.Y;
                    y < (float)_game.screenHeight;
                    y += (float)_game.tileSize
                )
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(new Vector2(base.drawnPosition.X, y)),
                        (_game.currentTheme == 4)
                            ? new Rectangle(16 + (flipper ? 1 : 0) * 16, 160, 16, 16)
                            : new Rectangle(16 + (flipper ? 1 : 0) * 16, 32, 16, 16),
                        _game.trackTint,
                        0f,
                        Vector2.Zero,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.5f + base.drawnPosition.Y * 1E-05f + 0.01f
                    );
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(new Vector2(base.drawnPosition.X, y)),
                        (_game.currentTheme == 4)
                            ? new Rectangle(16 + (flipper ? 1 : 0) * 16, 160, 16, 16)
                            : new Rectangle(16 + (flipper ? 1 : 0) * 16, 32, 16, 16),
                        _game.trackShadowTint * darkness,
                        0f,
                        Vector2.Zero,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.5f + base.drawnPosition.Y * 1E-05f + 0.005f
                    );
                    darkness += 0.1f;
                    flipper = !flipper;
                }
            }

            public bool CanLandHere(Vector2 test_position)
            {
                int track_y = GetYAtPoint(test_position.X);
                if (
                    test_position.Y >= (float)(track_y - 2)
                    && test_position.Y <= (float)(track_y + 8)
                )
                {
                    return true;
                }
                return false;
            }

            public int GetYAtPoint(float x)
            {
                int local_x = (int)(x - position.X);
                if (trackType == TrackType.UpSlope)
                {
                    return (int)(position.Y - 2f - (float)local_x);
                }
                if (trackType == TrackType.DownSlope)
                {
                    return (int)(position.Y - 2f - 16f + (float)local_x);
                }
                if (trackType == TrackType.IceDownSlope)
                {
                    return (int)(position.Y - 2f - 16f + (float)local_x);
                }
                if (trackType == TrackType.SlimeUpSlope)
                {
                    return (int)(position.Y - 2f - (float)local_x);
                }
                return (int)(position.Y - 2f);
            }

            public override Track Clone(SMineCart game)
            {
                Track new_track = new(game);
                CloneOver(new_track);
                return new_track;
            }

            public virtual void CloneOver(Track clone)
            {
                base.CloneOver(clone);
                clone.trackType = trackType;
                clone._showSecondTile = _showSecondTile;
                if (obstacle != null)
                {
                    clone.obstacle = obstacle.Clone(clone._game);
                }
            }
        }

        public class PlayerMineCartCharacter : MineCartCharacter, ICollideable
        {
            public PlayerMineCartCharacter(SMineCart game)
                : base(game) { }

            public Rectangle GetLocalBounds()
            {
                return new Rectangle(-4, -12, 8, 12);
            }

            public virtual Rectangle GetBounds()
            {
                Rectangle bounds = GetLocalBounds();
                bounds.X += (int)position.X;
                bounds.Y += (int)position.Y;
                return bounds;
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (!IsActive())
                {
                    return;
                }
                int old_x_pos = (int)(position.X / (float)_game.tileSize);
                float old_y_velocity = velocity.Y;
                if (
                    _game.gameState != GameStates.Cutscene
                    && _jumping
                    && !_game.isJumpPressed
                    && !_game.gamePaused
                )
                {
                    ReleaseJump();
                }
                base._Update(time, random, shouldPlaySound);
                if (_grounded && _game.respawnCounter <= 0)
                {
                    if (_game.minecartLoop.IsPaused && _game.currentTheme != 7)
                    {
                        _game.minecartLoop.Resume();
                    }
                    if (old_x_pos != (int)(position.X / (float)_game.tileSize) && random.NextBool())
                    {
                        minecartBumpOffset = -random.Next(1, 3);
                    }
                }
                else if (!_grounded)
                {
                    if (!_game.minecartLoop.IsPaused)
                    {
                        _game.minecartLoop.Pause();
                    }
                    minecartBumpOffset = 0f;
                }
                minecartBumpOffset = Utility.MoveTowards(minecartBumpOffset, 0f, time * 20f);
                foreach (Pickup overlap in _game.GetOverlaps<Pickup>(this))
                {
                    overlap.Collect(this, random, shouldPlaySound);
                }
                Obstacle obstacle = _game.GetOverlap<Obstacle>(this);
                if (
                    _game.GetOverlap<Obstacle>(this) != null
                    && (
                        (
                            !(velocity.Y > 0f)
                            && !(old_y_velocity > 0f)
                            && !(position.Y < obstacle.position.Y - 1f)
                        ) || !obstacle.OnBounce(this, random, shouldPlaySound)
                    )
                    && !obstacle.OnBump(this, random, shouldPlaySound)
                )
                {
                    _game.Die();
                }
            }

            public override void OnJump(bool shouldPlaySound)
            {
                SMineCartGlobal.PlaySound(shouldPlaySound, "pickUpItem", 200);
            }

            public override void OnFall(bool shouldPlaySound)
            {
                SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
                _game.createSparkShower();
            }

            public override void OnLand(Random random, bool shouldPlaySound)
            {
                if (currentTrackType == Track.TrackType.SlimeUpSlope)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "slimeHit");
                }
                else
                {
                    if (
                        currentTrackType >= Track.TrackType.MushroomLeft
                        && currentTrackType <= Track.TrackType.MushroomRight
                    )
                    {
                        SMineCartGlobal.PlaySound(shouldPlaySound, "slimeHit");
                        bool purple = false;
                        if (GetTrack().GetType() != typeof(Track))
                        {
                            purple = true;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            _game.AddEntity(
                                new MineDebris(
                                    _game,
                                    new Rectangle(362 + (purple ? 5 : 0), 802, 5, 4),
                                    position,
                                    random.Next(-30, 31),
                                    random.Next(-50, -39),
                                    0f,
                                    1f,
                                    0.75f,
                                    1f,
                                    1,
                                    1f,
                                    0.15f
                                ),
                                random
                            );
                        }
                        return;
                    }
                    SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
                }
                _game.createSparkShower();
            }

            public override void OnTrackChange(bool shouldPlaySound)
            {
                if (_hasJustSnapped || !_grounded)
                {
                    return;
                }
                if (currentTrackType == Track.TrackType.SlimeUpSlope)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "slimeHit");
                }
                else
                {
                    if (
                        currentTrackType >= Track.TrackType.MushroomLeft
                        && currentTrackType <= Track.TrackType.MushroomRight
                    )
                    {
                        return;
                    }
                    SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
                }
                _game.createSparkShower();
            }

            public override MineCartCharacter Clone(SMineCart game)
            {
                PlayerMineCartCharacter clone = new PlayerMineCartCharacter(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(MineCartCharacter clone)
            {
                base.CloneOver(clone);
            }
        }

        public class CheckpointIndicator : Entity
        {
            public const int CENTER_TO_POST_BASE_OFFSET = 5;

            public float rotation;

            protected bool _activated;

            public float swayRotation = 120f;

            public float swayTimer;

            public CheckpointIndicator(SMineCart game)
                : base(game) { }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (!_activated)
                {
                    return;
                }
                swayTimer += time * ((float)Math.PI * 2f);
                if ((double)swayTimer >= Math.PI * 2.0)
                {
                    swayTimer = 0f;
                    swayRotation -= 20f;
                    if (swayRotation <= 30f)
                    {
                        swayRotation = 30f;
                    }
                }
                rotation = (float)Math.Sin(swayTimer) * swayRotation;
            }

            public void Activate(bool shouldPlaySound)
            {
                if (!_activated)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "fireball");
                    _activated = true;
                }
            }

            public override void _Draw(SpriteBatch b)
            {
                float rad_rotation = rotation * (float)Math.PI / 180f;
                Vector2 lantern_offset = new Vector2(0f, -12f);
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(16, 112, 16, 16),
                    _game.trackTint,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.31f
                );
                if (_activated)
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(base.drawnPosition + lantern_offset),
                        new Rectangle(48, 112, 16, 16),
                        Color.White,
                        rad_rotation,
                        new Vector2(8f, 16f) + lantern_offset,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.3f
                    );
                }
                else
                {
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(base.drawnPosition + lantern_offset),
                        new Rectangle(32, 112, 16, 16),
                        Color.White,
                        rad_rotation,
                        new Vector2(8f, 16f) + lantern_offset,
                        _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.3f
                    );
                }
            }

            public override CheckpointIndicator Clone(SMineCart game)
            {
                CheckpointIndicator clone = new CheckpointIndicator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is CheckpointIndicator indicator)
                {
                    indicator.rotation = rotation;
                    indicator._activated = _activated;
                    indicator.swayRotation = swayRotation;
                    indicator.swayTimer = swayTimer;
                }
            }
        }

        public class GoalIndicator : Entity
        {
            public float rotation;

            protected bool _activated;

            public GoalIndicator(SMineCart game)
                : base(game) { }

            public void Activate()
            {
                if (!_activated)
                {
                    _activated = true;
                }
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (_activated)
                {
                    rotation += time * 360f / 0.25f;
                }
            }

            public override void _Draw(SpriteBatch b)
            {
                float rad_rotation = rotation * (float)Math.PI / 180f;
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition),
                    new Rectangle(16, 128, 16, 16),
                    _game.trackTint,
                    0f,
                    new Vector2(8f, 16f),
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.31f
                );
                Vector2 sign_offset = new Vector2(0f, -8f);
                SMineCartGlobal.Draw(
                    _game.shouldDraw,
                    b,
                    _game.texture,
                    _game.TransformDraw(base.drawnPosition + sign_offset),
                    new Rectangle(32, 128, 16, 16),
                    Color.White,
                    rad_rotation,
                    new Vector2(8f, 16f) + sign_offset,
                    _game.GetPixelScale(),
                    SpriteEffects.None,
                    0.3f
                );
            }

            public override GoalIndicator Clone(SMineCart game)
            {
                GoalIndicator clone = new GoalIndicator(game);
                CloneOver(clone);
                return clone;
            }

            public override void CloneOver(Entity clone)
            {
                base.CloneOver(clone);
                if (clone is GoalIndicator indicator)
                {
                    indicator.rotation = rotation;
                    indicator._activated = _activated;
                }
            }
        }

        public class MineCartCharacter : BaseCharacter
        {
            public float minecartBumpOffset;

            public float jumpStrength = 300f;

            public float maxFallSpeed = 150f;

            public float jumpGravity = 3400f;

            public float fallGravity = 3000f;

            public float jumpFloatDuration = 0.1f;

            public float gravity;

            protected float _jumpBuffer;

            protected float _jumpFloatAge;

            protected float _speedMultiplier = 1f;

            protected float _jumpMomentumThreshhold = -30f;

            public float jumpGracePeriod;

            protected bool _grounded = true;

            protected bool _jumping;

            public float rotation;

            public Vector2 cartScale = Vector2.One;

            public Track.TrackType currentTrackType = Track.TrackType.None;

            public float characterExtraHeight;

            protected bool _hasJustSnapped;

            public float forcedJumpTime;

            public MineCartCharacter(SMineCart game)
                : base(game) { }

            public void QueueJump()
            {
                _jumpBuffer = 0.25f;
            }

            public virtual void OnDie()
            {
                cartScale = Vector2.One;
                _speedMultiplier = 1f;
            }

            public void SnapToFloor()
            {
                List<Track> position_tracks = _game.GetTracksForXPosition(position.X);
                if (position_tracks != null)
                {
                    int i = 0;
                    if (i < position_tracks.Count)
                    {
                        Track track = position_tracks[i];
                        position.Y = track.GetYAtPoint(position.X);
                        _grounded = true;
                        gravity = 0f;
                        velocity.Y = 0f;
                        characterExtraHeight = 0f;
                        minecartBumpOffset = 0f;
                        _hasJustSnapped = true;
                    }
                }
            }

            public Track GetTrack(Vector2 offset = default(Vector2))
            {
                int[] offsets = new int[3] { 0, 4, -4 };
                foreach (int x_offset in offsets)
                {
                    Vector2 test_position = position + offset + new Vector2(x_offset, 0f);
                    List<Track> tracks = _game.GetTracksForXPosition(test_position.X);
                    if (tracks == null)
                    {
                        continue;
                    }
                    for (int j = 0; j < tracks.Count; j++)
                    {
                        if (tracks[j].CanLandHere(test_position))
                        {
                            return tracks[j];
                        }
                    }
                }
                return null;
            }

            protected override void _Update(float time, Random random, bool shouldPlaySound)
            {
                if (_game.respawnCounter > 0)
                {
                    characterExtraHeight = 0f;
                    rotation = 0f;
                    _jumpBuffer = 0f;
                    jumpGracePeriod = 0f;
                    gravity = 0f;
                    velocity.Y = 0f;
                    minecartBumpOffset = 0f;
                    SnapToFloor();
                    return;
                }
                base._Update(time, random, shouldPlaySound);
                if (jumpGracePeriod > 0f)
                {
                    jumpGracePeriod -= time;
                }
                if ((_grounded || jumpGracePeriod > 0f) && _jumpBuffer > 0f && _game.isJumpPressed)
                {
                    _jumpBuffer = 0f;
                    Jump(shouldPlaySound);
                }
                else if (_jumpBuffer > 0f)
                {
                    _jumpBuffer -= time;
                }
                bool found_valid_ground = false;
                Track.TrackType old_track_type = currentTrackType;
                Track track = GetTrack();
                if (track != null && _grounded)
                {
                    track.WhileCartGrounded(this, time);
                }
                bool grounded = _grounded;
                if (velocity.Y >= 0f && track != null)
                {
                    position.Y = track.GetYAtPoint(position.X);
                    currentTrackType = track.trackType;
                    if (!_grounded)
                    {
                        cartScale = new Vector2(1.5f, 0.5f);
                        rotation = 0f;
                        OnLand(random, shouldPlaySound);
                    }
                    found_valid_ground = true;
                    velocity.Y = 0f;
                    _grounded = true;
                }
                else if (_grounded && velocity.Y >= 0f)
                {
                    track = GetTrack(new Vector2(0f, 2f));
                    if (track != null)
                    {
                        position.Y = track.GetYAtPoint(position.X);
                        currentTrackType = track.trackType;
                        found_valid_ground = true;
                        velocity.Y = 0f;
                        _grounded = true;
                    }
                }
                if (!found_valid_ground)
                {
                    if (_grounded)
                    {
                        gravity = 0f;
                        velocity.Y = GetMaxFallSpeed();
                        if (!IsJumping())
                        {
                            OnFall(shouldPlaySound);
                            jumpGracePeriod = maxJumpGraceTime;
                        }
                    }
                    currentTrackType = Track.TrackType.None;
                    _grounded = false;
                }
                float ground_rotation = 0f;
                if (currentTrackType == Track.TrackType.Straight)
                {
                    ground_rotation = 0f;
                }
                else if (currentTrackType == Track.TrackType.UpSlope)
                {
                    ground_rotation = -45f;
                }
                else if (currentTrackType == Track.TrackType.DownSlope)
                {
                    ground_rotation = 30f;
                }
                if (IsJumping())
                {
                    rotation = Utility.MoveTowards(rotation, -45f, 300f * time);
                    characterExtraHeight = 0f;
                }
                else if (!_grounded)
                {
                    rotation = Utility.MoveTowards(rotation, 0f, 100f * time);
                    characterExtraHeight = Utility.MoveTowards(
                        characterExtraHeight,
                        16f,
                        24f * time
                    );
                }
                else
                {
                    rotation = Utility.MoveTowards(rotation, ground_rotation, 360f * time);
                    characterExtraHeight = Utility.MoveTowards(
                        characterExtraHeight,
                        0f,
                        128f * time
                    );
                }
                cartScale.X = Utility.MoveTowards(cartScale.X, 1f, 4f * time);
                cartScale.Y = Utility.MoveTowards(cartScale.Y, 1f, 4f * time);
                if (grounded && old_track_type != currentTrackType)
                {
                    if (
                        (rotation < 0f && ground_rotation > 0f)
                        || (rotation > 0f && ground_rotation < 0f)
                    )
                    {
                        rotation = 0f;
                    }
                    OnTrackChange(shouldPlaySound);
                }
                if (forcedJumpTime > 0f)
                {
                    forcedJumpTime -= time;
                    if (_grounded)
                    {
                        forcedJumpTime = 0f;
                    }
                }
                if (!_grounded)
                {
                    if (_jumping)
                    {
                        _jumpFloatAge += time;
                        if (_jumpFloatAge < jumpFloatDuration)
                        {
                            gravity = 0f;
                            velocity.Y = Utility.Lerp(
                                0f,
                                0f - jumpStrength,
                                _jumpFloatAge / jumpFloatDuration
                            );
                        }
                        else if (velocity.Y <= _jumpMomentumThreshhold * 2f)
                        {
                            gravity += time * jumpGravity;
                        }
                        else
                        {
                            velocity.Y = _jumpMomentumThreshhold;
                            ReleaseJump();
                        }
                    }
                    else
                    {
                        gravity += time * fallGravity;
                    }
                    velocity.Y += time * gravity;
                }
                else
                {
                    _jumping = false;
                }
                if (_game.currentTheme == 5)
                {
                    _speedMultiplier = 1f;
                }
                if (currentTrackType == Track.TrackType.SlimeUpSlope)
                {
                    _speedMultiplier = 0.5f;
                }
                else if (currentTrackType == Track.TrackType.IceDownSlope)
                {
                    _speedMultiplier = Utility.MoveTowards(_speedMultiplier, 3f, time * 2f);
                }
                else if (_grounded)
                {
                    _speedMultiplier = Utility.MoveTowards(_speedMultiplier, 1f, time * 6f);
                }
                if (!(this is PlayerMineCartCharacter))
                {
                    _speedMultiplier = 1f;
                }
                position.X += time * velocity.X * _speedMultiplier;
                position.Y += time * velocity.Y;
                if (velocity.Y > 0f)
                {
                    _jumping = false;
                }
                if (velocity.Y > GetMaxFallSpeed())
                {
                    velocity.Y = GetMaxFallSpeed();
                }
                if (_hasJustSnapped)
                {
                    _hasJustSnapped = false;
                }
            }

            public float GetMaxFallSpeed()
            {
                if (_game.currentTheme == 2)
                {
                    return 75f;
                }
                return maxFallSpeed;
            }

            public virtual void OnLand(Random random, bool shouldPlaySound) { }

            public virtual void OnTrackChange(bool shouldPlaySound) { }

            public virtual void OnFall(bool shouldPlaySound) { }

            public virtual void OnJump(bool shouldPlaySound) { }

            public void ReleaseJump()
            {
                if (!(forcedJumpTime > 0f) && _jumping && velocity.Y < 0f)
                {
                    _jumping = false;
                    gravity = 0f;
                    if (velocity.Y < _jumpMomentumThreshhold)
                    {
                        velocity.Y = _jumpMomentumThreshhold;
                    }
                }
            }

            public bool IsJumping()
            {
                return _jumping;
            }

            public bool IsGrounded()
            {
                return _grounded;
            }

            public void Bounce(float forced_bounce_time = 0f)
            {
                forcedJumpTime = forced_bounce_time;
                _jumping = true;
                gravity = 0f;
                cartScale = new Vector2(0.5f, 1.5f);
                velocity.Y = 0f - jumpStrength;
                _grounded = false;
            }

            public void Jump(bool shouldPlaySound)
            {
                if (_grounded || jumpGracePeriod > 0f)
                {
                    _jumping = true;
                    gravity = 0f;
                    _jumpFloatAge = 0f;
                    cartScale = new Vector2(0.5f, 1.5f);
                    OnJump(shouldPlaySound);
                    velocity.Y = 0f - jumpStrength;
                    _grounded = false;
                }
            }

            public void ForceGrounded()
            {
                _grounded = true;
                gravity = 0f;
                velocity.Y = 0f;
            }

            public override void _Draw(SpriteBatch b)
            {
                if (_game.respawnCounter / 200 % 2 == 0)
                {
                    float rad_rotation = rotation * (float)Math.PI / 180f;
                    Vector2 right = new Vector2(
                        (float)Math.Cos(rad_rotation),
                        0f - (float)Math.Sin(rad_rotation)
                    );
                    Vector2 up = new Vector2(
                        (float)Math.Sin(rad_rotation),
                        0f - (float)Math.Cos(rad_rotation)
                    );
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            base.drawnPosition + up * (0f - minecartBumpOffset) + up * 4f
                        ),
                        new Rectangle(0, 0, 16, 16),
                        Color.White,
                        rad_rotation,
                        new Vector2(8f, 14f),
                        cartScale * _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.45f
                    );
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        _game.texture,
                        _game.TransformDraw(
                            base.drawnPosition + up * (0f - minecartBumpOffset) + up * 4f
                        ),
                        new Rectangle(0, 16, 16, 16),
                        Color.White,
                        rad_rotation,
                        new Vector2(8f, 14f),
                        cartScale * _game.GetPixelScale(),
                        SpriteEffects.None,
                        0.4f
                    );
                    SMineCartGlobal.Draw(
                        _game.shouldDraw,
                        b,
                        Game1.mouseCursors,
                        _game.TransformDraw(
                            base.drawnPosition
                                + right * -2f
                                + up * (0f - minecartBumpOffset)
                                + up * 12f
                                + new Vector2(0f, 0f - characterExtraHeight)
                        ),
                        new Rectangle(
                            294 + (int)(_game.totalTimeMS % 400.0) / 100 * 16,
                            1432,
                            16,
                            16
                        ),
                        Color.Lime,
                        0f,
                        new Vector2(8f, 8f),
                        _game.GetPixelScale() * 2f / 3f,
                        SpriteEffects.None,
                        0.425f
                    );
                }
            }

            public override MineCartCharacter Clone(SMineCart game)
            {
                MineCartCharacter character = new(game);
                CloneOver(character);
                return character;
            }

            public virtual void CloneOver(MineCartCharacter clone)
            {
                base.CloneOver(clone);
                clone.minecartBumpOffset = minecartBumpOffset;
                clone.jumpStrength = jumpStrength;
                clone.maxFallSpeed = maxFallSpeed;
                clone.jumpGravity = jumpGravity;
                clone.fallGravity = fallGravity;
                clone.jumpFloatDuration = jumpFloatDuration;
                clone.gravity = gravity;
                clone._jumpBuffer = _jumpBuffer;
                clone._jumpFloatAge = _jumpFloatAge;
                clone._speedMultiplier = _speedMultiplier;
                clone._jumpMomentumThreshhold = _jumpMomentumThreshhold;
                clone.jumpGracePeriod = jumpGracePeriod;
                clone._grounded = _grounded;
                clone._jumping = _jumping;
                clone.rotation = rotation;
                clone.cartScale = cartScale;
                clone.currentTrackType = currentTrackType;
                clone.characterExtraHeight = characterExtraHeight;
                clone._hasJustSnapped = _hasJustSnapped;
                clone.forcedJumpTime = forcedJumpTime;
            }
        }

        public GameStates gameState;

        public const int followDistance = 96;

        public float pixelScale = 4f;

        public const int tilesBeyondViewportToSimulate = 4;

        public const int bgLoopWidth = 96;

        public const float gravity = 0.21f;

        public const int brownArea = 0;

        public const int frostArea = 1;

        public const int darkArea = 3;

        public const int waterArea = 2;

        public const int lavaArea = 4;

        public const int heavenlyArea = 5;

        public const int sunsetArea = 6;

        public const int endingCutscene = 7;

        public const int bonusLevel1 = 8;

        public const int mushroomArea = 9;

        public const int LAST_LEVEL = 6;

        public readonly int[] infiniteModeLevels = new int[8] { 0, 1, 2, 3, 5, 9, 4, 6 };

        public float shakeMagnitude;

        // protected
        public Vector2 _shakeOffset = Vector2.Zero;

        public const int infiniteMode = 2;

        public const int progressMode = 3;

        public const int respawnTime = 1400;

        /// <summary>How long the player can jump after running off the track, measured in seconds.</summary>
        public static float maxJumpGraceTime = 0.1f;

        public float slimeBossPosition = -100f;

        public float slimeBossSpeed;

        public float secondsOnThisLevel;

        public int fruitEatCount;

        public int currentFruitCheckIndex = -1;

        public float currentFruitCheckMagnitude;

        public const int checkpointScanDistance = 16;

        public int coinCount;

        public bool gamePaused;

        private SparklingText perfectText;

        private float lakeSpeedAccumulator;

        private float backBGPosition;

        private float midBGPosition;

        private float waterFallPosition;

        public Vector2 upperLeft;

        private TASStopWatch musicSW;

        private bool titleJunimoStartedBobbing;

        private bool lastLevelWasPerfect;

        private bool completelyPerfect = true;

        private int screenWidth;

        private int screenHeight;

        public int tileSize;

        private int waterfallWidth = 1;

        private int ytileOffset;

        //private
        public int score;

        private int levelsBeat;

        private int gameMode;

        private int livesLeft;

        private int distanceToTravel = -1;

        private int respawnCounter;

        private int currentTheme;

        //private
        public bool reachedFinish;

        //private
        public bool gameOver;

        private float screenDarkness;

        protected string cutsceneText = "";

        public float fadeDelta;

        private ICue minecartLoop;

        private Texture2D texture;

        private Dictionary<int, List<Track>> _tracks;

        private List<LakeDecor> lakeDecor = new List<LakeDecor>();

        private List<Point> obstacles = new List<Point>();

        private List<Spark> sparkShower = new List<Spark>();

        private List<int> levelThemesFinishedThisRun = new List<int>();

        private Color backBGTint;

        private Color midBGTint;

        private Color caveTint;

        private Color lakeTint;

        private Color waterfallTint;

        private Color trackShadowTint;

        private Color trackTint;

        private Rectangle midBGSource = new Rectangle(64, 0, 96, 162);

        private Rectangle backBGSource = new Rectangle(64, 162, 96, 111);

        private Rectangle lakeBGSource = new Rectangle(0, 80, 16, 97);

        private int backBGYOffset;

        private int midBGYOffset;

        protected double _totalTime;

        //private
        public MineCartCharacter player;

        //private
        public MineCartCharacter trackBuilderCharacter;

        private MineDebris titleScreenJunimo;

        private List<Entity> _entities;

        public LevelTransition[] LEVEL_TRANSITIONS;

        protected BaseTrackGenerator _lastGenerator;

        protected BaseTrackGenerator _forcedNextGenerator;

        public float screenLeftBound;

        public Point generatorPosition;

        private BaseTrackGenerator _trackGenerator;

        protected GoalIndicator _goalIndicator;

        public int bottomTile;

        public int topTile;

        public float deathTimer;

        protected int _lastTilePosition = -1;

        public int slimeResetPosition = -80;

        public float checkpointPosition;

        public int furthestGeneratedCheckpoint;

        public bool isJumpPressed;

        public float stateTimer;

        public int cutsceneTick;

        public float pauseBeforeTitleFadeOutTimer;

        public float mapTimer;

        private List<KeyValuePair<string, int>> _currentHighScores;

        private int currentHighScore;

        public float scoreUpdateTimer;

        protected HashSet<CollectableFruits> _spawnedFruit;

        //protected
        public HashSet<CollectableFruits> _collectedFruit;

        public List<int> checkpointPositions;

        protected Dictionary<ObstacleTypes, List<Type>> _validObstacles;

        protected List<GeneratorRoll> _generatorRolls;

        private bool _trackAddedFlip;

        protected bool _buttonState;

        public bool _wasJustChatting;

        public double totalTime => _totalTime;

        public double totalTimeMS => _totalTime * 1000.0;

        protected SMineCart() { }

        public SMineCart(int whichTheme, int mode, Random genRandom, ulong currentFrame)
        {
            CurrentFrame = currentFrame;
            StartFrame = currentFrame;
            random = genRandom.Copy();
            _entities = new List<Entity>();
            _collectedFruit = new HashSet<CollectableFruits>();
            _generatorRolls = new List<GeneratorRoll>();
            _validObstacles = new Dictionary<ObstacleTypes, List<Type>>();
            initLevelTransitions();
            if (Game1.player.team.junimoKartScores.GetScores().Count == 0)
            {
                Game1.player.team.junimoKartScores.AddScore(
                    Game1.RequireCharacter("Lewis").displayName,
                    50000
                );
                Game1.player.team.junimoKartScores.AddScore(
                    Game1.RequireCharacter("Shane").displayName,
                    25000
                );
                Game1.player.team.junimoKartScores.AddScore(
                    Game1.RequireCharacter("Sam").displayName,
                    10000
                );
                Game1.player.team.junimoKartScores.AddScore(
                    Game1.RequireCharacter("Abigail").displayName,
                    5000
                );
                Game1.player.team.junimoKartScores.AddScore(
                    Game1.RequireCharacter("Vincent").displayName,
                    250
                );
            }
            changeScreenSize();
            texture = Game1.content.Load<Texture2D>("Minigames\\MineCart");
            SMineCartGlobal.PlaySound(shouldPlaySound, "minecartLoop", out minecartLoop);
            minecartLoop.Pause();
            backBGYOffset = tileSize * 2;
            ytileOffset = screenHeight / 2 / tileSize;
            gameMode = mode;
            bottomTile = screenHeight / tileSize - 1;
            topTile = 4;
            currentTheme = whichTheme;
            ShowTitle();
        }

        public void initLevelTransitions()
        {
            LEVEL_TRANSITIONS = new LevelTransition[15]
            {
                new LevelTransition(-1, 0, 2, 5, "rrr"),
                new LevelTransition(0, 8, 5, 5, "rddrrd", () => lastLevelWasPerfect),
                new LevelTransition(0, 1, 5, 5, "rddlddrdd"),
                new LevelTransition(
                    1,
                    3,
                    6,
                    11,
                    "drdrrrrrrrrruuuuu",
                    () => secondsOnThisLevel <= 60f
                ),
                new LevelTransition(1, 5, 6, 11, "rrurruuu", (Func<bool>)random.NextBool),
                new LevelTransition(1, 2, 6, 11, "rrurrrrddr"),
                new LevelTransition(8, 5, 8, 8, "ddrruuu", (Func<bool>)random.NextBool),
                new LevelTransition(8, 2, 8, 8, "ddrrrrddr"),
                new LevelTransition(5, 3, 10, 7, "urruulluurrrrrddddddr"),
                new LevelTransition(2, 3, 13, 12, "rurruuu"),
                new LevelTransition(3, 9, 16, 8, "rruuluu", (Func<bool>)random.NextBool),
                new LevelTransition(3, 4, 16, 8, "rrddrddr"),
                new LevelTransition(4, 6, 20, 12, "ruuruuuuuu"),
                new LevelTransition(9, 6, 17, 4, "rrdrrru"),
                new LevelTransition(6, 7, 22, 4, "rr")
            };
        }

        public void ShowTitle()
        {
            musicSW = new TASStopWatch();
            SMineCartGlobal.ChangeMusicTrack(
                shouldPlaySound,
                "junimoKart",
                track_interruptable: false,
                MusicContext.MiniGame
            );
            titleJunimoStartedBobbing = false;
            completelyPerfect = true;
            screenDarkness = 1f;
            fadeDelta = -1f;
            ResetState();
            player.enabled = false;
            setUpTheme(0);
            levelThemesFinishedThisRun.Clear();
            gameState = GameStates.Title;
            CreateLakeDecor();
            RefreshHighScore();
            titleScreenJunimo = AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(259, 492, 14, 20),
                    new Vector2(screenWidth / 2 - 128 + 137, screenHeight / 2 - 35 + 46),
                    100f,
                    0f,
                    0f,
                    0f,
                    99999f,
                    1f,
                    1,
                    1f,
                    0.24f
                ),
                random
            );
            if (gameMode == 3)
            {
                setUpTheme(-1);
            }
            else
            {
                setUpTheme(0);
            }
        }

        public void RefreshHighScore()
        {
            _currentHighScores = Game1.player.team.junimoKartScores.GetScores();
            currentHighScore = 0;
            if (_currentHighScores.Count > 0)
            {
                currentHighScore = _currentHighScores[0].Value;
            }
        }

        public Obstacle AddObstacle(Track track, ObstacleTypes obstacle_type)
        {
            if (track == null || !_validObstacles.TryGetValue(obstacle_type, out var obstacleTypes))
            {
                return null;
            }
            Type type = random.ChooseFrom(obstacleTypes);
            Obstacle obstacle = AddEntity(Activator.CreateInstance(type) as Obstacle, random);
            if (!obstacle.CanSpawnHere(track))
            {
                obstacle.Destroy();
                return null;
            }
            obstacle.position.X = track.position.X + (float)(tileSize / 2);
            obstacle.position.Y = track.GetYAtPoint(obstacle.position.X);
            track.obstacle = obstacle;
            obstacle.InitializeObstacle(track, random);
            return obstacle;
        }

        public virtual T AddEntity<T>(T new_entity, Random random)
            where T : Entity
        {
            _entities.Add(new_entity);
            new_entity.Initialize(this, random);
            return new_entity;
        }

        public Track GetTrackForXPosition(float x)
        {
            int tile_position = (int)(x / (float)tileSize);
            if (!_tracks.TryGetValue(tile_position, out var tracks))
            {
                return null;
            }
            return tracks[0];
        }

        public void AddCheckpoint(int tile_x)
        {
            if (gameMode != 2)
            {
                tile_x = GetValidCheckpointPosition(tile_x);
                if (
                    tile_x != furthestGeneratedCheckpoint
                    && tile_x > furthestGeneratedCheckpoint + 8
                    && IsTileInBounds(
                        (int)(GetTrackForXPosition(tile_x * tileSize).position.Y / (float)tileSize)
                    )
                )
                {
                    furthestGeneratedCheckpoint = tile_x;
                    CheckpointIndicator checkpoint_indicator = AddEntity(
                        new CheckpointIndicator(this),
                        random
                    );
                    checkpoint_indicator.position.X = ((float)tile_x + 0.5f) * (float)tileSize;
                    checkpoint_indicator.position.Y = GetTrackForXPosition(tile_x * tileSize)
                        .GetYAtPoint(checkpoint_indicator.position.X + 5f);
                    checkpointPositions.Add(tile_x);
                }
            }
        }

        public List<Track> GetTracksForXPosition(float x)
        {
            int tile_position = (int)(x / (float)tileSize);
            if (!_tracks.TryGetValue(tile_position, out var tracks))
            {
                return null;
            }
            return tracks;
        }

        protected bool _IsGeneratingOnUpperHalf()
        {
            int mid_point = (topTile + bottomTile) / 2;
            if (generatorPosition.Y <= mid_point)
            {
                return true;
            }
            return false;
        }

        protected bool _IsGeneratingOnLowerHalf()
        {
            int mid_point = (topTile + bottomTile) / 2;
            if (generatorPosition.Y >= mid_point)
            {
                return true;
            }
            return false;
        }

        protected void _GenerateMoreTrack()
        {
            while (
                (float)(generatorPosition.X * tileSize)
                <= screenLeftBound + (float)screenWidth + (float)(16 * tileSize)
            )
            {
                if (_trackGenerator == null)
                {
                    if (generatorPosition.X >= distanceToTravel)
                    {
                        _trackGenerator = null;
                        break;
                    }
                    for (int tries = 0; tries < 2; tries++)
                    {
                        for (int i = 0; i < _generatorRolls.Count; i++)
                        {
                            if (_forcedNextGenerator != null)
                            {
                                _trackGenerator = _forcedNextGenerator;
                                _forcedNextGenerator = null;
                                break;
                            }
                            if (
                                _generatorRolls[i].generator != _lastGenerator
                                && random.NextDouble() < (double)_generatorRolls[i].chance
                                && (
                                    _generatorRolls[i].additionalGenerationCondition == null
                                    || _generatorRolls[i].additionalGenerationCondition()
                                )
                            )
                            {
                                _trackGenerator = _generatorRolls[i].generator;
                                _forcedNextGenerator = _generatorRolls[i].forcedNextGenerator;
                                break;
                            }
                        }
                        if (_trackGenerator != null)
                        {
                            break;
                        }
                        if (_trackGenerator == null)
                        {
                            if (_lastGenerator != null)
                            {
                                _lastGenerator = null;
                                continue;
                            }
                            _trackGenerator = new StraightAwayGenerator(this)
                                .SetLength(2, 2)
                                .SetStaggerChance(0f)
                                .SetCheckpoint(checkpoint: false);
                            _forcedNextGenerator = null;
                        }
                    }
                    _trackGenerator.Initialize(random);
                    _lastGenerator = _trackGenerator;
                }
                _trackGenerator?.GenerateTrack(random, shouldPlaySound);
                if (generatorPosition.X >= distanceToTravel)
                {
                    break;
                }
                _trackGenerator = null;
            }
            if (generatorPosition.X >= distanceToTravel)
            {
                Track track = AddTrack(generatorPosition.X, generatorPosition.Y);
                if (_goalIndicator == null)
                {
                    _goalIndicator = AddEntity(new GoalIndicator(this), random);
                    _goalIndicator.position.X =
                        ((float)generatorPosition.X + 0.5f) * (float)tileSize;
                    _goalIndicator.position.Y = track.GetYAtPoint(_goalIndicator.position.X);
                }
                else
                {
                    CreatePickup(
                        new Vector2((float)generatorPosition.X + 0.5f, generatorPosition.Y - 1)
                            * tileSize,
                        fruit_only: true
                    );
                }
                generatorPosition.X++;
            }
        }

        public Track AddTrack(int x, int y, Track.TrackType type = Track.TrackType.Straight)
        {
            if (type == Track.TrackType.UpSlope || type == Track.TrackType.SlimeUpSlope)
            {
                y++;
            }
            _trackAddedFlip = !_trackAddedFlip;
            Track track_object = new Track(this, type, _trackAddedFlip);
            track_object.position.X = x * tileSize;
            track_object.position.Y = y * tileSize;
            return AddTrack(track_object);
        }

        public Track AddTrack(Track track_object)
        {
            Track track = AddEntity(track_object, random);
            int x = (int)(track.position.X / (float)tileSize);
            if (!_tracks.TryGetValue(x, out var tracks))
            {
                tracks = (_tracks[x] = new List<Track>());
            }
            tracks.Add(track_object);
            tracks.OrderBy((Track o) => o.position.Y);
            return track;
        }

        public bool overrideFreeMouseMovement()
        {
            return Game1.options.SnappyMenus;
        }

        public void UpdateMapTick(float time)
        {
            mapTimer += time;
            MapJunimo map_junimo = null;
            foreach (Entity entity in _entities)
            {
                if (entity is MapJunimo junimo)
                {
                    map_junimo = junimo;
                    break;
                }
            }
            if (mapTimer >= 2f && map_junimo.moveState == MapJunimo.MoveState.Idle)
            {
                map_junimo.StartMoving();
            }
            if (map_junimo.moveState == MapJunimo.MoveState.Moving)
            {
                mapTimer = 0f;
            }
            if (map_junimo.moveState == MapJunimo.MoveState.Finished && mapTimer >= 1.5f)
            {
                fadeDelta = 1f;
            }
            if (screenDarkness >= 1f && fadeDelta > 0f)
            {
                ShowCutscene();
            }
        }

        public void UpdateCutsceneTick()
        {
            int fade_out_time = 400;
            if (gamePaused)
            {
                return;
            }
            if (cutsceneTick == 0)
            {
                if (!minecartLoop.IsPaused)
                {
                    minecartLoop.Pause();
                }
                cutsceneText = Game1.content.LoadString(
                    "Strings\\UI:Junimo_Kart_Level_" + currentTheme
                );
                if (currentTheme == 7)
                {
                    cutsceneText = "";
                }
                player.enabled = false;
                screenDarkness = 1f;
                fadeDelta = -1f;
            }
            if (cutsceneTick == 100)
            {
                player.enabled = true;
            }
            if (currentTheme == 0)
            {
                if (cutsceneTick == 0)
                {
                    Roadblock roadblock = AddEntity(new Roadblock(this), random);
                    roadblock.position.X = 6 * tileSize;
                    roadblock.position.Y = 10 * tileSize;
                    Roadblock roadblock2 = AddEntity(new Roadblock(this), random);
                    roadblock2.position.X = 19 * tileSize;
                    roadblock2.position.Y = 10 * tileSize;
                }
                if (cutsceneTick == 140)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 150)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 130)
                {
                    AddEntity(new FallingBoulder(this), random).position = new Vector2(
                        player.position.X + 100f,
                        -16f
                    );
                }
                if (cutsceneTick == 160)
                {
                    AddEntity(new FallingBoulder(this), random).position = new Vector2(
                        player.position.X + 100f,
                        -16f
                    );
                }
                if (cutsceneTick == 190)
                {
                    AddEntity(new FallingBoulder(this), random).position = new Vector2(
                        player.position.X + 100f,
                        -16f
                    );
                }
                if (cutsceneTick == 270)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 275)
                {
                    player.ReleaseJump();
                }
            }
            if (currentTheme == 1)
            {
                if (cutsceneTick == 0)
                {
                    AddTrack(2, 9, Track.TrackType.UpSlope);
                    AddTrack(3, 8, Track.TrackType.UpSlope);
                    AddTrack(4, 8);
                    AddTrack(5, 8);
                    AddTrack(6, 7, Track.TrackType.UpSlope);
                    AddTrack(7, 8, Track.TrackType.IceDownSlope);
                    AddTrack(8, 9, Track.TrackType.IceDownSlope);
                    AddTrack(9, 10, Track.TrackType.IceDownSlope);
                    AddTrack(13, 9, Track.TrackType.UpSlope);
                    AddTrack(17, 8, Track.TrackType.UpSlope);
                    AddTrack(19, 10, Track.TrackType.UpSlope);
                    AddTrack(21, 6, Track.TrackType.UpSlope);
                    AddTrack(24, 8);
                    AddTrack(25, 8);
                    AddTrack(26, 8);
                    AddTrack(27, 8);
                    AddTrack(28, 8);
                }
                if (cutsceneTick == 100)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 130)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 200)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 215)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 260)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 270)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 304)
                {
                    player.Jump(shouldPlaySound);
                }
            }
            if (currentTheme == 4)
            {
                if (cutsceneTick == 0)
                {
                    AddTrack(1, 12, Track.TrackType.UpSlope);
                    AddTrack(2, 11, Track.TrackType.UpSlope);
                    AddTrack(3, 10, Track.TrackType.UpSlope);
                    AddTrack(4, 9, Track.TrackType.UpSlope);
                    AddTrack(5, 8, Track.TrackType.UpSlope);
                    AddTrack(6, 9, Track.TrackType.DownSlope);
                    AddTrack(7, 8, Track.TrackType.UpSlope);
                    AddTrack(8, 9, Track.TrackType.DownSlope);
                    AddTrack(9, 8, Track.TrackType.UpSlope);
                    AddTrack(10, 9, Track.TrackType.DownSlope);
                    AddTrack(11, 8, Track.TrackType.UpSlope);
                    AddTrack(12, 9, Track.TrackType.DownSlope);
                    AddTrack(13, 8, Track.TrackType.UpSlope);
                    AddTrack(14, 9, Track.TrackType.DownSlope);
                    AddTrack(15, 8, Track.TrackType.UpSlope);
                    AddTrack(16, 9, Track.TrackType.DownSlope);
                    AddTrack(17, 8, Track.TrackType.UpSlope);
                    AddTrack(18, 9, Track.TrackType.DownSlope);
                    AddTrack(19, 8, Track.TrackType.UpSlope);
                    AddTrack(20, 9, Track.TrackType.DownSlope);
                    AddTrack(21, 8, Track.TrackType.UpSlope);
                    AddTrack(22, 7, Track.TrackType.UpSlope);
                    AddTrack(23, 6, Track.TrackType.UpSlope);
                    AddTrack(24, 5, Track.TrackType.UpSlope);
                    AddTrack(25, 4, Track.TrackType.UpSlope);
                    AddTrack(26, 3, Track.TrackType.UpSlope);
                    AddTrack(27, 2, Track.TrackType.UpSlope);
                }
                if (cutsceneTick == 100)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 115)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 265)
                {
                    player.Jump(shouldPlaySound);
                }
            }
            if (currentTheme == 2)
            {
                if (cutsceneTick == 0)
                {
                    AddEntity(new Whale(this), random);
                    AddEntity(new PlayerBubbleSpawner(this), random);
                }
                if (cutsceneTick == 250)
                {
                    player.velocity.X = 0f;
                    foreach (Entity entity3 in _entities)
                    {
                        if (entity3 is Whale whale)
                        {
                            SMineCartGlobal.PlaySound(shouldPlaySound, "croak");
                            whale.SetState(Whale.CurrentState.OpenMouth);
                            break;
                        }
                    }
                }
                if (cutsceneTick == 260)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 265)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 310)
                {
                    player.velocity.X = -100f;
                }
            }
            if (currentTheme == 3)
            {
                if (cutsceneTick == 0)
                {
                    AddTrack(-1, 3);
                    AddTrack(0, 3);
                    AddTrack(1, 4, Track.TrackType.DownSlope);
                    AddTrack(2, 4);
                    AddTrack(3, 4);
                    AddTrack(4, 4);
                    AddTrack(5, 4);
                    AddTrack(6, -2);
                    AddTrack(7, -2);
                    AddTrack(8, -2);
                    AddTrack(9, -2);
                    AddTrack(19, 9);
                    AddTrack(20, 9);
                    AddTrack(21, 8, Track.TrackType.UpSlope);
                    AddTrack(22, 8);
                    AddTrack(23, 8);
                    AddTrack(24, 9, Track.TrackType.DownSlope);
                    AddTrack(25, 9);
                    AddTrack(26, 8);
                    AddTrack(27, 8);
                    AddTrack(28, 8);
                    player.position.Y = 3 * tileSize;
                }
                if (cutsceneTick == 150)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 130)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 200)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 215)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 0)
                {
                    WillOWisp willOWisp = AddEntity(new WillOWisp(this), random);
                    willOWisp.position.X = 10 * tileSize;
                    willOWisp.position.Y = 5 * tileSize;
                    willOWisp.visible = false;
                }
                if (cutsceneTick == 300)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "ghost");
                }
                if (cutsceneTick >= 300 && cutsceneTick % 3 == 0 && cutsceneTick < 350)
                {
                    foreach (Entity entity2 in _entities)
                    {
                        if (entity2 is WillOWisp)
                        {
                            entity2.visible = !entity2.visible;
                        }
                    }
                }
                if (cutsceneTick == 350)
                {
                    foreach (Entity entity in _entities)
                    {
                        if (entity is WillOWisp)
                        {
                            entity.visible = true;
                        }
                    }
                }
            }
            if (currentTheme == 9)
            {
                if (cutsceneTick == 0)
                {
                    AddTrack(0, 6);
                    AddTrack(1, 6);
                    AddTrack(2, 6);
                    AddTrack(3, 6);
                    Track spring_track = AddTrack(4, 6);
                    MushroomSpring mushroomSpring = AddEntity(new MushroomSpring(this), random);
                    mushroomSpring.InitializeObstacle(spring_track, random);
                    mushroomSpring.position = new Vector2(4.5f, 6f) * tileSize;
                    AddTrack(8, 6, Track.TrackType.MushroomLeft);
                    AddTrack(9, 6, Track.TrackType.MushroomMiddle);
                    AddTrack(10, 6, Track.TrackType.MushroomRight);
                    AddTrack(12, 10);
                    List<BalanceTrack> track_parts = new List<BalanceTrack>();
                    NoxiousMushroom noxiousMushroom = AddEntity(new NoxiousMushroom(this), random);
                    noxiousMushroom.position = new Vector2(12.5f, 10f) * tileSize;
                    noxiousMushroom.nextFire = 3f;
                    BalanceTrack track_piece = new BalanceTrack(
                        this,
                        Track.TrackType.MushroomLeft,
                        showSecondTile: false
                    );
                    track_piece.position.X = 15 * tileSize;
                    track_piece.position.Y = 9 * tileSize;
                    track_parts.Add(track_piece);
                    AddTrack(track_piece);
                    track_piece = new BalanceTrack(
                        this,
                        Track.TrackType.MushroomMiddle,
                        showSecondTile: false
                    );
                    track_piece.position.X = 16 * tileSize;
                    track_piece.position.Y = 9 * tileSize;
                    track_parts.Add(track_piece);
                    AddTrack(track_piece);
                    track_piece = new BalanceTrack(
                        this,
                        Track.TrackType.MushroomRight,
                        showSecondTile: false
                    );
                    track_piece.position.X = 17 * tileSize;
                    track_piece.position.Y = 9 * tileSize;
                    track_parts.Add(track_piece);
                    AddTrack(track_piece);
                    List<BalanceTrack> other_track_parts = new List<BalanceTrack>();
                    track_piece = new BalanceTrack(
                        this,
                        Track.TrackType.MushroomLeft,
                        showSecondTile: false
                    );
                    track_piece.position.X = 22 * tileSize;
                    track_piece.position.Y = 9 * tileSize;
                    other_track_parts.Add(track_piece);
                    AddTrack(track_piece);
                    track_piece = new BalanceTrack(
                        this,
                        Track.TrackType.MushroomMiddle,
                        showSecondTile: false
                    );
                    track_piece.position.X = 23 * tileSize;
                    track_piece.position.Y = 9 * tileSize;
                    other_track_parts.Add(track_piece);
                    AddTrack(track_piece);
                    track_piece = new BalanceTrack(
                        this,
                        Track.TrackType.MushroomRight,
                        showSecondTile: false
                    );
                    track_piece.position.X = 24 * tileSize;
                    track_piece.position.Y = 9 * tileSize;
                    other_track_parts.Add(track_piece);
                    AddTrack(track_piece);
                    foreach (BalanceTrack item in track_parts)
                    {
                        item.connectedTracks = new List<BalanceTrack>(track_parts);
                        item.counterBalancedTracks = new List<BalanceTrack>(other_track_parts);
                    }
                    foreach (BalanceTrack item2 in other_track_parts)
                    {
                        item2.connectedTracks = new List<BalanceTrack>(other_track_parts);
                        item2.counterBalancedTracks = new List<BalanceTrack>(track_parts);
                    }
                    player.position.Y = 6 * tileSize;
                }
                if (cutsceneTick == 115)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 120)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 230)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 250)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 298)
                {
                    player.Jump(shouldPlaySound);
                }
            }
            if (currentTheme == 6)
            {
                if (cutsceneTick == 0)
                {
                    AddTrack(0, 6);
                    AddTrack(1, 3);
                    AddTrack(2, 8);
                    AddTrack(4, 4);
                    AddTrack(5, 4);
                    AddTrack(6, 2);
                    AddTrack(8, 8);
                    AddTrack(9, 1);
                    AddTrack(10, 2);
                    AddTrack(12, 8);
                    AddTrack(13, 6);
                    AddTrack(14, 6);
                    AddTrack(15, 8);
                    AddTrack(17, 4);
                    AddTrack(18, 2);
                    AddTrack(19, 2);
                    AddTrack(20, 2);
                    AddTrack(21, 2);
                    AddTrack(22, 2);
                    AddTrack(23, 2);
                    AddTrack(24, 2);
                    AddTrack(25, 2);
                    AddTrack(26, 2);
                    AddTrack(27, 2);
                    AddTrack(28, 2);
                    player.position.Y = 6 * tileSize;
                }
                if (cutsceneTick == 129)
                {
                    player.Jump(shouldPlaySound);
                }
                if (cutsceneTick == 170)
                {
                    player.ReleaseJump();
                }
                if (cutsceneTick == 214)
                {
                    player.Jump(shouldPlaySound);
                }
            }
            if (currentTheme == 7)
            {
                fade_out_time = 800;
                if (cutsceneTick == 0)
                {
                    if (completelyPerfect)
                    {
                        AddEntity(
                            new MineDebris(
                                this,
                                new Rectangle(256, 182, 48, 45),
                                new Vector2(
                                    (float)(20 * tileSize) + 12f,
                                    (float)(10 * tileSize) - 21.5f
                                ),
                                0f,
                                0f,
                                0f,
                                0f,
                                1000f,
                                1f,
                                1,
                                0f,
                                0.23f,
                                holdLastFrame: true
                            ),
                            random
                        );
                    }
                    else
                    {
                        AddEntity(
                            new MineDebris(
                                this,
                                new Rectangle(256, 112, 25, 32),
                                new Vector2(
                                    (float)(20 * tileSize) + 12f,
                                    (float)(10 * tileSize) - 16f
                                ),
                                0f,
                                0f,
                                0f,
                                0f,
                                1000f,
                                1f,
                                1,
                                0f,
                                0.23f,
                                holdLastFrame: true
                            ),
                            random
                        );
                    }
                }
                if (cutsceneTick == 200)
                {
                    player.velocity.X = 40f;
                }
                if (cutsceneTick == 250)
                {
                    player.velocity.X = 20f;
                }
                if (cutsceneTick == 300)
                {
                    player.velocity.X = 0f;
                }
                if (cutsceneTick >= 350 && cutsceneTick % 10 == 0 && cutsceneTick < 600)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "junimoMeep1");
                    AddEntity(new EndingJunimo(this, completelyPerfect), random).position =
                        new Vector2(20 * tileSize, 10 * tileSize);
                }
            }
            if (cutsceneTick == fade_out_time)
            {
                screenDarkness = 0f;
                fadeDelta = 2f;
            }
            if (cutsceneTick == fade_out_time + 100)
            {
                EndCutscene();
                return;
            }
            if (player.velocity.X > 0f && player.position.X > (float)(screenWidth + tileSize))
            {
                if (!minecartLoop.IsPaused)
                {
                    minecartLoop.Pause();
                }
                player.enabled = false;
            }
            if (player.velocity.X < 0f && player.position.X < (float)(-tileSize))
            {
                if (!minecartLoop.IsPaused)
                {
                    minecartLoop.Pause();
                }
                player.enabled = false;
            }
            if (currentTheme == 5 && cutsceneTick == 100)
            {
                AddEntity(new HugeSlime(this), random);
                slimeBossPosition = -100f;
            }
        }

        public void UpdateFruitsSummary(float time)
        {
            if (currentTheme == 7)
            {
                currentFruitCheckIndex = -1;
                ShowCutscene();
            }
            if (gamePaused)
            {
                return;
            }
            if (stateTimer >= 0f)
            {
                stateTimer -= time;
                if (stateTimer < 0f)
                {
                    stateTimer = 0f;
                }
            }
            if (stateTimer != 0f)
            {
                return;
            }
            if (livesLeft < 3 && gameMode == 3)
            {
                livesLeft++;
                stateTimer = 0.25f;
                SMineCartGlobal.PlaySound(shouldPlaySound, "coin");
                return;
            }
            if (lastLevelWasPerfect && perfectText == null && gameMode == 3)
            {
                perfectText = new SparklingText(
                    Game1.dialogueFont,
                    Game1.content.LoadString("Strings\\UI:BobberBar_Perfect"),
                    Color.Lime,
                    Color.White,
                    rainbow: true,
                    0.1,
                    2500,
                    -1,
                    500,
                    0f
                );
                SMineCartGlobal.PlaySound(shouldPlaySound, "yoba");
            }
            if (currentFruitCheckIndex == -1)
            {
                fruitEatCount = 0;
                currentFruitCheckIndex = 0;
                stateTimer = 0.5f;
                return;
            }
            if (currentFruitCheckIndex >= 3)
            {
                perfectText = null;
                currentFruitCheckIndex = -1;
                ShowMap();
                return;
            }
            if (_collectedFruit.Contains((CollectableFruits)currentFruitCheckIndex))
            {
                _collectedFruit.Remove((CollectableFruits)currentFruitCheckIndex);
                SMineCartGlobal.PlaySound(
                    shouldPlaySound,
                    "newArtifact",
                    currentFruitCheckIndex * 100
                );
                fruitEatCount++;
                if (fruitEatCount >= 3)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "yoba");
                    if (gameMode == 3)
                    {
                        livesLeft++;
                    }
                    else
                    {
                        score += 5000;
                        UpdateScoreState();
                    }
                }
            }
            else
            {
                SMineCartGlobal.PlaySound(shouldPlaySound, "sell", currentFruitCheckIndex * 100);
            }
            stateTimer = 0.5f;
            currentFruitCheckMagnitude = 3f;
            currentFruitCheckIndex++;
        }

        public void UpdateInput()
        {
            if (Game1.IsChatting || Game1.textEntry != null)
            {
                _wasJustChatting = true;
            }
            else
            {
                if (gamePaused)
                {
                    return;
                }
                bool button_pressed = false;
                if (Simulating)
                {
                    _wasJustChatting = false;
                    button_pressed = ShouldPressButton;
                    if (button_pressed != _buttonState)
                    {
                        _buttonState = button_pressed;
                        if (_buttonState)
                        {
                            player?.QueueJump();
                            isJumpPressed = true;
                        }
                        else
                        {
                            player?.ReleaseJump();
                            isJumpPressed = false;
                        }
                    }
                }
                else
                {
                    if (Game1.input.GetMouseState().LeftButton == ButtonState.Pressed)
                    {
                        button_pressed = true;
                    }
                    if (
                        Game1.isOneOfTheseKeysDown(
                            Game1.input.GetKeyboardState(),
                            Game1.options.useToolButton
                        )
                        || Game1.isOneOfTheseKeysDown(
                            Game1.input.GetKeyboardState(),
                            Game1.options.actionButton
                        )
                        || Game1.input.GetKeyboardState().IsKeyDown(Keys.Space)
                        || Game1.input.GetKeyboardState().IsKeyDown(Keys.LeftShift)
                    )
                    {
                        button_pressed = true;
                    }
                    if (
                        Game1.input.GetGamePadState().IsButtonDown(Buttons.A)
                        || Game1.input.GetGamePadState().IsButtonDown(Buttons.B)
                    )
                    {
                        button_pressed = true;
                    }

                    if (button_pressed != _buttonState)
                    {
                        _buttonState = button_pressed;
                        if (_buttonState)
                        {
                            if (gameState == GameStates.Title)
                            {
                                if (
                                    pauseBeforeTitleFadeOutTimer == 0f
                                    && screenDarkness == 0f
                                    && fadeDelta <= 0f
                                )
                                {
                                    pauseBeforeTitleFadeOutTimer = 0.5f;
                                    SMineCartGlobal.PlaySound(shouldPlaySound, "junimoMeep1");
                                    if (titleScreenJunimo != null)
                                    {
                                        titleScreenJunimo.Destroy();
                                        AddEntity(
                                            new MineDebris(
                                                this,
                                                new Rectangle(259, 492, 14, 20),
                                                new Vector2(
                                                    screenLeftBound
                                                        + (float)(screenWidth / 2)
                                                        - 128f
                                                        + 137f,
                                                    screenHeight / 2 - 35 + 46
                                                ),
                                                110f,
                                                -200f,
                                                0f,
                                                3f,
                                                99999f,
                                                1f,
                                                1,
                                                1f,
                                                0.24f
                                            ),
                                            random
                                        );
                                    }
                                    musicSW?.Stop();
                                    musicSW = null;
                                }
                                return;
                            }
                            if (gameState == GameStates.Cutscene)
                            {
                                EndCutscene();
                                return;
                            }
                            if (gameState == GameStates.Map)
                            {
                                fadeDelta = 1f;
                                return;
                            }
                            player?.QueueJump();
                            isJumpPressed = true;
                        }
                        else if (!gamePaused)
                        {
                            player?.ReleaseJump();
                            isJumpPressed = false;
                        }
                    }
                    _wasJustChatting = false;
                }
            }
        }

        public virtual bool CanPause()
        {
            if (gameState == GameStates.Ingame)
            {
                return true;
            }
            if (gameState == GameStates.FruitsSummary)
            {
                return true;
            }
            if (gameState == GameStates.Cutscene)
            {
                return true;
            }
            if (gameState == GameStates.Map)
            {
                return true;
            }
            return false;
        }

        public void Simulate(bool pressButton)
        {
            try
            {
                speedTick(pressButton);
                speedDraw();
                buttonPresses.Add(pressButton);
            }
            catch (Exception e)
            {
                foreach (string line in e.ToString().Split('\n'))
                {
                    Controller.Console.Alert(line);
                }
                throw;
            }
        }

        public bool speedTick(bool pressButton, bool pause = false)
        {
            ShouldPressButton = pressButton;
            Simulating = true;
            shouldDraw = false;
            shouldPlaySound = false;

            CurrentFrame++;
            musicSW?.Advance(TASDateTime.CurrentGameTime.ElapsedGameTime);
            UpdateInput();

            float delta_time = (float)TASDateTime.CurrentGameTime.ElapsedGameTime.TotalSeconds;
            shakeMagnitude = Utility.MoveTowards(shakeMagnitude, 0f, delta_time * 3f);
            currentFruitCheckMagnitude = Utility.MoveTowards(
                currentFruitCheckMagnitude,
                0f,
                delta_time * 6f
            );
            _totalTime += delta_time;
            screenDarkness += fadeDelta * delta_time;
            if (screenDarkness < 0f)
            {
                screenDarkness = 0f;
            }
            if (screenDarkness > 1f)
            {
                screenDarkness = 1f;
            }

            for (int n = 0; n < _entities.Count; n++)
            {
                if (_entities[n] != null && _entities[n].IsActive())
                {
                    _entities[n].Update(delta_time, random, shouldPlaySound);
                }
            }
            for (int l = 0; l < _entities.Count; l++)
            {
                if (_entities[l] != null && _entities[l].ShouldReap())
                {
                    _entities.RemoveAt(l);
                    l--;
                }
            }

            int delta_ms = (int)(delta_time * 1000f);
            float old_screen_left_bound = screenLeftBound;
            if (gameState == GameStates.Ingame)
            {
                secondsOnThisLevel += delta_time;
                float minimum_left_bound = 0f;

                if (deathTimer <= 0f && respawnCounter > 0)
                {
                    if (
                        screenLeftBound - Math.Max(player.position.X - 96f, minimum_left_bound)
                        > 400f
                    )
                    {
                        screenLeftBound = Utility.MoveTowards(
                            screenLeftBound,
                            Math.Max(player.position.X - 96f, 0f),
                            1200f * delta_time
                        );
                    }
                    else if (
                        screenLeftBound - Math.Max(player.position.X - 96f, minimum_left_bound)
                        > 200f
                    )
                    {
                        screenLeftBound = Utility.MoveTowards(
                            screenLeftBound,
                            Math.Max(player.position.X - 96f, minimum_left_bound),
                            600f * delta_time
                        );
                    }
                    else
                    {
                        screenLeftBound = Utility.MoveTowards(
                            screenLeftBound,
                            Math.Max(player.position.X - 96f, minimum_left_bound),
                            300f * delta_time
                        );
                    }
                    if (screenLeftBound < minimum_left_bound)
                    {
                        screenLeftBound = minimum_left_bound;
                    }
                }
                else if (deathTimer <= 0f && (float)respawnCounter <= 0f && !reachedFinish)
                {
                    screenLeftBound = player.position.X - 96f;
                }
                if (screenLeftBound < minimum_left_bound)
                {
                    screenLeftBound = minimum_left_bound;
                }

                if (
                    (float)(generatorPosition.X * tileSize)
                    <= screenLeftBound + (float)screenWidth + (float)(16 * tileSize)
                )
                {
                    _GenerateMoreTrack();
                }
                int player_tile_position = (int)player.position.X / tileSize;
                if (player_tile_position > _lastTilePosition)
                {
                    int number_of_motions = player_tile_position - _lastTilePosition;
                    _lastTilePosition = player_tile_position;
                    for (int j = 0; j < number_of_motions; j++)
                    {
                        score += 10;
                    }
                }
                if (
                    _goalIndicator != null
                    && distanceToTravel != -1
                    && player.position.X >= _goalIndicator.position.X
                    && distanceToTravel != -1
                    && player.position.Y <= _goalIndicator.position.Y * (float)tileSize + 4f
                    && !reachedFinish
                    && fadeDelta < 0f
                )
                {
                    levelThemesFinishedThisRun.Add(currentTheme);
                    if (gameMode == 2)
                    {
                        score += 5000;
                    }
                    reachedFinish = true;
                    fadeDelta = 1f;
                }
                if (score > currentHighScore)
                {
                    currentHighScore = score;
                }

                float death_buffer = 3f;
                if (currentTheme == 9)
                {
                    death_buffer = 32f;
                }
                if (player.position.Y > (float)screenHeight + death_buffer)
                {
                    Die();
                }
            }
            float parallax_scroll_speed =
                (screenLeftBound - old_screen_left_bound) / (float)tileSize;
            waterFallPosition += (float)delta_ms * (parallax_scroll_speed * 6f / 5f);
            if (waterFallPosition > (float)(screenWidth * 3 / 2))
            {
                waterFallPosition %= screenWidth * 3 / 2;
                waterfallWidth = random.Next(6);
            }

            return false;
        }

        public bool tick(GameTime time)
        {
            CurrentFrame++;
            musicSW?.Advance(time.ElapsedGameTime);
            UpdateInput();
            float delta_time = (float)time.ElapsedGameTime.TotalSeconds;
            if (gamePaused)
            {
                delta_time = 0f;
            }
            if (!CanPause())
            {
                gamePaused = false;
            }
            shakeMagnitude = Utility.MoveTowards(shakeMagnitude, 0f, delta_time * 3f);
            currentFruitCheckMagnitude = Utility.MoveTowards(
                currentFruitCheckMagnitude,
                0f,
                delta_time * 6f
            );
            _totalTime += delta_time;
            screenDarkness += fadeDelta * delta_time;
            if (screenDarkness < 0f)
            {
                screenDarkness = 0f;
            }
            if (screenDarkness > 1f)
            {
                screenDarkness = 1f;
            }
            if (gameState == GameStates.Title)
            {
                if (pauseBeforeTitleFadeOutTimer > 0f)
                {
                    pauseBeforeTitleFadeOutTimer -= 0.0166666f;
                    if (pauseBeforeTitleFadeOutTimer <= 0f)
                    {
                        fadeDelta = 1f;
                    }
                }
                if (fadeDelta >= 0f && screenDarkness >= 1f)
                {
                    restartLevel(new_game: true);
                    return false;
                }
                if (random.NextDouble() < 0.1)
                {
                    AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(0, 250, 5, 5),
                            Utility.getRandomPositionInThisRectangle(
                                new Rectangle(
                                    (int)screenLeftBound + screenWidth / 2 - 128,
                                    screenHeight / 2 - 35,
                                    256,
                                    71
                                ),
                                random
                            ),
                            100f,
                            0f,
                            0f,
                            0f,
                            0.6f,
                            1f,
                            6,
                            0.1f,
                            0.23f
                        ),
                        random
                    );
                }
                if (
                    musicSW != null
                    && Game1.currentSong?.Name == "junimoKart"
                    && Game1.currentSong.IsPlaying
                    && !musicSW.IsRunning
                )
                {
                    musicSW.Start();
                }
                if (titleScreenJunimo != null && !titleJunimoStartedBobbing)
                {
                    TASStopWatch stopwatch = musicSW;
                    if (stopwatch != null && stopwatch.ElapsedMilliseconds >= 48000)
                    {
                        titleScreenJunimo.reset(
                            new Rectangle(417, 347, 14, 20),
                            titleScreenJunimo.position,
                            100f,
                            0f,
                            0f,
                            0f,
                            9999f,
                            1f,
                            2,
                            0.25f,
                            titleScreenJunimo.depth
                        );
                        titleJunimoStartedBobbing = true;
                        goto IL_039e;
                    }
                }
                if (titleScreenJunimo != null && titleJunimoStartedBobbing)
                {
                    Stopwatch stopwatch2 = musicSW;
                    if (stopwatch2 != null && stopwatch2.ElapsedMilliseconds >= 80000)
                    {
                        titleScreenJunimo.reset(
                            new Rectangle(259, 492, 14, 20),
                            titleScreenJunimo.position,
                            100f,
                            0f,
                            0f,
                            0f,
                            99999f,
                            1f,
                            1,
                            1f,
                            0.24f
                        );
                        musicSW.Stop();
                        musicSW = null;
                    }
                }
            }
            else if (gameState == GameStates.Map)
            {
                UpdateMapTick(delta_time);
            }
            else if (gameState == GameStates.Cutscene)
            {
                if (!gamePaused)
                {
                    delta_time = 0.0166666f;
                }
                UpdateCutsceneTick();
                if (!gamePaused)
                {
                    cutsceneTick++;
                }
            }
            else if (gameState == GameStates.FruitsSummary)
            {
                UpdateFruitsSummary(delta_time);
            }
            goto IL_039e;
            IL_039e:
            int delta_ms = (int)(delta_time * 1000f);
            for (int n = 0; n < _entities.Count; n++)
            {
                if (_entities[n] != null && _entities[n].IsActive())
                {
                    _entities[n].Update(delta_time, random, shouldPlaySound);
                }
            }
            if (deathTimer <= 0f && respawnCounter > 0)
            {
                for (int m = 0; m < _entities.Count; m++)
                {
                    _entities[m].OnPlayerReset();
                }
            }
            for (int l = 0; l < _entities.Count; l++)
            {
                if (_entities[l] != null && _entities[l].ShouldReap())
                {
                    _entities.RemoveAt(l);
                    l--;
                }
            }
            float old_screen_left_bound = screenLeftBound;
            if (gameState == GameStates.Ingame)
            {
                secondsOnThisLevel += delta_time;
                if (screenDarkness >= 1f && gameOver)
                {
                    if (gameMode == 3)
                    {
                        ShowTitle();
                    }
                    else
                    {
                        levelsBeat = 0;
                        coinCount = 0;
                        setUpTheme(0);
                        restartLevel(new_game: true);
                    }
                    return false;
                }
                if (checkpointPositions.Count > 0)
                {
                    int k = 0;
                    while (
                        k < checkpointPositions.Count
                        && player.position.X >= (float)(checkpointPositions[k] * tileSize)
                    )
                    {
                        foreach (Entity entity2 in _entities)
                        {
                            if (
                                entity2 is CheckpointIndicator indicator2
                                && (int)(indicator2.position.X / (float)tileSize)
                                    == checkpointPositions[k]
                            )
                            {
                                indicator2.Activate(shouldPlaySound);
                                break;
                            }
                        }
                        checkpointPosition =
                            ((float)checkpointPositions[k] + 0.5f) * (float)tileSize;
                        ReapEntities();
                        checkpointPositions.RemoveAt(k);
                        k--;
                        k++;
                    }
                }
                float minimum_left_bound = 0f;
                if (gameState == GameStates.Cutscene)
                {
                    screenLeftBound = 0f;
                }
                else
                {
                    if (deathTimer <= 0f && respawnCounter > 0)
                    {
                        if (
                            screenLeftBound - Math.Max(player.position.X - 96f, minimum_left_bound)
                            > 400f
                        )
                        {
                            screenLeftBound = Utility.MoveTowards(
                                screenLeftBound,
                                Math.Max(player.position.X - 96f, 0f),
                                1200f * delta_time
                            );
                        }
                        else if (
                            screenLeftBound - Math.Max(player.position.X - 96f, minimum_left_bound)
                            > 200f
                        )
                        {
                            screenLeftBound = Utility.MoveTowards(
                                screenLeftBound,
                                Math.Max(player.position.X - 96f, minimum_left_bound),
                                600f * delta_time
                            );
                        }
                        else
                        {
                            screenLeftBound = Utility.MoveTowards(
                                screenLeftBound,
                                Math.Max(player.position.X - 96f, minimum_left_bound),
                                300f * delta_time
                            );
                        }
                        if (screenLeftBound < minimum_left_bound)
                        {
                            screenLeftBound = minimum_left_bound;
                        }
                    }
                    else if (deathTimer <= 0f && (float)respawnCounter <= 0f && !reachedFinish)
                    {
                        screenLeftBound = player.position.X - 96f;
                    }
                    if (screenLeftBound < minimum_left_bound)
                    {
                        screenLeftBound = minimum_left_bound;
                    }
                }
                if (
                    (float)(generatorPosition.X * tileSize)
                    <= screenLeftBound + (float)screenWidth + (float)(16 * tileSize)
                )
                {
                    _GenerateMoreTrack();
                }
                int player_tile_position = (int)player.position.X / tileSize;
                if (respawnCounter <= 0)
                {
                    if (player_tile_position > _lastTilePosition)
                    {
                        int number_of_motions = player_tile_position - _lastTilePosition;
                        _lastTilePosition = player_tile_position;
                        for (int j = 0; j < number_of_motions; j++)
                        {
                            score += 10;
                        }
                    }
                }
                else if (respawnCounter > 0)
                {
                    if (deathTimer > 0f)
                    {
                        deathTimer -= delta_time;
                    }
                    else if (
                        screenLeftBound <= Math.Max(minimum_left_bound, player.position.X - 96f)
                    )
                    {
                        if (!player.enabled)
                        {
                            GC.Collect(0, GCCollectionMode.Forced);
                        }
                        player.enabled = true;
                        respawnCounter -= delta_ms;
                    }
                }
                if (
                    _goalIndicator != null
                    && distanceToTravel != -1
                    && player.position.X >= _goalIndicator.position.X
                    && distanceToTravel != -1
                    && player.position.Y <= _goalIndicator.position.Y * (float)tileSize + 4f
                    && !reachedFinish
                    && fadeDelta < 0f
                )
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "reward");
                    levelThemesFinishedThisRun.Add(currentTheme);
                    if (gameMode == 2)
                    {
                        score += 5000;
                        UpdateScoreState();
                    }
                    foreach (Entity entity in _entities)
                    {
                        if (entity is GoalIndicator indicator)
                        {
                            indicator.Activate();
                        }
                        else if (entity is Coin || entity is Fruit)
                        {
                            lastLevelWasPerfect = false;
                        }
                    }
                    reachedFinish = true;
                    fadeDelta = 1f;
                }
                if (score > currentHighScore)
                {
                    currentHighScore = score;
                }
                if (scoreUpdateTimer <= 0f)
                {
                    UpdateScoreState();
                }
                else
                {
                    scoreUpdateTimer -= delta_time;
                }
                if (reachedFinish && random.NextDouble() < 0.25 && !gamePaused)
                {
                    createSparkShower();
                }
                if (reachedFinish && screenDarkness >= 1f)
                {
                    reachedFinish = false;
                    if (gameMode != 3)
                    {
                        currentTheme = infiniteModeLevels[(levelsBeat + 1) % 8];
                    }
                    levelsBeat++;
                    setUpTheme(currentTheme);
                    restartLevel();
                }
                float death_buffer = 3f;
                if (currentTheme == 9)
                {
                    death_buffer = 32f;
                }
                if (player.position.Y > (float)screenHeight + death_buffer)
                {
                    Die();
                }
            }
            else if (gameState == GameStates.FruitsSummary)
            {
                screenLeftBound = 0f;
                if (perfectText != null && perfectText.update(time))
                {
                    perfectText = null;
                }
            }
            if (gameState == GameStates.Title)
            {
                screenLeftBound += delta_time * 100f;
            }
            float parallax_scroll_speed =
                (screenLeftBound - old_screen_left_bound) / (float)tileSize;
            lakeSpeedAccumulator += (float)delta_ms * (parallax_scroll_speed / 4f) % 96f;
            backBGPosition += (float)delta_ms * (parallax_scroll_speed / 5f);
            backBGPosition = (backBGPosition + 9600f) % 96f;
            midBGPosition += (float)delta_ms * (parallax_scroll_speed / 4f);
            midBGPosition = (midBGPosition + 9600f) % 96f;
            waterFallPosition += (float)delta_ms * (parallax_scroll_speed * 6f / 5f);
            if (waterFallPosition > (float)(screenWidth * 3 / 2))
            {
                waterFallPosition %= screenWidth * 3 / 2;
                waterfallWidth = random.Next(6);
            }
            for (int i = sparkShower.Count - 1; i >= 0; i--)
            {
                sparkShower[i].dy += 0.105f * (delta_time / 0.0166666f);
                sparkShower[i].x += sparkShower[i].dx * (delta_time / 0.0166666f);
                sparkShower[i].y += sparkShower[i].dy * (delta_time / 0.0166666f);
                sparkShower[i].c.B = (byte)(
                    0.0
                    + Math.Max(
                        0.0,
                        Math.Sin(totalTimeMS / (Math.PI * 20.0 / (double)sparkShower[i].dx)) * 255.0
                    )
                );
                if (reachedFinish)
                {
                    sparkShower[i].c.R = (byte)(
                        0.0
                        + Math.Max(
                            0.0,
                            Math.Sin(
                                (totalTimeMS + 50.0) / (Math.PI * 20.0 / (double)sparkShower[i].dx)
                            ) * 255.0
                        )
                    );
                    sparkShower[i].c.G = (byte)(
                        0.0
                        + Math.Max(
                            0.0,
                            Math.Sin(
                                (totalTimeMS + 100.0) / (Math.PI * 20.0 / (double)sparkShower[i].dx)
                            ) * 255.0
                        )
                    );
                    if (sparkShower[i].c.R == 0)
                    {
                        sparkShower[i].c.R = byte.MaxValue;
                    }
                    if (sparkShower[i].c.G == 0)
                    {
                        sparkShower[i].c.G = byte.MaxValue;
                    }
                }
                if (sparkShower[i].y > (float)screenHeight)
                {
                    sparkShower.RemoveAt(i);
                }
            }
            return false;
        }

        public void UpdateScoreState()
        {
            Game1.player.team.junimoKartStatus.UpdateState(score.ToString());
            scoreUpdateTimer = 1f;
        }

        public int GetValidCheckpointPosition(int x_pos)
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                if (GetTrackForXPosition(x_pos * tileSize) != null)
                {
                    break;
                }
                x_pos--;
            }
            for (; i < 16; i++)
            {
                if (GetTrackForXPosition(x_pos * tileSize) == null)
                {
                    x_pos++;
                    break;
                }
                x_pos--;
            }
            if (GetTrackForXPosition(x_pos * tileSize) == null)
            {
                return furthestGeneratedCheckpoint;
            }
            int valid_x_pos = x_pos;
            int tile_y = (int)(GetTrackForXPosition(x_pos * tileSize).position.Y / (float)tileSize);
            x_pos++;
            int consecutive_valid_tracks = 0;
            for (i = 0; i < 16; i++)
            {
                Track current_track = GetTrackForXPosition(x_pos * tileSize);
                if (current_track == null)
                {
                    return furthestGeneratedCheckpoint;
                }
                if (Math.Abs((int)(current_track.position.Y / (float)tileSize) - tile_y) <= 1)
                {
                    consecutive_valid_tracks++;
                    if (consecutive_valid_tracks >= 3)
                    {
                        return valid_x_pos;
                    }
                }
                else
                {
                    consecutive_valid_tracks = 0;
                    valid_x_pos = x_pos;
                    tile_y = (int)(
                        GetTrackForXPosition(x_pos * tileSize).position.Y / (float)tileSize
                    );
                }
                x_pos++;
            }
            return furthestGeneratedCheckpoint;
        }

        public virtual void CollectFruit(CollectableFruits fruit_type)
        {
            _collectedFruit.Add(fruit_type);
            if (gameMode == 3)
            {
                CollectCoin(10);
                return;
            }
            score += 1000;
            UpdateScoreState();
        }

        public virtual void CollectCoin(int amount)
        {
            if (gameMode == 3)
            {
                coinCount += amount;
                if (coinCount >= 100)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "yoba");
                    int added_lives = coinCount / 100;
                    coinCount %= 100;
                    livesLeft += added_lives;
                }
            }
            else
            {
                score += 30;
                UpdateScoreState();
            }
        }

        public void submitHighScore()
        {
            Game1.player.team.junimoKartScores.AddScore(Game1.player.Name, score);
            RefreshHighScore();
        }

        public void Die()
        {
            if (respawnCounter > 0 || deathTimer > 0f || reachedFinish || !player.enabled)
            {
                return;
            }
            player.OnDie();
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(16, 96, 16, 16),
                    player.position,
                    random.Next(-80, 81),
                    random.Next(-100, -49),
                    0f,
                    1f,
                    1f
                ),
                random
            );
            AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(32, 96, 16, 16),
                        player.position + new Vector2(0f, 0f - player.characterExtraHeight),
                        random.Next(-80, 81),
                        random.Next(-150, -99),
                        0.1f,
                        1f,
                        1f,
                        2f / 3f
                    ),
                    random
                )
                .SetColor(Color.Lime);
            player.position.Y = -1000f;
            SMineCartGlobal.PlaySound(shouldPlaySound, "fishEscape");
            player.enabled = false;
            lastLevelWasPerfect = false;
            completelyPerfect = false;
            if (gameState == GameStates.Cutscene)
            {
                return;
            }
            livesLeft--;
            if (gameMode != 3 || livesLeft < 0)
            {
                gameOver = true;
                fadeDelta = 1f;
                if (gameMode == 2)
                {
                    submitHighScore();
                }
                return;
            }
            player.position.X = checkpointPosition;
            for (int i = 0; i < 6; i++)
            {
                Track runway_track = GetTrackForXPosition(
                    (checkpointPosition / (float)tileSize + (float)i) * (float)tileSize
                );
                if (runway_track != null && runway_track.obstacle != null)
                {
                    runway_track.obstacle.Destroy();
                    runway_track.obstacle = null;
                }
            }
            player.SnapToFloor();
            deathTimer = 0.25f;
            respawnCounter = 1400;
        }

        public void ReapEntities()
        {
            float reap_position = checkpointPosition - 96f - (float)(4 * tileSize);
            foreach (int grid_position in new List<int>(_tracks.Keys))
            {
                if ((float)grid_position < reap_position / (float)tileSize)
                {
                    for (int i = 0; i < _tracks[grid_position].Count; i++)
                    {
                        Track track = _tracks[grid_position][i];
                        _entities.Remove(track);
                    }
                    _tracks.Remove(grid_position);
                }
            }
        }

        public void receiveLeftClick(int x, int y, bool playSound = true) { }

        public void releaseLeftClick(int x, int y) { }

        public void releaseRightClick(int x, int y) { }

        public void receiveRightClick(int x, int y, bool playSound = true) { }

        public void receiveKeyPress(Keys k)
        {
            if (Game1.input.GetGamePadState().IsButtonDown(Buttons.Back) || k.Equals(Keys.Escape))
            {
                QuitGame();
            }
            else if (
                (
                    CanPause()
                    && !Game1.options.gamepadControls
                    && (k.Equals(Keys.P) || k.Equals(Keys.Enter))
                )
                || (
                    Game1.options.gamepadControls
                    && Game1.input.GetGamePadState().IsButtonDown(Buttons.Start)
                )
            )
            {
                gamePaused = !gamePaused;
                if (gamePaused)
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "bigSelect");
                }
                else
                {
                    SMineCartGlobal.PlaySound(shouldPlaySound, "bigDeSelect");
                }
            }
        }

        public void receiveKeyRelease(Keys k) { }

        public void ResetState()
        {
            gameOver = false;
            screenLeftBound = 0f;
            respawnCounter = 0;
            deathTimer = 0f;
            _spawnedFruit = new HashSet<CollectableFruits>();
            sparkShower.Clear();
            _goalIndicator = null;
            checkpointPositions = new List<int>();
            _tracks = new Dictionary<int, List<Track>>();
            _entities = new List<Entity>();
            player = AddEntity(new PlayerMineCartCharacter(this), random);
            player.position.X = 0f;
            player.position.Y = ytileOffset * tileSize;
            generatorPosition.X = 0;
            generatorPosition.Y = ytileOffset + 1;
            _lastGenerator = null;
            _trackGenerator = null;
            _forcedNextGenerator = null;
            trackBuilderCharacter = AddEntity(new MineCartCharacter(this), random);
            trackBuilderCharacter.visible = false;
            trackBuilderCharacter.enabled = false;
            _lastTilePosition = 0;
            pauseBeforeTitleFadeOutTimer = 0f;
            lakeDecor.Clear();
            obstacles.Clear();
            reachedFinish = false;
        }

        public void QuitGame()
        {
            unload();
            SMineCartGlobal.PlaySound(shouldPlaySound, "bigDeSelect");
            Game1.currentMinigame = null;
        }

        private void restartLevel(bool new_game = false)
        {
            if (new_game)
            {
                livesLeft = 3;
                _collectedFruit.Clear();
                coinCount = 0;
                score = 0;
                levelsBeat = 0;
            }
            ResetState();
            if ((levelsBeat > 0 && _collectedFruit.Count > 0) || (livesLeft < 3 && !new_game))
            {
                ShowFruitsSummary();
            }
            else
            {
                ShowMap();
            }
        }

        public void ShowFruitsSummary()
        {
            SMineCartGlobal.ChangeMusicTrack(
                shouldPlaySound,
                "none",
                track_interruptable: false,
                MusicContext.MiniGame
            );
            if (!minecartLoop.IsPaused)
            {
                minecartLoop.Pause();
            }
            gameState = GameStates.FruitsSummary;
            player.enabled = false;
            stateTimer = 0.75f;
        }

        public void ShowMap()
        {
            if (gameMode == 2)
            {
                ShowCutscene();
                return;
            }
            gameState = GameStates.Map;
            mapTimer = 0f;
            screenDarkness = 1f;
            ResetState();
            player.enabled = false;
            SMineCartGlobal.ChangeMusicTrack(
                shouldPlaySound,
                "none",
                track_interruptable: false,
                MusicContext.MiniGame
            );

            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(256, 864, 16, 16),
                    new Vector2(261f, 106f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.15f,
                    0.2f
                )
                {
                    ySinWaveMagnitude = random.Next(1, 6)
                },
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(256, 864, 16, 16),
                    new Vector2(276f, 117f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.17f,
                    0.2f
                )
                {
                    ySinWaveMagnitude = random.Next(1, 6)
                },
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(256, 864, 16, 16),
                    new Vector2(234f, 136f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.19f,
                    0.2f
                )
                {
                    ySinWaveMagnitude = random.Next(1, 6)
                },
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(256, 864, 16, 16),
                    new Vector2(264f, 131f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.19f,
                    0.2f
                )
                {
                    ySinWaveMagnitude = random.Next(1, 6)
                },
                random
            );
            if (random.NextDouble() < 0.4)
            {
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(256, 864, 16, 16),
                        new Vector2(247f, 119f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.19f,
                        0.2f
                    )
                    {
                        ySinWaveMagnitude = random.Next(1, 6)
                    },
                    random
                );
            }
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(96, 864, 16, 16),
                    new Vector2(327f, 186f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.17f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(96, 864, 16, 16),
                    new Vector2(362f, 190f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.19f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(96, 864, 16, 16),
                    new Vector2(299f, 197f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.21f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(96, 864, 16, 16),
                    new Vector2(375f, 212f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.16f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(402, 660, 100, 72),
                    new Vector2(205f, 184f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    2,
                    0.765f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(0, 736, 48, 50),
                    new Vector2(280f, 66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    2,
                    0.765f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(402, 638, 3, 21),
                    new Vector2(234.66f, 66.66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    4,
                    0.1f,
                    0.55f
                ),
                random
            );
            if (currentTheme == 0)
            {
                AddEntity(
                    new CosmeticFallingBoulder(this, 72f, new Color(130, 96, 79), 96f, 0.45f),
                    random
                ).position = new Vector2(40 + random.Next(40), -16f);
                if (random.NextBool())
                {
                    AddEntity(
                        new CosmeticFallingBoulder(this, 72f, new Color(130, 96, 79), 80f, 0.5f),
                        random
                    ).position = new Vector2(80 + random.Next(40), -16f);
                }
                if (random.NextBool())
                {
                    AddEntity(
                        new CosmeticFallingBoulder(this, 72f, new Color(130, 96, 79), 88f, 0.55f),
                        random
                    ).position = new Vector2(120 + random.Next(40), -16f);
                }
            }
            else if (currentTheme == 1)
            {
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 604, 15, 12),
                            new Vector2(119f, 162f),
                            0f,
                            0f,
                            0f,
                            0f,
                            0.8f,
                            1f,
                            1,
                            0.1f,
                            0.55f
                        ),
                        random
                    )
                    .SetDestroySound("boulderBreak");
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 604, 15, 12),
                            new Vector2(49f, 166f),
                            0f,
                            0f,
                            0f,
                            0f,
                            1.2f,
                            1f,
                            1,
                            0.1f,
                            0.55f
                        ),
                        random
                    )
                    .SetDestroySound("boulderBreak");
                for (int n = 0; n < 4; n++)
                {
                    AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(421, 607, 5, 5),
                            new Vector2(119f, 162f),
                            random.Next(-30, 31),
                            random.Next(-50, -39),
                            0.25f,
                            1f,
                            0.75f,
                            1f,
                            1,
                            1f,
                            0.45f,
                            holdLastFrame: false,
                            0.8f
                        ),
                        random
                    );
                }
                for (int m = 0; m < 4; m++)
                {
                    AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(421, 607, 5, 5),
                            new Vector2(49f, 166f),
                            random.Next(-30, 31),
                            random.Next(-50, -39),
                            0.25f,
                            1f,
                            0.75f,
                            1f,
                            1,
                            1f,
                            0.45f,
                            holdLastFrame: false,
                            1.2f
                        ),
                        random
                    );
                }
            }
            else if (currentTheme == 3)
            {
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(455, 512, 58, 64),
                            new Vector2(250f, 136f),
                            0f,
                            0f,
                            0f,
                            0f,
                            0.8f,
                            1f,
                            1,
                            0.1f,
                            0.21f
                        ),
                        random
                    )
                    .SetDestroySound("barrelBreak");
                for (int l = 0; l < 32; l++)
                {
                    AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(51, 53, 9, 9),
                            new Vector2(250f, 136f)
                                + new Vector2(random.Next(-20, 31), random.Next(-20, 21)),
                            random.Next(-30, 31),
                            random.Next(-70, -39),
                            0.25f,
                            1f,
                            0.75f,
                            1f,
                            1,
                            1f,
                            0.45f,
                            holdLastFrame: false,
                            0.8f + 0.01f * (float)l
                        ),
                        random
                    );
                }
            }
            else if (currentTheme == 2)
            {
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(416, 368, 24, 16),
                        new Vector2(217f, 177f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.1f,
                        0.54f,
                        holdLastFrame: true,
                        0.8f
                    ),
                    random
                );
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(416, 368, 1, 1),
                            new Vector2(217f, 177f),
                            0f,
                            0f,
                            0f,
                            0f,
                            0.8f,
                            1f,
                            1,
                            0.1f,
                            0.55f
                        ),
                        random
                    )
                    .SetDestroySound("pullItemFromWater");
            }
            else if (currentTheme == 4)
            {
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 591, 12, 11),
                            new Vector2(328f, 197f),
                            0f,
                            0f,
                            0f,
                            0f,
                            99f,
                            1f,
                            4,
                            0.1f,
                            0.34f,
                            holdLastFrame: false,
                            2.5f
                        ),
                        random
                    )
                    .SetStartSound("fireball");
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 591, 12, 11),
                        new Vector2(336f, 197f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.1f,
                        0.35f,
                        holdLastFrame: false,
                        2.625f
                    ),
                    random
                );
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 591, 12, 11),
                            new Vector2(344f, 197f),
                            0f,
                            0f,
                            0f,
                            0f,
                            99f,
                            1f,
                            4,
                            0.1f,
                            0.34f,
                            holdLastFrame: false,
                            2.75f
                        ),
                        random
                    )
                    .SetStartSound("fireball");
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 591, 12, 11),
                        new Vector2(344f, 189f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.1f,
                        0.35f,
                        holdLastFrame: false,
                        2.825f
                    ),
                    random
                );
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 591, 12, 11),
                            new Vector2(344f, 181f),
                            0f,
                            0f,
                            0f,
                            0f,
                            99f,
                            1f,
                            4,
                            0.1f,
                            0.34f,
                            holdLastFrame: false,
                            3f
                        ),
                        random
                    )
                    .SetStartSound("fireball");
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 591, 12, 11),
                        new Vector2(344f, 173f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.1f,
                        0.35f,
                        holdLastFrame: false,
                        3.125f
                    ),
                    random
                );
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 591, 12, 11),
                            new Vector2(344f, 165f),
                            0f,
                            0f,
                            0f,
                            0f,
                            99f,
                            1f,
                            4,
                            0.1f,
                            0.34f,
                            holdLastFrame: false,
                            3.25f
                        ),
                        random
                    )
                    .SetStartSound("fireball");
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 591, 12, 11),
                        new Vector2(352f, 165f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.1f,
                        0.35f,
                        holdLastFrame: false,
                        3.325f
                    ),
                    random
                );
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 591, 12, 11),
                            new Vector2(360f, 165f),
                            0f,
                            0f,
                            0f,
                            0f,
                            99f,
                            1f,
                            4,
                            0.1f,
                            0.34f,
                            holdLastFrame: false,
                            3.5f
                        ),
                        random
                    )
                    .SetStartSound("fireball");
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 591, 12, 11),
                        new Vector2(360f, 157f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        4,
                        0.1f,
                        0.35f,
                        holdLastFrame: false,
                        3.625f
                    ),
                    random
                );
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(401, 591, 12, 11),
                            new Vector2(360f, 149f),
                            0f,
                            0f,
                            0f,
                            0f,
                            99f,
                            1f,
                            4,
                            0.1f,
                            0.34f,
                            holdLastFrame: false,
                            3.75f
                        ),
                        random
                    )
                    .SetStartSound("fireball");
            }
            else if (currentTheme == 5)
            {
                AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(416, 384, 16, 16),
                            new Vector2(213f, 34f),
                            0f,
                            0f,
                            0f,
                            0f,
                            5f,
                            1f,
                            6,
                            0.1f,
                            0.55f
                        ),
                        random
                    )
                    .SetDestroySound("slimedead");
                for (int k = 0; k < 8; k++)
                {
                    AddEntity(
                        new MineDebris(
                            this,
                            new Rectangle(427, 607, 6, 6),
                            new Vector2(205 + random.Next(3, 14), 26 + random.Next(6, 14)),
                            random.Next(-30, 31),
                            random.Next(-60, -39),
                            0.25f,
                            1f,
                            0.75f,
                            1f,
                            1,
                            1f,
                            0.45f,
                            holdLastFrame: false,
                            5f + (float)k * 0.005f
                        ),
                        random
                    );
                }
            }
            if (currentTheme == 9)
            {
                for (int j = 0; j < 8; j++)
                {
                    AddEntity(
                            new MineDebris(
                                this,
                                new Rectangle(368, 784, 16, 16),
                                new Vector2(274 + random.Next(-19, 20), 46 + random.Next(6, 14)),
                                random.Next(-4, 5),
                                -16f,
                                0f,
                                0.05f,
                                2f,
                                1f,
                                3,
                                0.33f,
                                0.35f,
                                holdLastFrame: true,
                                1f + (float)j * 0.1f
                            ),
                            random
                        )
                        .SetStartSound("dirtyHit");
                }
            }
            else if (currentTheme == 6)
            {
                for (int i = 0; i < 52; i++)
                {
                    AddEntity(
                        new CosmeticFallingBoulder(
                            this,
                            random.Next(72, 195),
                            new Color(100, 66, 49),
                            96 + random.Next(-10, 11),
                            0.65f + (float)i * 0.05f
                        ),
                        random
                    ).position = new Vector2(5 + random.Next(360), -16f);
                }
            }
            if (!levelThemesFinishedThisRun.Contains(1))
            {
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 604, 15, 12),
                        new Vector2(119f, 162f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        1,
                        0.1f,
                        0.55f
                    ),
                    random
                );
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(401, 604, 15, 12),
                        new Vector2(49f, 166f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        1,
                        0.1f,
                        0.55f
                    ),
                    random
                );
            }
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(415, levelThemesFinishedThisRun.Contains(0) ? 630 : 650, 10, 9),
                    new Vector2(88f, 87.66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    5,
                    0.1f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(415, levelThemesFinishedThisRun.Contains(1) ? 630 : 650, 10, 9),
                    new Vector2(105f, 183.66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    5,
                    0.1f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(415, levelThemesFinishedThisRun.Contains(5) ? 630 : 640, 10, 9),
                    new Vector2(169f, 119.66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    5,
                    0.1f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(415, levelThemesFinishedThisRun.Contains(4) ? 630 : 650, 10, 9),
                    new Vector2(328f, 199.66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    5,
                    0.1f,
                    0.55f
                ),
                random
            );
            AddEntity(
                new MineDebris(
                    this,
                    new Rectangle(415, levelThemesFinishedThisRun.Contains(6) ? 630 : 650, 10, 9),
                    new Vector2(361f, 72.66f),
                    0f,
                    0f,
                    0f,
                    0f,
                    99f,
                    1f,
                    5,
                    0.1f,
                    0.55f
                ),
                random
            );
            if (levelThemesFinishedThisRun.Contains(2))
            {
                AddEntity(
                    new MineDebris(
                        this,
                        new Rectangle(466, 642, 17, 17),
                        new Vector2(216.66f, 200.66f),
                        0f,
                        0f,
                        0f,
                        0f,
                        99f,
                        1f,
                        1,
                        0.17f,
                        0.52f
                    ),
                    random
                );
            }
            fadeDelta = -1f;
            MapJunimo map_junimo = AddEntity(new MapJunimo(this), random);
            LevelTransition[] lEVEL_TRANSITIONS = LEVEL_TRANSITIONS;
            foreach (LevelTransition transition in lEVEL_TRANSITIONS)
            {
                if (
                    transition.startLevel == currentTheme
                    && (transition.shouldTakePath == null || transition.shouldTakePath())
                )
                {
                    map_junimo.position = new Vector2(
                        ((float)transition.startGridCoordinates.X + 0.5f) * (float)tileSize,
                        ((float)transition.startGridCoordinates.Y + 0.5f) * (float)tileSize
                    );
                    map_junimo.moveString = transition.pathString;
                    currentTheme = transition.destinationLevel;
                    break;
                }
            }
        }

        public void ShowCutscene()
        {
            gameState = GameStates.Cutscene;
            screenDarkness = 1f;
            ResetState();
            player.enabled = false;
            setGameModeParameters();
            setUpTheme(currentTheme);
            cutsceneTick = 0;
            SMineCartGlobal.ChangeMusicTrack(
                shouldPlaySound,
                "none",
                track_interruptable: false,
                MusicContext.MiniGame
            );
            for (int i = 0; i < screenWidth / tileSize + 4; i++)
            {
                AddTrack(i, 10).visible = false;
            }
            player.SnapToFloor();
            if (gameMode == 2)
            {
                EndCutscene();
            }
        }

        public void PlayLevelMusic()
        {
            if (currentTheme == 0)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "EarthMine",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 1)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "FrostMine",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 2)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "junimoKart_whaleMusic",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 4)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "tribal",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 3)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "junimoKart_ghostMusic",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 5)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "junimoKart_slimeMusic",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 9)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "junimoKart_mushroomMusic",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 6)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "nightTime",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
            else if (currentTheme == 8)
            {
                SMineCartGlobal.ChangeMusicTrack(
                    shouldPlaySound,
                    "Upper_Ambient",
                    track_interruptable: false,
                    MusicContext.MiniGame
                );
            }
        }

        public void EndCutscene()
        {
            if (!minecartLoop.IsPaused)
            {
                minecartLoop.Pause();
            }
            gameState = GameStates.Ingame;
            GC.Collect(0, GCCollectionMode.Forced);
            ResetState();
            setUpTheme(currentTheme);
            PlayLevelMusic();
            player.enabled = true;
            createBeginningOfLevel();
            player.position.X = (float)tileSize * 0.5f;
            player.SnapToFloor();
            checkpointPosition = player.position.X;
            furthestGeneratedCheckpoint = 0;
            lastLevelWasPerfect = true;
            secondsOnThisLevel = 0f;
            if (currentTheme == 2)
            {
                AddEntity(new Whale(this), random);
                AddEntity(new PlayerBubbleSpawner(this), random);
            }
            if (currentTheme == 5)
            {
                AddEntity(new HugeSlime(this), random).position = new Vector2(0f, 0f);
            }
            screenDarkness = 1f;
            fadeDelta = -1f;
            if (gameMode == 3 && currentTheme == 7)
            {
                unload();
                Game1.globalFadeToClear(
                    delegate
                    {
                        Game1.drawObjectDialogue(
                            Game1.content.LoadString(
                                "Strings\\StringsFromCSFiles:MineCart.cs.12106"
                            )
                        );
                    },
                    0.015f
                );
                Game1.currentMinigame = null;
                DelayedAction.playSoundAfterDelay("discoverMineral", 1000);
            }
        }

        public void createSparkShower(Vector2 position)
        {
            int number = random.Next(3, 7);
            for (int i = 0; i < number; i++)
            {
                sparkShower.Add(
                    new Spark(
                        position.X - 3f,
                        position.Y,
                        (float)random.Next(-200, 5) / 100f,
                        (float)(-random.Next(5, 150)) / 100f
                    )
                );
            }
        }

        public void createSparkShower()
        {
            int number = random.Next(3, 7);
            for (int i = 0; i < number; i++)
            {
                sparkShower.Add(
                    new Spark(
                        player.drawnPosition.X - 3f,
                        player.drawnPosition.Y,
                        (float)random.Next(-200, 5) / 100f,
                        (float)(-random.Next(5, 150)) / 100f
                    )
                );
            }
        }

        public void CreateLakeDecor()
        {
            for (int i = 0; i < 16; i++)
            {
                lakeDecor.Add(new LakeDecor(this, currentTheme, random: random));
            }
        }

        public void CreateBGDecor()
        {
            for (int i = 0; i < 16; i++)
            {
                lakeDecor.Add(new LakeDecor(this, currentTheme, bgDecor: true, i, random: random));
            }
        }

        public void createBeginningOfLevel()
        {
            CreateLakeDecor();
            for (int i = 0; i < 15; i++)
            {
                AddTrack(generatorPosition.X, generatorPosition.Y);
                generatorPosition.X++;
            }
        }

        public void setGameModeParameters()
        {
            switch (gameMode)
            {
                case 3:
                    distanceToTravel = 350;
                    break;
                case 2:
                    distanceToTravel = 150;
                    break;
            }
        }

        public void AddValidObstacle(ObstacleTypes obstacle_type, Type type)
        {
            if (_validObstacles != null)
            {
                if (!_validObstacles.TryGetValue(obstacle_type, out var obstacleTypes))
                {
                    obstacleTypes = (_validObstacles[obstacle_type] = new List<Type>());
                }
                obstacleTypes.Add(type);
            }
        }

        public void setUpTheme(int whichTheme)
        {
            _generatorRolls = new List<GeneratorRoll>();
            _validObstacles = new Dictionary<ObstacleTypes, List<Type>>();
            float additional_trap_spawn_rate = 0f;
            float movement_speed_multiplier = 1f;
            if (gameState == GameStates.Cutscene)
            {
                additional_trap_spawn_rate = 0f;
                movement_speed_multiplier = 1f;
            }
            else if (gameMode == 2)
            {
                int cycle_completions = levelsBeat / infiniteModeLevels.Length;
                additional_trap_spawn_rate = (float)cycle_completions * 0.25f;
                movement_speed_multiplier = 1f + (float)cycle_completions * 0.25f;
            }
            midBGSource = new Rectangle(64, 0, 96, 162);
            backBGSource = new Rectangle(64, 162, 96, 111);
            lakeBGSource = new Rectangle(0, 80, 16, 97);
            backBGYOffset = tileSize * 2;
            midBGYOffset = 0;
            switch (whichTheme)
            {
                case 9:
                    AddValidObstacle(ObstacleTypes.Difficult, typeof(NoxiousMushroom));
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new MushroomBalanceTrackGenerator(this)
                                .SetHopSize(2, 2)
                                .SetReleaseJumpChance(1f)
                                .SetStaggerValues(0, -1, 3)
                                .SetTrackType(Track.TrackType.Straight)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.15f,
                            new MushroomBalanceTrackGenerator(this)
                                .SetHopSize(1, 1)
                                .SetReleaseJumpChance(1f)
                                .SetStaggerValues(-2, 4)
                                .SetTrackType(Track.TrackType.Straight)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.2f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-1, 0, 1)
                                .SetLength(4, 4)
                                .SetCheckpoint(checkpoint: true)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(4, 3)
                                .SetNumberOfHops(1, 1)
                                .SetReleaseJumpChance(0f)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(2)
                                .SetStaggerChance(0f)
                                .SetLength(7, 7)
                                .AddObstacle<StraightAwayGenerator>(ObstacleTypes.Difficult, 3)
                                .SetCheckpoint(checkpoint: false)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.2f,
                            new MushroomBunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 3)
                                .SetStaggerValues(-3, -1, 2, 3)
                                .SetReleaseJumpChance(0.25f)
                                .AddPickupFunction<MushroomBunnyHopGenerator>(
                                    BaseTrackGenerator.Always
                                )
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.05f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 3)
                                .SetStaggerValues(-3, -1, 2, 3)
                                .SetReleaseJumpChance(0.33f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.35f,
                            new BunnyHopGenerator(this)
                                .SetTrackType(Track.TrackType.MushroomMiddle)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 3)
                                .SetStaggerValues(-3, -4, 4)
                                .SetReleaseJumpChance(0.33f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.5f,
                            new MushroomBalanceTrackGenerator(this)
                                .SetHopSize(1, 1)
                                .SetReleaseJumpChance(1f)
                                .SetStaggerValues(-2, 4)
                                .SetTrackType(Track.TrackType.Straight)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(2, -1, 0, 1, 2)
                                .SetLength(3, 5)
                                .SetCheckpoint(checkpoint: true)
                        )
                    );
                    CreateBGDecor();
                    backBGTint = Color.White;
                    backBGSource = new Rectangle(0, 789, 96, 111);
                    midBGTint = Color.White;
                    caveTint = Color.Purple;
                    lakeBGSource = new Rectangle(304, 0, 16, 0);
                    lakeTint = new Color(0, 8, 46);
                    midBGSource = new Rectangle(416, 736, 96, 149);
                    midBGYOffset = -13;
                    waterfallTint = new Color(100, 0, 140) * 0.5f;
                    trackTint = new Color(130, 50, 230);
                    player.velocity.X = 120f;
                    trackShadowTint = new Color(0, 225, 225);
                    break;
                case 1:
                {
                    AddValidObstacle(ObstacleTypes.Normal, typeof(Roadblock));
                    AddValidObstacle(ObstacleTypes.Difficult, typeof(Roadblock));
                    BaseTrackGenerator wavy_generator = new StraightAwayGenerator(this)
                        .SetMinimumDistanceBetweenStaggers(2)
                        .SetStaggerChance(1f)
                        .SetStaggerValueRange(-1, 1)
                        .SetLength(4, 4)
                        .SetCheckpoint(checkpoint: true);
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.3f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 4)
                                .SetReleaseJumpChance(0.1f)
                                .SetStaggerValues(-2, -1)
                                .SetTrackType(Track.TrackType.UpSlope),
                            _IsGeneratingOnLowerHalf,
                            wavy_generator
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.15f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 4)
                                .SetReleaseJumpChance(0.1f)
                                .SetStaggerValues(3, 2, 1)
                                .SetTrackType(Track.TrackType.UpSlope),
                            _IsGeneratingOnUpperHalf,
                            wavy_generator
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.5f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(0)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(1)
                                .SetLength(3, 5)
                                .AddPickupFunction<StraightAwayGenerator>(
                                    BaseTrackGenerator.IceDownSlopesOnly
                                )
                                .AddObstacle<StraightAwayGenerator>(ObstacleTypes.Normal, -12)
                        )
                    );
                    _generatorRolls.Add(new GeneratorRoll(0.3f, wavy_generator));
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(2)
                                .SetStaggerChance(1f)
                                .SetStaggerValueRange(-1, 1)
                                .SetLength(3, 6)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Difficult,
                                    -13,
                                    0.5f + additional_trap_spawn_rate
                                )
                        )
                    );
                    backBGTint = new Color(93, 242, 255);
                    midBGTint = Color.White;
                    caveTint = new Color(230, 244, 254);
                    lakeBGSource = new Rectangle(304, 0, 16, 0);
                    lakeTint = new Color(147, 217, 255);
                    midBGSource = new Rectangle(320, 135, 96, 149);
                    midBGYOffset = -13;
                    waterfallTint = Color.LightCyan * 0.5f;
                    trackTint = new Color(186, 240, 255);
                    player.velocity.X = 85f;
                    // NoiseGenerator.Amplitude = 2.8;
                    // NoiseGenerator.Frequency = 0.18;
                    trackShadowTint = new Color(50, 145, 250);
                    break;
                }
                case 2:
                    backBGTint = Color.White;
                    midBGTint = Color.White;
                    caveTint = Color.SlateGray;
                    lakeTint = new Color(75, 104, 88);
                    waterfallTint = Color.White * 0f;
                    trackTint = new Color(100, 220, 255);
                    player.velocity.X = 85f;
                    // NoiseGenerator.Amplitude = 3.0;
                    // NoiseGenerator.Frequency = 0.15;
                    trackShadowTint = new Color(32, 45, 180);
                    midBGSource = new Rectangle(416, 0, 96, 69);
                    backBGSource = new Rectangle(320, 0, 96, 135);
                    backBGYOffset = 0;
                    lakeBGSource = new Rectangle(304, 0, 16, 0);
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this)
                                .SetLength(2, 5)
                                .SetDepth(-7, -3)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this).SetLength(1, 3).SetDepth(100, 100)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(2, -1, 0, 1, 2)
                                .SetLength(3, 5)
                                .SetCheckpoint(checkpoint: true)
                        )
                    );
                    CreateBGDecor();
                    if (gameMode != 2)
                    {
                        distanceToTravel = 300;
                    }
                    break;
                case 4:
                    AddValidObstacle(ObstacleTypes.Normal, typeof(FallingBoulderSpawner));
                    backBGTint = new Color(255, 137, 82);
                    midBGTint = new Color(255, 82, 40);
                    caveTint = Color.DarkRed;
                    lakeTint = Color.Red;
                    lakeBGSource = new Rectangle(304, 97, 16, 97);
                    trackTint = new Color(255, 160, 160);
                    waterfallTint = Color.Red * 0.9f;
                    trackShadowTint = Color.Orange;
                    player.velocity.X = 120f;
                    // NoiseGenerator.Amplitude = 3.0;
                    // NoiseGenerator.Frequency = 0.18;
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(3, 5)
                                .SetStaggerValues(-3, -1, 1, 3)
                                .SetReleaseJumpChance(0.33f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(0)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-1, 1)
                                .SetLength(5, 8)
                                .AddPickupFunction<StraightAwayGenerator>(BaseTrackGenerator.Always)
                                .SetCheckpoint(checkpoint: true)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -13,
                                    0.5f + additional_trap_spawn_rate
                                )
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(0)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-1, 1)
                                .SetLength(5, 8)
                                .AddPickupFunction<StraightAwayGenerator>(BaseTrackGenerator.Always)
                                .SetCheckpoint(checkpoint: true)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -13,
                                    0.5f + additional_trap_spawn_rate
                                )
                        )
                    );
                    break;
                case 3:
                    backBGTint = new Color(60, 60, 60);
                    midBGTint = new Color(60, 60, 60);
                    caveTint = new Color(70, 70, 70);
                    lakeTint = new Color(60, 70, 80);
                    trackTint = Color.DimGray;
                    waterfallTint = Color.Black * 0f;
                    trackShadowTint = Color.Black;
                    player.velocity.X = 120f;
                    // NoiseGenerator.Amplitude = 3.0;
                    // NoiseGenerator.Frequency = 0.2;
                    AddValidObstacle(ObstacleTypes.Normal, typeof(Roadblock));
                    AddValidObstacle(ObstacleTypes.Difficult, typeof(WillOWisp));
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new SmallGapGenerator(this).SetLength(3, 5).SetDepth(-10, -6)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this).SetLength(1, 3).SetDepth(3, 3)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(4, 3)
                                .SetNumberOfHops(1, 1)
                                .SetReleaseJumpChance(0f)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(2)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-1, 0, 0, -1)
                                .SetLength(7, 9)
                                .AddObstacle<StraightAwayGenerator>(ObstacleTypes.Difficult, -10)
                                .AddPickupFunction<StraightAwayGenerator>(
                                    BaseTrackGenerator.EveryOtherTile
                                )
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -13,
                                    0.75f + additional_trap_spawn_rate
                                )
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(2)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(4, -1, 0, 1, -4)
                                .SetLength(2, 6)
                                .AddPickupFunction<StraightAwayGenerator>(
                                    BaseTrackGenerator.EveryOtherTile
                                )
                        )
                    );
                    if (gameMode != 2)
                    {
                        distanceToTravel = 450;
                    }
                    else
                    {
                        distanceToTravel = (int)((float)distanceToTravel * 1.5f);
                    }
                    CreateBGDecor();
                    break;
                case 5:
                    AddValidObstacle(ObstacleTypes.Air, typeof(FallingBoulderSpawner));
                    AddValidObstacle(ObstacleTypes.Normal, typeof(Roadblock));
                    backBGTint = new Color(180, 250, 180);
                    midBGSource = new Rectangle(416, 69, 96, 162);
                    midBGTint = Color.White;
                    caveTint = new Color(255, 200, 60);
                    lakeTint = new Color(24, 151, 62);
                    trackTint = Color.LightSlateGray;
                    waterfallTint = new Color(0, 255, 180) * 0.5f;
                    trackShadowTint = new Color(0, 180, 50);
                    player.velocity.X = 100f;
                    slimeBossSpeed = player.velocity.X;
                    // NoiseGenerator.Amplitude = 3.1;
                    // NoiseGenerator.Frequency = 0.24;
                    lakeBGSource = new Rectangle(304, 0, 16, 0);
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(10, 10)
                                .SetNumberOfHops(1, 1)
                                .SetReleaseJumpChance(0.1f)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this)
                                .SetLength(2, 5)
                                .SetDepth(-7, -3)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(0)
                                .SetStaggerChance(1f)
                                .SetStaggerValueRange(-1, -1)
                                .SetLength(3, 5)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Air,
                                    -11,
                                    0.75f + additional_trap_spawn_rate
                                )
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetStaggerValues(1, -2)
                                .SetNumberOfHops(2, 2)
                                .SetReleaseJumpChance(0.25f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                                .SetTrackType(Track.TrackType.SlimeUpSlope)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-1, -1, 0, 2, 2)
                                .SetLength(3, 5)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -10,
                                    0.3f + additional_trap_spawn_rate
                                )
                        )
                    );
                    break;
                case 6:
                    backBGTint = Color.White;
                    midBGTint = Color.White;
                    caveTint = Color.Black;
                    lakeTint = Color.Black;
                    waterfallTint = Color.BlueViolet * 0.25f;
                    trackTint = new Color(150, 70, 120);
                    player.velocity.X = 110f;
                    // NoiseGenerator.Amplitude = 3.5;
                    // NoiseGenerator.Frequency = 0.35;
                    trackShadowTint = Color.Black;
                    midBGSource = new Rectangle(416, 231, 96, 53);
                    backBGSource = new Rectangle(320, 284, 96, 116);
                    backBGYOffset = 20;
                    AddValidObstacle(ObstacleTypes.Normal, typeof(Roadblock));
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new RapidHopsGenerator(this)
                                .SetLength(3, 5)
                                .SetYStep(-1)
                                .AddPickupFunction<RapidHopsGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new RapidHopsGenerator(this)
                                .SetLength(3, 5)
                                .SetYStep(2)
                                .SetChaotic(chaotic: true)
                                .AddPickupFunction<RapidHopsGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new RapidHopsGenerator(this).SetLength(3, 5).SetYStep(-2)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.05f,
                            new RapidHopsGenerator(this).SetLength(3, 5).SetYStep(3)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(4, 3)
                                .SetNumberOfHops(1, 1)
                                .SetReleaseJumpChance(0f)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(3, 5)
                                .SetStaggerValues(-3, -1, 1, 3)
                                .SetReleaseJumpChance(0.33f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValueRange(-1, 2)
                                .SetLength(3, 8)
                                .AddPickupFunction<StraightAwayGenerator>(
                                    BaseTrackGenerator.EveryOtherTile
                                )
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -10,
                                    0.75f + additional_trap_spawn_rate
                                )
                        )
                    );
                    generatorPosition.Y = screenHeight / tileSize - 2;
                    CreateBGDecor();
                    if (gameMode != 2)
                    {
                        distanceToTravel = 500;
                    }
                    break;
                case 0:
                    backBGTint = Color.DarkKhaki;
                    midBGTint = Color.SandyBrown;
                    caveTint = Color.SandyBrown;
                    lakeTint = Color.MediumAquamarine;
                    trackTint = Color.Beige;
                    waterfallTint = Color.MediumAquamarine * 0.9f;
                    trackShadowTint = new Color(60, 60, 60);
                    player.velocity.X = 95f;
                    // NoiseGenerator.Amplitude = 2.0;
                    // NoiseGenerator.Frequency = 0.12;
                    AddValidObstacle(ObstacleTypes.Normal, typeof(Roadblock));
                    AddValidObstacle(ObstacleTypes.Normal, typeof(FallingBoulderSpawner));
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this).SetLength(1, 3).SetDepth(2, 2)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(-2, -1, 1, 2)
                                .SetNumberOfHops(2, 2)
                                .SetReleaseJumpChance(1f)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.3f,
                            new SmallGapGenerator(this)
                                .SetLength(1, 1)
                                .SetDepth(-4, -2)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this)
                                .SetLength(1, 4)
                                .SetDepth(-3, -3)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 2)
                                .SetReleaseJumpChance(1f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.5f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(2)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-3, -2, -1, 2)
                                .SetLength(2, 4)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -11,
                                    0.3f + additional_trap_spawn_rate
                                )
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.015f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(-3, -4, 4, 3)
                                .SetNumberOfHops(1, 1)
                                .SetReleaseJumpChance(0.1f)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValueRange(-1, 1)
                                .SetLength(3, 5)
                                .AddObstacle<StraightAwayGenerator>(
                                    ObstacleTypes.Normal,
                                    -10,
                                    0.3f + additional_trap_spawn_rate
                                )
                        )
                    );
                    generatorPosition.Y = screenHeight / tileSize - 3;
                    break;
                case 8:
                    backBGTint = new Color(10, 30, 50);
                    midBGTint = Color.Black;
                    caveTint = Color.Black;
                    lakeTint = new Color(0, 60, 150);
                    trackTint = new Color(0, 90, 180);
                    waterfallTint = Color.MediumAquamarine * 0f;
                    trackShadowTint = new Color(0, 0, 60);
                    player.velocity.X = 100f;
                    generatorPosition.Y = screenHeight / tileSize - 4;
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this)
                                .SetLength(1, 3)
                                .SetDepth(2, 2)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.25f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(-2, -1, 1, 2)
                                .SetNumberOfHops(2, 2)
                                .SetReleaseJumpChance(1f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.3f,
                            new SmallGapGenerator(this)
                                .SetLength(1, 1)
                                .SetDepth(-4, -2)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new SmallGapGenerator(this)
                                .SetLength(1, 4)
                                .SetDepth(-3, -3)
                                .AddPickupFunction<SmallGapGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.1f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(1, 1)
                                .SetNumberOfHops(2, 2)
                                .SetReleaseJumpChance(1f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.5f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(2)
                                .SetStaggerChance(1f)
                                .SetStaggerValues(-3, -2, -1, 2)
                                .SetLength(2, 4)
                                .AddPickupFunction<StraightAwayGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            0.015f,
                            new BunnyHopGenerator(this)
                                .SetHopSize(2, 3)
                                .SetStaggerValues(-3, -4, 4, 3)
                                .SetNumberOfHops(1, 1)
                                .SetReleaseJumpChance(0.1f)
                                .AddPickupFunction<BunnyHopGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    _generatorRolls.Add(
                        new GeneratorRoll(
                            1f,
                            new StraightAwayGenerator(this)
                                .SetMinimumDistanceBetweenStaggers(1)
                                .SetStaggerChance(1f)
                                .SetStaggerValueRange(-1, 1)
                                .SetLength(3, 5)
                                .AddPickupFunction<StraightAwayGenerator>(BaseTrackGenerator.Always)
                        )
                    );
                    if (gameMode != 2)
                    {
                        distanceToTravel = 200;
                    }
                    break;
                case 7:
                    backBGTint = Color.DarkKhaki;
                    midBGTint = Color.SandyBrown;
                    caveTint = Color.SandyBrown;
                    lakeTint = Color.MediumAquamarine;
                    trackTint = Color.Beige;
                    waterfallTint = Color.MediumAquamarine * 0.9f;
                    trackShadowTint = new Color(60, 60, 60);
                    player.velocity.X = 95f;
                    break;
            }
            player.velocity.X *= movement_speed_multiplier;
            trackBuilderCharacter.velocity = player.velocity;
            currentTheme = whichTheme;
        }

        public int KeepTileInBounds(int y)
        {
            if (y < topTile)
            {
                return 4;
            }
            if (y > bottomTile)
            {
                return bottomTile;
            }
            return y;
        }

        public bool IsTileInBounds(int y)
        {
            if (y < topTile)
            {
                return false;
            }
            if (y > bottomTile)
            {
                return false;
            }
            return true;
        }

        public T GetOverlap<T>(ICollideable source)
            where T : Entity
        {
            Rectangle source_rect = source.GetBounds();
            foreach (Entity entity in _entities)
            {
                if (
                    entity.IsActive()
                    && entity is ICollideable collideable_entity
                    && entity is T match
                )
                {
                    Rectangle other_rect = collideable_entity.GetBounds();
                    if (source_rect.Intersects(other_rect))
                    {
                        return match;
                    }
                }
            }
            return null;
        }

        public List<T> GetOverlaps<T>(ICollideable source)
            where T : Entity
        {
            List<T> overlaps = new List<T>();
            Rectangle source_rect = source.GetBounds();
            foreach (Entity entity in _entities)
            {
                if (
                    entity.IsActive()
                    && entity is ICollideable collideable_entity
                    && entity is T match
                )
                {
                    Rectangle other_rect = collideable_entity.GetBounds();
                    if (source_rect.Intersects(other_rect))
                    {
                        overlaps.Add(match);
                    }
                }
            }
            return overlaps;
        }

        public Pickup CreatePickup(Vector2 position, bool fruit_only = false)
        {
            if (position.Y < (float)tileSize && !fruit_only)
            {
                return null;
            }
            Pickup pickup = null;
            int spawned_fruit = 0;
            for (int i = 0; i < 3 && _spawnedFruit.Contains((CollectableFruits)i); i++)
            {
                spawned_fruit++;
            }
            if (spawned_fruit <= 2)
            {
                float boundary_position = 0f;
                switch (spawned_fruit)
                {
                    case 0:
                        boundary_position = 0.15f * (float)distanceToTravel * (float)tileSize;
                        break;
                    case 1:
                        boundary_position = 0.48f * (float)distanceToTravel * (float)tileSize;
                        break;
                    case 2:
                        boundary_position = 0.81f * (float)distanceToTravel * (float)tileSize;
                        break;
                }
                if (position.X >= boundary_position)
                {
                    _spawnedFruit.Add((CollectableFruits)spawned_fruit);
                    pickup = AddEntity(
                        (Pickup)new Fruit(this, (CollectableFruits)spawned_fruit),
                        random
                    );
                }
            }
            if (pickup == null && !fruit_only)
            {
                pickup = AddEntity((Pickup)new Coin(this), random);
            }
            if (pickup != null)
            {
                pickup.position = position;
            }
            return pickup;
        }

        public void speedDraw()
        {
            _shakeOffset = new Vector2(
                Utility.Lerp(0f - shakeMagnitude, shakeMagnitude, (float)random.NextDouble()),
                Utility.Lerp(0f - shakeMagnitude, shakeMagnitude, (float)random.NextDouble())
            );

            for (int i2 = 0; i2 < lakeDecor.Count; i2++)
            {
                lakeDecor[i2].SpeedDraw(random);
            }

            for (int j = 0; j < 3; j++)
            {
                Vector2 shake_magnitude = Vector2.Zero;
                if (currentFruitCheckMagnitude > 0f && j == currentFruitCheckIndex - 1)
                {
                    shake_magnitude.X = Utility.Lerp(
                        0f - currentFruitCheckMagnitude,
                        currentFruitCheckMagnitude,
                        (float)random.NextDouble()
                    );
                    shake_magnitude.Y = Utility.Lerp(
                        0f - currentFruitCheckMagnitude,
                        currentFruitCheckMagnitude,
                        (float)random.NextDouble()
                    );
                }
            }
        }

        public void draw(SpriteBatch b)
        {
            _shakeOffset = new Vector2(
                Utility.Lerp(0f - shakeMagnitude, shakeMagnitude, (float)random.NextDouble()),
                Utility.Lerp(0f - shakeMagnitude, shakeMagnitude, (float)random.NextDouble())
            );
            if (gamePaused)
            {
                _shakeOffset = Vector2.Zero;
            }
            Rectangle cached_scissor_rect = b.GraphicsDevice.ScissorRectangle;
            Game1.isUsingBackToFrontSorting = true;
            if (shouldDraw)
            {
                b.Begin(
                    SpriteSortMode.BackToFront,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    null,
                    Utility.ScissorEnabled
                );
            }
            Rectangle scissor_rect = new Rectangle(
                (int)upperLeft.X,
                (int)upperLeft.Y,
                (int)((float)screenWidth * pixelScale),
                (int)((float)screenHeight * pixelScale)
            );
            scissor_rect = Utility.ConstrainScissorRectToScreen(scissor_rect);
            b.GraphicsDevice.ScissorRectangle = scissor_rect;
            if (gameState != GameStates.Map)
            {
                if (gameState == GameStates.FruitsSummary)
                {
                    perfectText?.draw(b, TransformDraw(new Vector2(80f, 40f)));
                }
                else if (gameState != GameStates.Cutscene)
                {
                    for (int n = 0; n <= screenWidth / tileSize + 1; n++)
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(
                                new Rectangle(
                                    n * tileSize - (int)lakeSpeedAccumulator % tileSize,
                                    tileSize * 9,
                                    tileSize,
                                    screenHeight - 96
                                )
                            ),
                            lakeBGSource,
                            lakeTint,
                            0f,
                            Vector2.Zero,
                            SpriteEffects.None,
                            0.9f
                        );
                    }
                    for (int i2 = 0; i2 < lakeDecor.Count; i2++)
                    {
                        lakeDecor[i2].Draw(b, random);
                    }
                    for (int i3 = 0; i3 <= screenWidth / backBGSource.Width + 2; i3++)
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(
                                new Vector2(
                                    0f - backBGPosition + (float)(i3 * backBGSource.Width),
                                    backBGYOffset
                                )
                            ),
                            backBGSource,
                            backBGTint,
                            0f,
                            Vector2.Zero,
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.7f
                        );
                    }
                    for (int i4 = 0; i4 < screenWidth / midBGSource.Width + 2; i4++)
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(
                                new Vector2(
                                    0f - midBGPosition + (float)(i4 * midBGSource.Width),
                                    162 - midBGSource.Height + midBGYOffset
                                )
                            ),
                            midBGSource,
                            midBGTint,
                            0f,
                            Vector2.Zero,
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.6f
                        );
                    }
                }
            }
            foreach (Entity entity in _entities)
            {
                if (entity.IsOnScreen())
                {
                    entity.Draw(b);
                }
            }
            foreach (Spark s in sparkShower)
            {
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    Game1.staminaRect,
                    TransformDraw(new Rectangle((int)s.x, (int)s.y, 1, 1)),
                    null,
                    s.c,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.3f
                );
            }
            if (gameState == GameStates.Title)
            {
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    texture,
                    TransformDraw(new Vector2(screenWidth / 2 - 128, screenHeight / 2 - 35)),
                    new Rectangle(256, 409, 256, 71),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    GetPixelScale(),
                    SpriteEffects.None,
                    0.25f
                );
                if (gameMode == 2)
                {
                    Vector2 score_offset = new Vector2(125f, 0f);
                    Vector2 draw_position3 = new Vector2(
                        (float)(screenWidth / 2) - score_offset.X / 2f,
                        155f
                    );
                    for (int i5 = 0; i5 < 5 && i5 < _currentHighScores.Count; i5++)
                    {
                        Color color = Color.White;
                        if (i5 == 0)
                        {
                            color = Utility.GetPrismaticColor();
                        }
                        KeyValuePair<string, int> score = _currentHighScores[i5];
                        int score_text_width =
                            (int)Game1.dialogueFont.MeasureString(score.Value.ToString() ?? "").X
                            / 4;
                        SMineCartGlobal.DrawString(
                            shouldDraw,
                            b,
                            Game1.dialogueFont,
                            "#" + (i5 + 1),
                            TransformDraw(draw_position3),
                            color,
                            0f,
                            Vector2.Zero,
                            GetPixelScale() / 4f,
                            SpriteEffects.None,
                            0.199f
                        );
                        SMineCartGlobal.DrawString(
                            shouldDraw,
                            b,
                            Game1.dialogueFont,
                            score.Key,
                            TransformDraw(draw_position3 + new Vector2(16f, 0f)),
                            color,
                            0f,
                            Vector2.Zero,
                            GetPixelScale() / 4f,
                            SpriteEffects.None,
                            0.199f
                        );
                        SMineCartGlobal.DrawString(
                            shouldDraw,
                            b,
                            Game1.dialogueFont,
                            score.Value.ToString() ?? "",
                            TransformDraw(
                                draw_position3 + score_offset - new Vector2(score_text_width, 0f)
                            ),
                            color,
                            0f,
                            Vector2.Zero,
                            GetPixelScale() / 4f,
                            SpriteEffects.None,
                            0.199f
                        );
                        Vector2 shadow_offset = new Vector2(1f, 1f);
                        SMineCartGlobal.DrawString(
                            shouldDraw,
                            b,
                            Game1.dialogueFont,
                            "#" + (i5 + 1),
                            TransformDraw(draw_position3 + shadow_offset),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            GetPixelScale() / 4f,
                            SpriteEffects.None,
                            0.1999f
                        );
                        SMineCartGlobal.DrawString(
                            shouldDraw,
                            b,
                            Game1.dialogueFont,
                            score.Key,
                            TransformDraw(draw_position3 + new Vector2(16f, 0f) + shadow_offset),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            GetPixelScale() / 4f,
                            SpriteEffects.None,
                            0.1999f
                        );
                        SMineCartGlobal.DrawString(
                            shouldDraw,
                            b,
                            Game1.dialogueFont,
                            score.Value.ToString() ?? "",
                            TransformDraw(
                                draw_position3
                                    + score_offset
                                    - new Vector2(score_text_width, 0f)
                                    + shadow_offset
                            ),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            GetPixelScale() / 4f,
                            SpriteEffects.None,
                            0.1999f
                        );
                        draw_position3.Y += 10f;
                    }
                }
            }
            else if (gameState == GameStates.Map)
            {
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    texture,
                    TransformDraw(new Vector2(0f, 0f)),
                    new Rectangle(0, 512, 400, 224),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    GetPixelScale(),
                    SpriteEffects.None,
                    0.6f
                );
                if (!levelThemesFinishedThisRun.Contains(3))
                {
                    SMineCartGlobal.Draw(
                        shouldDraw,
                        b,
                        texture,
                        TransformDraw(new Vector2(221f, 104f)),
                        new Rectangle(455, 512, 57, 64),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        GetPixelScale(),
                        SpriteEffects.None,
                        0.21f
                    );
                }
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    texture,
                    TransformDraw(new Vector2(369f, 51f)),
                    new Rectangle(480, 579, 31, 32),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    GetPixelScale(),
                    SpriteEffects.None,
                    0.21f
                );
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    texture,
                    TransformDraw(new Vector2(109f, 198f)),
                    new Rectangle(420, 512, 25, 26),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    GetPixelScale(),
                    SpriteEffects.None,
                    0.21f
                );
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    texture,
                    TransformDraw(new Vector2(229f, 213f)),
                    new Rectangle(425, 541, 9, 11),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    GetPixelScale(),
                    SpriteEffects.None,
                    0.21f
                );
            }
            else if (gameState != GameStates.FruitsSummary)
            {
                if (gameState == GameStates.Cutscene)
                {
                    float scale_adjustment = GetPixelScale() / 4f;
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        cutsceneText,
                        TransformDraw(
                            new Vector2(
                                screenWidth / 2
                                    - (int)(
                                        Game1.dialogueFont.MeasureString(cutsceneText).X / 2f / 4f
                                    ),
                                32f
                            )
                        ),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        scale_adjustment,
                        SpriteEffects.None,
                        0.199f
                    );
                }
                else
                {
                    for (int j2 = 0; j2 < waterfallWidth; j2 += 2)
                    {
                        for (int m = -2; m <= screenHeight / tileSize + 1; m++)
                        {
                            SMineCartGlobal.Draw(
                                shouldDraw,
                                b,
                                texture,
                                TransformDraw(
                                    new Vector2(
                                        (float)(screenWidth + tileSize * j2) - waterFallPosition,
                                        m * tileSize
                                            + (int)(_totalTime * 48.0 + (double)(tileSize * 100))
                                                % tileSize
                                    )
                                ),
                                new Rectangle(48, 32, 16, 16),
                                waterfallTint,
                                0f,
                                Vector2.Zero,
                                GetPixelScale(),
                                SpriteEffects.None,
                                0.2f
                            );
                        }
                    }
                }
            }
            if (
                !gamePaused
                && (
                    gameState == GameStates.Ingame
                    || gameState == GameStates.Cutscene
                    || gameState == GameStates.FruitsSummary
                    || gameState == GameStates.Map
                )
            )
            {
                _shakeOffset = Vector2.Zero;
                Vector2 draw_position2 = new Vector2(4f, 4f);
                if (gameMode == 2)
                {
                    string txtbestScore = Game1.content.LoadString(
                        "Strings\\StringsFromCSFiles:MineCart.cs.12115"
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.content.LoadString(
                            "Strings\\StringsFromCSFiles:FishingGame.cs.10444",
                            this.score
                        ),
                        TransformDraw(draw_position2),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.1f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.content.LoadString(
                            "Strings\\StringsFromCSFiles:FishingGame.cs.10444",
                            this.score
                        ),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.11f
                    );
                    draw_position2.Y += 10f;
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        txtbestScore + currentHighScore,
                        TransformDraw(draw_position2),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.1f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        txtbestScore + currentHighScore,
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.11f
                    );
                }
                else
                {
                    draw_position2.X = 4f;
                    for (int l = 0; l < livesLeft; l++)
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2),
                            new Rectangle(160, 32, 16, 16),
                            Color.White,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.07f
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                            new Rectangle(160, 32, 16, 16),
                            Color.Black,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.071f
                        );
                        draw_position2.X += 18f;
                        if (draw_position2.X > 90f && l < livesLeft - 1)
                        {
                            draw_position2.X = 4f;
                            draw_position2.Y += 18f;
                        }
                    }
                    draw_position2.X = 4f;
                    draw_position2.X += 36f;
                    for (int k = livesLeft; k < 3; k++)
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2),
                            new Rectangle(160, 48, 16, 16),
                            Color.White,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.07f
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                            new Rectangle(160, 48, 16, 16),
                            Color.Black,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.071f
                        );
                        draw_position2.X -= 18f;
                    }
                }
                draw_position2.X = 4f;
                draw_position2.Y += 18f;
                for (int j = 0; j < 3; j++)
                {
                    Vector2 shake_magnitude = Vector2.Zero;
                    if (currentFruitCheckMagnitude > 0f && j == currentFruitCheckIndex - 1)
                    {
                        shake_magnitude.X = Utility.Lerp(
                            0f - currentFruitCheckMagnitude,
                            currentFruitCheckMagnitude,
                            (float)random.NextDouble()
                        );
                        shake_magnitude.Y = Utility.Lerp(
                            0f - currentFruitCheckMagnitude,
                            currentFruitCheckMagnitude,
                            (float)random.NextDouble()
                        );
                    }
                    if (_collectedFruit.Contains((CollectableFruits)j))
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + shake_magnitude),
                            new Rectangle(160 + j * 16, 0, 16, 16),
                            Color.White,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.07f
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + new Vector2(1f, 1f) + shake_magnitude),
                            new Rectangle(160 + j * 16, 0, 16, 16),
                            Color.Black,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.075f
                        );
                    }
                    else
                    {
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + shake_magnitude),
                            new Rectangle(160 + j * 16, 16, 16, 16),
                            Color.White,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.07f
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + shake_magnitude + new Vector2(1f, 1f)),
                            new Rectangle(160 + j * 16, 16, 16, 16),
                            Color.Black,
                            0f,
                            new Vector2(0f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.075f
                        );
                    }
                    draw_position2.X += 18f;
                }
                if (gameMode == 3)
                {
                    draw_position2.X = 4f;
                    draw_position2.Y += 18f;
                    SMineCartGlobal.Draw(
                        shouldDraw,
                        b,
                        texture,
                        TransformDraw(draw_position2),
                        new Rectangle(0, 272, 9, 11),
                        Color.White,
                        0f,
                        new Vector2(0f, 0f),
                        GetPixelScale(),
                        SpriteEffects.None,
                        0.07f
                    );
                    SMineCartGlobal.Draw(
                        shouldDraw,
                        b,
                        texture,
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                        new Rectangle(0, 272, 9, 11),
                        Color.Black,
                        0f,
                        new Vector2(0f, 0f),
                        GetPixelScale(),
                        SpriteEffects.None,
                        0.08f
                    );
                    draw_position2.X += 12f;
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.01f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)) + new Vector2(-3f, -3f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)) + new Vector2(-2f, -2f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)) + new Vector2(-1f, -1f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f))
                            + new Vector2(-3.5f, -3.5f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f))
                            + new Vector2(-1.5f, -1.5f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        coinCount.ToString("00"),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f))
                            + new Vector2(-2.5f, -2.5f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                }
                if (Game1.IsMultiplayer)
                {
                    string time_of_day_string = Game1.getTimeOfDayString(Game1.timeOfDay);
                    draw_position2 = new Vector2(
                        (float)screenWidth
                            - Game1.dialogueFont.MeasureString(time_of_day_string).X / 4f
                            - 4f,
                        4f
                    );
                    Color timeColor = Color.White;
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2),
                        timeColor,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.01f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)) + new Vector2(-3f, -3f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)) + new Vector2(-2f, -2f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)) + new Vector2(-1f, -1f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f))
                            + new Vector2(-3.5f, -3.5f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f))
                            + new Vector2(-1.5f, -1.5f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        Game1.getTimeOfDayString(Game1.timeOfDay),
                        TransformDraw(draw_position2 + new Vector2(1f, 1f))
                            + new Vector2(-2.5f, -2.5f),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.02f
                    );
                }
                if (gameState == GameStates.Ingame)
                {
                    float left_edge = (float)(screenWidth - 192) / 2f;
                    float right_edge = left_edge + 192f;
                    draw_position2 = new Vector2(left_edge, 4f);
                    for (int i = 0; i < 12; i++)
                    {
                        Rectangle source_rect = new Rectangle(192, 48, 16, 16);
                        if (i == 0)
                        {
                            source_rect = new Rectangle(176, 48, 16, 16);
                        }
                        else if (i >= 11)
                        {
                            source_rect = new Rectangle(207, 48, 16, 16);
                        }
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2),
                            source_rect,
                            Color.White,
                            0f,
                            Vector2.Zero,
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.15f
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                            source_rect,
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.17f
                        );
                        draw_position2.X += 16f;
                    }
                    SMineCartGlobal.Draw(
                        shouldDraw,
                        b,
                        texture,
                        TransformDraw(draw_position2),
                        new Rectangle(176, 64, 16, 16),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        GetPixelScale(),
                        SpriteEffects.None,
                        0.15f
                    );
                    draw_position2.X += 8f;
                    string level_text = (levelsBeat + 1).ToString() ?? "";
                    draw_position2.Y += 3f;
                    SMineCartGlobal.DrawString(
                        shouldDraw,
                        b,
                        Game1.dialogueFont,
                        level_text,
                        TransformDraw(
                            draw_position2
                                - new Vector2(
                                    Game1.dialogueFont.MeasureString(level_text).X / 2f / 4f,
                                    0f
                                )
                        ),
                        Color.Black,
                        0f,
                        Vector2.Zero,
                        GetPixelScale() / 4f,
                        SpriteEffects.None,
                        0.1f
                    );
                    draw_position2.X += 1f;
                    draw_position2.Y += 1f;
                    draw_position2 = new Vector2(left_edge, 4f);
                    if (player != null && player.visible)
                    {
                        draw_position2.X = Utility.Lerp(
                            left_edge,
                            right_edge,
                            Math.Min(player.position.X / (float)(distanceToTravel * tileSize), 1f)
                        );
                    }
                    SMineCartGlobal.Draw(
                        shouldDraw,
                        b,
                        texture,
                        TransformDraw(draw_position2),
                        new Rectangle(240, 48, 16, 16),
                        Color.White,
                        0f,
                        new Vector2(8f, 0f),
                        GetPixelScale(),
                        SpriteEffects.None,
                        0.12f
                    );
                    SMineCartGlobal.Draw(
                        shouldDraw,
                        b,
                        texture,
                        TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                        new Rectangle(240, 48, 16, 16),
                        Color.Black,
                        0f,
                        new Vector2(8f, 0f),
                        GetPixelScale(),
                        SpriteEffects.None,
                        0.13f
                    );
                    if (checkpointPosition > (float)tileSize * 0.5f)
                    {
                        draw_position2.X = Utility.Lerp(
                            left_edge,
                            right_edge,
                            checkpointPosition / (float)(distanceToTravel * tileSize)
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2),
                            new Rectangle(224, 48, 16, 16),
                            Color.White,
                            0f,
                            new Vector2(8f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.125f
                        );
                        SMineCartGlobal.Draw(
                            shouldDraw,
                            b,
                            texture,
                            TransformDraw(draw_position2 + new Vector2(1f, 1f)),
                            new Rectangle(224, 48, 16, 16),
                            Color.Black,
                            0f,
                            new Vector2(8f, 0f),
                            GetPixelScale(),
                            SpriteEffects.None,
                            0.135f
                        );
                    }
                }
            }
            if (gameMode == 2 && Game1.IsMultiplayer && gameState != 0)
            {
                Game1.player.team.junimoKartStatus.Draw(
                    b,
                    TransformDraw(new Vector2(4f, screenHeight - 4)),
                    GetPixelScale(),
                    0.01f,
                    PlayerStatusList.HorizontalAlignment.Left,
                    PlayerStatusList.VerticalAlignment.Bottom
                );
            }
            if (screenDarkness > 0f)
            {
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    Game1.staminaRect,
                    TransformDraw(new Rectangle(0, 0, screenWidth, screenHeight + tileSize)),
                    null,
                    Color.Black * screenDarkness,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.145f
                );
            }
            if (gamePaused)
            {
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    Game1.staminaRect,
                    TransformDraw(new Rectangle(0, 0, screenWidth, screenHeight + tileSize)),
                    null,
                    Color.Black * 0.75f,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.145f
                );
                string current_text = Game1.content.LoadString(
                    "Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378"
                );
                Vector2 draw_position = default(Vector2);
                draw_position.X = screenWidth / 2;
                draw_position.Y = screenHeight / 4;
                SMineCartGlobal.DrawString(
                    shouldDraw,
                    b,
                    Game1.dialogueFont,
                    current_text,
                    TransformDraw(
                        draw_position
                            - new Vector2(
                                Game1.dialogueFont.MeasureString(current_text).X / 2f / 4f,
                                0f
                            )
                    ),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    GetPixelScale() / 4f,
                    SpriteEffects.None,
                    0.1f
                );
            }
            if (!Game1.options.hardwareCursor && !Game1.options.gamepadControls)
            {
                SMineCartGlobal.Draw(
                    shouldDraw,
                    b,
                    Game1.mouseCursors,
                    new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                    Game1.getSourceRectForStandardTileSheet(
                        Game1.mouseCursors,
                        Game1.options.gamepadControls ? 44 : 0,
                        16,
                        16
                    ),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f + Game1.dialogueButtonScale / 150f,
                    SpriteEffects.None,
                    0.0001f
                );
            }
            if (shouldDraw)
            {
                b.End();
            }
            Game1.isUsingBackToFrontSorting = false;
            b.GraphicsDevice.ScissorRectangle = cached_scissor_rect;
        }

        public float GetPixelScale()
        {
            return pixelScale;
        }

        public Rectangle TransformDraw(Rectangle dest)
        {
            dest.X =
                (int)Math.Round(((float)dest.X + _shakeOffset.X) * pixelScale) + (int)upperLeft.X;
            dest.Y =
                (int)Math.Round(((float)dest.Y + _shakeOffset.Y) * pixelScale) + (int)upperLeft.Y;
            dest.Width = (int)((float)dest.Width * pixelScale);
            dest.Height = (int)((float)dest.Height * pixelScale);
            return dest;
        }

        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public Vector2 TransformDraw(Vector2 dest)
        {
            dest.X = (int)Math.Round((dest.X + _shakeOffset.X) * pixelScale) + (int)upperLeft.X;
            dest.Y = (int)Math.Round((dest.Y + _shakeOffset.Y) * pixelScale) + (int)upperLeft.Y;
            return dest;
        }

        public void changeScreenSize()
        {
            screenWidth = 400;
            screenHeight = 220;
            float pixel_zoom_adjustment = 1f / Game1.options.zoomLevel;
            int viewport_width = Game1.game1.localMultiplayerWindow.Width;
            int viewport_height = Game1.game1.localMultiplayerWindow.Height;
            pixelScale = Math.Min(
                5,
                (int)
                    Math.Floor(
                        Math.Min(
                            (float)(viewport_width / screenWidth) * pixel_zoom_adjustment,
                            (float)(viewport_height / screenHeight) * pixel_zoom_adjustment
                        )
                    )
            );
            upperLeft = new Vector2(
                (float)(viewport_width / 2) * pixel_zoom_adjustment,
                (float)(viewport_height / 2) * pixel_zoom_adjustment
            );
            upperLeft.X -= (float)(screenWidth / 2) * pixelScale;
            upperLeft.Y -= (float)(screenHeight / 2) * pixelScale;
            tileSize = 16;
            ytileOffset = screenHeight / 2 / tileSize;
        }

        public void unload()
        {
            Game1.stopMusicTrack(MusicContext.MiniGame);
            Game1.player.team.junimoKartStatus.WithdrawState();
            Game1.player.faceDirection(0);
            if (minecartLoop != null && minecartLoop.IsPlaying)
            {
                minecartLoop.Stop(AudioStopOptions.Immediate);
            }
        }

        public bool forceQuit()
        {
            if (gameState != GameStates.Cutscene && gameState != 0 && gameMode == 2)
            {
                submitHighScore();
            }
            unload();
            return true;
        }

        public void leftClickHeld(int x, int y) { }

        public void receiveEventPoke(int data)
        {
            throw new NotImplementedException();
        }

        public string minigameId()
        {
            return "MineCart";
        }

        public bool doMainGameUpdates()
        {
            return false;
        }

        public SMineCart Clone()
        {
            if (perfectText != null) { }
            SMineCart clone = new SMineCart
            {
                StartFrame = StartFrame,
                CurrentFrame = CurrentFrame,
                random = random.Copy(),
                shouldDraw = shouldDraw,
                shouldPlaySound = shouldPlaySound,
                buttonPresses = new List<bool>(buttonPresses),

                gameState = gameState,
                pixelScale = pixelScale,
                shakeMagnitude = shakeMagnitude,
                _shakeOffset = _shakeOffset,
                slimeBossPosition = slimeBossPosition,
                slimeBossSpeed = slimeBossSpeed,
                secondsOnThisLevel = secondsOnThisLevel,
                fruitEatCount = fruitEatCount,
                currentFruitCheckIndex = currentFruitCheckIndex,
                currentFruitCheckMagnitude = currentFruitCheckMagnitude,
                coinCount = coinCount,
                gamePaused = gamePaused,
                lakeSpeedAccumulator = lakeSpeedAccumulator,
                backBGPosition = backBGPosition,
                midBGPosition = midBGPosition,
                waterFallPosition = waterFallPosition,
                upperLeft = upperLeft,
                titleJunimoStartedBobbing = titleJunimoStartedBobbing,
                lastLevelWasPerfect = lastLevelWasPerfect,
                completelyPerfect = completelyPerfect,
                screenWidth = screenWidth,
                screenHeight = screenHeight,
                tileSize = tileSize,
                waterfallWidth = waterfallWidth,
                ytileOffset = ytileOffset,
                score = score,
                levelsBeat = levelsBeat,
                gameMode = gameMode,
                livesLeft = livesLeft,
                distanceToTravel = distanceToTravel,
                respawnCounter = respawnCounter,
                currentTheme = currentTheme,
                reachedFinish = reachedFinish,
                gameOver = gameOver,
                screenDarkness = screenDarkness,
                cutsceneText = cutsceneText,
                fadeDelta = fadeDelta,

                backBGTint = backBGTint,
                midBGTint = midBGTint,
                caveTint = caveTint,
                lakeTint = lakeTint,
                waterfallTint = waterfallTint,
                trackShadowTint = trackShadowTint,
                trackTint = trackTint,
                midBGSource = midBGSource,
                backBGSource = backBGSource,
                lakeBGSource = lakeBGSource,
                backBGYOffset = backBGYOffset,
                midBGYOffset = midBGYOffset,
                _totalTime = _totalTime,
                screenLeftBound = screenLeftBound,
                generatorPosition = generatorPosition,
                bottomTile = bottomTile,
                topTile = topTile,
                deathTimer = deathTimer,
                _lastTilePosition = _lastTilePosition,
                slimeResetPosition = slimeResetPosition,
                checkpointPosition = checkpointPosition,
                furthestGeneratedCheckpoint = furthestGeneratedCheckpoint,
                isJumpPressed = isJumpPressed,
                stateTimer = stateTimer,
                cutsceneTick = cutsceneTick,
                pauseBeforeTitleFadeOutTimer = pauseBeforeTitleFadeOutTimer,
                mapTimer = mapTimer,
                currentHighScore = currentHighScore,
                scoreUpdateTimer = scoreUpdateTimer,
                _trackAddedFlip = _trackAddedFlip,
                _buttonState = _buttonState,
                _wasJustChatting = _wasJustChatting,

                // private Texture2D texture;
                texture = texture,
                // private ICue minecartLoop;
                minecartLoop = minecartLoop,

                // private Dictionary<int, List<Track>> _tracks;
                _tracks = new Dictionary<int, List<Track>>(),
                // private List<LakeDecor> lakeDecor = new List<LakeDecor>();
                lakeDecor = new List<LakeDecor>(),
                // private List<Point> obstacles = new List<Point>();
                obstacles = new List<Point>(obstacles),
                // private List<Spark> sparkShower = new List<Spark>();
                sparkShower = new List<Spark>(),
                // private List<int> levelThemesFinishedThisRun = new List<int>();
                levelThemesFinishedThisRun = new List<int>(levelThemesFinishedThisRun),

                // public LevelTransition[] LEVEL_TRANSITIONS;
                LEVEL_TRANSITIONS = LEVEL_TRANSITIONS,

                // public List<int> checkpointPositions;
                checkpointPositions = new List<int>(checkpointPositions),
                // private List<KeyValuePair<string, int>> _currentHighScores;
                _currentHighScores = new List<KeyValuePair<string, int>>(_currentHighScores),
                // protected HashSet<CollectableFruits> _spawnedFruit;
                _spawnedFruit = new HashSet<CollectableFruits>(_spawnedFruit),
                // protected HashSet<CollectableFruits> _collectedFruit;
                _collectedFruit = new HashSet<CollectableFruits>(_collectedFruit),

                // public Stopwatch musicSW;
                musicSW = musicSW?.Clone(),
            };

            // FIXME: need to not allow cloning when perfectText is not null
            // ONLY exists in progress mode
            clone.perfectText = null;

            foreach (LakeDecor decor in lakeDecor)
            {
                clone.lakeDecor.Add(decor.Clone(clone));
            }

            foreach (Spark spark in sparkShower)
            {
                clone.sparkShower.Add(spark.Clone());
            }

            // private MineDebris titleScreenJunimo;
            clone.titleScreenJunimo = titleScreenJunimo.Clone(clone);
            // private List<Entity> _entities;
            clone._entities = new List<Entity>();
            List<int> obstacleTrackIndexes = new List<int>();
            foreach (Entity entity in _entities)
            {
                Entity nextEntity = entity.Clone(clone);
                if (entity is Track track)
                {
                    if (track.obstacle != null)
                    {
                        obstacleTrackIndexes.Add(clone._entities.Count);
                    }
                    clone.AddTrack(nextEntity as Track);
                }
                else if (entity is Obstacle obstacle)
                {
                    if (obstacle is NoxiousMushroom mushroom)
                    {
                        // need to find track that is tied to this mushroom
                        for (int i = 0; i < obstacleTrackIndexes.Count; i++)
                        {
                            Track obstacleTrack = (Track)clone._entities[obstacleTrackIndexes[i]];
                            if (obstacleTrack.position == mushroom._track.position)
                            {
                                obstacleTrack.obstacle = nextEntity as Obstacle;
                                (nextEntity as NoxiousMushroom)._track = obstacleTrack;
                                break;
                            }
                        }
                    }
                    else if (obstacle is FallingBoulderSpawner spawner)
                    {
                        // need to find track that is tied to this spawner
                        for (int i = 0; i < obstacleTrackIndexes.Count; i++)
                        {
                            Track obstacleTrack = (Track)clone._entities[obstacleTrackIndexes[i]];
                            if (obstacleTrack.position == spawner._track.position)
                            {
                                obstacleTrack.obstacle = nextEntity as Obstacle;
                                (nextEntity as FallingBoulderSpawner)._track = obstacleTrack;
                                break;
                            }
                        }
                    }
                    clone._entities.Add(nextEntity);
                }
                // private MineCartCharacter player;
                else if (entity == player)
                {
                    clone.player = (PlayerMineCartCharacter)nextEntity;
                    clone._entities.Add(nextEntity);
                }
                // private MineCartCharacter trackBuilderCharacter;
                else if (entity == trackBuilderCharacter)
                {
                    clone.trackBuilderCharacter = (MineCartCharacter)nextEntity;
                    clone._entities.Add(nextEntity);
                }
                else if (entity is GoalIndicator)
                {
                    // protected GoalIndicator _goalIndicator;
                    clone._goalIndicator = nextEntity as GoalIndicator;
                    clone._entities.Add(nextEntity);
                }
                else
                {
                    clone._entities.Add(nextEntity);
                }
            }

            // protected BaseTrackGenerator _lastGenerator;
            clone._lastGenerator = _lastGenerator?.Clone(clone);
            // protected BaseTrackGenerator _forcedNextGenerator;
            clone._forcedNextGenerator = _forcedNextGenerator?.Clone(clone);

            // private BaseTrackGenerator _trackGenerator;
            clone._trackGenerator = _trackGenerator?.Clone(clone);

            // protected Dictionary<ObstacleTypes, List<Type>> _validObstacles;
            clone._validObstacles = new Dictionary<ObstacleTypes, List<Type>>();
            foreach (KeyValuePair<ObstacleTypes, List<Type>> pair in _validObstacles)
            {
                List<Type> types = new List<Type>();
                foreach (Type type in pair.Value)
                {
                    types.Add(type);
                }
                clone._validObstacles.Add(pair.Key, types);
            }

            // protected List<GeneratorRoll> _generatorRolls;
            clone._generatorRolls = new List<GeneratorRoll>();
            foreach (GeneratorRoll roll in _generatorRolls)
            {
                clone._generatorRolls.Add(roll.Clone(clone));
            }

            return clone;
        }
    }
}
