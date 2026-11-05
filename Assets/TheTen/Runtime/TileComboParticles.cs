using UnityEngine;

namespace TheTen
{
    // Emitters are created on demand and outlive the tile visuals that triggered them.
    public sealed class TileComboParticles : MonoBehaviour
    {
        [Range(.2f, 1.5f)] public float EffectScale = .85f;
        readonly ComboParticleGraphic[,] pool = new ComboParticleGraphic[5, BoardModel.TileCount];
        public void Play(double multiplier, Vector2[] centers)
        {
            StopAll();
            int stage = Mathf.Max(1, ComboFeedback.EffectStage(multiplier));
            var prefab = Resources.Load<ComboParticleGraphic>("Effects/Combo/ComboStage" + stage);
            if (prefab == null) { Debug.LogError("Missing tile combo effect stage " + stage); return; }
            for (int i = 0; i < Mathf.Min(pool.GetLength(1), centers.Length); i++)
            {
                var effect = pool[stage - 1, i];
                if (effect == null)
                {
                    effect = Instantiate(prefab, transform);
                    effect.name = "Tile combo stage " + stage + " slot " + i;
                    pool[stage - 1, i] = effect;
                }
                var rect = (RectTransform)effect.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = centers[i]; rect.localScale = Vector3.one * EffectScale;
                effect.PlayBurst();
            }
        }
        public void StopAll()
        {
            foreach (var effect in pool) if (effect != null) effect.StopBurst();
        }
        void OnDisable() { StopAll(); }
    }
}
