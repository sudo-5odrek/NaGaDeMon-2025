using UnityEngine;
using System.Collections.Generic;
using Grid;

namespace Building
{
    /// <summary>
    /// Handles logical links between buildings (inputs/outputs).
    /// Tracks first connection tiles and other connectors feeding into this building.
    /// </summary>
    public class BuildingConnector : MonoBehaviour
    {
        [Header("Connection Settings")]
        [Tooltip("Whether this building can output resources/connections.")]
        public bool canOutput = true;

        [Tooltip("Whether this building can receive input from another source.")]
        public bool canInput = true;

        [Tooltip("Maximum number of outgoing connections allowed.")]
        public int maxOutputs = 1;

        [Tooltip("Maximum number of input connections allowed.")]
        public int maxInputs = 1;

        [Header("Registered Connections")]
        [Tooltip("Grid coordinates of first conveyor tiles placed from this building.")]
        public List<Vector3Int> outputTilePositions = new();

        [Tooltip("Other connectors feeding into this building.")]
        public List<BuildingConnector> inputSources = new();

        public Vector3Int GridOrigin { get; private set; }

        private void Start()
        {
            (int gx, int gy) = GridManager.Instance.GridFromWorld(transform.position);
            GridOrigin = new Vector3Int(gx, gy, 0);
        }

        // --------------------------------------------------
        // STATE CHECKS
        // --------------------------------------------------

        public bool CanAcceptInput() => canInput && inputSources.Count < maxInputs;
        public bool CanProvideOutput() => canOutput && outputTilePositions.Count < maxOutputs;

        // --------------------------------------------------
        // REGISTRATION
        // --------------------------------------------------

        /// <summary>
        /// Registers an output connection starting from this building.
        /// Called by ConnectionModeManager when placement logic confirms placement.
        /// </summary>
        /// <param name="firstConnection">The first connection object placed after this building.</param>
        /// <param name="worldPosition">World position of the first connection tile.</param>
        public void RegisterOutputConnection(GameObject firstConnection, Vector3 worldPosition)
        {
            if (!CanProvideOutput())
            {
                Debug.LogWarning($"[{name}] Reached max output limit ({maxOutputs}).");
                return;
            }

            (int gx, int gy) = GridManager.Instance.GridFromWorld(worldPosition);
            Vector3Int gridPos = new(gx, gy, 0);

            if (!outputTilePositions.Contains(gridPos))
            {
                outputTilePositions.Add(gridPos);
                Debug.Log($"🔗 [{name}] registered output connection at {gridPos}");
            }
        }

        /// <summary>
        /// Registers another building as an input source.
        /// </summary>
        public void RegisterInput(BuildingConnector source)
        {
            if (!CanAcceptInput())
            {
                Debug.LogWarning($"[{name}] Reached max input limit ({maxInputs}).");
                return;
            }

            if (!inputSources.Contains(source))
            {
                inputSources.Add(source);
                Debug.Log($"🔗 [{name}] accepted input from {source.name}");
            }
        }

        /// <summary>
        /// Clears all registered connections (optional for rebuilding or destruction).
        /// </summary>
        public void ClearConnections()
        {
            outputTilePositions.Clear();
            inputSources.Clear();
        }
    }
}
