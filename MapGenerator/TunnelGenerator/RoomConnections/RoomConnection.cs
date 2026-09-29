using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoomConnection
{
    public int room1Index;
    public int room2Index;
    public float distance;
    public bool isMSTConnection; // true = MST, false = extra

    // Exit points on the walls
    public Vector2Int wallPoint1;
    public Vector2Int wallPoint2;

    // Exit tiles
    public Vector2Int exitTile1;
    public Vector2Int exitTile2;

    public RoomConnection(int room1, int room2, float distance, bool isMST)
    {
        this.room1Index = room1;
        this.room2Index = room2;
        this.distance = distance;
        this.isMSTConnection = isMST;
    }

    public void SetWallPoints(Vector2Int wall1, Vector2Int wall2)
    {
        this.wallPoint1 = wall1;
        this.wallPoint2 = wall2;
    }

    public void SetExitTiles(Vector2Int exit1, Vector2Int exit2)
    {
        this.exitTile1 = exit1;
        this.exitTile2 = exit2;
    }
}

[System.Serializable]
public class RoomConnectionResult
{
    public List<RoomConnection> allConnections;
    public List<RoomConnection> mstConnections;
    public List<RoomConnection> extraConnections;

    public Dictionary<int, int> roomConnectionCount;

    public RoomConnectionResult()
    {
        allConnections = new List<RoomConnection>();
        mstConnections = new List<RoomConnection>();
        extraConnections = new List<RoomConnection>();
        roomConnectionCount = new Dictionary<int, int>();
    }

    public void AddConnection(RoomConnection connection)
    {
        allConnections.Add(connection);

        if (connection.isMSTConnection)
            mstConnections.Add(connection);
        else
            extraConnections.Add(connection);

        if (!roomConnectionCount.ContainsKey(connection.room1Index))
            roomConnectionCount[connection.room1Index] = 0;
        if (!roomConnectionCount.ContainsKey(connection.room2Index))
            roomConnectionCount[connection.room2Index] = 0;

        roomConnectionCount[connection.room1Index]++;
        roomConnectionCount[connection.room2Index]++;
    }

    public int GetConnectionCount(int roomIndex)
    {
        return roomConnectionCount.ContainsKey(roomIndex) ? roomConnectionCount[roomIndex] : 0;
    }
}