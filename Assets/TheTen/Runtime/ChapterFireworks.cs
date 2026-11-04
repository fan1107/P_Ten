using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    // Screen-space matte confetti; purely visual and never intercept input.
    public sealed class ChapterFireworks : MonoBehaviour
    {
        const float Lifetime = 1.6f;
        readonly Vector2[] origins = new Vector2[160];
        readonly Vector2[] velocities = new Vector2[160];
        readonly float[] delays = new float[160];
        readonly Image[] pixels = new Image[160];
        float age;

        public static void PlaySpecial(RectTransform parent, Vector2 center, int bonus)
        {
            // Fast special matches replace the previous burst instead of accumulating particles.
            foreach (var previous in parent.GetComponentsInChildren<ChapterFireworks>())
                if (previous.name == "Special fireworks") { previous.gameObject.SetActive(false); Destroy(previous.gameObject); }
            Create(parent, "Special fireworks").Build(false, center, bonus);
        }
        public static void Play(RectTransform parent, bool popup)
        {
            Create(parent, "Chapter fireworks").Build(popup);
        }
        static ChapterFireworks Create(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(ChapterFireworks));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(720, 1280);
            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            return go.GetComponent<ChapterFireworks>();
        }

        void Build(bool popup, Vector2? specialCenter = null, int bonus = 1)
        {
            var centers = popup
                ? new[] { new Vector2(120, -435), new Vector2(600, -435), new Vector2(110, -820), new Vector2(610, -820), new Vector2(360, -350) }
                : new[] { new Vector2(360, -740), new Vector2(180, -620), new Vector2(540, -620), new Vector2(220, -900), new Vector2(500, -900) };
            var colors = new Color[] { new Color32(255, 222, 115, 255), new Color32(188, 255, 207, 255), new Color32(255, 145, 177, 255), new Color32(187, 158, 255, 255), Color.white };
            if (specialCenter.HasValue)
            {
                var center = specialCenter.Value;
                center.x = Mathf.Clamp(center.x, 180, 540); center.y = Mathf.Clamp(center.y, -900, -560);
                centers = new[] { center, center + new Vector2(-65, 40), center + new Vector2(65, 40), center + new Vector2(-50, -55), center + new Vector2(50, -55) };
                colors = bonus == 4
                    ? new Color[] { new Color32(255, 100, 100, 255), new Color32(255, 190, 85, 255), Color.white, new Color32(255, 140, 205, 255), new Color32(255, 230, 140, 255) }
                    : new Color[] { new Color32(255, 225, 90, 255), new Color32(110, 195, 255, 255), Color.white, new Color32(195, 240, 255, 255), new Color32(255, 240, 160, 255) };
            }
            var random = new System.Random(71);
            for (int i = 0; i < pixels.Length; i++)
            {
                int burst = i / 32;
                float angle = (i % 32) * Mathf.PI * 2 / 32;
                float speed = 100 + (float)random.NextDouble() * 190;
                if (specialCenter.HasValue) speed *= bonus == 4 ? 1.05f : .7f;
                origins[i] = centers[burst];
                velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                delays[i] = burst * .11f;
                var go = new GameObject("Firework pixel", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform; rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = Vector2.one * (i % 4 == 0 ? 12 : 6);
                rect.anchoredPosition = origins[i];
                pixels[i] = go.GetComponent<Image>(); pixels[i].raycastTarget = false;
                pixels[i].sprite = JewelTheme.Get("Round");
                pixels[i].color = colors[(i + burst) % colors.Length];
                pixels[i].enabled = false;
            }
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            for (int i = 0; i < pixels.Length; i++)
            {
                float t = age - delays[i];
                pixels[i].enabled = t >= 0 && t < Lifetime;
                if (!pixels[i].enabled) continue;
                var position = origins[i] + velocities[i] * (1 - Mathf.Exp(-2 * t)) + Vector2.down * (65 * t * t);
                pixels[i].rectTransform.anchoredPosition = position;
                var color = pixels[i].color;
                color.a = 1 - Mathf.SmoothStep(0, 1, (t - .5f) / (Lifetime - .5f));
                pixels[i].color = color;
            }
            if (age >= Lifetime + .44f) Destroy(gameObject);
        }
    }
}
