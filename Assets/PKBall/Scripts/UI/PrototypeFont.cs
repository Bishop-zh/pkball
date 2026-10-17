using UnityEngine;
namespace PKBall.Prototype
{
    /// <summary>Browsers cannot use Windows' implicit Chinese font fallback.</summary>
    public static class PrototypeFont
    {
        static Font cached;
        public static Font Default
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (cached == null) cached = Resources.Load<Font>("Portfolio/Fonts/NotoSansSC-Regular");
                return cached;
#else
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif
            }
        }
    }
}
