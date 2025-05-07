using UnityEngine;

/// <summary>
/// Client-side script to manage the lifetime of a pooled projectile.
/// Returns the projectile to the ClientGameObjectPool after a specified duration.
/// </summary>
public class ClientProjectileLifetime : MonoBehaviour
{
    private float _lifetime;
    private bool _isInitialized = false;
    private float _timeActive = 0f;

    /// <summary>
    /// Initializes the projectile with its maximum lifetime.
    /// </summary>
    /// <param name="lifetime">How long this projectile should exist before being returned to the pool.</param>
    public void Initialize(float lifetime)
    {
        _lifetime = lifetime;
        _timeActive = 0f;
        _isInitialized = true;
    }

    void OnEnable()
    {
        // Reset when enabled (e.g., when taken from pool)
        // Initialize() must be called after this to set the specific lifetime for this use.
        _isInitialized = false; 
        _timeActive = 0f;
    }

    void Update()
    {
        if (!_isInitialized || !gameObject.activeInHierarchy) return;

        _timeActive += Time.deltaTime;
        if (_timeActive >= _lifetime)
        {
            ReturnToPool();
        }
    }

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

    // Optional: A method to be called by collision scripts to return to pool immediately
    public void ForceReturnToPool()
    {
        ReturnToPool();
    }
} 