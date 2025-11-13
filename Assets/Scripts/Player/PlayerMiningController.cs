using System.Collections;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Collider2D))]
    public class PlayerMiningController : MonoBehaviour
    {
        private MiningNode currentNode;
        private Coroutine miningRoutine;
        private bool isMining;
        public PlayerInventory inventory;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out MiningNode node))
            {
                currentNode = node;
                StartCoroutine(StartMiningAfterDelay(node.activationDelay));
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<MiningNode>() == currentNode)
            {
                StopMining();
                currentNode = null;
            }
        }

        private IEnumerator StartMiningAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (currentNode != null)
                StartMining();
        }

        private void StartMining()
        {
            if (isMining || currentNode == null)
                return;

            isMining = true;
            miningRoutine = StartCoroutine(MiningLoop());
        }

        private void StopMining()
        {
            if (miningRoutine != null)
                StopCoroutine(miningRoutine);
            isMining = false;
        }

        private IEnumerator MiningLoop()
        {
            while (isMining && currentNode != null)
            {
                if (currentNode.TryMine(out ItemDefinition item, out int amount))
                {
                    if (inventory != null)
                    {
                        inventory.AddItem(item, amount);
                        Debug.Log($"⛏️ Mined {amount}x {item.displayName}");
                    }
                    else
                        Debug.LogWarning("⚠️ No inventory on player to store mined items!");
                }
                else
                {
                    Debug.Log("💀 Node depleted");
                    StopMining();
                    yield break;
                }

                yield return new WaitForSeconds(currentNode.miningInterval);
            }
        }
    }
}
