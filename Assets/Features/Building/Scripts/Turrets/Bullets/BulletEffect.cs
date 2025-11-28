using UnityEngine;

namespace Features.Building.Scripts.Turrets.Bullets
{
    public abstract class BulletEffect : ScriptableObject
    {
        public abstract void ApplyEffect(GameObject enemyGO, Vector3 hitPoint);
    }
}

