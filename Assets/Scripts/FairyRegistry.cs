using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Singleton registry to track active fairies for line-based chaining.
public class FairyRegistry : MonoBehaviour
{
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

    public void Register(Fairy fairy)
    {
        if (fairy != null && !activeFairies.Contains(fairy))
        {
            activeFairies.Add(fairy);
        }
    }

    public void Deregister(Fairy fairy)
    {
        if (fairy != null)
        {
            activeFairies.Remove(fairy);
        }
    }

    // Find the next fairy in the same line
    public Fairy FindNextInLine(System.Guid lineId, int currentIndex)
    {
        int nextIndex = currentIndex + 1;
        // Use LINQ to find the fairy efficiently
        return activeFairies.FirstOrDefault(f => f.GetLineId() == lineId && f.GetIndexInLine() == nextIndex);
    }

    // Optional: Method to find all fairies in a line (might be useful later)
    public List<Fairy> FindByLine(System.Guid lineId)
    {
        return activeFairies.Where(f => f.GetLineId() == lineId).ToList();
    }

    // Optional: Get current count for debugging
    public int GetActiveCount()
    {
        return activeFairies.Count;
    }
} 