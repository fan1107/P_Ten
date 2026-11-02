using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TheTen.Editor
{
    public static class UiTypographyBuilder
    {
        const string Root = "Assets/Resources/Fonts/";

        [MenuItem("THE TEN/Build Noto Sans Typography")]
        public static void Build()
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
                throw new InvalidOperationException("Import TMP Essential Resources before building typography.");

            var latin = string.Concat(Enumerable.Range(32, 95).Select(c => (char)c)) + "×·−→Ⅱ—…•";
            var primary = CreateFont("NotoSans-Bold.ttf", "NotoSans-Bold SDF", latin, false);
            // Prewarm the ranking UI's Korean text; dynamic fallback also supports future local names.
            var source = string.Concat(Directory.GetFiles("Assets/TheTen/Runtime", "*.cs").Select(File.ReadAllText));
            var korean = new string(source.Where(c => c >= 0x3000 && !char.IsSurrogate(c)).Distinct().ToArray()) + "−→Ⅱ";
            var fallback = CreateFont("NotoSansCJKkr-Regular.otf", "NotoSans-Korean SDF", korean, true);
            // The bundled CJK source is Regular; add SDF weight so the outline does not swallow thin strokes.
            fallback.normalStyle = .75f;
            fallback.material.SetFloat("_WeightNormal", fallback.normalStyle);
            EditorUtility.SetDirty(fallback);
            EditorUtility.SetDirty(fallback.material);
            primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            if (!primary.HasCharacters(latin, out uint[] missingGlyphs, true, true))
                throw new InvalidOperationException("UI glyphs are missing from Noto Sans and its fallback.");
            Preset(primary, "NotoSans-Outline", new Color32(12, 40, 32, 255), .14f);
            Preset(primary, "NotoSans-TileOutline", new Color32(245, 238, 205, 230), .09f);

            TMP_Settings.defaultFontAsset = primary;
            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAssetPath").stringValue = "Fonts/";
            settings.FindProperty("m_matchMaterialPreset").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(TMP_Settings.instance);
            EditorUtility.SetDirty(primary);
            AssetDatabase.SaveAssets();
            Debug.Log("Noto Sans SDF fonts and outline materials are ready.");
        }

        static TMP_FontAsset CreateFont(string file, string name, string characters, bool dynamic)
        {
            var path = Root + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null) return asset;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + file);
            if (source == null) throw new InvalidOperationException("Missing font source: " + file);
            asset = TMP_FontAsset.CreateFontAsset(source, 64, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name;
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.TryAddCharacters(characters, out string missing);
            if (!string.IsNullOrEmpty(missing) && (dynamic || missing.Any(c => !"−→Ⅱ".Contains(c))))
                throw new InvalidOperationException(name + " is missing glyphs: " + missing);
            if (!dynamic) asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, path);
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.filterMode = FilterMode.Bilinear;
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void Preset(TMP_FontAsset font, string name, Color color, float width)
        {
            string path = Root + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", color);
            material.SetFloat("_OutlineWidth", width);
            material.SetFloat("_OutlineSoftness", 0);
            EditorUtility.SetDirty(material);
        }
    }
}
