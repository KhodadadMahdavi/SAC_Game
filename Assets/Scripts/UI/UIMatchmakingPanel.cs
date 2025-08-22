using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TTT.UI
{
    /// <summary>
    /// Matchmaking panel: Play/Cancel and status text.
    /// Emits PlayRequested() and CancelRequested().
    /// </summary>
    [AddComponentMenu("TTT/UI/Matchmaking Panel")]
    public class UIMatchmakingPanel : MonoBehaviour
    {
        [Header("Buttons")]
        public Button btnPlay;
        public Button btnCancel;

        [Header("Status")]
        public TMP_Text statusText;

        public event Action PlayRequested;
        public event Action CancelRequested;

        void Start()
        {
            if (btnPlay) btnPlay.onClick.AddListener(OnPlay);
            if (btnCancel) btnCancel.onClick.AddListener(OnCancel);
            SetSearching(false);
            SetStatus("");
        }

        void OnDestroy()
        {
            if (btnPlay) btnPlay.onClick.RemoveListener(OnPlay);
            if (btnCancel) btnCancel.onClick.RemoveListener(OnCancel);
        }

        private void OnPlay()
        {
            SetSearching(true);
            SetStatus("Searching for opponent...");
            PlayRequested?.Invoke();
        }

        private void OnCancel()
        {
            SetSearching(false);
            SetStatus("Cancelled.");
            CancelRequested?.Invoke();
        }

        public void SetSearching(bool searching)
        {
            if (btnPlay) btnPlay.gameObject.SetActive(!searching);
            if (btnCancel) btnCancel.gameObject.SetActive(searching);
        }

        public void SetStatus(string msg)
        {
            if (statusText) statusText.text = msg ?? "";
        }
    }
}
