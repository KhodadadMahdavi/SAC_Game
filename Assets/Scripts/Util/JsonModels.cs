using System;

namespace TTT.Json
{
    [Serializable]
    public struct StateMessage
    {
        public int[] board;            // length 9
        public int next;               // 1=X, 2=O
        public int winner;             // 0 none, 1 X, 2 O, 3 draw
        public int[] winning_line;     // optional
        public long deadline_tick;     // optional
        public int seat_you;           // 0 or 1 (your seat)
    }

    [Serializable]
    public struct ErrorMessage
    {
        public string code;
        public string message;
    }

    [Serializable]
    public struct ClientMove
    {
        public int index;
    }
}
