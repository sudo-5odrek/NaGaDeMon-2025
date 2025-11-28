using UnityEngine;

namespace Features.Player.Scripts
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.1f;
        private Vector3 velocity;

        void FixedUpdate()
        {
            if (!target) return;
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }
    }
}
