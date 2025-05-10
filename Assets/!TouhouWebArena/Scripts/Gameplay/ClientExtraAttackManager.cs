using UnityEngine;
using Unity.Netcode;

// Forward declare PlayerRole if it's in the global namespace or a specific TouhouWebArena namespace
// using TouhouWebArena; // Assuming PlayerRole is here or global

// Define PlayAreaBounds struct if not globally available
public struct PlayAreaBounds // Or use PlayAreaDef.PlayAreaBounds if that was intended and exists
{
    public Vector2 min;
    public Vector2 max;
}

public class ClientExtraAttackManager : NetworkBehaviour // Inherit from NetworkBehaviour for ClientRpc
{
    public static ClientExtraAttackManager Instance { get; private set; }

    [Header("Extra Attack Prefabs (Assign existing prefabs here)")]
    [Tooltip("Assign the 'ReimuExtraAttackOrb.prefab' here. It should have ReimuExtraAttackOrb_Client.cs script.")]
    [SerializeField] private GameObject reimuExtraAttackPrefab; 
    [Tooltip("Assign the 'MarisaExtraAttackEarthlightRay.prefab' here. It should have MarisaExtraAttackLaser_Client.cs script.")]
    [SerializeField] private GameObject marisaExtraAttackPrefab;

    [Header("Marisa Laser Specific Spawn Anchors")]
    [Tooltip("Assign the Transform that marks the bottom-center spawn point for Marisa's laser when targeting Player 1.")]
    [SerializeField] private Transform marisaLaserSpawnAnchorP1;
    [Tooltip("Assign the Transform that marks the bottom-center spawn point for Marisa's laser when targeting Player 2.")]
    [SerializeField] private Transform marisaLaserSpawnAnchorP2;
    [Tooltip("The horizontal random spread (total width) for Marisa's laser spawn point around its anchor.")]
    [SerializeField] private float marisaLaserXSpread = 0.5f;

    [Header("Reimu Orb Specific Parameters (for sync)")]
    [Tooltip("Min initial sideways force for Reimu's Orb. Should match prefab.")]
    [SerializeField] private float reimuOrbInitialSidewaysForceMin = 2f;
    [Tooltip("Max initial sideways force for Reimu's Orb. Should match prefab.")]
    [SerializeField] private float reimuOrbInitialSidewaysForceMax = 4f;

    [Header("Marisa Laser Specific Parameters (for sync)")]
    [Tooltip("Max tilt angle for Marisa's Laser. Should match prefab.")]
    [SerializeField] private float marisaLaserMaxTiltAngle = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {Instance = this;
        }
    }

    // Called by ClientFairyHealth when a trigger fairy is killed by any player on this client
    public void OnTriggerFairyKilled(ulong attackerOwnerClientId)
    {
        if (!NetworkManager.Singleton.IsClient) return;

        PlayerData? attackerPlayerDataNullable = PlayerDataManager.Instance.GetPlayerData(attackerOwnerClientId);
        if (!attackerPlayerDataNullable.HasValue)
        {
            Debug.LogWarning($"[ClientExtraAttackManager] Could not find PlayerData for attacker ClientId: {attackerOwnerClientId}");
            return;
        }
        PlayerData attackerPlayerData = attackerPlayerDataNullable.Value;

        string killerCharacterName = attackerPlayerData.SelectedCharacter.ToString();
        PlayerRole killerPlayerRole = attackerPlayerData.Role;

        Debug.Log($"[ClientExtraAttackManager] Trigger fairy killed by {killerCharacterName} (Role: {killerPlayerRole}, ClientId: {attackerOwnerClientId}) on this client's simulation.");

        // If the local player *is* the attacker, generate random values and inform the server.
        // All clients (including this one) will spawn the attack when the Relay RPC is received.
        if (attackerOwnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            if (PlayerExtraAttackRelay.LocalInstance != null)
            {
                // Determine target player role to calculate bounds correctly for random value generation
                PlayerRole targetPlayerRole = (killerPlayerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                Transform targetSpawnCenterTransform = SpawnAreaManager.Instance.GetSpawnCenterForTargetedPlayer(targetPlayerRole);
                Vector2 spawnZoneDimensions = SpawnAreaManager.Instance.GetSpawnZoneDimensions();
                PlayAreaBounds targetPlayAreaBounds = new PlayAreaBounds
                {
                    min = new Vector2(targetSpawnCenterTransform.position.x - spawnZoneDimensions.x / 2, targetSpawnCenterTransform.position.y - spawnZoneDimensions.y / 2),
                    max = new Vector2(targetSpawnCenterTransform.position.x + spawnZoneDimensions.x / 2, targetSpawnCenterTransform.position.y + spawnZoneDimensions.y / 2)
                };

                // Generate random values for Reimu's attack
                float reimuSpawnX = Random.Range(targetPlayAreaBounds.min.x + 0.5f, targetPlayAreaBounds.max.x - 0.5f);
                float reimuSpawnY = targetPlayAreaBounds.max.y - Random.Range(0.5f, 1.5f);
                float reimuSidewaysForce = Random.Range(reimuOrbInitialSidewaysForceMin, reimuOrbInitialSidewaysForceMax);
                if (Random.value < 0.5f) reimuSidewaysForce *= -1;

                // Generate random values for Marisa's attack
                float marisaSpawnXOffset = Random.Range(-marisaLaserXSpread / 2f, marisaLaserXSpread / 2f);
                float marisaTiltAngle = 0f;
                if (Random.value < 0.3f)
                {
                    marisaTiltAngle = Random.Range(-marisaLaserMaxTiltAngle, marisaLaserMaxTiltAngle);
                }

                Debug.Log($"[ClientExtraAttackManager] Local player {NetworkManager.Singleton.LocalClientId} ({killerCharacterName}) killed trigger. Informing server with generated params.");
                PlayerExtraAttackRelay.LocalInstance.InformServerOfExtraAttackTriggerServerRpc(
                    killerCharacterName, 
                    killerPlayerRole, 
                    attackerOwnerClientId,
                    reimuSpawnX,
                    reimuSpawnY,
                    reimuSidewaysForce,
                    marisaSpawnXOffset,
                    marisaTiltAngle
                );
            }
            else
            {
                Debug.LogError("[ClientExtraAttackManager] PlayerExtraAttackRelay.LocalInstance is null. Cannot inform server.");
            }
        }
        // The local spawn that was here previously: SpawnExtraAttack(killerCharacterName, killerPlayerRole, attackerOwnerClientId);
        // is REMOVED. All spawning now happens via RelayExtraAttackToClientsClientRpc to ensure synchronized parameters.
    }

    [ClientRpc]
    public void RelayExtraAttackToClientsClientRpc(string characterName, PlayerRole attackerPlayerRole, ulong originalAttackerClientId,
                                                 float pReimuSpawnX, float pReimuSpawnY, float pReimuSidewaysForce,
                                                 float pMarisaSpawnXOffset, float pMarisaTiltAngle)
    {
        Debug.Log($"[ClientExtraAttackManager] Received RelayExtraAttackToClientsClientRpc. Killer: {characterName} ({attackerPlayerRole}), Original Attacker CID: {originalAttackerClientId}, My CID: {NetworkManager.Singleton.LocalClientId}. Using provided params.");
        
        SpawnExtraAttackInternal(characterName, attackerPlayerRole, originalAttackerClientId, 
                                 pReimuSpawnX, pReimuSpawnY, pReimuSidewaysForce, 
                                 pMarisaSpawnXOffset, pMarisaTiltAngle);
    }

    private void SpawnExtraAttackInternal(string characterName, PlayerRole attackerPlayerRole, ulong actualAttackerClientId,
                                          float pReimuSpawnX, float pReimuSpawnY, float pReimuSidewaysForce,
                                          float pMarisaSpawnXOffset, float pMarisaTiltAngle)
    {
        PlayerRole targetPlayerRole = (attackerPlayerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
        Transform targetSpawnCenterTransform = SpawnAreaManager.Instance.GetSpawnCenterForTargetedPlayer(targetPlayerRole);
        Vector2 spawnZoneDimensions = SpawnAreaManager.Instance.GetSpawnZoneDimensions();
        PlayAreaBounds targetPlayAreaBounds = new PlayAreaBounds // This is still useful for clamping or other logic
        {
            min = new Vector2(targetSpawnCenterTransform.position.x - spawnZoneDimensions.x / 2, targetSpawnCenterTransform.position.y - spawnZoneDimensions.y / 2),
            max = new Vector2(targetSpawnCenterTransform.position.x + spawnZoneDimensions.x / 2, targetSpawnCenterTransform.position.y + spawnZoneDimensions.y / 2)
        };

        if (targetSpawnCenterTransform == null) { Debug.LogError($"[CEA MGR] TargetSpawnCenterTransform null for {targetPlayerRole}"); return; }

        Debug.Log($"[ClientExtraAttackManager] Spawning INTERNAL EA for char '{characterName}' targeting {targetPlayerRole}. Original attacker {attackerPlayerRole} (CID: {actualAttackerClientId}).");

        if (characterName == "HakureiReimu")
        {
            if (reimuExtraAttackPrefab == null) { Debug.LogError("Reimu Extra Attack Prefab not assigned!"); return; }
            SpawnReimuExtraAttackInternal(targetSpawnCenterTransform, actualAttackerClientId, pReimuSpawnX, pReimuSpawnY, pReimuSidewaysForce);
        }
        else if (characterName == "KirisameMarisa")
        {
            if (marisaExtraAttackPrefab == null) { Debug.LogError("Marisa Extra Attack Prefab not assigned!"); return; }
            Transform laserSpecificSpawnAnchor = (targetPlayerRole == PlayerRole.Player1) ? marisaLaserSpawnAnchorP1 : marisaLaserSpawnAnchorP2;
            if (laserSpecificSpawnAnchor == null)
            {
                Debug.LogError($"[CEA MGR] Marisa laser spawn anchor for {targetPlayerRole} is not assigned!"); return;
            }
            SpawnMarisaExtraAttackInternal(laserSpecificSpawnAnchor, targetPlayAreaBounds, actualAttackerClientId, pMarisaSpawnXOffset, pMarisaTiltAngle);
        }
        else
        {
            Debug.LogWarning($"[ClientExtraAttackManager] Unknown character name for extra attack: {characterName}");
        }
    }

    private void SpawnReimuExtraAttackInternal(Transform targetSpawnAreaAnchor, ulong attackerClientId, 
                                               float spawnX, float spawnY, float sidewaysForce)
    {
        if (reimuExtraAttackPrefab.GetComponent<PooledObjectInfo>() == null) { Debug.LogError("Reimu prefab missing PooledObjectInfo"); return; }
        string prefabId = reimuExtraAttackPrefab.GetComponent<PooledObjectInfo>().PrefabID;
        GameObject orbGO = ClientGameObjectPool.Instance.GetObject(prefabId);
        if (orbGO == null) { Debug.LogError($"[CEA MGR] Failed to get Reimu EA Orb ('{prefabId}') from pool."); return; }

        Vector3 spawnPosition = new Vector3(spawnX, spawnY, targetSpawnAreaAnchor.position.z); 
        orbGO.transform.position = spawnPosition;
        orbGO.transform.rotation = Quaternion.identity;
        orbGO.SetActive(true);

        ReimuExtraAttackOrb_Client orbScript = orbGO.GetComponent<ReimuExtraAttackOrb_Client>();
        if (orbScript != null) { orbScript.Initialize(attackerClientId, sidewaysForce); } // Use predeterminedSidewaysForce
        else { Debug.LogError("Reimu Extra Attack Orb prefab is missing ReimuExtraAttackOrb_Client script!"); }
        Debug.Log($"[CEA MGR] Spawned Reimu EA Orb INTERNAL at {spawnPosition} with force {sidewaysForce}");
    }

    private void SpawnMarisaExtraAttackInternal(Transform specificLaserSpawnAnchor, PlayAreaBounds targetPlayAreaBounds, ulong attackerClientId,
                                                float spawnXOffset, float predeterminedTiltAngle)
    {
        if (marisaExtraAttackPrefab.GetComponent<PooledObjectInfo>() == null) { Debug.LogError("Marisa prefab missing PooledObjectInfo"); return; }
        string prefabId = marisaExtraAttackPrefab.GetComponent<PooledObjectInfo>().PrefabID;
        GameObject laserGO = ClientGameObjectPool.Instance.GetObject(prefabId);
        if (laserGO == null) { Debug.LogError($"[CEA MGR] Failed to get Marisa EA Laser ('{prefabId}') from pool."); return; }

        float finalSpawnX = specificLaserSpawnAnchor.position.x + spawnXOffset;
        finalSpawnX = Mathf.Clamp(finalSpawnX, targetPlayAreaBounds.min.x, targetPlayAreaBounds.max.x); // Still clamp based on overall bounds

        Vector3 spawnPosition = new Vector3(finalSpawnX, specificLaserSpawnAnchor.position.y, specificLaserSpawnAnchor.position.z);
        laserGO.transform.position = spawnPosition;
        laserGO.SetActive(true);

        MarisaExtraAttackLaser_Client laserScript = laserGO.GetComponent<MarisaExtraAttackLaser_Client>();
        if (laserScript != null) { laserScript.Initialize(attackerClientId, targetPlayAreaBounds, predeterminedTiltAngle); }
        else { Debug.LogError("Marisa Extra Attack (EarthlightRay) prefab is missing MarisaExtraAttackLaser_Client script!"); }
        Debug.Log($"[CEA MGR] Spawned Marisa EA Laser INTERNAL at {spawnPosition}, Tilt Angle: {predeterminedTiltAngle}");
    }
} 