using UnityEngine;

/// <summary>
/// Manages references to the designated spawn areas for opponent bullets.
/// PlayerAttackRelay queries this manager to find the correct RectTransform based on the local player's role.
/// </summary>
public class SpawnAreaManager : MonoBehaviour
{
    public static SpawnAreaManager Instance { get; private set; }

    [Header("Opponent Bullet Spawn Areas")]
    [Tooltip("The Transform whose position is the CENTER of the area ON PLAYER 1'S SIDE where bullets sent BY PLAYER 2 will spawn.")]
    [SerializeField] private Transform player1TargetedSpawnCenter;

    [Tooltip("The Transform whose position is the CENTER of the area ON PLAYER 2'S SIDE where bullets sent BY PLAYER 1 will spawn.")]
    [SerializeField] private Transform player2TargetedSpawnCenter;

    [Tooltip("The dimensions (Width, Height) of the spawn zones. Assumed to be the same for both players.")]
    [SerializeField] private Vector2 spawnZoneDimensions = new Vector2(5f, 2f); // Example dimensions

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SpawnAreaManager] Duplicate instance detected, destroying self.", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player1TargetedSpawnCenter == null)
            Debug.LogError("[SpawnAreaManager] Player 1 Targeted Spawn Center is not assigned!", this);
        if (player2TargetedSpawnCenter == null)
            Debug.LogError("[SpawnAreaManager] Player 2 Targeted Spawn Center is not assigned!", this);
    }

    /// <summary>
    /// Gets the Transform representing the center of the spawn area for the targeted player.
    /// </summary>
    public Transform GetSpawnCenterForTargetedPlayer(PlayerRole targetedPlayerRole)
    {
        switch (targetedPlayerRole)
        {
            case PlayerRole.Player1:
                return player1TargetedSpawnCenter;
            case PlayerRole.Player2:
                return player2TargetedSpawnCenter;
            default:
                Debug.LogWarning($"[SpawnAreaManager] GetOpponentSpawnAreaForPlayer called with invalid role: {targetedPlayerRole}");
                return null;
        }
    }

    /// <summary>
    /// Gets the configured dimensions for the spawn zones.
    /// </summary>
    public Vector2 GetSpawnZoneDimensions()
    {
        return spawnZoneDimensions;
    }
} 