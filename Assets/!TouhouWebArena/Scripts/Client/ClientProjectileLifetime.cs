using UnityEngine;

/// <summary>
/// Client-side script responsible for managing the timed lifecycle of a pooled projectile.
/// After a specified duration, set via `Initialize()`, this script automatically returns
/// the projectile GameObject to the `ClientGameObjectPool`.
/// It also provides a method for immediate, forceful return to the pool (e.g., on collision).
/// </summary>
public class ClientProjectileLifetime : MonoBehaviour
{
    private float _lifetime;
    private bool _isInitialized = false; // Flag to ensure Initialize() is called after OnEnable() to set the specific lifetime.
    private float _timeActive = 0f;

    /// <summary>
    /// Initializes the projectile with its maximum lifetime for its current use.
    /// This method MUST be called after the GameObject is activated and `OnEnable` has run,
    /// as `OnEnable` resets `_isInitialized`.
    /// </summary>
    /// <param name="lifetime">How long this projectile should exist (in seconds) before being returned to the pool.</param>
    public void Initialize(float lifetime)
    {
        _lifetime = lifetime;
        _timeActive = 0f;
        _isInitialized = true;
        // Debug.Log($"[ClientProjectileLifetime] INITIALIZE: {gameObject.name} with lifetime: {_lifetime}. InstanceID: {gameObject.GetInstanceID()}"); // Kept for debugging if needed
    }

    /// <summary>
    /// Called when the GameObject is set active (e.g., when retrieved from the object pool).
    /// Resets `_isInitialized` to false and `_timeActive` to 0. 
    /// The `Initialize()` method must be called subsequently to properly set the lifetime for this activation.
    /// </summary>
    void OnEnable()
    {
        _isInitialized = false; 
        _timeActive = 0f;
    }

    private int updateLogCounter = 0;
    private const int UPDATE_LOG_INTERVAL = 60; // Log approximately every second

    void Update()
    {
        // Enhanced logging for specific problematic bullets
        if (gameObject.name.StartsWith("RedSmallCircle_Variant") || gameObject.name.StartsWith("WhiteSmallOval_Variant"))
        {
            updateLogCounter++;
            // Log periodically to check state
            if (updateLogCounter % UPDATE_LOG_INTERVAL == 1)
            {
                Debug.Log($"[CPL UPDATE PERIODIC]: {gameObject.name} (ID: {gameObject.GetInstanceID()}) | " +
                          $"isInit: {_isInitialized} | activeInHierarchy: {gameObject.activeInHierarchy} | " +
                          $"timeActive: {_timeActive:F3} | lifetime: {_lifetime:F3} | deltaTime: {Time.deltaTime:F5}");
            }
            // Log if it's about to despawn (and wasn't just logged by periodic check)
            if (_isInitialized && _timeActive >= _lifetime && (updateLogCounter % UPDATE_LOG_INTERVAL != 1) )
            {
                 // Debug.Log($"[CPL UPDATE PRE-DESPAWN]: {gameObject.name} (ID: {gameObject.GetInstanceID()}) | " +
                 //           $"isInit: {_isInitialized} | timeActive: {_timeActive:F3} | lifetime: {_lifetime:F3}");
            }
        }

        if (!_isInitialized || !gameObject.activeInHierarchy) return;

        _timeActive += Time.deltaTime;
        if (_timeActive >= _lifetime)
        {
            // Debug.Log($"[ClientProjectileLifetime] LIFETIME REACHED: {gameObject.name} (ID: {gameObject.GetInstanceID()}). " +
            //           $"Lifetime was: {_lifetime:F3}s, TimeActive was: {_timeActive:F3}s. Returning to pool.");
            ReturnToPool();
        }
    }

    /// <summary>
    /// Returns the GameObject to the ClientGameObjectPool.
    /// If the pool instance is not available, it destroys the GameObject as a fallback.
    /// </summary>
    private void ReturnToPool()
    {
        if (ClientGameObjectPool.Instance != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(gameObject);
        }
        else
        {
            // Fallback if pool is somehow gone, just destroy
            Debug.LogWarning("[ClientProjectileLifetime] ClientGameObjectPool instance not found. Destroying object instead of pooling.", gameObject);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Immediately returns this projectile to the object pool.
    /// Typically called by other components (e.g., collision handlers) when the projectile should be despawned prematurely.
    /// </summary>
    public void ForceReturnToPool()
    {
        ReturnToPool();
    }
} 