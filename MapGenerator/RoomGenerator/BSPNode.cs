using UnityEngine;

public class BSPNode
{
    public RectInt bounds;
    public BSPNode child1;
    public BSPNode child2;
    public int depth;
    public bool isLeaf;

    public BSPNode(RectInt bounds, int depth = 0)
    {
        this.bounds = bounds;
        this.depth = depth;
        this.isLeaf = true;
    }

    public bool IsLeaf()
    {
        return child1 == null && child2 == null;
    }

    public void Split(BSPNode left, BSPNode right)
    {
        this.child1 = left;
        this.child2 = right;
        this.isLeaf = false;
    }
}