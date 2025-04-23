using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton registry to track active fairies for line-based chaining.
/// Stores references to active <see cref="Fairy"/> instances and provides
/// methods to register, deregister, and find fairies based on their line ID and index.
/// </summary>
public class FairyRegistry : MonoBehaviour
{
    /// <summary>
    /// Gets the singleton instance of the FairyRegistry.
    /// </summary>
    public static FairyRegistry Instance { get; private set; }

    // Using a list and LINQ for simplicity. For very high fairy counts, 
    // a Dictionary<Guid, List<Fairy>> might be more performant for FindByLine,
    // but requires managing list creation/removal.
    private readonly List<Fairy> activeFairies = new List<Fairy>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate FairyRegistry instance found, destroying self.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers a <see cref="Fairy"/> instance with the registry.
    /// </summary>
    /// <param name="fairy">The fairy instance to register. Ignored if null or already registered.</param>
    public void Register(Fairy fairy)
    {
        if (fairy != null && !activeFairies.Contains(fairy))
        {
            activeFairies.Add(fairy);
        }
    }

    /// <summary>
    /// Deregisters a <see cref="Fairy"/> instance from the registry.
    /// </summary>
    /// <param name="fairy">The fairy instance to deregister. Ignored if null.</param>
    public void Deregister(Fairy fairy)
    {
        if (fairy != null)
        {
            activeFairies.Remove(fairy);
        }
    }

    /// <summary>
    /// Finds the next <see cref="Fairy"/> in the same line formation based on its index.
    /// </summary>
    /// <param name="lineId">The unique identifier of the fairy line.</param>
    /// <param name="currentIndex">The index of the current fairy within the line.</param>
    /// <returns>The next Fairy in the line, or null if none is found.</returns>
    public Fairy FindNextInLine(System.Guid lineId, int currentIndex)
    {
        int nextIndex = currentIndex + 1;
        // Use LINQ to find the fairy efficiently
        return activeFairies.FirstOrDefault(f => f.GetLineId() == lineId && f.GetIndexInLine() == nextIndex);
    }

    /// <summary>
    /// Finds all active <see cref="Fairy"/> instances belonging to a specific line.
    /// </summary>
    /// <param name="lineId">The unique identifier of the fairy line.</param>
    /// <returns>A list of fairies belonging to the specified line.</returns>
    public List<Fairy> FindByLine(System.Guid lineId)
    {
        return activeFairies.Where(f => f.GetLineId() == lineId).ToList();
    }

    /// <summary>
    /// Gets the current number of active <see cref="Fairy"/> instances registered.
    /// Useful for debugging.
    /// </summary>
    /// <returns>The count of active fairies.</returns>
    public int GetActiveCount()
    {
        return activeFairies.Count;
    }
} 