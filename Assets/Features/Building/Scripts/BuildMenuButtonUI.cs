using NaGaDeMon.Features.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NaGaDeMon.Features.Building
{
    public class BuildMenuButtonUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Transform costRoot;

        [Header("Cost UI")]
        [Tooltip("Prefab that includes an icon (Image) and a TMP_Text for the amount.")]
        [SerializeField] private GameObject costEntryPrefab;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Setup(BuildingData building, System.Action onClick)
        {
            if (button == null)
                button = GetComponent<Button>();
            // ---------------------------
            // Set main icon + name
            // ---------------------------
            icon.sprite = building.icon;
            nameText.text = building.buildingName;

            // ---------------------------
            // Clear old cost entries
            // ---------------------------
            foreach (Transform child in costRoot)
                Destroy(child.gameObject);

            // ---------------------------
            // Generate cost entries
            // ---------------------------
            foreach (var cost in building.cost)
            {
                var entry = Instantiate(costEntryPrefab, costRoot);

                // Prefab must have: CostEntryUI script
                var ui = entry.GetComponent<CostEntryUI>();

                if (ui == null)
                {
                    Debug.LogError("CostEntryPrefab is missing CostEntryUI component.");
                    continue;
                }

                ui.Set(cost.item.icon, cost.amount);
            }

            // ---------------------------
            // Set the button callback
            // ---------------------------
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}