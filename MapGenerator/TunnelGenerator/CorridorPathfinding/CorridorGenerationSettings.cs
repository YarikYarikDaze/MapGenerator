using UnityEngine;

[System.Serializable]
public class CorridorGenerationSettings
{
    [Header("Generation")]
    [Tooltip("—ид дл€ генерации (-1 = случайный)")]
    public int seed = -1;

    [Tooltip("—клонность к повороту (0 = только пр€мо, 1 = поворот при любой возможности)")]
    [Range(0f, 1f)]
    public float turnProbability = 0.3f;

    [Tooltip("ћаксимальное количество итераций дл€ одного коридора")]
    [Range(1, 50)]
    public int maxIterations = 10;

    [Header("Visualization")]
    public bool showCorridors = true;
    public Color corridorColor = new Color(0.8f, 0.6f, 0.2f, 0.6f);
    public bool showIntersections = true;
    public Color intersectionColor = Color.magenta;
    public float intersectionSize = 0.8f;

    [HideInInspector]
    public int actualSeed;
}