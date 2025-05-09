using UnityEngine;

/// <summary>
/// [Client Only] Detects collision between a client-side projectile (bullet) and stage boundaries (walls).
/// Upon collision with an object on the "StageWalls" layer, it instructs the associated `ClientProjectileLifetime` 
/// component to return the bullet to the object pool. This script works with both trigger and non-trigger colliders.
/// </summary>
[RequireComponent(typeof(ClientProjectileLifetime))]
[RequireComponent(typeof(Collider2D))] // Ensure there's a collider to detect collisions
public class ClientBulletWallCollision : MonoBehaviour
{
    private ClientProjectileLifetime _projectileLifetime; // Cached reference to manage bullet despawn.
    // private const string STAGE_WALL_TAG = "StageWall"; // Using layer check instead
    private int _stageWallLayer = -1; // Cached integer representation of the "StageWalls" layer.

    /// <summary>
    /// Caches the ClientProjectileLifetime component and the integer representation of the "StageWalls" layer.
    /// Disables the script if dependencies are missing or the layer is not found, logging appropriate errors.
    /// Also checks if the attached Collider2D is set as a trigger and logs a warning if not, as triggers are often preferred for simple despawn logic.
    /// </summary>
    void Awake()
    {
        _projectileLifetime = GetComponent<ClientProjectileLifetime>();
        if (_projectileLifetime == null)
        {
            Debug.LogError("[ClientBulletWallCollision] ClientProjectileLifetime component not found on this GameObject! Bullets may not despawn on wall hit.", gameObject);
            enabled = false; 
            return;
        }

        // Get the integer representation of the layer named "StageWalls"
        _stageWallLayer = LayerMask.NameToLayer("StageWalls");
        if (_stageWallLayer == -1) // LayerMask.NameToLayer returns -1 if layer doesn't exist
        {
            Debug.LogError("[ClientBulletWallCollision] StageWalls layer not found! Please ensure the layer exists in Project Settings -> Tags and Layers. Wall collision will not work.", gameObject);
            enabled = false; 
            return;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            // For best results with pooling and simple despawn, bullets are often triggers.
            // If they are solid colliders, they'll stop physically, which is also fine,
            // but this script will still despawn them on OnCollisionEnter2D.
            Debug.LogWarning($"[ClientBulletWallCollision] Collider2D on {gameObject.name} is not set to IsTrigger. Will use OnCollisionEnter2D. Consider using triggers for simple despawn behavior.", gameObject);
        }
    }

    /// <summary>
    /// Called when this GameObject's collider (set as a trigger) overlaps with another collider.
    /// Checks if the other collider is on the "StageWalls" layer and, if so, forces the bullet to return to the pool.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_projectileLifetime.enabled || _stageWallLayer == -1) return; // Script might have been disabled in Awake

        if (other.gameObject.layer == _stageWallLayer)
        {
            // Debug.Log($"[ClientBulletWallCollision] Bullet {gameObject.name} hit wall (Trigger): {other.name}");
            _projectileLifetime.ForceReturnToPool();
        }
    }

    /// <summary>
    /// Called when this GameObject's collider (not set as a trigger) collides with another collider.
    /// Checks if the other collider is on the "StageWalls" layer and, if so, forces the bullet to return to the pool.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_projectileLifetime.enabled || _stageWallLayer == -1) return; // Script might have been disabled in Awake

        if (collision.gameObject.layer == _stageWallLayer)
        {
            // Debug.Log($"[ClientBulletWallCollision] Bullet {gameObject.name} hit wall (Collision): {collision.gameObject.name}");
            _projectileLifetime.ForceReturnToPool();
        }
    }
} 