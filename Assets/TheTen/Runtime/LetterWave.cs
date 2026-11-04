using UnityEngine;
using TMPro;

namespace TheTen
{
    public sealed class LetterWave : MonoBehaviour
    {
        public const string Message = "3 TAPS. MAKE 10.";
        RectTransform[] letters;
        float age;
        public void Initialize(TMP_FontAsset font, int size = 28)
        {
            var widths = new float[Message.Length]; float total = 0;
            for (int i = 0; i < Message.Length; i++)
            {
                widths[i] = font.characterLookupTable.TryGetValue(Message[i], out var character)
                    ? character.glyph.metrics.horizontalAdvance * size / font.faceInfo.pointSize * font.faceInfo.scale : size * .3f;
                total += widths[i];
            }
            letters = new RectTransform[Message.Length]; float x = -total / 2;
            for (int i = 0; i < letters.Length; i++)
            {
                var go = new GameObject("Intro character " + i, typeof(RectTransform), typeof(TextMeshProUGUI));
                var rect = (RectTransform)go.transform; rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(widths[i] + 8, size * 1.75f);
                rect.anchoredPosition = new Vector2(x + widths[i] / 2, 0);
                var text = go.GetComponent<TextMeshProUGUI>(); text.text = Message[i].ToString();
                UiTypography.Configure(text, size, i == 0 ? new Color32(246, 209, 95, 255)
                    : i == 13 || i == 14 ? new Color32(241, 116, 104, 255) : new Color32(245, 242, 228, 255));
                letters[i] = rect; x += widths[i];
            }
        }
        void Update()
        {
            if (letters == null) return;
            age += Time.unscaledDeltaTime;
            for (int i = 0; i < letters.Length; i++)
            {
                float phase = Mathf.Repeat(age - i * .075f, 3.6f);
                float wave = phase < .6f ? Mathf.Sin(phase / .6f * Mathf.PI) : 0;
                letters[i].localScale = Vector3.one * (1 + .22f * wave);
            }
        }
        void OnDisable()
        {
            if (letters != null) foreach (var letter in letters) if (letter != null) letter.localScale = Vector3.one;
        }
    }
}
