/// <summary>
/// Defines the contract for GameObjects (like bullets or certain enemies)
/// that can be destroyed or otherwise affected by a player's death bomb effect.
/// The <see cref="PlayerDeathBomb"/> script finds components implementing this interface
/// within its radius and calls <see cref="ClearByBomb"/>.
/// </summary>
public interface IClearableByBomb
{
    /// <summary>
    /// Method called by <see cref="PlayerDeathBomb"/> on the server when this object
    /// is within the bomb's radius and on the correct side of the playfield.
    /// Implementations should handle the object's destruction, return to pool, or other clearing logic.
    /// </summary>
    /// <param name="bombingPlayer">The <see cref="PlayerRole"/> of the player whose bomb triggered the clear.</param>
    void ClearByBomb(PlayerRole bombingPlayer);
} 