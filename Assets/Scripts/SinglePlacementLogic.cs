using Grid;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/Placement Logic/Single")]
public class SinglePlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    private GameObject prefab;
    private float rotation;
    private GameObject previewObject;
    private bool isPlacing = false;
    
    public void SetPlacementCallback(System.Action<Vector3, GameObject> callback)
    {
       
    }

    // --------------------------------------------------
    // SETUP
    // --------------------------------------------------

    public void Setup(GameObject prefab, float rotation)
    {
        // Avoid recreating preview mid-placement
        if (isPlacing) return;

        this.prefab = prefab;
        this.rotation = rotation;
        CreatePreview();
    }

    // --------------------------------------------------
    // PREVIEW & DRAG
    // --------------------------------------------------

    public void OnStartDrag(Vector3 start)
    {
        isPlacing = true;
    }

    public void OnDragging(Vector3 current)
    {
        // Single placement — not used
    }

    public void OnEndDrag(Vector3 worldEnd)
    {
        // ✅ Place object at target
        Place(worldEnd);

        // ✅ Clear preview immediately after placement
        ClearPreview();

        // ✅ Release placement flag
        isPlacing = false;
    }

    public void UpdatePreview(Vector3 worldCurrent)
    {
        if (previewObject)
            previewObject.transform.position = worldCurrent;
    }

    public void ClearPreview()
    {
        if (previewObject)
        {
            Object.Destroy(previewObject);
            previewObject = null;
        }
    }

    // --------------------------------------------------
    // INTERNAL HELPERS
    // --------------------------------------------------

    private void CreatePreview()
    {
        previewObject = Object.Instantiate(prefab, Vector3.zero, Quaternion.Euler(0, 0, rotation));
        BuildUtils.MakePreview(previewObject);
    }

    private void Place(Vector3 pos)
    {
        var obj = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
        GridManager.Instance.BlockNodesUnderObject(obj);
    }
}
