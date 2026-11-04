using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.TextCore.LowLevel;
namespace TheTen.Editor {
public static class CasualMatteBuilder {
const string Root = "Assets/Resources/Atlas/CasualMatte/";
[MenuItem("THE TEN/Build Casual Matte art")]
public static void Build() {
AssetDatabase.Refresh();
Slice("Tiles",6,2,new[]{"Tile1","Tile2","Tile3","Tile4","Tile5","Tile6","Tile7","Tile8","Tile9","Tile0","TileMinus","TileDouble"},0);
Slice("Icons",3,3,new[]{"HomePlay","HomeTrophy","HomeStore","HomeBook","HomeGear","HomeChart","ReviveTicket","ReviveAd","ReviveFinish"},0);
Slice("Tile3",1,1,new[]{"Tile3"},0);
Import("Background",false); Import("HomeHero",false);
Panel("PanelDark", new Color32(72,47,98,255)); Panel("PanelCream",new Color32(255,237,193,255));
Panel("ButtonPlum",new Color32(239,120,116,255)); Panel("BarTrack",new Color32(42,27,62,255)); Panel("Round",Color.white);
BuildFont();
var path=Root+"CasualMatte.spriteatlas";
var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
if(atlas==null){atlas=new SpriteAtlas();AssetDatabase.CreateAsset(atlas,path);}
atlas.Remove(atlas.GetPackables());
atlas.Add(new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Tiles.png"),AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Icons.png"),AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Tile3.png")});
atlas.SetPackingSettings(new SpriteAtlasPackingSettings{padding=4,enableRotation=false,enableTightPacking=false});
atlas.SetTextureSettings(new SpriteAtlasTextureSettings{filterMode=FilterMode.Bilinear,generateMipMaps=false,sRGB=true});
atlas.SetIncludeInBuild(true);EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();
}
static void Panel(string name,Color fill){
var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);
for(int y=0;y<128;y++)for(int x=0;x<128;x++){
float dx=Mathf.Max(Mathf.Abs(x-63.5f)-36,0),dy=Mathf.Max(Mathf.Abs(y-63.5f)-36,0);
float alpha=Mathf.Clamp01(27.5f-Mathf.Sqrt(dx*dx+dy*dy));
var c=fill;
if(name!="Round") c=Color.Lerp(fill*.73f,fill,Mathf.Clamp01((y-4)/8f));
c.a=alpha;tex.SetPixel(x,y,c);
}
tex.Apply();File.WriteAllBytes(Root+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
AssetDatabase.ImportAsset(Root+name+".png");var importer=Import(name,false);importer.spriteBorder=new Vector4(32,32,32,32);importer.SaveAndReimport();
}
        static TextureImporter Import(string name, bool read)
        {
            var path = Root + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = read; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100; importer.SaveAndReimport();
            return importer;
        }
        static void Slice(string name, int columns, int rows, string[] names, int border)
        {
            var importer = Import(name, true);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + name + ".png");
            var pixels = texture.GetPixels32();
            var metadata = new SpriteMetaData[names.Length];
            for (int n = 0; n < names.Length; n++)
            {
                int x0 = n % columns * texture.width / columns, x1 = (n % columns + 1) * texture.width / columns;
                if (name == "Tiles")
                {
                    // The generated sheet has slightly inset outer columns. Split at its transparent gutters.
                    int[] gutters = { 0, 278, 523, 768, 1013, 1259, 1536 };
                    x0 = gutters[n % columns] * texture.width / 1536;
                    x1 = gutters[n % columns + 1] * texture.width / 1536;
                }
                int y0 = (rows - 1 - n / columns) * texture.height / rows, y1 = (rows - n / columns) * texture.height / rows;
                int left = x1, right = x0, bottom = y1, top = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a > 240)
                    { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
                if (right <= left || top <= bottom) throw new InvalidOperationException("Empty sprite: " + names[n]);
                metadata[n] = new SpriteMetaData { name = names[n], rect = new Rect(left, bottom, right - left + 1, top - bottom + 1), pivot = new Vector2(.5f, .5f), alignment = 0, border = new Vector4(border, border, border, border) };
            }
            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable 618
            importer.spritesheet = metadata;
#pragma warning restore 618
            importer.isReadable = false; importer.SaveAndReimport();
        }
        static void BuildFont()
        {
            var path = Root + "Fredoka SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) return;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fredoka.ttf");
            var font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "Fredoka SDF";
            font.material.SetFloat("_FaceDilate", .16f);
            font.TryAddCharacters(string.Concat(Enumerable.Range(32, 95).Select(c => (char)c)) + "×−", out string missing);
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/NotoSans-Bold SDF.asset") };
            AssetDatabase.CreateAsset(font, path);
            foreach (var texture in font.atlasTextures) { texture.filterMode = FilterMode.Bilinear; AssetDatabase.AddObjectToAsset(texture, font); }
            AssetDatabase.AddObjectToAsset(font.material, font); EditorUtility.SetDirty(font);
        }
    }
}
