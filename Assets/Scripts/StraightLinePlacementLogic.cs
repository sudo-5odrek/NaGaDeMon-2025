using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Build/Placement Logic/Straight Line")]
public class StraightLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    [Header("Visual Settings")]
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.4f);

    private GameObject prefab;
    private float rotation;

    private Vector3 worldStart;
    private bool isDragging = false;

    private GameObject hoverPreview;                // single ghost under cursor before drag
    private readonly List<GameObject> previewLine = new(); // line of ghosts during drag

    // --------------------------------------------------
    // SETUP
    // --------------------------------------------------

    public void Setup(GameObject prefab, float rotation)
    {
        this.prefab = prefab;
        this.rotation = rotation;
        CreateHoverPreview();
    }

    // --------------------------------------------------
    // HOVER PREVIEW (before click)
    // --------------------------------------------------

    public void UpdatePreview(Vector3 worldCurrent)
    {
        // Only show single-cell hover preview before clicking
        if (!isDragging && hoverPreview)
        {
            hoverPreview.transform.position = worldCurrent;
        }
    }

    // --------------------------------------------------
    // CLICK → DRAG → RELEASE
    // --------------------------------------------------

    public void OnStartDrag(Vector3 worldStart)
    {
        this.worldStart = worldStart;
        isDragging = true;

        // Remove hover preview once dragging starts
        if (hoverPreview)
            Object.Destroy(hoverPreview);

        ClearPreviewLine();
    }

    public void OnDragging(Vector3 worldCurrent)
    {
        if (!isDragging) return;
        Debug.Log("Dragging from " + worldStart + " to " + worldCurrent);

        ClearPreviewLine();
        DrawPreviewLine(worldStart, worldCurrent);
    }

    public void OnEndDrag(Vector3 worldEnd)
    {
        if (!isDragging) return;

        ClearPreviewLine();
        PlaceLine(worldStart, worldEnd);

        isDragging = false;
        CreateHoverPreview(); // return to hover mode after placement
    }

    public void ClearPreview()
    {
        ClearPreviewLine();
        if (hoverPreview) Object.Destroy(hoverPreview);
    }

    // --------------------------------------------------
    // INTERNAL HELPERS
    // --------------------------------------------------

    private void CreateHoverPreview()
    {
        hoverPreview = Object.Instantiate(prefab);
        BuildUtils.MakePreview(hoverPreview);
    }

    private void ClearPreviewLine()
    {
        foreach (var g in previewLine)
            if (g) Object.Destroy(g);
        previewLine.Clear();
    }

    private void DrawPreviewLine(Vector3 startWorld, Vector3 endWorld)
    {
        (int startX, int startY) = GridManager.Instance.GridFromWorld(startWorld);
        (int endX, int endY) = GridManager.Instance.GridFromWorld(endWorld);

        // Decide main axis
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

    private void PlaceLine(Vector3 startWorld, Vector3 endWorld)
    {
        (int startX, int startY) = GridManager.Instance.GridFromWorld(startWorld);
        (int endX, int endY) = GridManager.Instance.GridFromWorld(endWorld);

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
}
