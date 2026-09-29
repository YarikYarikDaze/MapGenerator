using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CorridorGenerationResult
{
    public List<Corridor> corridors;
    public List<Vector2Int> intersections; // Corridor intersection points
    public int seed;
    public int successfulCorridors;
    public int failedCorridors;

    public CorridorGenerationResult(int seed)
    {
        this.corridors = new List<Corridor>();
        this.intersections = new List<Vector2Int>();
        this.seed = seed;
        this.successfulCorridors = 0;
        this.failedCorridors = 0;
    }

    public void AddCorridor(Corridor corridor)
    {
        corridors.Add(corridor);
        successfulCorridors++;
    }

    public void RegisterFailure()
    {
        failedCorridors++;
    }

    /// <summary>
    /// Check: is tile in one of corridors
    /// </summary>
    public bool IsCorridorTile(Vector2Int tile)
    {
        foreach (var corridor in corridors)
        {
            if (corridor.tiles.Contains(tile))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Find all corridor intersections
    /// </summary>
    public void CalculateIntersections()
    {
        intersections.Clear();
        Dictionary<Vector2Int, int> tileCount = new Dictionary<Vector2Int, int>();

        foreach (var corridor in corridors)
        {
            foreach (var tile in corridor.tiles)
            {
                if (!tileCount.ContainsKey(tile))
                    tileCount[tile] = 0;
                tileCount[tile]++;
            }
        }

        foreach (var kvp in tileCount)
        {
            if (kvp.Value > 1)
            {
                intersections.Add(kvp.Key);
            }
        }
    }
}