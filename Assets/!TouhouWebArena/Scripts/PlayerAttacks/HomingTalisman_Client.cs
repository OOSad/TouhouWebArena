using UnityEngine;

namespace TouhouWebArena.PlayerAttacks
{
    /// <summary>
    /// Client-side logic for Reimu's homing talisman charge attack.
    /// </summary>
    public class HomingTalisman_Client : MonoBehaviour
    {
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float seekDelay = 0.2f; // Delay before starting to seek
        private float _timeActive;
        private float _seekTimer;
        private ClientProjectileLifetime _projectileLifetime;

        private void Awake()
        {
            _projectileLifetime = GetComponent<ClientProjectileLifetime>();
            if (_projectileLifetime == null)
            {
                Debug.LogError("HomingTalisman_Client requires a ClientProjectileLifetime component!", this);
            }
        }

        /// <summary>
        /// Initializes the talisman.
        /// </summary>
        /// <param name="initialDelay">Delay before the talisman starts moving/seeking.</param>
        public void Initialize(float initialDelay)
        {
            _timeActive = -initialDelay; // Start time negative to account for delay
            _seekTimer = seekDelay;

            if (_projectileLifetime != null)
            {
                _projectileLifetime.Initialize(lifetime + initialDelay);
            }
            // Start facing upwards or based on initial spawn rotation
            // transform.rotation = Quaternion.identity; 
        }

        // ... existing code ...
    }
} 