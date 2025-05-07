using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace TouhouWebArena.Managers
{
    /// <summary>
    /// [Server Only] Manages references to and controls game entity spawners (Spirit, Fairy).
    /// </summary>
    public class ServerSpawnerManager : NetworkBehaviour
    {
        public static ServerSpawnerManager Instance { get; private set; }

        // Cached references (Server only)
        private SpiritSpawner spiritSpawnerInstance;
        private List<FairySpawner> fairySpawnerInstances = new List<FairySpawner>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                enabled = false; // This script is server-only
                return;
            }

            // Cache Spawner References on Server
            CacheSpawnerReferences();
        }

        public override void OnNetworkDespawn()
        {
             if (Instance == this)
            {
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Finds and caches references to the Spirit and Fairy spawners in the scene.
        /// Server only.
        /// </summary>
        private void CacheSpawnerReferences()
        {
            if (!IsServer) return;

            spiritSpawnerInstance = FindFirstObjectByType<SpiritSpawner>();
            if (spiritSpawnerInstance == null)
            {
                Debug.LogWarning("[ServerSpawnerManager] Could not find SpiritSpawner instance to cache.");
            }

            fairySpawnerInstances.Clear();
            fairySpawnerInstances.AddRange(FindObjectsByType<FairySpawner>(FindObjectsSortMode.None));
            if (fairySpawnerInstances.Count == 0)
            {
                Debug.LogWarning("[ServerSpawnerManager] Could not find any FairySpawner instances to cache.");
            }
            Debug.Log($"[ServerSpawnerManager] Cached {fairySpawnerInstances.Count} Fairy Spawners and {(spiritSpawnerInstance != null ? 1 : 0)} Spirit Spawner.");
        }

        /// <summary>
        /// [Server Only] Disables spawning on all cached spawners.
        /// </summary>
        public void PauseAllSpawners()
        {
             if (!IsServer) return;
             Debug.Log("[ServerSpawnerManager] Pausing all spawners.");
             spiritSpawnerInstance?.SetSpawningEnabledServer(false);
             foreach(var fs in fairySpawnerInstances) { fs?.SetSpawningEnabledServer(false); }
        }

        /// <summary>
        /// [Server Only] Enables spawning on all cached spawners.
        /// </summary>
        public void ResumeAllSpawners()
        {
             if (!IsServer) return;
             Debug.Log("[ServerSpawnerManager] Resuming all spawners.");
             spiritSpawnerInstance?.SetSpawningEnabledServer(true);
             foreach(var fs in fairySpawnerInstances) { fs?.SetSpawningEnabledServer(true); }
        }
    }
} 