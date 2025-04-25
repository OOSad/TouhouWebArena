using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Defines the data for a Level 4 spellcard, which typically involves
    /// spawning a persistent character representation (illusion) on the opponent's screen
    /// that moves and executes complex attack patterns.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterLevel4Spellcard", menuName = "TouhouWebArena/Level 4 Spellcard Data")]
    public class Level4SpellcardData : ScriptableObject
    {
        [Header("Illusion Appearance & Lifetime")]
        [Tooltip("The prefab representing the character illusion. Must have NetworkObject, NetworkTransform, Level4IllusionController, and IllusionHealth components.")]
        [SerializeField] private GameObject illusionPrefab;

        [Tooltip("How long the illusion persists on the opponent's screen (in seconds).")]
        [SerializeField] private float duration = 15f;

        [Header("Illusion Health & Interaction")]
        [Tooltip("Total health points of the illusion. Can be shot down by the opponent.")]
        [SerializeField] private float health = 75f; // Default health

        [Header("Illusion Movement")]
        [Tooltip("How far down from the top boundary the illusion can move (e.g., 2 units).")]
        [SerializeField] private float movementAreaHeight = 2.0f;
        [Tooltip("Minimum time (seconds) between random movements.")]
        [SerializeField] private float minMoveDelay = 2.0f; // Adjusted default
        [Tooltip("Maximum time (seconds) between random movements.")]
        [SerializeField] private float maxMoveDelay = 3.5f; // Adjusted default

        [Header("Illusion Attacks")]
        [Tooltip("The pool of possible composite attack patterns the illusion can perform after moving.")]
        [SerializeField] private List<CompositeAttackPattern> attackPool = new List<CompositeAttackPattern>();
        [Tooltip("How many composite patterns are randomly selected from the pool and executed each time the illusion moves.")]
        [SerializeField] private int attacksPerMove = 1;

        // --- Getters ---
        public GameObject IllusionPrefab => illusionPrefab;
        public float Duration => duration;
        public float Health => health;
        public float MovementAreaHeight => movementAreaHeight;
        public float MinMoveDelay => minMoveDelay;
        public float MaxMoveDelay => maxMoveDelay;
        public List<CompositeAttackPattern> AttackPool => attackPool;
        public int AttacksPerMove => attacksPerMove;
    }
} 