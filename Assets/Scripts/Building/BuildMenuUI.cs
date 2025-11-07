using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class BuildMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;          // The root panel of the build menu
    public Transform buttonContainer;     // Parent for all dynamically created buttons
    public GameObject buttonPrefab;       // A prefab with: Button + Icon (Image) + Name (TMP Text)

    private InputSystem_Actions input;

    private void Awake()
    {
        // Reuse the same input instance used globally
        input = InputContextManager.Instance.input;

        // Bind the toggle menu input (e.g. B key)
        input.Player.BuildMenu.performed += OnToggleMenu;
    }

    private void OnEnable()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        input.Player.BuildMenu.performed -= OnToggleMenu;
    }

    // --------------------------------------------------
    // MENU MANAGEMENT
    // --------------------------------------------------

    private void OnToggleMenu(InputAction.CallbackContext ctx)
    {
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;

        bool isOpen = menuPanel.activeSelf;
        menuPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            PopulateMenu();
        }
        else
        {
            // If menu closes while placing, cancel placement
            if (BuildManager.Instance != null && BuildManager.Instance.isActiveAndEnabled)
            {
                InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
            }
        }
    }

    // --------------------------------------------------
    // POPULATION
    // --------------------------------------------------

    private void PopulateMenu()
    {
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        // Create new buttons from the building data in BuildManager
        foreach (var building in BuildManager.Instance.availableBuildings)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();

            // Assign onClick event
            btn.onClick.AddListener(() => SelectBuilding(building));

            // Optional visuals (depends on your prefab structure)
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
        menuPanel.SetActive(false);

        // Switch input context
        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
    }
}
