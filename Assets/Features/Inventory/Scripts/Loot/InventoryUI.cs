using NaGaDeMon.Features.Player;
using TMPro;
using UnityEngine;

namespace Features.Inventory.Scripts.Loot
{
    public class ContextUI : MonoBehaviour
    {
        public LootType displayType = LootType.Gold;
        public TextMeshProUGUI text;
        public TextMeshProUGUI contextText;

        private void OnEnable()
        {
            // Subscribe to context changes
            if (InputContextManager.Instance != null)
                InputContextManager.Instance.OnContextChange += UpdateContext;

            // Update immediately
            UpdateContext();
        }

        private void OnDisable()
        {

            if (InputContextManager.Instance != null)
                InputContextManager.Instance.OnContextChange -= UpdateContext;
        }

        

        private void UpdateContext()
        {
            if (InputContextManager.Instance == null || contextText == null)
                return;

            string modeLabel = InputContextManager.Instance.CurrentMode switch
            {
                InputContextManager.InputMode.Normal => "",
                InputContextManager.InputMode.Build => "Building Mode",
                InputContextManager.InputMode.Connect => "Connection Mode",
                _ => "Unknown Mode"
            };

            contextText.text = modeLabel;
        }
    }
}