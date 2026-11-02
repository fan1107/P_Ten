using UnityEngine;

namespace TheTen
{
    // Rapid updates restart one pulse instead of stacking coroutines.
    public sealed class ScalePulse : MonoBehaviour
    {
        public const float Duration = .36f;
        float age = Duration, peak = 1.14f;
        public bool IsPlaying => age < Duration;
        public float Strength => age < .09f ? Mathf.SmoothStep(0, 1, age / .09f)
            : 1 - Mathf.SmoothStep(0, 1, (age - .09f) / (Duration - .09f));
        public void Play(float scale = 1.14f) { peak = scale; age = 0; Draw(); }
        public void ResetScale() { age = Duration; transform.localScale = Vector3.one; }
        void Update()
        {
            if (!IsPlaying) return;
            age = Mathf.Min(Duration, age + Time.unscaledDeltaTime); Draw();
        }
        void Draw() { transform.localScale = Vector3.one * (1 + (peak - 1) * Strength); }
        void OnDisable() { ResetScale(); }
    }
}
