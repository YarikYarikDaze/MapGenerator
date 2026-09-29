using UnityEngine;
using System.Collections.Generic;

public class RoomConnectionGenerator
{
    private RoomConnectionSettings settings;
    private MapGenerationResult mapResult;
    private MSTConnectionGenerator mstGenerator;

    public RoomConnectionResult Generate(RoomConnectionSettings settings, MapGenerationResult mapResult)
    {
        this.settings = settings;
        this.mapResult = mapResult;

        if (mapResult == null || mapResult.rooms == null || mapResult.rooms.Count < 2)
        {
            Debug.LogError("Invalid map result for room connection generation");
            return null;
        }

        RoomConnectionResult result = new RoomConnectionResult();

        for (int i = 0; i < mapResult.rooms.Count; i++)
        {
            result.roomConnectionCount[i] = 0;
        }

        // 1. MST generation
        mstGenerator = new MSTConnectionGenerator();
        List<RoomConnection> mstConnections = mstGenerator.GenerateMST(mapResult.rooms);

        foreach (var connection in mstConnections)
        {
            result.AddConnection(connection);
        }

        Debug.Log($"MST created with {mstConnections.Count} connections");

        // 2. Adding extra connection for rooms with one connection
        AddExtraConnections(result);

        // 3. Calculating exit points and tiles
        CalculateExitTiles(result);

        return result;
    }

    /// <summary>
    /// Calculating exit points on the room walls and tiles
    /// </summary>
    private void CalculateExitTiles(RoomConnectionResult result)
    {
        foreach (var connection in result.allConnections)
        {
            BSPRoom room1 = mapResult.rooms[connection.room1Index];
            BSPRoom room2 = mapResult.rooms[connection.room2Index];

            // Find exit points
            Vector2Int wallPoint1 = FindWallPoint(room1, room2);
            Vector2Int wallPoint2 = FindWallPoint(room2, room1);

            connection.SetWallPoints(wallPoint1, wallPoint2);

            // Calculating exit tiles
            Vector2Int exitTile1 = CalculateExitTile(wallPoint1, room1);
            Vector2Int exitTile2 = CalculateExitTile(wallPoint2, room2);

            connection.SetExitTiles(exitTile1, exitTile2);

            Debug.Log($"Connection {connection.room1Index}->{connection.room2Index}: " +
                      $"Wall1: {wallPoint1} -> Exit1: {exitTile1}, " +
                      $"Wall2: {wallPoint2} -> Exit2: {exitTile2}");
        }
    }

    /// <summary>
    /// Find the point on the room wall where the line connecting the centers of the rooms intersects it
    /// </summary>
    private Vector2Int FindWallPoint(BSPRoom fromRoom, BSPRoom toRoom)
    {
        Vector2 fromCenter = new Vector2(fromRoom.bounds.center.x, fromRoom.bounds.center.y);
        Vector2 toCenter = new Vector2(toRoom.bounds.center.x, toRoom.bounds.center.y);

        RectInt bounds = fromRoom.bounds;

        // Direction from the center of fromRoom to the center of toRoom
        Vector2 direction = (toCenter - fromCenter).normalized;

        // Check for intersections between the ray originating at `fromRoom` and directed towards `toRoom` and all the walls.
        Vector2? intersectionPoint = null;
        float minDistance = float.MaxValue;

        // Left wall
        Vector2? leftIntersection = RayIntersectsVerticalLine(
            fromCenter, direction,
            bounds.xMin,
            bounds.yMin,
            bounds.yMax
        );
        if (leftIntersection.HasValue)
        {
            float dist = Vector2.Distance(fromCenter, leftIntersection.Value);
            if (dist < minDistance)
            {
                minDistance = dist;
                intersectionPoint = leftIntersection;
            }
        }

        // Right wall
        Vector2? rightIntersection = RayIntersectsVerticalLine(
            fromCenter, direction,
            bounds.xMax - 1,
            bounds.yMin,
            bounds.yMax
        );
        if (rightIntersection.HasValue)
        {
            float dist = Vector2.Distance(fromCenter, rightIntersection.Value);
            if (dist < minDistance)
            {
                minDistance = dist;
                intersectionPoint = rightIntersection;
            }
        }

        // Lower wall
        Vector2? bottomIntersection = RayIntersectsHorizontalLine(
            fromCenter, direction,
            bounds.yMin,
            bounds.xMin,
            bounds.xMax
        );
        if (bottomIntersection.HasValue)
        {
            float dist = Vector2.Distance(fromCenter, bottomIntersection.Value);
            if (dist < minDistance)
            {
                minDistance = dist;
                intersectionPoint = bottomIntersection;
            }
        }

        // Upper wall
        Vector2? topIntersection = RayIntersectsHorizontalLine(
            fromCenter, direction,
            bounds.yMax - 1,
            bounds.xMin,
            bounds.xMax
        );
        if (topIntersection.HasValue)
        {
            float dist = Vector2.Distance(fromCenter, topIntersection.Value);
            if (dist < minDistance)
            {
                minDistance = dist;
                intersectionPoint = topIntersection;
            }
        }

        // If an intersection is found, apply offsets from the corners
        if (intersectionPoint.HasValue)
        {
            Vector2Int wallPoint = ApplyWallOffsets(intersectionPoint.Value, bounds);
            return wallPoint;
        }

        // Fallback: если не нашли пересечение, возвращаем центр комнаты
        Debug.LogWarning($"No intersection found for room at {bounds.position}");
        return new Vector2Int((int)bounds.center.x, (int)bounds.center.y);
    }

    /// <summary>
    /// Calculate the room exit tile (outside the wall).
    /// </summary>
    private Vector2Int CalculateExitTile(Vector2Int wallPoint, BSPRoom room)
    {
        RectInt bounds = room.bounds;

        // Determine which wall the point is on
        bool onLeftWall = (wallPoint.x == bounds.xMin);
        bool onRightWall = (wallPoint.x == bounds.xMax - 1);
        bool onBottomWall = (wallPoint.y == bounds.yMin);
        bool onTopWall = (wallPoint.y == bounds.yMax - 1);

        Vector2Int exitTile = wallPoint;

        // Set back from the foundation of the room's wall
        if (onLeftWall)
        {
            exitTile = new Vector2Int(wallPoint.x - 1, wallPoint.y);
        }
        else if (onRightWall)
        {
            exitTile = new Vector2Int(wallPoint.x + 1, wallPoint.y);
        }
        else if (onBottomWall)
        {
            exitTile = new Vector2Int(wallPoint.x, wallPoint.y - 1);
        }
        else if (onTopWall)
        {
            exitTile = new Vector2Int(wallPoint.x, wallPoint.y + 1);
        }
        else
        {
            // If the point is not on a wall (which should not happen), we attempt to determine the nearest wall
            Debug.LogWarning($"Wall point {wallPoint} is not on any wall of room {bounds}. Attempting to find nearest wall.");

            // Находим ближайшую стену
            int distToLeft = Mathf.Abs(wallPoint.x - bounds.xMin);
            int distToRight = Mathf.Abs(wallPoint.x - (bounds.xMax - 1));
            int distToBottom = Mathf.Abs(wallPoint.y - bounds.yMin);
            int distToTop = Mathf.Abs(wallPoint.y - (bounds.yMax - 1));

            int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

            if (minDist == distToLeft)
                exitTile = new Vector2Int(bounds.xMin - 1, wallPoint.y);
            else if (minDist == distToRight)
                exitTile = new Vector2Int(bounds.xMax, wallPoint.y);
            else if (minDist == distToBottom)
                exitTile = new Vector2Int(wallPoint.x, bounds.yMin - 1);
            else
                exitTile = new Vector2Int(wallPoint.x, bounds.yMax);
        }

        return exitTile;
    }

    /// <summary>
    /// Check for the intersection of a ray with a vertical line (wall)
    /// </summary>
    private Vector2? RayIntersectsVerticalLine(Vector2 rayOrigin, Vector2 rayDirection, int lineX, int minY, int maxY)
    {
        if (Mathf.Approximately(rayDirection.x, 0))
            return null;

        float t = (lineX - rayOrigin.x) / rayDirection.x;

        if (t <= 0)
            return null;

        float intersectY = rayOrigin.y + rayDirection.y * t;

        if (intersectY >= minY && intersectY < maxY)
        {
            return new Vector2(lineX, intersectY);
        }

        return null;
    }

    /// <summary>
    /// Check for the intersection of a ray with a horizontal line (wall)
    /// </summary>
    private Vector2? RayIntersectsHorizontalLine(Vector2 rayOrigin, Vector2 rayDirection, int lineY, int minX, int maxX)
    {
        if (Mathf.Approximately(rayDirection.y, 0))
            return null;

        float t = (lineY - rayOrigin.y) / rayDirection.y;

        if (t <= 0)
            return null;

        float intersectX = rayOrigin.x + rayDirection.x * t;

        if (intersectX >= minX && intersectX < maxX)
        {
            return new Vector2(intersectX, lineY);
        }

        return null;
    }

    /// <summary>
    /// Apply offsets from wall corners to a point on the wall
    /// </summary>
    private Vector2Int ApplyWallOffsets(Vector2 point, RectInt bounds)
    {
        int x = Mathf.RoundToInt(point.x);
        int y = Mathf.RoundToInt(point.y);

        bool onLeftWall = (x == bounds.xMin);
        bool onRightWall = (x == bounds.xMax - 1);
        bool onBottomWall = (y == bounds.yMin);
        bool onTopWall = (y == bounds.yMax - 1);

        if (onLeftWall || onRightWall)
        {
            // bounds.yMin + offset ... bounds.yMax - 1 - offset
            y = Mathf.Clamp(y,
                bounds.yMin + settings.minWallOffset,
                bounds.yMax - 1 - settings.minWallOffset
            );
        }
        else if (onBottomWall || onTopWall)
        {
            // bounds.xMin + offset ... bounds.xMax - 1 - offset
            x = Mathf.Clamp(x,
                bounds.xMin + settings.minWallOffset,
                bounds.xMax - 1 - settings.minWallOffset
            );
        }

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Adding extra connections for rooms with only one connection
    /// </summary>
    private void AddExtraConnections(RoomConnectionResult result)
    {
        int extraConnectionsAdded = 0;

        for (int roomIndex = 0; roomIndex < mapResult.rooms.Count; roomIndex++)
        {
            if (result.GetConnectionCount(roomIndex) != 1)
                continue;

            int connectedRoomIndex = GetConnectedRoom(result, roomIndex);
            int targetRoomIndex = FindBestExtraConnection(result, roomIndex, connectedRoomIndex);

            if (targetRoomIndex != -1)
            {
                BSPRoom room1 = mapResult.rooms[roomIndex];
                BSPRoom room2 = mapResult.rooms[targetRoomIndex];

                float distance = CalculateRoomDistance(room1, room2);

                RoomConnection extraConnection = new RoomConnection(
                    roomIndex,
                    targetRoomIndex,
                    distance,
                    false
                );

                result.AddConnection(extraConnection);
                extraConnectionsAdded++;

                Debug.Log($"Extra connection: Room {roomIndex} -> Room {targetRoomIndex} (distance: {distance:F1})");
            }
        }

        Debug.Log($"Added {extraConnectionsAdded} extra connections");
    }

    private int GetConnectedRoom(RoomConnectionResult result, int roomIndex)
    {
        foreach (var connection in result.allConnections)
        {
            if (connection.room1Index == roomIndex)
                return connection.room2Index;
            if (connection.room2Index == roomIndex)
                return connection.room1Index;
        }

        return -1;
    }

    /// <summary>
    /// Finding extra connections with minimum distance
    /// </summary>
    private int FindBestExtraConnection(RoomConnectionResult result, int roomIndex, int excludeRoomIndex)
    {
        BSPRoom sourceRoom = mapResult.rooms[roomIndex];
        float bestDistance = float.MaxValue;
        int bestRoomIndex = -1;

        for (int targetIndex = 0; targetIndex < mapResult.rooms.Count; targetIndex++)
        {
            if (targetIndex == roomIndex || targetIndex == excludeRoomIndex)
                continue;

            if (HasConnection(result, roomIndex, targetIndex))
                continue;

            BSPRoom targetRoom = mapResult.rooms[targetIndex];
            float distance = CalculateRoomDistance(sourceRoom, targetRoom);

            if (distance > settings.maxCorridorLength)
                continue;

            if (PathIntersectsAnyRoom(sourceRoom, targetRoom, roomIndex, targetIndex))
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestRoomIndex = targetIndex;
            }
        }

        return bestRoomIndex;
    }

    private bool HasConnection(RoomConnectionResult result, int room1, int room2)
    {
        foreach (var connection in result.allConnections)
        {
            if ((connection.room1Index == room1 && connection.room2Index == room2) ||
                (connection.room1Index == room2 && connection.room2Index == room1))
            {
                return true;
            }
        }
        return false;
    }

    private bool PathIntersectsAnyRoom(BSPRoom room1, BSPRoom room2, int room1Index, int room2Index)
    {
        Vector2 center1 = new Vector2(room1.bounds.center.x, room1.bounds.center.y);
        Vector2 center2 = new Vector2(room2.bounds.center.x, room2.bounds.center.y);

        for (int i = 0; i < mapResult.rooms.Count; i++)
        {
            if (i == room1Index || i == room2Index)
                continue;

            BSPRoom checkRoom = mapResult.rooms[i];

            if (LineIntersectsRect(center1, center2, checkRoom.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private bool LineIntersectsRect(Vector2 lineStart, Vector2 lineEnd, RectInt rect)
    {
        Vector2 rectMin = new Vector2(rect.xMin, rect.yMin);
        Vector2 rectMax = new Vector2(rect.xMax, rect.yMax);

        if (IsPointInRect(lineStart, rect) || IsPointInRect(lineEnd, rect))
            return true;

        Vector2[] rectCorners = new Vector2[]
        {
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMax, rect.yMin),
            new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.xMin, rect.yMax)
        };

        for (int i = 0; i < 4; i++)
        {
            Vector2 corner1 = rectCorners[i];
            Vector2 corner2 = rectCorners[(i + 1) % 4];

            if (LinesIntersect(lineStart, lineEnd, corner1, corner2))
                return true;
        }

        return false;
    }

    private bool IsPointInRect(Vector2 point, RectInt rect)
    {
        return point.x >= rect.xMin && point.x < rect.xMax &&
               point.y >= rect.yMin && point.y < rect.yMax;
    }

    private bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);

        if (Mathf.Approximately(denominator, 0))
            return false;

        float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
        float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;

        return ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1;
    }

    private float CalculateRoomDistance(BSPRoom room1, BSPRoom room2)
    {
        Vector2 center1 = new Vector2(room1.bounds.center.x, room1.bounds.center.y);
        Vector2 center2 = new Vector2(room2.bounds.center.x, room2.bounds.center.y);

        return Vector2.Distance(center1, center2);
    }
}