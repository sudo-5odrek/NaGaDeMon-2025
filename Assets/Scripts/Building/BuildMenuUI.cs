using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Building
{
    public class BuildMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject menuPanel;          
        public Transform buttonContainer;     
        public GameObject buttonPrefab;       

        public bool IsOpen => menuPanel != null && menuPanel.activeSelf;

        private void OnEnable()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        // --------------------------------------------------
        // MENU CONTROL
        // --------------------------------------------------

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

        // --------------------------------------------------
        // MENU POPULATION
        // --------------------------------------------------

        private void PopulateMenu()
        {
            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);

            foreach (var building in BuildManager.Instance.availableBuildings)
            {
                GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
                Button btn = btnObj.GetComponent<Button>();

                btn.onClick.AddListener(() => SelectBuilding(building));

                Transform icon = btnObj.transform.Find("Icon");
                Transform name = btnObj.transform.Find("Name");

                if (icon != null && icon.TryGetComponent(out Image img))
                    img.sprite = building.icon;

                if (name != null && name.TryGetComponent(out TMP_Text text))
                    text.text = building.buildingName;
            }
        }

        private void SelectBuilding(BuildingData building)
        {
            if (BuildManager.Instance == null) return;

            BuildManager.Instance.StartPlacement(building);
            Hide(); // Close the menu immediately after selecting
        }
    }
}
