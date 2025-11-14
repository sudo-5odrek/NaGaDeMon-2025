using UnityEngine;

public static class BuildUtils
{
    // --------------------------------------------------------------------
    // INITIAL PREVIEW SETUP
    // --------------------------------------------------------------------
    public static void MakePreview(GameObject obj)
    {
        // Ignore Raycasts so it doesn’t interfere with clicks
        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Tint sprite (default preview look)
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            var c = sr.color;
            c.a = 0.4f;
            sr.color = c;
        }

        // Disable any gameplay scripts except Transform/Renderer
        foreach (var comp in obj.GetComponentsInChildren<MonoBehaviour>())
        {
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

    // --------------------------------------------------------------------
    // SET COLOR TINT FOR PREVIEW (VALID/INVALID)
    // --------------------------------------------------------------------
    public static void SetPreviewTint(GameObject obj, Color tint)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = tint;

            // Keep original alpha if tint has full opacity
            if (Mathf.Approximately(tint.a, 1f))
                c.a = sr.color.a; // usually 0.4f

            sr.color = c;
        }
    }
}