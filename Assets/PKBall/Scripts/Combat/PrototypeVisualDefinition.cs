using UnityEngine;

namespace PKBall.Prototype
{
    [CreateAssetMenu(fileName = "PrototypeVisual", menuName = "PKBall/Prototype Character Visual")]
    public sealed class PrototypeVisualDefinition : ScriptableObject
    {
        public string visualId;
        public Texture2D portrait;
        public Color tint = Color.white;
        public float spriteScale = .42f;
    }
}
