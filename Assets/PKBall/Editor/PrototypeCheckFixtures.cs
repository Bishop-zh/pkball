using System.Collections.Generic;
using System.Reflection;

namespace PKBall.Prototype.Editor
{
    // Only editor fixtures can bypass rule APIs when constructing invalid or legacy input.
    internal static class PrototypeCheckFixtures
    {
        private static T Field<T>(PrototypeRun run,string name) =>
            (T)typeof(PrototypeRun).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        public static List<PrototypeItemInstance> Bag(PrototypeRun run) => Field<List<PrototypeItemInstance>>(run,"bag");
        public static List<PrototypeItemInstance> PendingRewards(PrototypeRun run) => Field<List<PrototypeItemInstance>>(run,"pendingRewards");
        public static PrototypeItemDefinition[] Shop(PrototypeRun run) => Field<PrototypeItemDefinition[]>(run,"shop");
    }
}
