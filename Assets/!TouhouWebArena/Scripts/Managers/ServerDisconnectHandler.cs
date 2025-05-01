using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using TouhouWebArena;

namespace TouhouWebArena.Managers
{
    /// <summary>
    /// [Server Only] Handles logic related to client disconnections during a match.
    /// </summary>
    public class ServerDisconnectHandler : NetworkBehaviour
    {
        public static ServerDisconnectHandler Instance { get; private set; }

        [Header("Dependencies")]
        [Tooltip("Reference to the RoundManager instance.")]
        [SerializeField] private RoundManager roundManager;

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
            if (roundManager == null)
            {
                 Debug.LogError("[ServerDisconnectHandler] RoundManager reference not set in the inspector!");
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

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnectServer;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnectServer;
                }
            }
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// [Server Only] Handles client disconnections.
        /// If a client disconnects during the post-match/rematch phase, force remaining clients back to menu.
        /// </summary>
        private void HandleClientDisconnectServer(ulong disconnectedClientId)
        {
            if (!IsServer || roundManager == null) return;

            Debug.Log($"[ServerDisconnectHandler] Client disconnected: {disconnectedClientId}");

            if (roundManager.IsInPostMatchPhase())
            {
                Debug.Log("[ServerDisconnectHandler] Client disconnected during post-match phase. Resetting rematch state and returning remaining players to menu.");

                roundManager.ResetRematchFlags();

                roundManager.StopDisconnectHandlerCoroutines();

                List<ulong> remainingClientIds = new List<ulong>();
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.ClientId != disconnectedClientId)
                    {
                        remainingClientIds.Add(client.ClientId);
                    }
                }

                if (remainingClientIds.Count > 0)
                {
                    ClientRpcParams remainingClientsParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = remainingClientIds.ToArray()
                        }
                    };
                    ForceReturnToMenuClientRpc(remainingClientsParams);
                }
                else
                {
                    Debug.Log("[ServerDisconnectHandler] Last client disconnected during post-match. Shutting down server and returning to menu.");

                    var networkManager = NetworkManager.Singleton;

                    roundManager.ResetRematchFlags();

                    if (networkManager != null)
                    {
                        Debug.Log("[ServerDisconnectHandler] Initiating NetworkManager Shutdown...");
                        networkManager.Shutdown();
                        Debug.Log("[ServerDisconnectHandler] NetworkManager Shutdown called.");
                    }
                    else
                    {
                        Debug.LogWarning("[ServerDisconnectHandler] NetworkManager was already null before shutdown.");
                    }

                    PlayerDataManager pdmInstance = FindObjectOfType<PlayerDataManager>();
                    if (pdmInstance != null && pdmInstance.TryGetComponent<NetworkObject>(out var pdmNetObj))
                    {
                        if (pdmNetObj.IsSpawned)
                        {
                             Debug.Log("[ServerDisconnectHandler] Despawning PlayerDataManager instance...");
                             pdmNetObj.Despawn(true);
                        }
                        else { Debug.LogWarning("[ServerDisconnectHandler] Found PlayerDataManager but it wasn't spawned?"); }
                    }
                    else { Debug.LogWarning("[ServerDisconnectHandler] Could not find PlayerDataManager instance to despawn."); }

                    Debug.Log("[ServerDisconnectHandler] Loading MainMenuScene...");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                }
            }
            else
            {
                Debug.Log("[ServerDisconnectHandler] Client disconnected during active gameplay or before match end. Normal disconnect process handled by NetworkManager.");
            }
        }

        /// <summary>
        /// [ClientRpc] Forces the targeted clients to shut down networking and return to the main menu.
        /// Used when a player quits during the post-match phase.
        /// </summary>
        [ClientRpc]
        private void ForceReturnToMenuClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton == null) {
                 Debug.LogWarning($"[ServerDisconnectHandler - Client ?] Received ForceReturnToMenu RPC but NetworkManager is already null.");
                 return; 
            }

            Debug.Log($"[ServerDisconnectHandler - Client {NetworkManager.Singleton.LocalClientId}] Received ForceReturnToMenu RPC.");

            NetworkManager.Singleton.Shutdown();

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }
} 