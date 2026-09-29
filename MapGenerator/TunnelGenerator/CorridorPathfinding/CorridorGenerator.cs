using UnityEngine;
using System.Collections.Generic;

public class CorridorGenerator
{
    private CorridorGenerationSettings settings;
    private MapGenerationResult mapResult;
    private RoomConnectionResult connectionResult;
    private System.Random random;
    private CorridorGenerationResult result;

    // Exit tiles for current corridor
    private Vector2Int currentStartTile;
    private Vector2Int currentTargetTile;

    private readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // вверх
        new Vector2Int(1, 0),   // вправо
        new Vector2Int(0, -1),  // вниз
        new Vector2Int(-1, 0)   // влево
    };

    public CorridorGenerationResult Generate(
        CorridorGenerationSettings settings,
        MapGenerationResult mapResult,
        RoomConnectionResult connectionResult)
    {
        this.settings = settings;
        this.mapResult = mapResult;
        this.connectionResult = connectionResult;

        if (settings == null)
        {
            Debug.LogError("CorridorGenerationSettings is null");
            return null;
        }

        if (mapResult == null)
        {
            Debug.LogError("MapGenerationResult is null");
            return null;
        }

        if (mapResult.grid == null)
        {
            Debug.LogError("MapGenerationResult.grid is null");
            return null;
        }

        if (connectionResult == null || connectionResult.allConnections == null)
        {
            Debug.LogError("RoomConnectionResult or connections list is null");
            return null;
        }

        if (connectionResult.allConnections.Count == 0)
        {
            Debug.LogWarning("No connections to generate corridors for");
            return new CorridorGenerationResult(settings.seed == -1 ?
                UnityEngine.Random.Range(0, int.MaxValue) : settings.seed);
        }

        // Seed setting
        settings.actualSeed = settings.seed == -1 ?
            UnityEngine.Random.Range(0, int.MaxValue) : settings.seed;

        result = new CorridorGenerationResult(settings.actualSeed);

        Debug.Log($"Starting corridor generation with seed: {settings.actualSeed}");

        // Corridor generation for each connection
        for (int i = 0; i < connectionResult.allConnections.Count; i++)
        {
            var connection = connectionResult.allConnections[i];

            if (connection == null)
            {
                Debug.LogWarning($"Connection at index {i} is null, skipping");
                continue;
            }

            int corridorSeed = settings.actualSeed;

            GenerateCorridorForConnection(connection, i, corridorSeed);
        }

        // Calculate intersection
        result.CalculateIntersections();

        Debug.Log($"Corridor generation complete: {result.successfulCorridors} successful, " +
                  $"{result.failedCorridors} failed, {result.intersections.Count} intersections");

        WriteCorridorsToGrid();

        return result;
    }

    /// <summary>
    /// Corridor generation
    /// </summary>
    private void GenerateCorridorForConnection(RoomConnection connection, int connectionIndex, int baseSeed)
    {
        BSPRoom room1 = mapResult.rooms[connection.room1Index];
        BSPRoom room2 = mapResult.rooms[connection.room2Index];

        Vector2Int startTile = connection.exitTile1;
        Vector2Int targetTile = connection.exitTile2;

        // Attempting to construct a corridor using multiple iterations
        for (int iteration = 0; iteration < settings.maxIterations; iteration++)
        {
            random = new System.Random(baseSeed + iteration);

            // Setting current exit tiles
            currentStartTile = startTile;
            currentTargetTile = targetTile;

            Corridor corridor = TryBuildCorridor(
                startTile,
                targetTile,
                room1,
                room2,
                connectionIndex,
                iteration
            );

            if (corridor != null)
            {
                result.AddCorridor(corridor);
                Debug.Log($"Corridor {connectionIndex} built successfully on iteration {iteration + 1}. " +
                         $"Start: {startTile}, Target: {targetTile}, End: {corridor.actualEndTile}, Tiles: {corridor.tiles.Count}");
                return;
            }
        }

        // All iterations failed
        result.RegisterFailure();
        Debug.LogWarning($"Failed to build corridor {connectionIndex} after {settings.maxIterations} iterations. " +
                        $"Rooms {connection.room1Index} and {connection.room2Index} remain disconnected.");
    }

    /// <summary>
    /// Attempt to build the corridor
    /// </summary>
    private Corridor TryBuildCorridor(
        Vector2Int startTile,
        Vector2Int targetTile,
        BSPRoom startRoom,
        BSPRoom targetRoom,
        int connectionIndex,
        int iteration)
    {
        Corridor corridor = new Corridor(
            connectionIndex,
            mapResult.rooms.IndexOf(startRoom),
            mapResult.rooms.IndexOf(targetRoom),
            startTile,
            targetTile
        );

        Vector2 targetCenter = new Vector2(targetTile.x + 0.5f, targetTile.y + 0.5f);

        // Determine initial direction
        Vector2 startCenter = new Vector2(startTile.x + 0.5f, startTile.y + 0.5f);
        Vector2 toTarget = (targetCenter - startCenter).normalized;
        Vector2Int initialDirection = GetDirectionFromVector(toTarget);

        Vector2Int current = startTile;
        Vector2Int previousDirection = initialDirection;

        corridor.AddTile(startTile);

        int maxSteps = mapResult.mapSize.x + mapResult.mapSize.y;
        int steps = 0;

        while (steps < maxSteps)
        {
            steps++;

            // Check if current tile is exit tile
            if (current == targetTile)
            {
                corridor.UpdateActualEndTile(current);
                corridor.iterationUsed = iteration;
                return corridor; // успех!
            }

            // Choose next direction
            Vector2Int? nextDirection = ChooseNextDirection(
                current,
                targetTile,
                previousDirection
            );

            if (!nextDirection.HasValue)
            {
                return null; // no valid directions
            }

            previousDirection = nextDirection.Value;
            current += nextDirection.Value;

            // Check if the current position is valid
            if (!IsValidCorridorPosition(current))
            {
                return null;
            }

            corridor.AddTile(current);
        }

        Debug.LogWarning($"Corridor exceeded max steps ({maxSteps})");
        return null;
    }

    /// <summary>
    /// Convert a direction vector into one of the four cardinal directions
    /// </summary>
    private Vector2Int GetDirectionFromVector(Vector2 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absX > absY)
        {
            return direction.x > 0 ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
        }
        else
        {
            return direction.y > 0 ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
        }
    }

    /// <summary>
    /// Select the next direction of movement
    /// </summary>
    private Vector2Int? ChooseNextDirection(
        Vector2Int current,
        Vector2Int goalTile,
        Vector2Int previousDirection)
    {
        Vector2 goalCenter = new Vector2(goalTile.x + 0.5f, goalTile.y + 0.5f);
        Vector2 currentCenter = new Vector2(current.x + 0.5f, current.y + 0.5f);

        float straightDistance = float.MaxValue;
        Vector2Int straightDir = previousDirection;
        bool straightBlocked = false;

        List<Vector2Int> turnDirections = new List<Vector2Int>();
        List<float> turnDistances = new List<float>();
        List<bool> turnBlocked = new List<bool>();

        // Check all possible directions
        foreach (var dir in directions)
        {
            Vector2Int nextPos = current + dir;

            // Check validity of the direction
            if (!IsInBounds(nextPos))
            {
                if (dir == previousDirection)
                    straightBlocked = true;
                continue;
            }

            bool blocked = !IsValidCorridorPosition(nextPos);

            // calculate Manhattan distance
            Vector2 nextCenter = new Vector2(nextPos.x + 0.5f, nextPos.y + 0.5f);
            float distance = ManhattanDistance(nextCenter, goalCenter);

            if (dir == previousDirection)
            {
                straightDistance = distance;
                straightBlocked = blocked;
            }
            else
            {
                turnDirections.Add(dir);
                turnDistances.Add(distance);
                turnBlocked.Add(blocked);
            }
        }

        if (straightBlocked && turnDirections.Count == 0)
        {
            return null;
        }

        // Find best turn
        int bestTurnIndex = -1;
        float bestTurnDistance = float.MaxValue;

        for (int i = 0; i < turnDirections.Count; i++)
        {
            if (turnBlocked[i])
                continue;

            if (turnDistances[i] < bestTurnDistance)
            {
                bestTurnDistance = turnDistances[i];
                bestTurnIndex = i;
            }
        }

        // Logic for choosing the direction:

        // 1. If directly blocked, use the best turn
        if (straightBlocked)
        {
            if (bestTurnIndex != -1)
            {
                return turnDirections[bestTurnIndex];
            }
            return null;
        }

        // 2. If the best turn has a shorter distance than going straight, we turn
        if (bestTurnIndex != -1 && bestTurnDistance < straightDistance)
        {
            return turnDirections[bestTurnIndex];
        }

        // 3. If the best turn has equal distance with the straight
        if (bestTurnIndex != -1 && Mathf.Approximately(bestTurnDistance, straightDistance))
        {
            float randomValue = (float)random.NextDouble();

            // Random turn with turnProbability consideration
            if (randomValue < settings.turnProbability)
            {
                return turnDirections[bestTurnIndex];
            }
        }

        // 4. All other cases: go straight
        return straightDir;
    }

    float ManhattanDistance(Vector2 firstCenter, Vector2 secondCenter)
    {
        return Mathf.Abs(firstCenter.x - secondCenter.x) + Mathf.Abs(firstCenter.y - secondCenter.y);
    }

    /// <summary>
    /// Check: is the position valid for a corridor?
    /// The position is valid if:
    /// - It is within the map boundaries
    /// - It is NOT inside a room (except for permitted exit tiles)
    /// - It does NOT touch a room (except for the start and target rooms)
    /// </summary>
    private bool IsValidCorridorPosition(Vector2Int pos)
    {
        if (!IsInBounds(pos))
            return false;

        if (pos == currentStartTile || pos == currentTargetTile)
            return true;

        if (IsInsideAnyRoom(pos))
            return false;

        if (IsAdjacentToForbiddenRoom(pos))
            return false;

        return true;
    }

    /// <summary>
    /// Check: is inside of any room
    /// </summary>
    private bool IsInsideAnyRoom(Vector2Int pos)
    {
        foreach (var room in mapResult.rooms)
        {
            if (IsInsideRoom(pos, room))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check: does the position fall within a forbidden room
    /// (any room other than the start and target rooms for the current corridor)
    /// </summary>
    private bool IsAdjacentToForbiddenRoom(Vector2Int pos)
    {
        int startRoomIndex = -1;
        int targetRoomIndex = -1;

        for (int i = 0; i < mapResult.rooms.Count; i++)
        {
            var room = mapResult.rooms[i];

            if (IsAdjacentToRoom(currentStartTile, room) || IsInsideRoom(currentStartTile, room))
            {
                startRoomIndex = i;
            }

            if (IsAdjacentToRoom(currentTargetTile, room) || IsInsideRoom(currentTargetTile, room))
            {
                targetRoomIndex = i;
            }
        }

        for (int i = 0; i < mapResult.rooms.Count; i++)
        {
            var room = mapResult.rooms[i];

            if (i == startRoomIndex || i == targetRoomIndex)
                continue;

            if (IsAdjacentToRoom(pos, room))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check: if inside of the specific room
    /// </summary>
    private bool IsInsideRoom(Vector2Int pos, BSPRoom room)
    {
        RectInt bounds = room.bounds;
        return pos.x >= bounds.xMin && pos.x < bounds.xMax &&
               pos.y >= bounds.yMin && pos.y < bounds.yMax;
    }

    /// <summary>
    /// Check: does the position adjoin the room?
    /// </summary>
    private bool IsAdjacentToRoom(Vector2Int pos, BSPRoom room)
    {
        RectInt bounds = room.bounds;

        // ѕровер€ем все 4 направлени€
        foreach (var dir in directions)
        {
            Vector2Int neighbor = pos + dir;

            // ≈сли соседн€€ клетка внутри комнаты - мы касаемс€ комнаты
            if (neighbor.x >= bounds.xMin && neighbor.x < bounds.xMax &&
                neighbor.y >= bounds.yMin && neighbor.y < bounds.yMax)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check: if the position in map boundaries
    /// </summary>
    private bool IsInBounds(Vector2Int pos)
    {
        if (mapResult == null || mapResult.mapSize.x <= 0 || mapResult.mapSize.y <= 0)
            return false;

        return pos.x >= 0 && pos.x < mapResult.mapSize.x &&
               pos.y >= 0 && pos.y < mapResult.mapSize.y;
    }

    private void WriteCorridorsToGrid()
    {
        if (mapResult == null || mapResult.grid == null)
        {
            Debug.LogWarning("MapResult or grid is null, cannot write corridors to grid");
            return;
        }

        const int CORRIDOR_VALUE = -1;

        int tilesWritten = 0;
        int tilesSkipped = 0;

        foreach (var corridor in result.corridors)
        {
            if (corridor == null || corridor.tiles == null)
                continue;

            foreach (var tile in corridor.tiles)
            {
                if (!IsInBounds(tile))
                {
                    Debug.LogWarning($"Corridor tile {tile} is out of bounds ({mapResult.mapSize.x}x{mapResult.mapSize.y})");
                    continue;
                }

                int currentValue = mapResult.grid[tile.x, tile.y];

                // 0 = empty space
                // -1 = corridor
                if (currentValue == 0 || currentValue == CORRIDOR_VALUE)
                {
                    mapResult.grid[tile.x, tile.y] = CORRIDOR_VALUE;
                    tilesWritten++;
                }
                else
                {
                    tilesSkipped++;
                }
            }
        }

        Debug.Log($"Written {tilesWritten} corridor tiles to grid " +
                  $"(skipped {tilesSkipped} tiles, total corridors: {result.corridors.Count})");
    }
}