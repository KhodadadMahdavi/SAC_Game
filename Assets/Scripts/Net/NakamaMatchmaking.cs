using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace TTT.Net
{
    /// <summary>
    /// Wraps Add/RemoveMatchmaker and exposes Matched event.
    /// </summary>
    public class NakamaMatchmaking
    {
        public bool IsSearching { get; private set; }
        public IMatchmakerTicket Ticket { get; private set; }

        private readonly NakamaConnection _conn;
        private readonly TTT.GameConfigSO _config;

        public event Action OnSearching;
        public event Action OnCancelled;
        public event Action<IMatchmakerMatched> OnMatched;
        public event Action<string> OnError;

        public NakamaMatchmaking(NakamaConnection conn, TTT.GameConfigSO config)
        {
            _conn = conn;
            _config = config;
        }

        public async Task<bool> StartAsync()
        {
            if (_conn.Socket == null || !_conn.Socket.IsConnected)
            {
                OnError?.Invoke("Socket is not connected.");
                return false;
            }
            if (IsSearching) return true;

            try
            {
                var matchmakingProperties = new Dictionary<string, string>
                {
                    {"engine", "unity" }
                };

                var query = _config.MatchmakingQuery ?? string.Empty;
                Ticket = await _conn.Socket.AddMatchmakerAsync(query, _config.MinCount, _config.MaxCount, matchmakingProperties);
                _conn.Socket.ReceivedMatchmakerMatched += Socket_ReceivedMatchmakerMatched;
                IsSearching = true;
                OnSearching?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Matchmaking] Start failed: {ex}");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task CancelAsync()
        {
            if (!IsSearching || Ticket == null || _conn.Socket == null) { OnCancelled?.Invoke(); return; }
            try
            {
                await _conn.Socket.RemoveMatchmakerAsync(Ticket);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Matchmaking] Cancel error: {ex.Message}");
            }
            finally
            {
                _conn.Socket.ReceivedMatchmakerMatched -= Socket_ReceivedMatchmakerMatched;
                Ticket = null;
                IsSearching = false;
                OnCancelled?.Invoke();
            }
        }

        private void Socket_ReceivedMatchmakerMatched(IMatchmakerMatched matched)
        {
            // Stop searching; hand over to match client to JoinMatchAsync.
            IsSearching = false;
            _conn.Socket.ReceivedMatchmakerMatched -= Socket_ReceivedMatchmakerMatched;
            OnMatched?.Invoke(matched);
        }
    }
}
