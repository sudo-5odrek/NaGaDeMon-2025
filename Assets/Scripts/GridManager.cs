using UnityEngine;

[ExecuteAlways]
public class GridManager : MonoBehaviour
{
    public static GridManager I;

    [Header("Grid Settings")]
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    [Header("Obstacle Detection")]
    [Tooltip("Layer mask for things that block pathfinding.")]
    public LayerMask obstacleMask;

    [Header("Colors")]
    public Color gridColor = new Color(1f, 1f, 1f, 0.2f);
    public Color blockedColor = new Color(1f, 0f, 0f, 0.3f);

    private Node[,] nodes;

    void Awake()
    {
        I = this;
        GenerateGrid();
    }

    void OnValidate()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Vector3 center = WorldFromGrid(x, y);

            // 👇 Check for obstacle overlap at this tile
            bool blocked = Physics2D.OverlapBox(center, Vector2.one * (cellSize * 0.9f), 0, obstacleMask);

            nodes[x, y] = new Node(x, y, !blocked);
        }
    }

    public Node GetNode(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return nodes[x, y];
    }

    public Vector3 WorldFromGrid(int x, int y)
    {
        float originX = -(width / 2f) * cellSize + cellSize / 2f;
        float originY = -(height / 2f) * cellSize + cellSize / 2f;
        float worldX = originX + x * cellSize;
        float worldY = originY + y * cellSize;
        return new Vector3(worldX, worldY, 0);
    }

    public (int, int) GridFromWorld(Vector3 pos)
    {
        float originX = -(width / 2f) * cellSize;
        float originY = -(height / 2f) * cellSize;

        int gx = Mathf.FloorToInt((pos.x - originX) / cellSize);
        int gy = Mathf.FloorToInt((pos.y - originY) / cellSize);

        return (gx, gy);
    }

    public void SetWalkable(int x, int y, bool walkable)
    {
        var node = GetNode(x, y);
        if (node != null) node.walkable = walkable;
    }

    void OnDrawGizmos()
    {
        if (nodes == null) GenerateGrid();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Node n = nodes[x, y];
            Vector3 center = WorldFromGrid(x, y);

            if (n.walkable)
            {
                Gizmos.color = gridColor;
                Gizmos.DrawWireCube(center, Vector3.one * cellSize);
            }
            else
            {
                Gizmos.color = blockedColor;
                Gizmos.DrawCube(center, Vector3.one * cellSize * 0.95f);
            }
        }
    }
}

[System.Serializable]
public class Node
{
    public int x, y;
    public bool walkable;
    public Node parent;
    public float g, h;
    public float f => g + h;

    public Node(int x, int y, bool walkable)
    {
        this.x = x;
        this.y = y;
        this.walkable = walkable;
    }
}
