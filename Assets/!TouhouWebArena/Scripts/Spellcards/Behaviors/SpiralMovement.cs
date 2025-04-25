using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Moves the attached NetworkObject forward radially while also applying tangential movement based on world directions,
    /// creating a spiral effect. Server-authoritative.
    /// </summary>
    public class SpiralMovement : NetworkBehaviour
    {
        private float _radialSpeed = 5f;
        private float _tangentialSpeed = 0f;
        private Vector3 _spawnCenter = Vector3.zero;
        private float _spawnTime = 0f;

        // --- Server-Side Movement --- 

        void Update()
        {
            if (!IsServer) return;

            // Calculate direction from spawn center to current position
            Vector3 directionFromCenter = transform.position - _spawnCenter;

            // Avoid issues if bullet is exactly at the center
            if (directionFromCenter == Vector3.zero)
            {
                // Default to moving along initial transform.up if at center
                directionFromCenter = transform.up;
            }

            // Normalize to get radial direction
            Vector3 radialDirection = directionFromCenter.normalized;

            // Calculate tangential direction (perpendicular in 2D)
            Vector3 tangentialDirection = new Vector3(-radialDirection.y, radialDirection.x, 0);

            // Calculate movement vectors based on world directions
            Vector3 radialMovement = radialDirection * _radialSpeed * Time.deltaTime;
            Vector3 tangentialMovement = tangentialDirection * _tangentialSpeed * Time.deltaTime;
            
            // Apply combined movement
            transform.position += radialMovement + tangentialMovement;
        }

        // --- Initialization (Called by Spawner) --- 

        /// <summary>
        /// **[Server Only]** Initializes the movement speeds and the spawn center (world position the spiral originates from).
        /// </summary>
        /// <param name="radialSpeed">Speed moving directly outwards from the center.</param>
        /// <param name="tangentialSpeed">Speed moving sideways along the circumference. Sign determines direction (e.g., positive=clockwise, negative=counter-clockwise).</param>
        /// <param name="spawnCenter">The world position the spiral originates from.</param>
        public void Initialize(float radialSpeed, float tangentialSpeed, Vector3 spawnCenter)
        {
            if (!IsServer) return;
            _radialSpeed = radialSpeed;
            _tangentialSpeed = tangentialSpeed;
            _spawnCenter = spawnCenter;
            _spawnTime = Time.time; // Make sure we record spawn time here for the logging
        }
    }
} 