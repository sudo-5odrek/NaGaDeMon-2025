using Enemy;
using UnityEngine;

namespace Building.Turrets.Bullets
{
    public abstract class BulletEffect : ScriptableObject
    {
        public abstract void ApplyEffect(GameObject enemyGO, Vector3 hitPoint);
    }
}

