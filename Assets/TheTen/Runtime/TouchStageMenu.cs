using System.Collections;
using TMPro;
using UnityEngine;

namespace TheTen
{
    public sealed partial class TheTenApp
    {
        public TouchRun TouchGame { get; private set; }
        RectTransform touchHud;
        const string TouchHint = "Tap tiles until the sum is 10.";
        TMP_Text touchStageTitle, tapCount, selectedTotal, touchProgress, touchHint;
        const int LowTapThreshold = 5;
        Vector2 tapCountOrigin;

        void ShowTouchStages()
        {
            IsHome = false;
            Run = null;
            TouchGame = null;
            tileComboParticles.StopAll();
            survivalHud.gameObject.SetActive(false);
            if (touchHud != null) touchHud.gameObject.SetActive(false);
            Modal("TOUCH LAB", "Select a stage. Clear all 36 tiles within the tap limit.");
            for (int i = 0; i < TouchStages.Count; i++)
            {
                int stage = i + 1;
                Button(stage.ToString("00"), overlay, 75 + 145 * (i % 4), 484 + 106 * (i / 4),
                    132, 85, paper, () => StartTouchStage(stage));
            }
            Button("HOME", overlay, 80, 1060, 560, 76, ink, LeaveTouchLab, paper);
        }

        void LeaveTouchLab()
        {
            TouchGame = null;
            if (touchHud != null) touchHud.gameObject.SetActive(false);
            survivalHud.gameObject.SetActive(true);
            ShowHome();
        }

        void StartTouchStage(int stage)
        {
            ClearOverlay();
            TouchGame = new TouchRun(stage, Balance);
            if (touchHud == null) BuildTouchHud();
            touchHud.gameObject.SetActive(true);
            touchHint.text = TouchHint;
            RenderBoard();
            UpdateTouchHud();
        }

        void BuildTouchHud()
        {
            touchHud = Rect("Touch stage HUD", root, 0, 0, 720, 1280);
            touchStageTitle = Label("", touchHud, 60, 32, 600, 70, 40, paper, align: TextAnchor.MiddleCenter);
            var card = Panel("Touch counters", touchHud, 40, 120, 640, 155, Color.white);
            MintTheme.Apply(card, "PanelDark");
            Label("TAPS LEFT", card.transform, 0, 12, 320, 42, 24, paper, align: TextAnchor.MiddleCenter);
            Label("SUM / 10", card.transform, 320, 12, 320, 42, 24, paper, align: TextAnchor.MiddleCenter);
            tapCount = Label("", card.transform, 0, 54, 320, 88, 59, paper, align: TextAnchor.MiddleCenter);
            tapCount.name = "Taps left count";
            tapCountOrigin = tapCount.rectTransform.anchoredPosition;
            selectedTotal = Label("", card.transform, 320, 54, 320, 88, 59, gold, align: TextAnchor.MiddleCenter);
            touchHint = Label(TouchHint, touchHud, 40, 282, 640, 54, 29, paper, align: TextAnchor.MiddleCenter);
            touchProgress = Label("", touchHud, 30, 1040, 660, 68, 27, paper, align: TextAnchor.MiddleCenter);
            Button("STAGES", touchHud, 40, 1150, 200, 72, paper, ShowTouchStages);
            Button("HOW TO", touchHud, 260, 1150, 200, 72, ink, ShowTouchHelp, paper);
            Button("RETRY", touchHud, 480, 1150, 200, 72, paper, () => StartTouchStage(TouchGame.Stage));
        }

        void UpdateTouchHud()
        {
            touchStageTitle.text = $"STAGE {TouchGame.Stage:00} / {TouchStages.Count}";
            tapCount.text = TouchGame.TapsLeft.ToString();
            tapCount.color = TouchGame.TapsLeft <= LowTapThreshold ? timeRed : paper;
            if (TouchGame.TapsLeft > LowTapThreshold) tapCount.rectTransform.anchoredPosition = tapCountOrigin;
            selectedTotal.text = TouchGame.Total.ToString();
            touchProgress.text = $"{TouchGame.Board.Remaining} TILES LEFT     {TouchGame.Score:N0} PTS";
        }

        void AnimateLowTaps()
        {
            if (TouchGame.TapsLeft > LowTapThreshold) return;
            float phase = Time.unscaledTime * 48f;
            tapCount.rectTransform.anchoredPosition = tapCountOrigin
                + new Vector2(2.2f * Mathf.Sin(phase), .9f * Mathf.Cos(phase * 1.4f));
        }

        void ShowTouchHelp()
        {
            Modal("MAKE 10", "Tap exposed tiles to total 10 and clear them. If no tiles can make 10, the remaining board reshuffles for free.");
            Label("Each new selection costs one tap. Tap a selected tile to undo, or tap empty space to clear your selection. Spent taps stay spent.\n\nFrom stage 3, a -1 tile subtracts one and awards double points. You can go over 10 and come back down.\n\nFrom stage 5, ×2 doubles your running sum and awards quadruple points. Use it once per match, after a number.",
                overlay, 90, 470, 540, 470, 25, paper, align: TextAnchor.MiddleLeft);
            Button("GOT IT", overlay, 80, 1040, 560, 76, ink, ClearOverlay, paper);
        }

        void SelectTouchTile(int id)
        {
            var result = TouchGame.Select(id, out bool rearranged, out var matchedIds);
            if (result == SelectionResult.Ignored) return;
            if (result == SelectionResult.Ten) PlayTileBursts(matchedIds, 1);
            if (rearranged)
            {
                foreach (var tile in TouchGame.Board.Tiles)
                    if (!tile.Removed) tileVisuals[tile.Id].SetValue(tile);
            }
            RefreshFaces();
            touchHint.text = rearranged ? "Board reshuffled for free." : TouchHint;
            UpdateTouchHud();
            if (TouchGame.State == TouchStageState.Playing) return;
            if (result == SelectionResult.Ten) StartCoroutine(ShowResultAfterTileBurst(TouchGame));
            else ShowTouchResult();
        }

        IEnumerator ShowResultAfterTileBurst(TouchRun completed)
        {
            yield return new WaitForSecondsRealtime(TileVisual.ClearDuration);
            while (TouchGame == completed && overlay != null) yield return null;
            if (TouchGame == completed) ShowTouchResult();
        }

        void ShowTouchResult()
        {
            int stage = TouchGame.Stage;
            bool clear = TouchGame.State == TouchStageState.Cleared;
            bool finished = clear && stage == TouchStages.Count;
            string title = finished ? "FINAL STAGE CLEAR!" : clear ? "STAGE CLEAR!" : "OUT OF TAPS";
            string detail = clear ? "All 36 tiles cleared." : "Try the stage again with a different selection.";
            Modal(title, detail);
            Label($"STAGE {stage:00} / {TouchStages.Count}\n\n{TouchGame.Score:N0} PTS\n{TouchGame.TapsLeft} TAPS LEFT",
                overlay, 80, 490, 560, 260, 37, paper, align: TextAnchor.MiddleCenter);
            if (clear && !finished)
                Button("NEXT STAGE", overlay, 80, 800, 560, 80, mint, () => StartTouchStage(stage + 1));
            else
                Button(finished ? "HOME" : "RETRY", overlay, 80, 800, 560, 80, mint,
                    () => { if (finished) LeaveTouchLab(); else StartTouchStage(stage); });
            Button("STAGES", overlay, 80, 912, 560, 76, ink, ShowTouchStages, paper);
        }
    }
}
