using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapVisualizationResult
{
    public Transform mapRootObject;
    public List<GameObject> roomFloorObjects;
    public List<GameObject> corridorFloorObjects;
    public List<GameObject> wallObjects;
    public List<GameObject> ceilingObjects;
    public int totalObjectsCreated;
    public System.DateTime visualizationTime;

    public MapVisualizationResult()
    {
        roomFloorObjects = new List<GameObject>();
        corridorFloorObjects = new List<GameObject>();
        wallObjects = new List<GameObject>();
        ceilingObjects = new List<GameObject>();
        totalObjectsCreated = 0;
        visualizationTime = System.DateTime.Now;
    }

    public void AddRoomFloorObject(GameObject obj)
    {
        if (obj != null)
        {
            roomFloorObjects.Add(obj);
            totalObjectsCreated++;
        }
    }

    public void AddCorridorFloorObject(GameObject obj)
    {
        if (obj != null)
        {
            corridorFloorObjects.Add(obj);
            totalObjectsCreated++;
        }
    }

    public void AddWallObject(GameObject obj)
    {
        if (obj != null)
        {
            wallObjects.Add(obj);
            totalObjectsCreated++;
        }
    }

    public void AddCeilingObject(GameObject obj)
    {
        if (obj != null)
        {
            ceilingObjects.Add(obj);
            totalObjectsCreated++;
        }
    }

    public void Clear()
    {
        if (mapRootObject != null)
        {
            Object.DestroyImmediate(mapRootObject.gameObject);
            mapRootObject = null;
        }

        roomFloorObjects.Clear();
        corridorFloorObjects.Clear();
        wallObjects.Clear();
        ceilingObjects.Clear();
        totalObjectsCreated = 0;
    }
}