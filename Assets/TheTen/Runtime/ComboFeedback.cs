using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheTen
{
    // Persistent status badge, centered vertically in front of the combo slider.
    public sealed class ComboFeedback : MonoBehaviour
    {
        CanvasGroup group;
        RoundedBadgeGraphic face;
        TMP_Text multiplier;
        ScalePulse pulse;
        double factor = 1;
        bool active;
        readonly ComboParticleGraphic[] effects = new ComboParticleGraphic[5];
        public float Pulse => pulse != null ? pulse.Strength : 0;

        public static Color TierColor(double value)
        {
            if (value >= 5) return new Color32(238, 101, 86, 255);
            if (value >= 4) return new Color32(246, 209, 95, 255);
            if (value >= 3) return new Color32(112, 184, 255, 255);
            if (value >= 2) return new Color32(119, 198, 104, 255);
            return new Color32(245, 238, 205, 255);
        }
        public void Initialize()
        {
            var rect = (RectTransform)transform;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition += new Vector2(rect.rect.width / 2, -rect.rect.height / 2);
            group = gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            pulse = gameObject.AddComponent<ScalePulse>();
            var faceObject = new GameObject("Combo badge face", typeof(RectTransform), typeof(RoundedBadgeGraphic));
            var faceRect = (RectTransform)faceObject.transform; faceRect.SetParent(transform, false);
            faceRect.anchorMin = Vector2.zero; faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = faceRect.offsetMax = Vector2.zero;
            face = faceObject.GetComponent<RoundedBadgeGraphic>(); face.raycastTarget = false;
            var caption = new GameObject("Combo multiplier", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)caption.transform; textRect.SetParent(transform, false);
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(.5f, .5f);
            textRect.sizeDelta = new Vector2(rect.rect.width - 12, 42);
            multiplier = caption.GetComponent<TextMeshProUGUI>();
            UiTypography.Configure(multiplier, 34, new Color32(26, 51, 54, 255));
            for (int i = 0; i < effects.Length; i++)
            {
                var prefab = Resources.Load<ComboParticleGraphic>("Effects/Combo/ComboStage" + (i + 1));
                if (prefab == null) { Debug.LogError("Missing combo effect prefab stage " + (i + 1)); continue; }
                effects[i] = Instantiate(prefab, transform);
                var effectRect = (RectTransform)effects[i].transform;
                effectRect.anchorMin = effectRect.anchorMax = effectRect.pivot = new Vector2(.5f, .5f);
                effectRect.anchoredPosition = Vector2.zero;
                effectRect.SetAsFirstSibling();
            }
            SetState(1, false); Hide();
        }
        public void SetState(double value, bool hasCombo)
        {
            double next = hasCombo ? value : 1;
            if (next > factor || hasCombo && !active) pulse.Play(1.16f);
            else if (!hasCombo) pulse.ResetScale();
            factor = next; active = hasCombo;
            multiplier.text = next >= 5 ? "×5!!" : "×" + next.ToString("0.#");
            face.color = TierColor(next); group.alpha = 1;
        }
        public static int EffectStage(double multiplier)
        {
            return multiplier >= 5 ? 5 : multiplier >= 4 ? 4 : multiplier >= 2.5 ? 3 : multiplier >= 2 ? 2 : multiplier >= 1.5 ? 1 : 0;
        }
        public void PlayMatch(double multiplier)
        {
            int stage = EffectStage(multiplier);
            foreach (var effect in effects) if (effect != null) effect.StopBurst();
            if (stage > 0 && effects[stage - 1] != null) effects[stage - 1].PlayBurst();
        }
        public void Hide()
        {
            foreach (var effect in effects) if (effect != null) effect.StopBurst();
            if (group != null) group.alpha = 0;
            if (pulse != null) pulse.ResetScale();
        }
    }
}
