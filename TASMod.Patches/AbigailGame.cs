// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using HarmonyLib;
// using Microsoft.Xna.Framework;
// using StardewValley.Minigames;

// namespace TASMod.Patches
// {

//     public static class AbigailGameData
//     {
//         // basic neighborlist implementation
//         // every frame I will need to build up the neighborlist
//         // and then I can use the neighborlist to check for collisions
//         public static int NeighborDim = 64;
//         public static Dictionary<Tuple<int, int>, List<AbigailGame.CowboyMonster>> neighborList = new Dictionary<Tuple<int, int>, List<AbigailGame.CowboyMonster>>();

//         public static void BuildNeighborList()
//         {
//             neighborList.Clear();
//             foreach (AbigailGame.CowboyMonster monster in AbigailGame.monsters)
//             {
//                 Tuple<int, int> key = new Tuple<int, int>((int)monster.position.X / NeighborDim, (int)monster.position.Y / NeighborDim);
//                 if (!neighborList.ContainsKey(key))
//                 {
//                     neighborList[key] = new List<AbigailGame.CowboyMonster>();
//                 }
//                 neighborList[key].Add(monster);
//             }
//         }
// /*
//     public static bool isCollidingWithMonster(Rectangle r, CowboyMonster subject)
//     {
//         foreach (CowboyMonster monster in monsters)
//         {
//             if ((subject == null || !subject.Equals(monster)) && Math.Abs(monster.position.X - r.X) < 48 && Math.Abs(monster.position.Y - r.Y) < 48 && r.Intersects(new Rectangle(monster.position.X, monster.position.Y, 48, 48)))
//             {
//                 return true;
//             }
//         }

//         return false;
//     }
//     */
//         public static bool isCollidingWithMonster(Rectangle r, AbigailGame.CowboyMonster subject)
//         {
//             Tuple<int, int> key = new Tuple<int, int>((int)r.X / NeighborDim, (int)r.Y / NeighborDim);
//             for(int i = -1; i <= 1; i++)
//             {
//                 for(int j = -1; j <= 1; j++)
//                 {
//                     Tuple<int, int> neighborKey = new Tuple<int, int>(key.Item1 + i, key.Item2 + j);
//                     if (neighborList.ContainsKey(neighborKey))
//                     {
//                         foreach (AbigailGame.CowboyMonster monster in neighborList[neighborKey])
//                         {
//                             if ((subject == null || !subject.Equals(monster)) && Math.Abs(monster.position.X - r.X) < 48 && Math.Abs(monster.position.Y - r.Y) < 48 && r.Intersects(new Rectangle(monster.position.X, monster.position.Y, 48, 48)))
//                             {
//                                 return true;
//                             }
//                         }
//                     }
//                 }
//             }
//             return false;
//         }
//     }
//     public class AbigailGame_Tick : IPatch
//     {
//         public override string Name => "AbigailGame.Tick";

//         public override void Patch(Harmony harmony)
//         {
//             harmony.Patch(
//                 original: AccessTools.Method(
//                     typeof(AbigailGame),
//                     nameof(AbigailGame.tick)
//                 ),
//                 prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
//             );
//         }

//         public static bool Prefix()
//         {
//             AbigailGameData.BuildNeighborList();
//             return true;
//         }
//     }

//     public class AbigailGame_isCollidingWithMonster : IPatch
//     {
//         public override string Name => "AbigailGame.isCollidingWithMonster";

//         public override void Patch(Harmony harmony)
//         {
//             harmony.Patch(
//                 original: AccessTools.Method(
//                     typeof(AbigailGame),
//                     nameof(AbigailGame.isCollidingWithMonster)
//                 ),
//                 prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
//                 postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
//             );
//         }

//         public static bool Prefix(AbigailGame.CowboyMonster subject)
//         {
//             // skip default implementation if subject is not null (we have a monster to compare)
//             return subject != null;
//         }

//         public static void Postfix(ref bool __result, Rectangle r, AbigailGame.CowboyMonster subject)
//         {
//             if (subject != null)
//                 __result = AbigailGameData.isCollidingWithMonster(r, subject);
//         }
//     }
    
// }