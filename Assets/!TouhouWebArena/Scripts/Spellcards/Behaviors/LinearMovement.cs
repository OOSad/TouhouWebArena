using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Moves the GameObject forward at a constant speed.
    /// Assumes the bullet's rotation is set correctly upon spawning.
    /// </summary>
    public class LinearMovement : NetworkBehaviour // Inherits from NetworkBehaviour for potential network context, even if movement is local
    {
        public float speed = 5f;

        // We assume the initial direction is baked into the transform's rotation
        // by the spawning logic.
        // Movement is executed client-side for performance in bullet hell scenarios.

        void Update()
        {
            // Movement logic should only be executed on the server
            // Clients receive position updates via NetworkObject synchronization
            if (!IsServer) return;

            // Use transform.up because in 2D, forward is typically the Y axis.
            // Adjust if your project uses a different convention.
            transform.position += transform.up * speed * Time.deltaTime;

            // Note: No network synchronization here (no NetworkTransform).
            // The spawner tells clients where to spawn and with what parameters.
            // Movement is predicted/handled locally.
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
