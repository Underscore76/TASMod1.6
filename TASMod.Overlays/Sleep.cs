using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TASMod.Helpers;
using Microsoft.Xna.Framework;

namespace TASMod.Overlays
{
    public class Sleep : IOverlay
    {
        public override string Name => "Sleep";
        public int RowHeight = 42;
        public int MaxRowWidth;
        public Color BackgroundColor = new Color(0, 0, 0, 220);
        public string MaxString = "ABCDEFGHI";
        public float fontScale = 1.5f;
        public override string Description => "overlay current overnight updates";

        private Dictionary<string, Rectangle> luckRects;
        private Dictionary<string, Rectangle> weatherRects;

        private string last_LocationName;
        private int last_Day;
        private uint last_Steps;
        private NightInfo.Tomorrow TomorrowInfo;

        public Sleep()
        {
            MaxRowWidth = (int)(42f * (RowHeight / 28f) + MeasureString(MaxString, fontScale).X);

            // TODO: This is copy-over, can you programmatically get these rectangles?
            luckRects = new Dictionary<string, Rectangle>
            {
                { "background", new Rectangle(624, 305, 42, 28) },
                { "stardrop", new Rectangle(644, 333, 13, 13) },
                { "pyramid", new Rectangle(592, 333, 13, 13) },
                { "neutral", new Rectangle(540, 333, 13, 13) },
                { "bat", new Rectangle(540, 346, 13, 13) },
                { "skull", new Rectangle(592, 346, 13, 13) }
            };

            weatherRects = new Dictionary<string, Rectangle>
            {
                { "background", new Rectangle(413, 305, 42, 28) },
                { getWeatherText(Game1.weather_sunny), new Rectangle(413, 333, 13, 13) },
                { getWeatherText(Game1.weather_wedding), new Rectangle(413, 333, 13, 13) },
                { getWeatherText(Game1.weather_snow), new Rectangle(465, 346, 13, 13) },
                { getWeatherText(Game1.weather_rain), new Rectangle(465, 333, 13, 13) },
                { getWeatherText(Game1.weather_debris), new Rectangle(465, 359, 13, 13) },
                { getWeatherText(Game1.weather_lightning), new Rectangle(413, 346, 13, 13) },
                { getWeatherText(Game1.weather_festival), new Rectangle(413, 372, 13, 13) }
            };
        }

        public override void ActiveUpdate()
        {
            var currentLocation = InstanceCurrentLocation.Get(0);
            var player = InstanceCurrentPlayer.Get(0);
            if (!currentLocation.Active || !player.Active) return;
            if (currentLocation.Name != last_LocationName || Game1.dayOfMonth != last_Day || player.Player.stats.StepsTaken != last_Steps)
            {
                last_LocationName = currentLocation.Name;
                last_Day = Game1.dayOfMonth;
                last_Steps = player.Player.stats.StepsTaken;
                TomorrowInfo = NightInfo.GetTomorrow(0);
            }
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            var CurrentLocation = InstanceCurrentLocation.Get(0);
            if (CurrentLocation.Active)
            {
                Vector2 tilePosition = Vector2.Zero;
                if (Game1.currentLocation is FarmHouse farmHouse)
                {
                    tilePosition = Utility.PointToVector2(farmHouse.getBedSpot()) + new Vector2(2, -4);
                    tilePosition = Game1.GlobalToLocal(Game1.viewport, tilePosition * Game1.tileSize);
                }
                Vector2 origin = tilePosition;
                {
                    if (DrawLuckRow(spriteBatch, origin, TomorrowInfo.dailyLuck))
                        origin.Y += RowHeight;
                }
                {
                    if (DrawWeatherRow(spriteBatch, origin, Game1.weatherForTomorrow))
                        origin.Y += RowHeight;
                }
                {
                    if (Int32.TryParse(TomorrowInfo.dishOfTheDay, out int dishOfTheDay))
                    {
                        if (DrawDishOfTheDayRow(spriteBatch, origin, dishOfTheDay))
                            origin.Y += RowHeight;
                    }
                }
                {
                    if (DrawFriendGiftRow(spriteBatch, origin, TomorrowInfo.receiveGift, TomorrowInfo.friend))
                        origin.Y += RowHeight;
                }
            }
        }

        public void DrawIconTextRow(SpriteBatch spriteBatch, Texture2D texture, Vector2 origin, Rectangle bgRect, Vector2 offset, Rectangle? iconRect, string text, int height, int rowWidth = -1)
        {
            // compute scaling
            float scale = (float)height / bgRect.Height;
            int width = (int)(bgRect.Width * scale);

            // draw ROW background
            if (rowWidth == -1)
                rowWidth = MaxRowWidth;
            Rectangle destRect = new Rectangle((int)origin.X, (int)origin.Y, rowWidth, height);
            DrawRectLocal(0, spriteBatch, destRect, BackgroundColor);

            // draw the background sprite
            Rectangle destBGRect = new Rectangle((int)origin.X, (int)origin.Y, width, height);
            DrawRectFromTexture(0, spriteBatch, texture, bgRect, destBGRect, Color.White);

            // add customization icon
            if (iconRect != null)
            {
                Rectangle destIconRect = new Rectangle((int)(origin.X + offset.X * scale), (int)(origin.Y + offset.Y * scale), (int)(iconRect?.Width * scale), (int)(iconRect?.Height * scale));
                DrawRectFromTexture(0, spriteBatch, texture, (Rectangle)iconRect, destIconRect, Color.White);
            }
            // draw the text
            if (text != null && text != "")
            {
                // keep text inside of row
                Vector2 textSize = MeasureString(text, fontScale);
                while (textSize.X + width > rowWidth)
                {
                    text = text.Remove(text.Length - 1);
                    textSize = MeasureString(text, fontScale);
                }
                Rectangle textRect = new Rectangle((int)origin.X + width, (int)origin.Y, rowWidth - width, height);
                // TODO: This is for some reason leading to some clipped text??
                DrawCenteredTextInRectLocal(0, spriteBatch, textRect, text, Color.White, fontScale);
            }
        }

        public bool DrawLuckRow(SpriteBatch spriteBatch, Vector2 origin, double luck)
        {
            // custom pad the text
            string text = (luck > 0 ? " " : "") + luck.ToString("F3") + " ";
            Vector2 offset = new Vector2(15, 6);
            DrawIconTextRow(spriteBatch, Game1.mouseCursors, origin, luckRects["background"], offset, luckRects[getLuckString(luck)], text, RowHeight);
            return true;
        }
        public bool DrawWeatherRow(SpriteBatch spriteBatch, Vector2 origin, string weather)
        {
            // custom pad the textq
            string text = getWeatherText(weather);
            Vector2 offset = new Vector2(3, 3);
            DrawIconTextRow(spriteBatch, Game1.mouseCursors, origin, weatherRects["background"], offset, weatherRects[text], text, RowHeight);
            return true;
        }
        public bool DrawDishOfTheDayRow(SpriteBatch spriteBatch, Vector2 origin, int dishOfTheDay)
        {
            Rectangle sourceRect = GameLocation.getSourceRectForObject(dishOfTheDay);
            DrawIconTextRow(spriteBatch, Game1.objectSpriteSheet, origin, sourceRect, Vector2.Zero, null, DropInfo.ObjectName(dishOfTheDay.ToString()), RowHeight);
            return true;
        }

        public bool DrawFriendGiftRow(SpriteBatch spriteBatch, Vector2 origin, bool receiveGift, string whichFriend)
        {
            if (receiveGift)
            {
                NPC friend = Game1.getCharacterFromName(whichFriend);
                Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(friend.Portrait, 0);
                DrawIconTextRow(spriteBatch, friend.Portrait, origin, sourceRect, Vector2.Zero, null, null, RowHeight);
            }
            return receiveGift;
        }

        private string getLuckString(double luck)
        {
            if (luck < -0.07) return "skull";
            if (luck < -0.02) return "bat";
            if (luck > 0.07) return "stardrop";
            if (luck > 0.02) return "pyramid";
            return "neutral";
        }
        private string getWeatherText(string weather)
        {
            switch (weather)
            {
                case Game1.weather_sunny:
                    return "Sunny  ";
                case Game1.weather_rain:
                    return "Rainy  ";
                case Game1.weather_debris:
                    return "Debris ";
                case Game1.weather_lightning:
                    return "T-Storm";
                case Game1.weather_festival:
                    return "Festive";
                case Game1.weather_snow:
                    return "Snow   ";
                case Game1.weather_wedding:
                    return "Wedding";
            }
            return "Unknown";
        }
    }
}