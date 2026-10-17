namespace PKBall.Prototype
{
    // Explicit state, independent of Unity's global random stream and render callbacks.
    public sealed class PrototypeCombatRandom
    {
        public uint State { get; private set; }
        public long Sequence { get; private set; }
        public PrototypeCombatRandom(uint seed = 1) { Reset(seed); }
        public void Reset(uint seed) { State=seed==0 ? 1u : seed; Sequence=0; }
        public float NextValue()
        {
            uint x=State;x^=x<<13;x^=x>>17;x^=x<<5;State=x;Sequence++;
            return (x>>8)*(1f/16777216f);
        }
        public static uint SeedFor(string id)
        {
            uint hash=2166136261;
            if(id!=null)foreach(char c in id)hash=unchecked((hash^c)*16777619);
            return hash==0 ? 1u : hash;
        }
    }
}
