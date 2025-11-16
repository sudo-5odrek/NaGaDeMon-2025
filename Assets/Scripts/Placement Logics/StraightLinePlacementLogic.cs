using UnityEngine;
using System.Collections.Generic;
using Grid;

[CreateAssetMenu(menuName = "Build/Placement Logic/Straight Line")]
public class StraightLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    private GameObject prefab;
    private float rotation;

    private Vector3 worldStart;
    private bool isDragging = false;

    // Hover preview (before drag)
    private GameObject hoverPreview;

    // Line preview during drag
    private readonly List<GameObject> previewLine = new();

    // Optional callback when line is placed (useful for conveyors)
    private System.Action<List<GameObject>> onPlaced;

    public void SetPlacementCallback(System.Action<List<GameObject>> callback)
    {
        onPlaced = callback;
    }

    // ----------------------------------------------------------------------
    // SETUP
    // ----------------------------------------------------------------------

    public void Setup(GameObject prefab, float rotation)
    {
        this.prefab = prefab;
        this.rotation = rotation;

        CreateHoverPreview();
    }

    // ----------------------------------------------------------------------
    // ROTATION INPUT
    // ----------------------------------------------------------------------

    public void ApplyRotation(float newRotation)
    {
        rotation = newRotation;

        // update preview objects
        if (hoverPreview)
            hoverPreview.transform.rotation = Quaternion.Euler(0, 0, rotation);

        foreach (var g in previewLine)
            if (g)
                g.transform.rotation = Quaternion.Euler(0, 0, rotation);
    }

    // ----------------------------------------------------------------------
    // PREVIEW (before dragging)
    // ----------------------------------------------------------------------

    public void UpdatePreview(Vector3 worldCurrent)
    {
        if (!isDragging && hoverPreview)
            hoverPreview.transform.position = worldCurrent;
    }

    // ----------------------------------------------------------------------
    // DRAG LOGIC
    // ----------------------------------------------------------------------

    public void OnStartDrag(Vector3 startWorldPos)
    {
        isDragging = true;
        worldStart = startWorldPos;

        // remove single hover ghost
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
        ClearPreviewLine();

        // Final placement
        PlaceLine(worldStart, worldEnd);
    }

    // ----------------------------------------------------------------------
    // INTERNAL — Ghosts & Placement
    // ----------------------------------------------------------------------

    private void CreateHoverPreview()
    {
        ClearPreviewLine();

        if (!prefab) return;

        hoverPreview = Object.Instantiate(prefab);
        BuildUtils.MakePreview(hoverPreview);
        hoverPreview.transform.rotation = Quaternion.Euler(0, 0, rotation);
    }

    private void ClearPreviewLine()
    {
        foreach (var g in previewLine)
            if (g) Object.Destroy(g);

        previewLine.Clear();
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
            {
                Vector3 pos = GridManager.Instance.WorldFromGrid(startX, y);
                previewLine.Add(CreateGhost(pos));
            }
        }
        else
        {
            int step = startX < endX ? 1 : -1;

            for (int x = startX; x != endX + step; x += step)
            {
                Vector3 pos = GridManager.Instance.WorldFromGrid(x, startY);
                previewLine.Add(CreateGhost(pos));
            }
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
        var placedObjects = new List<GameObject>();

        (int startX, int startY) = GridManager.Instance.GridFromWorld(start);
        (int endX, int endY) = GridManager.Instance.GridFromWorld(end);

        bool vertical = Mathf.Abs(endY - startY) > Mathf.Abs(endX - startX);

        if (vertical)
        {
            int step = startY < endY ? 1 : -1;

            for (int y = startY; y != endY + step; y += step)
            {
                Vector3 pos = GridManager.Instance.WorldFromGrid(startX, y);
                placedObjects.Add(Place(pos));
            }
        }
        else
        {
            int step = startX < endX ? 1 : -1;

            for (int x = startX; x != endX + step; x += step)
            {
                Vector3 pos = GridManager.Instance.WorldFromGrid(x, startY);
                placedObjects.Add(Place(pos));
            }
        }

        // Give higher systems the placed line
        onPlaced?.Invoke(placedObjects);
    }

    private GameObject Place(Vector3 pos)
    {
        var obj = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
        GridManager.Instance.BlockNodesUnderObject(obj);
        return obj;
    }

    // ----------------------------------------------------------------------
    // CANCEL & CLEAR
    // ----------------------------------------------------------------------

    public void Cancel()
    {
        isDragging = false;
        ClearPreview();
    }

    public void ClearPreview()
    {
        ClearPreviewLine();

        if (hoverPreview)
            Object.Destroy(hoverPreview);

        hoverPreview = null;
    }
}
