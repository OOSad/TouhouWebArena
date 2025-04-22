using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Moves the GameObject forward at a constant speed.
    /// Assumes the bullet's rotation is set correctly upon spawning.
    /// </summary>
    public class LinearMovement : NetworkBehaviour
    {
        public float speed = 5f;

        // We assume the initial direction is baked into the transform's rotation
        // by the spawning logic.
        // Movement is executed client-side for performance in bullet hell scenarios.

        void Update()
        {            
            // Use transform.up because in 2D, forward is typically the Y axis.
            // Adjust if your project uses a different convention.
            transform.position += transform.up * speed * Time.deltaTime;

            // Note: No network synchronization here. The spawner tells clients
            // where to spawn and with what parameters. Movement is predicted/
            // handled locally.
        }

        /// <summary>
        /// Sets the speed for this bullet.
        /// This should be called by the spellcard activation logic after spawning.
        /// </summary>
        public void Initialize(float initialSpeed)
        {
            speed = initialSpeed;
        }
    }
} 