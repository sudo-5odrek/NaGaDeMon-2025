using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    public class InventoryUIItemEntry : MonoBehaviour
    {
        [Header("References")]
        public Image iconImage;
        public TMP_Text countText;

        [HideInInspector] public ItemDefinition Item;  // The item this entry represents

        private int _currentCount;

        public void Setup(ItemDefinition item, int initialCount = 0)
        {
            Item = item;
            _currentCount = initialCount;

            if (iconImage != null)
                iconImage.sprite = item.icon; // adapt field name if needed

            RefreshCountText();
        }

        public void SetCount(int value)
        {
            _currentCount = value;
            RefreshCountText();
        }

        public void Add(int delta)
        {
            _currentCount += delta;
            if (_currentCount < 0) _currentCount = 0;
            RefreshCountText();
        }

        private void RefreshCountText()
        {
            if (countText != null)
                countText.text = _currentCount.ToString();
        }
    }
}