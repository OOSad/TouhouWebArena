using UnityEngine;
using System.Collections.Generic;

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Represents a group of SpellcardActions that are executed together as a single attack unit,
    /// potentially aimed as a whole towards the target.
    /// Used within the Level 4 spellcard's attack pool.
    /// </summary>
    [System.Serializable] // Make it visible and editable in the Inspector
    public class CompositeAttackPattern
    {
        [Tooltip("Optional name for easier identification in the inspector.")]
        public string patternName = "New Attack Pattern";

        [Tooltip("If true, the entire pattern (all its actions) will be rotated to face the target player when executed.")]
        public bool orientPatternTowardsTarget = false;

        [Tooltip("The sequence of actions performed as part of this composite attack. Delays within actions are relative to the start of this pattern's execution.")]
        public List<SpellcardAction> actions = new List<SpellcardAction>();
    }
} 