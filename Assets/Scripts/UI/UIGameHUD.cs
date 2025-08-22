using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TTT.UI
{
    /// <summary>
    /// Game HUD: mark label (X/O), turn label, countdown, leave, rejoin banner.
    /// Emits LeaveRequested().
    /// </summary>
    [AddComponentMenu("TTT/UI/Game HUD")]
    public class UIGameHud : MonoBehaviour
    {
        [Header("Labels")]
        public TMP_Text labelMark;     // "You are X" or "You are O"
        public TMP_Text labelTurn;     // "Your turn" / "Opponent's turn"

        [Header("Countdown")]
        public CountdownView countdownView;

        [Header("Buttons")]
        public Button btnLeave;

        [Header("Rejoin")]
        public GameObject rejoinBanner; 
        [Range(0.2f, 10f)] public float rejoinBannerDefaultSeconds = 1.5f;

        public event Action LeaveRequested;


        void Start()
        {
            if (btnLeave) btnLeave.onClick.AddListener(() => LeaveRequested?.Invoke());
            ShowRejoin(false);
            SetTurn(isYourTurn: false);
            SetMark(0);
            SetCountdown(0);
        }

        void OnDestroy()
        {
            if (btnLeave) btnLeave.onClick.RemoveAllListeners();
        }

        public void SetMark(int mark) // 1=X, 2=O
        {
            if (!labelMark) return;
            string s = mark == 1 ? "X" : mark == 2 ? "O" : "?";
            labelMark.text = $"You are {s}";
        }

        public void SetTurn(bool isYourTurn)
        {
            if (labelTurn) labelTurn.text = isYourTurn ? "Your turn" : "Opponent's turn";
        }

        /// <summary> Sets countdown seconds remaining for current turn. </summary>
        public void SetCountdown(float seconds)
        {
            if (countdownView) countdownView.Set(seconds);
        }


        public void ShowRejoin(bool show)
        {
            if (rejoinBanner) rejoinBanner.SetActive(show);
            if (!show) { StopAllCoroutines(); }
        }

        /// <summary>Shows the rejoin banner for a short duration, then hides it.</summary>
        public void PulseRejoin(float seconds = -1f)
        {
            if (seconds <= 0f) seconds = rejoinBannerDefaultSeconds;
            if (!rejoinBanner) return;

            rejoinBanner.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(CoHideRejoin(seconds));
        }

        private IEnumerator CoHideRejoin(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            ShowRejoin(false);
        }
    }
}
