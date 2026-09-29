using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularMapGenerator : MonoBehaviour
{
    [Header("Module 1: BSP Room Generation")]
    public BSPModuleSettings bspSettings = new BSPModuleSettings();

    [Header("Module 2: Room Connections")]
    public RoomConnectionSettings connectionSettings = new RoomConnectionSettings();

    [Header("Module 3: Corridor Generation")]
    public CorridorGenerationSettings corridorSettings = new CorridorGenerationSettings();

    [Header("Module 4: 3D Map Visualization")]
    public MapVisualizationSettings visualizationSettings = new MapVisualizationSettings();

    [Header("Results")]
    [HideInInspector]
    public MapGenerationResult bspResult;

    [HideInInspector]
    public RoomConnectionResult connectionResult;

    [HideInInspector]
    public CorridorGenerationResult corridorResult;

    [HideInInspector]
    public MapVisualizationResult visualizationResult;

    [Header("Gizmo Settings")]
    public bool showGrid = true;
    public int gridStep = 10;
    public bool showRoomLabels = true;

    private BSPMapGenerator bspGenerator;

    private RoomConnectionGenerator connectionGenerator;

    private CorridorGenerator corridorGenerator;

    private MapVisualizer mapVisualizer;

    void OnEnable()
    {
        EnsureSettingsInitialized();
    }

    void Awake()
    {
        EnsureSettingsInitialized();
        bspGenerator = new BSPMapGenerator();
        connectionGenerator = new RoomConnectionGenerator();
        corridorGenerator = new CorridorGenerator();
        mapVisualizer = new MapVisualizer();
        bspResult = null;
        connectionResult = null;
        corridorResult = null;
        visualizationResult = null;
    }

    public void EnsureSettingsInitialized()
    {
        if (bspSettings == null)
        {
            bspSettings = new BSPModuleSettings();
        }

        if (connectionSettings == null)
        {
            connectionSettings = new RoomConnectionSettings();
        }

        if (corridorSettings == null)
        {
            corridorSettings = new CorridorGenerationSettings();
        }

        if (visualizationSettings == null)
        {
            visualizationSettings = new MapVisualizationSettings();
        }
    }

    public bool SettingsInitializationCheck()
    {
        return bspSettings == null || connectionSettings == null || corridorSettings == null || visualizationSettings == null;
    }

    // ============================================================
    // MODULE 1: BSP GENERATION
    // ============================================================

    public void GenerateBSP()
    {
        if (bspSettings == null)
        {
            Debug.LogError("BSP Settings are null!");
            return;
        }

        if (bspGenerator == null)
        {
            bspGenerator = new BSPMapGenerator();
        }

        try
        {
            if (bspResult != null)
            {
                bspResult = null;
            }

            bspResult = bspGenerator.Generate(bspSettings);

            Debug.Log($"BSP Generated: {bspResult.rooms.Count} rooms, seed: {bspResult.seed}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BSP Generation failed: {e.Message}\n{e.StackTrace}");
        }
    }

    // ============================================================
    // MODULE 2: CONNECTION GENERATION
    // 

    public void GenerateConnections()
    {
        if (bspResult == null || bspResult.rooms == null || bspResult.rooms.Count < 2)
        {
            Debug.LogError("Generate rooms first! Need at least 2 rooms.");
            return;
        }

        if (connectionSettings == null)
        {
            Debug.LogError("Connection Settings are null!");
            return;
        }

        if (connectionGenerator == null)
        {
            connectionGenerator = new RoomConnectionGenerator();
        }

        try
        {
            ClearConnectionsOnly();

            connectionResult = connectionGenerator.Generate(connectionSettings, bspResult);

            Debug.Log($"Connections Generated: {connectionResult.allConnections.Count} total " +
                      $"({connectionResult.mstConnections.Count} MST + {connectionResult.extraConnections.Count} extra)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Connection Generation failed: {e.Message}\n{e.StackTrace}");
        }
    }

    // ============================================================
    // MODULE 3: CORRIDOR GENERATION
    // ============================================================

    public void GenerateCorridors()
    {
        if (connectionResult == null || connectionResult.allConnections == null ||
            connectionResult.allConnections.Count == 0)
        {
            Debug.LogError("Generate connections first! Need at least 1 connection.");
            return;
        }

        if (corridorSettings == null)
        {
            Debug.LogError("Corridor Settings are null!");
            return;
        }

        if (corridorGenerator == null)
        {
            corridorGenerator = new CorridorGenerator();
        }

        try
        {
            ClearCorridorsOnly();

            corridorResult = corridorGenerator.Generate(corridorSettings, bspResult, connectionResult);

            Debug.Log($"Corridors Generated: {corridorResult.successfulCorridors} successful, " +
                      $"{corridorResult.failedCorridors} failed, {corridorResult.intersections.Count} intersections");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Corridor Generation failed: {e.Message}\n{e.StackTrace}");
        }
    }

    // ============================================================
    // MODULE 4: 3D MAP VISUALIZATION
    // ============================================================

    public void Visualize3DMap()
    {
        if (bspResult == null)
        {
            Debug.LogError("Generate BSP first! No map data to visualize.");
            return;
        }

        if (visualizationSettings == null)
        {
            Debug.LogError("Visualization Settings are null!");
            return;
        }

        if (mapVisualizer == null)
        {
            mapVisualizer = new MapVisualizer();
        }

        try
        {
            ClearVisualizationOnly();

            visualizationResult = mapVisualizer.Visualize(
                visualizationSettings,
                bspResult,
                connectionResult,
                corridorResult
            );

            if (visualizationResult != null)
            {
                Debug.Log($"3D Map Visualized: {visualizationResult.totalObjectsCreated} objects created");
            }
            else
            {
                Debug.LogError("Visualization returned null result");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"3D Visualization failed: {e.Message}\n{e.StackTrace}");
        }
    }

    // ============================================================
    // GENERAL CONTROLS
    // ============================================================

    public void ClearGeneration()
    {
        ClearVisualizationOnly();
        ClearCorridorsOnly();
        ClearConnectionsOnly();

        if(bspResult != null)
        {
            bspResult = null;
        }

        Debug.Log("Generation cleared");
    }

    public void RegenerateAll()
    {
        GenerateBSP();
        GenerateConnections();
        GenerateCorridors();
        Visualize3DMap();
    }

    public void ClearConnectionsOnly()
    {
        if(connectionResult != null)
        {
            connectionResult = null;
        }
        Debug.Log("Connections cleared");
    }

    public void ClearCorridorsOnly()
    {
        if (corridorResult != null)
        {
            // Clear corridor tiles from grid in mapResults
            if (bspResult != null && bspResult.grid != null)
            {
                const int CORRIDOR_VALUE = -1;

                for (int x = 0; x < bspResult.mapSize.x; x++)
                {
                    for (int y = 0; y < bspResult.mapSize.y; y++)
                    {
                        if (bspResult.grid[x, y] == CORRIDOR_VALUE)
                        {
                            bspResult.grid[x, y] = 0;
                        }
                    }
                }
            }
        }

        corridorResult = null;
        Debug.Log("Corridors cleared");
    }

    public void ClearVisualizationOnly()
    {
        if (visualizationResult != null)
        {
            visualizationResult.Clear();
            visualizationResult = null;
        }
        Debug.Log("Visualization cleared");
    }

    // ============================================================
    // GIZMOS VISUALIZATION
    // ============================================================

    void OnDrawGizmos()
    {
        DrawGrid();
        DrawRooms();
        DrawConnections();
        DrawCorridors();
    }

    private void DrawGrid()
    {
        if (!showGrid || bspResult == null) return;

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        for (int x = 0; x <= bspResult.mapSize.x; x += gridStep)
        {
            Gizmos.DrawLine(
                new Vector3(x, 0, 0),
                new Vector3(x, 0, bspResult.mapSize.y)
            );
        }

        for (int y = 0; y <= bspResult.mapSize.y; y += gridStep)
        {
            Gizmos.DrawLine(
                new Vector3(0, 0, y),
                new Vector3(bspResult.mapSize.x, 0, y)
            );
        }
    }

    private void DrawRooms()
    {
        if (bspResult == null || bspResult.rooms == null) return;

        for (int i = 0; i < bspResult.rooms.Count; i++)
        {
            var room = bspResult.rooms[i];
            if (room == null) continue;

            Vector3 center = new Vector3(room.bounds.center.x, 0, room.bounds.center.y);
            Vector3 size = new Vector3(room.bounds.width, 0.1f, room.bounds.height);

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
            Gizmos.DrawCube(center, size);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
            if (showRoomLabels)
            {
                UnityEditor.Handles.Label(
                    center + Vector3.up * 0.5f,
                    $"Room {i}\n{room.bounds.width}×{room.bounds.height}",
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = Color.white },
                        fontStyle = FontStyle.Bold
                    }
                );
            }
#endif
        }

        Gizmos.color = Color.red;
        Vector3 mapCenter = new Vector3(bspResult.mapSize.x / 2f, 0, bspResult.mapSize.y / 2f);
        Vector3 mapSize = new Vector3(bspResult.mapSize.x, 0.1f, bspResult.mapSize.y);
        Gizmos.DrawWireCube(mapCenter, mapSize);
    }

    private void DrawConnections()
    {
        if (connectionResult == null || connectionSettings == null || bspResult == null)
            return;

        if (connectionResult.allConnections == null || connectionResult.allConnections.Count == 0)
            return;

        foreach (var connection in connectionResult.allConnections)
        {
            if (connection.room1Index >= bspResult.rooms.Count ||
                connection.room2Index >= bspResult.rooms.Count)
                continue;

            if (connection.isMSTConnection && !connectionSettings.showMSTConnections)
                continue;
            if (!connection.isMSTConnection && !connectionSettings.showExtraConnections)
                continue;

            var room1 = bspResult.rooms[connection.room1Index];
            var room2 = bspResult.rooms[connection.room2Index];

            Vector3 exit1Pos = new Vector3(connection.exitTile1.x + 0.5f, 0.3f, connection.exitTile1.y + 0.5f);
            Vector3 exit2Pos = new Vector3(connection.exitTile2.x + 0.5f, 0.3f, connection.exitTile2.y + 0.5f);

            Gizmos.color = connection.isMSTConnection ?
                connectionSettings.mstConnectionColor :
                connectionSettings.extraConnectionColor;

            Gizmos.DrawLine(exit1Pos, exit2Pos);

            Vector3 direction = (exit2Pos - exit1Pos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction) * 0.5f;
            Vector3 mid = (exit1Pos + exit2Pos) / 2f;

            Gizmos.DrawLine(mid, mid - direction * 0.5f + right);
            Gizmos.DrawLine(mid, mid - direction * 0.5f - right);

            if (connectionSettings.showDoorPoints)
            {
                Gizmos.color = connectionSettings.doorPointColor;

                Vector3 exitSize = new Vector3(0.8f, 0.4f, 0.8f);
                Gizmos.DrawCube(exit1Pos, exitSize);
                Gizmos.DrawCube(exit2Pos, exitSize);

                Vector3 wall1Pos = new Vector3(connection.wallPoint1.x + 0.5f, 0.25f, connection.wallPoint1.y + 0.5f);
                Vector3 wall2Pos = new Vector3(connection.wallPoint2.x + 0.5f, 0.25f, connection.wallPoint2.y + 0.5f);

                Gizmos.color = connectionSettings.doorPointColor * 0.7f;
                Gizmos.DrawSphere(wall1Pos, connectionSettings.doorPointSize * 0.6f);
                Gizmos.DrawSphere(wall2Pos, connectionSettings.doorPointSize * 0.6f);
            }

#if UNITY_EDITOR
            string label = connection.isMSTConnection ? "MST" : "Extra";
            UnityEditor.Handles.Label(
                mid + Vector3.up * 0.3f,
                $"{label}\n{connection.distance:F1}",
                new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = Gizmos.color },
                    fontSize = 9
                }
            );

            if (connectionSettings.showDoorPoints)
            {
                UnityEditor.Handles.Label(
                    exit1Pos + Vector3.up * 0.6f,
                    $"Exit\n({connection.exitTile1.x},{connection.exitTile1.y})",
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = connectionSettings.doorPointColor },
                        fontSize = 8
                    }
                );

                UnityEditor.Handles.Label(
                    exit2Pos + Vector3.up * 0.6f,
                    $"Exit\n({connection.exitTile2.x},{connection.exitTile2.y})",
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = connectionSettings.doorPointColor },
                        fontSize = 8
                    }
                );
            }
#endif
        }
    }

    private void DrawCorridors()
    {
        if (corridorResult == null || corridorSettings == null || !corridorSettings.showCorridors)
            return;

        if (corridorResult.corridors == null || corridorResult.corridors.Count == 0)
            return;

        foreach (var corridor in corridorResult.corridors)
        {
            Gizmos.color = corridorSettings.corridorColor;

            foreach (var tile in corridor.tiles)
            {
                Vector3 center = new Vector3(tile.x + 0.5f, 0.2f, tile.y + 0.5f);
                Vector3 size = new Vector3(0.9f, 0.1f, 0.9f);

                Gizmos.DrawCube(center, size);
            }
        }

        if (corridorSettings.showIntersections && corridorResult.intersections.Count > 0)
        {
            Gizmos.color = corridorSettings.intersectionColor;

            foreach (var intersection in corridorResult.intersections)
            {
                Vector3 center = new Vector3(intersection.x + 0.5f, 0.4f, intersection.y + 0.5f);
                Gizmos.DrawSphere(center, corridorSettings.intersectionSize);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    center + Vector3.up * 0.5f,
                    "×",
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = corridorSettings.intersectionColor },
                        fontSize = 16,
                        fontStyle = FontStyle.Bold
                    }
                );
#endif
            }
        }
    }
}