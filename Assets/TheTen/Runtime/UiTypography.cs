using System;
using TMPro;
using UnityEngine;

namespace TheTen
{
    // Shared SDF assets keep outlines sharp during UI scaling without per-label material copies.
    public static class UiTypography
    {
        static TMP_FontAsset font;
        static Material outline, tileOutline;
        public static TMP_FontAsset Font => font != null ? font : font = Load<TMP_FontAsset>("Fonts/NotoSans-Bold SDF");

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = Resources.Load<T>(path);
            if (asset == null) throw new InvalidOperationException("Missing UI typography asset: " + path);
            return asset;
        }

        public static void Configure(TMP_Text text, float size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool wrap = false)
        {
            text.font = Font;
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal; // The source face is already Bold.
            text.color = color;
            text.alignment = Alignment(alignment);
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;
            text.extraPadding = true;
            ApplyOutline(text);
        }

        public static void ApplyOutline(TMP_Text text)
        {
            if (outline == null) outline = Load<Material>("Fonts/NotoSans-Outline");
            if (tileOutline == null) tileOutline = Load<Material>("Fonts/NotoSans-TileOutline");
            var color = text.color;
            text.fontSharedMaterial = color.r * .2126f + color.g * .7152f + color.b * .0722f < .45f ? tileOutline : outline;
        }

        static TextAlignmentOptions Alignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}
