using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    /// <summary>Shared runtime sprite setup for the generated UGUI atlas slices.</summary>
    public static class PrototypeUiSkin
    {
        public static readonly Color Navy = new Color(.045f, .09f, .16f);
        public static readonly Color Cream = new Color(1f, .95f, .81f);
        public static readonly Color Cyan = new Color(.17f, .79f, .87f);
        public static readonly Color Gold = new Color(.92f, .64f, .28f);
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Sprite SpriteFor(string path, int border = 0)
        {
            string key = path + ":" + border;
            if (Sprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Sprite imported = Resources.Load<Sprite>(path);
            if (imported != null && border == 0) { Sprites[key] = imported; return imported; }
            if (path.StartsWith("Icons/"))
            {
                var catalog = Resources.Load<PrototypeItemArtCatalog>("BattleArt/ItemArtCatalog");
                imported = catalog == null ? null : catalog.Find(path.Substring(6));
                if (imported != null) { Sprites[key] = imported; return imported; }
            }
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            Vector4 edges = border <= 0 ? Vector4.zero : new Vector4(border, border, border, border);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, edges);
            Sprites[key] = sprite;
            return sprite;
        }

        public static void SetSliced(Image image, string path, int border)
        {
            Sprite sprite = SpriteFor(path, border);
            if (sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
        }
    }
}
