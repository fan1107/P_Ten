using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheTen.Editor
{
    public static class ComboParticlePrefabBuilder
    {
        const string Root = "Assets/Resources/Effects/Combo/";
        [MenuItem("THE TEN/Apply punchier combo effect defaults")]
        public static void ApplyPunchierDefaults()
        {
            for (int level = 1; level <= 5; level++)
            {
                string path = Root + "ComboStage" + level + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try { ConfigurePunch(root, level); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }
        static void ConfigurePunch(GameObject root, int level)
        {
            var ui = root.GetComponent<ComboParticleGraphic>();
            var ps = ui.Particles; ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            int[] counts = { 18, 30, 46, 68, 92 };
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(90 + level * 38, 130 + level * 86);
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f + level * .025f, .55f + level * .065f);
            main.startSize = new ParticleSystem.MinMaxCurve(5 + level, 10 + level * 2.6f);
            main.maxParticles = 256;
            var emission = ps.emission;
            var bursts = new ParticleSystem.Burst[level];
            for (int i = 0; i < level; i++) bursts[i] = new ParticleSystem.Burst(i * .055f, (short)(i == 0 ? counts[level-1] : counts[level-1] / 3));
            emission.SetBursts(bursts);
            var shape = ps.shape; shape.radius = 6 + level * 2; shape.radiusThickness = 1;
            var velocity = ps.velocityOverLifetime; velocity.enabled = true;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,1),new Keyframe(.22f,.65f),new Keyframe(1,.15f)));
            velocity.y = new ParticleSystem.MinMaxCurve(-10 - level * 3);
            var size = ps.sizeOverLifetime;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,.45f),new Keyframe(.08f,1.2f),new Keyframe(.25f,.85f),new Keyframe(1,0)));
            ui.StretchSparkles = level >= 2;
        }
        [MenuItem("THE TEN/Create missing combo effect prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            int[] counts = { 12, 20, 30, 42, 56 };
            Color[] colors = { new Color32(255,224,151,255), new Color32(134,239,186,255), new Color32(123,204,255,255), new Color32(240,166,255,255), new Color32(255,147,111,255) };
            for (int level = 1; level <= 5; level++)
            {
                string path = Root + "ComboStage" + level + ".prefab";
                if (File.Exists(path)) continue; // Preserve designer edits.
                var root = new GameObject("ComboStage" + level, typeof(RectTransform), typeof(CanvasGroup), typeof(ComboParticleGraphic));
                var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(640, 240);
                var group = root.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
                var child = new GameObject("Particles", typeof(ParticleSystem)); child.transform.SetParent(root.transform, false);
                var ps = child.GetComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = false; main.playOnAwake = false; main.duration = 1.4f;
                main.simulationSpace = ParticleSystemSimulationSpace.Local; main.useUnscaledTime = true;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate; main.maxParticles = 256;
                main.startLifetime = new ParticleSystem.MinMaxCurve(.4f + level * .06f, .65f + level * .09f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(45 + level * 14, 85 + level * 26);
                main.startSize = new ParticleSystem.MinMaxCurve(4 + level, 9 + level * 1.6f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                main.startColor = new ParticleSystem.MinMaxGradient(colors[level-1], new Color(1, .97f, .8f));
                var emission = ps.emission; emission.rateOverTime = 0;
                var bursts = new ParticleSystem.Burst[level >= 4 ? 3 : level >= 2 ? 2 : 1];
                for (int b=0;b<bursts.Length;b++) bursts[b] = new ParticleSystem.Burst(b * .1f, (short)(b == 0 ? counts[level-1] : counts[level-1]/3));
                emission.SetBursts(bursts);
                var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 32; shape.radiusThickness = .4f;
                var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.y = new ParticleSystem.MinMaxCurve(-12 - level * 5);
                var fade = ps.colorOverLifetime; fade.enabled = true;
                var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) },
                    new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(1,.4f), new GradientAlphaKey(0,1) }); fade.color = gradient;
                var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,1,1,.15f));
                var spin = ps.rotationOverLifetime; spin.enabled = true; spin.z = new ParticleSystem.MinMaxCurve(-2,2);
                child.GetComponent<ParticleSystemRenderer>().enabled = false;
                var ui = root.GetComponent<ComboParticleGraphic>(); ui.Particles = ps; ui.ParticleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Atlas/CasualMatte/Round.png");
                ui.StretchSparkles = level >= 3; ui.raycastTarget = false;
                ConfigurePunch(root, level);
                PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
