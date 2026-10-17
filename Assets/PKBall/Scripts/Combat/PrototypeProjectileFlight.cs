using UnityEngine;

namespace PKBall.Prototype
{
    // Ordinary projectile rules have no GameObject or visual-budget dependency.
    public struct PrototypeProjectileFlight
    {
        public Vector3 position;
        public float speed;
        public float startsAt;

        public bool Advance(float now, float deltaTime, Vector3 targetPosition, float targetRadius)
        {
            if (now < startsAt) return false;
            Vector3 desired = targetPosition - position;
            desired.y = 0;
            float travel = speed * deltaTime;
            if (desired.magnitude <= targetRadius + travel) return true;
            position += desired.normalized * travel;
            return false;
        }
    }
}
