using UnityEngine;
using System.Collections.Generic;

public class MapVisualizer
{
    private MapVisualizationSettings settings;
    private MapGenerationResult mapResult;
    private RoomConnectionResult connectionResult;
    private CorridorGenerationResult corridorResult;

    private GameObject roomFloorPrefab;
    private GameObject corridorFloorPrefab;
    private GameObject roomCeilingPrefab;
    private GameObject corridorCeilingPrefab;
    private GameObject roomWallStraightPrefab;
    private GameObject corridorWallStraightPrefab;
    private GameObject roomDoorwayPrefab;

    private const int EMPTY_TILE = 0;
    private const int CORRIDOR_TILE = -1;

    private readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // North
        new Vector2Int(1, 0),   // East
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, 0)   // West
    };

    private readonly float[] wallRotations = new float[]
    {
        0f,    // North
        90f,   // East
        180f,  // South
        270f   // West
    };

    public MapVisualizationResult Visualize(
        MapVisualizationSettings settings,
        MapGenerationResult mapResult,
        RoomConnectionResult connectionResult,
        CorridorGenerationResult corridorResult)
    {
        this.settings = settings;
        this.mapResult = mapResult;
        this.connectionResult = connectionResult;
        this.corridorResult = corridorResult;

        if (settings == null)
        {
            Debug.LogError("MapVisualizationSettings is null");
            return null;
        }

        if (mapResult == null)
        {
            Debug.LogError("MapGenerationResult is null");
            return null;
        }

        MapVisualizationResult result = new MapVisualizationResult();

        Debug.Log("Starting 3D map visualization...");

        CreateMapParent(result);

        LoadPrefabs();

        // Floor generation
        if (settings.generateRoomFloors)
        {
            GenerateFloors(result);
        }

        // Ceiling generation
        if (settings.generateCeilings)
        {
            GenerateCeilings(result);
        }

        // Room walls generation
        if (settings.generateRoomWalls)
        {
            GenerateRoomWalls(result);
        }

        // Corridor walls generation
        if (settings.generateCorridorWalls)
        {
            GenerateCorridorWalls(result);
        }

        Debug.Log($"✓ Map visualization complete: {result.totalObjectsCreated} objects created");

        return result;
    }

    private void CreateMapParent(MapVisualizationResult result)
    {
        Transform parent = settings.mapParent;

        if (parent == null && settings.autoCreateParent)
        {
            GameObject mapRoot = new GameObject("GeneratedMap");
            parent = mapRoot.transform;
            Debug.Log("Created map root object: GeneratedMap");
        }

        if (parent == null)
        {
            Debug.LogWarning("No map parent specified and autoCreateParent is disabled");
            return;
        }

        result.mapRootObject = parent;
    }

    private void LoadPrefabs()
    {
        roomFloorPrefab = Resources.Load<GameObject>(settings.GetRoomFloorPath());
        corridorFloorPrefab = Resources.Load<GameObject>(settings.GetCorridorFloorPath());
        roomCeilingPrefab = Resources.Load<GameObject>(settings.GetRoomCeilingPath());
        corridorCeilingPrefab = Resources.Load<GameObject>(settings.GetCorridorCeilingPath());
        roomWallStraightPrefab = Resources.Load<GameObject>(settings.GetRoomWallStraightPath());
        corridorWallStraightPrefab = Resources.Load<GameObject>(settings.GetCorridorWallStraightPath());
        roomDoorwayPrefab = Resources.Load<GameObject>(settings.GetRoomDoorwayPath());

        if (settings.showDebugInfo)
        {
            Debug.Log($"Loaded prefabs: " +
                     $"RoomFloor={roomFloorPrefab != null}, " +
                     $"CorridorFloor={corridorFloorPrefab != null}, " +
                     $"RoomCeiling={roomCeilingPrefab != null}, " +
                     $"CorridorCeiling={corridorCeilingPrefab != null}, " +
                     $"RoomWall={roomWallStraightPrefab != null}, " +
                     $"CorridorWall={corridorWallStraightPrefab != null}, " +
                     $"Doorway={roomDoorwayPrefab != null}");
        }
    }

    private void GenerateFloors(MapVisualizationResult result)
    {
        if (mapResult.grid == null)
        {
            Debug.LogError("Map grid is null, cannot generate floors");
            return;
        }

        GameObject floorsParent = new GameObject("Floors");
        floorsParent.transform.SetParent(result.mapRootObject);
        floorsParent.transform.localPosition = Vector3.zero;

        int roomFloorCount = 0;
        int corridorFloorCount = 0;

        for (int x = 0; x < mapResult.mapSize.x; x++)
        {
            for (int y = 0; y < mapResult.mapSize.y; y++)
            {
                int cellValue = mapResult.grid[x, y];

                if (cellValue == CORRIDOR_TILE && corridorFloorPrefab != null)
                {
                    GameObject floorTile = CreateTile(corridorFloorPrefab, x, y, 0, floorsParent.transform, "CorridorFloor");
                    if (floorTile != null)
                    {
                        result.AddCorridorFloorObject(floorTile);
                        corridorFloorCount++;
                    }
                }
                else if (cellValue > 0 && roomFloorPrefab != null)
                {
                    GameObject floorTile = CreateTile(roomFloorPrefab, x, y, 0, floorsParent.transform, "RoomFloor");
                    if (floorTile != null)
                    {
                        result.AddRoomFloorObject(floorTile);
                        roomFloorCount++;
                    }
                }
            }
        }

        Debug.Log($"Floor generation: {roomFloorCount} room tiles, {corridorFloorCount} corridor tiles");
    }

    private void GenerateCeilings(MapVisualizationResult result)
    {
        if (mapResult.grid == null)
        {
            Debug.LogError("Map grid is null, cannot generate ceilings");
            return;
        }

        GameObject ceilingsParent = new GameObject("Ceilings");
        ceilingsParent.transform.SetParent(result.mapRootObject);
        ceilingsParent.transform.localPosition = Vector3.zero;

        int roomCeilingCount = 0;
        int corridorCeilingCount = 0;

        for (int x = 0; x < mapResult.mapSize.x; x++)
        {
            for (int y = 0; y < mapResult.mapSize.y; y++)
            {
                int cellValue = mapResult.grid[x, y];

                if (cellValue == CORRIDOR_TILE && corridorCeilingPrefab != null)
                {
                    int heightLevel = settings.corridorWallHeight;
                    GameObject ceilingTile = CreateTile(corridorCeilingPrefab, x, y, heightLevel, ceilingsParent.transform, "CorridorCeiling");
                    if (ceilingTile != null)
                    {
                        result.AddCeilingObject(ceilingTile);
                        corridorCeilingCount++;
                    }
                }
                else if (cellValue > 0 && roomCeilingPrefab != null)
                {
                    int heightLevel = settings.roomWallHeight;
                    GameObject ceilingTile = CreateTile(roomCeilingPrefab, x, y, heightLevel, ceilingsParent.transform, "RoomCeiling");
                    if (ceilingTile != null)
                    {
                        result.AddCeilingObject(ceilingTile);
                        roomCeilingCount++;
                    }
                }
            }
        }

        Debug.Log($"Ceiling generation: {roomCeilingCount} room tiles, {corridorCeilingCount} corridor tiles");
    }

    private void GenerateRoomWalls(MapVisualizationResult result)
    {
        if (mapResult.grid == null)
        {
            Debug.LogError("Map grid is null, cannot generate room walls");
            return;
        }

        if (roomWallStraightPrefab == null)
        {
            Debug.LogWarning("Room wall prefab is null, skipping room wall generation");
            return;
        }

        GameObject roomWallsParent = new GameObject("RoomWalls");
        roomWallsParent.transform.SetParent(result.mapRootObject);
        roomWallsParent.transform.localPosition = Vector3.zero;

        int wallCount = 0;

        // Get all exit tiles from connections
        HashSet<Vector2Int> exitTiles = GetAllExitTiles();

        for (int x = 0; x < mapResult.mapSize.x; x++)
        {
            for (int y = 0; y < mapResult.mapSize.y; y++)
            {
                int cellValue = mapResult.grid[x, y];

                if (cellValue <= 0) continue;

                Vector2Int currentPos = new Vector2Int(x, y);

                // Check all directions
                for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
                {
                    Vector2Int dir = directions[dirIndex];
                    Vector2Int neighborPos = currentPos + dir;

                    // Check: should place wall in this direction
                    if (ShouldPlaceRoomWall(currentPos, neighborPos, exitTiles, out bool isDoorway))
                    {
                        GameObject wallPrefab = isDoorway ? roomDoorwayPrefab : roomWallStraightPrefab;

                        if (wallPrefab != null)
                        {
                            int wallHeight = settings.roomWallHeight;

                            if (isDoorway)
                            {
                                // Calculating doorway height
                                int doorwayHeight = settings.GetDoorwayHeight();

                                // Generating doorway
                                for (int h = 0; h < doorwayHeight; h++)
                                {
                                    GameObject doorway = CreateWallTile(
                                        wallPrefab,
                                        x, y, h,
                                        wallRotations[dirIndex],
                                        roomWallsParent.transform,
                                        "RoomDoorway"
                                    );

                                    if (doorway != null)
                                    {
                                        result.AddWallObject(doorway);
                                        wallCount++;
                                    }
                                }

                                // Generate wall ontop of doorway
                                if (roomWallStraightPrefab != null)
                                {
                                    for (int h = doorwayHeight; h < wallHeight; h++)
                                    {
                                        GameObject wall = CreateWallTile(
                                            roomWallStraightPrefab,
                                            x, y, h,
                                            wallRotations[dirIndex],
                                            roomWallsParent.transform,
                                            "RoomWall"
                                        );

                                        if (wall != null)
                                        {
                                            result.AddWallObject(wall);
                                            wallCount++;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Generate wall
                                for (int h = 0; h < wallHeight; h++)
                                {
                                    GameObject wall = CreateWallTile(
                                        wallPrefab,
                                        x, y, h,
                                        wallRotations[dirIndex],
                                        roomWallsParent.transform,
                                        "RoomWall"
                                    );

                                    if (wall != null)
                                    {
                                        result.AddWallObject(wall);
                                        wallCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"Room wall generation: {wallCount} wall tiles");
    }

    /// <summary>
    /// Corridor wall generation
    /// </summary>
    private void GenerateCorridorWalls(MapVisualizationResult result)
    {
        if (mapResult.grid == null)
        {
            Debug.LogError("Map grid is null, cannot generate corridor walls");
            return;
        }

        if (corridorWallStraightPrefab == null)
        {
            Debug.LogWarning("Corridor wall prefab is null, skipping corridor wall generation");
            return;
        }

        GameObject corridorWallsParent = new GameObject("CorridorWalls");
        corridorWallsParent.transform.SetParent(result.mapRootObject);
        corridorWallsParent.transform.localPosition = Vector3.zero;

        int wallCount = 0;

        HashSet<Vector2Int> intersectionTiles = GetCorridorIntersections();

        for (int x = 0; x < mapResult.mapSize.x; x++)
        {
            for (int y = 0; y < mapResult.mapSize.y; y++)
            {
                int cellValue = mapResult.grid[x, y];

                if (cellValue != CORRIDOR_TILE) continue;

                Vector2Int currentPos = new Vector2Int(x, y);

                for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
                {
                    Vector2Int dir = directions[dirIndex];
                    Vector2Int neighborPos = currentPos + dir;

                    if (ShouldPlaceCorridorWall(currentPos, neighborPos, intersectionTiles))
                    {
                        int wallHeight = settings.corridorWallHeight;

                        for (int h = 0; h < wallHeight; h++)
                        {
                            GameObject wall = CreateWallTile(
                                corridorWallStraightPrefab,
                                x, y, h,
                                wallRotations[dirIndex],
                                corridorWallsParent.transform,
                                "CorridorWall"
                            );

                            if (wall != null)
                            {
                                result.AddWallObject(wall);
                                wallCount++;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"Corridor wall generation: {wallCount} wall tiles");
    }

    /// <summary>
    /// Check: should place room wall in the specific direction
    /// </summary>
    private bool ShouldPlaceRoomWall(Vector2Int currentPos, Vector2Int neighborPos, HashSet<Vector2Int> exitTiles, out bool isDoorway)
    {
        isDoorway = false;

        // Check map bounds
        if (!IsInBounds(neighborPos))
        {
            return true;
        }

        int neighborValue = mapResult.grid[neighborPos.x, neighborPos.y];

        // Neighbour tile is empty tile
        if (neighborValue == EMPTY_TILE)
        {
            return true;
        }

        // Neighbour tile is another room
        int currentValue = mapResult.grid[currentPos.x, currentPos.y];
        if (neighborValue > 0 && neighborValue != currentValue)
        {
            return true;
        }

        // Neighbour tile is corridor tile
        if (neighborValue == CORRIDOR_TILE)
        {
            if (exitTiles.Contains(neighborPos))
            {
                if (IsExitTileForRoom(currentPos, neighborPos))
                {
                    isDoorway = true;
                    return true;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Check: should place corridor wall in the specific direction
    /// </summary>
    private bool ShouldPlaceCorridorWall(Vector2Int currentPos, Vector2Int neighborPos, HashSet<Vector2Int> intersectionTiles)
    {
        if (!IsInBounds(neighborPos))
        {
            return true;
        }

        int neighborValue = mapResult.grid[neighborPos.x, neighborPos.y];

        if (neighborValue == EMPTY_TILE)
        {
            return true;
        }

        if (neighborValue > 0)
        {
            if (IsCorridorExitTile(currentPos))
            {
                return false;
            }

            return true;
        }

        if (neighborValue == CORRIDOR_TILE)
        {
            if (intersectionTiles.Contains(currentPos))
            {
                return false;
            }

            return false;
        }

        return false;
    }

    private bool IsCorridorExitTile(Vector2Int corridorTile)
    {
        if (connectionResult == null || connectionResult.allConnections == null)
            return false;

        foreach (var connection in connectionResult.allConnections)
        {
            if (corridorTile == connection.exitTile1 || corridorTile == connection.exitTile2)
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<Vector2Int> GetAllExitTiles()
    {
        HashSet<Vector2Int> exitTiles = new HashSet<Vector2Int>();

        if (connectionResult == null || connectionResult.allConnections == null)
            return exitTiles;

        foreach (var connection in connectionResult.allConnections)
        {
            exitTiles.Add(connection.exitTile1);
            exitTiles.Add(connection.exitTile2);
        }

        return exitTiles;
    }

    private bool IsExitTileForRoom(Vector2Int roomTile, Vector2Int corridorTile)
    {
        if (connectionResult == null || connectionResult.allConnections == null)
            return false;

        int roomId = mapResult.grid[roomTile.x, roomTile.y];

        foreach (var connection in connectionResult.allConnections)
        {
            // Check room 1
            if (connection.room1Index + 1 == roomId && IsAdjacent(roomTile, connection.exitTile1))
            {
                return corridorTile == connection.exitTile1;
            }

            // Check room 2
            if (connection.room2Index + 1 == roomId && IsAdjacent(roomTile, connection.exitTile2))
            {
                return corridorTile == connection.exitTile2;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all corridor intersections
    /// </summary>
    private HashSet<Vector2Int> GetCorridorIntersections()
    {
        HashSet<Vector2Int> intersections = new HashSet<Vector2Int>();

        if (corridorResult == null || corridorResult.intersections == null)
            return intersections;

        foreach (var intersection in corridorResult.intersections)
        {
            intersections.Add(intersection);
        }

        return intersections;
    }

    private bool IsAdjacent(Vector2Int tile1, Vector2Int tile2)
    {
        int dx = Mathf.Abs(tile1.x - tile2.x);
        int dy = Mathf.Abs(tile1.y - tile2.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    private GameObject CreateTile(GameObject prefab, int gridX, int gridY, int heightLevel, Transform parent, string namePrefix)
    {
        if (prefab == null) return null;

        Vector3 position = GridToWorldPosition(gridX, gridY, heightLevel);
        GameObject tile = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        tile.name = $"{namePrefix}_{gridX}_{gridY}_H{heightLevel}";
        tile.transform.localScale = Vector3.one * settings.tileScale;

        return tile;
    }

    private GameObject CreateWallTile(GameObject prefab, int gridX, int gridY, int heightLevel, float rotationY, Transform parent, string namePrefix)
    {
        if (prefab == null) return null;

        Vector3 position = GridToWorldPosition(gridX, gridY, heightLevel);
        Quaternion rotation = Quaternion.Euler(0, rotationY, 0);

        GameObject wall = Object.Instantiate(prefab, position, rotation, parent);
        wall.name = $"{namePrefix}_{gridX}_{gridY}_H{heightLevel}_R{rotationY}";
        wall.transform.localScale = Vector3.one * settings.tileScale;

        return wall;
    }

    private Vector3 GridToWorldPosition(int gridX, int gridY, int heightLevel)
    {
        float worldX = (gridX + 0.5f) * settings.tileScale;
        float worldY = heightLevel * settings.tileScale;
        float worldZ = (gridY + 0.5f) * settings.tileScale;

        return new Vector3(worldX, worldY, worldZ);
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapResult.mapSize.x &&
               pos.y >= 0 && pos.y < mapResult.mapSize.y;
    }
}