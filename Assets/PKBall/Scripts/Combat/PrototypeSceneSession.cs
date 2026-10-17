using UnityEngine;

namespace PKBall.Prototype
{
    public enum PrototypeSceneView { MainMenu, Preparation, Battle }

    // Only run data crosses scenes; scene objects are owned by their scene.
    public static class PrototypeSceneSession
    {
        public static PrototypeRun Run;
        public static string SceneName(PrototypeSceneView view)
        {
            switch (view)
            {
                case PrototypeSceneView.MainMenu: return "MainMenu";
                case PrototypeSceneView.Preparation: return "Preparation";
                default: return "PrototypeBattle";
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Run = null; }
    }
}
