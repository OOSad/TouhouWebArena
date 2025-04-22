using UnityEngine;
using System.Collections.Generic;

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Defines the type of spatial formation for spawning bullets.
    /// </summary>
    public enum FormationType
    {
        Point,    // Single point spawn (uses positionOffset)
        Circle,   // Circle formation (uses positionOffset, count, radius)
        Line      // Line formation (uses positionOffset, count, spacing, angle) - angle relative to parent transform forward
    }

    /// <summary>
    /// Defines the type of movement behavior for spawned bullets.
    /// </summary>
    public enum BehaviorType
    {
        Linear,
        Homing,        // Simple homing towards a target
        DelayedHoming  // Moves linearly first, then homes
        // Add more complex behaviors here later (e.g., Wavy, Orbit, Accelerate)
    }

    /// <summary>
    /// Describes a single action or pattern within a spellcard sequence.
    /// </summary>
    [System.Serializable]
    public class SpellcardAction
    {
        [Header("Timing")]
        [Tooltip("Delay in seconds after spellcard activation before this action starts.")]
        public float startDelay = 0f;

        [Header("Spawning")]
        [Tooltip("Bullet prefabs to spawn. If multiple, they will be cycled through for the formation.")]
        public List<GameObject> bulletPrefabs;

        [Tooltip("The formation pattern to use for spawning.")]
        public FormationType formation = FormationType.Point;

        [Tooltip("Base offset from the spellcard origin transform.")]
        public Vector2 positionOffset = Vector2.zero;

        [Tooltip("Number of bullets to spawn in the formation (used by Circle, Line).")]
        public int count = 1;

        [Tooltip("Radius of the circle formation.")]
        public float radius = 1f;

        [Tooltip("Spacing between bullets in a line formation.")]
        public float spacing = 0.5f;

        [Tooltip("Angle (degrees) of the line formation relative to the spawner's forward direction.")]
        public float angle = 0f;

        [Header("Behavior")]
        [Tooltip("Movement behavior assigned to the spawned bullets.")]
        public BehaviorType behavior = BehaviorType.Linear;

        [Tooltip("Initial speed of the bullets.")]
        public float speed = 5f;

        [Tooltip("Delay before homing starts (for DelayedHoming).")]
        public float homingDelay = 0.5f;

        // Add target acquisition parameters later if needed (e.g., target tag, closest player)
    }

    /// <summary>
    /// ScriptableObject to define a spellcard, composed of a sequence of actions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpellcard", menuName = "TouhouWebArena/Spellcard Data")]
    public class SpellcardData : ScriptableObject
    {
        [Tooltip("The charge level required to activate this spellcard (e.g., 2, 3, 4).")]
        public int requiredChargeLevel = 2;

        // Potential field for character association if needed later
        // public Character associatedCharacter;

        [Tooltip("The sequence of actions performed when this spellcard is activated.")]
        public List<SpellcardAction> actions;

        // Could add overall duration, sound effects, visual effects etc. here later
    }
} 