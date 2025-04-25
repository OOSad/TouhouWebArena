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
        DelayedHoming,
        /// <summary>Bullet performs DelayedHoming, pauses, then homes again after <see cref="SpellcardAction.secondHomingDelay"/>.</summary>
        DoubleHoming, // Added for Fantasy Seal
        /// <summary>Bullet moves radially outwards while also moving tangentially.</summary>
        Spiral // Added for Level 4
        // Add more complex behaviors here later (e.g., Wavy, Orbit, Accelerate)
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
