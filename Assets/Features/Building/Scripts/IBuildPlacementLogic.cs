using System.Collections.Generic;
using Building;
using UnityEngine;

namespace Interface
{
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

        int GetPreviewCount();
    
        /// <summary>
        /// Called every frame after the preview updates.
        /// Should update the UI cost display based on preview count.
        /// </summary>
        void UpdateCostPreview(BuildingData data);

        /// <summary>
        /// Called to get the current placement world position
        /// for positioning the cost label.
        /// </summary>
        Vector3 GetPreviewPlacement();
        
        /// <summary>
        /// Called by controllers (BuildManager / ConnectionModeManager)
        /// to tint all preview ghosts (green/red).
        /// </summary>
        void SetGhostColor(Color color);
        
        
        /// <summary>
        /// Called when a drag ends without placing anything 
        /// (e.g. too expensive, invalid, cancelled).
        /// Should clear line ghosts and restore hover preview.
        /// </summary>
        void AbortDrag();
        
        bool ValidatePlacement(out object context);
        List<Vector3> GetPlacementPositions();
    }
}