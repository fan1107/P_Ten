using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    // Existing UI names resolve exclusively to the current matte theme.
    public static class MintTheme
    {
        public static Sprite Sprite(string name) => JewelTheme.Replacement(name);
        public static void Apply(Image image, string sprite, bool sliced = true)
        {
            image.sprite = Sprite(sprite);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white; image.pixelsPerUnitMultiplier = 2f;
        }
    }
}
