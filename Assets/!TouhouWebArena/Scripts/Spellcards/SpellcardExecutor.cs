using UnityEngine;
using Unity.Netcode;
using System.Collections; // Keep for potential future coroutines
using TouhouWebArena.Spellcards.Behaviors; // Required for behavior scripts
// Required for NetworkObjectPool (Removed as it's in global namespace)
using System.Collections.Generic; // Required for List

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// Handles the execution of spellcard patterns based on SpellcardData.
    /// DEPRECATED: This script originally handled client-side spellcard execution.
    /// All spellcard spawning and execution logic is now handled server-side within
    /// <see cref="TouhouWebArena.PlayerShooting.RequestSpellcardServerRpc(int, ServerRpcParams)"/> and its helper methods/coroutines.
    /// This class currently exists mainly as a potential component holder or for legacy reference.
    /// It does not contain active spellcard execution logic.
    /// </summary>
    public class SpellcardExecutor : MonoBehaviour // Keep MonoBehaviour if other logic might be added
    {
        // Remove pool and opponent references, as they aren't used for client execution anymore
        // private NetworkObjectPool pool;
        // private Transform opponentPlayerTransform;

        /* // Comment out original FindOpponentPlayer 
        void FindOpponentPlayer()
        {
            var players = FindObjectsOfType<CharacterStats>(); 
            foreach (var player in players)
            {
                if (!player.IsOwner) 
                {
                    opponentPlayerTransform = player.transform;
                    Debug.Log("SpellcardExecutor found opponent target: " + opponentPlayerTransform.name);
                    return;
                }
            }
            Debug.LogWarning("SpellcardExecutor could not find opponent player transform!");
        }
        */

        /* // Comment out client-side execution logic
        public void ExecuteSpellcard(SpellcardData spellcardData, Vector3 originPosition, Quaternion originRotation)
        {
           // ... original logic ... 
            foreach (SpellcardAction action in spellcardData.actions)
            {
                StartCoroutine(ExecuteActionCoroutine(action, originPosition, originRotation));
            }
        }

        private IEnumerator ExecuteActionCoroutine(SpellcardAction action, Vector3 originPosition, Quaternion originRotation)
        {
           // ... original coroutine logic ...
        }
        */

        /* // Remove the ClientRpc
        [ClientRpc]
        public void ExecuteSpellcardClientRpc(string spellcardResourcePath, Vector3 originPosition, Quaternion originRotation, ClientRpcParams clientRpcParams = default)
        {
           // ... original RPC logic ...
            ExecuteSpellcard(spellcardData, originPosition, originRotation);
        }
        */

        // Keep the class shell in case it's needed for other spellcard-related functionality later

        Transform FindOpponentPlayerTransform()
        {
            var players = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None); 
            foreach (var player in players)
            {
                if (!player.IsOwner) 
                {
                    return player.transform;
                }
            }
            Debug.LogWarning("SpellcardExecutor could not find opponent player transform!");
            return null;
        }
    }
} 