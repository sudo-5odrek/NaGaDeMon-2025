using System.Collections.Generic;
using UnityEngine;

namespace NaGaDeMon.Features.Building.Turrets.Bullets
{
    [CreateAssetMenu(menuName = "Game/Combat/Bullet Effects/Configuration")]
    public class BulletEffects : ScriptableObject
    {
        [Header("Bullet Settings")]
        public float speed = 10f;
        public float lifetime = 3f;
        public float fireInterval = 0.5f; 
        

        [Header("Prefab")]
        public GameObject bulletPrefab;

        [Header("Stackable Effects")]
        public List<BulletEffect> effects = new List<BulletEffect>();
    }
}