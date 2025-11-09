using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;

namespace TASMod.Helpers
{
    public class MenuInfo
    {
        public IClickableMenu Menu;
        public bool Active => Menu != null;
        public IClickableMenu SubMenu
        {
            get
            {
                if (Menu is TitleMenu)
                    return TitleMenu.subMenu;
                return null;
            }
        }
        public bool IsSaveGame => Menu is SaveGameMenu;
        public bool CanQuit => (Menu as SaveGameMenu).quit;
        public bool IsDialogue => Menu is DialogueBox;
        public bool Transitioning => (Menu as DialogueBox).transitioning;
        public bool IsQuestion => (Menu as DialogueBox).isQuestion;
        public int CharacterIndexInDialogue => (Menu as DialogueBox).characterIndexInDialogue;
        public int SafetyTimer => (Menu as DialogueBox).safetyTimer;
        public int SelectedResponseIndex => (Menu as DialogueBox).selectedResponse;
        public string SelectedResponse
        {
            get
            {
                if (
                    (Game1.activeClickableMenu as DialogueBox).responses != null
                    && (Game1.activeClickableMenu as DialogueBox).responses.Length > 0
                )
                {
                    return (Game1.activeClickableMenu as DialogueBox)
                        .responses[SelectedResponseIndex]
                        .responseText;
                }
                return null;
            }
        }
        public string CurrentString => (Menu as DialogueBox).getCurrentString();
    }

    public class InstanceCurrentMenu
    {
        public static MenuInfo Get(int index)
        {
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new MenuInfo { Menu = null };
            var menu = Reflector.GetStaticVar(index, "Game1__activeClickableMenu") as IClickableMenu;
            return new MenuInfo { Menu = menu };
        }
    }
}
