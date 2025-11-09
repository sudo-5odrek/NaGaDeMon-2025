using UnityEngine;
using System.Collections.Generic;
using Grid; // ✅ Needed for GridManager and Node

[CreateAssetMenu(menuName = "Build/Placement Logic/Pathfinding")]
public class PathfindingPlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    [Header("Visual Settings")]
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.4f);

    private GameObject prefab;
    private float rotation;

    private Vector3 worldStart;
    private bool isDragging = false;

    private GameObject hoverPreview;
    private readonly List<GameObject> previewTiles = new();
    private List<Node> currentPath = new();

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
    // PREVIEW (hover before drag)
    // --------------------------------------------------

    public void UpdatePreview(Vector3 worldCurrent)
    {
        if (!isDragging && hoverPreview)
        {
            hoverPreview.transform.position = GridManager.Instance.GetClosestNodeWorldPos(worldCurrent);
        }
    }

    // --------------------------------------------------
    // CLICK → DRAG → RELEASE
    // --------------------------------------------------

    public void OnStartDrag(Vector3 worldStart)
    {
        this.worldStart = worldStart;
        isDragging = true;

        if (hoverPreview)
            Object.Destroy(hoverPreview);

        ClearPreviewTiles();
    }

    public void OnDragging(Vector3 worldCurrent)
    {
        if (!isDragging) return;

        ClearPreviewTiles();

        // --- Get start and end nodes ---
        Node startNode = GridManager.Instance.GetClosestNode(worldStart);
        Node endNode = GridManager.Instance.GetClosestNode(worldCurrent);
        if (startNode == null || endNode == null) return;

        // --- Compute path ---
        currentPath = Pathfinder.FindPath(startNode, endNode, false);
        if (currentPath == null) return;

        // --- Draw ghosts along path ---
        foreach (var node in currentPath)
        {
            Vector3 worldPos = GridManager.Instance.WorldFromGrid(node.x, node.y);
            previewTiles.Add(CreateGhost(worldPos));
        }
    }

    public void OnEndDrag(Vector3 worldEnd)
    {
        if (!isDragging) return;

        ClearPreviewTiles();
        PlacePath(worldStart, worldEnd);

        isDragging = false;
        CreateHoverPreview();
    }

    public void ClearPreview()
    {
        ClearPreviewTiles();
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

    private void ClearPreviewTiles()
    {
        foreach (var g in previewTiles)
            if (g) Object.Destroy(g);
        previewTiles.Clear();
    }

    private GameObject CreateGhost(Vector3 pos)
    {
        GameObject ghost = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
        BuildUtils.MakePreview(ghost);
        return ghost;
    }

    private void PlacePath(Vector3 startWorld, Vector3 endWorld)
    {
        Node startNode = GridManager.Instance.GetClosestNode(startWorld);
        Node endNode = GridManager.Instance.GetClosestNode(endWorld);
        if (startNode == null || endNode == null) return;

        currentPath = Pathfinder.FindPath(startNode, endNode, false);
        if (currentPath == null || currentPath.Count == 0) return;

        foreach (var node in currentPath)
        {
            Vector3 worldPos = GridManager.Instance.WorldFromGrid(node.x, node.y);
            var obj = Object.Instantiate(prefab, worldPos, Quaternion.Euler(0, 0, rotation));
            GridManager.Instance.BlockNodesUnderObject(obj);
        }
    }
}
