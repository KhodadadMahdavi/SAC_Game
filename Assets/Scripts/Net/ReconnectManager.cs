using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TTT.Net
{
    /// <summary>
    /// Attempts to reconnect the socket and rejoin the last known match.
    /// </summary>
    public class ReconnectManager
    {
        private readonly NakamaConnection _conn;
        private readonly NakamaMatchClient _matchClient;
        private readonly TTT.GameConfigSO _config;

        public event Action<bool> OnRejoinResult; // true if rejoined

        public ReconnectManager(NakamaConnection conn, NakamaMatchClient matchClient, TTT.GameConfigSO config)
        {
            _conn = conn;
            _matchClient = matchClient;
            _config = config;
        }

        public async Task TryRejoinAsync(CancellationToken ct = default)
        {
            if (!_config.RejoinEnabled) { OnRejoinResult?.Invoke(false); return; }

            var matchId = PlayerPrefs.GetString("last_match_id", "");
            if (string.IsNullOrEmpty(matchId))
            {
                OnRejoinResult?.Invoke(false);
                return;
            }

            var tEnd = Time.realtimeSinceStartup + Mathf.Max(1f, _config.RejoinTimeoutSeconds);
            while (Time.realtimeSinceStartup < tEnd && !ct.IsCancellationRequested)
            {
                if (_conn.Socket == null || !_conn.Socket.IsConnected)
                {
                    var ok = await _conn.ReconnectSocketAsync();
                    if (!ok)
                    {
                        await Task.Delay(300);
                        continue;
                    }
                }

                var joined = await _matchClient.JoinByIdAsync(matchId);
                if (joined)
                {
                    OnRejoinResult?.Invoke(true);
                    return;
                }

                await Task.Delay(300);
            }
            OnRejoinResult?.Invoke(false);
        }
    }
}
