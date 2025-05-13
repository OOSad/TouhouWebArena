using UnityEngine;
using Unity.Netcode;
using Unity.Collections; // Required for FixedString64Bytes

// Potentially using TouhouWebArena.Gameplay; // If AttackType enum or spawning logic is here

/// <summary>
/// Component placed on the Player prefab to handle relaying attack triggers (like fairy kills)
/// from the client to the server, and receiving attack commands targeted at this player.
/// </summary>
public class PlayerAttackRelay : NetworkBehaviour
{
    // Static reference to the locally owned instance
    public static PlayerAttackRelay LocalInstance { get; private set; }

    private Transform _resolvedOpponentSpawnCenter; // Changed from RectTransform
    private Vector2 _opponentSpawnDimensions;    // NEW: Store dimensions

    // --- Corrected Prefab IDs ---
    private const string STAGE_SMALL_BULLET_PREFAB_ID = "StageSmallBullet"; 
    private const string STAGE_LARGE_BULLET_PREFAB_ID = "StageLargeBullet";
    // --- End Corrected Prefab IDs ---

    [Tooltip("Chance (0.0 to 1.0) of spawning a large bullet instead of a small one.")]
    [SerializeField, Range(0f, 1f)] private float largeBulletChance = 0.2f;

    [Header("Retaliation Bullet Settings")]
    [Tooltip("Minimum speed for spawned stage bullets.")]
    [SerializeField] private float minStageBulletSpeed = 4f;
    [Tooltip("Maximum speed for spawned stage bullets.")]
    [SerializeField] private float maxStageBulletSpeed = 7f;
    [Tooltip("Maximum angle deviation (degrees) from straight down for spawned stage bullets.")]
    [SerializeField, Range(0f, 45f)] private float maxStageBulletAngleOffset = 15f;

    // Reference to the local bullet spawner or attack manager for this player
    // Needed to execute attacks when ReceiveOpponentAttackClientRpc is called.
    // Example: private ClientSideAttackSpawner _attackSpawner;
    private ClientGameObjectPool _clientObjectPool; // For spawning bullets locally

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Call the base method

        if (IsOwner)
        {
            LocalInstance = this;
            if (PlayerDataManager.Instance != null && SpawnAreaManager.Instance != null)
            {
                PlayerData? myPlayerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
                if (myPlayerData.HasValue)
                {
                    PlayerRole myRole = myPlayerData.Value.Role;
                    _resolvedOpponentSpawnCenter = SpawnAreaManager.Instance.GetSpawnCenterForTargetedPlayer(myRole);
                    _opponentSpawnDimensions = SpawnAreaManager.Instance.GetSpawnZoneDimensions(); 
                    // Debug.Log($"[PlayerAttackRelay {OwnerClientId} ({myRole})] OnNetworkSpawn: Resolved spawn center: {_resolvedOpponentSpawnCenter?.name}");
                }
            }
        }
        // All clients (including host/server if it's also a player) need access to the pool for spawning bullets
        _clientObjectPool = FindFirstObjectByType<ClientGameObjectPool>();
        if (_clientObjectPool == null)
        {
            Debug.LogError($"[PlayerAttackRelay {OwnerClientId}] ClientGameObjectPool not found!", this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && LocalInstance == this) LocalInstance = null;
        base.OnNetworkDespawn(); // Call the base method
    }

    // --- ServerRpc called by ClientFairyController when local player kills a fairy --- 
    [ServerRpc]
    public void ReportFairyKillServerRpc(ServerRpcParams rpcParams = default)
    {   
        ulong killerClientId = rpcParams.Receive.SenderClientId;
        PlayerRole killerRole = PlayerDataManager.Instance.GetPlayerData(killerClientId)?.Role ?? PlayerRole.None;
        // Debug.Log($"[Server PlayerAttackRelay for {killerClientId} ({killerRole})] ReportFairyKillServerRpc received.");

        ulong opponentClientId = GetOpponentClientId(killerClientId);
        if (opponentClientId == ulong.MaxValue) 
        {
            Debug.LogWarning($"[Server PlayerAttackRelay for {killerClientId}] Could not find opponent.");
            return;
        }
        PlayerRole opponentRole = PlayerDataManager.Instance.GetPlayerData(opponentClientId)?.Role ?? PlayerRole.None;
        // Debug.Log($"[Server PlayerAttackRelay for {killerClientId}] Opponent identified as Client {opponentClientId} ({opponentRole}).");

        bool spawnLargeBullet = Random.value < largeBulletChance;
        FixedString64Bytes bulletPrefabID = spawnLargeBullet ? 
            new FixedString64Bytes(STAGE_LARGE_BULLET_PREFAB_ID) : 
            new FixedString64Bytes(STAGE_SMALL_BULLET_PREFAB_ID);
        
        Vector2 normalizedSpawnPosition = new Vector2(Random.value, Random.value); 

        // --- Generate speed and direction variation ---
        float actualSpeed = Random.Range(minStageBulletSpeed, maxStageBulletSpeed);
        float angleOffset = Random.Range(-maxStageBulletAngleOffset, maxStageBulletAngleOffset);
        Quaternion rotation = Quaternion.AngleAxis(angleOffset, Vector3.forward); // Z-axis rotation for 2D
        Vector2 direction = rotation * Vector2.down; // Rotate Vector2.down
        // --- End variation generation ---

        if (EffectNetworkHandler.Instance != null)
        {
            ClientRpcParams allClientsRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds }
            };
            // Debug.Log($"[Server PlayerAttackRelay for {killerClientId} ({killerRole})] Telling EffectNetworkHandler to send RPC to ALL CLIENTS. Explicit Target for bullet: {opponentClientId} ({opponentRole}). Bullet: {bulletPrefabID}, NormPos: {normalizedSpawnPosition}, Speed: {actualSpeed}, Direction: {direction}.");
            EffectNetworkHandler.Instance.SpawnStageBulletClientRpc(opponentClientId, bulletPrefabID, normalizedSpawnPosition, actualSpeed, direction, allClientsRpcParams);
        }
        else
        {
            Debug.LogError($"[Server PlayerAttackRelay for {killerClientId}] EffectNetworkHandler is null. Cannot send RPC.");
        }
    }

    [ServerRpc]
    public void ReportSpiritKillServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong killerClientId = rpcParams.Receive.SenderClientId;
        // PlayerRole killerRole = PlayerDataManager.Instance.GetPlayerData(killerClientId)?.Role ?? PlayerRole.None;
        // Debug.Log($"[Server PlayerAttackRelay for {killerClientId} ({killerRole})] ReportSpiritKillServerRpc received.");

        ulong opponentClientId = GetOpponentClientId(killerClientId);
        if (opponentClientId == ulong.MaxValue) 
        {
            Debug.LogWarning($"[Server PlayerAttackRelay for {killerClientId}] Could not find opponent for spirit revenge spawn.");
            return;
        }
        PlayerRole opponentPlayerRole = PlayerDataManager.Instance.GetPlayerData(opponentClientId)?.Role ?? PlayerRole.None;
        if (opponentPlayerRole == PlayerRole.None)
        {
            Debug.LogWarning($"[Server PlayerAttackRelay for {killerClientId}] Opponent {opponentClientId} has no role. Cannot spawn revenge spirit.");
            return;
        }

        // Debug.Log($"[Server PlayerAttackRelay for {killerClientId}] Opponent for revenge spirit identified as Client {opponentClientId} ({opponentPlayerRole}). Calling SpiritSpawner.");
        if (SpiritSpawner.Instance != null)
        {
            SpiritSpawner.Instance.SpawnRevengeSpirit(opponentPlayerRole);
        }
        else
        {
            Debug.LogError($"[Server PlayerAttackRelay for {killerClientId}] SpiritSpawner.Instance is null. Cannot spawn revenge spirit.");
        }
    }

    // New ClientRpc specifically for receiving stage bullet spawn command
    [ClientRpc]
    public void ReceiveStageBulletSpawnClientRpc(FixedString64Bytes bulletPrefabID, Vector2 normalizedSpawnPosition, ClientRpcParams clientRpcParams = default)
    {
        // This RPC should only execute on the client targeted by the server.
        // Debug.Log($"[Client PlayerAttackRelay {OwnerClientId}] Received ReceiveStageBulletSpawnClientRpc for bullet {bulletPrefabID}, NormPos: {normalizedSpawnPosition}. Calling SpawnLocalStageBullet.");
        SpawnLocalStageBullet(bulletPrefabID, normalizedSpawnPosition);
    }

    // Renamed: Removed [ClientRpc] attribute, now a local method
    public void SpawnLocalStageBullet(FixedString64Bytes bulletPrefabID, Vector2 normalizedSpawnPosition)
    {
        // Log that the *local* method was called
        // Debug.Log($"[PlayerAttackRelay {OwnerClientId}] SpawnLocalStageBullet executing. Bullet: {bulletPrefabID}, NormPos: {normalizedSpawnPosition}");

        if (_clientObjectPool == null || _resolvedOpponentSpawnCenter == null) 
        {
            Debug.LogError($"[Client PlayerAttackRelay {OwnerClientId}] Pool ({_clientObjectPool?.name}) or SpawnCenter ({_resolvedOpponentSpawnCenter?.name}) is null. Cannot spawn stage bullet {bulletPrefabID}.");
            return;
        }

        GameObject bulletInstance = _clientObjectPool.GetObject(bulletPrefabID.ToString());
        if (bulletInstance == null) 
        {
            Debug.LogWarning($"[Client PlayerAttackRelay {OwnerClientId}] Failed to get bullet '{bulletPrefabID}' from pool.");
            return;
        }

        Vector3 spawnCenterPos = _resolvedOpponentSpawnCenter.position;
        float offsetX = (normalizedSpawnPosition.x - 0.5f) * _opponentSpawnDimensions.x;
        float offsetY = (normalizedSpawnPosition.y - 0.5f) * _opponentSpawnDimensions.y;
        Vector3 worldSpawnPos = new Vector3(spawnCenterPos.x + offsetX, spawnCenterPos.y + offsetY, spawnCenterPos.z );

        bulletInstance.transform.position = worldSpawnPos;
        bulletInstance.transform.rotation = Quaternion.identity;
        
        // --- NEW: Initialize the bullet's movement --- 
        StageSmallBulletMoverScript mover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        if (mover != null)
        {
            // Determine the role of the player this bullet belongs to (the owner of this PlayerAttackRelay)
            PlayerRole ownerRole = PlayerRole.None;
            if (PlayerDataManager.Instance != null) 
            {
                ownerRole = PlayerDataManager.Instance.GetPlayerData(OwnerClientId)?.Role ?? PlayerRole.None;
            }

            // Bullet travels straight down by default
            Vector3 initialDirection = Vector3.down; 
            // Use the bullet's own defined default speed and max lifetime
            mover.Initialize(initialDirection, mover.DefaultSpeed, mover.MaxLifetime, ownerRole);
        }
        else
        {
            Debug.LogError($"[Client PlayerAttackRelay {OwnerClientId}] Spawned bullet {bulletPrefabID} is missing StageSmallBulletMoverScript!", bulletInstance);
        }
        // --- END NEW --- 

        bulletInstance.SetActive(true);
        // Debug.Log($"[Client PlayerAttackRelay {OwnerClientId}] Successfully spawned stage bullet {bulletPrefabID} at world pos {worldSpawnPos} in area {_resolvedOpponentSpawnCenter.name}");
    }


    // --- OLD ClientRpc called by the Server ON the player who should be attacked (Kept for now) --- 
    [ClientRpc]
    public void ReceiveOpponentAttackClientRpc(AttackType type, int intensity, ClientRpcParams clientRpcParams = default)
    {   
        // Debug.Log($"Client {OwnerClientId} received OLD opponent attack: Type={type}, Intensity={intensity}");
        // _attackSpawner?.SpawnGarbageAttack(type, intensity);
    }

    private ulong GetOpponentClientId(ulong myClientId)
    {
        foreach (var clientKvp in NetworkManager.Singleton.ConnectedClients)
        {
            if (clientKvp.Key != myClientId) return clientKvp.Key; // Corrected to iterate ConnectedClients dictionary
        }
        Debug.LogWarning("[PlayerAttackRelay GetOpponentClientId] Could not find opponent for Client " + myClientId + ". Connected clients: " + NetworkManager.Singleton.ConnectedClients.Count);
        return ulong.MaxValue;
    }
}

// Example Enum - Define this properly in your project
public enum AttackType
{
    None,
    FairyGarbage_Small,
    FairyGarbage_Medium
} 