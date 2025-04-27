using UnityEngine;
using System.Collections.Generic;

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Represents a group of SpellcardActions that are executed together as a single attack unit,
    /// potentially aimed as a whole towards the target, and potentially including movement of the illusion.
    /// Used within the Level 4 spellcard's attack pool.
    /// </summary>
    [System.Serializable] // Make it visible and editable in the Inspector
    public class CompositeAttackPattern
    {
        [Tooltip("Optional name for easier identification in the inspector.")]
        public string patternName = "New Attack Pattern";

        [Tooltip("If true, the entire pattern (all its actions) will be rotated to face the target player when executed.")]
        public bool orientPatternTowardsTarget = false;

        [Header("Movement During Attack (Optional)")]
        [Tooltip("If true, the illusion will perform a short movement while executing this pattern.")]
        public bool performMovementDuringAttack = false;
        [Tooltip("The direction and distance the illusion moves relative to its starting position during the attack.")]
        public Vector2 attackMovementVector = Vector2.zero;
        [Tooltip("How long (in seconds) the movement takes to complete.")]
        public float attackMovementDuration = 1.0f;

        [Header("Attack Actions")]
        [Tooltip("The sequence of actions performed as part of this composite attack. Delays within actions are relative to the start of this pattern's execution.")]
        public List<SpellcardAction> actions = new List<SpellcardAction>();
    }
} 