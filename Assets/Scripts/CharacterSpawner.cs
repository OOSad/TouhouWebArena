using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // Required for Dictionary
using System.Collections; // Required for Coroutine

public class CharacterSpawner : NetworkBehaviour
{
    [System.Serializable]
    public struct CharacterPrefabMapping
    {
        public string characterName;
        public GameObject characterPrefab;
    }

    [SerializeField] private List<CharacterPrefabMapping> characterPrefabs; // Assign character prefabs in the Inspector
    [SerializeField] private Transform player1SpawnPoint; // Assign Player 1 spawn point in the Inspector
    [SerializeField] private Transform player2SpawnPoint; // Assign Player 2 spawn point in the Inspector

    private Dictionary<string, GameObject> characterPrefabDict;

    private void Awake()
    {
        // Convert list to dictionary for easier lookup
        characterPrefabDict = new Dictionary<string, GameObject>();
        foreach (var mapping in characterPrefabs)
        {
            if (!string.IsNullOrEmpty(mapping.characterName) && mapping.characterPrefab != null)
            {
                characterPrefabDict[mapping.characterName] = mapping.characterPrefab;
            }
            else
            {
                Debug.LogError("Invalid CharacterPrefabMapping detected. Please check configuration in Inspector.");
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Only the server should spawn characters

        // SpawnCharacters();
        // Instead of spawning immediately, start a short delay coroutine
        StartCoroutine(DelayedSpawnCharacters());
    }

    private IEnumerator DelayedSpawnCharacters()
    {
        // Wait a very short time for clients to potentially finish loading/syncing
        // yield return new WaitForSeconds(0.1f);
        yield return new WaitForSeconds(1.0f); // Increased delay to 1 second
        
        Debug.Log("Executing SpawnCharacters after delay.");
        SpawnCharacters();
    }

    private void SpawnCharacters()
    {
        PlayerDataManager.PlayerData? player1Data = PlayerDataManager.Instance.GetPlayer1Data();
        PlayerDataManager.PlayerData? player2Data = PlayerDataManager.Instance.GetPlayer2Data();

        if (player1Data.HasValue)
        {
            SpawnPlayerCharacter(player1Data.Value, player1SpawnPoint.position, player1SpawnPoint.rotation);
        }
        else
        {
            Debug.LogError("Could not retrieve Player 1 data.");
        }

        if (player2Data.HasValue)
        {
            SpawnPlayerCharacter(player2Data.Value, player2SpawnPoint.position, player2SpawnPoint.rotation);
        }
        else
        {
            Debug.LogError("Could not retrieve Player 2 data.");
        }
    }

    private void SpawnPlayerCharacter(PlayerDataManager.PlayerData playerData, Vector3 position, Quaternion rotation)
    {
        string characterName = playerData.SelectedCharacter.ToString();
        if (characterPrefabDict.TryGetValue(characterName, out GameObject prefabToSpawn))
        {
            // Instantiate at default position first
            // GameObject characterInstance = Instantiate(prefabToSpawn, position, rotation);
            GameObject characterInstance = Instantiate(prefabToSpawn);
            NetworkObject networkObject = characterInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                // Spawn the object first
                networkObject.SpawnAsPlayerObject(playerData.ClientId);

                // THEN set the position and rotation on the server. NetworkTransform will sync this.
                characterInstance.transform.position = position;
                characterInstance.transform.rotation = rotation;
                // Add log immediately after setting position
                Debug.Log($"[Server] Set {characterInstance.name} position to {position} on frame {Time.frameCount}");

                Debug.Log($"Spawned {characterName} for Player {playerData.ClientId} ({playerData.PlayerName}) at {position}"); // Updated log
            }
            else
            {
                Debug.LogError($"Prefab {characterName} is missing a NetworkObject component.");
                Destroy(characterInstance); // Clean up the wrongly configured prefab instance
            }
        }
        else
        {
            Debug.LogError($"No prefab found for character name: {characterName}. Cannot spawn character for Player {playerData.ClientId}.");
        }
    }
} 