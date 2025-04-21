using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Likely needed if NetworkTransform used on bullets
using System.Collections; // Required for Coroutines

[RequireComponent(typeof(NetworkObject))] // Ensure player has NetworkObject
[RequireComponent(typeof(CharacterStats))] // Ensure player has CharacterStats
public class PlayerShooting : NetworkBehaviour
{
    [Header("Prefabs")] // Added Header
    [SerializeField]
    [Tooltip("The prefab for Reimu's Charge Attack projectiles (HomingTalisman).")]
    private GameObject reimuChargeAttackPrefab; // Renamed
    [SerializeField]
    [Tooltip("The prefab for Marisa's Charge Attack projectile (IllusionLaser).")]
    private GameObject marisaChargeAttackPrefab; // Added

    [Header("Spell Bar")]
    [Tooltip("Reference to the player's spell bar controller. Assigned at runtime.")]
    private SpellBarController spellBarController;

    [Header("Input Settings")]
    [SerializeField] private KeyCode fireKey = KeyCode.Z; // Configurable fire key

    // Private variables
    private float nextFireTime = 0f; // Time when next burst can start
    private const float firePointVerticalOffset = 0.5f; // How far above the player center bullets spawn
    private Coroutine burstCoroutine; // To track if a burst is active

    // --- Added Local state for charging ---
    private bool isHoldingChargeKey = false;
    // --- End Added ---

    // --- Added Reference to CharacterStats ---
    private CharacterStats characterStats;
    // --- End Added Reference ---

    /// <summary>
    /// Called when the NetworkObject is spawned. We use this to find the correct spell bar.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Good practice to call the base method

        if (IsOwner)
        {
            FindAndAssignSpellBar();
        }
    }

    /// <summary>
    /// Finds the SpellBarController in the scene matching this player's OwnerClientId.
    /// </summary>
    private void FindAndAssignSpellBar()
    {
        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>();
        bool foundBar = false;
        foreach (SpellBarController bar in allSpellBars)
        {
            if (bar.GetTargetPlayerId() == (int)OwnerClientId)
            {
                spellBarController = bar;
                foundBar = true;
                break; // Found the correct bar, no need to check others
            }
        }

        if (!foundBar)
        {
            
        }
    }

    void Start()
    {
        // Initialize to allow firing immediately
    }

    void Update()
    {
        // Only the owner of this object should process input and update state
        if (!IsOwner) return;

        // --- Input Reading (Owner Only) ---
        isHoldingChargeKey = Input.GetKey(fireKey);
        bool justPressedFireKey = Input.GetKeyDown(fireKey);
        bool justReleasedFireKey = Input.GetKeyUp(fireKey); // Added KeyUp check

        // --- Send Input State to Server --- 
        UpdateChargeStateServerRpc(isHoldingChargeKey);

        // --- Check for Charge Attack Release --- 
        if (justReleasedFireKey)
        { 
            // Check charge level ONLY IF the spell bar reference is valid
            if (spellBarController != null && spellBarController.currentActiveFill.Value >= 1.0f)
            {
                // Trigger the charge attack
                PerformChargeAttackServerRpc(); 
            }
            // Reset burst coroutine maybe? Or does holding prevent burst anyway?
            // Let's assume holding Z prevents starting a new burst, so releasing is fine.
        }
        // --- End Charge Attack Check ---

        // --- Burst Fire Action --- 
        // Reverted condition: Use KeyDown to initiate burst sequence.
        // Holding the key won't re-trigger due to cooldown and burstCoroutine check.
        if (justPressedFireKey) 
        {   
            // Check if cooldown is met AND no burst active
            if (Time.time >= nextFireTime && burstCoroutine == null)
            {
                burstCoroutine = StartCoroutine(BurstFireSequence());

                // Calculate cooldown: total burst duration + burst cooldown delay
                float burstDuration = characterStats.GetBurstCount() > 1 ? (characterStats.GetBurstCount() - 1) * characterStats.GetTimeBetweenBurstShots() : 0f;
                nextFireTime = Time.time + burstDuration + characterStats.GetBurstCooldown(); // Use stat
            }
        }
    }

    // --- RPC to inform server of charging state ---
    [ServerRpc]
    private void UpdateChargeStateServerRpc(bool clientIsCharging, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // --- Get Sender's Player Object and Character Stats --- 
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out NetworkClient networkClient))
        {
            
            return;
        }
        NetworkObject playerObject = networkClient.PlayerObject;
        if (playerObject == null)
        {
            
            return;
        }
        CharacterStats stats = playerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {
            
            return;
        }
        // --- End Get Stats ---

        // Find the spell bar controller intended for the client who sent the RPC
        SpellBarController targetBar = null;
        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>(); // Find on server
        foreach (SpellBarController bar in allSpellBars)
        {
            if (bar.GetTargetPlayerId() == (int)senderClientId)
            {
                targetBar = bar;
                break;
            }
        }

        if (targetBar != null)
        {
            // Get rates from the character stats
            float passiveRate = stats.GetPassiveFillRate();
            float activeRate = stats.GetActiveChargeRate();

            // Tell the found bar to calculate its state based on the client's input AND character's rates
            targetBar.ServerCalculateState(clientIsCharging, Time.deltaTime, passiveRate, activeRate); // Pass rates
        }
        else
        {
            
        }
    }
    // --- End RPC ---

    // --- NEW: ServerRpc for Charge Attack --- 
    [ServerRpc]
    private void PerformChargeAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong ownerClientId = rpcParams.Receive.SenderClientId;
        
        // Get player object and stats
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {   
             
             return;
        }

        // --- Get the Character-Specific Prefab from CharacterStats --- 
        GameObject chargePrefab = stats.GetChargeAttackPrefab();
        if (chargePrefab == null)
        {
            
            return;
        }
        // -----------------------------------------------------------

        // Determine Character and Execute Attack 
        string characterName = stats.GetCharacterName(); 
        

        // Use the exact names set in the CharacterStats Inspector
        if (characterName == "HakureiReimu") 
        { 
            // Pass the specific prefab obtained from stats
            SpawnReimuChargeAttack(playerTransform, ownerClientId, chargePrefab);
        }
        else if (characterName == "KirisameMarisa")
        { 
            // Pass the specific prefab obtained from stats
            SpawnMarisaChargeAttack(playerTransform, ownerClientId, chargePrefab);
        }
        else
        {
            
        }
        
        // The active spell bar resets automatically via UpdateChargeStateServerRpc when isHoldingChargeKey becomes false.
    }
    // --- END NEW ServerRpc ---

    // --- Helper method for Reimu's Attack (Now takes prefab parameter) ---
    private void SpawnReimuChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        // No longer checks internal field, uses parameter
        // if (reimuChargeAttackPrefab == null) ... 

        // Define spawn offsets 
        Vector3 forward = playerTransform.up; 
        Vector3 right = playerTransform.right;
        float forwardOffset = 0.8f; 
        float horizontalSpread = 0.3f; 

        Vector3[] relativePositions = new Vector3[4]
        {
            forward * forwardOffset - right * horizontalSpread * 1.5f,
            forward * forwardOffset - right * horizontalSpread * 0.5f,
            forward * forwardOffset + right * horizontalSpread * 0.5f,
            forward * forwardOffset + right * horizontalSpread * 1.5f
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = playerTransform.position + relativePositions[i];
            GameObject instance = Instantiate(attackPrefab, spawnPos, playerTransform.rotation); 
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.SpawnWithOwnership(ownerClientId); 
            }
            else
            {
                Destroy(instance); 
            }
        }
    }

    // --- Helper method for Marisa's Attack ---
    private void SpawnMarisaChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        if (attackPrefab == null)
        {
            return;
        }

        // Calculate the STARTING position slightly in front
        float forwardOffset = 0.5f; // Adjust as needed
        Vector3 spawnPos = playerTransform.position + playerTransform.up * forwardOffset;
        // Removed Max check as pivot handles origin now?
        // spawnPos.y = Mathf.Max(spawnPos.y, playerTransform.position.y);

        // --- Instantiate laser at the calculated START position --- 
        // Assumes prefab's pivot is at the bottom and sprite/collider are pre-sized
        GameObject instance = Instantiate(attackPrefab, spawnPos, Quaternion.identity);
        
        // Spawn
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnWithOwnership(ownerClientId);
        }
        else
        {
            Destroy(instance);
            return;
        }
    }

    private IEnumerator BurstFireSequence()
    {
        // Ensure CharacterStats is linked
        if (characterStats == null)
        {
             yield break;
        }

        // Use stats from CharacterStats component
        for (int i = 0; i < characterStats.GetBurstCount(); i++)
        {
            // Tell the server to fire a bullet pair
            RequestFireServerRpc();

            // Wait before the next shot in the burst (unless it's the last one)
            if (i < characterStats.GetBurstCount() - 1)
            {
                yield return new WaitForSeconds(characterStats.GetTimeBetweenBurstShots());
            }
        }
        burstCoroutine = null; // Allow next burst after cooldown
    }

    [ServerRpc]
    private void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // --- Get Sender's Player Object and Character Stats (as done in other RPC) --- 
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            
            return;
        }
        CharacterStats senderStats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (senderStats == null)
        {
            
            return;
        }
        // --- End Get Stats ---

        GameObject bulletToSpawn = senderStats.GetBulletPrefab();
        if (bulletToSpawn == null)
        {
            
            return;
        }

        // Calculate the center spawn point above the player
        Transform playerTransform = networkClient.PlayerObject.transform; // Use the player object's transform
        Vector3 centerSpawnPoint = playerTransform.position + playerTransform.up * firePointVerticalOffset; 
        Quaternion spawnRotation = playerTransform.rotation;

        // Calculate left and right offset vectors based on player's right direction
        float spread = senderStats.GetBulletSpread(); // Use spread from stats
        Vector3 rightOffset = playerTransform.right * (spread / 2f);
        Vector3 leftOffset = -rightOffset;

        // Calculate final positions for the bullet pair
        Vector3 spawnPositionLeft = centerSpawnPoint + leftOffset;
        Vector3 spawnPositionRight = centerSpawnPoint + rightOffset;

        // Spawn the left bullet
        SpawnSingleBullet(bulletToSpawn, spawnPositionLeft, spawnRotation, senderClientId); // Pass prefab & senderId

        // Spawn the right bullet
        SpawnSingleBullet(bulletToSpawn, spawnPositionRight, spawnRotation, senderClientId); // Pass prefab & senderId
    }

    // Helper method to spawn one bullet
    private void SpawnSingleBullet(GameObject prefab, Vector3 position, Quaternion rotation, ulong ownerId) // Added prefab & ownerId parameters
    {
        // --- Get Prefab ID --- 
        PoolableObjectIdentity identity = prefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
             Debug.LogError($"Prefab '{prefab.name}' is missing PoolableObjectIdentity or PrefabID. Cannot get from pool.", prefab);
             return;
        }
        string prefabID = identity.PrefabID;

        // --- Get object from pool using PrefabID --- 
        NetworkObject bulletNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);

        if (bulletNetworkObject == null)
        {
            Debug.LogError($"Failed to get object with ID '{prefabID}' from NetworkObjectPool.", this);
            return;
        }

        // --- Position, Activate, and Get Components --- 
        bulletNetworkObject.transform.position = position;
        bulletNetworkObject.transform.rotation = rotation;
        bulletNetworkObject.gameObject.SetActive(true); // Activate the object from pool

        // We already have the NetworkObject, get the other required component
        BulletMovement bulletMovement = bulletNetworkObject.GetComponent<BulletMovement>(); 

        // Check if BulletMovement component exists (should always be there if prefab is correct)
        if (bulletMovement == null) 
        {
            Debug.LogError($"Pooled object '{prefab.name}' is missing the BulletMovement component! Returning to pool.", bulletNetworkObject.gameObject);
            // Return the invalid object to the pool immediately without spawning
            NetworkObjectPool.Instance.ReturnNetworkObject(bulletNetworkObject);
            return;
        }
        
        // --- Spawn and Assign Owner --- 
        bulletNetworkObject.Spawn(true); // Spawn first...

        // ...then set parent AFTER spawning.
        if (NetworkObjectPool.Instance != null)
        {
             bulletNetworkObject.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: false); // Use worldPositionStays = false AFTER setting position/rotation
        }
        // else // Log removed for brevity, was logged before activation
        // {
        //      Debug.LogError("NetworkObjectPool instance is null, cannot set parent for pooled object!", this);
        // }

        // Assign Owner Role (Server Only) AFTER Spawning 
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(ownerId); 
            if (ownerData.HasValue)
            {
                bulletMovement.OwnerRole.Value = ownerData.Value.Role;
            }
            else
            {
                Debug.LogWarning($"Could not find PlayerData for ownerId {ownerId} when spawning bullet.", this);
                bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager instance not found! Cannot assign OwnerRole to bullet.", this);
             bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
        }
    }

    // --- Added Awake --- 
    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
             enabled = false;
        } else {
             // Initialize nextFireTime based on stats to prevent immediate AI firing if cooldown is high
             nextFireTime = Time.time; // Or potentially slightly delayed if needed
        }
    }
    // --- End Added Awake ---

    // --- Added: Method for AI to trigger shooting ---
    /// <summary>
    /// Initiates a standard burst fire sequence if the cooldown allows.
    /// Should only be called by the owner client's AI Controller.
    /// </summary>
    public void StartAIShot()
    {
        if (!IsOwner) return; // AI Control should be local? Or Server?
        
        // Check if cooldown is met AND no burst active
        if (Time.time >= nextFireTime && burstCoroutine == null)
        {
            burstCoroutine = StartCoroutine(BurstFireSequence());

            // Calculate cooldown: total burst duration + burst cooldown delay
            float burstDuration = characterStats.GetBurstCount() > 1 ? (characterStats.GetBurstCount() - 1) * characterStats.GetTimeBetweenBurstShots() : 0f;
            nextFireTime = Time.time + burstDuration + characterStats.GetBurstCooldown(); // Use stat
        }
        else
        {
            
        }
    }
    // --- End Added Method ---
} 