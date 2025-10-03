using System.Collections.Generic;
using TASMod.Inputs;

namespace TASMod.Recording
{
    public static class GamePadInputQueue
    {
        public static Queue<TASGamePadState>[] Queues = new Queue<TASGamePadState>[4]
        {
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>()
        };

        public static void Clear()
        {
            for (int i = 0; i < 4; i++)
            {
                Queues[i].Clear();
            }
        }

        public static void PushGamePadInput(
            int index,
            TASGamePadState state
        )
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            Queues[index].Enqueue(state);
        }

        public static bool HasAnyInput()
        {
            for (int i = 0; i < 4; i++)
            {
                if (Queues[i].Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasInput(int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            return Queues[index].Count > 0;
        }

        public static TASGamePadState GetNextInput(
            int index
        )
        {
            if (index < 0 || index >= 4 || Queues[index].Count == 0)
            {
                return TASInputState.gState[index];
            }
            return Queues[index].Dequeue();
        }

        public static TASGamePadState[] GetNextInputs()
        {
            TASGamePadState[] states = new TASGamePadState[4];
            for (int i = 0; i < 4; i++)
            {
                states[i] = GetNextInput(i);
            }
            return states;
        }
    }
}