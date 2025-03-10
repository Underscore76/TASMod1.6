using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Minigames;
using TASMod.Extensions;

namespace TASMod.Overlays
{
    public class JotPK : IOverlay
    {
        public override string Name => "JotPK";
        public override string Description => "helper overlay for Journey of the Prairie King";
        public static Color EnemyColor = new Color(255, 0, 0, 128);
        public static Color EnemyMovementColor = new Color(255, 255, 0, 196);
        public static Color PlayerColor = new Color(0, 0, 255, 128);
        public static Color PlayerBulletColor = Color.Cyan;
        public static Color ItemColor = new Color(0, 255, 0, 128);
        public static float BulletLineLength = 100;

        public List<string> ImGuiDetails = new();

        public List<Tuple<string, int>> EnemyTypes = new List<Tuple<string, int>>()
        {
            new Tuple<string,int>("orc", 0),
            new Tuple<string,int>("ghost", 1),
            new Tuple<string,int>("ogre", 2),
            new Tuple<string,int>("mummy", 3),
            new Tuple<string,int>("devil", 4),
            new Tuple<string,int>("mushroom", 5),
            new Tuple<string,int>("spikey", 6),
        };

        public Dictionary<int, string> Items = new Dictionary<int, string>()
        {
            { 0, "1 Coin" },
            { 1, "5 Coin" },
            { 2, "Spread" },
            { 3, "Rapid Fire" },
            { 4, "Nuke" },
            { 5, "Zombie" },
            { 6, "Speed" },
            { 7, "Shotgun" },
            { 8, "Life" },
            { 9, "Teleport" },
            { 10, "Sherriff" }
        };


        public string getLootIfKilledNextFrame(int type)
        {
            Random random = Game1.random.Copy();
            // add
            switch (type)
            {
                case 0:
                case 2:
                case 5:
                case 6:
                case 7:
                    random.NextBool();
                    random.NextBool();
                    break;
                case 3:
                    random.NextBool();
                    break;
                case 1:
                case 4:
                    random.NextBool();
                    break;
            }
            if (random.NextDouble() < 0.05)
            {
                if (type != 0 && random.NextDouble() < 0.1)
                {
                    return Items[1];
                }
                if (random.NextDouble() < 0.01)
                {
                    return Items[1];
                }
                return Items[0];
            }
            if (random.NextDouble() < 0.05)
            {
                if (random.NextDouble() < 0.15)
                {
                    return Items[random.Next(6, 8)];
                }
                if (random.NextDouble() < 0.07)
                {
                    return Items[10];
                }
                int loot = random.Next(2, 10);
                if (loot == 5 && random.NextDouble() < 0.4)
                {
                    loot = random.Next(2, 10);
                }
                return Items[loot];
            }
            return "";
        }

        public List<Tuple<Vector2, Vector2>> GetShotLines(AbigailGame game)
        {
            List<Tuple<Vector2, Vector2>> lines = new List<Tuple<Vector2, Vector2>>();
            Point bulletSpawn = new Point((int)game.playerPosition.X + 24, (int)game.playerPosition.Y + 24 - 6);
            // up
            {
                Vector2 start = new Vector2(bulletSpawn.X, bulletSpawn.Y - 22);
                Vector2 dir = new Vector2(0, -1);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // right
            {
                Vector2 start = new Vector2(bulletSpawn.X + 16, bulletSpawn.Y - 6);
                Vector2 dir = new Vector2(1, 0);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // down
            {
                Vector2 start = new Vector2(bulletSpawn.X, bulletSpawn.Y + 10);
                Vector2 dir = new Vector2(0, 1);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // left
            {
                Vector2 start = new Vector2(bulletSpawn.X - 16, bulletSpawn.Y - 6);
                Vector2 dir = new Vector2(-1, 0);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // up right
            {
                Vector2 start = new Vector2(bulletSpawn.X + AbigailGame.TileSize / 2, bulletSpawn.Y - AbigailGame.TileSize / 2);
                Vector2 dir = new Vector2(1, -1);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // up left
            {
                Vector2 start = new Vector2(bulletSpawn.X - AbigailGame.TileSize / 2, bulletSpawn.Y - AbigailGame.TileSize / 2);
                Vector2 dir = new Vector2(-1, -1);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // down right
            {
                Vector2 start = new Vector2(bulletSpawn.X + AbigailGame.TileSize / 2, bulletSpawn.Y + AbigailGame.TileSize / 4);
                Vector2 dir = new Vector2(1, 1);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            // down left
            {
                Vector2 start = new Vector2(bulletSpawn.X - AbigailGame.TileSize / 2, bulletSpawn.Y + AbigailGame.TileSize / 4);
                Vector2 dir = new Vector2(-1, 1);
                Vector2 end = start + dir * BulletLineLength;
                lines.Add(new Tuple<Vector2, Vector2>(start, end));
            }
            return lines;
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (Game1.currentMinigame is null || Game1.currentMinigame is not AbigailGame)
                return;

            var game = (AbigailGame)Game1.currentMinigame;
            foreach (var monster in AbigailGame.monsters)
            {
                if (monster is null) continue;
                if (monster is AbigailGame.Dracula) continue;
                if (monster is AbigailGame.Outlaw) continue;

                var rect = new Rectangle(
                    (int)(AbigailGame.topLeftScreenCoordinate.X + monster.position.X),
                    (int)(AbigailGame.topLeftScreenCoordinate.Y + monster.position.Y),
                    AbigailGame.TileSize,
                    AbigailGame.TileSize
                );
                DrawRectLocal(spriteBatch, rect, EnemyColor);
                var center = AbigailGame.topLeftScreenCoordinate + monster.position.Center.ToVector2();
                var movement = monster.acceleration;
                var next = center + movement;
                DrawLineLocal(spriteBatch, center, next, EnemyMovementColor, 1);
            }
            foreach (var powerup in AbigailGame.powerups)
            {
                if (powerup is null) continue;
                var rect = new Rectangle(
                    (int)(AbigailGame.topLeftScreenCoordinate.X + powerup.position.X),
                    (int)(AbigailGame.topLeftScreenCoordinate.Y + powerup.position.Y),
                    AbigailGame.TileSize,
                    AbigailGame.TileSize
                );
                DrawRectLocal(spriteBatch, rect, ItemColor);
            }
            foreach (var bullet in game.bullets)
            {
                var rect = new Rectangle(bullet.position.X, bullet.position.Y, 12, 12);
                rect.Offset(
                    (int)AbigailGame.topLeftScreenCoordinate.X,
                    (int)AbigailGame.topLeftScreenCoordinate.Y
                );
                DrawRectLocal(spriteBatch, rect, Color.White);
            }
            {
                var rect = game.playerBoundingBox;
                rect.Offset(
                    (int)AbigailGame.topLeftScreenCoordinate.X,
                    (int)AbigailGame.topLeftScreenCoordinate.Y
                );
                DrawRectLocal(spriteBatch, rect, PlayerColor);
            }
            foreach (var line in GetShotLines(game))
            {
                DrawLineLocal(spriteBatch,
                    AbigailGame.topLeftScreenCoordinate + line.Item1,
                    AbigailGame.topLeftScreenCoordinate + line.Item2,
                    PlayerBulletColor, 1);
            }
        }

        public override void RenderImGui()
        {
            if (Game1.currentMinigame is null || Game1.currentMinigame is not AbigailGame)
                return;

            if (ImGui.CollapsingHeader("AbigailGame"))
            {
                var game = (AbigailGame)Game1.currentMinigame;
                ImGui.SeparatorText("Timers");
                ImGui.Text($"Wave Timer: {AbigailGame.waveTimer}");
                ImGui.Text($"Shot Timer: {game.shotTimer - 16}");
                ImGui.SeparatorText("Enemy Drops");
                foreach (var type in EnemyTypes)
                {
                    ImGui.Text($"{type.Item1}: {getLootIfKilledNextFrame(type.Item2)}");
                }
                if (game.spawnQueue.Length < 4)
                    return;
                ImGui.SeparatorText("Spawn Queue");
                ImGui.Text($"Spawn NORTH: {game.spawnQueue[0].Count}");
                ImGui.Text($"Spawn EAST: {game.spawnQueue[1].Count}");
                ImGui.Text($"Spawn SOUTH: {game.spawnQueue[2].Count}");
                ImGui.Text($"Spawn WEST: {game.spawnQueue[3].Count}");
            }
        }
    }
}