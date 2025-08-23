# Tic-Tac-Toe Unity Client (Nakama)

## Abstract

This repository contains a **Unity client** for a server‑authoritative Tic‑Tac‑Toe experience powered by **Nakama**. The client implements connection, matchmaking, real‑time match play, a **10‑second turn timer**, personalized rendering based on the server’s `seat_you`, and a resilient **rejoin** flow. Content is delivered via **Addressables**, enabling hot updates of gameplay prefabs without republishing the app.

## Game description and rules

Tic‑Tac‑Toe is played on a 3×3 grid by two players, **X** and **O**. X moves first and players alternate turns placing a mark in an empty cell. The first player to align three identical marks in a row, column, or diagonal wins. If the grid fills with no such alignment, the game ends in a draw. In this implementation, the server enforces legality and timing, and the client renders the **authoritative** state it receives.

---

## Client architecture overview

### Architecture viewpoints (concise, 42010‑aligned)

**Context view.** The Unity app communicates with a running Nakama server (gRPC/HTTP for auth, WebSocket for match updates). Remote content (board prefab) is hosted behind a static HTTP endpoint for Addressables.

**Logical view.** The client is layered:

- **UI layer.** Panels for Connect, Matchmaking, Game HUD, and a toast surface for transient feedback.
    
- **Net layer.** `NakamaConnection` (device auth + socket), `NakamaMatchmaking` (pooling), `NakamaMatchClient` (join/leave/moves/state), `ReconnectManager` (rejoin).
    
- **Game layer.** `TTTMatchController` orchestrates play; `BoardView` and `CellButton` render input and marks.
    
- **Content & config.** `AddressablesBootstrap` initializes and updates catalogs, then loads the remote `GameBoard` prefab; `GameConfigSO` centralizes client defaults (host/port, opcodes, timer).
    

**Process/runtime view.** Startup initializes Addressables and updates catalogs, then shows the Connect panel. On connect the client authenticates with a **device ID**, creates a socket via `client.NewSocket(useMainThread: true)`, and subscribes to socket events **after** `ConnectAsync`. When the matchmaker signals a pairing, the client joins the match and renders server snapshots (`opState` / `opGameOver`).

**Information view.** Inbound snapshots include `board[9]`, `next`, `winner`, optional `winning_line`, `deadline_tick`, and **`seat_you`** (0 or 1). Outbound moves include `{ index: 0..8 }`. The client treats every snapshot as the source of truth and updates the UI accordingly.

**Deployment view (client).** The app targets **Android** (portrait, IL2CPP, ARM64). The `GameBoard` prefab is an Addressable deployed remotely; it can be updated independently of the app build.

---

## Matchmaking policy and cadence

The server’s matchmaker emits matches on a **10‑second cadence**. A player may enter the pool at any time, yet will be notified on the next cycle boundary. Deterministic seating assigns **seat 0 → X** and **seat 1 → O**; the server includes `seat_you` in each snapshot for immediate client role resolution.

## Timing model and countdown

The server runs at **5 Hz**. For each turn it sets `deadline_tick = current_tick + 5 × 10s`. The client displays a **10‑second** countdown aligned to state changes. For precise visual sync you may extend the payload with `server_tick` and derive `secondsLeft = (deadline_tick − server_tick) / tickRate`.

## Rejoin behavior

If the socket reconnects while a match is still alive, the client attempts to **rejoin** the last `matchId`. On success it receives a full snapshot, resumes play, and shows a brief **rejoin banner** which auto‑hides.

## Remote Addressables in this project

This project uses Unity Addressables to deliver gameplay content remotely so the app can adopt new board visuals/logic without republishing. Concretely, only the gameplay prefab(s) are remote: `GameBoard.prefab` (and its `CellButton` children). All core UI remains local for instant boot.

What the client does at runtime (from `AddressablesBootstrap.cs`)

At startup the client initializes Addressables, checks for a new remote catalog, applies updates if present, and then loads the board prefab by key from `GameConfigSO.GameBoardKey` (default "GameBoard"). After loading, it raises an event so gameplay can instantiate the board.

```csharp
// AddressablesBootstrap.cs (essentials)
using UnityEngine.AddressableAssets;

async void Start()
{
    await Addressables.InitializeAsync().Task;

    var catalogs = await Addressables.CheckForCatalogUpdates().Task;
    if (catalogs != null && catalogs.Count > 0)
        await Addressables.UpdateCatalogs(catalogs).Task;

    string key = config ? config.GameBoardKey : "GameBoard";
    var boardPrefab = await Addressables.LoadAssetAsync<GameObject>(key).Task;
    if (boardPrefab == null) throw new System.Exception($"Missing Addressable '{key}'");

    OnGameplayAssetsReady?.Invoke(boardPrefab);
}
```

`TTTMatchController` subscribes to that event, spawns the board under `BoardPlaceholder`, and wires cell clicks to send moves:

```csharp
// TTTMatchController.Init(...)
addrBootstrap.OnGameplayAssetsReady += prefab =>
{
    if (_boardGO) Destroy(_boardGO);
    _boardGO = Instantiate(prefab, boardParent);
    _board = _boardGO.GetComponent<BoardView>();
    if (_board)
        _board.OnCellClicked += async i => await OnCellClicked(i);
};
```

### How we build and host (project‑specific)

In the Addressables Groups window we keep two group categories:

`UI.Local` → static, non‑remote UI assets (`CanvasRoot`, panels, Toast).

`Gameplay.Remote` → `GameBoard.prefab` (key "GameBoard") and `CellButton.prefab` with Remote Build/Load Paths.

Build Addressables (Default Build Script). Upload the generated `ServerData/<Platform>` to your HTTP host/CDN and set your Remote Load Path accordingly in the Addressables profile. On next app start, the bootstrap above will pull the updated catalog and load the new prefab.

### Updating content in production

When you edit the remote board prefab, run Build → Update a Previous Build against the last catalog. Upload the new `ServerData/<Platform>` output. Clients will detect the catalog change in `CheckForCatalogUpdates()` and download only the changed bundles.

### Notes specific to this client

The board prefab is loaded once per app run; we do not unload it. If you add screen transitions that recreate gameplay, call `Addressables.ReleaseInstance(instance)` when destroying it.

Errors during init surface as a user toast; the game can still connect (UI is local), but gameplay will not spawn until the prefab loads successfully.


---
## Configuration

### GameConfigSO

The `GameConfigSO` ScriptableObject centralizes client defaults:

- Nakama defaults: scheme (http/https), host, port, server key, socket security.
- Matchmaking bounds and UI timer: `TurnTimeoutSeconds = 10`, `TickRate = 5`.
- Protocol constants: `MatchName`, `OpState = 1`, `OpMove = 2`, `OpError = 3`, `OpGameOver = 4`.
- Addressables key for the remote `GameBoard` prefab.

The Connect panel pre‑populates fields from the config and persists the last values in `PlayerPrefs`. Ensure the **client timer** matches the **server** timeout (10 seconds).

---

## Build and run

### Prerequisites

Install Unity (2021 LTS or newer), Android Build Support, and TextMeshPro. Ensure the Nakama server from the companion repository is running.

### Editor play

Open the project, enter Nakama **host/port/SSL** in the Connect panel, and press **Connect**. Use two clients to press **Play** and join the matchmaking pool; the match will start when a cycle emits.

### Android build

Set **Orientation** to _Portrait_, **Scripting Backend** to _IL2CPP_, **ARM64** architecture. For secure transport use valid certificates; for local testing prefer `http/ws` and allow cleartext if needed on Android’s network security config.

---

## Minimal code examples

### Connect and hook the socket (C#)

```csharp
// Assume: client created with proper scheme/host/port.
var socket = client.NewSocket(useMainThread: true);
socket.Closed += () => Debug.Log("Socket closed");
await socket.ConnectAsync(session, appearOnline: true);

// Subscribe AFTER connect so this is the live socket
socket.ReceivedMatchState += state => Debug.Log($"op={state.OpCode} bytes={state.State?.Length ?? 0}");
```

### Start matchmaking and join (C#)

```csharp
var ticket = await socket.AddMatchmakerAsync(query: "", minCount: 2, maxCount: 2, stringProperties: null);
socket.ReceivedMatchmakerMatched += async matched => {
    var match = await socket.JoinMatchAsync(matched);
    Debug.Log($"Joined match {match.Id}");
};
```

### Handle snapshots and render turn (C#)

```csharp
[Serializable] struct StateMessage {
  public int[] board; public int next; public int winner; public int[] winning_line; public long deadline_tick; public int seat_you;
}

socket.ReceivedMatchState += s => {
  if (s.OpCode != 1 && s.OpCode != 4) return; // 1=state, 4=gameover
  var json = System.Text.Encoding.UTF8.GetString(s.State);
  var sm = JsonUtility.FromJson<StateMessage>(json);
  int yourMark = sm.seat_you == 0 ? 1 : 2; // 1=X,2=O
  bool yourTurn = sm.winner == 0 && sm.next == yourMark;
  // Update BoardView from sm.board, labelTurn from yourTurn, and countdown from config (10s)
};
```

### Send a move (C#)

```csharp
[Serializable] struct ClientMove { public int index; }
async Task SendMoveAsync(ISocket socket, string matchId, int idx) {
  var json = JsonUtility.ToJson(new ClientMove{ index = idx });
  var bytes = System.Text.Encoding.UTF8.GetBytes(json);
  await socket.SendMatchStateAsync(matchId, 2 /* opMove */, bytes);
}
```

### Snapshot payload (server → client)

```json
{
  "board": [0,1,0, 0,2,0, 0,0,0],
  "next": 1,
  "winner": 0,
  "winning_line": [0,4,8],
  "deadline_tick": 245,
  "seat_you": 0
}
```

---

## Testing and evaluation guidance

Functional testing should validate: deterministic seating and first‑turn assignment; legality checks for all move scenarios; win detection across all eight lines; draw detection; enforcement of the **10‑second** deadline; rejoin success after transient disconnects; opponent departure handing and winner attribution. Latency or packet‑loss simulation can be used to observe client behavior under adverse conditions.

---

## Limitations and future work

The client currently focuses on a single game mode with minimal live‑ops UI. It does not surface persistent progression, leaderboards, store fronts, friends/clans, or party formation. It also assumes a single server region and does not expose settings for region selection or quality‑of‑service metrics. Future iterations can:

### Integrate broader backend features

Add UI flows for **leaderboards** (browse, season resets, prize claims), **shops** (catalog, offers, receipts), **storage‑backed** profiles and cosmetics, **friends/clans** (requests, invites, roles), and **parties** for pre‑match coordination. These features are powered by Nakama services and surfaced in the client with authoritative RPCs.

### Extend with Nakama runtime capabilities

Adopt **RPCs** for actions like purchases and prize claims, **hooks** for validation and enrichment, **scheduled jobs** for rotations and seasonal resets, and **custom HTTP endpoints** for internal tools. The client then interacts only with supported surfaces; all authority remains server‑side.

### Improve UX and telemetry

Introduce explicit turn indicators with color and haptics, cell disabling when it is not the user’s turn, subtle animations on mark placement, and structured client telemetry for UX diagnostics. Add a “Rematch” affordance that orchestrates party or direct rematch flows via RPC.

---

## Acknowledgements

This client targets the companion Nakama module that provides personalized snapshots, deterministic seating, a 10‑second per‑turn deadline, and batched matchmaking cadence. Together they form a minimal yet robust architecture appropriate for academic analysis and practical gameplay.
