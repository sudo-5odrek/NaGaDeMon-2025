using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Building.Scripts
{
    public class CostEntryUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        public void Set(Sprite iconSprite, int amount)
        {
            icon.sprite = iconSprite;
            amountText.text = amount.ToString();
        }
    }
}