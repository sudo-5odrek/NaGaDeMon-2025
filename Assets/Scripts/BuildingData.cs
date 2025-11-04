using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "TD/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public Sprite icon;
    public GameObject prefab;
    public int cost = 0; // optional, for later
}