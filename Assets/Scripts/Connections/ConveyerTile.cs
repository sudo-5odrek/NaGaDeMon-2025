using UnityEngine;

namespace Building
{
    public class ConveyorTile : MonoBehaviour
    {
        [Header("Runtime Direction")]
        public Vector2Int direction;   // grid direction of item flow
        public Transform nextTile;  // the conveyor this tile feeds into
        public BuildingConnector targetBuilding; // if it ends on a building input

        public GameObject visual;

        public Vector3Int GridPos { get; private set; }

        public void SetAsFirst()
        {
            visual.SetActive(false);
        }

        private void Start()
        {
            (int gx, int gy) = Grid.GridManager.Instance.GridFromWorld(transform.position);
            GridPos = new Vector3Int(gx, gy, 0);
        }

        public void SetDirection(Vector2Int dir)
        {
            direction = dir;
            transform.rotation = Quaternion.Euler(0, 0, GetRotationFromDirection(dir));
        }

        private float GetRotationFromDirection(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return 0f;
            if (dir == Vector2Int.right) return -90f;
            if (dir == Vector2Int.down) return 180f;
            if (dir == Vector2Int.left) return 90f;
            return 0f;
        }
    }
}