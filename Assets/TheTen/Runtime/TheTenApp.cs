using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace TheTen
{
    public sealed partial class TheTenApp : MonoBehaviour, IPointerClickHandler
    {
        public GameBalance Balance;
        [HideInInspector] public string SaveNamespace = "TheTen";
        public RunModel Run { get; private set; }
        public ReviveTicketManager Tickets { get; private set; }
        public bool IsHome { get; private set; } = true;
        readonly Dictionary<int, Image> faces = new Dictionary<int, Image>();
        readonly Dictionary<int, TMP_Text> numbers = new Dictionary<int, TMP_Text>();
        readonly Dictionary<int, TileVisual> tileVisuals = new Dictionary<int, TileVisual>();
        readonly Color ink = JewelTheme.Ink;
        readonly Color mint = new Color32(239, 120, 116, 255);
        readonly Color paper = new Color32(245, 242, 228, 255);
        readonly Color muted = new Color32(180, 151, 137, 255);
        readonly Color gold = new Color32(243, 203, 111, 255);
        readonly Color timeGreen = new Color32(119, 198, 104, 255);
        readonly Color timeYellow = new Color32(244, 200, 79, 255);
        readonly Color timeRed = new Color32(238, 101, 86, 255);
        readonly Color comboPurple = new Color32(178, 121, 237, 255);
        TMP_FontAsset font;
        Canvas canvas;
        RectTransform root, boardRoot, boardEffectsRoot, overlay, shuffleNotice;
        TMP_Text timerText, chapterText, bestText, toastText, lifeText, scoreText;
        Image timeFill, comboFill;
        ComboFeedback comboFeedback;
        TileComboParticles tileComboParticles;
        ScalePulse scorePulse;
        int displayedScore;
        MockServerClock mockClock;
        IRewardedAdService ads;
        LocalRanking ranking;
        string runId;
        bool operationBusy, transitionBusy, submitted, appSuspended;
        float refreshIn, toastUntil;
        float penaltyStartedAt = float.NegativeInfinity;
        RunState renderedState;
        bool destroyed;

        async void Start()
        {
            if (Balance == null) Balance = ScriptableObject.CreateInstance<GameBalance>();
            ranking = new LocalRanking(SaveNamespace);
            font = UiTypography.Font;
            ITrustedServerClock clock;
            IPurchaseGateway purchase;
            if (Balance.EnableMockCommerce)
            { mockClock = new MockServerClock(); clock = mockClock; purchase = new MockPurchaseGateway(SaveNamespace); ads = new MockRewardedAdService(Balance); }
            else
            { clock = new UnavailableServerClock(); purchase = new UnavailablePurchaseGateway(); ads = new UnavailableAdService(); }
            Tickets = new ReviveTicketManager(clock, new PlayerPrefsTicketStorage(SaveNamespace + (Balance.EnableMockCommerce ? ".Mock.Tickets.v15" : ".Production.Tickets.v15")), purchase);
            BuildCanvas(); BuildGame(); ShowHome();
            await Tickets.RestoreAsync();
            if (!destroyed && IsHome) ShowHome();
        }
        void OnDestroy() { destroyed = true; }
        void Update()
        {
            if (canvas == null) return;
            var safe = Screen.safeArea;
            canvas.scaleFactor = Mathf.Min(safe.width / 720f, safe.height / 1280f);
            root.anchoredPosition = (safe.center - new Vector2(Screen.width, Screen.height) / 2) / canvas.scaleFactor;
            if (Run != null && !IsHome)
            {
                if (!appSuspended) Run.Tick(Time.unscaledDeltaTime);
                UpdateHud();
                if ((Run.State == RunState.Shuffling || Run.State == RunState.BoardTransition) && !transitionBusy) StartCoroutine(Transition());
                if (Run.State != renderedState)
                {
                    renderedState = Run.State;
                    RefreshFaces();
                    if (Run.State == RunState.ReviveChoice) ShowRevive();
                    if (Run.State == RunState.GameOver) ShowGameOver();
                }
            }
            if (Time.unscaledTime > toastUntil && toastText != null) toastText.text = "";
            refreshIn -= Time.unscaledDeltaTime;
            if (refreshIn <= 0) { refreshIn = 30; RefreshTickets(); }
        }
        async void RefreshTickets() { if (Tickets != null) await Tickets.RefreshAsync(); }
        void OnApplicationPause(bool pause) { HandleFocus(!pause); }
        void OnApplicationFocus(bool focus) { HandleFocus(focus); }
        void HandleFocus(bool focus)
        {
            appSuspended = !focus;
            if (!focus && Run != null && !IsHome && Run.State == RunState.Playing) { Run.Pause(); ShowPause(); }
            if (focus) RefreshTickets();
        }
        void BuildCanvas()
        {
            var go = new GameObject("THE TEN • Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            go.transform.SetParent(transform);
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Catch empty screen margins as well as the background inside the safe area.
            go.GetComponent<Image>().color = Color.clear;
            root = Rect("Portrait safe area", go.transform, 0, 0, 720, 1280);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            Panel("Background", root, 0, 0, 720, 1280, new Color32(224, 234, 218, 255));
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform);
            }
        }
        void BuildGame()
        {
            chapterText = Label("CHAPTER 01", root, 110, 28, 500, 64, 38, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Rect("Play introduction", root, 90, 1220, 540, 36).gameObject.AddComponent<LetterWave>().Initialize(font);
            Button("Ⅱ", root, 614, 43, 64, 64, paper, () => { if (Run != null && Run.State == RunState.Playing) { Run.Pause(); ShowPause(); } });
            Panel("Clock card", root, 40, 115, 640, 100, ink);
            lifeText = Label("LIFE 1 / 2", root, 250, 88, 220, 24, 22, mint, align: TextAnchor.MiddleRight);
            timerText = Label(Balance.InitialTime.ToString("0.0"), root, 60, 125, 280, 80, 64, paper, FontStyle.Bold);
            timerText.name = "Countdown";
            timerText.rectTransform.pivot = new Vector2(0, .5f);
            timerText.rectTransform.anchoredPosition += new Vector2(0, -40);

            Panel("Time track", root, 40, 229, 640, 34, mint);
            timeFill = Panel("Time remaining", root, 48, 235, 624, 22, timeGreen);
            const float comboCenterY = 302;
            Panel("Combo track", root, 40, comboCenterY - 13, 640, 26, mint);
            comboFill = Panel("Combo remaining", root, 48, comboCenterY - 7, 0, 14, comboPurple);
            // Keep the fill centered on the badge as its thickness pulses.
            comboFill.rectTransform.pivot = new Vector2(0, .5f);
            comboFill.rectTransform.anchoredPosition = new Vector2(48, -comboCenterY);
            comboFeedback = Rect("Combo badge", root, 284, comboCenterY - 27, 152, 54).gameObject.AddComponent<ComboFeedback>();
            comboFeedback.Initialize();
            scoreText = Label("0", root, 380, 132, 280, 66, 54, new Color32(246, 209, 95, 255), align: TextAnchor.MiddleCenter);
            scoreText.name = "Run score";
            scoreText.textWrappingMode = TextWrappingModes.NoWrap;
            scoreText.rectTransform.pivot = new Vector2(.5f, .5f);
            scoreText.rectTransform.anchoredPosition += new Vector2(140, -33);
            scorePulse = scoreText.gameObject.AddComponent<ScalePulse>();
            Panel("Board well", root, 30, BoardModel.BoardTop, 660, BoardModel.BoardBottom - BoardModel.BoardTop, new Color32(205, 220, 199, 255));
            boardRoot = Rect("Layered tiles", root, 0, 0, 720, 1280);
            boardEffectsRoot = Rect("Tile effects", root, 0, 0, 720, 1280);
            boardRoot.localScale = boardEffectsRoot.localScale = Vector3.one * 1.08f;
            boardRoot.anchoredPosition = boardEffectsRoot.anchoredPosition = new Vector2(-28.8f, 100);
            var tileComboRoot = Rect("Tile combo particles", root, 0, 0, 720, 1280);
            tileComboRoot.localScale = boardEffectsRoot.localScale;
            tileComboRoot.anchoredPosition = boardEffectsRoot.anchoredPosition;
            tileComboParticles = tileComboRoot.gameObject.AddComponent<TileComboParticles>();
            bestText = Label("BEST 0 PTS", root, 42, 1050, 636, 55, 32, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            bestText.name = "Best score";
            toastText = Label("", root, 35, 1110, 650, 28, 18, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Button("HOW TO PLAY", root, 42, 1165, 302, 59, paper, ShowHelp);
            Button("LAB / MOCK", root, 362, 1165, 316, 59, paper, ShowLab);

        }
        public void StartRun()
        {
            if (operationBusy || transitionBusy) return;
            if (shuffleNotice != null) Destroy(shuffleNotice.gameObject);
            ClearOverlay(); IsHome = false; submitted = false;
            penaltyStartedAt = float.NegativeInfinity;
            displayedScore = 0; scorePulse.ResetScale();
            comboFeedback.Hide();
            foreach (var effect in GetComponentsInChildren<ChapterFireworks>()) Destroy(effect.gameObject);
            runId = Guid.NewGuid().ToString("N");
            Run = new RunModel(Balance, UnityEngine.Random.Range(0, int.MaxValue));
            renderedState = RunState.Playing; RenderBoard(); UpdateHud();
        }
        public void SelectTile(int id)
        {
            if (IsHome || operationBusy || overlay != null) return;
            // The model clears a failed selection immediately; keep its IDs for visual feedback.
            int[] attempt = Run.Selection.Count == 2 ? new[] { Run.Selection[0], Run.Selection[1], id } : null;
            var result = Run.Select(id);
            if (result == SelectionResult.Ten)
            {
                var centers = attempt.Select(tileId => (Vector2)tileComboParticles.transform.InverseTransformPoint(
                    faces[tileId].rectTransform.TransformPoint(faces[tileId].rectTransform.rect.center))).ToArray();
                tileComboParticles.Play(Run.ComboMultiplier, centers);
                if (Run.State == RunState.BoardTransition) ChapterFireworks.Play(root, false);
                if (Run.LastMatchBonusMultiplier > 1)
                {
                    var tile = Run.Board.Tiles[id];
                    ChapterFireworks.PlaySpecial(boardEffectsRoot, new Vector2(tile.X + BoardModel.TileWidth / 2, -tile.Y - BoardModel.TileHeight / 2), Run.LastMatchBonusMultiplier);
                }
                toastText.text = "";
            }
            else if (result == SelectionResult.Miss)
            {
                foreach (int tileId in attempt)
                    if (tileVisuals.TryGetValue(tileId, out var visual) && visual != null) visual.ShowMiss();
                penaltyStartedAt = Time.unscaledTime;
                Toast("NOT TEN   -" + Balance.MissPenalty.ToString("0.#") + " SECONDS");
            }
            RefreshFaces(); UpdateHud();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            // Tile/menu buttons handle their own clicks before events reach this ancestor.
            if (eventData.button != PointerEventData.InputButton.Left || IsHome || operationBusy || overlay != null
                || Run == null || !Run.CancelSelection()) return;
            RefreshFaces(); UpdateHud();
        }
        void RenderBoard()
        {
            tileComboParticles.StopAll();
            foreach (Transform child in boardRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach (Transform child in boardEffectsRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            faces.Clear(); numbers.Clear(); tileVisuals.Clear();
            foreach (var tile in Run.Board.Tiles.OrderBy(t => t.Layer).ThenBy(t => t.Y).ThenBy(t => t.X))
            {
                if (tile.Removed) continue;
                var card = Rect("Tile " + tile.Id + " • " + tile.Label, boardRoot, tile.X, tile.Y, BoardModel.TileWidth, BoardModel.TileHeight);
                var shadow = Panel("Shadow", card, 5, 7, BoardModel.TileWidth, BoardModel.TileHeight, new Color32(24, 13, 42, 100));
                shadow.raycastTarget = false;
                var face = Panel("Face", card, 0, 0, BoardModel.TileWidth, BoardModel.TileHeight, Color.white);
                face.sprite = JewelTheme.Tile(tile.Value);
                face.type = Image.Type.Simple;
                face.color = JewelTheme.Tile(tile.Value) != null ? Color.white : TileVisual.TileColor(tile.Value);
                var button = face.gameObject.AddComponent<Button>(); int id = tile.Id;
                button.onClick.AddListener(() => SelectTile(id));
                button.transition = Selectable.Transition.None;
                faces[id] = face;
                numbers[id] = Label(tile.Label, face.transform, 5, 28, BoardModel.TileWidth - 10, 76, tile.Value == BoardModel.Double || tile.Value == -1 ? 44 : 54, ink, FontStyle.Normal, TextAnchor.MiddleCenter);
                numbers[id].textWrappingMode = TextWrappingModes.NoWrap;
                numbers[id].fontStyle = FontStyles.Bold;
                numbers[id].color = ink;
                UiTypography.ApplyOutline(numbers[id]);
                var visual = card.gameObject.AddComponent<TileVisual>();
                visual.Initialize(face, shadow, numbers[id], boardEffectsRoot, Run.Board.IsAvailable(id), tile.Value);
                tileVisuals[id] = visual;
            }
            RefreshFaces();
        }
        void RefreshFaces()
        {
            foreach (var pair in faces)
            {
                int id = pair.Key;
                var visual = tileVisuals[id];
                if (visual == null) continue;
                if (Run.Board.Tiles[id].Removed) { visual.ClearWithSparkles(); continue; }
                bool available = Run.Board.IsAvailable(id), selected = Run.Selection.Contains(id);
                visual.SetState(available, selected);
            }
        }
        void UpdateHud()
        {
            timerText.text = Run.RemainingTime.ToString("0.0");
            float penalty = Mathf.Clamp01((Time.unscaledTime - penaltyStartedAt) / TileVisual.MissDuration);
            float tint = 1 - Mathf.SmoothStep(0, 1, (penalty - .18f) / .82f);
            float pulseSize = penalty < .2f ? Mathf.SmoothStep(0, 1, penalty / .2f)
                : 1 - Mathf.SmoothStep(0, 1, (penalty - .2f) / .8f);
            timerText.color = Color.Lerp(Run.RemainingTime <= 10 ? gold : paper, TileVisual.MissColor, tint);
            timerText.rectTransform.localScale = Vector3.one * (1 + .14f * pulseSize);
            chapterText.text = "CHAPTER " + Run.Chapter.ToString("00");
            lifeText.text = Run.ReviveUsed ? "LIFE 2 / 2" : "LIFE 1 / 2";
            // A fresh life starts with a full bar. Earned time above that stays visible in the numeral.
            float timeRange = Mathf.Max(1, Run.ReviveUsed ? Balance.ReviveTime : Balance.InitialTime);
            float remaining = Mathf.Clamp01((float)Run.RemainingTime / timeRange);
            timeFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 624 * remaining);
            timeFill.color = remaining > 2f / 3 ? timeGreen : remaining > 1f / 3 ? timeYellow : timeRed;
            comboFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 624 * Mathf.Clamp01((float)(Run.ComboRemaining / Math.Max(.1, Balance.ComboWindow))));
            comboFill.color = ComboFeedback.TierColor(Run.ComboMultiplier);
            comboFeedback.SetState(Run.ComboMultiplier, Run.ComboRemaining > 0);
            float pulse = Run.ComboRemaining > 0 ? comboFeedback.Pulse : 0;
            comboFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 14 + 2 * pulse);
            scoreText.text = Run.Score.ToString("N0");
            if (Run.Score > displayedScore) { scorePulse.Play(); comboFeedback.PlayMatch(Run.ComboMultiplier); }
            else if (Run.Score < displayedScore) scorePulse.ResetScale();
            displayedScore = Run.Score;
            bestText.text = "BEST " + Math.Max(Run.Score, ranking.BestScore).ToString("N0") + " PTS";
        }
        IEnumerator Transition()
        {
            transitionBusy = true;
            bool shuffling = Run.State == RunState.Shuffling;
            if (shuffling) StartCoroutine(ShowShuffleNotice());
            else Toast("CHAPTER " + Run.Chapter.ToString("00") + " CLEAR!");
            yield return new WaitForSecondsRealtime(Mathf.Max(Balance.TransitionSeconds, TileVisual.ClearDuration));
            while (appSuspended || overlay != null) yield return null;
            if (Run.State == RunState.BoardTransition)
            {
                bool confirmed = false;
                comboFeedback.Hide();
                overlay = Rect("Chapter clear modal", root, 0, 0, 720, 1280);
                var dim = Panel("Dim", overlay, 0, 0, 720, 1280, new Color32(26, 51, 54, 180));
                dim.sprite = null;
                Panel("Clear card", overlay, 60, 410, 600, 440, ink);
                Label("CHAPTER " + Run.Chapter.ToString("00"), overlay, 90, 455, 540, 48, 30, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                Label("CLEAR!", overlay, 90, 520, 540, 70, 54, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                Label("ALL TILES REMOVED", overlay, 90, 615, 540, 30, 19, muted, align: TextAnchor.MiddleCenter);
                Button("CONFIRM", overlay, 130, 715, 460, 80, ink, () => confirmed = true, paper);
                ChapterFireworks.Play(overlay, true);
                while (!confirmed || appSuspended) yield return null;
                ClearOverlay();
                foreach (var effect in GetComponentsInChildren<ChapterFireworks>()) Destroy(effect.gameObject);
            }
            bool ok = Run.CompleteTransition(out bool recovered);
            RenderBoard(); transitionBusy = false;
            UpdateHud();
            if (!shuffling || !ok)
                Toast(!ok ? "No legal layout remains. Run ended safely." : "READY");
        }
        IEnumerator ShowShuffleNotice()
        {
            if (shuffleNotice != null) Destroy(shuffleNotice.gameObject);
            var notice = Rect("Shuffle notice", root, 50, 350, 620, 76);
            shuffleNotice = notice;
            var group = notice.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            var panel = Panel("Shuffle notice panel", notice, 0, 0, 620, 76, Color.white);
            MintTheme.Apply(panel, "PanelDark"); panel.raycastTarget = false;
            KoreanLabel("더 진행할 수 없어 셔플!", notice, 16, 0, 588, 76, 30, paper, TextAnchor.MiddleCenter);
            const float duration = 1.6f;
            for (float age = 0; age < duration; age += Time.unscaledDeltaTime)
            {
                if (notice == null) yield break;
                if (IsHome) break;
                group.alpha = Mathf.Min(Mathf.Clamp01(age / .15f), Mathf.Clamp01((duration - age) / .3f));
                yield return null;
            }
            if (notice != null) Destroy(notice.gameObject);
            if (shuffleNotice == notice) shuffleNotice = null;
        }

        void Toast(string message) { toastText.text = message; toastUntil = Time.unscaledTime + 2.5f; }

        void ShowHome() { BuildRuinsHome(); }
        TMP_Text KoreanLabel(string text, Transform parent, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var label = Label(text, parent, x, y, w, h, size, color, align: align);
            label.color = color;
            UiTypography.ApplyOutline(label);
            return label;
        }
        void PixelTrophy(Transform parent, float x, float y)
        {
            string[] pixels = { "01111110", "11111111", "10111101", "10111101", "01111010", "00111100", "00011000", "00011000", "00111100", "01111110" };
            for (int row = 0; row < pixels.Length; row++)
                for (int col = 0; col < pixels[row].Length; col++)
                    if (pixels[row][col] == '1') Panel("Trophy pixel", parent, x + col * 5, y + row * 5, 5, 5, row < 5 ? gold : new Color32(189, 131, 57, 255)).raycastTarget = false;
        }
        void ShowRanking(int selected)
        {
            string[] countries = { "전체", "한국", "미국", "일본", "중국" };
            Modal("RANKING", "");
            KoreanLabel("숫자를 맞추고, 더 높은 기록으로", overlay, 60, 315, 600, 35, 23, mint, TextAnchor.MiddleCenter);
            for (int i = 0; i < countries.Length; i++)
            {
                int tab = i;
                var card = Panel("Rank tab " + countries[i], overlay, 60 + i * 122, 395, 112, 62, Color.white);
                MintTheme.Apply(card, selected == i ? "ButtonMint" : "PanelDark");
                var button = card.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => ShowRanking(tab));
                button.interactable = selected != i;
                button.transition = Selectable.Transition.None;
                KoreanLabel(countries[i], card.transform, 0, 0, 112, 62, 23, selected == i ? ink : mint, TextAnchor.MiddleCenter);
                if (selected == i) Panel("Selected tab underline", card.transform, 24, 53, 64, 4, gold).raycastTarget = false;
            }
            KoreanLabel(selected == 0 ? "전 세계 랭킹" : countries[selected] + " 랭킹", overlay, 65, 485, 350, 35, 25, paper);
            KoreanLabel("점수 순", overlay, 475, 485, 180, 35, 18, mint, TextAnchor.MiddleRight);
            for (int i = 0; i < 4; i++)
            {
                float y = 540 + i * 76;
                Panel("Rank row " + (i + 1), overlay, 65, y, 590, 65, new Color32(36, 75, 58, 255));
                Label((i + 1).ToString("00"), overlay, 85, y + 10, 65, 45, 26, i == 0 ? gold : mint);
                KoreanLabel("기록 대기 중", overlay, 170, y + 15, 300, 35, 21, mint);
                Label("--", overlay, 490, y + 10, 140, 45, 25, mint, align: TextAnchor.MiddleRight);
            }
            KoreanLabel("아직 등록된 랭킹이 없습니다", overlay, 65, 860, 590, 34, 22, paper, TextAnchor.MiddleCenter);
            KoreanLabel("UI 미리보기 · 서버 연결 전", overlay, 65, 900, 590, 28, 17, mint, TextAnchor.MiddleCenter);
            var personal = Panel("Local record card", overlay, 65, 963, 590, 108, Color.white);
            MintTheme.Apply(personal, "PanelCream");
            KoreanLabel("내 기기 최고 점수", personal.transform, 24, 15, 320, 34, 22, ink);
            KoreanLabel("세계·국가 순위는 연결 후 표시됩니다", personal.transform, 24, 62, 470, 25, 16, ink);
            var localBest = Label(ranking.BestScore.ToString("N0"), personal.transform, 330, 12, 230, 42, 30, gold, align: TextAnchor.MiddleRight);
            localBest.color = ink; UiTypography.ApplyOutline(localBest);
            Button("BACK", overlay, 80, 1110, 560, 70, ink, ShowHome, paper);
        }
        void Modal(string title, string description, bool home = false)
        {
            comboFeedback?.Hide();
            ClearOverlay();
            overlay = Rect("Modal", root, 0, 0, 720, 1280);
            Panel("Modal background", overlay, 0, 0, 720, 1280, new Color32(224, 234, 218, 255));
            Panel("Modal card", overlay, 30, home ? 200 : 150, 660, home ? 1020 : 1070, ink);
            Label(title, overlay, 55, home ? 254 : 180, 610, home ? 160 : 130, home ? 67 : 51, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(description, overlay, 65, home ? 425 : 330, 590, home ? 65 : 140, home ? 18 : 20, muted, align: TextAnchor.MiddleCenter);
        }
        void ClearOverlay()
        {
            if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); overlay = null; }
        }
        void ShowPause()
        {
            if (canvas == null) return;
            Modal("TAKE A BREATH.", "Your clock is paused.\nYour tiles will be right here.");
            Button("RESUME", overlay, 80, 560, 560, 80, ink, () => { ClearOverlay(); Run.Resume(); }, paper);
            Button("END RUN", overlay, 80, 670, 560, 70, paper, () => { Run.End(); ShowGameOver(); });
        }
        void ShowHelp()
        {
            if (Run != null && !IsHome) Run.Pause();
            Modal("THREE MAKES TEN.", "Each match: +" + Balance.MatchScore + " points, +" + Balance.MatchBonus + " seconds.\nMiss: -" + Balance.MissPenalty + " seconds and selection cleared.\nCombo within " + Balance.ComboWindow + "s: ×1.5 → ×2 → ×2.5 → ×3 → ×4 → ×5.");
            Label("01   Clear the top tiles to reveal the layers.\n\n02   All 36 tiles belong to the layered board.\n        Only uncovered tiles can be selected.\n\n03   Tap a selected tile again to undo.\n        Tap empty space to cancel all selections.\n        Cancelling has no time penalty.\n\n04   No move? The board shuffles automatically.\n\n05   Chapter 3: 0 and −1. Chapter 5: ×2.\n        ×2 cannot be first, or used twice.\n\n06   0 / −1: SCORE ×2.  ×2 tile: SCORE ×4.\n        Multiplies combo points; highest bonus wins.\n        One revive per run. Your board stays.", overlay, 65, 510, 590, 480, 18, ink);
            Button("GOT IT", overlay, 80, 1080, 560, 80, ink, ReturnFromMenu, paper);
        }
        void ReturnFromMenu()
        {
            if (IsHome) { ShowHome(); return; }
            ClearOverlay();
            if (Run.State == RunState.Paused) Run.Resume();
            else if (Run.State == RunState.ReviveChoice) ShowRevive();
            else if (Run.State == RunState.GameOver) ShowGameOver();
        }
        void ShowRevive()
        {
            comboFeedback?.Hide(); ClearOverlay();
            overlay = Rect("Revive modal", root, 0, 0, 720, 1280);
            Panel("Modal background", overlay, 0, 0, 720, 1280, Color.white);
            Panel("Modal card", overlay, 50, 185, 620, 940, ink);
            Label("ONE MORE LIFE?", overlay, 65, 270, 590, 100, 48, paper, align: TextAnchor.MiddleCenter);
            Label("KEEP YOUR BOARD. START WITH " + Balance.ReviveTime.ToString("0.#") + " SECONDS.",
                overlay, 65, 420, 590, 50, 19, mint, align: TextAnchor.MiddleCenter);
            Label("SCORE   " + Run.Score.ToString("N0") + " PTS", overlay, 65, 520, 590, 60, 34, paper, align: TextAnchor.MiddleCenter);
            bool canUse = Tickets.CurrentTicketCount > 0 && Tickets.SyncState == TicketSyncState.Ready;
            HomeButton(canUse ? "USE REVIVE TICKET  (" + Tickets.CurrentTicketCount + ")" : "WATCH REWARDED AD / MOCK",
                canUse ? "USE TICKET" : "WATCH AD", canUse ? "ReviveTicket" : "ReviveAd", 94, 630, 148,
                () => { if (canUse) UseTicket(); else WatchAd(); }, frame: "RevivePrimaryFrame");
            HomeButton("FINISH RUN", "FINISH", "ReviveFinish", 286, 630, 148,
                () => { if (!operationBusy && Run.State == RunState.ReviveChoice) { Run.End(); ShowGameOver(); } });
            HomeButton("REVIVE PASS / MOCK STORE", "REVIVE PASS", "ReviveTicket", 478, 630, 148, ShowStore);
            var noteColor = new Color32(120, 166, 107, 255);
            string serviceNote = Balance.EnableMockCommerce ? "MOCK" : "UNAVAILABLE";
            Label(canUse ? Tickets.CurrentTicketCount + " LEFT" + (Balance.EnableMockCommerce ? " / MOCK" : "") : serviceNote,
                overlay, 79, 818, 178, 28, 15, noteColor, align: TextAnchor.MiddleCenter);
            Label(serviceNote, overlay, 463, 818, 178, 28, 15, noteColor, align: TextAnchor.MiddleCenter);
            Label(Tickets.SyncState == TicketSyncState.Ready ? "MAXIMUM ONE REVIVE.\nEQUAL CHANCES FOR EVERYONE."
                : "SERVER TIME PENDING. NO TICKETS ISSUED.\nYOU CAN STILL USE A REWARDED AD.",
                overlay, 65, 940, 590, 90, 19, mint, align: TextAnchor.MiddleCenter);
        }
        async void UseTicket()
        {
            if (operationBusy || Run.State != RunState.ReviveChoice) return;
            operationBusy = true;
            bool used = await Tickets.TryConsumeAsync(runId);
            if (destroyed) return;
            if (used && Run.Revive()) { ClearOverlay(); Toast("WELCOME BACK"); }
            else { ShowRevive(); Toast("Ticket confirmation pending. Try again."); }
            operationBusy = false;
        }
        async void WatchAd()
        {
            if (operationBusy || !Run.BeginAd()) return;
            operationBusy = true;
            Modal("MOCK AD", "Prototype simulation — no real advertisement.\nWaiting for a verified reward callback…");
            MockAdOutcome outcome;
            try { outcome = await ads.ShowAsync(); } catch { outcome = MockAdOutcome.Unavailable; }
            if (destroyed) return;
            if (outcome == MockAdOutcome.Rewarded) { Run.Revive(); ClearOverlay(); }
            else
            {
                Run.CancelAd();
                if (outcome == MockAdOutcome.Unavailable && Balance.UnavailablePolicy == AdUnavailablePolicy.EndRun) { Run.End(); ShowGameOver(); }
                else { ShowRevive(); Toast(outcome == MockAdOutcome.Cancelled ? "Ad cancelled. No reward granted." : "Ad unavailable. You may retry or finish."); }
            }
            operationBusy = false;
        }
        void ShowGameOver()
        {
            if (!submitted) { ranking.Submit(Run.Score); submitted = true; }
            Modal("NICELY PLAYED.", "Keep the combo going.\nCome back for another perfect ten.");
            Label(Run.Score.ToString("N0"), overlay, 60, 480, 600, 110, 80, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label("TOTAL SCORE", overlay, 60, 595, 600, 40, 20, muted, align: TextAnchor.MiddleCenter);
            Label("MATCHES    " + Run.SuccessCount + "\nSURVIVED    " + Run.TotalSurvivalTime.ToString("0.0") + "s\n\nCHAPTER " + Run.Chapter + "    •    " + Run.TotalRemovedTiles + " TILES\nLOCAL BEST    " + ranking.BestScore.ToString("N0") + " PTS", overlay, 60, 670, 600, 230, 25, ink, align: TextAnchor.MiddleCenter);
            Button("PLAY AGAIN", overlay, 80, 960, 560, 80, ink, StartRun, paper);
            Button("HOME", overlay, 80, 1068, 560, 65, paper, ShowHome);
        }
        void ShowStore()
        {
            if (operationBusy) return;
            Modal("A DAILY SECOND\nCHANCE.", "AD-FREE REVIVE PASS\nNon-consumable • Reference price KRW 9,000");
            Label("2 tickets each day. No rollover.\nResets at 00:00 Asia/Seoul.\nOne revive per run, with or without the pass.\nNo time or score bonuses.", overlay, 65, 490, 590, 190, 25, ink, align: TextAnchor.MiddleCenter);
            Label((Balance.EnableMockCommerce ? "MOCK STORE — NO CHARGE\n" : "STORE NOT CONNECTED\n") + (Tickets.HasAdFreeRevivePurchase ? "Owned  •  Tickets " + Tickets.CurrentTicketCount + "/2" : "Not purchased") + "\n" + Tickets.SyncState + "  •  " + Tickets.LastResetDate, overlay, 65, 705, 590, 110, 21, muted, align: TextAnchor.MiddleCenter);
            Button(Tickets.HasAdFreeRevivePurchase ? "REFRESH TICKETS" : "SIMULATE VERIFIED PURCHASE", overlay, 80, 850, 560, 75, ink, () => Commerce(false), paper);
            Button("RESTORE PURCHASE", overlay, 80, 950, 560, 65, paper, () => Commerce(true));
            Button("BACK", overlay, 80, 1040, 560, 65, mint, ReturnFromMenu);
        }
        async void Commerce(bool restore)
        {
            if (operationBusy) return;
            operationBusy = true;
            Modal("VERIFYING…", "Confirming purchase and server date.\nNo tickets are issued before verification.");
            bool ok = restore ? await Tickets.RestoreAsync() : Tickets.HasAdFreeRevivePurchase ? await Tickets.RefreshAsync() : await Tickets.PurchaseAsync();
            if (destroyed) return;
            operationBusy = false; ShowStore();
            if (!ok) Toast("Verification pending. No tickets granted.");
        }
        void ShowLab()
        {
            if (operationBusy) return;
            if (Run != null && !IsHome) Run.Pause();
            Modal("PROTOTYPE LAB", "These controls simulate external services.\nThey are not production purchase or time verification.");
            Button("EXPIRE CURRENT LIFE", overlay, 80, 510, 560, 65, paper, () => { ClearOverlay(); Run.Resume(); Run.Tick(1000000); });
            Button("MOCK AD: " + Balance.AdOutcome, overlay, 80, 600, 560, 65, paper, () => { Balance.AdOutcome = (MockAdOutcome)(((int)Balance.AdOutcome + 1) % 3); ShowLab(); });
            Button("MOCK SERVER: " + (mockClock != null && mockClock.Online ? "ONLINE" : "OFFLINE"), overlay, 80, 690, 560, 65, paper, () => { if (mockClock != null) mockClock.Online = !mockClock.Online; RefreshTickets(); ShowLab(); });
            Button("ADVANCE MOCK SERVER +1 DAY", overlay, 80, 780, 560, 65, paper, () => { mockClock?.AdvanceDay(); RefreshTickets(); Toast("Mock server advanced. Refreshing tickets."); });
            Button("REVIVE PASS / MOCK STORE", overlay, 80, 870, 560, 65, mint, ShowStore);
            Button("BACK", overlay, 80, 1010, 560, 75, ink, ReturnFromMenu, paper);
        }

        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var r = Rect(name, parent, x, y, w, h); var image = r.gameObject.AddComponent<Image>(); image.color = color;
            if (name == "Background" || name == "Modal background") MintTheme.Apply(image, "MintGarden", false);
            else if (name == "Clock card" || name == "Modal card") MintTheme.Apply(image, "PanelDark");
            else if (name == "Time track" || name == "Combo track") MintTheme.Apply(image, "BarTrack");
            else if (name == "Time remaining" || name == "Combo remaining" || name == "Shadow") { image.sprite = JewelTheme.Get("Round"); image.type = Image.Type.Sliced; }
            else if (name == "Board well") { image.color = Color.clear; image.raycastTarget = false; }
            return image;
        }
        TMP_Text Label(string value, Transform parent, float x, float y, float w, float h, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var r = Rect("Text", parent, x, y, w, h); var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            UiTypography.Configure(text, size, color == ink ? paper : color == muted ? mint : color, align, true);
            text.text = value;
            return text;
        }
        void Button(string title, Transform parent, float x, float y, float w, float h, Color color, Action action, Color? textColor = null)
        {
            var image = Panel(title, parent, x, y, w, h, color);
            MintTheme.Apply(image, color == ink ? "PanelDark" : color == paper ? "PanelCream" : "ButtonMint");
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            var label = Label(title, image.transform, 12, 0, w - 24, h, 25, textColor ?? ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            label.color = textColor ?? (color == ink ? paper : ink);
            UiTypography.ApplyOutline(label);
        }
        void Logo(Transform parent, float x, float y, float w, float h, int size)
        {
            Label("THE TEN!", parent, x + 4, y + 6, w, h, size, ink).color = new Color32(18, 48, 34, 255);
            Label("THE TEN!", parent, x, y, w, h, size, paper);
        }
    }
}
