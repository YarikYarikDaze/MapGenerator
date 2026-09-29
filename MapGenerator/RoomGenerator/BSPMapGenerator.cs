using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BSPMapGenerator
{
    private BSPModuleSettings settings;
    private System.Random random;

    private Vector2Int averageRoomSize;
    private Vector2Int maxRoomSize;
    private Vector2Int minRoomSize;

    public MapGenerationResult Generate(BSPModuleSettings settings)
    {
        this.settings = settings;

        // Seed setting
        settings.actualSeed = settings.seed == -1 ? UnityEngine.Random.Range(0, int.MaxValue) : settings.seed;
        random = new System.Random(settings.actualSeed);

        Vector2Int mapSize = settings.GetMapSize();
        MapGenerationResult result = new MapGenerationResult(mapSize.x, mapSize.y, settings.actualSeed);

        // Calculating room sizes
        averageRoomSize = settings.GetAverageRoomSize();
        maxRoomSize = settings.GetMaxRoomSize();
        minRoomSize = settings.GetMinRoomSize();

        Debug.Log($"Room size targets - Min: {minRoomSize}, Avg: {averageRoomSize}, Max: {maxRoomSize}");

        // BSP tree generation
        BSPNode root = new BSPNode(new RectInt(0, 0, mapSize.x, mapSize.y));
        List<BSPNode> leafNodes = GenerateBSPTreeBFS(root);

        Debug.Log($"Generated {leafNodes.Count} leaf nodes");

        // Room generation inside BSP leaves
        CreateRooms(result, leafNodes);

        FillGrid(result);

        return result;
    }

    /// <summary>
    /// BSP tree generation using BFS aproach
    /// </summary>
    private List<BSPNode> GenerateBSPTreeBFS(BSPNode root)
    {
        Queue<BSPNode> queue = new Queue<BSPNode>();
        List<BSPNode> allNodes = new List<BSPNode>();
        List<BSPNode> leafNodes = new List<BSPNode>();

        queue.Enqueue(root);
        allNodes.Add(root);

        int iterations = 0;
        int maxIterations = 1000;

        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Check: reached required leaves count
            int currentLeafCount = allNodes.Count(n => n.IsLeaf());
            if (currentLeafCount >= settings.targetRoomCount)
            {
                break;
            }

            BSPNode currentNode = queue.Dequeue();

            // Try splitting the node
            if (TrySplitNode(currentNode, out BSPNode child1, out BSPNode child2))
            {
                currentNode.Split(child1, child2);

                queue.Enqueue(child1);
                queue.Enqueue(child2);

                allNodes.Add(child1);
                allNodes.Add(child2);
            }
        }

        // Collecting leaves
        leafNodes = allNodes.Where(n => n.IsLeaf()).ToList();

        // If leaves count is different from target count
        if (leafNodes.Count != settings.targetRoomCount)
        {
            Debug.LogWarning($"Generated {leafNodes.Count} rooms instead of {settings.targetRoomCount}");
        }

        return leafNodes;
    }

    /// <summary>
    /// Splitting the node
    /// </summary>
    private bool TrySplitNode(BSPNode node, out BSPNode child1, out BSPNode child2)
    {
        child1 = null;
        child2 = null;

        // Check minimum size for splitting
        // The node must be large enough to accommodate at least 2 rooms
        int minSplitWidth = minRoomSize.x * 2 + 4; // +4 for spacing between rooms
        int minSplitHeight = minRoomSize.y * 2 + 4; // +4 for spacing between rooms

        if (node.bounds.width < minSplitWidth || node.bounds.height < minSplitHeight)
        {
            return false;
        }

        // Determining the separation direction
        bool splitHorizontally = DetermineSplitDirection(node.bounds);

        // Выполнение разделения
        return splitHorizontally ?
            SplitHorizontally(node, out child1, out child2) :
            SplitVertically(node, out child1, out child2);
    }

    /// <summary>
    /// Determination of the separation direction based on aspect ration
    /// </summary>
    private bool DetermineSplitDirection(RectInt bounds)
    {
        float aspectRatio = bounds.width / (float)bounds.height;

        // If the node is too wide, split it vertically
        if (aspectRatio > 1.25f)
            return false; // Vertical split

        // If the node is too high, divide it horizontally
        if (aspectRatio < 0.8f)
            return true; // Horizontal split

        // Иначе случайно
        return random.NextDouble() < 0.5;
    }

    /// <summary>
    /// Horizontal split
    /// </summary>
    private bool SplitHorizontally(BSPNode node, out BSPNode child1, out BSPNode child2)
    {
        child1 = null;
        child2 = null;

        // Minimum distance from the edge, accounting for the minimum room size + offset
        int minOffset = minRoomSize.y + 2;

        int minY = node.bounds.y + minOffset;
        int maxY = node.bounds.y + node.bounds.height - minOffset;

        if (maxY <= minY)
            return false;

        // Separation with a tendency towards the center
        int splitY = GetWeightedRandomPosition(minY, maxY, (minY + maxY) / 2);

        child1 = new BSPNode(
            new RectInt(
                node.bounds.x,
                node.bounds.y,
                node.bounds.width,
                splitY - node.bounds.y
            ),
            node.depth + 1
        );

        child2 = new BSPNode(
            new RectInt(
                node.bounds.x,
                splitY,
                node.bounds.width,
                node.bounds.y + node.bounds.height - splitY
            ),
            node.depth + 1
        );

        return true;
    }

    /// <summary>
    /// Vertical split
    /// </summary>
    private bool SplitVertically(BSPNode node, out BSPNode child1, out BSPNode child2)
    {
        child1 = null;
        child2 = null;

        // Minimum distance from the edge, accounting for the minimum room size + offset
        int minOffset = minRoomSize.x + 2;

        int minX = node.bounds.x + minOffset;
        int maxX = node.bounds.x + node.bounds.width - minOffset;

        if (maxX <= minX)
            return false;

        // Separation with a tendency towards the center
        int splitX = GetWeightedRandomPosition(minX, maxX, (minX + maxX) / 2);

        child1 = new BSPNode(
            new RectInt(
                node.bounds.x,
                node.bounds.y,
                splitX - node.bounds.x,
                node.bounds.height
            ),
            node.depth + 1
        );

        child2 = new BSPNode(
            new RectInt(
                splitX,
                node.bounds.y,
                node.bounds.x + node.bounds.width - splitX,
                node.bounds.height
            ),
            node.depth + 1
        );

        return true;
    }

    /// <summary>
    /// Obtaining a random position weighted towards the center (normal distribution)
    /// </summary>
    private int GetWeightedRandomPosition(int min, int max, int target)
    {
        if (min >= max) return min;

        // Use the Box-Muller transform for the normal distribution.
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                               System.Math.Sin(2.0 * System.Math.PI * u2);

        double stdDev = (max - min) * 0.25;
        double mean = target;

        int result = Mathf.RoundToInt((float)(mean + stdDev * randStdNormal));
        return Mathf.Clamp(result, min, max);
    }

    /// <summary>
    /// Creating rooms at leaves nodes
    /// </summary>
    private void CreateRooms(MapGenerationResult result, List<BSPNode> leafNodes)
    {
        foreach (var leaf in leafNodes)
        {
            RectInt roomBounds = GenerateRoomInContainer(leaf.bounds);

            if (roomBounds.width >= minRoomSize.x && roomBounds.height >= minRoomSize.y)
            {
                BSPRoom room = new BSPRoom(roomBounds);
                result.rooms.Add(room);
            }
            else
            {
                Debug.LogWarning($"Room too small: {roomBounds.width}x{roomBounds.height}, skipped");
            }
        }
    }

    /// <summary>
    /// Generating a room inside a container with a normal distribution of dimensions
    /// </summary>
    private RectInt GenerateRoomInContainer(RectInt container)
    {
        // Calculating the room size using a normal distribution around the average size.
        int roomWidth = GenerateRoomDimension(
            minRoomSize.x,
            Mathf.Min(maxRoomSize.x, container.width - 2),
            averageRoomSize.x
        );

        int roomHeight = GenerateRoomDimension(
            minRoomSize.y,
            Mathf.Min(maxRoomSize.y, container.height - 2),
            averageRoomSize.y
        );

        // Adjustin room aspect ration
        AdjustRoomAspectRatio(ref roomWidth, ref roomHeight, container);

        // Calculate the center of the entire grid
        Vector2Int mapCenter = new Vector2Int(
            settings.GetMapSize().x / 2,
            settings.GetMapSize().y / 2
        );

        // Calculate the ideal position of the room within the container, 
        // so that the room's center is as close as possible to the map's center
        int idealRoomCenterX = mapCenter.x;
        int idealRoomCenterY = mapCenter.y;

        // Calculating the ideal position for the top-left corner of the room
        int idealRoomX = idealRoomCenterX - roomWidth / 2;
        int idealRoomY = idealRoomCenterY - roomHeight / 2;

        int minRoomX = container.x + 1;
        int maxRoomX = container.x + container.width - roomWidth - 1;
        int minRoomY = container.y + 1;
        int maxRoomY = container.y + container.height - roomHeight - 1;

        int targetX = Mathf.Clamp(idealRoomX, minRoomX, maxRoomX);
        int targetY = Mathf.Clamp(idealRoomY, minRoomY, maxRoomY);

        // Positioning the room inside the container, distributed around targetX/targetY
        int roomX = GetWeightedRandomPosition(minRoomX, maxRoomX, targetX);
        int roomY = GetWeightedRandomPosition(minRoomY, maxRoomY, targetY);

        return new RectInt(roomX, roomY, roomWidth, roomHeight);
    }

    /// <summary>
    /// Генерация размера измерения с нормальным распределением
    /// </summary>
    private int GenerateRoomDimension(int min, int max, int target)
    {
        if (min >= max) return min;

        // Generation of a dimension size with a normal distribution
        target = Mathf.Clamp(target, min, max);

        // Box-Muller transform
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                               System.Math.Sin(2.0 * System.Math.PI * u2);

        // The standard deviation depends on the sizeDistributionFocus setting
        // The higher the focus, the tighter the distribution around the mean
        double range = max - min;
        double stdDev = range * (0.5 - settings.sizeDistributionFocus * 0.5);

        int result = Mathf.RoundToInt((float)(target + stdDev * randStdNormal));
        return Mathf.Clamp(result, min, max);
    }

    /// <summary>
    /// Adjustment of the room's aspect ratio
    /// </summary>
    private void AdjustRoomAspectRatio(ref int width, ref int height, RectInt container)
    {
        float currentAspect = Mathf.Max(width / (float)height, height / (float)width);

        if (currentAspect > settings.maxRoomAspectRatio)
        {
            if (width > height)
            {
                // The room is too wide
                int newWidth = Mathf.RoundToInt(height * settings.maxRoomAspectRatio);
                width = Mathf.Min(newWidth, container.width - 2);
                width = Mathf.Max(width, minRoomSize.x);
            }
            else
            {
                // The room is too high
                int newHeight = Mathf.RoundToInt(width * settings.maxRoomAspectRatio);
                height = Mathf.Min(newHeight, container.height - 2);
                height = Mathf.Max(height, minRoomSize.y);
            }
        }
    }

    private void FillGrid(MapGenerationResult result)
    {
        if (result == null || result.grid == null)
        {
            Debug.LogError("Cannot fill grid - result or grid is null");
            return;
        }

        for (int x = 0; x < result.mapSize.x; x++)
        {
            for (int y = 0; y < result.mapSize.y; y++)
            {
                result.grid[x, y] = 0;
            }
        }

        for (int i = 0; i < result.rooms.Count; i++)
        {
            var room = result.rooms[i];

            if (room == null)
            {
                Debug.LogWarning($"Room at index {i} is null, skipping");
                continue;
            }

            for (int x = room.bounds.x; x < room.bounds.x + room.bounds.width; x++)
            {
                for (int y = room.bounds.y; y < room.bounds.y + room.bounds.height; y++)
                {
                    if (x >= 0 && x < result.mapSize.x && y >= 0 && y < result.mapSize.y)
                    {
                        result.grid[x, y] = i + 1;
                    }
                }
            }
        }

        Debug.Log($"Grid filled: {result.mapSize.x}x{result.mapSize.y}, {result.rooms.Count} rooms");
    }
}