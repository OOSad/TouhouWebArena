using UnityEngine;
using System.Collections.Generic;
using TouhouWebArena.Spellcards.Behaviors; // For BehaviorType enum

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Defines a single action within a spellcard pattern, typically involving spawning bullets.
    /// Configurable for various formations, behaviors, and timing.
    /// </summary>
    [System.Serializable] // Essential for viewing/editing in the Inspector
    public class SpellcardAction
    {
        [Header("Timing")]
        [Tooltip("Seconds to wait before this Action begins, relative to the start of the pattern it belongs to (e.g., CompositeAttackPattern).")]
        public float startDelay = 0f;

        [Header("Spawning")]
        [Tooltip("Bullet prefab(s) to spawn. Cycles if multiple are provided.")]
        public List<GameObject> bulletPrefabs = new List<GameObject>();
        [Tooltip("Offset relative to the pattern origin point.")]
        public Vector2 positionOffset = Vector2.zero;
        [Tooltip("Number of bullets to spawn.")]
        public int count = 1;

        [Header("Formation Shape")]
        [Tooltip("The arrangement of spawned bullets (Point, Circle, Line).")]
        public FormationType formation = FormationType.Point;
        [Tooltip("Radius used for Circle formation.")]
        public float radius = 1f;
        [Tooltip("Spacing between bullets for Line formation.")]
        public float spacing = 0.5f;
        [Tooltip("Base angle (degrees) for Point/Line formation, or angular offset for Circle. This is relative to the pattern's overall rotation.")]
        public float angle = 0f;
        // Note: Aiming ('aimAtTarget' flag) was removed; handled by CompositeAttackPattern.orientPatternTowardsTarget now.

        [Header("Bullet Behavior")]
        [Tooltip("The movement behavior applied to the spawned bullets (Linear, DelayedHoming, Spiral, etc.).")]
        public BehaviorType behavior = BehaviorType.Linear;

        [Header("Behavior Speeds")]
        [Tooltip("Target/final speed for Linear, initial outward speed for DelayedHoming/Spiral. For Line formations, this is the speed of the first bullet.")]
        public float speed = 5f;
        [Tooltip("(Line Only) Amount speed increases for each subsequent bullet in the line.")]
        public float speedIncrementPerBullet = 0f;
        [Tooltip("(Homing Behaviors) Speed when actively homing.")]
        public float homingSpeed = 4f;
        [Tooltip("(Spiral Behavior Only) Speed moving tangentially (sideways). Sign determines direction.")]
        public float tangentialSpeed = 1.0f;

        [Header("Initial Speed Transition (Optional)")]
        [Tooltip("Enable to make the bullet start at Initial Speed and transition to Speed over Transition Duration.")]
        public bool useInitialSpeed = false;
        [Tooltip("The speed the bullet starts at if Use Initial Speed is enabled.")]
        public float initialSpeed = 1f;
        [Tooltip("Duration (seconds) over which the bullet transitions from Initial Speed to Speed. 0 = instant.")]
        public float speedTransitionDuration = 0.5f;

        [Header("Behavior Timing & Parameters")]
        [Tooltip("(Delayed/Double Homing/Delayed Turn) Delay before homing/turning starts.")]
        public float homingDelay = 0.5f;
        [Tooltip("(Double Homing Only) Duration of the pause between first and second homing phases.")]
        public float secondHomingDelay = 0.2f;
        [Tooltip("(Double Homing Only) Duration of the first homing phase.")]
        public float firstHomingDuration = 1.0f;
        [Tooltip("(Double Homing Only) Look-ahead distance used for the second homing phase target calculation.")]
        public float secondHomingLookAheadDistance = 5.0f;
        [Tooltip("(Delayed Random Turn Only) Max angle offset (degrees) from base rotation for initial random spread.")]
        public float spreadAngle = 45f;
        [Tooltip("(Delayed Random Turn Only) Minimum angular speed (degrees/sec) for the random turn.")]
        public float minTurnSpeed = 90f;
        [Tooltip("(Delayed Random Turn Only) Maximum angular speed (degrees/sec) for the random turn.")]
        public float maxTurnSpeed = 270f;

        [Header("Spawning & Formation Modifiers")]
        [Tooltip("If > 0, skips spawning every Nth bullet in a formation (e.g., set to 4 to skip every 4th bullet).")]
        public int skipEveryNth = 0;

        [Header("Timing & Lifetime")]
        [Tooltip("Seconds to wait between spawning each individual bullet within this action. Set to 0 for simultaneous spawn.")]
        public float intraActionDelay = 0f;
        [Tooltip("Overrides the default lifetime of the spawned bullet prefab (seconds). <= 0 uses prefab default.")]
        public float lifetime = 0f;
    }
} 