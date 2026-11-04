using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheTen
{
    // Visual time never changes board coordinates, score, or the gameplay clock.
    public sealed class TileVisual : MonoBehaviour
    {
        public const float ClearDuration = .48f;
        public const float MissDuration = .44f;
        public static readonly Color MissColor = new Color32(255, 99, 91, 255);
        public bool IsClearing { get; private set; }
        public bool IsRevealing => revealAge < .4f;
        Image face, shadow, cover;
        TMP_Text number;
        CanvasGroup group;
        RectTransform faceRect, effectsParent, burst;
        RectTransform[] sparkRects;
        Image[] sparks;
        Vector2 faceOrigin, shadowOrigin, burstOrigin;
        bool available, selected;
        float lift, revealAge = 1, clearAge;
        float missStartedAt = float.NegativeInfinity;
        Color baseColor;
        Sprite jewelFace;
        readonly Color ink = JewelTheme.Ink;
        readonly Color blockedInk = new Color32(67, 47, 43, 255);
        public static Color TileColor(int value)
        {
            if (value == 0) return new Color32(255, 220, 91, 255);
            if (value == -1) return new Color32(112, 184, 255, 255);
            if (value == BoardModel.Double) return new Color32(255, 112, 112, 255);
            return Color.white;
        }

        public void Initialize(Image tileFace, Image tileShadow, TMP_Text tileNumber, RectTransform effects, bool initiallyAvailable, int value = 1)
        {
            face = tileFace; shadow = tileShadow; number = tileNumber; effectsParent = effects;
            jewelFace = JewelTheme.Tile(value);
            baseColor = jewelFace != null ? Color.white : face.color;
            face.color = baseColor;
            if (jewelFace != null) { face.sprite = jewelFace; shadow.sprite = jewelFace; }
            faceRect = face.rectTransform;
            faceRect.pivot = new Vector2(.5f, .5f);
            faceOrigin = new Vector2(BoardModel.TileWidth / 2, -BoardModel.TileHeight / 2);
            faceRect.anchoredPosition = faceOrigin;
            shadowOrigin = shadow.rectTransform.anchoredPosition;
            group = gameObject.AddComponent<CanvasGroup>();
            var ghost = new GameObject("Reveal shading", typeof(RectTransform), typeof(Image));
            ghost.transform.SetParent(face.transform, false); ghost.transform.SetAsFirstSibling();
            cover = ghost.GetComponent<Image>(); cover.sprite = jewelFace != null ? jewelFace : MintTheme.Sprite("TileBlocked"); cover.raycastTarget = false;
            cover.rectTransform.anchorMin = Vector2.zero; cover.rectTransform.anchorMax = Vector2.one;
            cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
            cover.color = Color.clear;
            available = initiallyAvailable;
            SetState(initiallyAvailable, false);
        }

        public void SetState(bool canSelect, bool isSelected)
        {
            if (IsClearing) return;
            if (canSelect && !available) revealAge = 0;
            available = canSelect; selected = isSelected;
            // A fast player can select a newly exposed tile immediately. The reveal
            // shortens rather than swallowing that tap or blocking the whole board.
            if (selected && revealAge < .14f) revealAge = .14f;
            face.sprite = jewelFace != null ? jewelFace : MintTheme.Sprite(selected ? "TileSelected" : available ? "TileIdle" : "TileBlocked");
            if (!IsRevealing) number.color = available ? ink : blockedInk;
        }

        public void ClearWithSparkles()
        {
            if (IsClearing) return;
            missStartedAt = float.NegativeInfinity; face.color = baseColor;
            IsClearing = true; clearAge = 0;
            group.blocksRaycasts = false; group.interactable = false;
            face.raycastTarget = false; face.GetComponent<Button>().interactable = false;
            face.sprite = jewelFace != null ? jewelFace : MintTheme.Sprite("TileSelected");
            cover.color = Color.clear;
            BuildBurst();
        }

        public void ShowMiss()
        {
            if (IsClearing) return;
            missStartedAt = Time.unscaledTime;
            face.color = MissColor;
        }

        void Update()
        {
            float delta = Time.unscaledDeltaTime;
            if (IsClearing)
            {
                clearAge += delta; float t = Mathf.Clamp01(clearAge / ClearDuration);
                float size = t < .22f ? Mathf.Lerp(1, 1.08f, t / .22f) : Mathf.Lerp(1.08f, .72f, (t - .22f) / .78f);
                faceRect.localScale = Vector3.one * size;
                faceRect.anchoredPosition = faceOrigin + new Vector2(0, lift + 20 * t);
                group.alpha = 1 - Mathf.SmoothStep(0, 1, (t - .16f) / .7f);
                DrawBurst(t);
                if (t >= 1) { Destroy(burst.gameObject); Destroy(gameObject); }
                return;
            }
            lift = Mathf.MoveTowards(lift, selected ? 10 : 0, delta * 85);
            revealAge = Mathf.Min(1, revealAge + delta);
            float reveal = Mathf.SmoothStep(0, 1, (revealAge - .12f) / .28f);
            cover.color = jewelFace != null ? new Color(.25f, .15f, .22f, available ? (1 - reveal) * .45f : .45f) : new Color(1, 1, 1, available ? 1 - reveal : 0);
            number.color = available ? Color.Lerp(blockedInk, ink, reveal) : blockedInk;
            float miss = Mathf.Clamp01((Time.unscaledTime - missStartedAt) / MissDuration);
            float tint = 1 - Mathf.SmoothStep(0, 1, (miss - .18f) / .82f);
            float shake = miss < 1 ? Mathf.Sin(miss * Mathf.PI * 6) * 5 * (1 - miss) : 0;
            var stateColor = jewelFace != null && selected ? new Color(1.12f, 1.08f, .86f) : baseColor;
            face.color = Color.Lerp(stateColor, MissColor, tint);
            faceRect.anchoredPosition = faceOrigin + new Vector2(Mathf.Round(shake), Mathf.Round(lift) - (available ? 3 * (1 - reveal) : 0));
            shadow.rectTransform.anchoredPosition = shadowOrigin + new Vector2(lift * .2f, -lift * .35f);
            shadow.color = new Color(.08f, .025f, .06f, Mathf.Lerp(.6f, .35f, lift / 10));
        }

        void BuildBurst()
        {
            var go = new GameObject("Tile pixel burst", typeof(RectTransform));
            burst = (RectTransform)go.transform; burst.SetParent(effectsParent, false);
            burst.anchorMin = burst.anchorMax = burst.pivot = new Vector2(0, 1);
            burstOrigin = ((RectTransform)transform).anchoredPosition + faceOrigin + new Vector2(0, 10);
            burst.anchoredPosition = burstOrigin;
            sparkRects = new RectTransform[8]; sparks = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                var spark = new GameObject("Pixel sparkle " + i, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)spark.transform; rect.SetParent(burst, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = Vector2.one * (i % 2 == 0 ? 9 : 5);
                var image = spark.GetComponent<Image>(); image.raycastTarget = false;
                image.color = i % 2 == 0 ? new Color32(255, 237, 155, 255) : new Color32(218, 252, 192, 255);
                sparkRects[i] = rect; sparks[i] = image;
            }
            DrawBurst(0);
        }
        void DrawBurst(float t)
        {
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = i * Mathf.PI / 4;
                float radius = Mathf.Lerp(23, 68, 1 - (1 - t) * (1 - t));
                sparkRects[i].anchoredPosition = new Vector2(Mathf.Round(Mathf.Cos(angle) * radius / 2) * 2,
                    Mathf.Round((Mathf.Sin(angle) * radius + 18 * t) / 2) * 2);
                sparkRects[i].localScale = Vector3.one * (t < .6f ? 1 : .6f);
                var color = sparks[i].color; color.a = 1 - Mathf.SmoothStep(0, 1, (t - .35f) / .65f); sparks[i].color = color;
            }
        }
        void OnDestroy() { if (burst != null) Destroy(burst.gameObject); }
    }
}
