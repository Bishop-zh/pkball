using System;
using UnityEngine;
namespace PKBall.Prototype
{
    [CreateAssetMenu(menuName = "PKBall/Art/Item Catalog")]
    public sealed class PrototypeItemArtCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string itemId, displayName;
            public Sprite icon;
            public GameObject modelOverride;
        }
        public Entry[] entries;
        public Sprite Find(string id)
        {
            if (entries == null) return null;
            foreach (var entry in entries) if (entry.itemId == id) return entry.icon;
            return null;
        }
    }
}
