using UnityEngine;

public static class BuildUtils
{
    public static void MakePreview(GameObject obj)
    {
        // Ignore Raycasts so it doesn’t interfere with clicks
        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Tint sprite
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
            sr.color = new Color(1f, 1f, 1f, 0.4f);

        // Disable any gameplay scripts except Transform/Renderer
        foreach (var comp in obj.GetComponentsInChildren<MonoBehaviour>())
        {
            // Keep SpriteRenderer + Transform only
            if (comp is SpriteRenderer) continue;
            comp.enabled = false;
        }

        // Disable colliders to avoid blocking placement
        foreach (var col in obj.GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // Disable rigidbodies to prevent physics
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody2D>())
            rb.simulated = false;
    }
}

