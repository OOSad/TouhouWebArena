using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Likely needed if NetworkTransform used on bullets
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Added for Dictionary
using TouhouWebArena.Spellcards; // Required for SpellcardExecutor
using TouhouWebArena.Spellcards.Behaviors; // Required for bullet behaviors

/// <summary>
/// Handles player input for shooting actions (basic shot, charge attacks, spellcards)
/// and communicates with the server to execute these actions authoritatively.
/// Also interacts with the <see cref="SpellBarController"/> to manage charge levels and spellcard costs.
/// </summary>
[RequireComponent(typeof(NetworkObject))] // Ensure player has NetworkObject
[RequireComponent(typeof(CharacterStats))] // Ensure player has CharacterStats
public class PlayerShooting : NetworkBehaviour
{
    // Note: Prefabs specific to characters (like charge attacks) are now typically fetched from CharacterStats.
    // These fields might be deprecated or serve as fallbacks if needed.
    [Header("Prefabs")]
    [SerializeField]
    [Tooltip("(Legacy/Fallback?) The prefab for Reimu's Charge Attack projectiles (HomingTalisman).")]
    private GameObject reimuChargeAttackPrefab;
    [SerializeField]
    [Tooltip("(Legacy/Fallback?) The prefab for Marisa's Charge Attack projectile (IllusionLaser).")]
    private GameObject marisaChargeAttackPrefab;

    [Header("Spell Bar")]
    [Tooltip("Reference to the player's spell bar controller. Assigned at runtime by the owning client.")]
    private SpellBarController spellBarController;

    [Header("Input Settings")]
    [Tooltip("The keyboard key used to trigger shooting actions.")]
    [SerializeField] private KeyCode fireKey = KeyCode.Z;

    // --- Timing and State --- 
    private float nextFireTime = 0f; // Tracks cooldown for basic shot bursts.
    private const float firePointVerticalOffset = 0.5f; // Vertical offset from player center for bullet spawns.
    private Coroutine burstCoroutine; // Reference to the active basic shot burst coroutine.
    private bool isHoldingChargeKey = false; // Local state tracking if the fire key is held down.

    // --- Component References --- 
    private CharacterStats characterStats; // Cached reference to the player's stats.

    // --- Server-Side Cache --- 
    // Cache of SpellBarControllers keyed by ClientId, used by the server for updates.
    private Dictionary<ulong, SpellBarController> playerSpellBars = new Dictionary<ulong, SpellBarController>();

    /// <summary>
    /// Initializes references and caches when the NetworkObject spawns.
    /// Server caches all spell bars, Owner client finds its own spell bar.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Good practice to call the base method

        // Server caches all spell bars
        if (IsServer)
        {
            ServerInitializeSpellBars();
        }

        // Owner finds its specific spell bar for local checks (like triggering spellcards)
        if (IsOwner)
        {
            FindAndAssignSpellBar(); // Keep this for owner-specific UI interaction logic
        }
    }

    // --- NEW: Server initializes spell bar cache ---
    private void ServerInitializeSpellBars()
    {
        if (!IsServer) return;

        playerSpellBars.Clear();
        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>();
        foreach (SpellBarController bar in allSpellBars)
        {
            // Use TargetPlayerId as the key, which corresponds to the expected ClientId
            playerSpellBars[(ulong)bar.GetTargetPlayerId()] = bar;
        }
    }
    // --- END NEW ---

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

    /// <summary>
    /// Main update loop.
    /// Server: Handles passive spell bar fill for all connected players.
    /// Owner Client: Reads input, sends charge state to server, requests attacks/spellcards on key release,
    /// initiates basic shot burst sequence on key press (respecting cooldown).
    /// </summary>
    void Update()
    {
        // --- Server-Side Passive Spell Bar Update ---
        if (IsServer)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients) // Use kvp to get ClientId directly
            {
                ulong clientId = kvp.Key;
                NetworkClient networkClient = kvp.Value;

                // Find the bar associated with this client ID in our cache
                if (playerSpellBars.TryGetValue(clientId, out SpellBarController bar))
                {
                    // Get the player's stats
                    if (networkClient.PlayerObject != null)
                    {
                        CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
                        if (stats != null)
                        {
                            // Apply passive fill rate
                            float passiveRate = stats.GetPassiveFillRate();
                            float newFill = bar.currentPassiveFill.Value + passiveRate * Time.deltaTime;
                            // Update the NetworkVariable (automatically syncs to clients)
                            bar.currentPassiveFill.Value = Mathf.Clamp(newFill, 0f, SpellBarController.MaxFillAmount);
                        }
                    }
                }
            }
        }
        // --- End Server-Side Update ---

        // Only the owner of this object should process input and update state
        if (!IsOwner) return;

        // --- Input Reading (Owner Only) ---
        isHoldingChargeKey = Input.GetKey(fireKey);
        bool justPressedFireKey = Input.GetKeyDown(fireKey);
        bool justReleasedFireKey = Input.GetKeyUp(fireKey); // Added KeyUp check

        // --- Send Input State to Server ---
        UpdateChargeStateServerRpc(isHoldingChargeKey);

        // --- Check for Charge Attack / Spellcard Release ---
        if (justReleasedFireKey)
        {
            // Check charge level ONLY IF the spell bar reference is valid
            if (spellBarController != null)
            {
                float chargeLevel = spellBarController.currentActiveFill.Value;
                int spellLevel = Mathf.FloorToInt(chargeLevel);

                if (spellLevel >= 2) // Levels 2, 3, 4 are spellcards
                {
                    // Trigger the Spellcard
                    RequestSpellcardServerRpc(spellLevel);
                }
                else if (spellLevel >= 1) // Level 1 is charge attack
            {
                // Trigger the charge attack
                PerformChargeAttackServerRpc();
                }
                // If spellLevel is 0, nothing happens on release
            }
            // Reset burst coroutine maybe? Or does holding prevent burst anyway?
            // Let's assume holding Z prevents starting a new burst, so releasing is fine.
        }
        // --- End Charge Attack / Spellcard Check ---

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

    /// <summary>
    /// ServerRpc called by the owning client to inform the server whether the fire key is being held.
    /// The server uses this information to update the corresponding player's active spell bar charge.
    /// </summary>
    /// <param name="clientIsCharging">True if the client is holding the fire key, false otherwise.</param>
    /// <param name="rpcParams">Standard RPC parameters.</param>
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
        // Use the server cache instead of FindObjectsOfType here
        if (playerSpellBars.TryGetValue(senderClientId, out SpellBarController targetBar))
        {
            // Get active rate from the character stats
            float activeRate = stats.GetActiveChargeRate();

            // Tell the found bar to calculate its ACTIVE state based on the client's input
            targetBar.ServerCalculateState(clientIsCharging, activeRate); // Removed deltaTime, passiveRate
        }
        else
        {

        }
    }
    // --- End RPC ---

    /// <summary>
    /// ServerRpc called by the owning client when the fire key is released with enough charge for a Charge Attack (Level 1).
    /// The server identifies the character, fetches the appropriate charge attack prefab from <see cref="CharacterStats"/>,
    /// and executes the corresponding spawn logic (e.g., <see cref="SpawnReimuChargeAttack"/> or <see cref="SpawnMarisaChargeAttack"/>).
    /// </summary>
    /// <param name="rpcParams">Standard RPC parameters.</param>
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

    /// <summary>
    /// Server-side helper to spawn Reimu's charge attack pattern.
    /// Instantiates and spawns multiple projectiles based on the provided prefab.
    /// </summary>
    /// <param name="playerTransform">The transform of the owning player.</param>
    /// <param name="ownerClientId">The NetworkClientId of the owning player.</param>
    /// <param name="attackPrefab">The specific GameObject prefab for Reimu's charge attack.</param>
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

    /// <summary>
    /// Server-side helper to spawn Marisa's charge attack (Illusion Laser).
    /// Instantiates and spawns the laser prefab.
    /// </summary>
    /// <param name="playerTransform">The transform of the owning player.</param>
    /// <param name="ownerClientId">The NetworkClientId of the owning player.</param>
    /// <param name="attackPrefab">The specific GameObject prefab for Marisa's charge attack.</param>
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

    /// <summary>
    /// Coroutine run by the owning client to handle the burst fire sequence for the basic shot.
    /// Calls <see cref="RequestFireServerRpc"/> repeatedly based on burst count and timing defined in <see cref="CharacterStats"/>.
    /// </summary>
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

    /// <summary>
    /// ServerRpc called by the owning client for each shot within a basic shot burst.
    /// Fetches the player's standard bullet prefab from <see cref="CharacterStats"/> and spawns a pair of bullets
    /// using <see cref="SpawnSingleBullet"/>.
    /// </summary>
    /// <param name="rpcParams">Standard RPC parameters.</param>
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
    /// <summary>
    /// Server-side helper method to spawn a single bullet.
    /// Gets an object from the <see cref="NetworkObjectPool"/> using the prefab's <see cref="PoolableObjectIdentity.PrefabID"/>,
    /// sets its position/rotation, activates it, spawns it on the network with ownership,
    /// parents it to the pool object, and assigns the owner's role to the <see cref="BulletMovement"/> component.
    /// </summary>
    /// <param name="prefab">The bullet prefab GameObject to spawn.</param>
    /// <param name="position">The world position to spawn the bullet at.</param>
    /// <param name="rotation">The world rotation to spawn the bullet with.</param>
    /// <param name="ownerId">The NetworkClientId of the player spawning the bullet.</param>
    private void SpawnSingleBullet(GameObject prefab, Vector3 position, Quaternion rotation, ulong ownerId) // Added prefab & ownerId parameters
    {
        // --- Get Prefab ID ---
        PoolableObjectIdentity identity = prefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {

             return;
        }
        string prefabID = identity.PrefabID;

        // --- Get object from pool using PrefabID ---
        NetworkObject bulletNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);

        if (bulletNetworkObject == null)
        {

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

                bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
            }
        }
        else
        {

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

    /// <summary>
    /// Called by an AI controller (presumably on the owner client) to initiate a basic shot burst,
    /// respecting the standard shooting cooldowns.
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

    /// <summary>
    /// ServerRpc called by the owning client when the fire key is released with enough charge for a Spellcard (Level 2, 3, or 4).
    /// Identifies sender and opponent, loads the appropriate <see cref="SpellcardData"/> resource,
    /// consumes the spell bar cost on the server, and starts the <see cref="ServerExecuteSpellcardActions"/> coroutine
    /// to handle the spellcard's execution on the opponent's side.
    /// </summary>
    /// <param name="spellLevel">The level of the spellcard being declared (2, 3, or 4).</param>
    /// <param name="rpcParams">Standard RPC parameters.</param>
    [ServerRpc]
    private void RequestSpellcardServerRpc(int spellLevel, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // 1. Get Sender Info (Player Object, CharacterStats)
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out NetworkClient senderClient) || senderClient.PlayerObject == null)
        {

            return;
        }
        NetworkObject senderPlayerObject = senderClient.PlayerObject;
        CharacterStats senderStats = senderPlayerObject.GetComponent<CharacterStats>();
        if (senderStats == null)
        {

            return;
        }
        string senderCharacterName = senderStats.GetCharacterName();
        if (senderPlayerObject == null || senderStats == null) return; // Added null check consolidation

        // 2. Find Opponent
        ulong opponentClientId = ulong.MaxValue;
        NetworkObject opponentPlayerObject = null;
        foreach (var connectedClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (connectedClient.ClientId != senderClientId)
            {
                opponentClientId = connectedClient.ClientId;
                opponentPlayerObject = connectedClient.PlayerObject;
                break; // Found the opponent
            }
        }

        if (opponentPlayerObject == null)
        {

            return;
        }
        // Don't need opponentTransform here anymore
        // Transform opponentTransform = opponentPlayerObject.transform;

        // 3. Construct Spellcard Resource Path
        string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";

        // --- Load SpellcardData ON SERVER ---
        SpellcardData spellcardData = Resources.Load<SpellcardData>(resourcePath);
        if (spellcardData == null)
        {

            return;
        }

        // --- Find Sender's Spell Bar Controller ---
        SpellBarController senderBar = null;
        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>(); // Find on server
        foreach (SpellBarController bar in allSpellBars)
        {
            if (bar.GetTargetPlayerId() == (int)senderClientId)
            {
                senderBar = bar;
                break;
            }
        }

        if (senderBar == null)
        {

             return; // Cannot proceed without the bar
        }

        // --- Consume Spell Bar Charge (Passive and Active) ---
        // Cost is based on spell level (L2=1 segment, L3=2, L4=3), converted to 0-4 scale
        float cost = (spellLevel - 1) * 1.0f; // Each level costs 1.0 on the 0-4 scale (which is 25%)
        float currentPassive = senderBar.currentPassiveFill.Value;
        float newPassiveFill = Mathf.Max(0f, currentPassive - cost); // Subtract cost, ensure non-negative

        senderBar.currentPassiveFill.Value = newPassiveFill;
        senderBar.currentActiveFill.Value = 0f; // Reset active charge
        // ----------------------------------------------------

        // 4. Determine Spellcard Origin & Capture Target Position
        Vector3 opponentCurrentPos = opponentPlayerObject.transform.position;
        Vector3 originPosition = opponentCurrentPos + new Vector3(0, 5f, 0);
        Quaternion originRotation = Quaternion.identity;

        // --- 5. Execute Spawning Logic SERVER-SIDE ---

        StartCoroutine(ServerExecuteSpellcardActions(spellcardData, originPosition, originRotation, opponentClientId, opponentCurrentPos)); // Removed senderId
    }
    // --- END Spellcard ServerRpc ---

    /// <summary>
    /// Server-side coroutine that executes the actions defined in a <see cref="SpellcardData"/> ScriptableObject.
    /// Handles delays and iterates through actions, spawning projectiles using the <see cref="NetworkObjectPool"/>
    /// and configuring their behavior via <see cref="ConfigureBulletBehavior"/>.
    /// Spellcards typically spawn on the opponent's side of the field.
    /// </summary>
    /// <param name="spellcardData">The ScriptableObject defining the spellcard pattern.</param>
    /// <param name="originPosition">The calculated origin point for the spellcard pattern (usually above the opponent).</param>
    /// <param name="originRotation">The base rotation for the spellcard pattern.</param>
    /// <param name="opponentId">The NetworkClientId of the opponent player.</param>
    /// <param name="capturedOpponentPosition">The opponent's position captured when the spellcard was initiated.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator ServerExecuteSpellcardActions(SpellcardData spellcardData, Vector3 originPosition, Quaternion originRotation, ulong opponentId, Vector3 capturedOpponentPosition) // Removed senderId
    {
        if (!IsServer) yield break;

        if (NetworkObjectPool.Instance == null) { /* ... error log ... */ yield break; }

        // --- Determine Target Side ---
        // Assuming boundaryX is 0. Modify if your stage center is different.
        // We use the captured position as a reliable indicator of the side.
        bool isTargetOnPositiveSide = (capturedOpponentPosition.x >= 0.0f);

        // ---------------------------

        foreach (SpellcardAction action in spellcardData.actions)
        {
            // Handle start delay
            if (action.startDelay > 0)
            {
                yield return new WaitForSeconds(action.startDelay); // Coroutine delay works fine on server
            }

            if (action.bulletPrefabs == null || action.bulletPrefabs.Count == 0)
            {

                 continue; // Skip this action
            }

            int prefabIndex = 0;
            Vector3 spawnPositionBase = originPosition + (Vector3)action.positionOffset;

            for (int i = 0; i < action.count; i++)
            {
                GameObject prefabToSpawn = action.bulletPrefabs[prefabIndex % action.bulletPrefabs.Count];
                if (prefabToSpawn == null) { continue; }

                // --- Get Prefab ID ---
                PoolableObjectIdentity identity = prefabToSpawn.GetComponent<PoolableObjectIdentity>();
                if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
                {

                     continue;
                }
                string prefabID = identity.PrefabID;

                Vector3 spawnPos = spawnPositionBase;
                Quaternion spawnRot = originRotation;

                // Calculate position and rotation based on formation type (Same logic as before)
                switch (action.formation)
                {
                    case FormationType.Point:
                        spawnPos = spawnPositionBase;
                        spawnRot = originRotation;
                        break;
                    case FormationType.Circle:
                        spawnPos = spawnPositionBase;
                        if (action.count > 0) {
                            float angleDegrees = i * (360f / action.count);
                            spawnRot = Quaternion.Euler(0, 0, angleDegrees + 90);
                        } else {
                            spawnRot = originRotation;
                        }
                        break;
                    case FormationType.Line:
                        float lineAngleRad = action.angle * Mathf.Deg2Rad;
                        Vector3 lineDirection = new Vector3(Mathf.Cos(lineAngleRad), Mathf.Sin(lineAngleRad), 0);
                        float totalLength = action.spacing * (action.count - 1);
                        float startOffset = -totalLength / 2f;
                        spawnPos = spawnPositionBase + lineDirection * (startOffset + i * action.spacing);
                        spawnRot = Quaternion.Euler(0, 0, action.angle + 90);
                        break;
                }

                // --- Get bullet instance from the pool ON SERVER ---
                NetworkObject bulletInstanceNO = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
                if (bulletInstanceNO == null)
                {

                    continue;
                }
                GameObject bulletInstance = bulletInstanceNO.gameObject;

                // --- Set Transform BEFORE Spawning ---
                bulletInstance.transform.position = spawnPos;
                bulletInstance.transform.rotation = spawnRot;
                bulletInstance.gameObject.SetActive(true);

                // --- Configure Behavior SERVER-SIDE ---
                ConfigureBulletBehavior(bulletInstance, action, opponentId, capturedOpponentPosition, isTargetOnPositiveSide); // Removed senderId

                // --- Spawn the NetworkObject SERVER-SIDE ---
                bulletInstanceNO.Spawn(true); // Spawn the object on the network

                // --- Set Parent AFTER Spawning ---
                 if (NetworkObjectPool.Instance != null)
                 {
                      // Parent AFTER spawning
                      bulletInstanceNO.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: false);
                 }
                 else {

                 }

                prefabIndex++;
            }
        }
    }
    // --- END Server-side Coroutine ---

    /// <summary>
    /// Server-side helper method called by <see cref="ServerExecuteSpellcardActions"/> to configure a newly spawned spellcard projectile.
    /// Disables unused movement behaviors, sets up the <see cref="NetworkBulletLifetime"/> boundary based on the target's side,
    /// and initializes the chosen movement behavior (<see cref="LinearMovement"/>, <see cref="DelayedHoming"/>, etc.) with parameters from the <see cref="SpellcardAction"/>.
    /// </summary>
    /// <param name="bulletInstance">The GameObject of the spawned projectile.</param>
    /// <param name="action">The <see cref="SpellcardAction"/> defining the projectile's properties.</param>
    /// <param name="opponentId">The NetworkClientId of the opponent player (used for homing targets).</param>
    /// <param name="capturedOpponentPosition">The opponent's captured position (used for homing targets).</param>
    /// <param name="isTargetOnPositiveSide">Whether the target opponent is on the positive X side (right side) of the playfield.</param>
    private void ConfigureBulletBehavior(GameObject bulletInstance, SpellcardAction action, ulong opponentId, Vector3 capturedOpponentPosition, bool isTargetOnPositiveSide) // Removed senderId
    {
        // Disable all potential behaviors first
        var linear = bulletInstance.GetComponent<LinearMovement>();
        var delayedHoming = bulletInstance.GetComponent<DelayedHoming>();
        var lifetime = bulletInstance.GetComponent<NetworkBulletLifetime>(); // Get Lifetime component
        if (linear) linear.enabled = false;
        if (delayedHoming) delayedHoming.enabled = false;
        // Don't disable lifetime, it should always run on the server

        // --- Configure Lifetime Boundary ---
        if (lifetime != null) {
            lifetime.keepOnPositiveSide = isTargetOnPositiveSide;
            // Removed InitializeOwner call
            // lifetime.isClearableByBomb = action.isClearableByBomb; // Keep this if using the flag
        } else {

        }

        // Enable and initialize the correct movement behavior
        switch (action.behavior)
        {
            case BehaviorType.Linear:
                if (linear != null)
                {
                    linear.enabled = true;
                    linear.Initialize(action.speed);
                }
                else
                {
                    // Missing component, do nothing
                    break; // Add break to fix fallthrough
                }
                break;

            case BehaviorType.DelayedHoming:
                if (delayedHoming != null)
                {
                    if (opponentId != ulong.MaxValue) {
                         delayedHoming.Initialize(action.speed, action.homingSpeed, action.homingDelay, opponentId, capturedOpponentPosition);
                         if (!delayedHoming.enabled) {
                            delayedHoming.enabled = true;
                         }
                    } else {
                        // Fallback to linear if no opponent
                        if (linear != null) {
                            linear.enabled = true;
                            linear.Initialize(action.speed);
                        }
                    }
                }
                else
                {
                    // Missing component, do nothing
                    break; // Add break to fix fallthrough
                }
                break;

            case BehaviorType.Homing:

                break;
        }
    }
    // --- END Helper Method ---
} 