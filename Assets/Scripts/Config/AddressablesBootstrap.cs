using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TTT
{
    /// <summary>
    /// Initializes Addressables, updates catalogs if needed, and loads the remote GameBoard prefab.
    /// Place this on a bootstrap object in your App scene.
    /// Exposes OnGameplayAssetsReady(GameObject boardPrefab).
    /// </summary>
    [AddComponentMenu("TTT/Addressables Bootstrap")]
    public class AddressablesBootstrap : MonoBehaviour
    {
        public GameConfigSO config;

        public event Action<GameObject> OnGameplayAssetsReady;

        [Header("Optional status hooks")]
        public TTT.UI.UIToast toast; // optional, to display update progress

        private CancellationTokenSource _cts;

        async void Start()
        {
            _cts = new CancellationTokenSource();
            try
            {
                await InitializeAndUpdateAsync(_cts.Token);

                // Load the board prefab
                string key = config ? config.GameBoardKey : "GameBoard";
                var boardPrefab = await Addressables.LoadAssetAsync<GameObject>(key).Task;
                if (boardPrefab == null)
                    throw new Exception($"Failed to load Addressable '{key}'.");

                OnGameplayAssetsReady?.Invoke(boardPrefab);
                toast?.Show("Gameplay assets ready.", 1.5f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressablesBootstrap] {ex}");
                toast?.Show("Asset init failed. Check network/URL.", 3f);
            }
        }

        private async Task InitializeAndUpdateAsync(CancellationToken ct)
        {
            await Addressables.InitializeAsync().Task;

            List<string> catalogsToUpdate = await Addressables.CheckForCatalogUpdates().Task;
            if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
            {
                toast?.Show("Updating content…", 2f);
                await Addressables.UpdateCatalogs(catalogsToUpdate).Task;
            }
        }

        void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
