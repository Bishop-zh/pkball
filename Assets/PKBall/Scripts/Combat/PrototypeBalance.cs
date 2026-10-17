using UnityEngine;

namespace PKBall.Prototype
{
    [CreateAssetMenu(fileName = "PrototypeBalance", menuName = "PKBall/Prototype Balance")]
    public sealed class PrototypeBalance : ScriptableObject
    {
        [Header("Arena and movement")]
        public float arenaHalfWidth = 5.8f;
        public float arenaHalfDepth = 3.8f;
        public float baseSpeed = 4f;
        public float minimumSpeed = 1.5f;
        public float maximumSpeed = 12f;
        public float massMinimum = 0.7f;
        public float massMaximum = 2.5f;
        public float speedRecovery = 0.35f;
        [Header("Wall return")]
        public float wallReturnStrength = 0.65f;
        public float wallReturnCooldown = 0.12f;

        [Header("Collision")]
        public float impactThreshold = 1.5f;
        public float impactBaseDamage = 4f;
        public float impactSpeedDamage = 1.8f;
        public float impactPairCooldown = 0.25f;
        public float impactMaxTargetHealthFraction = 0.18f;

        [Header("Energy and finish")]
        public float maximumEnergy = 100f;
        public float energyPerSecond = 0.8f;
        public float energyPerSpeed = 0.15f;
        public float impactEnergyBase = 2f;
        public float impactEnergyPerStrength = 0.5f;
        public float zeroEnergyDamagePerMetre = 0.5f;
        public float suddenDeathStart = 30f;
        public float suddenDeathDamagePerSecondAtEnd = 45f;
        public float maximumBattleTime = 60f;
        public float suddenDeathInitialDamagePerSecond = 3f;
        public float suddenDeathRecoveryAtEnd = .1f;

        public float OvertimeProgress(float elapsed) => Mathf.Clamp01((elapsed-suddenDeathStart)/Mathf.Max(1,maximumBattleTime-suddenDeathStart));
        public float OvertimeDamage(float elapsed) => elapsed<suddenDeathStart?0:Mathf.Lerp(suddenDeathInitialDamagePerSecond,suddenDeathDamagePerSecondAtEnd,OvertimeProgress(elapsed));
        public float RecoveryMultiplier(float elapsed) => Mathf.Lerp(1,suddenDeathRecoveryAtEnd,OvertimeProgress(elapsed));

        public float EffectiveMass(float maximumHealth)
        {
            return Mathf.Clamp(Mathf.Sqrt(Mathf.Max(1f, maximumHealth) / 100f), massMinimum, massMaximum);
        }

        public float TargetSpeed(float speedStat, float maximumHealth)
        {
            return Mathf.Clamp(baseSpeed * (speedStat / 100f) / EffectiveMass(maximumHealth), minimumSpeed, maximumSpeed);
        }
    }
}
