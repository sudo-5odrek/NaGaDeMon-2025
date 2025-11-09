using UnityEngine;
using System.Collections.Generic;
using Grid;

[CreateAssetMenu(menuName = "Build/Placement Logic/Straight Line")]
public class StraightLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    [Header("Visual Settings")]
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.4f);

    private GameObject prefab;
    private float rotation;

    private Vector3 worldStart;
    private bool isDragging = false;
    

    private GameObject hoverPreview;
    private readonly List<GameObject> previewLine = new();

    // --------------------------------------------------
    // SETUP
    // --------------------------------------------------

    public void Setup(GameObject prefab, float rotation)
    {
         // 🧠 skip preview creation if still placing
        this.prefab = prefab;
        this.rotation = rotation;
        CreateHoverPreview();
    }

    // --------------------------------------------------
    // PREVIEW (before drag)
    // --------------------------------------------------

    public void UpdatePreview(Vector3 worldCurrent)
    {
        if (!isDragging && hoverPreview)
            hoverPreview.transform.position = worldCurrent;
    }

    // --------------------------------------------------
    // DRAG EVENTS
    // --------------------------------------------------

    public void OnStartDrag(Vector3 start)
    {
        isDragging = true;
        worldStart = start;

        if (hoverPreview)
            Object.Destroy(hoverPreview);

        ClearPreviewLine();
    }

    public void OnDragging(Vector3 current)
    {
        if (!isDragging) return;

        ClearPreviewLine();
        DrawPreviewLine(worldStart, current);
    }

    public void OnEndDrag(Vector3 worldEnd)
    {
        if (!isDragging) return;
        isDragging = false;

        // 🧠 Suppress immediate preview recreation

        ClearPreviewLine();
        PlaceLine(worldStart, worldEnd);
        
        
    }

    // --------------------------------------------------
    // INTERNAL HELPERS
    // --------------------------------------------------
    

    private void ClearPreviewLine()
    {
        foreach (var g in previewLine)
            if (g) Object.Destroy(g);
        previewLine.Clear();
    }

    private void CreateHoverPreview()
    {
        hoverPreview = Object.Instantiate(prefab);
        BuildUtils.MakePreview(hoverPreview);
    }

    private void DrawPreviewLine(Vector3 start, Vector3 end)
    {
        (int startX, int startY) = GridManager.Instance.GridFromWorld(start);
        (int endX, int endY) = GridManager.Instance.GridFromWorld(end);

        bool vertical = Mathf.Abs(endY - startY) > Mathf.Abs(endX - startX);
        if (vertical)
        {
            int step = startY < endY ? 1 : -1;
            for (int y = startY; y != endY + step; y += step)
                previewLine.Add(CreateGhost(GridManager.Instance.WorldFromGrid(startX, y)));
        }
        else
        {
            int step = startX < endX ? 1 : -1;
            for (int x = startX; x != endX + step; x += step)
                previewLine.Add(CreateGhost(GridManager.Instance.WorldFromGrid(x, startY)));
        }
    }

    private GameObject CreateGhost(Vector3 pos)
    {
        GameObject ghost = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
        BuildUtils.MakePreview(ghost);
        return ghost;
    }

    private void PlaceLine(Vector3 start, Vector3 end)
    {
        (int startX, int startY) = GridManager.Instance.GridFromWorld(start);
        (int endX, int endY) = GridManager.Instance.GridFromWorld(end);

        bool vertical = Mathf.Abs(endY - startY) > Mathf.Abs(endX - startX);
        if (vertical)
        {
            int step = startY < endY ? 1 : -1;
            for (int y = startY; y != endY + step; y += step)
                Place(GridManager.Instance.WorldFromGrid(startX, y));
        }
        else
        {
            int step = startX < endX ? 1 : -1;
            for (int x = startX; x != endX + step; x += step)
                Place(GridManager.Instance.WorldFromGrid(x, startY));
        }
    }

    private void Place(Vector3 pos)
    {
        var obj = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
        GridManager.Instance.BlockNodesUnderObject(obj);
    }

    public void ClearPreview()
    {
        ClearPreviewLine();
        if (hoverPreview)
            Object.Destroy(hoverPreview);
    }
}
