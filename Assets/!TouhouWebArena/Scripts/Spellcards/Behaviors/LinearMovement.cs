using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// A server-authoritative movement behavior that moves the attached GameObject forward
    /// along its local Y-axis (<c>transform.up</c>) at a constant speed.
    /// The initial direction must be set by rotating the GameObject upon spawning.
    /// </summary>
    public class LinearMovement : NetworkBehaviour // Inherits from NetworkBehaviour for potential network context, even if movement is local
    {
        /// <summary>
        /// The speed at which the GameObject moves forward.
        /// Can be set via the Inspector or using <see cref="Initialize"/>.
        /// </summary>
        [Tooltip("Speed in units per second along the local Y-axis.")]
        public float speed = 5f;

        // We assume the initial direction is baked into the transform's rotation
        // by the spawning logic.
        // Movement is executed client-side for performance in bullet hell scenarios.

        /// <summary>
        /// Server-only Update loop to move the object.
        /// Client positions are updated via NetworkTransform synchronization (assuming one is attached).
        /// </summary>
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
        /// Initializes the movement speed. Typically called by the spawning logic on the server
        /// immediately after retrieving the object from the pool and before spawning it.
        /// </summary>
        /// <param name="initialSpeed">The desired movement speed.</param>
        public void Initialize(float initialSpeed)
        {
            // Ensure this is only called on the server where the speed matters for movement calculation
            if (!IsServer) return;
            speed = initialSpeed;
        }
    }
}
