using UnityEngine;

namespace TheTen
{
    public sealed class HomeTileMotion : MonoBehaviour
    {
        public float Phase;
        RectTransform rect;
        Vector2 origin;
        void Start() { rect = (RectTransform)transform; origin = rect.anchoredPosition; }
        void Update() { rect.anchoredPosition = origin + new Vector2(0, Mathf.Round(Mathf.Sin(Time.unscaledTime * 1.8f + Phase) * 2)); }
    }
}
