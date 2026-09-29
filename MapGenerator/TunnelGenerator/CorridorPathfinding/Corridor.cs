using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Corridor
{
    public List<Vector2Int> tiles;
    public int connectionIndex; // Connection index from RoomConnectionResult
    public int room1Index;
    public int room2Index;
    public int iterationUsed;
    public Vector2Int startTile; // Exit tile 1
    public Vector2Int targetTile; // Exit tile 2
    public Vector2Int actualEndTile; // Actual tile where corridor ends(after generation)

    public Corridor(int connectionIndex, int room1, int room2, Vector2Int startTile, Vector2Int targetTile)
    {
        this.tiles = new List<Vector2Int>();
        this.connectionIndex = connectionIndex;
        this.room1Index = room1;
        this.room2Index = room2;
        this.startTile = startTile;
        this.targetTile = targetTile;
        this.actualEndTile = startTile;
        this.iterationUsed = 0;
    }

    public void AddTile(Vector2Int tile)
    {
        tiles.Add(tile);
    }

    public void UpdateActualEndTile(Vector2Int endTile)
    {
        this.actualEndTile = endTile;
    }
}