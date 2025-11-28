using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NaGaDeMon.Features.Building
{
    public class BuildMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject menuPanel;
        public Transform buttonContainer;
        public GameObject buttonPrefab;

        [Header("Cost UI")]
        [Tooltip("Prefab that contains a TMP_Text component to display a single cost line.")]
        public GameObject costTextPrefab;

        public bool IsOpen => menuPanel != null && menuPanel.activeSelf;

        private void OnEnable()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        // ----------------------------------------------------------------------
        // MENU CONTROL
        // ----------------------------------------------------------------------

        public void Show()
        {
            if (menuPanel == null) return;
            PopulateMenu();
            menuPanel.SetActive(true);
        }

        public void Hide()
        {
            if (menuPanel == null) return;
            if (!menuPanel.activeSelf) return;
            menuPanel.SetActive(false);
        }

        public void ToggleMenu()
        {
            if (menuPanel == null) return;
            if (IsOpen)
                Hide();
            else
                Show();
        }

        // ----------------------------------------------------------------------
        // MENU POPULATION
        // ----------------------------------------------------------------------

        private void PopulateMenu()
        {
            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);

            foreach (var building in BuildManager.Instance.availableBuildings)
            {
                GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);

                // Here’s the magic:
                var ui = btnObj.GetComponent<BuildMenuButtonUI>();

                ui.Setup(building, () =>
                {
                    SelectBuilding(building);
                });
            }
        }


        private void SelectBuilding(BuildingData building)
        {
            if (BuildManager.Instance == null) return;

            BuildManager.Instance.StartPlacement(building);
            Hide();
        }
    }
}
