using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace TheTen.Editor
{
    public static class MintArtBuilder
    {
        const string Root = "Assets/Resources/Atlas/";
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        static Texture2D New(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(new Color[w * h]); return t;
        }
        static void Fill(Texture2D t, int x, int y, int w, int h, Color c)
        { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) t.SetPixel(i, t.height - 1 - j, c); }
        static Sprite Save(Texture2D t, string name, int border = 0)
        {
            t.Apply(); string path = Root + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.alphaIsTransparency = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite Tile(string name, bool blocked, bool selected)
        {
            var t = New(32, 40);
            Color edge = C(selected ? "E9AC38" : "244B3D");
            Fill(t, 3, 3, 27, 36, C("1D372C")); Fill(t, 1, 2, 30, 35, edge); Fill(t, 2, 0, 28, 39, edge);
            Fill(t, 3, 27, 26, 10, C(blocked ? "45624E" : "60876A"));
            Fill(t, 3, 33, 26, 2, C(blocked ? "59715B" : "87A47A"));
            Fill(t, 3, 36, 26, 1, C("365B46"));
            Fill(t, 2, 2, 28, 28, C(blocked ? "9DAD89" : "E5D9B6"));
            Fill(t, 3, 1, 26, 30, C(blocked ? "AFBA96" : "F8EDCF"));
            Fill(t, 4, 2, 24, 1, C(blocked ? "C2CCAB" : "FFFAE7"));
            Fill(t, 3, 3, 1, 25, C(blocked ? "C2CCAB" : "FFFAE7"));
            Fill(t, 5, 30, 22, 1, C(blocked ? "879B77" : "CFCAA2"));
            if (selected)
            {
                Fill(t, 2, 1, 28, 1, C("FFF09A")); Fill(t, 1, 3, 1, 26, C("FFF09A"));
                Fill(t, 29, 3, 1, 26, C("F5BC42")); Fill(t, 3, 29, 26, 1, C("F5BC42"));
            }
            return Save(t, name);
        }
        static Sprite Frame(string name, string center, string rim, string highlight)
        {
            var t = New(24, 24);
            Fill(t, 2, 0, 20, 24, C("173D32")); Fill(t, 0, 2, 24, 20, C("173D32"));
            Fill(t, 2, 2, 20, 20, C(rim)); Fill(t, 3, 3, 18, 2, C(highlight));
            Fill(t, 3, 5, 2, 14, C(highlight)); Fill(t, 5, 5, 14, 14, C(center));
            Fill(t, 4, 20, 16, 1, C("274C3A"));
            return Save(t, name, 6);
        }
        [MenuItem("THE TEN/Build Mint Cream art")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);
            var sprites = new List<Sprite> { Tile("TileIdle", false, false), Tile("TileBlocked", true, false), Tile("TileSelected", false, true),
                Frame("PanelDark", "214E3C", "658868", "ABC393"), Frame("PanelCream", "F8EDCF", "A7B993", "FFF9DF"),
                Frame("ButtonMint", "AFC79E", "718F68", "D9E6B9"), Frame("BarTrack", "183C32", "6D906D", "C1D3A0") };
            var garden = (TextureImporter)AssetImporter.GetAtPath(Root + "MintGarden.png");
            if (garden != null)
            {
                garden.textureType = TextureImporterType.Sprite; garden.spriteImportMode = SpriteImportMode.Single;
                garden.filterMode = FilterMode.Point; garden.mipmapEnabled = false; garden.textureCompression = TextureImporterCompression.Uncompressed;
                garden.npotScale = TextureImporterNPOTScale.None; garden.maxTextureSize = 2048; garden.SaveAndReimport();
            }
            string atlasPath = Root + "MintCream.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, atlasPath); }
            atlas.Remove(atlas.GetPackables()); atlas.Add(sprites.Cast<Object>().ToArray());
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings { padding = 4, enableRotation = false, enableTightPacking = false });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings { filterMode = FilterMode.Point, generateMipMaps = false, sRGB = true });
            atlas.SetIncludeInBuild(true); EditorUtility.SetDirty(atlas);
            BuildFont(); AssetDatabase.SaveAssets();
            if (File.Exists(Root + "HomeRuins.png")) HomeArtBuilder.Build();
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        }

        // Original 5x7 glyphs: no external font download or license dependency.
        static void BuildFont()
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string[] patterns = {
                "01110/10001/10001/11111/10001/10001/10001", "11110/10001/10001/11110/10001/10001/11110",
                "01111/10000/10000/10000/10000/10000/01111", "11110/10001/10001/10001/10001/10001/11110",
                "11111/10000/10000/11110/10000/10000/11111", "11111/10000/10000/11110/10000/10000/10000",
                "01111/10000/10000/10111/10001/10001/01111", "10001/10001/10001/11111/10001/10001/10001",
                "11111/00100/00100/00100/00100/00100/11111", "00111/00010/00010/00010/00010/10010/01100",
                "10001/10010/10100/11000/10100/10010/10001", "10000/10000/10000/10000/10000/10000/11111",
                "10001/11011/10101/10101/10001/10001/10001", "10001/11001/10101/10011/10001/10001/10001",
                "01110/10001/10001/10001/10001/10001/01110", "11110/10001/10001/11110/10000/10000/10000",
                "01110/10001/10001/10001/10101/10010/01101", "11110/10001/10001/11110/10100/10010/10001",
                "01111/10000/10000/01110/00001/00001/11110", "11111/00100/00100/00100/00100/00100/00100",
                "10001/10001/10001/10001/10001/10001/01110", "10001/10001/10001/10001/10001/01010/00100",
                "10001/10001/10001/10101/10101/10101/01010", "10001/10001/01010/00100/01010/10001/10001",
                "10001/10001/01010/00100/00100/00100/00100", "11111/00001/00010/00100/01000/10000/11111",
                "01110/10001/10011/10101/11001/10001/01110", "00100/01100/00100/00100/00100/00100/01110",
                "01110/10001/00001/00010/00100/01000/11111", "11110/00001/00001/01110/00001/00001/11110",
                "00010/00110/01010/10010/11111/00010/00010", "11111/10000/10000/11110/00001/00001/11110",
                "01110/10000/10000/11110/10001/10001/01110", "11111/00001/00010/00100/01000/01000/01000",
                "01110/10001/10001/01110/10001/10001/01110", "01110/10001/10001/01111/00001/00001/01110"
            };
            var map = new Dictionary<char, string>();
            for (int i = 0; i < letters.Length; i++) map[letters[i]] = patterns[i];
            map['!'] = "00100/00100/00100/00100/00100/00000/00100";
            map['?'] = "01110/10001/00001/00010/00100/00000/00100";
            map['.'] = "00000/00000/00000/00000/00000/00110/00110";
            map[','] = "00000/00000/00000/00000/00110/00100/01000";
            map[':'] = "00000/00110/00110/00000/00110/00110/00000";
            map['/'] = "00001/00001/00010/00100/01000/10000/10000";
            map['+'] = "00000/00100/00100/11111/00100/00100/00000";
            map['-'] = map['−'] = "00000/00000/00000/11111/00000/00000/00000";
            map['='] = "00000/00000/11111/00000/11111/00000/00000";
            map['×'] = "00000/10001/01010/00100/01010/10001/00000";
            map['•'] = map['·'] = "00000/00000/00100/01110/00100/00000/00000";
            map['→'] = "00000/00100/00010/11111/00010/00100/00000";
            map['Ⅱ'] = "11011/11011/11011/11011/11011/11011/11011";
            map['('] = "00010/00100/01000/01000/01000/00100/00010";
            map[')'] = "01000/00100/00010/00010/00010/00100/01000";
            map['\''] = "00100/00100/00000/00000/00000/00000/00000";
            map['…'] = "00000/00000/00000/00000/00000/00000/10101";
            var chars = Enumerable.Range(32, 95).Select(i => (char)i).Concat(new[] { '×', '•', '·', '→', 'Ⅱ', '−', '…' }).ToArray();
            var texture = New(128, 80); var info = new List<CharacterInfo>();
            for (int n = 0; n < chars.Length; n++)
            {
                int x = n % 16 * 8 + 1, y = n / 16 * 10 + 1;
                char lookup = char.ToUpperInvariant(chars[n]);
                if (map.TryGetValue(lookup, out var glyph))
                {
                    var rows = glyph.Split('/');
                    for (int row = 0; row < 7; row++) for (int col = 0; col < 5; col++) if (rows[row][col] == '1') Fill(texture, x + col, y + row, 1, 1, Color.white);
                }
                float u0 = x / 128f, u1 = (x + 5) / 128f, v1 = 1 - y / 80f, v0 = 1 - (y + 7) / 80f;
                info.Add(new CharacterInfo { index = chars[n], advance = chars[n] == ' ' ? 4 : 6, minX = 0, maxX = 5, minY = -1, maxY = 6,
                    uvBottomLeft = new Vector2(u0, v0), uvBottomRight = new Vector2(u1, v0), uvTopLeft = new Vector2(u0, v1), uvTopRight = new Vector2(u1, v1) });
            }
            Save(texture, "MintGlyphs");
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "MintPixel.fontsettings");
            if (font == null) { font = new Font("Mint Pixel"); AssetDatabase.CreateAsset(font, Root + "MintPixel.fontsettings"); }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "MintPixel.mat");
            if (mat == null) { mat = new Material(Shader.Find("GUI/Text Shader")); AssetDatabase.CreateAsset(mat, Root + "MintPixel.mat"); }
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "MintGlyphs.png");
            font.material = mat; font.characterInfo = info.ToArray();
            var serialized = new SerializedObject(font); serialized.FindProperty("m_FontSize").floatValue = 8;
            serialized.FindProperty("m_LineSpacing").floatValue = 10; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font); EditorUtility.SetDirty(mat);
        }
    }
}
