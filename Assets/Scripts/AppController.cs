using TTT.Game;
using TTT.Net;
using TTT.UI;
using UnityEngine;

namespace TTT
{
    /// <summary>
    /// One place to wire UI ↔ Net ↔ Game. Put this on a GameObject in your App scene.
    /// Assign references in Inspector: config, UI panels, AddressablesBootstrap, boardParent.
    /// </summary>
    [AddComponentMenu("TTT/App Controller")]
    public class AppController : MonoBehaviour
    {
        [Header("Config/Bootstrap")]
        public GameConfigSO config;
        public AddressablesBootstrap addressablesBootstrap;

        [Header("UI")]
        public UIManager ui;
        public UIConnectPanel connectPanel;
        public UIMatchmakingPanel matchmakingPanel;
        public UIGameHud gameHud;

        [Header("Gameplay")]
        public TTTMatchController matchController;

        private NakamaConnection _conn;
        private NakamaMatchmaking _mm;
        private NakamaMatchClient _match;
        private ReconnectManager _rejoin;

        void Awake()
        {
            // Create Net layers
            _conn = new NakamaConnection(config);
            _match = new NakamaMatchClient(_conn, config);
            _mm = new NakamaMatchmaking(_conn, config);
            _rejoin = new ReconnectManager(_conn, _match, config);

            // Provide references to controller
            matchController.config = config;
            matchController.ui = ui;
            matchController.connectPanel = connectPanel;
            matchController.matchmakingPanel = matchmakingPanel;
            matchController.gameHud = gameHud;
            matchController.addrBootstrap = addressablesBootstrap;

            matchController.Init(_conn, _mm, _match, _rejoin);
        }
    }
}
