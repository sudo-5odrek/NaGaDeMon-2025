using UnityEngine;

public interface IBuildPlacementLogic
{
    /// <summary>
    /// Called when a building is selected and placement begins.
    /// Creates preview visuals and resets internal state.
    /// </summary>
    void Setup(GameObject prefab, float rotation);

    /// <summary>
    /// Called every frame while NOT dragging.
    /// Moves preview to the cursor.
    /// </summary>
    void UpdatePreview(Vector3 worldPos);

    /// <summary>
    /// Called once when the player presses the mouse button.
    /// </summary>
    void OnStartDrag(Vector3 startPos);

    /// <summary>
    /// Called every frame while dragging (for line placement, area placement, etc.)
    /// </summary>
    void OnDragging(Vector3 currentPos);

    /// <summary>
    /// Called once when the player releases the mouse button.
    /// Should place objects and destroy previews.
    /// </summary>
    void OnEndDrag(Vector3 endPos);

    /// <summary>
    /// Clears previews and resets the logic when the user cancels.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Called when the player rotates the object.
    /// Logic must update preview rotations.
    /// </summary>
    void ApplyRotation(float rotation);

    /// <summary>
    /// Destroys all preview visuals.
    /// </summary>
    void ClearPreview();
}