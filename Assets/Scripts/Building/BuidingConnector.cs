using UnityEngine;
using System.Collections.Generic;

namespace Building
{
    public class BuildingConnector : MonoBehaviour
    {
        [Header("Connection Settings")]
        public bool canOutput = true;
        public bool canInput = true;
        public int maxOutputs = 1;
        public int maxInputs = 1;

        [Header("Registered Connections")]
        public List<Vector3Int> outputTilePositions = new(); // ✅ first conveyor tile positions
        public List<BuildingConnector> inputSources = new();

        public Vector3Int GridOrigin { get; private set; }

        private void Start()
        {
            (int gx, int gy) = Grid.GridManager.Instance.GridFromWorld(transform.position);
            GridOrigin = new Vector3Int(gx, gy, 0);
        }

        public bool CanAcceptInput() => canInput && inputSources.Count < maxInputs;
        public bool CanProvideOutput() => canOutput && outputTilePositions.Count < maxOutputs;

        public void RegisterOutputPosition(Vector3Int firstTileGrid)
        {
            if (!outputTilePositions.Contains(firstTileGrid))
                outputTilePositions.Add(firstTileGrid);
        }

        public void RegisterInput(BuildingConnector source)
        {
            if (!inputSources.Contains(source))
                inputSources.Add(source);
        }
    }
}