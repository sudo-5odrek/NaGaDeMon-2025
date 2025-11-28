using System.Collections.Generic;
using Features.Building.Scripts.Building_Inventory;
using NaGaDeMon.Features.Building.Inventory;
using NaGaDeMon.Features.Inventory;
using UnityEngine;

namespace Features.Connections.Scripts
{
    public class Splitter : MonoBehaviour
    {
        private BuildingInventory inv;

        private BuildingInventoryPort receivePort;
        private BuildingInventoryPort output1;
        private BuildingInventoryPort output2;
        private BuildingInventoryPort output3;

        // Round-robin index among *active* outputs
        private int currentIndex = 0;

        private void Awake()
        {
            inv = GetComponent<BuildingInventory>();

            if (inv == null)
            {
                Debug.LogError($"Splitter on {name} is missing a BuildingInventory component!");
                enabled = false;
                return;
            }

            // Resolve ports by name
            receivePort = inv.GetPort("Receive") as BuildingInventoryPort;
            output1     = inv.GetPort("Output1") as BuildingInventoryPort;
            output2     = inv.GetPort("Output2") as BuildingInventoryPort;
            output3     = inv.GetPort("Output3") as BuildingInventoryPort;

            // Sanity checks (optional, but nice during setup)
            if (receivePort == null) Debug.LogError($"{name}: Missing 'Receive' port!");
            if (output1 == null)     Debug.LogError($"{name}: Missing 'Output1' port!");
            if (output2 == null)     Debug.LogError($"{name}: Missing 'Output2' port!");
            if (output3 == null)     Debug.LogError($"{name}: Missing 'Output3' port!");
        }

        private void OnEnable()
        {
            if (receivePort != null)
                receivePort.OnItemAdded += OnItemReceived;
        }

        private void OnDisable()
        {
            if (receivePort != null)
                receivePort.OnItemAdded -= OnItemReceived;
        }

        /// <summary>
        /// Called every time something is added to the Receive port.
        /// We try to immediately push 1 unit into the next available output.
        /// </summary>
        private void OnItemReceived(ItemDefinition item, float amount)
        {
            if (item == null || amount <= 0f)
                return;

            if (receivePort == null || receivePort.IsEmpty)
                return;

            var outputs = GetActiveOutputs();
            if (outputs.Count == 0)
                return;

            // Make sure index is in range
            if (currentIndex >= outputs.Count)
                currentIndex %= outputs.Count;

            var target = outputs[currentIndex];

            // Move exactly 1 unit per event (assuming belts push 1 at a time)
            float removed = receivePort.Remove(item, 1f);
            if (removed <= 0f)
                return;

            float added = target.Add(item, removed);

            if (added < removed)
            {
                // Output is full or rejected some amount → put it back
                receivePort.Add(item, removed - added);

                // Still advance index so we don't get stuck on a blocked output
                currentIndex = (currentIndex + 1) % outputs.Count;
            }
            else
            {
                // Success → advance to next output port
                currentIndex = (currentIndex + 1) % outputs.Count;
            }
        }

        /// <summary>
        /// Returns a list of output ports that are effectively "connected" and not full.
        /// Uses inv.currentOutputs to know how many outputs are actually in use.
        /// </summary>
        private List<BuildingInventoryPort> GetActiveOutputs()
        {
            var list = new List<BuildingInventoryPort>(3);

            int outputsConnected = inv.currentOutputs;

            if (outputsConnected >= 1 && output1 != null && !output1.IsFull)
                list.Add(output1);

            if (outputsConnected >= 2 && output2 != null && !output2.IsFull)
                list.Add(output2);

            if (outputsConnected >= 3 && output3 != null && !output3.IsFull)
                list.Add(output3);

            return list;
        }
    }
}
