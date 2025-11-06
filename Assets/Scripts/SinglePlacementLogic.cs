using UnityEngine;

[CreateAssetMenu(menuName = "Build/Placement Logic/Single")]
public class SinglePlacementLogic : ScriptableObject, IBuildPlacementLogic
{
    private GameObject prefab;
    private float rotation;
    private GameObject previewObject;

    public void Setup(GameObject prefab, float rotation)
    {
        this.prefab = prefab;
        this.rotation = rotation;
        CreatePreview();
    }

    public void OnStartDrag(Vector3 start) { } // not used here

    public void OnDragging(Vector3 current) { } // not used here

    public void OnEndDrag(Vector3 worldEnd)
    {
        // Place real object
        Place(worldEnd);
        ClearPreview();
    }

    public void UpdatePreview(Vector3 worldCurrent)
    {
        if (previewObject)
            previewObject.transform.position = worldCurrent;
    }

    public void ClearPreview()
    {
        if (previewObject)
            Object.Destroy(previewObject);
    }

    private void CreatePreview()
    {
        previewObject = Object.Instantiate(prefab);
        BuildUtils.MakePreview(previewObject);
    }

    private void Place(Vector3 pos)
    {
        var obj = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
        GridManager.Instance.BlockNodesUnderObject(obj);
    }
}
