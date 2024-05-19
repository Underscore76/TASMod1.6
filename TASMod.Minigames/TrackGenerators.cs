// using System;

// namespace TASMod.Minigames
// {
//     public class BaseTrackGenerator
//     {
//         public const int OBSTACLE_NONE = -10;

//         public const int OBSTACLE_MIDDLE = -10;

//         public const int OBSTACLE_FRONT = -11;

//         public const int OBSTACLE_BACK = -12;

//         public const int OBSTACLE_RANDOM = -13;

//         protected List<Track> _generatedTracks;

//         protected SMineCart _game;

//         protected Dictionary<int, KeyValuePair<ObstacleTypes, float>> _obstacleIndices =
//             new Dictionary<int, KeyValuePair<ObstacleTypes, float>>();

//         protected Func<Track, BaseTrackGenerator, bool> _pickupFunction;

//         public static bool FlatsOnly(Track track, BaseTrackGenerator generator)
//         {
//             return track.trackType == Track.TrackType.None;
//         }

//         public static bool UpSlopesOnly(Track track, BaseTrackGenerator generator)
//         {
//             return track.trackType == Track.TrackType.UpSlope;
//         }

//         public static bool DownSlopesOnly(Track track, BaseTrackGenerator generator)
//         {
//             return track.trackType == Track.TrackType.DownSlope;
//         }

//         public static bool IceDownSlopesOnly(Track track, BaseTrackGenerator generator)
//         {
//             return track.trackType == Track.TrackType.IceDownSlope;
//         }

//         public static bool Always(Track track, BaseTrackGenerator generator)
//         {
//             return true;
//         }

//         public static bool EveryOtherTile(Track track, BaseTrackGenerator generator)
//         {
//             if ((int)(track.position.X / 16f) % 2 == 0)
//             {
//                 return true;
//             }
//             return false;
//         }

//         public T AddObstacle<T>(
//             ObstacleTypes obstacle_type,
//             int position,
//             float obstacle_chance = 1f
//         )
//             where T : BaseTrackGenerator
//         {
//             _obstacleIndices.Add(
//                 position,
//                 new KeyValuePair<ObstacleTypes, float>(obstacle_type, obstacle_chance)
//             );
//             return this as T;
//         }

//         public T AddPickupFunction<T>(Func<Track, BaseTrackGenerator, bool> pickup_spawn_function)
//             where T : BaseTrackGenerator
//         {
//             _pickupFunction =
//                 (Func<Track, BaseTrackGenerator, bool>)
//                     Delegate.Combine(_pickupFunction, pickup_spawn_function);
//             return this as T;
//         }

//         public BaseTrackGenerator(SMineCart game)
//         {
//             _game = game;
//         }

//         public Track AddTrack(int x, int y, Track.TrackType track_type = Track.TrackType.Straight)
//         {
//             Track track = _game.AddTrack(x, y, track_type);
//             _generatedTracks.Add(track);
//             return track;
//         }

//         public Track AddTrack(Track track)
//         {
//             _game.AddTrack(track);
//             _generatedTracks.Add(track);
//             return track;
//         }

//         public Track AddPickupTrack(
//             int x,
//             int y,
//             Track.TrackType track_type = Track.TrackType.Straight
//         )
//         {
//             Track track = AddTrack(x, y, track_type);
//             if (_pickupFunction == null)
//             {
//                 return track;
//             }
//             Delegate[] invocationList = _pickupFunction.GetInvocationList();
//             for (int i = 0; i < invocationList.Length; i++)
//             {
//                 if (!((Func<Track, BaseTrackGenerator, bool>)invocationList[i])(track, this))
//                 {
//                     return track;
//                 }
//             }
//             Pickup pickup = _game.CreatePickup(track.position + new Vector2(8f, -_game.tileSize));
//             if (
//                 pickup != null
//                 && (
//                     track.trackType == Track.TrackType.DownSlope
//                     || track.trackType == Track.TrackType.UpSlope
//                     || track.trackType == Track.TrackType.IceDownSlope
//                     || track.trackType == Track.TrackType.SlimeUpSlope
//                 )
//             )
//             {
//                 pickup.position += new Vector2(0f, (float)(-_game.tileSize) * 0.75f);
//             }
//             return track;
//         }

//         public virtual void Initialize(Random random)
//         {
//             _generatedTracks = new List<Track>();
//         }

//         public void GenerateTrack(Random random, bool shouldPlaySound)
//         {
//             _GenerateTrack(random, shouldPlaySound);
//             PopulateObstacles(random, shouldPlaySound);
//         }

//         public void PopulateObstacles(Random random, bool shouldPlaySound)
//         {
//             if (_game.generatorPosition.X >= _game.distanceToTravel || _generatedTracks.Count == 0)
//             {
//                 return;
//             }
//             _generatedTracks.OrderBy((Track o) => o.position.X);
//             if (_obstacleIndices == null || _obstacleIndices.Count == 0)
//             {
//                 return;
//             }
//             foreach (int index in _obstacleIndices.Keys)
//             {
//                 if (random.NextBool(_obstacleIndices[index].Value))
//                 {
//                     int track_index = index switch
//                     {
//                         -12 => _generatedTracks.Count - 1,
//                         -11 => 0,
//                         -10 => (_generatedTracks.Count - 1) / 2,
//                         -13 => random.Next(_generatedTracks.Count),
//                         _ => index,
//                     };
//                     Track track = _generatedTracks[track_index];
//                     if (
//                         track != null
//                         && (int)(track.position.X / (float)_game.tileSize) < _game.distanceToTravel
//                     )
//                     {
//                         _game.AddObstacle(track, _obstacleIndices[index].Key);
//                     }
//                 }
//             }
//         }

//         protected virtual void _GenerateTrack(Random random, bool shouldPlaySound)
//         {
//             _game.generatorPosition.X++;
//         }

//         public virtual BaseTrackGenerator Clone(SMineCart game)
//         {
//             BaseTrackGenerator baseTrackGenerator = new BaseTrackGenerator(game);
//             CloneOver(baseTrackGenerator);
//             return baseTrackGenerator;
//         }

//         public virtual void CloneOver(BaseTrackGenerator clone)
//         {
//             // Controller.Console.Alert("\tCloning a BaseTrackGenerator CloneOver");
//             if (_generatedTracks != null)
//             {
//                 clone._generatedTracks = new List<Track>();
//                 foreach (Track generatedTrack in _generatedTracks)
//                 {
//                     clone._generatedTracks.Add(generatedTrack.Clone(clone._game));
//                 }
//             }
//             if (_obstacleIndices != null)
//             {
//                 clone._obstacleIndices = new Dictionary<int, KeyValuePair<ObstacleTypes, float>>(
//                     _obstacleIndices
//                 );
//             }
//             clone._pickupFunction = _pickupFunction;
//         }
//     }

//     public class GeneratorRoll
//     {
//         public float chance;

//         public BaseTrackGenerator generator;

//         public Func<bool> additionalGenerationCondition;

//         public BaseTrackGenerator forcedNextGenerator;

//         public GeneratorRoll(
//             float generator_chance,
//             BaseTrackGenerator track_generator,
//             Func<bool> additional_generation_condition = null,
//             BaseTrackGenerator forced_next_generator = null
//         )
//         {
//             chance = generator_chance;
//             generator = track_generator;
//             forcedNextGenerator = forced_next_generator;
//             additionalGenerationCondition = additional_generation_condition;
//         }

//         public GeneratorRoll Clone(SMineCart game)
//         {
//             // Controller.Console.Alert("Cloning a generator roll");
//             return new GeneratorRoll(
//                 chance,
//                 generator?.Clone(game),
//                 additionalGenerationCondition,
//                 forcedNextGenerator?.Clone(game)
//             );
//         }
//     }

//     public class StraightAwayGenerator : BaseTrackGenerator
//     {
//         public int straightAwayLength = 10;

//         public List<int> staggerPattern;

//         public int minLength = 3;

//         public int maxLength = 5;

//         public float staggerChance = 0.25f;

//         public int minimuimDistanceBetweenStaggers = 1;

//         public int currentStaggerDistance;

//         public bool generateCheckpoint = true;

//         protected bool _generatedCheckpoint = true;

//         public StraightAwayGenerator SetMinimumDistanceBetweenStaggers(int min)
//         {
//             minimuimDistanceBetweenStaggers = min;
//             return this;
//         }

//         public StraightAwayGenerator SetLength(int min, int max)
//         {
//             minLength = min;
//             maxLength = max;
//             return this;
//         }

//         public StraightAwayGenerator SetCheckpoint(bool checkpoint)
//         {
//             generateCheckpoint = checkpoint;
//             return this;
//         }

//         public StraightAwayGenerator SetStaggerChance(float chance)
//         {
//             staggerChance = chance;
//             return this;
//         }

//         public StraightAwayGenerator SetStaggerValues(params int[] args)
//         {
//             staggerPattern = new List<int>();
//             for (int i = 0; i < args.Length; i++)
//             {
//                 staggerPattern.Add(args[i]);
//             }
//             return this;
//         }

//         public StraightAwayGenerator SetStaggerValueRange(int min, int max)
//         {
//             staggerPattern = new List<int>();
//             for (int i = min; i <= max; i++)
//             {
//                 staggerPattern.Add(i);
//             }
//             return this;
//         }

//         public StraightAwayGenerator(SMineCart game)
//             : base(game) { }

//         public override void Initialize(Random random)
//         {
//             straightAwayLength = random.Next(minLength, maxLength + 1);
//             _generatedCheckpoint = false;
//             if (straightAwayLength <= 3)
//             {
//                 _generatedCheckpoint = true;
//             }
//             base.Initialize(random);
//         }

//         protected override void _GenerateTrack(Random random, bool shouldPlaySound)
//         {
//             if (_game.generatorPosition.X >= _game.distanceToTravel)
//             {
//                 return;
//             }
//             for (int i = 0; i < straightAwayLength; i++)
//             {
//                 if (_game.generatorPosition.X >= _game.distanceToTravel)
//                 {
//                     return;
//                 }
//                 int last_y = _game.generatorPosition.Y;
//                 if (currentStaggerDistance <= 0)
//                 {
//                     if (random.NextDouble() < (double)staggerChance)
//                     {
//                         _game.generatorPosition.Y += random.ChooseFrom(staggerPattern);
//                     }
//                     currentStaggerDistance = minimuimDistanceBetweenStaggers;
//                 }
//                 else
//                 {
//                     currentStaggerDistance--;
//                 }
//                 if (!_game.IsTileInBounds(_game.generatorPosition.Y))
//                 {
//                     _game.generatorPosition.Y = last_y;
//                     straightAwayLength = 0;
//                     break;
//                 }
//                 _game.generatorPosition.Y = _game.KeepTileInBounds(_game.generatorPosition.Y);
//                 Track.TrackType tile_type = Track.TrackType.Straight;
//                 if (_game.generatorPosition.Y < last_y)
//                 {
//                     tile_type = Track.TrackType.UpSlope;
//                 }
//                 else if (_game.generatorPosition.Y > last_y)
//                 {
//                     tile_type = Track.TrackType.DownSlope;
//                 }
//                 if (tile_type == Track.TrackType.DownSlope && _game.currentTheme == 1)
//                 {
//                     tile_type = Track.TrackType.IceDownSlope;
//                 }
//                 if (tile_type == Track.TrackType.UpSlope && _game.currentTheme == 5)
//                 {
//                     tile_type = Track.TrackType.SlimeUpSlope;
//                 }
//                 AddPickupTrack(_game.generatorPosition.X, _game.generatorPosition.Y, tile_type);
//                 _game.generatorPosition.X++;
//             }
//             if (
//                 _generatedTracks != null
//                 && _generatedTracks.Count > 0
//                 && generateCheckpoint
//                 && !_generatedCheckpoint
//             )
//             {
//                 _generatedCheckpoint = true;
//                 _generatedTracks.OrderBy((Track o) => o.position.X);
//                 _game.AddCheckpoint((int)(_generatedTracks[0].position.X / (float)_game.tileSize));
//             }
//         }

//         public override StraightAwayGenerator Clone(SMineCart game)
//         {
//             StraightAwayGenerator clone = new StraightAwayGenerator(game);
//             CloneOver(clone);
//             return clone;
//         }

//         public override void CloneOver(BaseTrackGenerator clone)
//         {
//             base.CloneOver(clone);
//             if (clone is StraightAwayGenerator straightAwayGenerator)
//             {
//                 straightAwayGenerator.straightAwayLength = straightAwayLength;
//                 straightAwayGenerator.staggerPattern = new List<int>(staggerPattern);
//                 straightAwayGenerator.minLength = minLength;
//                 straightAwayGenerator.maxLength = maxLength;
//                 straightAwayGenerator.staggerChance = staggerChance;
//                 straightAwayGenerator.minimuimDistanceBetweenStaggers =
//                     minimuimDistanceBetweenStaggers;
//                 straightAwayGenerator.currentStaggerDistance = currentStaggerDistance;
//                 straightAwayGenerator.generateCheckpoint = generateCheckpoint;
//                 straightAwayGenerator._generatedCheckpoint = _generatedCheckpoint;
//             }
//         }
//     }

//     public class SmallGapGenerator : BaseTrackGenerator
//     {
//         public int minLength = 3;

//         public int maxLength = 5;

//         public int minDepth = 5;

//         public int maxDepth = 5;

//         public SmallGapGenerator SetLength(int min, int max)
//         {
//             minLength = min;
//             maxLength = max;
//             return this;
//         }

//         public SmallGapGenerator SetDepth(int min, int max)
//         {
//             minDepth = min;
//             maxDepth = max;
//             return this;
//         }

//         public SmallGapGenerator(SMineCart game)
//             : base(game) { }

//         protected override void _GenerateTrack(Random random, bool shouldPlaySound)
//         {
//             if (_game.generatorPosition.X >= _game.distanceToTravel)
//             {
//                 return;
//             }
//             int depth = random.Next(minDepth, maxDepth + 1);
//             int length = random.Next(minLength, maxLength + 1);
//             AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
//             _game.generatorPosition.X++;
//             _game.generatorPosition.Y += depth;
//             for (int i = 0; i < length; i++)
//             {
//                 if (_game.generatorPosition.X >= _game.distanceToTravel)
//                 {
//                     _game.generatorPosition.Y -= depth;
//                     return;
//                 }
//                 AddPickupTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
//                 _game.generatorPosition.X++;
//             }
//             _game.generatorPosition.Y -= depth;
//             if (_game.generatorPosition.X < _game.distanceToTravel)
//             {
//                 AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
//                 _game.generatorPosition.X++;
//             }
//         }

//         public override SmallGapGenerator Clone(SMineCart game)
//         {
//             SmallGapGenerator clone = new SmallGapGenerator(game);
//             CloneOver(clone);
//             return clone;
//         }

//         public override void CloneOver(BaseTrackGenerator clone)
//         {
//             base.CloneOver(clone);
//             if (clone is SmallGapGenerator generator)
//             {
//                 generator.minLength = minLength;
//                 generator.maxLength = maxLength;
//                 generator.minDepth = minDepth;
//                 generator.maxDepth = maxDepth;
//             }
//         }
//     }

//     public class RapidHopsGenerator : BaseTrackGenerator
//     {
//         public int minLength = 3;

//         public int maxLength = 5;

//         private int startY;

//         public int yStep;

//         public bool chaotic;

//         public RapidHopsGenerator SetLength(int min, int max)
//         {
//             minLength = min;
//             maxLength = max;
//             return this;
//         }

//         public RapidHopsGenerator SetYStep(int yStep)
//         {
//             this.yStep = yStep;
//             return this;
//         }

//         public RapidHopsGenerator SetChaotic(bool chaotic)
//         {
//             this.chaotic = chaotic;
//             return this;
//         }

//         public RapidHopsGenerator(SMineCart game)
//             : base(game) { }

//         protected override void _GenerateTrack(Random random, bool shouldPlaySound)
//         {
//             if (_game.generatorPosition.X >= _game.distanceToTravel)
//             {
//                 return;
//             }
//             if (startY == 0)
//             {
//                 startY = _game.generatorPosition.Y;
//             }
//             int length = random.Next(minLength, maxLength + 1);
//             AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
//             _game.generatorPosition.X++;
//             _game.generatorPosition.Y += yStep;
//             for (int i = 0; i < length; i++)
//             {
//                 if (
//                     _game.generatorPosition.Y < 3
//                     || _game.generatorPosition.Y > _game.screenHeight / _game.tileSize - 2
//                 )
//                 {
//                     _game.generatorPosition.Y = _game.screenHeight / _game.tileSize - 2;
//                     startY = _game.generatorPosition.Y;
//                 }
//                 if (_game.generatorPosition.X >= _game.distanceToTravel)
//                 {
//                     _game.generatorPosition.Y -= yStep;
//                     return;
//                 }
//                 AddPickupTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
//                 _game.generatorPosition.X += random.Next(2, 4);
//                 if (random.NextDouble() < 0.33)
//                 {
//                     AddTrack(
//                         _game.generatorPosition.X - 1,
//                         Math.Min(
//                             _game.screenHeight / _game.tileSize - 2,
//                             _game.generatorPosition.Y + random.Next(5)
//                         )
//                     );
//                 }
//                 if (chaotic)
//                 {
//                     _game.generatorPosition.Y =
//                         startY + random.Next(-Math.Abs(yStep), Math.Abs(yStep) + 1);
//                 }
//                 else
//                 {
//                     _game.generatorPosition.Y += yStep;
//                 }
//             }
//             if (_game.generatorPosition.X < _game.distanceToTravel)
//             {
//                 _game.generatorPosition.Y -= yStep;
//                 AddTrack(_game.generatorPosition.X, _game.generatorPosition.Y);
//                 _game.generatorPosition.X++;
//             }
//         }

//         public override RapidHopsGenerator Clone(SMineCart game)
//         {
//             RapidHopsGenerator clone = new RapidHopsGenerator(game);
//             CloneOver(clone);
//             return clone;
//         }

//         public override void CloneOver(BaseTrackGenerator clone)
//         {
//             base.CloneOver(clone);
//             if (clone is RapidHopsGenerator generator)
//             {
//                 generator.minLength = minLength;
//                 generator.maxLength = maxLength;
//                 generator.startY = startY;
//                 generator.yStep = yStep;
//                 generator.chaotic = chaotic;
//             }
//         }
//     }
// }
