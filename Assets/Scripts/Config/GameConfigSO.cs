using UnityEngine;

namespace TTT
{
    /// <summary>
    /// Central config for client defaults, gameplay timing, and Addressables keys.
    /// Keep this LOCAL (not remote) so the app always boots with sane defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "TTT/Game Config", fileName = "GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Nakama Client Defaults")]
        [Tooltip("http or https for the REST client.")]
        public string DefaultScheme = "http";
        [Tooltip("Default host to prefill in the Connect panel.")]
        public string DefaultHost = "127.0.0.1";
        [Tooltip("Default HTTP port (7350 by default for Nakama).")]
        public int DefaultPort = 7350;
        [Tooltip("Default Server Key used by Nakama clients (usually 'defaultkey').")]
        public string ServerKey = "defaultkey";
        [Tooltip("Use secure WebSocket (wss) if true when connecting the socket.")]
        public bool UseSSLForSocket = false;

        [Header("Matchmaking")]
        [Tooltip("Nakama matchmaker search query; leave empty if not used.")]
        public string MatchmakingQuery = "";
        [Tooltip("Min players to find.")]
        public int MinCount = 2;
        [Tooltip("Max players to find.")]
        public int MaxCount = 2;

        [Header("Gameplay (Mirror server values)")]
        [Tooltip("Per-turn timeout (seconds). Keep in sync with server.")]
        public int TurnTimeoutSeconds = 10;
        [Tooltip("Server tick rate used to interpret deadline_tick (ticks per second).")]
        public int TickRate = 5;

        [Header("Match Type / Opcodes (for clarity)")]
        [Tooltip("Match handler name registered on the server.")]
        public string MatchName = "tictactoe";
        [Tooltip("Opcodes used by the server. Keep in sync with Go code.")]
        public int OpState = 1;     // server -> clients
        public int OpMove = 2;     // client -> server
        public int OpError = 3;     // server -> client
        public int OpGameOver = 4;  // server -> clients

        [Header("Addressables")]
        [Tooltip("Key or label for the remote Tic-Tac-Toe board prefab.")]
        public string GameBoardKey = "GameBoard";
        [Tooltip("Optional: label used for gameplay content group (if you use labels).")]
        public string GameplayLabel = "gameplay";

        [Header("Rejoin Behavior")]
        [Tooltip("If enabled, the client attempts to rejoin the last match on reconnect/start.")]
        public bool RejoinEnabled = true;
        [Tooltip("Seconds to keep trying to rejoin before giving up.")]
        public float RejoinTimeoutSeconds = 6f;

        /// <summary> Helper: compute seconds remaining from server deadline_tick and a current tick. </summary>
        public float SecondsRemainingFromDeadline(long deadlineTick, long currentTick)
        {
            var ticksLeft = Mathf.Max(0, (int)(deadlineTick - currentTick));
            return ticksLeft / Mathf.Max(1f, TickRate);
        }
    }
}
