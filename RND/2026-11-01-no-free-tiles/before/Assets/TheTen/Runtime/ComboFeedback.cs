using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    // A single reusable, non-interactive popup. Rapid matches replace the previous
    // award instead of stacking animations over the board or blocking tile input.
    public sealed class ComboFeedback : MonoBehaviour
    {
        const float Duration = 1.15f;
        RectTransform rect;
        CanvasGroup group;
        Text heading, points, multiplier;
        Image accent;
        float age = Duration, strength;
        public float Pulse => Mathf.Clamp01(1 - age / .32f);

        public static Color TierColor(double value)
        {
            if (value >= 5) return new Color32(166, 105, 231, 255);
            if (value >= 4) return new Color32(222, 89, 151, 255);
            if (value >= 3) return new Color32(235, 91, 89, 255);
            if (value >= 2.5) return new Color32(243, 121, 70, 255);
            if (value >= 2) return new Color32(245, 157, 65, 255);
            if (value >= 1.5) return new Color32(245, 183, 76, 255);
            return new Color32(243, 203, 111, 255);
        }

        public void Initialize(Font font)
        {
            rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(390, 104);
            rect.anchoredPosition = new Vector2(360, -826);
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            Box("Popup shadow", 6, -8, 390, 104, new Color32(26, 51, 54, 40));
            Box("Popup background", 0, 0, 390, 104, new Color32(26, 51, 54, 245));
            accent = Box("Popup accent", 0, 0, 6, 104, Color.white);
            heading = Caption("Award heading", font, 24, -10, 342, 23, 15);
            points = Caption("Award points", font, 22, -32, 220, 67, 53);
            multiplier = Caption("Award multiplier", font, 245, -32, 125, 67, 43);
            Hide();
        }

        public void Show(int award, double factor, bool increased)
        {
            age = 0; strength = Mathf.InverseLerp(1, 5, (float)factor);
            var color = TierColor(factor);
            points.text = "+" + award;
            multiplier.text = "×" + factor.ToString("0.#");
            heading.text = factor >= 5 ? "MAX COMBO!  /  POINTS" : increased ? "COMBO UP!  /  POINTS" : factor > 1 ? "COMBO!  /  POINTS" : "TEN!  /  POINTS";
            multiplier.color = accent.color = color;
            group.alpha = 1;
            Draw();
        }
        public void Hide()
        {
            age = Duration;
            if (group != null) group.alpha = 0;
            if (rect != null) rect.localScale = Vector3.one;
        }
        void Update()
        {
            if (age >= Duration) return;
            age = Mathf.Min(Duration, age + Time.unscaledDeltaTime);
            Draw();
        }
        void Draw()
        {
            float peak = 1.1f + .08f * strength;
            float scale = age < .13f ? Mathf.Lerp(.72f, peak, Mathf.SmoothStep(0, 1, age / .13f))
                : Mathf.Lerp(peak, 1, Mathf.SmoothStep(0, 1, (age - .13f) / .2f));
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2(360, -826 + 28 * Mathf.SmoothStep(0, 1, age / Duration));
            group.alpha = 1 - Mathf.SmoothStep(0, 1, (age - .72f) / (Duration - .72f));
        }
        RectTransform Child(string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(transform, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); return r;
        }
        Image Box(string name, float x, float y, float w, float h, Color color)
        {
            var image = Child(name, x, y, w, h).gameObject.AddComponent<Image>();
            if (name == "Popup background") { MintTheme.Apply(image, "PanelDark"); image.raycastTarget = false; return image; }
            image.color = color; image.raycastTarget = false; return image;
        }
        Text Caption(string name, Font font, float x, float y, float w, float h, int size)
        {
            var text = Child(name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Normal;
            text.color = new Color32(245, 242, 228, 255); text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false; return text;
        }
    }
}
