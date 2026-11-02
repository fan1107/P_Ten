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
            MintTheme.Apply(backdrop, "HomeRuins", false);
            Label("NUMBER PUZZLE  /  SURVIVAL", overlay, 95, 417, 530, 28, 19, paper, align: TextAnchor.MiddleCenter);
            Panel("Gold divider", overlay, 230, 459, 260, 2, new Color32(181, 157, 81, 220)).raycastTarget = false;
            var gem = Panel("Gold diamond", overlay, 356, 455, 8, 8, gold);
            gem.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); gem.raycastTarget = false;
            Label("MAKE ROOM\nFOR TEN.", overlay, 155, 482, 410, 72, 31, paper, align: TextAnchor.MiddleCenter);
            Label("A LITTLE ARITHMETIC.\nA LITTLE DISCOVERY.\nTHREE TILES AT A TIME.", overlay, 175, 570, 370, 65, 17, mint, align: TextAnchor.MiddleCenter);
            for (int i = 0; i < 3; i++)
            {
                float x = 179 + i * 128, y = i == 1 ? 674 : 694;
                var tile = Panel("Home tile", overlay, x, y, 106, 130, Color.white);
                MintTheme.Apply(tile, i == 1 ? "TileSelected" : "TileIdle", false); tile.raycastTarget = false;
                Label(new[] {"2", "3", "5"}[i], tile.transform, 0, 6, 106, 87, 60, ink, align: TextAnchor.MiddleCenter).color = ink;
                if (homeMotion) tile.gameObject.AddComponent<HomeTileMotion>().Phase = i * .8f;
            }
            Label("LOCAL BEST", overlay, 235, 870, 250, 25, 20, paper, align: TextAnchor.MiddleCenter);
            Label(ranking.BestScore.ToString("N0") + " PTS", overlay, 175, 902, 370, 40, 30, paper, align: TextAnchor.MiddleCenter);
            HomeIcon("Best trophy", overlay, "HomeTrophy", 206, 867, 25);
            Label("—", overlay, 185, 902, 40, 35, 22, gold);
            Label("—", overlay, 500, 902, 40, 35, 22, gold);

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
                Label(caption, overlay, x - 15, y + size + 12, size + 30, 30, 19, ink, align: TextAnchor.MiddleCenter).color = new Color32(12, 40, 32, 255);
                Label(caption, overlay, x - 16, y + size + 10, size + 30, 30, 19, paper, align: TextAnchor.MiddleCenter);
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
