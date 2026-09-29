using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ModularMapGenerator))]
public class ModularMapGeneratorEditor : Editor
{
    private ModularMapGenerator generator;

    private bool showBSPModule = true;
    private bool showBSPStatistics = true;
    private bool showConnectionModule = true;
    private bool showCorridorModule = true;
    private bool showVisualizationModule = true;

    void OnEnable()
    {
        generator = (ModularMapGenerator)target;

        if (generator.bspSettings == null)
        {
            generator.bspSettings = new BSPModuleSettings();
            EditorUtility.SetDirty(generator);
        }
    }

    public override void OnInspectorGUI()
    {
        if (generator == null)
        {
            EditorGUILayout.HelpBox("Generator reference is null!", MessageType.Error);
            return;
        }

        // Проверка и инициализация настроек
        if (!generator.SettingsInitializationCheck())
        {
            generator.EnsureSettingsInitialized();
            EditorUtility.SetDirty(generator);
        }

        serializedObject.Update();

        DrawMainHeader();

        EditorGUILayout.Space(10);
        DrawBSPModule();

        EditorGUILayout.Space(10);

        // The connection module becomes available after the rooms are generated
        bool canGenerateConnections = generator.bspResult != null &&
                                        generator.bspResult.rooms != null &&
                                        generator.bspResult.rooms.Count >= 2;

        if (canGenerateConnections)
        {
            DrawConnectionModule();
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Generate at least 2 rooms first to enable Connection module", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // The corridor module becomes available after connections are generated
        bool canGenerateCorridors = generator.connectionResult != null &&
                                    generator.connectionResult.allConnections != null &&
                                    generator.connectionResult.allConnections.Count > 0;

        if (canGenerateCorridors)
        {
            DrawCorridorModule();
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Generate connections first to enable Corridor module", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // The visualization module is available after the generation of the BSP and corridors
        bool canVisualize = generator.corridorResult != null &&
                            generator.corridorResult.corridors != null &&
                            generator.corridorResult.corridors.Count > 0;

        if (canVisualize)
        {
            DrawVisualizationModule();
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Generate BSP and corridors first to enable 3D Visualization module", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);
        DrawGlobalControls();

        EditorGUILayout.Space(10);
        DrawVisualizationSettings();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
        }
    }

    // ============================================================
    // HEADER
    // ============================================================

    private void DrawMainHeader()
    {
        EditorGUILayout.Space(5);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 18;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        EditorGUILayout.LabelField("Modular Map Generator", titleStyle);

        GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.fontStyle = FontStyle.Italic;

        DrawSeparator();
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    // ============================================================
    // MODULE 1: BSP ROOM GENERATION
    // ============================================================

    private void DrawBSPModule()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox);
        headerStyle.normal.textColor = Color.cyan;
        headerStyle.fontSize = 13;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        showBSPModule = EditorGUILayout.Foldout(showBSPModule, "", true);

        Rect headerRect = GUILayoutUtility.GetLastRect();
        headerRect.x = 0;
        headerRect.width = EditorGUIUtility.currentViewWidth;

        GUI.Label(headerRect, "Module 1: BSP Room Generation", headerStyle);

        if (!showBSPModule) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var settings = generator.bspSettings;

        EditorGUI.BeginChangeCheck();

        // Map Size Settings
        EditorGUILayout.LabelField("Map Size", EditorStyles.boldLabel);

        settings.mapArea = EditorGUILayout.IntSlider(
            new GUIContent("Map Area", "Общая площадь карты в тайлах"),
            settings.mapArea, 100, 10000
        );

        settings.mapAspectRatio = EditorGUILayout.Slider(
            new GUIContent("Aspect Ratio", "Соотношение сторон (ширина/высота)"),
            settings.mapAspectRatio, 0.5f, 2.0f
        );

        Vector2Int mapSize = settings.GetMapSize();
        EditorGUILayout.LabelField($"→ Size: {mapSize.x} × {mapSize.y}", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        // Room Settings
        EditorGUILayout.LabelField("Room Settings", EditorStyles.boldLabel);

        settings.targetRoomCount = EditorGUILayout.IntSlider(
            new GUIContent("Target Room Count", "Желаемое количество комнат"),
            settings.targetRoomCount, 2, 16
        );

        settings.roomAreaPercentage = EditorGUILayout.Slider(
            new GUIContent("Room Coverage %", "Процент площади, занятый комнатами"),
            settings.roomAreaPercentage, 0.2f, 0.45f
        );

        settings.maxRoomAspectRatio = EditorGUILayout.Slider(
            new GUIContent("Max Aspect Ratio", "Макс. соотношение сторон комнаты"),
            settings.maxRoomAspectRatio, 1f, 3.0f
        );

        settings.maxRoomSizeDeviation = EditorGUILayout.Slider(
            new GUIContent("Max room size deviation %", "Макс отклонение площади от средней (%)"),
            settings.maxRoomSizeDeviation, 0.2f, 0.5f
        );

        // Calculated properties
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int targetArea = settings.GetTargetRoomArea();
        Vector2Int avgSize = settings.GetAverageRoomSize();
        Vector2Int minSize = settings.GetMinRoomSize();
        Vector2Int maxSize = settings.GetMaxRoomSize();

        EditorGUILayout.LabelField("Calculated Parameters:", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"  Target Area: ~{targetArea} tiles", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Min Size: {minSize.x}×{minSize.y} ({minSize.x * minSize.y} tiles)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Avg Size: {avgSize.x}×{avgSize.y} ({avgSize.x * avgSize.y} tiles)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Max Size: {maxSize.x}×{maxSize.y} ({maxSize.x * maxSize.y} tiles)", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Distribution Settings
        EditorGUILayout.LabelField("Size Distribution", EditorStyles.boldLabel);

        settings.sizeDistributionFocus = EditorGUILayout.Slider(
            new GUIContent("Distribution Focus", "0 = равномерное, 1 = строго к среднему"),
            settings.sizeDistributionFocus, 0f, 1f
        );

        DrawDistributionIndicator(settings.sizeDistributionFocus);

        EditorGUILayout.Space(5);

        // Generation Settings
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        settings.seed = EditorGUILayout.IntField(
            new GUIContent("Seed", "Сид генерации (-1 = случайный)"),
            settings.seed
        );

        if (GUILayout.Button("🎲", GUILayout.Width(30)))
        {
            settings.seed = -1;
        }
        EditorGUILayout.EndHorizontal();

        if (generator.bspResult != null)
        {
            EditorGUILayout.LabelField($"→ Last Seed: {generator.bspResult.seed}", EditorStyles.miniLabel);
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(generator);
        }

        EditorGUILayout.Space(8);

        // Control buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
        if (GUILayout.Button("✨ Generate Rooms", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Generate BSP Map");
            generator.GenerateBSP();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
        GUI.enabled = generator.bspResult != null;
        if (GUILayout.Button("🔄 Regenerate", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Regenerate BSP Map");
            generator.bspSettings.seed = generator.bspResult.seed;
            generator.GenerateBSP();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }
        GUI.enabled = true;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        //  BSP statistics
        if (generator.bspResult != null)
        {
            EditorGUILayout.Space(8);
            DrawBSPStatistics();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDistributionIndicator(float focus)
    {
        Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        float focusWidth = rect.width * focus;
        Rect focusRect = new Rect(rect.x + (rect.width - focusWidth) / 2, rect.y, focusWidth, rect.height);
        EditorGUI.DrawRect(focusRect, new Color(0.3f, 0.8f, 0.3f));

        string label = focus < 0.3f ? "Wide variety" : focus < 0.7f ? "Moderate variety" : "Uniform sizes";
        EditorGUILayout.LabelField($"→ {label}", EditorStyles.miniLabel);
    }

    private void DrawBSPStatistics()
    {
        showBSPStatistics = EditorGUILayout.Foldout(showBSPStatistics, "Room Statistics", true);

        if (!showBSPStatistics) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var result = generator.bspResult;

        if (result == null || result.rooms == null || result.rooms.Count == 0)
        {
            EditorGUILayout.HelpBox("No rooms generated!", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField($"Map: {result.mapSize.x}×{result.mapSize.y} ({result.mapSize.x * result.mapSize.y} tiles)");
        EditorGUILayout.LabelField($"Rooms: {result.rooms.Count} | Seed: {result.seed}");

        int totalArea = 0, minArea = int.MaxValue, maxArea = 0;
        float minAspect = float.MaxValue, maxAspect = 0;
        int minW = int.MaxValue, maxW = 0, minH = int.MaxValue, maxH = 0;

        foreach (var room in result.rooms)
        {
            if (room == null) continue;

            int area = room.bounds.width * room.bounds.height;
            totalArea += area;
            minArea = Mathf.Min(minArea, area);
            maxArea = Mathf.Max(maxArea, area);
            minW = Mathf.Min(minW, room.bounds.width);
            maxW = Mathf.Max(maxW, room.bounds.width);
            minH = Mathf.Min(minH, room.bounds.height);
            maxH = Mathf.Max(maxH, room.bounds.height);

            float aspect = Mathf.Max(
                room.bounds.width / (float)room.bounds.height,
                room.bounds.height / (float)room.bounds.width
            );
            minAspect = Mathf.Min(minAspect, aspect);
            maxAspect = Mathf.Max(maxAspect, aspect);
        }

        float coverage = totalArea / (float)(result.mapSize.x * result.mapSize.y) * 100f;
        float avgArea = totalArea / (float)result.rooms.Count;

        EditorGUILayout.LabelField($"Coverage: {coverage:F1}% (target: {generator.bspSettings.roomAreaPercentage * 100:F0}%)");
        EditorGUILayout.LabelField($"Avg Area: {avgArea:F1} | Range: {minArea}-{maxArea}");
        EditorGUILayout.LabelField($"Width: {minW}-{maxW} | Height: {minH}-{maxH}");
        EditorGUILayout.LabelField($"Aspect: {minAspect:F2}-{maxAspect:F2}");

        EditorGUILayout.EndVertical();
    }

    // ============================================================
    // MODULE 2: ROOM CONNETIONS GENERATION
    // ============================================================

    private void DrawConnectionModule()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox);
        headerStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);
        headerStyle.fontSize = 13;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        showConnectionModule = EditorGUILayout.Foldout(showConnectionModule, "", true);

        Rect headerRect = GUILayoutUtility.GetLastRect();
        headerRect.x = 0;
        headerRect.width = EditorGUIUtility.currentViewWidth;

        GUI.Label(headerRect, "Module 2: Room Connections", headerStyle);

        if (!showConnectionModule) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (generator.connectionSettings == null)
        {
            EditorGUILayout.HelpBox("Connection settings are not initialized!", MessageType.Error);

            if (GUILayout.Button("Initialize Connection Settings"))
            {
                generator.connectionSettings = new RoomConnectionSettings();
                EditorUtility.SetDirty(generator);
            }

            EditorGUILayout.EndVertical();
            return;
        }

        var settings = generator.connectionSettings;

        EditorGUI.BeginChangeCheck();

        // Connection Settings
        EditorGUILayout.LabelField("Corridor Settings", EditorStyles.boldLabel);

        settings.maxCorridorLength = EditorGUILayout.Slider(
            new GUIContent("Max Corridor Length", "Максимальная допустимая длина коридора"),
            settings.maxCorridorLength, 5f, 100f
        );

        EditorGUILayout.Space(5);

        // Door Placement
        EditorGUILayout.LabelField("Door Placement", EditorStyles.boldLabel);

        settings.minWallOffset = EditorGUILayout.IntSlider(
            new GUIContent("Min Wall Offset", "Минимальный отступ двери от угла стены"),
            settings.minWallOffset, 1, 5
        );

        EditorGUILayout.Space(5);

        // Door Visualization
        EditorGUILayout.LabelField("Door Visualization", EditorStyles.boldLabel);

        settings.showDoorPoints = EditorGUILayout.Toggle("Show Door Points", settings.showDoorPoints);

        if (settings.showDoorPoints)
        {
            settings.doorPointColor = EditorGUILayout.ColorField("Door Color", settings.doorPointColor);
            settings.doorPointSize = EditorGUILayout.Slider("Door Size", settings.doorPointSize, 0.2f, 2f);
        }

        EditorGUILayout.Space(5);

        // Visualization
        EditorGUILayout.LabelField("🎨 Visualization", EditorStyles.boldLabel);

        settings.showMSTConnections = EditorGUILayout.Toggle("Show MST Connections", settings.showMSTConnections);
        settings.showExtraConnections = EditorGUILayout.Toggle("Show Extra Connections", settings.showExtraConnections);

        EditorGUILayout.Space(3);

        settings.mstConnectionColor = EditorGUILayout.ColorField("MST Color", settings.mstConnectionColor);
        settings.extraConnectionColor = EditorGUILayout.ColorField("Extra Color", settings.extraConnectionColor);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(8);

        // Control buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(1f, 0.8f, 0.2f);
        if (GUILayout.Button("🔗 Generate Connections", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Generate Connections");

            try
            {
                generator.GenerateConnections();
                EditorUtility.SetDirty(generator);
                SceneView.RepaintAll();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate connections: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Connection generation failed:\n{e.Message}", "OK");
            }
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        GUI.enabled = generator.connectionResult != null;
        if (GUILayout.Button("🗑️ Clear", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Clear Connections");
            generator.ClearConnectionsOnly();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }
        GUI.enabled = true;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }


    // ============================================================
    // MODULE 3: CORRIDOR GENERATION
    // ============================================================

    private void DrawCorridorModule()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox);
        headerStyle.normal.textColor = new Color(0.9f, 0.7f, 0.3f);
        headerStyle.fontSize = 13;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        showCorridorModule = EditorGUILayout.Foldout(showCorridorModule, "", true);

        Rect headerRect = GUILayoutUtility.GetLastRect();
        headerRect.x = 0;
        headerRect.width = EditorGUIUtility.currentViewWidth;

        GUI.Label(headerRect, "🛤️ Module 3: Corridor Generation", headerStyle);

        if (!showCorridorModule) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (generator.corridorSettings == null)
        {
            EditorGUILayout.HelpBox("Corridor settings are not initialized!", MessageType.Error);

            if (GUILayout.Button("Initialize Corridor Settings"))
            {
                generator.corridorSettings = new CorridorGenerationSettings();
                EditorUtility.SetDirty(generator);
            }

            EditorGUILayout.EndVertical();
            return;
        }

        var settings = generator.corridorSettings;

        EditorGUI.BeginChangeCheck();

        // Generation Settings
        EditorGUILayout.LabelField("⚙️ Generation Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        settings.seed = EditorGUILayout.IntField(
            new GUIContent("Seed", "Сид генерации коридоров (-1 = случайный)"),
            settings.seed
        );

        if (GUILayout.Button("🎲", GUILayout.Width(30)))
        {
            settings.seed = -1;
        }
        EditorGUILayout.EndHorizontal();

        if (generator.corridorResult != null)
        {
            EditorGUILayout.LabelField($"→ Last Used Seed: {generator.corridorResult.seed}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(3);

        settings.turnProbability = EditorGUILayout.Slider(
            new GUIContent("Turn Probability", "Склонность к повороту при выборе направления"),
            settings.turnProbability, 0f, 1f
        );

        settings.maxIterations = EditorGUILayout.IntSlider(
            new GUIContent("Max Iterations", "Максимальное количество попыток для построения одного коридора"),
            settings.maxIterations, 1, 50
        );

        EditorGUILayout.Space(5);

        // Visualization
        EditorGUILayout.LabelField("🎨 Visualization", EditorStyles.boldLabel);

        settings.showCorridors = EditorGUILayout.Toggle("Show Corridors", settings.showCorridors);

        if (settings.showCorridors)
        {
            settings.corridorColor = EditorGUILayout.ColorField("Corridor Color", settings.corridorColor);
        }

        EditorGUILayout.Space(2);

        settings.showIntersections = EditorGUILayout.Toggle("Show Intersections", settings.showIntersections);

        if (settings.showIntersections)
        {
            settings.intersectionColor = EditorGUILayout.ColorField("Intersection Color", settings.intersectionColor);
            settings.intersectionSize = EditorGUILayout.Slider("Intersection Size", settings.intersectionSize, 0.4f, 1.2f);
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(8);

        // Control buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.9f, 0.7f, 0.3f);
        if (GUILayout.Button("🛤️ Generate Corridors", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Generate Corridors");

            try
            {
                generator.GenerateCorridors();
                EditorUtility.SetDirty(generator);
                SceneView.RepaintAll();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate corridors: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Corridor generation failed:\n{e.Message}", "OK");
            }
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        GUI.enabled = generator.corridorResult != null;
        if (GUILayout.Button("🗑️ Clear", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Clear Corridors");
            generator.ClearCorridorsOnly();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }
        GUI.enabled = true;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ============================================================
    // MODULE 4: 3D MAP VISUALIZATION
    // ============================================================

    private void DrawVisualizationModule()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox);
        headerStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
        headerStyle.fontSize = 13;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        showVisualizationModule = EditorGUILayout.Foldout(showVisualizationModule, "", true);

        Rect headerRect = GUILayoutUtility.GetLastRect();
        headerRect.x = 0;
        headerRect.width = EditorGUIUtility.currentViewWidth;

        GUI.Label(headerRect, "🏗️ Module 4: 3D Map Visualization", headerStyle);

        if (!showVisualizationModule) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (generator.visualizationSettings == null)
        {
            EditorGUILayout.HelpBox("Visualization settings are not initialized!", MessageType.Error);

            if (GUILayout.Button("Initialize Visualization Settings"))
            {
                generator.visualizationSettings = new MapVisualizationSettings();
                EditorUtility.SetDirty(generator);
            }

            EditorGUILayout.EndVertical();
            return;
        }

        var settings = generator.visualizationSettings;

        EditorGUI.BeginChangeCheck();

        // Asset Paths
        EditorGUILayout.LabelField("📁 Asset Paths", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assets should be placed in Resources folder.\n" +
                               "Structure:\n" +
                               "  Rooms/Floor/floor_1\n" +
                               "  Rooms/Walls/...\n" +
                               "  Corridors/Floor/floor_1\n" +
                               "  Corridors/Walls/...",
                               MessageType.Info);

        settings.roomAssetsFolder = EditorGUILayout.TextField(
            new GUIContent("Room Assets Folder", "Путь к папке с ассетами комнат (Resources/)"),
            settings.roomAssetsFolder
        );

        EditorGUILayout.LabelField($"→ Floor: {settings.GetRoomFloorPath()}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"→ Walls: {settings.GetRoomWallsPath()}", EditorStyles.miniLabel);

        EditorGUILayout.Space(2);

        settings.corridorAssetsFolder = EditorGUILayout.TextField(
            new GUIContent("Corridor Assets Folder", "Путь к папке с ассетами коридоров (Resources/)"),
            settings.corridorAssetsFolder
        );

        EditorGUILayout.LabelField($"→ Floor: {settings.GetCorridorFloorPath()}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"→ Walls: {settings.GetCorridorWallsPath()}", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        // Visualization Options
        EditorGUILayout.LabelField("⚙️ Visualization Options", EditorStyles.boldLabel);

        settings.tileScale = EditorGUILayout.Slider(
            new GUIContent("Tile Scale", "Масштаб одного тайла в Unity единицах"),
            settings.tileScale, 0.1f, 10f
        );

        EditorGUILayout.Space(2);

        settings.roomWallHeight = EditorGUILayout.IntSlider(
            new GUIContent("Room Wall Height", "Высота стен комнат (в тайлах)"),
            settings.roomWallHeight, 1, 10
        );

        // Unity units height
        EditorGUILayout.LabelField(
            $"→ {settings.roomWallHeight} tiles = {settings.GetRoomWallHeightInUnits():F2} units",
            EditorStyles.miniLabel
        );

        EditorGUILayout.Space(2);

        settings.corridorWallHeight = EditorGUILayout.IntSlider(
            new GUIContent("Corridor Wall Height", "Высота стен коридоров (в тайлах)"),
            settings.corridorWallHeight, 1, 10
        );

        EditorGUILayout.LabelField(
            $"→ {settings.corridorWallHeight} tiles = {settings.GetCorridorWallHeightInUnits():F2} units",
            EditorStyles.miniLabel
        );

        // Wall height synchronization
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🔗 Sync Heights", GUILayout.Width(120)))
        {
            settings.corridorWallHeight = settings.roomWallHeight;
            EditorUtility.SetDirty(generator);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        settings.mapParent = EditorGUILayout.ObjectField(
            new GUIContent("Map Parent", "Родительский объект для карты"),
            settings.mapParent,
            typeof(Transform),
            true
        ) as Transform;

        EditorGUILayout.Space(5);

        // Generation Settings
        EditorGUILayout.LabelField("🔧 Generation Settings", EditorStyles.boldLabel);

        settings.autoCreateParent = EditorGUILayout.Toggle(
            new GUIContent("Auto Create Parent", "Автоматически создавать родительский объект"),
            settings.autoCreateParent
        );

        EditorGUILayout.Space(3);

        settings.generateRoomFloors = EditorGUILayout.Toggle("Generate Floors", settings.generateRoomFloors);
        settings.generateCeilings = EditorGUILayout.Toggle("Generate Ceilings", settings.generateCeilings);
        settings.generateRoomWalls = EditorGUILayout.Toggle("Generate Room Walls", settings.generateRoomWalls);
        settings.generateCorridorWalls = EditorGUILayout.Toggle("Generate Corridor Walls", settings.generateCorridorWalls);

        EditorGUILayout.Space(5);

        // Control buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("🏗️ Build 3D Map", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Build 3D Map");

            try
            {
                generator.Visualize3DMap();
                EditorUtility.SetDirty(generator);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to build 3D map: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"3D Map building failed:\n{e.Message}", "OK");
            }
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        GUI.enabled = generator.visualizationResult != null;
        if (GUILayout.Button("🗑️ Clear", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator, "Clear 3D Map");
            generator.ClearVisualizationOnly();
            EditorUtility.SetDirty(generator);
        }
        GUI.enabled = true;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ============================================================
    // GLOBAL CONTROLS
    // ============================================================

    private void DrawGlobalControls()
    {
        DrawSeparator();
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("🎮 Global Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("🔄 Regenerate All", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Regenerate All");
            generator.RegenerateAll();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Clear All");
            generator.ClearGeneration();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    // ============================================================
    // VISUALIZATION SETTINGS
    // ============================================================

    private void DrawVisualizationSettings()
    {
        DrawSeparator();
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Scene Visualization", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        generator.showGrid = EditorGUILayout.Toggle("Show Grid", generator.showGrid);

        if (generator.showGrid)
        {
            generator.gridStep = EditorGUILayout.IntSlider("Grid Step", generator.gridStep, 1, 50);
        }

        generator.showRoomLabels = EditorGUILayout.Toggle("Show Room Labels", generator.showRoomLabels);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }
    }
}
