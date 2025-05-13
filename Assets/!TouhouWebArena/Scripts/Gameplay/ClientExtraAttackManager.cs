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
    
    // This is now primarily used by the server if it needs a reference spread for its own calculations,
    // but the primary width will come from MarisaExtraAttackSpawner.
    [Tooltip("Reference horizontal random spread (total width) for Marisa's laser. Server might use its own source of truth from MarisaExtraAttackSpawner.")]
    [SerializeField] public float marisaLaserXSpread = 0.5f; // Made public for server access if needed, though server should use its own spawner's width

    [Header("Reimu Orb Specific Parameters (for sync)")]
    [Tooltip("Min initial sideways force for Reimu's Orb. Should match prefab.")]
    [SerializeField] public float reimuOrbInitialSidewaysForceMin = 2f; // Made public
    [Tooltip("Max initial sideways force for Reimu's Orb. Should match prefab.")]
    [SerializeField] public float reimuOrbInitialSidewaysForceMax = 4f; // Made public

    [Header("Marisa Laser Specific Parameters (for sync)")]
    [Tooltip("Max tilt angle for Marisa's Laser. Should match prefab.")]
    [SerializeField] public float marisaLaserMaxTiltAngle = 10f; // Made public

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
                float reimuSpawnY = targetPlayAreaBounds.max.y - Random.Range(0.5f, 1.5f); // Spawn near top of target area
                float reimuSidewaysForce = Random.Range(reimuOrbInitialSidewaysForceMin, reimuOrbInitialSidewaysForceMax);
                if (Random.value < 0.5f) reimuSidewaysForce *= -1;

                // Generate random values for Marisa's attack
                // The marisaSpawnXOffset is an OFFSET from the target's play area center.
                float marisaSpawnXOffset = Random.Range(-marisaLaserXSpread / 2f, marisaLaserXSpread / 2f);
                float marisaTiltAngle = 0f;
                if (Random.value < 0.3f) // 30% chance of tilt
                {
                    marisaTiltAngle = Random.Range(-marisaLaserMaxTiltAngle, marisaLaserMaxTiltAngle);
                }

                // Debug.Log($"[ClientExtraAttackManager] Local player {NetworkManager.Singleton.LocalClientId} ({killerCharacterName}) killed trigger. Informing server with generated params.");
                
                PlayerExtraAttackRelay.LocalInstance.InformServerOfExtraAttackTriggerServerRpc(
                    killerCharacterName, 
                    killerPlayerRole, 
                    attackerOwnerClientId,
                    reimuSpawnX,          // Reimu's absolute spawn X
                    reimuSpawnY,          // Reimu's absolute spawn Y
                    reimuSidewaysForce,   // Reimu's sideways force
                    marisaSpawnXOffset,   // Marisa's X *OFFSET*
                    marisaTiltAngle       // Marisa's tilt angle
                );
            }
            else
            {
                Debug.LogError("[ClientExtraAttackManager] PlayerExtraAttackRelay.LocalInstance is null. Cannot inform server.");
            }
        }
    }

    [ClientRpc]
    public void RelayExtraAttackToClientsClientRpc(string characterName, PlayerRole attackerPlayerRole, ulong originalAttackerClientId,
                                                 float pReimuSpawnX, float pReimuSpawnY, float pReimuSidewaysForce,
                                                 float pMarisaSpawnXOffset, float pMarisaTiltAngle) // Parameter is Marisa's X OFFSET
    {
        // Debug.Log($"[ClientExtraAttackManager] Received RelayExtraAttackToClientsClientRpc. Killer: {characterName} ({attackerPlayerRole}), Original Attacker CID: {originalAttackerClientId}, My CID: {NetworkManager.Singleton.LocalClientId}. Using provided params.");
        
        SpawnExtraAttackInternal(characterName, attackerPlayerRole, originalAttackerClientId, 
                                 pReimuSpawnX, pReimuSpawnY, pReimuSidewaysForce, 
                                 pMarisaSpawnXOffset, pMarisaTiltAngle); // Pass Marisa's X OFFSET
    }

    private void SpawnExtraAttackInternal(string characterName, PlayerRole attackerPlayerRole, ulong actualAttackerClientId,
                                          float pReimuSpawnX, float pReimuSpawnY, float pReimuSidewaysForce,
                                          float pMarisaSpawnXOffset, float pMarisaTiltAngle) // Parameter is Marisa's X OFFSET
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

        // Debug.Log($"[ClientExtraAttackManager] Spawning INTERNAL EA for char '{characterName}' targeting {targetPlayerRole}. Original attacker {attackerPlayerRole} (CID: {actualAttackerClientId}).");

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
            // Pass pMarisaSpawnXOffset directly
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
        // Debug.Log($"[CEA MGR] Spawned Reimu EA Orb INTERNAL at {spawnPosition} with force {sidewaysForce}");
    }

    private void SpawnMarisaExtraAttackInternal(Transform specificLaserSpawnAnchor, PlayAreaBounds targetPlayAreaBounds, ulong attackerClientId,
                                                float spawnXOffset, float predeterminedTiltAngle) // Parameter is Marisa's X OFFSET
    {
        if (marisaExtraAttackPrefab.GetComponent<PooledObjectInfo>() == null) { Debug.LogError("Marisa prefab missing PooledObjectInfo"); return; }
        string prefabId = marisaExtraAttackPrefab.GetComponent<PooledObjectInfo>().PrefabID;
        GameObject laserGO = ClientGameObjectPool.Instance.GetObject(prefabId);
        if (laserGO == null) { Debug.LogError($"[CEA MGR] Failed to get Marisa EA Laser ('{prefabId}') from pool."); return; }

        // Calculate the center of the target's play area for the X coordinate
        float targetPlayAreaCenterX = (targetPlayAreaBounds.min.x + targetPlayAreaBounds.max.x) / 2f;
        
        // Apply the pre-calculated random offset to this center
        float finalSpawnX = targetPlayAreaCenterX + spawnXOffset; // spawnXOffset is pMarisaSpawnXOffset

        // Ensure the final X position is still clamped within the target's overall play area boundaries
        // (targetPlayAreaBounds here is the general one from SpawnAreaManager for the opponent)
        finalSpawnX = Mathf.Clamp(finalSpawnX, targetPlayAreaBounds.min.x, targetPlayAreaBounds.max.x);

        // Use the Y and Z from the specificLaserSpawnAnchor (marisaLaserSpawnAnchorP1 or P2 from this script)
        Vector3 spawnPosition = new Vector3(finalSpawnX, specificLaserSpawnAnchor.position.y, specificLaserSpawnAnchor.position.z);
        laserGO.transform.position = spawnPosition;
        laserGO.SetActive(true);

        MarisaExtraAttackLaser_Client laserScript = laserGO.GetComponent<MarisaExtraAttackLaser_Client>();
        if (laserScript != null) { laserScript.Initialize(attackerClientId, targetPlayAreaBounds, predeterminedTiltAngle); }
        else { Debug.LogError("Marisa Extra Attack (EarthlightRay) prefab is missing MarisaExtraAttackLaser_Client script!"); }
        // Debug.Log($"[CEA MGR] Spawned Marisa EA Laser INTERNAL at {spawnPosition}, Tilt Angle: {predeterminedTiltAngle}");
    }
} 