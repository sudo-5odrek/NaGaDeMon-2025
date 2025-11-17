using System.Collections.Generic;
using UnityEngine;

namespace Building.Turrets.Bullets
{
    [CreateAssetMenu(menuName = "TD/Bullet Effects")]
    public class BulletEffects : ScriptableObject
    {
        [Header("Bullet Settings")]
        public float speed = 10f;
        public float lifetime = 3f;

        [Header("Prefab")]
        public GameObject bulletPrefab;

        [Header("Stackable Effects")]
        public List<BulletEffect> effects = new List<BulletEffect>();
    }
}