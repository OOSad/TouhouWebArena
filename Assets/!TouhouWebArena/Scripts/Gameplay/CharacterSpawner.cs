using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // Required for Dictionary
using System.Collections; // Required for Coroutine

/// <summary>
/// [Server Only] Responsible for spawning the player character prefabs into the scene.
/// Reads selected character information from <see cref="PlayerDataManager"/>,
/// finds the corresponding prefab using a configurable mapping, and instantiates/
/// spawns the character <see cref="NetworkObject"/> as a player-owned object at the designated spawn point.
/// Includes a delay before spawning to allow for initialization.
/// </summary>
public class CharacterSpawner : NetworkBehaviour
{
    /// <summary>
    /// Struct used in the Inspector to link a character name string to its prefab.
    /// </summary>
    [System.Serializable]
    public struct CharacterPrefabMapping
    {
        /// <summary>The string identifier for the character (e.g., "Reimu", "Marisa"). Must match values used elsewhere.</summary>
        public string characterName;
        /// <summary>The GameObject prefab for this character.</summary>
        public GameObject characterPrefab;
    }

    [Header("Configuration")]
    [Tooltip("List mapping character names to their prefabs. Configure in the Inspector.")]
    [SerializeField] private List<CharacterPrefabMapping> characterPrefabs;
    [Tooltip("The Transform defining the spawn position and rotation for Player 1.")]
    [SerializeField] private Transform player1SpawnPoint;
    [Tooltip("The Transform defining the spawn position and rotation for Player 2.")]
    [SerializeField] private Transform player2SpawnPoint;

    /// <summary>Dictionary created from <see cref="characterPrefabs"/> for efficient prefab lookup by name.</summary>
    private Dictionary<string, GameObject> characterPrefabDict;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Converts the <see cref="characterPrefabs"/> list into the <see cref="characterPrefabDict"/> dictionary.
    /// Includes validation for invalid mappings.
    /// </summary>
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
                Debug.LogError("CharacterSpawner: Invalid CharacterPrefabMapping detected (missing name or prefab). Please check configuration in Inspector.", this);
            }
        }
    }

    /// <summary>
    /// Called when the network object is spawned.
    /// If running on the server, starts the <see cref="DelayedSpawnCharacters"/> coroutine.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Only the server should spawn characters

        // Instead of spawning immediately, start a short delay coroutine
        StartCoroutine(DelayedSpawnCharacters());
    }

    /// <summary>
    /// [Server Only] Coroutine that waits for a short delay before calling <see cref="SpawnCharacters"/>.
    /// This helps ensure other systems (like PlayerDataManager) are ready.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator DelayedSpawnCharacters()
    {
        yield return new WaitForSeconds(1.5f); // Increased delay to 1.5 second
        
        SpawnCharacters();
    }

    /// <summary>
    /// [Server Only] Retrieves player data for both players from <see cref="PlayerDataManager"/>
    /// and calls <see cref="SpawnPlayerCharacter"/> for each valid player.
    /// </summary>
    private void SpawnCharacters()
    {
        // Use top-level PlayerData struct
        PlayerData? player1Data = PlayerDataManager.Instance.GetPlayer1Data();
        PlayerData? player2Data = PlayerDataManager.Instance.GetPlayer2Data();

        if (player1Data.HasValue)
        {
            SpawnPlayerCharacter(player1Data.Value, player1SpawnPoint.position, player1SpawnPoint.rotation);
        }
        else
        {
            Debug.LogError("CharacterSpawner: Could not retrieve Player 1 data from PlayerDataManager.", this);
        }

        if (player2Data.HasValue)
        {
            SpawnPlayerCharacter(player2Data.Value, player2SpawnPoint.position, player2SpawnPoint.rotation);
        }
        else
        {
            Debug.LogError("CharacterSpawner: Could not retrieve Player 2 data from PlayerDataManager.", this);
        }
    }

    /// <summary>
    /// [Server Only] Spawns a specific character prefab for a given player.
    /// Looks up the prefab using <see cref="GetPrefabByName"/>, instantiates it at the specified position/rotation,
    /// retrieves its <see cref="NetworkObject"/>, and spawns it as a player-owned object.
    /// Includes several error checks during the process.
    /// </summary>
    /// <param name="playerData">The <see cref="PlayerData"/> for the player to spawn.</param>
    /// <param name="position">The world position where the character should be spawned.</param>
    /// <param name="rotation">The world rotation the character should have when spawned.</param>
    private void SpawnPlayerCharacter(PlayerData playerData, Vector3 position, Quaternion rotation)
    {
        string characterName = playerData.SelectedCharacter.ToString();
        GameObject prefab = GetPrefabByName(characterName);
        if (prefab == null)
        {
            Debug.LogError($"[Spawner] Prefab lookup FAILED for character: {characterName}. Aborting spawn.");
            return;
        }

        GameObject characterInstance = Instantiate(prefab, position, rotation);
        if (characterInstance == null)
        {
            Debug.LogError($"[Spawner] Instantiate FAILED for prefab '{prefab.name}'. Aborting spawn.");
            return;
        }

        NetworkObject networkObject = characterInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.SpawnAsPlayerObject(playerData.ClientId);
        }
        else
        {
            Debug.LogError($"[CharacterSpawner] Instantiated character {characterInstance.name} is missing NetworkObject component! Destroying instance.", this);
            Destroy(characterInstance);
        }
    }

    /// <summary>
    /// Looks up a character prefab in the <see cref="characterPrefabDict"/> based on the character name.
    /// </summary>
    /// <param name="characterName">The string name of the character.</param>
    /// <returns>The associated GameObject prefab if found, otherwise null.</returns>
    private GameObject GetPrefabByName(string characterName)
    {
        if (characterPrefabDict.TryGetValue(characterName, out GameObject prefab))
        {
            return prefab;
        }
        else
        {
            Debug.LogError($"CharacterSpawner: No prefab found for character name: '{characterName}'. Check Character Prefab Mappings.", this);
            return null;
        }
    }
} 