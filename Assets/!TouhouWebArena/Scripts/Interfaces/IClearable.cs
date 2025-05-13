// using TouhouWebArena.Networking; // Assuming PlayerRole is here - REMOVED

namespace TouhouWebArena
{
    /// <summary>
    /// Interface for objects that can be cleared by effects like bombs or shockwaves.
    /// Implementers must define how they react to being cleared, potentially considering
    /// whether it's a forced clear (ignores normal rules) or a normal clear.
    /// </summary>
    public interface IClearable
    {
        /// <summary>
        /// Called by a clearing effect (bomb, shockwave) to attempt to clear this object.
        /// Implementation should handle server-authoritative logic if necessary (like in NetworkBulletLifetime),
        /// or client-side logic if appropriate (like in StageSmallBulletMoverScript).
        /// </summary>
        /// <param name="forceClear">If true, the object should be cleared regardless of its normal clearability rules (e.g., player death bomb). 
        /// If false, the object should only clear if it's designated as normally clearable (e.g., standard enemy shockwave).</param>
        /// <param name="sourceRole">The role of the player initiating the clear, or PlayerRole.None for environmental effects.</param>
        /// <returns>True if the object was successfully cleared, false otherwise.</returns>
        bool Clear(bool forceClear, PlayerRole sourceRole);
    }
} 