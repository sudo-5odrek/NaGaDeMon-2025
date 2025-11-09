using Building;
using UnityEngine;

namespace Interface
{
    public interface IConnectionPlacementLogic : IBuildPlacementLogic
    {
        /// <summary>
        /// Called when connection starts from a building.
        /// </summary>
        void BeginFromBuilding(BuildingConnector building, Vector3 worldStart);

        /// <summary>
        /// Ends the drag and returns connection info.
        /// </summary>
        ConnectionPlacementResult CompleteConnection(Vector3 worldEnd);
    }
}

