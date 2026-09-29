using UnityEngine;

[System.Serializable]
public class BSPRoom
{
    public RectInt bounds;
    public bool isLeaf;

    public BSPRoom(RectInt bounds)
    {
        this.bounds = bounds;
        this.isLeaf = true;
    }
}

