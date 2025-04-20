// Interface for objects that can be cleared by the player's death bomb
public interface IClearableByBomb
{
    /// <summary>
    /// Clears the object due to a player's death bomb.
    /// </summary>
    /// <param name="bombingPlayer">The role of the player who activated the bomb.</param>
    void ClearByBomb(PlayerRole bombingPlayer);
} 