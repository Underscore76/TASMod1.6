using StardewValley;
namespace TASMod.Helpers
{
    public class EventInfo
    {
        public Event Event;
        public bool Active => Event != null;
        public bool Skippable => Event?.skippable ?? false;
        public string CurrentCommand
        {
            get
            {
                if (Event.currentCommand < Event.eventCommands.Length)
                    return Event.eventCommands[Event.CurrentCommand];
                return "";
            }
        }
        public string LastCommand
        {
            get
            {
                if (Event.CurrentCommand > 0)
                    return Event.eventCommands[Event.CurrentCommand - 1];
                return "";
            }
        }
    }

    public class InstanceCurrentEvent
    {
        public static EventInfo Get(int index)
        {
            if (GameRunner.instance.gameInstances.Count == 1)
            {
                return new EventInfo { Event = Game1.CurrentEvent };
            }
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new EventInfo { Event = null };
            var location = InstanceCurrentLocation.Get(index).Location;
            var evt = location?.currentEvent;
            if (evt != null)
                return new EventInfo { Event = evt };
            return new EventInfo { Event = null };
        }
    }
}