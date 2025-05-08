using UnityEngine;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] Moves the GameObject radially outwards from a spawn center 
    /// while also applying tangential movement, creating a spiral effect.
    /// </summary>
    public class ClientSpiralMovement : MonoBehaviour
    {
        private float _radialSpeed;
        private float _tangentialSpeed;
        private Vector3 _spawnCenter;

        void Update()
        {
            // Calculate direction from spawn center to current position
            Vector3 directionFromCenter = transform.position - _spawnCenter;

            // Avoid issues if bullet is exactly at the center, 
            // though it should quickly move away from it.
            if (directionFromCenter == Vector3.zero)
            {
                // Default to moving along initial transform.up if at center.
                // The initial rotation of the bullet might give a preferred initial direction.
                directionFromCenter = transform.up; 
            }

            // Normalize to get radial direction
            Vector3 radialDirection = directionFromCenter.normalized;

            // Calculate tangential direction (perpendicular in 2D)
            // Assumes Z-axis is unused for 2D movement direction.
            Vector3 tangentialDirection = new Vector3(-radialDirection.y, radialDirection.x, 0);

            // Calculate movement vectors
            Vector3 radialMovement = radialDirection * _radialSpeed * Time.deltaTime;
            Vector3 tangentialMovement = tangentialDirection * _tangentialSpeed * Time.deltaTime;
            
            // Apply combined movement
            transform.position += radialMovement + tangentialMovement;
        }

        /// <summary>
        /// Initializes the spiral movement behavior.
        /// </summary>
        /// <param name="radialSpeed">Speed moving directly outwards from the center.</param>
        /// <param name="tangentialSpeed">Speed moving sideways. Sign determines direction.</param>
        /// <param name="spawnCenter">The world position the spiral originates from (bullet's initial position).</param>
        public void Initialize(float radialSpeed, float tangentialSpeed, Vector3 spawnCenter)
        {
            _radialSpeed = radialSpeed;
            _tangentialSpeed = tangentialSpeed;
            _spawnCenter = spawnCenter;
            
            this.enabled = true; // Ensure component is active
        }

        void OnEnable()
        {
            // If there's any state that needs resetting if the object is reused from a pool
            // and Initialize might not be called right after, do it here.
            // For now, _spawnCenter is critical and set by Initialize.
        }
    }
} 