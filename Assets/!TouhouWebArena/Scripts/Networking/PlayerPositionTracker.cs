using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerPositionTracker : NetworkBehaviour
{
    public NetworkVariable<Vector3> Player1Position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> Player2Position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Removed PLAYER_TAG as we now use PlayerDataManager
    private const int UPDATE_INTERVAL_FRAMES = 30;

    private int _frameCount = 0;
    // Removed _playerTransforms list as we get players directly

    void Update()
    {
        if (!IsServer) return; // Only the server updates positions

        // Ensure PlayerDataManager is available
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("PlayerPositionTracker: PlayerDataManager instance not found.");
            return;
        }

        _frameCount++;
        if (_frameCount >= UPDATE_INTERVAL_FRAMES)
        {
            _frameCount = 0;
            UpdatePlayerPositions();
        }
    }

    private void UpdatePlayerPositions()
    {
        PlayerData? p1Data = PlayerDataManager.Instance.GetPlayer1Data();
        PlayerData? p2Data = PlayerDataManager.Instance.GetPlayer2Data();

        // Update Player 1 Position
        if (p1Data.HasValue)
        {
            NetworkObject p1NetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(p1Data.Value.ClientId);
            if (p1NetworkObject != null)
            {
                Player1Position.Value = p1NetworkObject.transform.position;
            }
            else
            {
                // Player 1 NetworkObject not found (maybe disconnected or not spawned yet?)
                // Optionally reset or log
                // Player1Position.Value = Vector3.zero; 
            }
        }
        else
        {
            // Player 1 role not assigned yet
            // Optionally reset or log
            // Player1Position.Value = Vector3.zero; 
        }

        // Update Player 2 Position
        if (p2Data.HasValue)
        {
            NetworkObject p2NetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(p2Data.Value.ClientId);
            if (p2NetworkObject != null)
            {
                Player2Position.Value = p2NetworkObject.transform.position;
            }
             else
            {
                // Player 2 NetworkObject not found
                // Player2Position.Value = Vector3.zero;
            }
        }
        else
        {
            // Player 2 role not assigned yet
            // Player2Position.Value = Vector3.zero; 
        }

        // Old logic removed:
        // _playerTransforms = GameObject.FindGameObjectsWithTag(PLAYER_TAG)...
        // ... assignment based on list order ...
    }
} 