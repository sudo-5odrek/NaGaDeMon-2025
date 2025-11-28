using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Inventory.Scripts
{
    public class ItemWheelSegment : MonoBehaviour, IPointerEnterHandler
    {
        private ItemWheelUI wheel;
        private int index;

        public void Initialize(ItemWheelUI wheel, int index)
        {
            this.wheel = wheel;
            this.index = index;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            wheel.OnSegmentHovered(index);
        }
    }
}
