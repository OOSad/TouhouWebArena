using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class LilyWhiteSpawner : NetworkBehaviour
{
    public static LilyWhiteSpawner Instance { get; private set; }

    [Header("Timing Settings")]
    [Tooltip("Time in seconds before the first Lily White spawn and after a timer reset.")]
    public float spawnInterval = 40.0f;

    private Coroutine spawnCoroutine;

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
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnLilyWhiteRoutine());
    }

    private IEnumerator SpawnLilyWhiteRoutine()
    {
        yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            if (ClientLilyWhiteSpawnHandler.Instance != null)
            {
                ClientLilyWhiteSpawnHandler.Instance.SpawnLilyWhiteClientRpc();
            }
            else
            {
                Debug.LogWarning("LilyWhiteSpawner: ClientLilyWhiteSpawnHandler.Instance is null. Cannot send ClientRpc.");
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Public method to be called by Debug Menu or other systems
    public void ForceSpawnLilyWhite()
    {
        if (!IsServer) return;

        if (ClientLilyWhiteSpawnHandler.Instance != null)
        {
            Debug.Log("ForceSpawnLilyWhite called on server. Attempting to spawn Lily White.");
            ClientLilyWhiteSpawnHandler.Instance.SpawnLilyWhiteClientRpc();
        }
        else
        {
            Debug.LogWarning("LilyWhiteSpawner (ForceSpawn): ClientLilyWhiteSpawnHandler.Instance is null. Cannot send ClientRpc.");
        }
    }

    /// <summary>
    /// Resets Lily White's spawn timer. She will wait the full spawnInterval before appearing again.
    /// Should only be called on the server.
    /// </summary>
    public void ResetSpawnTimer()
    {
        if (!IsServer)
        {
            Debug.LogWarning("ResetSpawnTimer called on client. Ignoring.");
            return;
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        Debug.Log("LilyWhiteSpawner: Spawn timer reset. Lily White will spawn in " + spawnInterval + " seconds.");
        spawnCoroutine = StartCoroutine(SpawnLilyWhiteRoutine());
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
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