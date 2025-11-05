using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Build/Placement Logic/Straight Line")]
public class StraightLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    [Header("Settings")]
    [SerializeField] private Color previewColor = new(1f, 1f, 1f, 0.3f);

    private GameObject prefab;      // assigned at runtime
    private float rotation;         // from BuildManager
    private Vector3 worldStart;
    private readonly List<GameObject> previewObjects = new();

    // --- runtime setup ---
    public void Setup(GameObject prefab, float rotation)
    {
        this.prefab = prefab;
        this.rotation = rotation;
    }

    public void OnStartDrag(Vector3 start)
    {
        worldStart = start;
        ClearPreview();
    }

    public void OnDragging(Vector3 worldCurrent)
    {
        ClearPreview();

        (int startX, int startY) = GridManager.Instance.GridFromWorld(worldStart);
        (int endX, int endY)     = GridManager.Instance.GridFromWorld(worldCurrent);

        // pick dominant axis
        int dx = Mathf.Abs(endX - startX);
        int dy = Mathf.Abs(endY - startY);
        if (dx > dy) endY = startY; else endX = startX;

        foreach (var cell in GetCellsBetween(startX, startY, endX, endY))
        {
            Vector3 worldPos = GridManager.Instance.WorldFromGrid(cell.x, cell.y);
            previewObjects.Add(CreatePreviewGhost(worldPos));
        }
    }

    public void OnEndDrag(Vector3 worldEnd)
    {
        ClearPreview();

        (int startX, int startY) = GridManager.Instance.GridFromWorld(worldStart);
        (int endX, int endY)     = GridManager.Instance.GridFromWorld(worldEnd);

        int dx = Mathf.Abs(endX - startX);
        int dy = Mathf.Abs(endY - startY);
        if (dx > dy) endY = startY; else endX = startX;

        foreach (var cell in GetCellsBetween(startX, startY, endX, endY))
        {
            Vector3 worldPos = GridManager.Instance.WorldFromGrid(cell.x, cell.y);
            PlaceAt(worldPos);
        }
    }

    // --- helpers ---

    private IEnumerable<Vector2Int> GetCellsBetween(int sx, int sy, int ex, int ey)
    {
        var cells = new List<Vector2Int>();
        if (sx == ex)
        {
            int step = sy < ey ? 1 : -1;
            for (int y = sy; y != ey + step; y += step)
                cells.Add(new Vector2Int(sx, y));
        }
        else if (sy == ey)
        {
            int step = sx < ex ? 1 : -1;
            for (int x = sx; x != ex + step; x += step)
                cells.Add(new Vector2Int(x, sy));
        }
        return cells;
    }

    private GameObject CreatePreviewGhost(Vector3 worldPos)
    {
        GameObject ghost = Object.Instantiate(prefab, worldPos, Quaternion.Euler(0, 0, rotation));

        foreach (var comp in ghost.GetComponents<MonoBehaviour>())
            comp.enabled = false;

        foreach (var sr in ghost.GetComponentsInChildren<SpriteRenderer>())
            sr.color = previewColor;

        ghost.layer = LayerMask.NameToLayer("Ignore Raycast");
        return ghost;
    }

    private void ClearPreview()
    {
        foreach (var g in previewObjects)
            if (g) Object.Destroy(g);
        previewObjects.Clear();
    }

    private void PlaceAt(Vector3 worldPos)
    {
        GameObject obj = Object.Instantiate(prefab, worldPos, Quaternion.Euler(0, 0, rotation));
        GridManager.Instance.BlockNodesUnderObject(obj);
    }
}
