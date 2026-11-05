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
        const float BlockedShadeOpacity = .72f;
        public static readonly Color MissColor = new Color32(255, 99, 91, 255);
        public bool IsClearing { get; private set; }
        public bool IsRevealing => revealAge < .4f;
        Image face, shadow, cover;
        TileSelectionBorder selectionBorder;
        TMP_Text number;
        CanvasGroup group;
        RectTransform faceRect, effectsParent, burst;
        RectTransform[] sparkRects;
        Image[] sparks;
        Vector2 faceOrigin, shadowOrigin, burstOrigin;
        bool available, selected;
        float lift, revealAge = 1, clearAge;
        bool pressed;
        float releaseAge = 1, releaseScale = 1, clearStartScale = 1;
        const float SelectedScale = 1.12f;
        const float PopDuration = .32f;
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

        public void Initialize(Image tileFace, Image tileShadow, TMP_Text tileNumber, RectTransform effects, bool initiallyAvailable, Tile tile)
        {
            face = tileFace; shadow = tileShadow; number = tileNumber; effectsParent = effects;
            var border = new GameObject("Selection border", typeof(RectTransform), typeof(TileSelectionBorder));
            border.transform.SetParent(face.transform, false);
            selectionBorder = border.GetComponent<TileSelectionBorder>();
            selectionBorder.raycastTarget = false;
            var borderRect = selectionBorder.rectTransform;
            borderRect.anchorMin = Vector2.zero; borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
            selectionBorder.enabled = false;
            face.gameObject.AddComponent<TilePressFeedback>().Owner = this;
            faceRect = face.rectTransform;
            faceRect.pivot = new Vector2(.5f, .5f);
            faceOrigin = new Vector2(BoardModel.TileWidth / 2, -BoardModel.TileHeight / 2);
            faceRect.anchoredPosition = faceOrigin;
            shadowOrigin = shadow.rectTransform.anchoredPosition;
            group = gameObject.AddComponent<CanvasGroup>();
            var ghost = new GameObject("Reveal shading", typeof(RectTransform), typeof(Image));
            ghost.transform.SetParent(face.transform, false); ghost.transform.SetAsFirstSibling();
            cover = ghost.GetComponent<Image>(); cover.raycastTarget = false;
            cover.rectTransform.anchorMin = Vector2.zero; cover.rectTransform.anchorMax = Vector2.one;
            cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
            cover.color = Color.clear;
            available = initiallyAvailable;
            SetValue(tile);
            SetState(initiallyAvailable, false);
        }

        public void SetValue(Tile tile)
        {
            name = "Tile " + tile.Id + " • " + tile.Label;
            number.text = tile.Label;
            number.fontSize = tile.IsDouble || tile.Value == -1 ? 44 : 54;
            jewelFace = JewelTheme.Tile(tile.Value);
            face.sprite = cover.sprite = jewelFace;
            shadow.sprite = jewelFace != null ? jewelFace : JewelTheme.Get("Round");
            baseColor = jewelFace != null ? Color.white : TileColor(tile.Value);
            face.color = baseColor;
        }

        public void SetState(bool canSelect, bool isSelected)
        {
            if (IsClearing) return;
            if (canSelect && !available) revealAge = 0;
            if (isSelected && !selected && releaseAge >= PopDuration) StartReleasePop();
            available = canSelect; selected = isSelected;
            if (!available) { pressed = false; releaseAge = 1; }
            selectionBorder.enabled = available && selected;
            // A fast player can select a newly exposed tile immediately. The reveal
            // shortens rather than swallowing that tap or blocking the whole board.
            if (selected && revealAge < .14f) revealAge = .14f;
            face.sprite = jewelFace;
            if (!IsRevealing)
            {
                number.color = available ? ink : blockedInk;
                cover.color = new Color(.12f, .08f, .18f, available ? 0 : BlockedShadeOpacity);
            }
        }

        public void ClearWithSparkles()
        {
            if (IsClearing) return;
            missStartedAt = float.NegativeInfinity; face.color = baseColor;
            clearStartScale = faceRect.localScale.x; pressed = false;
            IsClearing = true; clearAge = 0;
            selectionBorder.enabled = false;
            group.blocksRaycasts = false; group.interactable = false;
            face.raycastTarget = false; face.GetComponent<Button>().interactable = false;
            face.sprite = jewelFace;
            cover.color = Color.clear;
            BuildBurst();
        }

        public void ShowMiss()
        {
            if (IsClearing) return;
            pressed = false; releaseAge = 1;
            missStartedAt = Time.unscaledTime;
            face.color = MissColor;
        }

        public bool BeginPress()
        {
            if (!available || IsClearing || !face.GetComponent<Button>().IsInteractable()) return false;
            pressed = true; releaseAge = 1;
            return true;
        }
        public void EndPress(bool cancelled)
        {
            if (!pressed) return;
            pressed = false;
            if (!cancelled && available && !IsClearing) StartReleasePop();
        }
        void StartReleasePop()
        {
            releaseScale = faceRect.localScale.x; releaseAge = 0;
        }
        void OnDisable()
        {
            pressed = false; releaseAge = 1;
            if (faceRect != null) faceRect.localScale = Vector3.one * (selected ? SelectedScale : 1);
        }

        void Update()
        {
            float delta = Time.unscaledDeltaTime;
            if (IsClearing)
            {
                clearAge += delta; float t = Mathf.Clamp01(clearAge / ClearDuration);
                float size = t < .22f ? Mathf.Lerp(clearStartScale, 1.24f, t / .22f) : Mathf.Lerp(1.24f, .72f, (t - .22f) / .78f);
                faceRect.localScale = Vector3.one * size;
                faceRect.anchoredPosition = faceOrigin + new Vector2(0, lift + 20 * t);
                group.alpha = 1 - Mathf.SmoothStep(0, 1, (t - .16f) / .7f);
                DrawBurst(t);
                if (t >= 1) { Destroy(burst.gameObject); Destroy(gameObject); }
                return;
            }
            float targetScale = selected ? SelectedScale : 1;
            float sizeNow;
            if (pressed) sizeNow = Mathf.MoveTowards(faceRect.localScale.x, .9f, delta * 4);
            else if (releaseAge < PopDuration)
            {
                releaseAge = Mathf.Min(PopDuration, releaseAge + delta);
                float t = releaseAge / PopDuration;
                sizeNow = t < .28f ? Mathf.Lerp(releaseScale, 1.28f, Mathf.SmoothStep(0, 1, t / .28f))
                    : t < .65f ? Mathf.Lerp(1.28f, targetScale - .04f, Mathf.SmoothStep(0, 1, (t - .28f) / .37f))
                    : Mathf.Lerp(targetScale - .04f, targetScale, Mathf.SmoothStep(0, 1, (t - .65f) / .35f));
            }
            else sizeNow = Mathf.MoveTowards(faceRect.localScale.x, targetScale, delta * 3);
            faceRect.localScale = Vector3.one * sizeNow;
            lift = Mathf.MoveTowards(lift, pressed ? -3 : selected ? 16 : 0, delta * 120);
            revealAge = Mathf.Min(1, revealAge + delta);
            float reveal = Mathf.SmoothStep(0, 1, (revealAge - .12f) / .28f);
            cover.color = new Color(.12f, .08f, .18f, available ? (1 - reveal) * BlockedShadeOpacity : BlockedShadeOpacity);
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
                var image = spark.GetComponent<Image>(); image.raycastTarget = false; image.sprite = JewelTheme.Get("Round");
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
                sparkRects[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + 18 * t);
                sparkRects[i].localScale = Vector3.one * (t < .6f ? 1 : .6f);
                var color = sparks[i].color; color.a = 1 - Mathf.SmoothStep(0, 1, (t - .35f) / .65f); sparks[i].color = color;
            }
        }
        void OnDestroy() { if (burst != null) Destroy(burst.gameObject); }
    }
}
