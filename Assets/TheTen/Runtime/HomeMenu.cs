using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    public sealed partial class TheTenApp
    {
        bool homeMotion = true;
        int[] homePreviewValues;

        void EnsureHomePreview()
        {
            if (homePreviewValues != null) return;
            var combinations = new System.Collections.Generic.List<int[]>();
            for (int a = 1; a <= 8; a++)
                for (int b = a; b <= 8; b++)
                {
                    int c = 10 - a - b;
                    if (c >= b && c <= 9) combinations.Add(new[] { a, b, c });
                }
            string key = SaveNamespace + ".HomePreviewCombination";
            int previous = PlayerPrefs.GetInt(key, -1);
            var random = new System.Random(Guid.NewGuid().GetHashCode());
            int index = previous >= 0 && previous < combinations.Count
                ? (previous + 1 + random.Next(combinations.Count - 1)) % combinations.Count
                : random.Next(combinations.Count);
            homePreviewValues = (int[])combinations[index].Clone();
            for (int i = 2; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = homePreviewValues[i]; homePreviewValues[i] = homePreviewValues[j]; homePreviewValues[j] = temp;
            }
            PlayerPrefs.SetInt(key, index); PlayerPrefs.Save();
        }

        void BuildHomePreviewTiles()
        {
            EnsureHomePreview();
            for (int i = 0; i < 3; i++)
            {
                int value = homePreviewValues[i];
                var tile = Panel("Home preview tile " + i, overlay, 140 + i * 150, 576, 140, 140, Color.white);
                tile.sprite = JewelTheme.Tile(value); tile.raycastTarget = false;
                var number = Label(value.ToString(), tile.transform, 8, 31, 124, 76, 66, ink, align: TextAnchor.MiddleCenter);
                number.color = ink; UiTypography.ApplyOutline(number);
            }
        }

        void BuildRuinsHome()
        {
            IsHome = true; comboFeedback?.Hide(); tileComboParticles?.StopAll(); ClearOverlay();
            overlay = Rect("Home menu", root, 0, 0, 720, 1280);
            var backdrop = Panel("Home ruins", overlay, 0, 0, 720, 1280, Color.white);
            MintTheme.Apply(backdrop, "HomeRuinsMonument", false);
            var hero = Panel("Ten hero", overlay, 35, 40, 650, 740, Color.white);
            hero.sprite = Resources.Load<Sprite>("Atlas/CasualMatte/HomeHero"); hero.preserveAspect = true; hero.raycastTarget = false;
            BuildHomePreviewTiles();
            var intro = Rect("Home introduction", overlay, 90, 795, 540, 44).gameObject.AddComponent<LetterWave>();
            intro.Initialize(font, 36, "TAP TO MAKE 10."); intro.enabled = homeMotion;
            Button("Home records", overlay, 170, 865, 380, 82, ink, ShowHomeRecord);
            overlay.Find("Home records").GetComponentInChildren<TMPro.TMP_Text>().text = "LOCAL BEST  " + ranking.BestScore.ToString("N0") + " PTS";
            Button("PLAY R&D", overlay, 75, 976, 310, 100, mint, ShowTouchStages, paper);
            overlay.Find("PLAY R&D").GetComponentInChildren<TMPro.TMP_Text>().fontSize = 38;
            Button("SURVIVAL", overlay, 401, 976, 244, 100, ink, StartRun, paper);
            HomeButton("Rank button", "RANKING", "HomeTrophy", 139, 1112, 102, () => ShowRanking(0));
            HomeButton("REVIVE PASS / MOCK STORE", "STORE", "HomeStore", 309, 1112, 102, ShowStore);
            HomeButton("HOW TO PLAY", "HOW TO", "HomeBook", 479, 1112, 102, ShowHelp);
            HomeButton("Home settings", "", "HomeGear", 630, 28, 60, ShowHomeSettings);
        }

        void HomeIcon(string name, Transform parent, string sprite, float x, float y, float size)
        {
            var icon = Panel(name, parent, x, y, size, size, Color.white);
            MintTheme.Apply(icon, sprite, false); icon.raycastTarget = false;
        }
        void HomeButton(string name, string caption, string sprite, float x, float y, float size, Action action, bool play = false, string frame = null)
        {
            var shadow = Panel(name + " shadow", overlay, x + 4, y + 6, size, size, new Color32(24, 13, 42, 100));
            shadow.raycastTarget = false;
            var face = Panel(name, overlay, x, y, size, size, Color.white);
            MintTheme.Apply(face, frame ?? (play ? "HomePlayFrame" : "HomeIconFrame"));
            var button = face.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => action());
            var colors = button.colors; colors.highlightedColor = new Color(1, 1, .85f); colors.pressedColor = new Color(.72f, .86f, .68f); button.colors = colors;
            HomeIcon(name + " icon", face.transform, sprite, size * .2f, size * .2f, size * .6f);
            if (caption.Length > 0)
            {
                Label(caption, overlay, x - 15, y + size + 10, size + 30, 30, 22, paper, align: TextAnchor.MiddleCenter);
            }
        }
        void ShowHomeSettings()
        {
            Modal("SETTINGS", "A little calm before your next run.");
            Button("MENU MOTION: " + (homeMotion ? "ON" : "OFF"), overlay, 80, 555, 560, 80, mint, () => { homeMotion = !homeMotion; ShowHomeSettings(); });
            Button("HOW TO PLAY", overlay, 80, 665, 560, 70, paper, ShowHelp);
            Button("BACK", overlay, 80, 1000, 560, 80, ink, ShowHome, paper);
        }
        void ShowHomeRecord()
        {
            Modal("YOUR RECORD", "LOCAL BEST  /  THIS DEVICE");
            HomeIcon("Record trophy", overlay, "HomeTrophy", 310, 490, 100);
            Label(ranking.BestScore.ToString("N0") + " PTS", overlay, 80, 650, 560, 80, 50, gold, align: TextAnchor.MiddleCenter);
            Button("WORLD / COUNTRY RANKING", overlay, 80, 835, 560, 75, mint, () => ShowRanking(0));
            Button("BACK", overlay, 80, 1000, 560, 80, ink, ShowHome, paper);
        }
    }
}
