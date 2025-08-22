using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace TTT.Net
{
    /// <summary>
    /// Creates IClient + ISession + ISocket. Handles device auth and socket connect/disconnect.
    /// Uses client.NewSocket(useMainThread: true).
    /// </summary>
    public class NakamaConnection
    {
        public IClient Client { get; private set; }
        public ISession Session { get; private set; }
        public ISocket Socket { get; private set; }

        public bool IsConnected => Socket != null && Socket.IsConnected;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        private readonly TTT.GameConfigSO _config;

        public NakamaConnection(TTT.GameConfigSO config)
        {
            _config = config;
        }

        public async Task<bool> ConnectAsync(string host)
        {
            try
            {
                // Scheme drives BOTH REST and Socket (http->ws, https->wss)
                var scheme = "http";

#if UNITY_WEBGL && !UNITY_EDITOR
                var adapter = new UnityWebRequestAdapter();    // WebGL build
#else
                var adapter = UnityWebRequestAdapter.Instance;  // Editor/Standalone/Android/iOS
#endif
                Client = new Client(scheme, host, _config ? _config.DefaultPort : 1002, _config ? _config.ServerKey : "defaultkey", adapter);

                // Authenticate by device ID (creates the user if needed)
                var deviceId = GetOrCreateDeviceId();
                Session = await Client.AuthenticateDeviceAsync(deviceId, null, true);

                // Create and connect socket (callbacks on main thread)
                CreateFreshSocket();
                await Socket.ConnectAsync(Session, true); // appearOnline = true

                OnConnected?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NakamaConnection] Connect failed: {ex}");
                OnError?.Invoke(ex.Message);
                Disconnect();
                return false;
            }
        }

        public async Task<bool> ReconnectSocketAsync()
        {
            if (Client == null || Session == null)
                return false;

            try
            {
                if (Socket != null && Socket.IsConnected)
                    return true;

                // Always recreate to avoid duplicate event subscriptions
                CreateFreshSocket();
                await Socket.ConnectAsync(Session, true);
                OnConnected?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NakamaConnection] Reconnect failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (Socket != null)
                {
                    Socket.Closed -= Socket_Closed;
                    if (Socket.IsConnected) Socket.CloseAsync();
                }
            }
            catch { /* ignore */ }
            finally
            {
                Socket = null;
                OnDisconnected?.Invoke();
            }
        }

        private void CreateFreshSocket()
        {
            if (Socket != null)
            {
                Socket.Closed -= Socket_Closed;
                try { if (Socket.IsConnected) Socket.CloseAsync(); } catch { }
            }
            Socket = Client.NewSocket(useMainThread: true);
            Socket.Closed += Socket_Closed;
        }

        private void Socket_Closed()
        {
            OnDisconnected?.Invoke();
        }

        private static string GetOrCreateDeviceId()
        {
            const string Key = "device_id";
            if (PlayerPrefs.HasKey(Key))
                return PlayerPrefs.GetString(Key);

            var id = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(id) || id == "unknown")
                id = Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(Key, id);
            PlayerPrefs.Save();
            return id;
        }
    }
}
