using UnityEngine;

namespace PKBall.Prototype
{
    public enum PrototypeProjectileVisual { Orb, Arrow, Sword, Bolt, Cannonball }
    public enum PrototypeHitVisual { None, Sparks, Poison, Burn, Frost, Shockwave }
    public enum PrototypeAttackVisual { Single, Volley, Beam, Shockwave }
    public enum PrototypeWeaponKind { Melee, Ranged }
    public enum PrototypeAttackShape { Thrust, Sweep, Smash }

    [CreateAssetMenu(fileName = "PrototypeWeapon", menuName = "PKBall/Prototype Weapon")]
    public sealed class PrototypeWeaponDefinition : ScriptableObject
    {
        public string displayName = "Weapon";
        public PrototypeWeaponKind kind;
        [Min(0.1f)] public float cooldown = 1f;
        [Min(0f)] public float damage = 10f;
        [Min(0.1f)] public float range = 2f;
        [Min(0f)] public float projectileSpeed = 8f;
        [Range(0f, 180f)] public float meleeHalfAngle = 70f;
        public PrototypeAttackShape attackShape = PrototypeAttackShape.Sweep;
        [Min(0f)] public float knockbackImpulse = 0.5f;
        public GameObject modelPrefab;
        public GameObject legendaryModelPrefab;
        public Vector3 modelScale = Vector3.one;
        [Min(1f)] public float impactMultiplier = 1f;
        public bool modifiesImpact;
        [Min(0)] public float growthPerActivation;
        [Min(0)] public float hitDelay = .12f;
        [Min(.05f)] public float animationDuration = .3f;
        public Color prototypeColor = Color.white;
        public float visualScale = 1;
        public float visualSize = .8f;
        public float effectScale = 1;
        public PrototypeAttackVisual visualStyle;
        public int visualCount = 1;
        public PrototypeProjectileVisual projectileVisual;
        public string projectileIconId;
        public float projectileSize = .18f;
        public PrototypeHitVisual hitVisual;
    }
}
