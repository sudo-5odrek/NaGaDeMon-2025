using NaGaDeMon.Features.Enemies;
using UnityEngine;

namespace NaGaDeMon.Features.Building.Turrets.Bullets
{
    public abstract class BulletEffect : ScriptableObject
    {
        public abstract void ApplyEffect(GameObject enemyGO, Vector3 hitPoint);
    }
}

