// using System;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;

// namespace TASMod.Minigames
// {
//     public class Entity
//     {
//         public Vector2 position;

//         protected SMineCart _game;

//         public bool visible = true;

//         public bool enabled = true;

//         protected bool _destroyed;

//         public Vector2 drawnPosition => position - new Vector2(_game.screenLeftBound, 0f);

//         public virtual void OnPlayerReset() { }

//         public Entity() { }

//         public Entity(SMineCart game)
//         {
//             _game = game;
//         }

//         public bool IsOnScreen()
//         {
//             if (position.X < _game.screenLeftBound - (float)(_game.tileSize * 4))
//             {
//                 return false;
//             }
//             if (
//                 position.X
//                 > _game.screenLeftBound + (float)_game.screenWidth + (float)(_game.tileSize * 4)
//             )
//             {
//                 return false;
//             }
//             return true;
//         }

//         public bool IsActive()
//         {
//             if (_destroyed)
//             {
//                 return false;
//             }
//             if (!enabled)
//             {
//                 return false;
//             }
//             return true;
//         }

//         public void Initialize(SMineCart game, Random random)
//         {
//             _game = game;
//             _Initialize(random);
//         }

//         public void Destroy()
//         {
//             _destroyed = true;
//         }

//         protected virtual void _Initialize(Random random) { }

//         public virtual bool ShouldReap()
//         {
//             return _destroyed;
//         }

//         public void Draw(SpriteBatch b)
//         {
//             if (!_destroyed && visible && enabled)
//             {
//                 _Draw(b);
//             }
//         }

//         public virtual void _Draw(SpriteBatch b) { }

//         public void Update(float time, Random random, bool shouldPlaySound)
//         {
//             if (!_destroyed && enabled)
//             {
//                 _Update(time, random, shouldPlaySound);
//             }
//         }

//         protected virtual void _Update(float time, Random random, bool shouldPlaySound) { }

//         public virtual Entity Clone(SMineCart game)
//         {
//             Entity entity = new Entity(game);
//             CloneOver(entity);
//             return entity;
//         }

//         public virtual void CloneOver(Entity clone)
//         {
//             clone.position = position;
//             clone.visible = visible;
//             clone.enabled = enabled;
//             clone._destroyed = _destroyed;
//         }
//     }

//     public class MapJunimo : Entity
//     {
//         public enum MoveState
//         {
//             Idle,
//             Moving,
//             Finished
//         }

//         public int direction = 2;

//         public string moveString = "";

//         public float moveSpeed = 60f;

//         public float pixelsToMove;

//         public MoveState moveState;

//         public float nextBump;

//         public float bumpHeight;

//         private bool isOnWater;

//         public MapJunimo(SMineCart game)
//             : base(game) { }

//         public void StartMoving()
//         {
//             moveState = MoveState.Moving;
//         }

//         protected override void _Update(float time, Random random, bool shouldPlaySound)
//         {
//             int desired_direction = direction;
//             isOnWater = false;
//             if (position.X > 194f && position.X < 251f && position.Y > 165f)
//             {
//                 isOnWater = true;
//                 _game.minecartLoop.Pause();
//             }
//             if (moveString.Length > 0)
//             {
//                 if (moveString[0] == 'u')
//                 {
//                     desired_direction = 0;
//                 }
//                 else if (moveString[0] == 'd')
//                 {
//                     desired_direction = 2;
//                 }
//                 else if (moveString[0] == 'l')
//                 {
//                     desired_direction = 3;
//                 }
//                 else if (moveString[0] == 'r')
//                 {
//                     desired_direction = 1;
//                 }
//             }
//             if (moveState == MoveState.Idle && !_game.minecartLoop.IsPaused)
//             {
//                 _game.minecartLoop.Pause();
//             }
//             if (moveState == MoveState.Moving)
//             {
//                 nextBump -= time;
//                 bumpHeight = Utility.MoveTowards(bumpHeight, 0f, time * 5f);
//                 if (nextBump <= 0f)
//                 {
//                     nextBump = Utility.RandomFloat(0.1f, 0.3f, random);
//                     bumpHeight = -2f;
//                 }
//                 if (!isOnWater && _game.minecartLoop.IsPaused)
//                 {
//                     _game.minecartLoop.Resume();
//                 }
//                 if (pixelsToMove <= 0f)
//                 {
//                     if (desired_direction != direction)
//                     {
//                         direction = desired_direction;
//                         if (!isOnWater)
//                         {
//                             SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
//                             _game.createSparkShower(position);
//                         }
//                         else
//                         {
//                             SMineCartGlobal.PlaySound(shouldPlaySound, "waterSlosh");
//                         }
//                     }
//                     if (moveString.Length > 0)
//                     {
//                         pixelsToMove = 16f;
//                         moveString = moveString.Substring(1);
//                     }
//                     else
//                     {
//                         moveState = MoveState.Finished;
//                         direction = 2;
//                         if (position.X < 368f)
//                         {
//                             if (!isOnWater)
//                             {
//                                 SMineCartGlobal.PlaySound(shouldPlaySound, "parry");
//                                 _game.createSparkShower(position);
//                             }
//                             else
//                             {
//                                 SMineCartGlobal.PlaySound(shouldPlaySound, "waterSlosh");
//                             }
//                         }
//                     }
//                 }
//                 if (pixelsToMove > 0f)
//                 {
//                     float pixels_to_move_now = Math.Min(pixelsToMove, moveSpeed * time);
//                     Vector2 direction_to_move = Vector2.Zero;
//                     if (direction == 1)
//                     {
//                         direction_to_move.X = 1f;
//                     }
//                     else if (direction == 3)
//                     {
//                         direction_to_move.X = -1f;
//                     }
//                     if (direction == 0)
//                     {
//                         direction_to_move.Y = -1f;
//                     }
//                     if (direction == 2)
//                     {
//                         direction_to_move.Y = 1f;
//                     }
//                     position += direction_to_move * pixels_to_move_now;
//                     pixelsToMove -= pixels_to_move_now;
//                 }
//             }
//             else
//             {
//                 bumpHeight = -2f;
//             }
//             if (moveState == MoveState.Finished && !_game.minecartLoop.IsPaused)
//             {
//                 _game.minecartLoop.Pause();
//             }
//             base._Update(time, random, shouldPlaySound);
//         }

//         public override void _Draw(SpriteBatch b)
//         {
//             SpriteEffects effect = SpriteEffects.None;
//             Rectangle source_rect = new Rectangle(400, 512, 16, 16);
//             if (direction == 0)
//             {
//                 source_rect.Y = 544;
//             }
//             else if (direction == 2)
//             {
//                 source_rect.Y = 512;
//             }
//             else
//             {
//                 source_rect.Y = 528;
//                 if (direction == 3)
//                 {
//                     effect = SpriteEffects.FlipHorizontally;
//                 }
//             }
//             if (isOnWater)
//             {
//                 source_rect.Height -= 3;
//                 SMineCartGlobal.Draw(
//                     _game.shouldDraw,
//                     b,
//                     _game.texture,
//                     _game.TransformDraw(
//                         base.drawnPosition + new Vector2(0f, -1f) + new Vector2(0f, 1f) * bumpHeight
//                     ),
//                     source_rect,
//                     Color.White,
//                     0f,
//                     new Vector2(8f, 8f),
//                     _game.GetPixelScale(),
//                     effect,
//                     0.45f
//                 );
//                 SMineCartGlobal.Draw(
//                     _game.shouldDraw,
//                     b,
//                     _game.texture,
//                     _game.TransformDraw(
//                         base.drawnPosition + new Vector2(2f, 10f) + new Vector2(0f, 1f) * bumpHeight
//                     ),
//                     new Rectangle(414, 624, 13, 5),
//                     Color.White,
//                     0f,
//                     new Vector2(8f, 8f),
//                     _game.GetPixelScale(),
//                     effect,
//                     0.44f
//                 );
//             }
//             else
//             {
//                 SMineCartGlobal.Draw(
//                     _game.shouldDraw,
//                     b,
//                     _game.texture,
//                     _game.TransformDraw(
//                         base.drawnPosition + new Vector2(0f, -1f) + new Vector2(0f, 1f) * bumpHeight
//                     ),
//                     source_rect,
//                     Color.White,
//                     0f,
//                     new Vector2(8f, 8f),
//                     _game.GetPixelScale(),
//                     effect,
//                     0.45f
//                 );
//             }
//         }

//         public override MapJunimo Clone(SMineCart game)
//         {
//             MapJunimo mapJunimo = new MapJunimo(game);
//             mapJunimo.CloneOver(this);
//             return mapJunimo;
//         }

//         public override void CloneOver(Entity clone)
//         {
//             base.CloneOver(clone);
//             if (clone is MapJunimo mapJunimo)
//             {
//                 mapJunimo.direction = direction;
//                 mapJunimo.moveString = moveString;
//                 mapJunimo.moveSpeed = moveSpeed;
//                 mapJunimo.pixelsToMove = pixelsToMove;
//                 mapJunimo.moveState = moveState;
//                 mapJunimo.nextBump = nextBump;
//                 mapJunimo.bumpHeight = bumpHeight;
//                 mapJunimo.isOnWater = isOnWater;
//             }
//         }
//     }
// }
