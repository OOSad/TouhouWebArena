using UnityEngine;
using Unity.Netcode;
using TouhouWebArena;
using TouhouWebArena.Spellcards;

/// <summary>
/// **[Server Only]** Handles the initial setup and triggering of Level 2 and 3 spellcard execution.
/// Loads data, finds target, calculates origin, and delegates action execution to <see cref="ServerSpellcardActionRunner"/>.
/// Instantiated and used by <see cref="ServerAttackSpawner"/>.
/// </summary>
public class ServerSpellcardExecutor
{
    private ServerSpellcardActionRunner _actionRunner;

    public ServerSpellcardExecutor(ServerSpellcardActionRunner actionRunner)
    {
        _actionRunner = actionRunner;
    }

    /// <summary>
    /// **[Server Only]** Initiates the execution of a Level 2 or 3 spellcard pattern against the opponent.
    /// </summary>
    /// <param name="senderClientId">The ClientId of the player declaring the spellcard.</param>
    /// <param name="senderCharacterName">The character name of the sender.</param>
    /// <param name="spellLevel">The level of the spellcard (2 or 3).</param>
    public void ExecuteLevel2or3Spellcard(ulong senderClientId, string senderCharacterName, int spellLevel)
    {
         if (!NetworkManager.Singleton.IsServer) return;
         if (_actionRunner == null) 
         {
             Debug.LogError("[ServerSpellcardExecutor] Action Runner is null!");
             return;
         }

        // --- Find Opponent --- 
        ulong opponentClientId = ulong.MaxValue;
        NetworkObject opponentPlayerObject = null;
        PlayerRole opponentRole = PlayerRole.None;
        Rect opponentBounds = new Rect();
        foreach (var connectedClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (connectedClient.ClientId != senderClientId)
            {
                opponentClientId = connectedClient.ClientId;
                opponentPlayerObject = connectedClient.PlayerObject;
                break;
            }
        }
        if (opponentPlayerObject == null)
        {
            Debug.LogWarning($"[ServerSpellcardExecutor.ExecuteLevel2or3] Could not find opponent for client {senderClientId}.");
            return; // Cannot execute spellcard without an opponent
        }
        // --- Determine Opponent Role and Bounds ---
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentClientId);
            if (opponentData.HasValue)
            {
                opponentRole = opponentData.Value.Role;
                opponentBounds = (opponentRole == PlayerRole.Player1) ? ClientAuthMovement.player1Bounds : ClientAuthMovement.player2Bounds;
            }
            else 
            { 
                Debug.LogError($"[ServerSpellcardExecutor.ExecuteLevel2or3] Could not get PlayerData for opponent {opponentClientId}."); 
                return; 
            }
        }
        else 
        { 
            Debug.LogError("[ServerSpellcardExecutor.ExecuteLevel2or3] PlayerDataManager instance missing."); 
            return; 
        }
        // -------------------------------------------
        
        Vector3 capturedOpponentPositionForHoming = opponentPlayerObject.transform.position;

        // --- Load Spellcard Resource --- 
        string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";
        SpellcardData spellcardData = Resources.Load<SpellcardData>(resourcePath);
        if (spellcardData == null)
        {   
            // Use string interpolation
            Debug.LogError($"[ServerSpellcardExecutor.ExecuteLevel2or3] Failed to load SpellcardData for Level {spellLevel} at path: {resourcePath}");
            return;
        }

        // Calculate Origin Position
        Vector3 originPosition = CalculateSpellcardOrigin(senderCharacterName, spellLevel, opponentBounds);
        Quaternion originRotation = Quaternion.identity;

        // Start Spawning Coroutine via the Runner
        _actionRunner.StartCoroutine(_actionRunner.RunSpellcardActions(spellcardData, originPosition, originRotation, opponentClientId, capturedOpponentPositionForHoming));
    }

    /// <summary>
    /// [Server Only] Calculates the origin position for Level 2/3 spellcards based on character and opponent bounds.
    /// </summary>
    private Vector3 CalculateSpellcardOrigin(string senderCharacterName, int spellLevel, Rect opponentBounds)
    {
         Vector3 origin = Vector3.zero;
         if (senderCharacterName == "HakureiReimu")
         {
             float randomX = Random.Range(opponentBounds.xMin + 0.5f, opponentBounds.xMax - 0.5f);
             origin = new Vector3(randomX, opponentBounds.yMax - 1.0f, 0);
         }
         else if (senderCharacterName == "KirisameMarisa")
         {
             if (spellLevel == 2)
             {
                 float edgeX = opponentBounds.center.x > 0 ? opponentBounds.xMax - 0.5f : opponentBounds.xMin + 0.5f;
                 origin = new Vector3(edgeX, opponentBounds.yMax - 1.0f, 0);
             }
             else if (spellLevel == 3)
             {
                 // Level 3: Spawn from the edge closest to the SENDER.
                 float edgeX;
                 // Need sender's role to determine closest edge reliably?
                 // Assuming opponentBounds.center.x < 0 means opponent is P1, so sender is P2 (right)
                 // spawn on the left edge (xMin) which is closest.
                 // Assuming opponentBounds.center.x > 0 means opponent is P2, so sender is P1 (left)
                 // spawn on the right edge (xMax) which is closest.
                 // Let's stick to the previous logic for now: spawn edge away from screen center?
                 edgeX = opponentBounds.center.x < 0 ? opponentBounds.xMax - 0.5f : opponentBounds.xMin + 0.5f; // Furthest edge?
                 origin = new Vector3(edgeX, opponentBounds.yMax - 1.0f, 0);
             }
             else
             {   // Fallback for unknown Marisa level
                 origin = new Vector3(opponentBounds.center.x, opponentBounds.yMax - 1.0f, 0);
             }
         }
         else
         {   // Fallback for unknown character
              // Use string interpolation
             Debug.LogWarning($"Unknown character '{senderCharacterName}' for spellcard origin. Defaulting to top-center.");
             origin = new Vector3(opponentBounds.center.x, opponentBounds.yMax - 1.0f, 0);
         }
         return origin;
    }
} 