using UnityEngine;

[System.Serializable]
public class MapVisualizationSettings
{
    [Header("Asset Paths")]
    [Tooltip("Путь к папке с ассетами комнат (относительно Resources/)")]
    public string roomAssetsFolder = "MapAssets/Rooms";

    [Tooltip("Путь к папке с ассетами коридоров (относительно Resources/)")]
    public string corridorAssetsFolder = "MapAssets/Corridors";

    [Header("Visualization Options")]
    [Tooltip("Масштаб одного тайла в Unity единицах")]
    public float tileScale = 1f;

    [Tooltip("Высота стен комнат (в тайлах)")]
    [Range(1, 10)]
    public int roomWallHeight = 3;

    [Tooltip("Высота стен коридоров (в тайлах)")]
    [Range(1, 10)]
    public int corridorWallHeight = 3;

    [Tooltip("Родительский объект для сгенерированной карты")]
    public Transform mapParent;

    [Header("Generation Settings")]
    [Tooltip("Автоматически создавать родительский объект если не указан")]
    public bool autoCreateParent = true;

    [Tooltip("Очищать предыдущую карту перед генерацией новой")]
    public bool clearPreviousMap = true;

    [Tooltip("Генерировать пол для комнат")]
    public bool generateRoomFloors = true;

    [Tooltip("Генерировать потолки")]
    public bool generateCeilings = true;

    [Tooltip("Генерировать стены для комнат")]
    public bool generateRoomWalls = true;

    [Tooltip("Генерировать стены для коридоров")]
    public bool generateCorridorWalls = true;

    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool showDebugInfo = false;

    /// <summary>
    /// Get room wall height in Unity units
    /// </summary>
    public float GetRoomWallHeightInUnits()
    {
        return roomWallHeight * tileScale;
    }

    public float GetCorridorWallHeightInUnits()
    {
        return corridorWallHeight * tileScale;
    }

    public string GetRoomFloorPath()
    {
        return $"{roomAssetsFolder}/Floor/floor_1";
    }

    public string GetCorridorFloorPath()
    {
        return $"{corridorAssetsFolder}/Floor/floor_1";
    }

    public string GetRoomCeilingPath()
    {
        return $"{roomAssetsFolder}/Floor/ceiling_1";
    }

    public string GetCorridorCeilingPath()
    {
        return $"{corridorAssetsFolder}/Floor/ceiling_1";
    }

    public string GetRoomWallsPath()
    {
        return $"{roomAssetsFolder}/Walls";
    }

    public string GetCorridorWallsPath()
    {
        return $"{corridorAssetsFolder}/Walls";
    }

    public string GetRoomWallStraightPath()
    {
        return $"{roomAssetsFolder}/Walls/wall_straight";
    }

    public string GetCorridorWallStraightPath()
    {
        return $"{corridorAssetsFolder}/Walls/wall_straight";
    }

    public string GetRoomDoorwayPath()
    {
        return $"{roomAssetsFolder}/Walls/doorway";
    }

    /// <summary>
    /// Calculate doorway height
    /// </summary>
    public int GetDoorwayHeight()
    {
        return Mathf.Min(roomWallHeight, corridorWallHeight);
    }
}