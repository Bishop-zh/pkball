using UnityEngine;

namespace PKBall.Prototype
{
    // One logical blade per summon, regardless of available renderers.
    public struct PrototypeSwordFlight
    {
        public const float PierceInterval = 2.4f;
        public Vector3 position, heading;
        public float speed, hitDelay;
        public int index;
        private bool attacking;
        private float startsAt, nextPierce, passUntil;

        public void Rally(float now)
        {
            attacking = true;
            passUntil = 0;
            startsAt = now + hitDelay;
            nextPierce = startsAt + PierceInterval;
        }

        public bool Advance(float now, float deltaTime, Vector3 ownerPosition, float ownerRadius,
            Vector3 targetPosition, float targetRadius)
        {
            if (now < startsAt) return false;
            if (passUntil > now) { position += heading * speed * deltaTime; return false; }
            if (!attacking && now >= nextPierce) attacking = true;
            if (!attacking)
            {
                float phase = now * 1.8f + index * 2.39996f;
                var orbit = ownerPosition + new Vector3(Mathf.Cos(phase)*(ownerRadius+1.2f),1.15f,Mathf.Sin(phase)*(ownerRadius+1.2f));
                position = Vector3.Distance(position,orbit)>4 ? orbit : Vector3.MoveTowards(position,orbit,speed*deltaTime);
                heading = new Vector3(-Mathf.Sin(phase),0,Mathf.Cos(phase));
                return false;
            }
            var desired = targetPosition - position;
            desired.y = 0;
            heading = desired.sqrMagnitude > .001f ? desired.normalized : Vector3.forward;
            float travel = speed * deltaTime;
            if (desired.magnitude <= targetRadius + travel)
            {
                attacking = false;
                passUntil = now + .16f;
                nextPierce = now + PierceInterval;
                return true;
            }
            position += heading * travel;
            return false;
        }
    }
}
