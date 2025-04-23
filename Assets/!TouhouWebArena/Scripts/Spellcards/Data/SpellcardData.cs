using UnityEngine;
using System.Collections.Generic;

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Defines the type of spatial formation used when spawning multiple bullets in a <see cref="SpellcardAction"/>.
    /// </summary>
    public enum FormationType
    {
        /// <summary>Spawn all bullets at a single point (defined by positionOffset).</summary>
        Point,
        /// <summary>Spawn bullets evenly distributed on the circumference of a circle.</summary>
        Circle,
        /// <summary>Spawn bullets evenly spaced along a straight line.</summary>
        Line
    }

    /// <summary>
    /// Defines the movement behavior assigned to bullets spawned by a <see cref="SpellcardAction"/>.
    /// </summary>
    public enum BehaviorType
    {
        /// <summary>Bullet travels in a straight line based on initial rotation.</summary>
        Linear,
        /// <summary>Bullet attempts to home towards the opponent player.</summary>
        Homing,
        /// <summary>Bullet travels linearly for a duration (<see cref="SpellcardAction.homingDelay"/>) before starting to home.</summary>
        DelayedHoming
        // Add more complex behaviors here later (e.g., Wavy, Orbit, Accelerate)
    }

    /// <summary>
    /// Represents a single, distinct pattern or step within a <see cref="SpellcardData"/> sequence.
    /// Defines the timing, spawning formation, bullet type, and behavior for a group of bullets.
    /// </summary>
    [System.Serializable]
    public class SpellcardAction
    {
        [Header("Timing")]
        /// <summary>
        /// Delay in seconds after the spellcard begins execution before this specific action starts.
        /// </summary>
        [Tooltip("Delay in seconds after the spellcard begins execution before this specific action starts.")]
        public float startDelay = 0f;

        [Header("Spawning")]
        /// <summary>
        /// The bullet prefab(s) to spawn for this action. If multiple are provided, the spawner will cycle through them when creating the formation (e.g., for alternating colors/types).
        /// </summary>
        [Tooltip("The bullet prefab(s) to spawn for this action. If multiple are provided, the spawner will cycle through them when creating the formation (e.g., for alternating colors/types).")]
        public List<GameObject> bulletPrefabs; // Use Prefab Variants here!

        /// <summary>
        /// The spatial pattern used to arrange the spawned bullets (Point, Circle, Line).
        /// </summary>
        [Tooltip("The spatial pattern used to arrange the spawned bullets (Point, Circle, Line).")]
        public FormationType formation = FormationType.Point;

        /// <summary>
        /// Base position offset relative to the spawner's transform where the formation is centered.
        /// </summary>
        [Tooltip("Base position offset relative to the spawner's transform where the formation is centered.")]
        public Vector2 positionOffset = Vector2.zero;

        /// <summary>
        /// Number of bullets to spawn in this action's formation (primarily for Circle and Line formations).
        /// </summary>
        [Tooltip("Number of bullets to spawn in this action's formation (primarily for Circle and Line formations).")]
        public int count = 1;

        /// <summary>
        /// Radius of the circle formation (used only if FormationType is Circle).
        /// </summary>
        [Tooltip("Radius of the circle formation (used only if FormationType is Circle).")]
        public float radius = 1f;

        /// <summary>
        /// Spacing between adjacent bullets in a line formation (used only if FormationType is Line).
        /// </summary>
        [Tooltip("Spacing between adjacent bullets in a line formation (used only if FormationType is Line).")]
        public float spacing = 0.5f;

        /// <summary>
        /// Angle (in degrees) of the line formation relative to the spawner's forward direction (used only if FormationType is Line).
        /// </summary>
        [Tooltip("Angle (in degrees) of the line formation relative to the spawner's forward direction (used only if FormationType is Line).")]
        public float angle = 0f;

        [Header("Behavior")]
        /// <summary>
        /// The movement behavior pattern assigned to all bullets spawned by this action.
        /// </summary>
        [Tooltip("The movement behavior pattern assigned to all bullets spawned by this action.")]
        public BehaviorType behavior = BehaviorType.Linear;

        /// <summary>
        /// The initial speed assigned to the spawned bullets.
        /// </summary>
        [Tooltip("The initial speed assigned to the spawned bullets.")]
        public float speed = 5f;

        /// <summary>
        /// For Line formations, this value is added to the speed of each subsequent bullet in the line (bullet 0 gets base speed, bullet 1 gets speed + increment, etc.).
        /// </summary>
        [Tooltip("For Line formations, the speed added to each subsequent bullet (0 = no increment).")]
        public float speedIncrementPerBullet = 0f;

        /// <summary>
        /// The speed at which bullets move while actively homing (used by Homing and DelayedHoming behaviors).
        /// </summary>
        [Tooltip("The speed at which bullets move while actively homing (used by Homing and DelayedHoming behaviors).")]
        public float homingSpeed = 4f;

        /// <summary>
        /// The duration (in seconds) the bullet travels linearly before starting to home (used only by DelayedHoming behavior).
        /// </summary>
        [Tooltip("The duration (in seconds) the bullet travels linearly before starting to home (used only by DelayedHoming behavior).")]
        public float homingDelay = 0.5f;

        /// <summary>
        /// Overrides the default lifetime of the spawned bullet prefab. Set to a positive value (seconds) to enable override. Values <= 0 use the prefab's default lifetime.
        /// </summary>
        [Tooltip("Override default bullet lifetime (seconds). <= 0 uses prefab default.")]
        public float lifetime = -1f;

        // Potential future additions: Target acquisition parameters (e.g., target tag, closest player), bullet lifetime override, sound effects per action.
    }

    /// <summary>
    /// A <see cref="ScriptableObject"/> asset that defines a complete spellcard.
    /// Contains the required charge level and a sequence of <see cref="SpellcardAction"/> steps
    /// that are executed in order when the spellcard is activated (handled by <see cref="TouhouWebArena.PlayerShooting"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpellcard", menuName = "TouhouWebArena/Spellcard Data")]
    public class SpellcardData : ScriptableObject
    {
        /// <summary>
        /// The minimum player charge level (e.g., 2, 3, 4) required to activate this specific spellcard.
        /// </summary>
        [Tooltip("The minimum player charge level (e.g., 2, 3, 4) required to activate this specific spellcard.")]
        public int requiredChargeLevel = 2;

        // Potential field for character association if needed later
        // public Character associatedCharacter;

        /// <summary>
        /// The sequence of actions (bullet patterns, timings, behaviors) performed when this spellcard is executed.
        /// </summary>
        [Tooltip("The sequence of actions (bullet patterns, timings, behaviors) performed when this spellcard is executed.")]
        public List<SpellcardAction> actions;

        // Potential future additions: Overall duration, activation sound effect, background visual effect, required character.
    }
}
