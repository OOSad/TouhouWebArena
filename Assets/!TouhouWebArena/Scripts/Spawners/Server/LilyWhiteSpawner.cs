using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class LilyWhiteSpawner : NetworkBehaviour
{
    public static LilyWhiteSpawner Instance { get; private set; }

    [Header("Timing Settings")]
    public float initialDelay = 50.0f;
    public float repeatInterval = 30.0f;

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
        yield return new WaitForSeconds(initialDelay);

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
            yield return new WaitForSeconds(repeatInterval);
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