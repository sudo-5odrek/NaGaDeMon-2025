using UnityEditor;
using UnityEngine;
using Inventory;

[CustomEditor(typeof(BuildingInventory))]
public class BuildingInventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Port Inventories", EditorStyles.boldLabel);

        BuildingInventory inv = (BuildingInventory)target;

        // Only show during play mode (since runtime inventory is created at runtime)
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Port inventory info is available in Play Mode.", MessageType.Info);
            return;
        }

        foreach (var port in inv.ports)
        {
            if (port == null)
                continue;

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Port: {port.portName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Type: {port.portType}");
            EditorGUILayout.LabelField($"Assigned Item: {port.itemDefinition?.displayName ?? "(unassigned)"}");

            var runtimeInv = port.RuntimeInventory;
            var dict = runtimeInv.GetAll();

            if (dict.Count == 0)
            {
                EditorGUILayout.LabelField("Contents: (empty)");
            }
            else
            {
                EditorGUILayout.LabelField("Contents:");
                EditorGUI.indentLevel++;

                foreach (var kvp in dict)
                {
                    EditorGUILayout.LabelField($"• {kvp.Key}: {kvp.Value}");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }
}