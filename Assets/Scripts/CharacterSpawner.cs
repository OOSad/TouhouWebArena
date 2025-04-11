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
        
        // Debug.Log("Executing SpawnCharacters after delay.");
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
        GameObject prefab = GetPrefabByName(characterName);
        if (prefab == null)
        {
            // Debug.LogError($"[Spawner] Prefab not found for character: {characterName}"); // Keep this essential error log
            return;
        }

        // Debug.Log($"[Spawner] Preparing to instantiate {characterName} for Client ID {playerData.ClientId}"); // Remove diagnostic log
        GameObject characterInstance = Instantiate(prefab, position, rotation);
        // int instanceID = characterInstance.GetInstanceID(); // Remove diagnostic variable
        // Debug.Log($"[Spawner] Instantiated {characterInstance.name} (Instance ID: {instanceID}) for Client ID {playerData.ClientId}"); // Remove diagnostic log

        NetworkObject networkObject = characterInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            // Debug.Log($"[Spawner] Attempting to spawn NetworkObject (NetID: {networkObject.NetworkObjectId}, InstanceID: {instanceID}) for Client ID {playerData.ClientId}. IsSpawned: {networkObject.IsSpawned}, IsOwner: {networkObject.IsOwner}"); // Remove diagnostic log
            // Remove try-catch block
            networkObject.SpawnWithOwnership(playerData.ClientId); 
            // Debug.Log($"[Spawner] Successfully spawned NetworkObject (NetID: {networkObject.NetworkObjectId}, InstanceID: {instanceID}) for Client ID {playerData.ClientId}. IsSpawned: {networkObject.IsSpawned}"); // Remove diagnostic log
        }
        else
        {
            Debug.LogError($"[Spawner] Instantiated character {characterInstance.name} is missing NetworkObject component!"); // Keep essential error log
            Destroy(characterInstance);
        }
    }

    private GameObject GetPrefabByName(string characterName)
    {
        if (characterPrefabDict.TryGetValue(characterName, out GameObject prefab))
        {
            return prefab;
        }
        else
        {
            Debug.LogError($"No prefab found for character name: {characterName}"); // Keep essential error log
            return null;
        }
    }
} 