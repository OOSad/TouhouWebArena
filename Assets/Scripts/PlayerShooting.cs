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
        Debug.Log($"Player {OwnerClientId} OnNetworkSpawn called. IsOwner: {IsOwner}"); // Log Spawn

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
        Debug.Log($"Player {OwnerClientId} attempting FindAndAssignSpellBar..."); // Log Start
        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>();
        Debug.Log($"Found {allSpellBars.Length} SpellBarControllers in scene."); // Log Count
        bool foundBar = false;
        foreach (SpellBarController bar in allSpellBars)
        {
            Debug.Log($"Checking bar {bar.gameObject.name} with TargetPlayerId: {bar.GetTargetPlayerId()}"); // Log Check
            // Cast OwnerClientId to int for comparison
            if (bar.GetTargetPlayerId() == (int)OwnerClientId)
            {
                spellBarController = bar;
                foundBar = true;
                Debug.Log($"Player {OwnerClientId} assigned SpellBar {bar.gameObject.name}");
                break; // Found the correct bar, no need to check others
            }
        }

        if (!foundBar)
        {
            Debug.LogWarning($"Player {OwnerClientId} could not find a SpellBarController with TargetPlayerId == {OwnerClientId}");
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

        // --- Check if Spell Bar is assigned before proceeding ---
        // We might still need the reference locally if shooting logic depends on charge state
        // but calculation is now done on server.
        // if (spellBarController == null)
        // {
        //     Debug.LogWarning($"Player {OwnerClientId} Update: spellBarController is NULL. Skipping Update logic.");
        //     return; // Skip processing if bar not found
        // }

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

        // --- State Calculation Removed (Done on Server via RPC) ---
        /*
        // --- State Update (Passive Fill) ---
        // TODO: Hook up enemy kill bonus here
        float newPassiveFill = spellBarController.currentPassiveFill.Value + spellBarController.passiveFillRate * Time.deltaTime;
        spellBarController.currentPassiveFill.Value = Mathf.Clamp(newPassiveFill, 0f, 4f); // Assuming MaxFillAmount is 4

        // --- State Update (Active Fill) ---
        float newActiveFill = spellBarController.currentActiveFill.Value;
        if (isHoldingChargeKey)
        {
            newActiveFill += spellBarController.activeChargeRate * Time.deltaTime;
            // Clamp against current passive value
            newActiveFill = Mathf.Clamp(newActiveFill, 0f, spellBarController.currentPassiveFill.Value);
        }
        else
        {
            newActiveFill = 0f; // Reset when not holding key
        }
        spellBarController.currentActiveFill.Value = newActiveFill;
        */

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
            Debug.LogError($"Server could not find NetworkClient for senderClientId {senderClientId}");
            return;
        }
        NetworkObject playerObject = networkClient.PlayerObject;
        if (playerObject == null)
        {
            Debug.LogError($"Server could not find PlayerObject for senderClientId {senderClientId}");
            return;
        }
        CharacterStats stats = playerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError($"Server could not find CharacterStats component on PlayerObject for senderClientId {senderClientId}");
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
            Debug.LogWarning($"Server received UpdateChargeStateServerRpc from Client {senderClientId} but could not find a SpellBarController with TargetPlayerId == {senderClientId}");
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
            Debug.LogError($"[Server] Could not find PlayerObject for senderClientId {ownerClientId} in PerformChargeAttackServerRpc");
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {   
             Debug.LogError($"[Server] Could not find CharacterStats for senderClientId {ownerClientId}");
             return;
        }

        // --- Get the Character-Specific Prefab from CharacterStats --- 
        GameObject chargePrefab = stats.GetChargeAttackPrefab();
        if (chargePrefab == null)
        {
            Debug.LogError($"[Server] Player {ownerClientId}: Charge Attack Prefab is not assigned in CharacterStats component!");
            return;
        }
        // -----------------------------------------------------------

        // Determine Character and Execute Attack 
        string characterName = stats.GetCharacterName(); 
        Debug.Log($"[Server] Player {ownerClientId} ({characterName}) performing charge attack with prefab {chargePrefab.name}.");

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
             Debug.LogWarning($"[Server] Player {ownerClientId} has unhandled character name ({characterName}) from CharacterStats for charge attack.");
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
                Debug.LogError($"[Server] Player {ownerClientId}: Reimu Charge Attack Prefab '{attackPrefab.name}' is missing NetworkObject component! Destroying instance.");
                Destroy(instance); 
            }
        }
    }

    // --- Helper method for Marisa's Attack ---
    private void SpawnMarisaChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        if (attackPrefab == null)
        {
            Debug.LogError($"[Server] Player {ownerClientId}: Marisa Charge Attack Prefab is null!"); // Simplified log
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
            Debug.LogError($"[Server] Player {ownerClientId}: Marisa Charge Attack Prefab '{attackPrefab.name}' is missing NetworkObject component! Destroying instance.");
            Destroy(instance);
        }
    }

    private IEnumerator BurstFireSequence()
    {
        // Ensure CharacterStats is linked
        if (characterStats == null)
        {
             Debug.LogError("PlayerShooting: CharacterStats reference is not set!");
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
            Debug.LogError($"Server could not find PlayerObject for senderClientId {senderClientId} in RequestFireServerRpc");
            return;
        }
        CharacterStats senderStats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (senderStats == null)
        {
            Debug.LogError($"Server could not find CharacterStats on PlayerObject for senderClientId {senderClientId} in RequestFireServerRpc");
            return;
        }
        // --- End Get Stats ---

        GameObject bulletToSpawn = senderStats.GetBulletPrefab();
        if (bulletToSpawn == null)
        {
            Debug.LogError($"Bullet Prefab is not assigned in CharacterStats for player {senderClientId}!");
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
        GameObject bulletInstance = Instantiate(prefab, position, rotation); // Use passed prefab
        NetworkObject bulletNetworkObject = bulletInstance.GetComponent<NetworkObject>();
        BulletMovement bulletMovement = bulletInstance.GetComponent<BulletMovement>(); // Get the script

        if (bulletNetworkObject == null || bulletMovement == null) // Check for both
        {
            Debug.LogError("Bullet Prefab is missing NetworkObject or BulletMovement script! Destroying instance.", bulletInstance); // Log context
            Destroy(bulletInstance);
            return;
        }

        // Spawn the bullet FIRST so NetworkVariables can be set
        bulletNetworkObject.Spawn(true);

        // --- Assign Owner Role (Server Only) AFTER Spawning ---
        if (PlayerDataManager.Instance != null)
        {
            // Use top-level PlayerData
            PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(ownerId); 
            if (ownerData.HasValue)
            {
                bulletMovement.OwnerRole.Value = ownerData.Value.Role;
            }
            else
            {
                Debug.LogError($"Could not find PlayerData for OwnerClientId {ownerId} to assign bullet owner! Setting to None.");
                bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager instance not found! Setting bullet OwnerRole to None.");
             bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
        }
    }

    // --- Added Awake --- 
    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            Debug.LogError("PlayerShooting could not find CharacterStats component on the same GameObject!", this);
        }
    }
    // --- End Added Awake ---
} 