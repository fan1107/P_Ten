using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace TheTen
{
    public static class MintTheme
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static Sprite Sprite(string name)
        {
            if (sprites.TryGetValue(name, out var found) && found != null) return found;
            var atlas = Resources.Load<SpriteAtlas>("Atlas/MintCream");
            var sprite = atlas != null ? atlas.GetSprite(name) : null;
            if (sprite == null) sprite = Resources.Load<Sprite>("Atlas/" + name);
            sprites[name] = sprite; return sprite;
        }
        public static void Apply(Image image, string sprite, bool sliced = true)
        {
            image.sprite = Sprite(sprite); image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white; image.pixelsPerUnitMultiplier = .25f;
        }
    }
}
