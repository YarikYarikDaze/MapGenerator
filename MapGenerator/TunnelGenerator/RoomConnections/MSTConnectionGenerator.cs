using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MSTConnectionGenerator
{
    private List<BSPRoom> rooms;

    public List<RoomConnection> GenerateMST(List<BSPRoom> rooms)
    {
        this.rooms = rooms;

        if (rooms == null || rooms.Count < 2)
        {
            Debug.LogWarning("Not enough rooms to generate MST");
            return new List<RoomConnection>();
        }

        // 1. Create all possible edges between rooms
        List<RoomConnection> allEdges = GenerateAllEdges();

        // 2. Sort edges by distance
        allEdges.Sort((a, b) => a.distance.CompareTo(b.distance));

        // 3. Kruskal algorithm
        List<RoomConnection> mst = KruskalMST(allEdges, rooms.Count);

        Debug.Log($"Generated MST with {mst.Count} connections for {rooms.Count} rooms");

        return mst;
    }

    private List<RoomConnection> GenerateAllEdges()
    {
        List<RoomConnection> edges = new List<RoomConnection>();

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                float distance = CalculateRoomDistance(rooms[i], rooms[j]);
                edges.Add(new RoomConnection(i, j, distance, true));
            }
        }

        return edges;
    }

    private float CalculateRoomDistance(BSPRoom room1, BSPRoom room2)
    {
        Vector2 center1 = new Vector2(room1.bounds.center.x, room1.bounds.center.y);
        Vector2 center2 = new Vector2(room2.bounds.center.x, room2.bounds.center.y);

        return Vector2.Distance(center1, center2);
    }

    private List<RoomConnection> KruskalMST(List<RoomConnection> sortedEdges, int nodeCount)
    {
        List<RoomConnection> mst = new List<RoomConnection>();

        // union of connected rooms
        UnionFind unionFind = new UnionFind(nodeCount);

        foreach (var edge in sortedEdges)
        {
            if (unionFind.Find(edge.room1Index) != unionFind.Find(edge.room2Index))
            {
                mst.Add(edge);
                unionFind.Union(edge.room1Index, edge.room2Index);

                if (mst.Count == nodeCount - 1)
                    break;
            }
        }

        return mst;
    }
}

public class UnionFind
{
    private int[] parent;
    private int[] rank;

    public UnionFind(int size)
    {
        parent = new int[size];
        rank = new int[size];

        for (int i = 0; i < size; i++)
        {
            parent[i] = i;
            rank[i] = 0;
        }
    }

    public int Find(int x)
    {
        if (parent[x] != x)
        {
            parent[x] = Find(parent[x]);
        }
        return parent[x];
    }

    public void Union(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);

        if (rootX == rootY)
            return;

        if (rank[rootX] < rank[rootY])
        {
            parent[rootX] = rootY;
        }
        else if (rank[rootX] > rank[rootY])
        {
            parent[rootY] = rootX;
        }
        else
        {
            parent[rootY] = rootX;
            rank[rootX]++;
        }
    }
}