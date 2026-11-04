using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TheTen
{
    // Art-only adapter. Number values and board geometry remain owned by the model.
    public static class JewelTheme
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static bool loaded;
        public static readonly Color Ink = new Color32(55, 34, 67, 255);
        public static readonly Color Cream = new Color32(249, 231, 191, 255);
        public static Sprite Get(string name)
        {
            if (!loaded)
            {
                foreach (var sheet in new[] { "Tiles", "Icons", "PanelDark", "PanelCream", "ButtonPlum", "BarTrack", "Round", "Tile3" })
                    foreach (var sprite in Resources.LoadAll<Sprite>("Atlas/CasualMatte/" + sheet)) sprites[sprite.name] = sprite;
                loaded = true;
            }
            return sprites.TryGetValue(name, out var found) ? found : null;
        }
        public static Sprite Tile(int value) => Get("Tile" + (value == BoardModel.Double ? "Double" : value == -1 ? "Minus" : value.ToString()));
        public static Sprite Replacement(string name)
        {
            if (name == "MintGarden" || name == "HomeRuinsMonument") return Resources.Load<Sprite>("Atlas/CasualMatte/Background");
            if (name == "PanelDark" || name == "HomeIconFrame") return Get("PanelDark");
            if (name == "HomePlayFrame" || name == "RevivePrimaryFrame") return Get("ButtonPlum");
            if (name == "PanelCream") return Get("PanelCream");
            if (name == "ButtonMint") return Get("ButtonPlum");
            if (name == "BarTrack") return Get("BarTrack");
            return Get(name);
        }
        public static TMP_FontAsset Font => Resources.Load<TMP_FontAsset>("Atlas/CasualMatte/Fredoka SDF");
    }
}
