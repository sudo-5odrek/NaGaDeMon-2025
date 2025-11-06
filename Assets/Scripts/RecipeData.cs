using UnityEngine;

[CreateAssetMenu(menuName = "Production/Recipe")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public ResourceStack[] inputs;
    public ResourceStack[] outputs;
    public float craftTime = 3f; // seconds
}

