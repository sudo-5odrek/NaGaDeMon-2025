using UnityEngine;

namespace NaGaDeMon.Features.Building
{
    public static class TargetPrediction
    {
        /// <summary>
        /// Predicts future position for a moving target given projectile speed.
        /// </summary>
        public static Vector2 PredictAimPosition(Vector2 shooterPos, Vector2 targetPos, Vector2 targetVelocity, float projectileSpeed)
        {
            // Relative position and velocity
            Vector2 toTarget = targetPos - shooterPos;

            float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector2.Dot(toTarget, targetVelocity);
            float c = Vector2.Dot(toTarget, toTarget);

            // Quadratic discriminant
            float discriminant = b * b - 4f * a * c;

            // No valid solution (target too fast or projectile too slow)
            if (discriminant < 0f || Mathf.Abs(a) < 0.001f)
                return targetPos;

            // Two possible intercept times; we take the smallest positive one
            float sqrtD = Mathf.Sqrt(discriminant);
            float t1 = (-b + sqrtD) / (2f * a);
            float t2 = (-b - sqrtD) / (2f * a);
            float t = Mathf.Min(t1, t2);
            if (t < 0f) t = Mathf.Max(t1, t2);
            if (t < 0f) return targetPos; // fallback if both negative

            // Predict future position
            return targetPos + targetVelocity * t;
        }
    }

}
