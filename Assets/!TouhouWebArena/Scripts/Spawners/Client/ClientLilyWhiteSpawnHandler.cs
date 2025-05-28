using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))] // Ensure AudioSource exists
public class ClientLilyWhiteSpawnHandler : NetworkBehaviour
{
    public static ClientLilyWhiteSpawnHandler Instance { get; private set; }

    public string lilyWhitePrefabID = "LilyWhite"; // Matches PooledObjectInfo PrefabID
    public AudioClip lilyWhiteSpawnSound; // Sound effect for Lily White's appearance

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        audioSource = GetComponent<AudioSource>();
    }

    [ClientRpc]
    public void SpawnLilyWhiteClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError("ClientLilyWhiteSpawnHandler: ClientGameObjectPool.Instance is null.");
            return;
        }

        // Define spawn positions for the two playfields
        float spawnXPlayer1 = -4.5f;
        float spawnXPlayer2 = 4.5f;

        // Spawn Lily White for Player 1's playfield side
        SpawnLilyWhiteInstance(spawnXPlayer1);

        // Spawn Lily White for Player 2's playfield side
        SpawnLilyWhiteInstance(spawnXPlayer2);
    }

    private void SpawnLilyWhiteInstance(float spawnX)
    {
        GameObject lilyWhiteInstance = ClientGameObjectPool.Instance.GetObject(lilyWhitePrefabID);
        if (lilyWhiteInstance == null)
        {
            Debug.LogError($"ClientLilyWhiteSpawnHandler: Failed to get '{lilyWhitePrefabID}' from pool for X: {spawnX}.");
            return;
        }

        ClientLilyWhiteController controller = lilyWhiteInstance.GetComponent<ClientLilyWhiteController>();
        if (controller == null)
        {
            Debug.LogError($"ClientLilyWhiteSpawnHandler: '{lilyWhitePrefabID}' prefab is missing ClientLilyWhiteController component.");
            ClientGameObjectPool.Instance.ReturnObject(lilyWhiteInstance); // Return if setup is wrong
            return;
        }

        // Determine the player role based on spawnX
        PlayerRole targetedPlayerRole = PlayerRole.None;
        if (Mathf.Approximately(spawnX, -4.5f)) // Assuming -4.5f is Player 1's side
        {
            targetedPlayerRole = PlayerRole.Player1;
        }
        else if (Mathf.Approximately(spawnX, 4.5f)) // Assuming 4.5f is Player 2's side
        {
            targetedPlayerRole = PlayerRole.Player2;
        }
        else
        {
            Debug.LogWarning($"[ClientLilyWhiteSpawnHandler] Unexpected spawnX value {spawnX}. Cannot determine targeted player role.");
        }

        controller.Initialize(spawnX, targetedPlayerRole); // Pass the targeted player role

        if (lilyWhiteSpawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lilyWhiteSpawnSound);
        }
    }

    new void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
} 