using UnityEngine;
using UnityEngine.EventSystems;

namespace NaGaDeMon.Features.Inventory
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
