using Building;
using UnityEngine;

public struct ConnectionPlacementResult
{
    public bool success;                  // was placement valid?
    public Vector3Int? firstTile;         // grid of first conveyor tile (for registration)
    public BuildingConnector start;       // building at start, if any
    public BuildingConnector target;      // building at end, if any
}