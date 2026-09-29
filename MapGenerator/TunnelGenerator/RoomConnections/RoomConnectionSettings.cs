using UnityEngine;

[System.Serializable]
public class RoomConnectionSettings
{
    [Header("Connection Rules")]
    [Tooltip("ћаксимальна€ допустима€ длина коридора")]
    [Range(5f, 100f)]
    public float maxCorridorLength = 30f;

    [Header("Door Placement")]
    [Tooltip("ћинимальный отступ двери от угла стены")]
    [Range(1, 5)]
    public int minWallOffset = 2;

    [Header("Visualization")]
    public bool showMSTConnections = true;
    public bool showExtraConnections = true;
    public Color mstConnectionColor = Color.yellow;
    public Color extraConnectionColor = Color.green;
    public float connectionLineWidth = 2f;

    [Header("Door Visualization")]
    public bool showDoorPoints = true;
    public Color doorPointColor = Color.red;
    public float doorPointSize = 0.5f;
}