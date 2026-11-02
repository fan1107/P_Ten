using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace TheTen.Editor
{
    public static class HomeArtBuilder
    {
        const string Root = "Assets/Resources/Atlas/";
        static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            {'c', new Color32(255, 244, 203, 255)}, {'g', new Color32(255, 202, 80, 255)},
            {'o', new Color32(188, 117, 45, 255)}, {'d', new Color32(18, 48, 39, 255)},
            {'r', new Color32(213, 78, 58, 255)}, {'s', new Color32(112, 141, 111, 255)}
        };
        static Sprite Icon(string name, params string[] rows)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[256]);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (Palette.TryGetValue(rows[y][x], out var color)) texture.SetPixel(x, 15 - y, color);
            return Save(texture, name, 0);
        }
        static Sprite Save(Texture2D texture, string name, int border)
        {
            texture.Apply(); var path = Root + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
            importer.spriteBorder = Vector4.one * border; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite Frame(string name, bool green, bool goldBorder = false)
        {
            var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++)
            {
                int edge = Mathf.Min(x, y, 23 - x, 23 - y);
                Color color = edge < 2 ? Palette['d'] : edge < 3 ? Palette[goldBorder ? 'o' : 's']
                    : edge < 4 ? Palette[goldBorder ? 'g' : 'c'] : goldBorder && edge < 5 ? Palette['c']
                    : green ? new Color32(62, 103, 43, 255) : new Color32(15, 54, 46, 255);
                texture.SetPixel(x, y, color);
            }
            return Save(texture, name, 5);
        }
        public static void Build()
        {
            var sprites = new List<Sprite>();
            sprites.Add(Icon("HomePlay", "................", "....cc..........", "....ccc.........", "....cccc........", "....ccccc.......", "....cccccc......", "....ccccccc.....", "....cccccccc....", "....cccccccc....", "....ccccccc.....", "....cccccc......", "....ccccc.......", "....cccc........", "....ccc.........", "....cc.........."));
            sprites.Add(Icon("HomeTrophy", "................", ".....oooooo.....", "....oggggggo....", "..ooogccccgooo..", "..ogogccccgogo..", "..ogogcccggogo..", "..ogogcggggogo..", "..oooggggggooo..", "....ooggggoo....", ".....ooggoo.....", "......oggo......", "......oggo......", "......oggo......", "....ooggggoo....", "....oggggggo....", "....oooooooo...."));
            sprites.Add(Icon("HomeStore", "................", "...dddddddddd...", "..rrccrrccrrcc..", "..rrccrrccrrcc..", ".rrrccrrccrrccc.", ".rrrccrrccrrccc.", ".ooossoossoosss.", "..dccccccccccd..", "..dcsssddssscd..", "..dcsssddssscd..", "..dcsssddssscd..", "..dccccddccccd..", "..dccccddccccd..", "..dddddddddddd..", "..ssssssssssss.."));
            sprites.Add(Icon("HomeBook", "................", "...........rr...", "..cccc.ddccrrc..", ".ccccccddccrrcc.", ".ccccccddccrrcc.", ".ccccccddccrrcc.", ".ccccccddcccccc.", ".ccccccddcccccc.", ".ccccccddcccccc.", ".ccccccddcccccc.", ".ccccccddcccccc.", ".ssssssddssssss.", ".dssssssssssssd.", "..dddddddddddd..", "................"));
            sprites.Add(Icon("HomeGear", "......cccc......", "......cccc......", "..cc..cccc..cc..", "..cccccccccccc..", "...cccccccccc...", "...cccddddccc...", "ccccccddddcccccc", "ccccddddddddcccc", "ccccddddddddcccc", "ccccccddddcccccc", "...cccddddccc...", "...cccccccccc...", "..cccccccccccc..", "..cc..cccc..cc..", "......cccc......", "......cccc......"));
            sprites.Add(Icon("HomeChart", "................", ".......ccc......", ".......ccc......", ".......ccc......", ".......ccc......", ".......ccc.ccc..", ".......ccc.ccc..", "..ccc..ccc.ccc..", "..ccc..ccc.ccc..", "..ccc..ccc.ccc..", "..ccc..ccc.ccc..", "..ccc..ccc.ccc..", "..ccc..ccc.ccc..", "..ccc..ccc.ccc..", "................"));
            sprites.Add(Frame("HomeIconFrame", false)); sprites.Add(Frame("HomePlayFrame", true));
            sprites.AddRange(ReviveSprites());
            var background = (TextureImporter)AssetImporter.GetAtPath(Root + "HomeRuins.png");
            background.textureType = TextureImporterType.Sprite; background.spriteImportMode = SpriteImportMode.Single;
            background.filterMode = FilterMode.Point; background.mipmapEnabled = false; background.textureCompression = TextureImporterCompression.Uncompressed;
            background.npotScale = TextureImporterNPOTScale.None; background.maxTextureSize = 2048; background.SaveAndReimport();
            AddToAtlas(sprites);
            AssetDatabase.SaveAssets();
        }
        static List<Sprite> ReviveSprites()
        {
            return new List<Sprite>
            {
                Icon("ReviveAd", "................", "................", "..gggggggggggg..", "..occcccccccco..", "..odccccccccdo..", "..odccdcccccdo..", "..occcddccccco..", "..odccdddcccdo..", "..odccddddccdo..", "..occcdddcccco..", "..odccddccccdo..", "..odccdcccccdo..", "..occcccccccco..", "..oooooooooooo..", "................", "................"),
                Icon("ReviveFinish", "................", "................", "...cs...........", "...csccddc......", "...csccddccddc..", "...csddccdcddc..", "...csddccddccd..", "...csccddcdccd..", "...csccddccddc..", "...csddccdcddc..", "...csddccddccd..", "...cs.....dccd..", "...cs...........", "...cs...........", "...cs...........", "................"),
                Icon("ReviveTicket", "................", "................", "................", "..gggggggggggg..", "..gccccccccccg..", "..gcgddggddgcg..", "..gcgddddddgcg..", "....gddddddg....", "....ggddddgg....", "..gcgggddgggcg..", "..gcggggggggcg..", "..gggggggggggg..", "..oooooooooooo..", "................", "................", "................"),
                Frame("RevivePrimaryFrame", true, true)
            };
        }
        [MenuItem("THE TEN/Build revive icons")]
        public static void BuildRevive()
        {
            AddToAtlas(ReviveSprites());
            AssetDatabase.SaveAssets();
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(Root + "MintCream.spriteatlas");
            if (atlas != null) UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        }
        static void AddToAtlas(List<Sprite> sprites)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(Root + "MintCream.spriteatlas");
            if (atlas != null)
            {
                var existing = new HashSet<Object>(UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas));
                UnityEditor.U2D.SpriteAtlasExtensions.Add(atlas, sprites.Where(s => !existing.Contains(s)).Cast<Object>().ToArray());
                EditorUtility.SetDirty(atlas);
            }
        }
    }
}
