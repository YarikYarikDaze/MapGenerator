using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapGenerationResult
{
    public int[,] grid;
    public List<BSPRoom> rooms;
    public Vector2Int mapSize;
    public int seed;
    public System.DateTime generationTime;

    public MapGenerationResult(int width, int height, int seed)
    {
        grid = new int[width, height];
        rooms = new List<BSPRoom>();
        mapSize = new Vector2Int(width, height);
        this.seed = seed;
        generationTime = System.DateTime.Now;
    }
}
