using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement; // For LoadSceneMode
using System.Collections; // For Coroutine

// Listens for Matchmaker events to handle game setup tasks like role assignment and scene loading.
// Should exist in the same scene as Matchmaker.
public class PlayerSetupManager : NetworkBehaviour
{
    [SerializeField] private float sceneTransitionDelay = 1.0f; // Match Matchmaker's delay? Or separate?
    [SerializeField] private string characterSelectSceneName = "CharacterSelectScene";

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Only the server needs to listen and react
            enabled = false;
            return;
        }

        // Subscribe to the Matchmaker event
        if (Matchmaker.Instance != null)
        {
            Matchmaker.Instance.OnMatchFoundServer += HandleMatchFound;
            // We might not need OnPlayerQueuedServer listener if registration is handled elsewhere
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && Matchmaker.Instance != null)
        {
            Matchmaker.Instance.OnMatchFoundServer -= HandleMatchFound;
        }
        base.OnNetworkDespawn();
    }

    private void HandleMatchFound(ulong player1Id, ulong player2Id)
    {
        // 1. Assign Roles
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AssignPlayerRole(player1Id, PlayerRole.Player1);
            PlayerDataManager.Instance.AssignPlayerRole(player2Id, PlayerRole.Player2);
        }
        else
        {
            // Decide how to handle this - maybe cancel scene load?
            return;
        }

        // 2. Trigger Scene Load (after a delay)
        StartCoroutine(LoadCharacterSelectSceneDelayed());
    }

    private IEnumerator LoadCharacterSelectSceneDelayed()
    {
        yield return new WaitForSeconds(sceneTransitionDelay);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(characterSelectSceneName, LoadSceneMode.Single);
        }
    }
} 