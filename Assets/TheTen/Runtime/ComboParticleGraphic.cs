using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace TheTen
{
    // Render native ParticleSystem simulation inside the overlay Canvas.
    // Particle modules remain editable on each prefab, with no camera or raycasts.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ComboParticleGraphic : MaskableGraphic
    {
        public ParticleSystem Particles;
        public Sprite ParticleSprite;
        [Tooltip("Alternate elongated sparkles with round particles.")]
        public bool StretchSparkles;
        ParticleSystem.Particle[] buffer;
        public override Texture mainTexture => ParticleSprite != null ? ParticleSprite.texture : Texture2D.whiteTexture;
        protected override void Awake()
        {
            base.Awake(); raycastTarget = false;
            buffer = new ParticleSystem.Particle[256];
        }
        [ContextMenu("Play combo burst")]
        public void PlayBurst()
        {
            if (Particles == null) return;
            Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Particles.Play(true); SetVerticesDirty();
        }
        public void StopBurst()
        {
            if (Particles != null) Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetVerticesDirty();
        }
        void LateUpdate() { if (Particles != null) SetVerticesDirty(); }
        protected override void OnDisable() { StopBurst(); base.OnDisable(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Particles == null) return;
            if (buffer == null) buffer = new ParticleSystem.Particle[256];
            int count = Particles.GetParticles(buffer);
            Vector4 uv = ParticleSprite != null ? DataUtility.GetOuterUV(ParticleSprite) : new Vector4(0, 0, 1, 1);
            for (int i = 0; i < count; i++)
            {
                var p = buffer[i];
                Vector3 center = transform.InverseTransformPoint(Particles.transform.TransformPoint(p.position));
                float half = p.GetCurrentSize(Particles) * .5f;
                float angle = p.rotation * Mathf.Deg2Rad;
                var right = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * half;
                var up = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle)) * half;
                if (StretchSparkles && i % 3 == 0) { right *= .35f; up *= 2.6f; }
                Color tint = p.GetCurrentColor(Particles) * color;
                int start = vh.currentVertCount;
                vh.AddVert(center - right - up, tint, new Vector2(uv.x, uv.y));
                vh.AddVert(center - right + up, tint, new Vector2(uv.x, uv.w));
                vh.AddVert(center + right + up, tint, new Vector2(uv.z, uv.w));
                vh.AddVert(center + right - up, tint, new Vector2(uv.z, uv.y));
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
