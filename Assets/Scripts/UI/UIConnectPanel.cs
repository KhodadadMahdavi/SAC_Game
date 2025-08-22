using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TTT.UI
{
    /// <summary>
    /// Connect panel: host, port, SSL, Connect button.
    /// Emits ConnectRequested(host, port, useSSL).
    /// </summary>
    [AddComponentMenu("TTT/UI/Connect Panel")]
    public class UIConnectPanel : MonoBehaviour
    {
        public const string PrefKeyHost = "nakama_host";

        [Header("Inputs")]
        public TMP_InputField inputHost;

        [Header("Buttons")]
        public Button btnConnect;

        [Header("Status")]
        public TMP_Text statusText;

        [Header("Config (optional)")]
        public GameConfigSO config;

        public event Action<string> ConnectRequested;

        void Start()
        {
            // Prefill defaults.
            string host = PlayerPrefs.GetString(PrefKeyHost, config ? config.DefaultHost : "127.0.0.1");

            if (inputHost) inputHost.text = host;

            if (btnConnect) btnConnect.onClick.AddListener(OnClickConnect);

            SetInteractable(true);
            SetStatus("");
        }

        void OnDestroy()
        {
            if (btnConnect) btnConnect.onClick.RemoveListener(OnClickConnect);
        }

        private void OnClickConnect()
        {
            var host = inputHost ? inputHost.text.Trim() : "127.0.0.1";
            if (string.IsNullOrEmpty(host))
            {
                SetStatus("Host is required.");
                return;
            }

            // Persist for next run.
            PlayerPrefs.SetString(PrefKeyHost, host);
            PlayerPrefs.Save();

            SetInteractable(false);
            SetStatus("Connecting...");

            ConnectRequested?.Invoke(host);
        }

        public void SetStatus(string msg)
        {
            if (statusText) statusText.text = msg ?? "";
        }

        public void SetInteractable(bool value)
        {
            if (btnConnect) btnConnect.interactable = value;
            if (inputHost) inputHost.interactable = value;
        }
    }
}
