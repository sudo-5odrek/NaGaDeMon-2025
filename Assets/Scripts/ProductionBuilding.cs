using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // so you can test even in Edit mode
public class ProductionBuilding : MonoBehaviour
{
    [Header("Assigned Data")]
    public ProductionBuildingData productionData;

    [Header("Runtime State (Debug)")]
    [SerializeField] private RecipeData activeRecipe;
    [SerializeField] private float craftTimer;
    [SerializeField] private bool isCrafting;

    [SerializeField] private List<ResourceStack> inputView = new();
    [SerializeField] private List<ResourceStack> outputView = new();

    // internal storage dictionaries (runtime only)
    private Dictionary<string, int> inputStorage = new();
    private Dictionary<string, int> outputStorage = new();
    
    public RecipeData ActiveRecipe => activeRecipe;
    public bool IsCrafting => isCrafting;
    public float CraftTimer => craftTimer;


    // --------------------------------------------------
    // UNITY LIFECYCLE
    // --------------------------------------------------

    private void Start()
    {
        if (productionData == null)
        {
            Debug.LogError($"ProductionBuilding '{name}' missing ProductionBuildingData!");
            return;
        }

        SetRecipe(productionData.defaultRecipe);
        SyncInspectorView();
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

        SyncInspectorView();
    }

    // --------------------------------------------------
    // RECIPE
    // --------------------------------------------------

    public void SetRecipe(RecipeData recipe)
    {
        if (recipe == null) return;
        activeRecipe = recipe;
        Debug.Log($"{name} set to recipe: {recipe.recipeName}");
    }

    private bool CanStartCraft()
    {
        foreach (var input in activeRecipe.inputs)
        {
            if (!inputStorage.ContainsKey(input.resourceId) || inputStorage[input.resourceId] < input.amount)
                return false;
        }

        foreach (var output in activeRecipe.outputs)
        {
            int stored = outputStorage.ContainsKey(output.resourceId) ? outputStorage[output.resourceId] : 0;
            if (stored + output.amount > productionData.outputStorageCapacity)
                return false;
        }

        return true;
    }

    private void ConsumeInputs()
    {
        foreach (var input in activeRecipe.inputs)
            inputStorage[input.resourceId] -= input.amount;
    }

    private void FinishCraft()
    {
        foreach (var output in activeRecipe.outputs)
        {
            if (!outputStorage.ContainsKey(output.resourceId))
                outputStorage[output.resourceId] = 0;
            outputStorage[output.resourceId] += output.amount;
        }
    }

    // --------------------------------------------------
    // STORAGE
    // --------------------------------------------------

    public bool AddInput(string resourceId, int amount)
    {
        if (!inputStorage.ContainsKey(resourceId))
            inputStorage[resourceId] = 0;

        if (inputStorage[resourceId] + amount > productionData.inputStorageCapacity)
            return false;

        inputStorage[resourceId] += amount;
        SyncInspectorView();
        return true;
    }

    public int TakeOutput(string resourceId, int amount)
    {
        if (!outputStorage.ContainsKey(resourceId))
            return 0;

        int available = outputStorage[resourceId];
        int taken = Mathf.Min(available, amount);
        outputStorage[resourceId] -= taken;
        SyncInspectorView();
        return taken;
    }

    // --------------------------------------------------
    // INSPECTOR DEBUGGING
    // --------------------------------------------------

    private void SyncInspectorView()
    {
        inputView.Clear();
        foreach (var kvp in inputStorage)
            inputView.Add(new ResourceStack { resourceId = kvp.Key, amount = kvp.Value });

        outputView.Clear();
        foreach (var kvp in outputStorage)
            outputView.Add(new ResourceStack { resourceId = kvp.Key, amount = kvp.Value });
    }

    // 🔹 This method lets you live-edit resources from the inspector
    private void ApplyInspectorChanges()
    {
        inputStorage.Clear();
        foreach (var stack in inputView)
            inputStorage[stack.resourceId] = stack.amount;

        outputStorage.Clear();
        foreach (var stack in outputView)
            outputStorage[stack.resourceId] = stack.amount;
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        ApplyInspectorChanges();
    }
#endif
}
