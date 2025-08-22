using UnityEngine;

namespace TTT.UI
{
    /// <summary> Simple panel switcher for Connect/Matchmaking/Game. Put this on CanvasRoot. </summary>
    [AddComponentMenu("TTT/UI/UI Manager")]
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject connectPanel;
        public GameObject matchmakingPanel;
        public GameObject gameHudPanel;

        void Awake()
        {
            // Default to Connect on first run.
            ShowConnect();
        }

        public void ShowConnect()
        {
            SetActive(connectPanel, true);
            SetActive(matchmakingPanel, false);
            SetActive(gameHudPanel, false);
        }

        public void ShowMatchmaking()
        {
            SetActive(connectPanel, false);
            SetActive(matchmakingPanel, true);
            SetActive(gameHudPanel, false);
        }

        public void ShowGame()
        {
            SetActive(connectPanel, false);
            SetActive(matchmakingPanel, false);
            SetActive(gameHudPanel, true);
        }

        private void SetActive(GameObject go, bool active)
        {
            if (go && go.activeSelf != active) go.SetActive(active);
        }
    }
}
