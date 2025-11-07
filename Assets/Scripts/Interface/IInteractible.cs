using Player;

namespace Interface
{
    public interface IInteractable
    {
        void OnHoverEnter();
        void OnHoverExit();
        void OnInteractHoldLeft(PlayerInventory playerInventory);
        void OnInteractHoldRight(PlayerInventory playerInventory);
    }
}
