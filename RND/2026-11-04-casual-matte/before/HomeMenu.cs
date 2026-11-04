using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    public sealed partial class TheTenApp
    {
        bool homeMotion = true;

        void BuildRuinsHome()
        {
            IsHome = true; comboFeedback?.Hide(); ClearOverlay();
            overlay = Rect("Home menu", root, 0, 0, 720, 1280);
            var backdrop = Panel("Home ruins", overlay, 0, 0, 720, 1280, Color.white);
            // Keep HomeRuins.png as the original; the monument is a separate background asset.
            MintTheme.Apply(backdrop, "HomeRuinsMonument", false);
            Label("NUMBER PUZZLE  /  SURVIVAL", overlay, 95, 402, 530, 33, 22, paper, align: TextAnchor.MiddleCenter);
            var intro = Rect("Home introduction", overlay, 90, 439, 540, 44).gameObject.AddComponent<LetterWave>();
            intro.Initialize(font, 36); intro.enabled = homeMotion;
            Label("LOCAL BEST", overlay, 235, 864, 250, 34, 24, paper, align: TextAnchor.MiddleCenter);
            Label(ranking.BestScore.ToString("N0") + " PTS", overlay, 175, 902, 370, 46, 36, paper, align: TextAnchor.MiddleCenter);
            HomeIcon("Best trophy", overlay, "HomeTrophy", 206, 867, 25);

            HomeButton("LET'S PLAY  →", "PLAY", "HomePlay", 79, 1014, 112, StartRun, true);
            HomeButton("Rank button", "RANKING", "HomeTrophy", 229, 1014, 112, () => ShowRanking(0));
            HomeButton("REVIVE PASS / MOCK STORE", "STORE", "HomeStore", 379, 1014, 112, ShowStore);
            HomeButton("HOW TO PLAY", "HOW TO", "HomeBook", 529, 1014, 112, ShowHelp);
            HomeButton("Home settings", "", "HomeGear", 640, 26, 54, ShowHomeSettings);
            HomeButton("Home records", "", "HomeChart", 640, 100, 54, ShowHomeRecord);
        }

        void HomeIcon(string name, Transform parent, string sprite, float x, float y, float size)
        {
            var icon = Panel(name, parent, x, y, size, size, Color.white);
            MintTheme.Apply(icon, sprite, false); icon.raycastTarget = false;
        }
        void HomeButton(string name, string caption, string sprite, float x, float y, float size, Action action, bool play = false, string frame = null)
        {
            var shadow = Panel(name + " shadow", overlay, x + 4, y + 6, size, size, new Color32(0, 26, 23, 180));
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
