using UnityEngine;

[System.Serializable]
public class BSPModuleSettings
{
    [Header("Map Size")]
    [Tooltip("Общая площадь карты в тайлах")]
    [Range(100, 10000)]
    public int mapArea = 2500;

    [Tooltip("Соотношение сторон карты (ширина/высота)")]
    [Range(0.5f, 2.0f)]
    public float mapAspectRatio = 1.0f;

    [Header("Room Settings")]
    [Tooltip("Желаемое количество комнат")]
    [Range(2, 16)]
    public int targetRoomCount = 8;

    [Tooltip("Процент площади, занятый комнатами")]
    [Range(0.2f, 0.45f)]
    public float roomAreaPercentage = 0.3f;

    [Tooltip("Максимальное соотношение сторон комнаты")]
    [Range(1f, 3f)]
    public float maxRoomAspectRatio = 1.5f;

    [Tooltip("Максимальное отличие от средней площади комнаты")]
    [Range(0.2f, 0.5f)]
    public float maxRoomSizeDeviation = 0.3f;

    [Tooltip("Стандартное отклонение для распределения размеров (0 = равномерное, 1 = близко к среднему)")]
    [Range(0f, 1f)]
    public float sizeDistributionFocus = 0.7f;

    [Header("Generation")]
    [Tooltip("Сид для генерации (-1 = случайный)")]
    public int seed = -1;

    [HideInInspector]
    public int actualSeed;

    // Calculating Properties
    public Vector2Int GetMapSize()
    {
        int width = Mathf.RoundToInt(Mathf.Sqrt(mapArea * mapAspectRatio));
        int height = Mathf.RoundToInt(mapArea / (float)width);
        return new Vector2Int(width, height);
    }

    public int GetTargetRoomArea()
    {
        return Mathf.RoundToInt(mapArea * roomAreaPercentage / targetRoomCount);
    }

    public Vector2Int GetAverageRoomSize()
    {
        int targetArea = GetTargetRoomArea();

        int width = Mathf.RoundToInt(Mathf.Sqrt(targetArea));
        int height = Mathf.RoundToInt(targetArea / (float)width);

        return new Vector2Int(width, height);
    }

    public Vector2Int GetMinRoomSize()
    {
        int targetArea = GetTargetRoomArea();
        int minArea = Mathf.RoundToInt(targetArea * (1f - maxRoomSizeDeviation));

        int minWidth = Mathf.RoundToInt(Mathf.Sqrt(minArea));
        int minHeight = Mathf.RoundToInt(minArea / (float)minWidth);

        // Ensuring minimum 3x3
        minWidth = Mathf.Max(3, minWidth);
        minHeight = Mathf.Max(3, minHeight);

        return new Vector2Int(minWidth, minHeight);
    }

    public Vector2Int GetMaxRoomSize()
    {
        int targetArea = GetTargetRoomArea();

        int maxArea = Mathf.RoundToInt(targetArea * (1f + maxRoomSizeDeviation));

        int maxWidth = Mathf.RoundToInt(Mathf.Sqrt(maxArea));
        int maxHeight = Mathf.RoundToInt(maxArea / (float)maxWidth);

        return new Vector2Int(maxWidth, maxHeight);
    }
}