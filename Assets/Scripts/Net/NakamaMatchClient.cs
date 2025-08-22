using System;
using System.Text;
using System.Threading.Tasks;
using Nakama;
using TTT.Util;
using UnityEngine;

namespace TTT.Net
{
    /// <summary>
    /// Handles Join/Leave/Send moves and broadcasts server messages to the game.
    /// </summary>
    public class NakamaMatchClient
    {
        public IMatch CurrentMatch { get; private set; }
        public string LastMatchId { get; private set; }
        public string SelfUserId { get; private set; }

        private readonly NakamaConnection _conn;
        private readonly TTT.GameConfigSO _config;

        public event Action OnJoined;
        public event Action OnLeft;
        public event Action<TTT.Json.StateMessage> OnState;
        public event Action<TTT.Json.StateMessage> OnGameOver;
        public event Action<TTT.Json.ErrorMessage> OnError;
        public event Action<string> OnClientError;

        public NakamaMatchClient(NakamaConnection conn, TTT.GameConfigSO config)
        {
            _conn = conn;
            _config = config;
        }

        public void HookSocket()
        {
            if (_conn.Socket == null) return;

            _conn.Socket.ReceivedMatchState += m => MainThreadDispatcher.Enqueue(() =>  Socket_ReceivedMatchState(m)) ;
        }

        public void UnhookSocket()
        {
            if (_conn.Socket == null) return;

            _conn.Socket.ReceivedMatchState -= m => MainThreadDispatcher.Enqueue(() => Socket_ReceivedMatchState(m));
        }

        public async Task<bool> JoinFromMatchmakerAsync(IMatchmakerMatched matched)
        {
            try
            {
                var match = await _conn.Socket.JoinMatchAsync(matched);
                SetCurrentMatch(match);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchClient] JoinFromMatchmaker failed: {ex}");
                OnClientError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task<bool> JoinByIdAsync(string matchId)
        {
            try
            {
                var match = await _conn.Socket.JoinMatchAsync(matchId);
                SetCurrentMatch(match);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchClient] JoinById failed: {ex.Message}");
                OnClientError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task LeaveAsync()
        {
            if (CurrentMatch == null || _conn.Socket == null) return;
            try
            {
                await _conn.Socket.LeaveMatchAsync(CurrentMatch);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchClient] Leave error: {ex.Message}");
            }
            finally
            {
                ClearMatch();
                OnLeft?.Invoke();
            }
        }

        public async Task SendMoveAsync(int index)
        {
            if (CurrentMatch == null) return;
            try
            {
                var payload = new TTT.Json.ClientMove { index = index };
                var json = JsonUtility.ToJson(payload);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _conn.Socket.SendMatchStateAsync(CurrentMatch.Id, (long)_config.OpMove, bytes);
            }
            catch (Exception ex)
            {
                OnClientError?.Invoke($"SendMove failed: {ex.Message}");
            }
        }

        private void SetCurrentMatch(IMatch match)
        {
            CurrentMatch = match;
            SelfUserId = match.Self.UserId;
            LastMatchId = match.Id;
            PlayerPrefs.SetString("last_match_id", LastMatchId);
            PlayerPrefs.Save();
            OnJoined?.Invoke();
        }

        private void ClearMatch()
        {
            CurrentMatch = null;
            // Keep LastMatchId for rejoin attempts; clear when we know match ended.
        }

        public void ClearLastMatchCache()
        {
            LastMatchId = null;
            PlayerPrefs.DeleteKey("last_match_id");
            PlayerPrefs.Save();
        }


        private void Socket_ReceivedMatchState(IMatchState state)
        {
            try
            {
                var op = (int)state.OpCode;
                var json = Encoding.UTF8.GetString(state.State);

                if (op == _config.OpState)
                {
                    var sm = JsonUtility.FromJson<TTT.Json.StateMessage>(json);
                    OnState?.Invoke(sm);
                }
                else if (op == _config.OpGameOver)
                {
                    var sm = JsonUtility.FromJson<TTT.Json.StateMessage>(json);
                    OnGameOver?.Invoke(sm);
                }
                else if (op == _config.OpError)
                {
                    var em = JsonUtility.FromJson<TTT.Json.ErrorMessage>(json);
                    OnError?.Invoke(em);
                }
                else
                {
                    // Unknown opcode -> ignore
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchClient] Parse error: {ex.Message}");
            }
        }
    }
}
