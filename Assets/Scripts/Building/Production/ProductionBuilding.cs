using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace Building.Production
{
    /// <summary>
    /// Handles crafting logic for a production building using the unified BuildingInventory system.
    /// Inputs are pulled from input ports, outputs are pushed into output ports.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProductionBuilding : MonoBehaviour, IHasProgress
    {
        [Header("Assigned Data")]
        [Tooltip("Production parameters, including recipe and storage capacities.")]
        public ProductionBuildingData productionData;

        [Tooltip("Building inventory containing all input/output ports.")]
        public BuildingInventory buildingInventory;

        [Header("Runtime State")]
        [SerializeField] private RecipeData activeRecipe;
        [SerializeField] private float craftTimer;
        [SerializeField] private bool isCrafting;

        [Header("Debug View (Read Only)")]
        [SerializeField] private List<string> inputDebugView = new();
        [SerializeField] private List<string> outputDebugView = new();

        // Cached runtime ports
        private List<BuildingInventoryPort> inputPorts = new();
        private List<BuildingInventoryPort> outputPorts = new();

        public RecipeData ActiveRecipe => activeRecipe;
        public bool IsCrafting => isCrafting;
        public float CraftTimer => craftTimer;
        
        public bool IsProcessing => isCrafting;

        // --------------------------------------------------
        // UNITY LIFECYCLE
        // --------------------------------------------------
        public float ProgressNormalized
        {
            get
            {
                if (activeRecipe == null || !isCrafting) return 0f;
                return Mathf.Clamp01(1f - (craftTimer / activeRecipe.craftTime));
            }
        }
        
        private void Start()
        {
            if (productionData == null)
            {
                Debug.LogError($"⚠️ {name}: Missing ProductionBuildingData!");
                return;
            }

            if (buildingInventory == null)
            {
                Debug.LogError($"⚠️ {name}: Missing BuildingInventory reference!");
                return;
            }

            CachePorts();
            SetRecipe(productionData.defaultRecipe);
            SyncDebugView();
        }

        private void Update()
        {
            if (!Application.isPlaying || activeRecipe == null)
                return;

            if (!isCrafting)
            {
                if (CanStartCraft())
                {
                    ConsumeInputs();
                    isCrafting = true;
                    craftTimer = activeRecipe.craftTime;
                }
            }
            else
            {
                craftTimer -= Time.deltaTime;
                if (craftTimer <= 0f)
                {
                    FinishCraft();
                    isCrafting = false;
                }
            }

            SyncDebugView();
        }

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------

        private void CachePorts()
        {
            inputPorts.Clear();
            outputPorts.Clear();

            foreach (var port in buildingInventory.ports)
            {
                switch (port.portType)
                {
                    case BuildingInventoryPort.PortType.Input:
                        inputPorts.Add(port);
                        break;
                    case BuildingInventoryPort.PortType.Output:
                        outputPorts.Add(port);
                        break;
                    case BuildingInventoryPort.PortType.Both:
                        // Both-type ports can handle both input and output
                        inputPorts.Add(port);
                        outputPorts.Add(port);
                        break;
                }
            }

            if (inputPorts.Count == 0)
                Debug.LogWarning($"⚠️ {name}: No input ports found!");
            if (outputPorts.Count == 0)
                Debug.LogWarning($"⚠️ {name}: No output ports found!");
        }

        // --------------------------------------------------
        // RECIPE LOGIC
        // --------------------------------------------------

        public void SetRecipe(RecipeData recipe)
        {
            if (recipe == null) return;
            activeRecipe = recipe;
            Debug.Log($"🏭 {name} set to recipe: {recipe.recipeName}");
        }

        private bool CanStartCraft()
        {
            // 1️⃣ Check all inputs are available
            foreach (var input in activeRecipe.inputs)
            {
                float available = 0f;
                foreach (var port in inputPorts)
                    available += port.GetItemAmount(input.itemDefinition);

                if (available < input.amount)
                    return false;
            }

            // 2️⃣ Check if outputs can fit
            foreach (var output in activeRecipe.outputs)
            {
                float space = 0f;
                foreach (var port in outputPorts)
                    space += port.GetFreeSpace();

                if (space < output.amount)
                    return false;
            }

            return true;
        }

        private void ConsumeInputs()
        {
            foreach (var input in activeRecipe.inputs)
            {
                float remaining = input.amount;
                foreach (var port in inputPorts)
                {
                    if (remaining <= 0) break;
                    float removed = port.Remove(input.itemDefinition, remaining);
                    remaining -= removed;
                }
            }
        }

        private void FinishCraft()
        {
            foreach (var output in activeRecipe.outputs)
            {
                float remaining = output.amount;
                foreach (var port in outputPorts)
                {
                    if (remaining <= 0) break;
                    float added = port.Add(output.itemDefinition, remaining);
                    remaining -= added;
                }
            }
        }

        // --------------------------------------------------
        // DEBUG / INSPECTOR
        // --------------------------------------------------

        private void SyncDebugView()
        {
            inputDebugView.Clear();
            outputDebugView.Clear();

            foreach (var port in inputPorts)
            {
                var inv = port.RuntimeInventory;
                foreach (var kvp in inv.GetAll())
                    inputDebugView.Add($"[{port.portName}] {kvp.Key}:{kvp.Value}");
            }

            foreach (var port in outputPorts)
            {
                var inv = port.RuntimeInventory;
                foreach (var kvp in inv.GetAll())
                    outputDebugView.Add($"[{port.portName}] {kvp.Key}:{kvp.Value}");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (buildingInventory == null) return;

            Vector3 pos = transform.position + Vector3.up * 2.0f;
            string label = $"Active Recipe: {(activeRecipe ? activeRecipe.recipeName : "None")}\n";

            foreach (var port in inputPorts)
            {
                label += $"IN {port.portName}: ";
                foreach (var kvp in port.RuntimeInventory.GetAll())
                    label += $"{kvp.Key}:{kvp.Value} ";
                label += "\n";
            }

            foreach (var port in outputPorts)
            {
                label += $"OUT {port.portName}: ";
                foreach (var kvp in port.RuntimeInventory.GetAll())
                    label += $"{kvp.Key}:{kvp.Value} ";
                label += "\n";
            }

            UnityEditor.Handles.Label(pos, label);
        }
#endif
    }
}
