# Extra Attack System Documentation

## Overview

Extra Attacks are character-specific special abilities automatically triggered when a player destroys a designated "trigger" fairy. This system is **client-authoritative for visual spawning and behavior**, with the server acting as a relay to ensure all clients witness the same event with synchronized parameters. This ensures responsive visuals for the triggering player while maintaining consistency across all participants.

## Triggering Mechanism

1.  **Fairy Marking (Server-Side):**
    *   `FairySpawner.cs` on the server determines which fairy in a wave will be a "trigger" fairy. It sets the `TriggerFairyIndex` in the `FairyWaveData` before this data is sent to clients when a new wave begins.

2.  **Client-Side Fairy Initialization:**
    *   `FairySpawnNetworkHandler.cs` on each client receives the `FairyWaveData`.
    *   When spawning fairies for the wave, it identifies the trigger fairy using the `TriggerFairyIndex`.
    *   It initializes the corresponding `ClientFairyHealth.cs` component with an `_isExtraAttackTrigger = true` flag.

3.  **Client-Side Kill Detection & Initial Action:**
    *   When any fairy\'s `ClientFairyHealth.TakeDamage()` method results in the fairy\'s death, it checks its `_isExtraAttackTrigger` flag.
    *   If `true`, it calls `ClientExtraAttackManager.Instance.OnTriggerFairyKilled(attackerOwnerClientId)`, passing the `OwnerClientId` of the player character that dealt the killing blow.

## Synchronization Flow for Randomized Parameters

To ensure all clients see the exact same extra attack (e.g., same spawn position, same initial trajectory, same cosmetic variations like tilt), the client that triggers the event pre-calculates all random values and these are relayed by the server.

1.  **Parameter Generation (Triggering Client):**
    *   Inside `ClientExtraAttackManager.OnTriggerFairyKilled()`:
        *   If the `attackerOwnerClientId` matches the `NetworkManager.Singleton.LocalClientId` (i.e., the local player\'s character made the kill):
            *   The client determines the `killerCharacterName` and `killerPlayerRole`.
            *   It then **pre-calculates all necessary random values** for the potential extra attacks:
                *   For Reimu\'s Orb: `reimuSpawnX`, `reimuSpawnY`, `reimuSidewaysForce`.
                *   For Marisa\'s Laser: `marisaSpawnXOffset`, `marisaTiltAngle`. (The decision to tilt and the specific angle are combined here).
            *   It calls `PlayerExtraAttackRelay.LocalInstance.InformServerOfExtraAttackTriggerServerRpc()` on its local Player object, passing:
                *   `killerCharacterName`
                *   `killerPlayerRole`
                *   `attackerOwnerClientId` (original triggerer)
                *   All pre-calculated random parameters (`reimuSpawnX`, `reimuSpawnY`, `reimuSidewaysForce`, `marisaSpawnXOffset`, `marisaTiltAngle`).

2.  **Server Relay:**
    *   The `PlayerExtraAttackRelay.InformServerOfExtraAttackTriggerServerRpc()` method (executing on the server) receives all the event data and the pre-calculated random parameters.
    *   It immediately calls `ClientExtraAttackManager.Instance.RelayExtraAttackToClientsClientRpc()` (this is a ClientRpc targeting the global `ClientExtraAttackManager` instance on all clients), passing through all the received data, including the exact random parameters.

3.  **Synchronized Client-Side Spawning:**
    *   All clients (including the one that originally sent the ServerRpc) receive the `ClientExtraAttackManager.RelayExtraAttackToClientsClientRpc()`.\
    *   This RPC handler now has the deterministic parameters. It calls `SpawnExtraAttackInternal()`, passing these exact values.
    *   Crucially, the original triggering client no longer spawns the attack immediately in `OnTriggerFairyKilled()`; it waits for this RPC call like all other clients to ensure it uses the server-confirmed (and thus relayed) parameters.

## Spawning Logic (`ClientExtraAttackManager.cs`)

1.  **`SpawnExtraAttackInternal(...)`:**
    *   Accepts `characterName`, `attackerPlayerRole`, `actualAttackerClientId`, and all the synchronized random parameters.
    *   Determines the `targetPlayerRole` (opponent of `attackerPlayerRole`).
    *   Calculates `targetPlayAreaBounds` for the opponent.
    *   Calls the appropriate character-specific internal spawn method:
        *   If Reimu: `SpawnReimuExtraAttackInternal(targetSpawnAreaAnchor, actualAttackerClientId, pReimuSpawnX, pReimuSpawnY, pReimuSidewaysForce)`
        *   If Marisa: `SpawnMarisaExtraAttackInternal(laserSpecificSpawnAnchor, targetPlayAreaBounds, actualAttackerClientId, pMarisaSpawnXOffset, pMarisaTiltAngle)`

2.  **`SpawnReimuExtraAttackInternal(...)`:**
    *   Gets a Reimu Orb prefab from the `ClientGameObjectPool`.
    *   Sets its spawn position using the provided `pReimuSpawnX`, `pReimuSpawnY`, and the Z from `targetSpawnAreaAnchor`.
    *   Calls `orbScript.Initialize(attackerClientId, pReimuSidewaysForce)`, passing the synchronized sideways force.

3.  **`SpawnMarisaExtraAttackInternal(...)`:**
    *   Gets a Marisa Laser prefab from the `ClientGameObjectPool`.
    *   Selects the correct `laserSpecificSpawnAnchor` (P1 or P2).
    *   Calculates the final spawn X using `laserSpecificSpawnAnchor.position.x + pMarisaSpawnXOffset` (clamped to `targetPlayAreaBounds`).
    *   Sets its spawn position.
    *   Calls `laserScript.Initialize(attackerClientId, targetPlayAreaBounds, pMarisaTiltAngle)`, passing the synchronized tilt angle.

## Character-Specific Extra Attacks (Client-Simulated)

### Reimu\'s Extra Attack: Yin-Yang Orb

*   **Prefab:** `ReimuExtraAttackOrb.prefab` (pooled). Contains `ReimuExtraAttackOrb_Client.cs`.
*   **Spawning:** Handled by `ClientExtraAttackManager.SpawnReimuExtraAttackInternal()` using synchronized spawn X, Y.
*   **Behavior (`ReimuExtraAttackOrb_Client.cs`):**
    *   **Initialization:** `Initialize(attackerClientId, predeterminedSidewaysForce)` sets the attacker and applies initial forces.
    *   **Movement:** Uses a `Rigidbody2D`.
        *   An initial `initialUpwardForce` (from prefab field) and the `predeterminedSidewaysForce` (synchronized) are applied as an impulse.
        *   Affected by gravity.
        *   Bounces off stage elements (requires appropriate `PhysicsMaterial2D` on its `CircleCollider2D`).
    *   **Damage:**
        *   On `OnTriggerEnter2D` with an opponent\'s `PlayerHitbox` (layer check, `OwnerClientId != _attackerClientId`):
            *   Calls `PlayerExtraAttackRelay.LocalInstance.ReportExtraAttackPlayerHitServerRpc()` to inform the server to deal damage.
        *   The orb **does not despawn** upon hitting a player. It continues its trajectory.
    *   **Other Collisions:** Despawns (`ReturnToPool()`) if it hits a `ClientFairyHealth` or `ClientSpiritHealth`.
    *   **Lifetime:** Automatically returns to the pool after a set `lifetime` (countdown in `Update()`).
    *   **Pooling:** Uses `ClientGameObjectPool.Instance.ReturnObject()`.

### Marisa\'s Extra Attack: Earthlight Ray (Laser)

*   **Prefab:** `MarisaExtraAttackEarthlightRay.prefab` (pooled). Contains `MarisaExtraAttackLaser_Client.cs`.
*   **Spawning:** Handled by `ClientExtraAttackManager.SpawnMarisaExtraAttackInternal()` using a synchronized X offset from a designated anchor (`marisaLaserSpawnAnchorP1` or `P2`).
*   **Behavior (`MarisaExtraAttackLaser_Client.cs`):**
    *   **Initialization:** `Initialize(attackerClientId, playBounds, predeterminedTiltAngle)` sets attacker, play area bounds (for height), and the exact tilt.
    *   **Visuals:** Uses a `SpriteRenderer`.
        *   Positioned at the synchronized spawn point (bottom of opponent\'s screen).
        *   Scaled/shaped to extend to the top of the `playBounds`.
        *   Rotation is set directly using `predeterminedTiltAngle`.
    *   **Activation Delay:** Has a configurable `activationDelay` (e.g., 0.5s). The laser is visible but non-damaging during this period (`currentActivationTimer` in `Update()`).
    *   **Damage:**
        *   `OnTriggerStay2D` is used.
        *   If `currentActivationTimer <= 0`:
            *   If it collides with an opponent\'s `PlayerHitbox` (layer check, `OwnerClientId != _attackerClientId`):
                *   Deals 1 damage per frame of contact by calling `PlayerExtraAttackRelay.LocalInstance.ReportExtraAttackPlayerHitServerRpc()`.\
                *   The player\'s own invincibility frames (I-frames, handled server-side by `PlayerHealth`) are responsible for preventing multiple damage instances from a single laser pass.
    *   **Lifetime:** Automatically returns to the pool after `activeDuration` (countdown in `Update()`).
    *   **Pooling:** Resets rotation to identity, then uses `ClientGameObjectPool.Instance.ReturnObject()`.

## Damage Application (Server-Authoritative)

While the extra attacks are visually client-simulated, the actual damage to players is server-authoritative.

1.  **Client Hit Report:**
    *   `ReimuExtraAttackOrb_Client.OnTriggerEnter2D` or `MarisaExtraAttackLaser_Client.OnTriggerStay2D` detects a collision with an opponent\'s `PlayerHitbox`.
    *   It calls `PlayerExtraAttackRelay.LocalInstance.ReportExtraAttackPlayerHitServerRpc(victimOwnerClientId, damageAmount, _attackerClientId)`.

2.  **Server Damage Processing (`PlayerExtraAttackRelay.cs`):**
    *   The `ReportExtraAttackPlayerHitServerRpc` method (executing on the server):
        *   Iterates through `NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values` to find the `NetworkObject` of the player whose `OwnerClientId` matches `victimOwnerClientId` and has a `PlayerHealth` component.
        *   If found, it calls `victimPlayerHealth.TakeDamage(damageAmount)` on that `PlayerHealth` component. Player health and subsequent death are server-authoritative.

## Key Scripts & Their Roles

*   **`FairySpawner.cs` (Server-Side):**
    *   Marks a fairy in `FairyWaveData` with `TriggerFairyIndex`.
*   **`FairySpawnNetworkHandler.cs` (Client-Side):**
    *   Reads `TriggerFairyIndex` from `FairyWaveData`.
    *   Initializes `ClientFairyHealth` with `_isExtraAttackTrigger` flag.
*   **`ClientFairyHealth.cs` (Client-Side, on Fairy Prefab):**
    *   Stores `_isExtraAttackTrigger`.
    *   On death (if trigger), calls `ClientExtraAttackManager.Instance.OnTriggerFairyKilled()`.
*   **`ClientExtraAttackManager.cs` (Client-Side Singleton, `NetworkBehaviour`):**
    *   `OnTriggerFairyKilled()`: If local player is attacker, generates all random parameters (spawn positions, forces, offsets, tilt angles) and calls `PlayerExtraAttackRelay...InformServerOfExtraAttackTriggerServerRpc()`.
    *   `RelayExtraAttackToClientsClientRpc()`: Receives trigger event and synchronized parameters from the server. Calls `SpawnExtraAttackInternal()`.
    *   `SpawnExtraAttackInternal()`: Determines target, calls character-specific internal spawn methods with synchronized parameters.
    *   `SpawnReimuExtraAttackInternal()` / `SpawnMarisaExtraAttackInternal()`: Get pooled object, set position using synchronized values, and call the attack script\'s `Initialize` method with synchronized parameters.
    *   Manages references to extra attack prefabs and parameters needed for random value generation (e.g., `marisaLaserXSpread`, `reimuOrbInitialSidewaysForceMin/Max`, `marisaLaserMaxTiltAngle`).
*   **`PlayerExtraAttackRelay.cs` (Client-Side, `NetworkBehaviour` on Player Prefab):**
    *   `LocalInstance`: Singleton accessor for the local player\'s relay.
    *   `InformServerOfExtraAttackTriggerServerRpc()`: Called by `ClientExtraAttackManager` (only on the client that triggered the kill). Sends event details and all pre-calculated random parameters to the server.
    *   `ReportExtraAttackPlayerHitServerRpc()`: Called by client-side attack scripts (`ReimuExtraAttackOrb_Client`, `MarisaExtraAttackLaser_Client`) when they hit an opponent. Sends victim and attacker details to the server for damage processing.
*   **`ReimuExtraAttackOrb_Client.cs` (Client-Side, on Reimu\'s Orb Prefab):**
    *   Manages orb\'s physics-based movement, lifetime, collision detection (player damage report, fairy/spirit despawn), and pooling.
    *   `Initialize` method now accepts a `predeterminedSidewaysForce`.
*   **`MarisaExtraAttackLaser_Client.cs` (Client-Side, on Marisa\'s Laser Prefab):**
    *   Manages laser\'s visuals (SpriteRenderer), activation delay, continuous damage logic (player damage report), lifetime, and pooling.
    *   `Initialize` method now accepts `playBounds` (for height) and a `predeterminedTiltAngle`.
*   **Supporting Manager Scripts (Client-Side Singletons):**
    *   `PlayerDataManager.cs`: Provides player character/role from `OwnerClientId`.
    *   `SpawnAreaManager.cs`: Provides opponent\'s play area bounds/transforms for targeting.
    *   `ClientGameObjectPool.cs`: Provides pooling for the extra attack prefabs.
    *   `NetworkManager.cs` (Netcode for GameObjects): Core networking functionality.
    *   `PlayerHealth.cs` (Server-Authoritative, on Player Prefab): Handles taking damage.

*(The old Player Death Bomb section can be removed from this file if it's documented elsewhere or considered out of scope for "Extra Attacks" specifically related to fairy triggers. For this update, I'm assuming it will be removed to keep the focus clear).* 