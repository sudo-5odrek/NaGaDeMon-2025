using System.Collections.Generic;
using UnityEngine;

namespace Building.Conveyer
{
    public class ConveyorPathController : MonoBehaviour
    {
        public List<GameObject> pathTiles = new();
        public BuildingConnector startConnector;
        public BuildingConnector endConnector;
        public float speed = 1f;

        private List<MovingItem> activeItems = new();
        private bool isBlocked;

        void Update()
        {
            if (isBlocked) return;

            MoveItems();
            TrySpawnNewItem();
        }

        void MoveItems()
        {
            foreach (var item in activeItems)
            {
                item.progress += Time.deltaTime * speed;
                if (item.progress >= pathTiles.Count - 1)
                {
                    TryDeliverItem(item);
                }
            }
        }

        void TrySpawnNewItem()
        {
            //if (startConnector && endConnector && startConnector.HasOutputItem())
            {
                if (!IsEndFull())
                {
                    //var item = startConnector.TakeOutputItem();
                    //activeItems.Add(new MovingItem(item));
                }
                else isBlocked = true;
            }
        }

        void TryDeliverItem(MovingItem item)
        {
            //if (endConnector.TryInsertItem(item.item))
                activeItems.Remove(item);
            //else
                isBlocked = true;
        }

        bool IsEndFull() => endConnector != null && !endConnector.CanAcceptInput();
        
        public void SetPathData(List<GameObject> tilesPositions)
        {
            pathTiles = tilesPositions;
        }
        
        public void AddToPath(List<GameObject> newTiles)
        {
            foreach (var t in newTiles)
                pathTiles.Add(t); // or whatever your existing list uses
        }
    }
    
    

    public class MovingItem
    {
        public GameObject item;
        public float progress;
        public MovingItem(GameObject i) { item = i; progress = 0; }
    }
}