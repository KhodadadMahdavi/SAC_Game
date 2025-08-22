using System.Threading.Tasks;
using TTT.Json;
using TTT.Net;
using TTT.UI;
using UnityEngine;

namespace TTT.Game
{
    /// <summary>
    /// Orchestrates gameplay: hooks Net events, updates BoardView/UI, sends moves.
    /// </summary>
    [AddComponentMenu("TTT/Game/TTT Match Controller")]
    public class TTTMatchController : MonoBehaviour
    {
        [Header("Refs (assign in AppController)")]
        public GameConfigSO config;
        public UIManager ui;
        public UIConnectPanel connectPanel;
        public UIMatchmakingPanel matchmakingPanel;
        public UIGameHud gameHud;
        public AddressablesBootstrap addrBootstrap;

        [Header("Dynamic board")]
        public Transform boardParent; // empty RectTransform under GameHud where board spawns
        private GameObject _boardGO;
        private BoardView _board;

        // Net layers
        private NakamaConnection _conn;
        private NakamaMatchmaking _mm;
        private NakamaMatchClient _match;
        private ReconnectManager _rejoin;

        private int _yourMark = 0; // 1 X, 2 O (unknown until we deduce)
        private bool _canInteract = false;

        public void Init(NakamaConnection conn, NakamaMatchmaking mm, NakamaMatchClient match, ReconnectManager rejoin)
        {
            _conn = conn;
            _mm = mm;
            _match = match;
            _rejoin = rejoin;

            // Wire UI -> actions
            connectPanel.ConnectRequested += async (host) => await OnConnect(host);
            matchmakingPanel.PlayRequested += async () => await OnPlay();
            matchmakingPanel.CancelRequested += async () => await _mm.CancelAsync();
            gameHud.LeaveRequested += async () => await OnLeave();

            _conn.OnConnected += () =>
            {
                _match.HookSocket();                 // subscribe on the actual live socket
                UIToast.Instance?.Show("Connected.", 1.2f);
            };

            _conn.OnDisconnected += () =>
            {
                _match.UnhookSocket();               // cleanly unhook
                UIToast.Instance?.Show("Disconnected.", 1.2f);
            };



            // Net events
            _conn.OnConnected += () => UIToast.Instance?.Show("Connected.", 1.2f);
            _conn.OnDisconnected += () => UIToast.Instance?.Show("Disconnected.", 1.2f);
            _conn.OnError += (e) => connectPanel.SetStatus($"Error: {e}");

            _mm.OnSearching += () => matchmakingPanel.SetStatus("Searching…");
            _mm.OnCancelled += () => matchmakingPanel.SetStatus("Cancelled.");
            _mm.OnMatched += async matched =>
            {
                matchmakingPanel.SetStatus("Match found. Joining…");
                var ok = await _match.JoinFromMatchmakerAsync(matched);
                if (ok)
                {
                    //_yourMark = 0; // will deduce on first move/state
                    ui.ShowGame();

                    gameHud.countdownView.maxSeconds = config.TurnTimeoutSeconds;

                    gameHud.SetMark(0);
                    if (gameHud.labelTurn) gameHud.labelTurn.text = "Waiting for server…";
                }
            };
            _mm.OnError += (e) => matchmakingPanel.SetStatus($"Error: {e}");

            _match.OnJoined += () =>
            {
                UIToast.Instance?.Show("Joined match.", 1f);
                _canInteract = true;
            };
            _match.OnLeft += () =>
            {
                UIToast.Instance?.Show("Left match.", 1f);
                _canInteract = false;
                _yourMark = 0;
                if (_board) _board.ClearAll();
                ui.ShowMatchmaking();
                matchmakingPanel.SetSearching(false);
            };
            _match.OnState += OnState;
            _match.OnGameOver += async sm =>
            {
                // winner: 1=X, 2=O, 3=draw
                if (sm.winner == 3)
                    UIToast.Instance?.Show("Draw!", 2f);
                else
                    UIToast.Instance?.Show((_yourMark != 0 && sm.winner == _yourMark) ? "You won! 🎉" : "You lost.", 2.2f);

                _canInteract = false;
                _yourMark = 0;
                _board?.ClearAll();

                // Prevent rejoin of a finished match.
                _match.ClearLastMatchCache();

                // Leave (safe even if server already closed it)
                await _match.LeaveAsync();

                // Back to matchmaking
                ui.ShowMatchmaking();
                matchmakingPanel.SetSearching(false);
            };


            _match.OnError += em => UIToast.Instance?.Show(em.message, 2f);
            _match.OnClientError += msg => UIToast.Instance?.Show(msg, 2f);

            _rejoin.OnRejoinResult += ok =>
            {
                if (ok)
                {
                    gameHud.PulseRejoin();  // auto-hide after default seconds
                    ui.ShowGame();
                }
                else
                {
                    gameHud.ShowRejoin(false);
                }
            };

            // Addressables -> load board prefab when ready
            addrBootstrap.OnGameplayAssetsReady += prefab =>
            {
                if (_boardGO) Destroy(_boardGO);
                _boardGO = Instantiate(prefab, boardParent ? boardParent : gameHud.transform);
                _board = _boardGO.GetComponent<BoardView>();
                if (_board)
                    _board.OnCellClicked += async i => await OnCellClicked(i);
            };
        }

        private async Task OnConnect(string host)
        {
            connectPanel.SetStatus("Connecting…");
            connectPanel.SetInteractable(false);

            var ok = await _conn.ConnectAsync(host);
            if (ok)
            {
                ui.ShowMatchmaking();
                matchmakingPanel.SetSearching(false);
                matchmakingPanel.SetStatus("Ready.");
                // Try rejoin
                await _rejoin.TryRejoinAsync();
            }
            else
            {
                connectPanel.SetStatus("Connection failed. Check address.");
            }

            connectPanel.SetInteractable(true);
        }

        private async Task OnPlay()
        {
            var ok = await _mm.StartAsync();
            if (!ok)
            {
                matchmakingPanel.SetSearching(false);
                matchmakingPanel.SetStatus("Search failed.");
            }
        }

        private async Task OnLeave()
        {
            await _match.LeaveAsync();
        }

        private async Task OnCellClicked(int index)
        {
            if (!_canInteract) return;

            if (_yourMark != 0 && _lastState.next != 0 && _yourMark != _lastState.next)
            {
                UIToast.Instance?.Show("Not your turn.", 1f);
                return;
            }
            await _match.SendMoveAsync(index);
        }

        private StateMessage _lastState;

        private void OnState(StateMessage sm)
        {
            _lastState = sm;

            // Board
            if (_board != null && sm.board != null && sm.board.Length >= 9)
                _board.SetBoard(sm.board);

            // Set your mark as soon as seat_you arrives
            if (_yourMark == 0 && (sm.seat_you == 0 || sm.seat_you == 1))
                _yourMark = (sm.seat_you == 0) ? 1 : 2;  // seat 0 => X(1), seat 1 => O(2)

            gameHud.SetMark(_yourMark);

            // --- Turn label ---
            var yourTurn = (sm.winner == 0 && _yourMark != 0 && sm.next == _yourMark);
            gameHud.SetTurn(yourTurn);

            // Reset
            gameHud.SetCountdown(config.TurnTimeoutSeconds);

            // Win highlight
            if (sm.winning_line != null && sm.winning_line.Length == 3)
                _board?.HighlightLine(sm.winning_line);
            else
                _board?.ClearHighlights();

            // Lock input if game ended
            _canInteract = (sm.winner == 0);
        }

    }
}
