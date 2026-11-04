using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using Text = TMPro.TMP_Text;

namespace TheTen.Tests
{
    public sealed class PrototypeFlowTests
    {
        TheTenApp app;
        GameBalance balance;
        string prefix;
        bool background;
        [UnitySetUp] public IEnumerator Setup()
        {
            foreach (var existing in UnityEngine.Object.FindObjectsByType<TheTenApp>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(existing.gameObject);
            yield return null;
            background = Application.runInBackground; Application.runInBackground = true;
            prefix = "TheTen.Test." + Guid.NewGuid().ToString("N");
            balance = ScriptableObject.CreateInstance<GameBalance>(); balance.MockAdSeconds = .01f;
            var go = new GameObject("Prototype under test"); app = go.AddComponent<TheTenApp>();
            app.Balance = balance; app.SaveNamespace = prefix;
            yield return null;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if (app != null) UnityEngine.Object.Destroy(app.gameObject);
            UnityEngine.Object.Destroy(balance);
            PlayerPrefs.DeleteKey(prefix + ".MockStore.NonConsumable");
            PlayerPrefs.DeleteKey(prefix + ".Mock.Tickets.v15");
            PlayerPrefs.DeleteKey(prefix + ".Prototype.BestScore.v1"); PlayerPrefs.Save();
            Application.runInBackground = background;
            yield return null;
        }
        Button Button(string name) => app.GetComponentsInChildren<Button>().First(b => b.name == name);
        void Begin() { Button("LET'S PLAY  →").onClick.Invoke(); Assert.IsFalse(app.IsHome); }
        void Tap(int id)
        {
            var b = app.GetComponentsInChildren<Button>().First(t => t.transform.parent.name.StartsWith("Tile " + id + " •"));
            b.onClick.Invoke();
        }
        GameObject ClickScreen(Vector2 screen, int pointerId = -1, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = screen, pointerId = pointerId, button = button };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            var hit = hits[0].gameObject; pointer.pointerCurrentRaycast = hits[0];
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            Assert.IsNotNull(handler);
            var pressed = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler) ?? handler;
            ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
            return hit;
        }
        void ClickGraphic(Graphic graphic)
        {
            Canvas.ForceUpdateCanvases();
            var rect = graphic.rectTransform;
            Assert.AreSame(graphic.gameObject, ClickScreen(RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))));
        }
        void ClickTile(int id)
        {
            var face = app.GetComponentsInChildren<Button>().First(b => b.transform.parent.name.StartsWith("Tile " + id + " •"));
            ClickGraphic(face.GetComponent<Image>());
        }
        Vector2 BoardScreenPoint(float x, float y)
        {
            var board = app.GetComponentsInChildren<RectTransform>().Single(r => r.name == "Layered tiles");
            return RectTransformUtility.WorldToScreenPoint(null, board.TransformPoint(new Vector3(x, -y, 0)));
        }
        [UnityTest] public IEnumerator FailedMatchUpdatesTimerAndClearsVisualSelection()
        {
            Begin();
            var move = app.Run.Board.Available().Take(3).Select(t => t.Id).ToArray();
            app.Run.Board.Tiles[move[0]].Value = 0;
            app.Run.Board.Tiles[move[1]].Value = -1;
            app.Run.Board.Tiles[move[2]].Value = 2;
            app.SendMessage("RenderBoard"); yield return null;
            app.SendMessage("OnApplicationFocus", true);
            var failedFaces = move.Select(id => app.GetComponentsInChildren<Button>()
                .First(b => b.transform.parent.name.StartsWith("Tile " + id + " •")).GetComponent<Image>()).ToArray();
            var colors = failedFaces.Select(f => f.color).ToArray();
            var origins = failedFaces.Select(f => f.rectTransform.anchoredPosition).ToArray();
            var modelPositions = move.Select(id => new Vector2(app.Run.Board.Tiles[id].X, app.Run.Board.Tiles[id].Y)).ToArray();
            var timer = app.GetComponentsInChildren<Text>().Single(t => t.name == "Countdown");
            var timerColor = timer.color;
            ClickTile(move[0]); ClickTile(move[1]);
            Assert.AreEqual(2, app.Run.Selection.Count);
            double time = app.Run.RemainingTime, survival = app.Run.TotalSurvivalTime;
            ClickTile(move[2]);
            Assert.AreEqual(time - 3, app.Run.RemainingTime); Assert.AreEqual(survival, app.Run.TotalSurvivalTime);
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(36, app.Run.Board.Remaining); Assert.AreEqual(0, app.Run.Score);
            foreach (int id in move)
            {
                var face = app.GetComponentsInChildren<Button>().First(b => b.transform.parent.name.StartsWith("Tile " + id + " •"));
                Assert.IsFalse(face.GetComponent<Image>().sprite.name.StartsWith("TileSelected"));
                Assert.IsTrue(face.GetComponentsInChildren<Text>().Where(t => t.fontSize == 11).All(t => t.text == ""));
            }
            var labels = app.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Assert.Contains(app.Run.RemainingTime.ToString("0.0"), labels);
            Assert.Contains("NOT TEN   -3 SECONDS", labels);
            Assert.AreEqual("0", app.GetComponentsInChildren<Text>().Single(t => t.name == "Run score").text);
            Assert.AreEqual(TileVisual.MissColor, timer.color);
            Assert.IsTrue(failedFaces.All(f => f.color == TileVisual.MissColor));
            // Feedback does not lock input or turn a cancellation into another penalty.
            ClickTile(move[0]); Assert.AreEqual(1, app.Run.Selection.Count);
            ClickScreen(BoardScreenPoint(35, 500)); Assert.IsEmpty(app.Run.Selection);
            Assert.AreEqual(time - 3, app.Run.RemainingTime);
            float maxShake = 0, maxScale = 1, deadline = Time.realtimeSinceStartup + .25f;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                maxShake = Mathf.Max(maxShake, Mathf.Abs(failedFaces[0].rectTransform.anchoredPosition.x - origins[0].x));
                maxScale = Mathf.Max(maxScale, timer.rectTransform.localScale.x);
            }
            Assert.Greater(maxShake, .5f); Assert.LessOrEqual(maxShake, 5);
            Assert.Greater(maxScale, 1.03f); Assert.LessOrEqual(maxScale, 1.141f);
            yield return new WaitForSecondsRealtime(TileVisual.MissDuration);
            for (int i = 0; i < move.Length; i++)
            {
                Assert.AreEqual(colors[i], failedFaces[i].color, "Restore each special tile's original tint.");
                Assert.AreEqual(origins[i], failedFaces[i].rectTransform.anchoredPosition);
                Assert.AreEqual(modelPositions[i], new Vector2(app.Run.Board.Tiles[move[i]].X, app.Run.Board.Tiles[move[i]].Y));
            }
            Assert.AreEqual((Color32)timerColor, (Color32)timer.color); Assert.AreEqual(Vector3.one, timer.rectTransform.localScale);
            Assert.AreEqual(36, app.Run.Board.Remaining); Assert.AreEqual(0, app.Run.Score);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RepeatedMissRestoresLowTimeColorAndNewRunResetsPulse()
        {
            Begin();
            var move = app.Run.Board.Available().Take(3).Select(t => t.Id).ToArray();
            foreach (int id in move) app.Run.Board.Tiles[id].Value = 1;
            app.SendMessage("RenderBoard"); yield return null;
            app.SendMessage("OnApplicationFocus", true);
            var timer = app.GetComponentsInChildren<Text>().Single(t => t.name == "Countdown");
            var normalColor = timer.color;
            app.Run.Tick(app.Run.RemainingTime - 12);
            foreach (int id in move) ClickTile(id);
            yield return new WaitForSecondsRealtime(.12f);
            double beforeSecondMiss = app.Run.RemainingTime;
            foreach (int id in move) ClickTile(id);
            Assert.AreEqual(beforeSecondMiss - 3, app.Run.RemainingTime);
            Assert.AreEqual(TileVisual.MissColor, timer.color);
            app.Run.Pause();
            yield return new WaitForSecondsRealtime(TileVisual.MissDuration + .1f);
            Assert.AreEqual(new Color32(243, 203, 111, 255), (Color32)timer.color);
            Assert.AreEqual(Vector3.one, timer.rectTransform.localScale);
            app.Run.Resume();
            foreach (int id in move) ClickTile(id);
            Assert.AreEqual(TileVisual.MissColor, timer.color);
            app.StartRun();
            Assert.AreEqual(normalColor, timer.color); Assert.AreEqual(Vector3.one, timer.rectTransform.localScale);
            yield return null;
            Assert.IsTrue(app.GetComponentsInChildren<TileVisual>().All(v => v.GetComponentsInChildren<Image>()
                .Where(i => i.name == "Face").All(i => i.color == Color.white)));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EmptySpaceCancelsAllWhileRetapsAndMenuButtonsKeepTheirActions()
        {
            Begin(); yield return null;
            app.SendMessage("OnApplicationFocus", true);
            var move = app.Run.Board.FindMove();
            ClickTile(move[0]); ClickTile(move[1]);
            double time = app.Run.RemainingTime;
            ClickTile(move[0]); CollectionAssert.AreEqual(new[] { move[1] }, app.Run.Selection);
            ClickTile(move[0]); Assert.AreEqual(2, app.Run.Selection.Count);
            Assert.AreEqual("Background", ClickScreen(BoardScreenPoint(35, 500)).name);
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(time, app.Run.RemainingTime);
            Assert.AreEqual("0", app.GetComponentsInChildren<Text>().Single(t => t.name == "Run score").text);
            ClickTile(move[0]); ClickTile(move[1]);
            ClickScreen(BoardScreenPoint(680, 1100), pointerId: 42);
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(time, app.Run.RemainingTime);
            ClickTile(move[0]);
            ClickScreen(BoardScreenPoint(35, 500), button: PointerEventData.InputButton.Right);
            CollectionAssert.AreEqual(new[] { move[0] }, app.Run.Selection);
            ClickGraphic(Button("HOW TO PLAY").GetComponent<Image>());
            Assert.AreEqual(RunState.Paused, app.Run.State); Assert.AreEqual(1, app.Run.Selection.Count);
            yield return null; // Newly created menu graphics join the canvas before the next click.
            ClickScreen(BoardScreenPoint(35, 700));
            Assert.AreEqual(1, app.Run.Selection.Count);
            ClickGraphic(Button("GOT IT").GetComponent<Image>());
            Assert.AreEqual(RunState.Playing, app.Run.State); Assert.AreEqual(1, app.Run.Selection.Count);
            ClickScreen(BoardScreenPoint(35, 500));
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(time, app.Run.RemainingTime);
            Assert.AreEqual(36, app.Run.Board.Remaining); Assert.AreEqual(0, app.Run.Score);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BlockedSixTilesRearrangeAndContinueWithoutGameOver()
        {
            Begin();
            foreach (var tile in app.Run.Board.Tiles)
            {
                tile.Removed = tile.Id >= 9;
                tile.Layer = tile.Id < 3 ? 0 : 1 + (tile.Id - 3) / 2;
                tile.X = tile.Id < 3 ? 40 : 400;
                tile.Y = 450;
                tile.Value = tile.Id == 0 ? 2 : tile.Id == 1 ? 3 : tile.Id == 2 ? 5 : 9;
            }
            Tap(0); Tap(1); Tap(2);
            Assert.AreEqual(RunState.Shuffling, app.Run.State);
            Assert.AreEqual(2, app.Run.Board.Available().Count);
            int score = app.Run.Score, removed = app.Run.TotalRemovedTiles;
            yield return new WaitForSecondsRealtime(balance.TransitionSeconds + .2f);
            Assert.AreEqual(RunState.Playing, app.Run.State);
            Assert.AreEqual(6, app.Run.Board.Remaining);
            Assert.AreEqual(score, app.Run.Score); Assert.AreEqual(removed, app.Run.TotalRemovedTiles);
            Assert.AreEqual(1, app.Run.Chapter); Assert.IsFalse(app.Run.ReviveUsed);
            Assert.AreEqual(6, app.GetComponentsInChildren<TileVisual>().Length);
            var notice = app.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Shuffle notice");
            Assert.AreEqual("더 진행할 수 없어 셔플!", notice.GetComponentInChildren<Text>().text);
            Assert.IsFalse(notice.GetComponent<CanvasGroup>().blocksRaycasts);
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.IsFalse(app.GetComponentsInChildren<RectTransform>().Any(t => t.name == "Shuffle notice"));
            foreach (var move in app.Run.Board.GuaranteedPath.ToList()) foreach (int id in move) Tap(id);
            Assert.AreEqual(RunState.BoardTransition, app.Run.State);
            Assert.AreEqual(0, app.Run.Board.Remaining);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SpecialTilesHaveColorsBonusAndNonBlockingFireworks()
        {
            Begin();
            var move = app.Run.Board.GuaranteedPath[0];
            app.Run.Board.Tiles[move[0]].Value = 0;
            app.Run.Board.Tiles[move[1]].Value = 5;
            app.Run.Board.Tiles[move[2]].Value = BoardModel.Double;
            var negative = app.Run.Board.Tiles.First(t => !move.Contains(t.Id)); negative.Value = -1;
            app.SendMessage("RenderBoard"); yield return null;
            foreach (int id in new[] {move[0], move[2], negative.Id})
            {
                var face = app.GetComponentsInChildren<Button>().First(b => b.transform.parent.name.StartsWith("Tile " + id + " •"));
                Assert.AreEqual(Color.white, face.GetComponent<Image>().color);
                Assert.AreSame(JewelTheme.Tile(app.Run.Board.Tiles[id].Value), face.GetComponent<Image>().sprite);
            }
            foreach (int id in move) Tap(id);
            Assert.AreEqual(40, app.Run.Score);
            var effect = app.GetComponentsInChildren<ChapterFireworks>().Single();
            Assert.AreEqual("Special fireworks", effect.name);
            Assert.IsTrue(effect.GetComponentsInChildren<Image>().All(i => !i.raycastTarget));
            Assert.AreEqual("×1", app.GetComponentInChildren<ComboFeedback>().GetComponentInChildren<Text>().text);
            app.Run.Pause(); yield return new WaitForSecondsRealtime(2.2f);
            Assert.IsTrue(effect == null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RuinsHomeButtonsOpenMenusAndReturn()
        {
            Assert.IsTrue(app.IsHome);
            foreach (string sprite in new[] { "HomeRuinsMonument", "HomePlay", "HomeTrophy", "HomeStore", "HomeBook", "HomeGear", "HomeChart" })
                Assert.IsNotNull(MintTheme.Sprite(sprite), sprite);
            var preview = app.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Home preview tile ")).OrderBy(i => i.name).ToArray();
            Assert.AreEqual(3, preview.Length);
            var previewValues = preview.Select(i => int.Parse(i.GetComponentInChildren<Text>().text)).ToArray();
            Assert.AreEqual(10, previewValues.Sum());
            for (int i = 0; i < preview.Length; i++)
            {
                Assert.AreSame(JewelTheme.Tile(previewValues[i]), preview[i].sprite);
                Assert.IsFalse(preview[i].raycastTarget);
            }
            var backdrop = app.GetComponentsInChildren<Image>().Single(i => i.name == "Home ruins");
            Assert.AreSame(MintTheme.Sprite("HomeRuinsMonument"), backdrop.sprite);
            Assert.IsFalse(backdrop.transform.parent.GetComponentsInChildren<Image>().Any(i => i.name == "Home tile"));
            Assert.IsFalse(backdrop.transform.parent.GetComponentsInChildren<Text>().Any(t =>
                t.text.Contains("MAKE ROOM") || t.text.Contains("A LITTLE") || t.text.Contains("THREE TILES")));
            var intro = app.GetComponentsInChildren<LetterWave>().Single(w => w.name == "Home introduction");
            var letters = intro.GetComponentsInChildren<Text>();
            Assert.AreEqual(LetterWave.Message, string.Concat(letters.Select(t => t.text)));
            Assert.IsTrue(letters.All(t => t.fontSize == 36 && !t.raycastTarget));
            Assert.AreEqual(new Color32(246, 209, 95, 255), (Color32)letters[0].color);
            Assert.AreEqual(new Color32(241, 116, 104, 255), (Color32)letters[13].color);
            Assert.AreEqual(letters[13].color, letters[14].color);
            var initialScales = letters.Select(t => t.transform.localScale.x).ToArray();
            yield return new WaitForSecondsRealtime(.14f);
            Assert.IsTrue(letters.Select((t, i) => Mathf.Abs(t.transform.localScale.x - initialScales[i])).Any(delta => delta > .02f));
            Button("Rank button").onClick.Invoke();
            Assert.IsNotNull(Button("Rank tab 한국"));
            Button("BACK").onClick.Invoke();
            Button("REVIVE PASS / MOCK STORE").onClick.Invoke();
            Assert.IsNotNull(Button("RESTORE PURCHASE")); Button("BACK").onClick.Invoke();
            Button("HOW TO PLAY").onClick.Invoke(); Button("GOT IT").onClick.Invoke();
            Button("Home settings").onClick.Invoke();
            Button("MENU MOTION: ON").onClick.Invoke(); Button("BACK").onClick.Invoke();
            Assert.IsEmpty(app.GetComponentsInChildren<HomeTileMotion>());
            intro = app.GetComponentsInChildren<LetterWave>().Single(w => w.name == "Home introduction");
            Assert.IsFalse(intro.enabled);
            yield return new WaitForSecondsRealtime(.14f);
            Assert.IsTrue(intro.GetComponentsInChildren<Text>().All(t => t.transform.localScale == Vector3.one));
            Button("Home settings").onClick.Invoke(); Button("MENU MOTION: OFF").onClick.Invoke(); Button("BACK").onClick.Invoke();
            intro = app.GetComponentsInChildren<LetterWave>().Single(w => w.name == "Home introduction");
            Assert.IsTrue(intro.enabled);
            CollectionAssert.AreEqual(previewValues, app.GetComponentsInChildren<Image>()
                .Where(i => i.name.StartsWith("Home preview tile ")).OrderBy(i => i.name)
                .Select(i => int.Parse(i.GetComponentInChildren<Text>().text)).ToArray());
            Button("Home records").onClick.Invoke();
            Assert.IsTrue(app.GetComponentsInChildren<Text>().Any(t => t.text == "YOUR RECORD"));
            Button("BACK").onClick.Invoke();
            Assert.IsTrue(app.IsHome); Assert.IsNull(app.Run);
            Begin(); yield return null;
            Assert.IsFalse(app.GetComponentsInChildren<Image>().Any(i => i.name == "Home ruins"));
            Assert.IsFalse(app.GetComponentsInChildren<LetterWave>().Any(w => w.name == "Home introduction"));
            Assert.IsTrue(app.GetComponentInChildren<LetterWave>().GetComponentsInChildren<Text>().All(t => t.fontSize == 28));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HomeToMatchThroughRealUiBindings()
        {
            Assert.IsTrue(app.IsHome); Begin();
            var move = app.Run.Board.FindMove();
            foreach (int id in move) Tap(id);
            Assert.AreEqual(3, app.Run.TotalRemovedTiles);
            Assert.AreEqual(33, app.Run.Board.Remaining);
            Assert.AreEqual(1, app.Run.SuccessCount);
            Assert.AreEqual(10, app.Run.Score);
            yield return null;
            Assert.AreEqual("10", app.GetComponentsInChildren<Text>().Single(t => t.name == "Run score").text);
            var bar = app.GetComponentsInChildren<Image>().First(i => i.name == "Combo remaining");
            var timer = app.GetComponentsInChildren<Image>().First(i => i.name == "Time remaining");
            Assert.Greater(bar.rectTransform.rect.width, 0);
            Assert.Less(bar.rectTransform.anchoredPosition.y, timer.rectTransform.anchoredPosition.y);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NumericScoreAndThickBarsTrackRemainingTimeAndRecovery()
        {
            Begin(); yield return null;
            app.SendMessage("OnApplicationFocus", true); app.StartRun();
            var score = app.GetComponentsInChildren<Text>().Single(t => t.name == "Run score");
            var timer = app.GetComponentsInChildren<Image>().Single(i => i.name == "Time remaining");
            var combo = app.GetComponentsInChildren<Image>().Single(i => i.name == "Combo remaining");
            Assert.AreEqual("0", score.text); Assert.AreEqual(TextAlignmentOptions.Center, score.alignment);
            Assert.AreEqual(new Color32(246, 209, 95, 255), (Color32)score.color);
            Assert.GreaterOrEqual(score.fontSize, 48);
            Assert.AreEqual(520, score.rectTransform.anchoredPosition.x + score.rectTransform.rect.width * (.5f - score.rectTransform.pivot.x));
            Assert.IsFalse(app.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("SCORE")
                || t.text.Contains("MATCH TO START") || t.text.Contains("=   10") || t.text.Contains("→   10")));
            Assert.AreEqual(22, timer.rectTransform.rect.height);
            Assert.AreEqual(14, combo.rectTransform.rect.height);
            Assert.AreEqual((Color32)ComboFeedback.TierColor(app.Run.ComboMultiplier), (Color32)combo.color);
            Assert.AreEqual(624, timer.rectTransform.rect.width);
            var levels = new[] { 24.7, 20, 10, 5.2 };
            var colors = new[] { new Color32(119, 198, 104, 255), new Color32(244, 200, 79, 255),
                new Color32(238, 101, 86, 255), new Color32(238, 101, 86, 255) };
            for (int i = 0; i < levels.Length; i++)
            {
                app.Run.Tick(app.Run.RemainingTime - levels[i]); app.SendMessage("UpdateHud");
                Assert.AreEqual(colors[i], (Color32)timer.color);
                Assert.AreEqual(624 * levels[i] / balance.InitialTime, timer.rectTransform.rect.width, .01);
            }
            // Successful matches can earn time back across both warning thresholds.
            var path = app.Run.Board.GuaranteedPath;
            for (int i = 0; i < 5; i++)
            {
                foreach (int id in path[i]) Tap(id);
                if (i == 1) Assert.AreEqual(colors[1], (Color32)timer.color);
            }
            Assert.AreEqual(colors[0], (Color32)timer.color);
            Assert.AreEqual(app.Run.Score.ToString("N0"), score.text);
            Assert.AreEqual((Color32)ComboFeedback.TierColor(app.Run.ComboMultiplier), (Color32)combo.color);
            app.Run.Tick(1000); app.SendMessage("UpdateHud");
            Assert.AreEqual(0, timer.rectTransform.rect.width); Assert.AreEqual(colors[3], (Color32)timer.color);
            Assert.IsTrue(app.Run.Revive()); app.SendMessage("UpdateHud");
            Assert.AreEqual(624, timer.rectTransform.rect.width); Assert.AreEqual(colors[0], (Color32)timer.color);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ChapterClearWaitsForConfirmationBeforeStartingNextBoard()
        {
            Begin();
            var header = app.GetComponentsInChildren<Text>().Single(t => t.text == "CHAPTER 01");
            Assert.AreEqual(TextAlignmentOptions.Center, header.alignment);
            Assert.AreEqual(360, header.rectTransform.anchoredPosition.x + header.rectTransform.rect.width / 2);
            Assert.IsFalse(app.GetComponentsInChildren<Text>().Any(t => t.text == "THE TEN!"));
            foreach (var move in app.Run.Board.GuaranteedPath.ToList()) foreach (int id in move) Tap(id);
            Assert.AreEqual(1, app.GetComponentsInChildren<ChapterFireworks>().Length);
            Assert.IsTrue(app.GetComponentInChildren<ChapterFireworks>().GetComponentsInChildren<Image>().All(i => !i.raycastTarget));
            double time = app.Run.RemainingTime, combo = app.Run.ComboRemaining;
            int score = app.Run.Score;
            var oldBoard = app.Run.Board;
            yield return new WaitForSecondsRealtime(balance.TransitionSeconds + .2f);
            var confirm = Button("CONFIRM");
            Assert.IsNotNull(confirm.transform.parent.GetComponentInChildren<ChapterFireworks>());
            Assert.IsTrue(app.GetComponentsInChildren<Text>().Any(t => t.text == "CLEAR!"));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(RunState.BoardTransition, app.Run.State);
            Assert.AreEqual(1, app.Run.Chapter); Assert.AreEqual(0, app.Run.Board.Remaining);
            Assert.AreEqual(time, app.Run.RemainingTime); Assert.AreEqual(combo, app.Run.ComboRemaining);
            // Editor focus can change during a test run; confirmation resumes only in foreground.
            app.SendMessage("OnApplicationFocus", true);
            confirm.onClick.Invoke(); confirm.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 2;
            while (app.Run.State == RunState.BoardTransition && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.AreEqual(2, app.Run.Chapter); Assert.AreEqual(RunState.Playing, app.Run.State);
            Assert.AreNotSame(oldBoard, app.Run.Board); Assert.AreEqual(36, app.Run.Board.Remaining);
            Assert.AreEqual(score, app.Run.Score);
            Assert.IsTrue(app.GetComponentsInChildren<Text>().Any(t => t.text == "CHAPTER 02"));
            Assert.IsFalse(app.GetComponentsInChildren<Button>().Any(b => b.name == "CONFIRM"));
            Assert.IsEmpty(app.GetComponentsInChildren<ChapterFireworks>());
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PressShrinksReleasePopsAndExitCancelsWithoutSelecting()
        {
            Begin(); int id = app.Run.Board.FindMove()[0];
            var button = app.GetComponentsInChildren<Button>().First(b => b.transform.parent.name.StartsWith("Tile " + id + " •"));
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = 12 };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.Less(rect.localScale.x, .94f); Assert.IsEmpty(app.Run.Selection);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.Contains(id, app.Run.Selection);
            float peak = 0, deadline = Time.realtimeSinceStartup + .4f;
            while (Time.realtimeSinceStartup < deadline) { peak = Mathf.Max(peak, rect.localScale.x); yield return null; }
            Assert.Greater(peak, 1.23f); Assert.AreEqual(1.12f, rect.localScale.x, .01f);
            Tap(id); yield return new WaitForSecondsRealtime(.35f);
            Assert.AreEqual(1, rect.localScale.x, .01f);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.1f);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(1, rect.localScale.x, .01f);
            Assert.AreEqual(0, app.Run.Score);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ComboParticlePrefabsEscalateAndReuseInstancesWithoutBlockingInput()
        {
            Begin();
            var feedback = app.GetComponentInChildren<ComboFeedback>();
            var effects = feedback.GetComponentsInChildren<ComboParticleGraphic>();
            Assert.AreEqual(5, effects.Length);
            double[] factors = { 1.5, 2, 2.5, 4, 5 };
            int previousCount = 0;
            for (int i = 0; i < factors.Length; i++)
            {
                Assert.AreEqual(i + 1, ComboFeedback.EffectStage(factors[i]));
                feedback.PlayMatch(factors[i]);
                yield return new WaitForSecondsRealtime(.22f);
                var active = effects.Single(e => e.Particles.particleCount > 0);
                Assert.IsTrue(active.name.StartsWith("ComboStage" + (i + 1)));
                Assert.Greater(active.Particles.particleCount, previousCount);
                previousCount = active.Particles.particleCount;
                Canvas.ForceUpdateCanvases();
                Assert.Greater(active.canvasRenderer.GetMesh().vertexCount, 0);
                Assert.IsFalse(active.raycastTarget);
            }
            Assert.AreEqual(3, ComboFeedback.EffectStage(3));
            feedback.PlayMatch(5); yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(5, feedback.GetComponentsInChildren<ComboParticleGraphic>().Length);
            Assert.IsTrue(effects.Any(e => e.Particles.particleCount > 0));
            feedback.Hide();
            Assert.IsTrue(effects.All(e => e.Particles.particleCount == 0));
            feedback.PlayMatch(1); yield return null;
            Assert.IsTrue(effects.All(e => e.Particles.particleCount == 0));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator MatchedTilesEmitThreeComboBurstsAndReuseThem()
        {
            Begin();
            var pool = app.GetComponentInChildren<TileComboParticles>();
            var move = app.Run.Board.FindMove();
            foreach (int id in move) Tap(id);
            yield return new WaitForSecondsRealtime(.08f);
            var effects = pool.GetComponentsInChildren<ComboParticleGraphic>();
            Assert.AreEqual(3, effects.Length);
            Assert.IsTrue(effects.All(e => e.Particles.particleCount > 0 && !e.raycastTarget));
            Assert.AreEqual(3, effects.Select(e => ((RectTransform)e.transform).anchoredPosition).Distinct().Count());
            foreach (int id in app.Run.Board.FindMove()) Tap(id);
            yield return new WaitForSecondsRealtime(.08f);
            CollectionAssert.AreEquivalent(effects, pool.GetComponentsInChildren<ComboParticleGraphic>());
            Assert.IsTrue(app.GetComponentInChildren<ComboFeedback>().GetComponentsInChildren<ComboParticleGraphic>().Any(e => e.Particles.particleCount > 0));
            app.StartRun(); yield return null;
            Assert.IsTrue(effects.All(e => e.Particles.particleCount == 0));
            Assert.AreEqual(36, app.Run.Board.Remaining);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SelectionLiftsAndReturnsWithoutMovingBoardGeometry()
        {
            Begin(); int id = app.Run.Board.FindMove()[0];
            var tile = app.Run.Board.Tiles[id]; var modelPosition = (tile.X, tile.Y);
            var face = app.GetComponentsInChildren<Button>().First(b => b.transform.parent.name.StartsWith("Tile " + id + " •"));
            var rect = (RectTransform)face.transform; var origin = rect.anchoredPosition;
            Tap(id); yield return new WaitForSecondsRealtime(.18f);
            Assert.Greater(rect.anchoredPosition.y, origin.y + 8);
            Assert.AreEqual(modelPosition, (tile.X, tile.Y));
            Tap(id); yield return new WaitForSecondsRealtime(.18f);
            Assert.AreEqual(origin.y, rect.anchoredPosition.y, .01f);
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(0, app.Run.Score);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MatchSparklesRevealExistingTilesAndAllowImmediateNextSelection()
        {
            Begin(); var path = app.Run.Board.GuaranteedPath;
            var initiallyBlocked = app.Run.Board.Tiles.Where(t => !app.Run.Board.IsAvailable(t.Id)).Select(t => t.Id).ToArray();
            var original = app.GetComponentsInChildren<TileVisual>().ToArray();
            foreach (int id in path[0]) Tap(id);
            foreach (int id in path[1]) Tap(id);
            Assert.AreEqual(25, app.Run.Score);
            var clearing = original.Where(v => v.IsClearing).ToArray();
            Assert.AreEqual(6, clearing.Length);
            Assert.IsTrue(clearing.All(v => !v.GetComponent<CanvasGroup>().blocksRaycasts));
            Assert.IsTrue(app.GetComponentsInChildren<Image>().Any(i => i.name.StartsWith("Pixel sparkle")));
            Assert.IsTrue(app.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Pixel sparkle")).All(i => !i.raycastTarget));
            int revealed = initiallyBlocked.First(id => app.Run.Board.IsAvailable(id));
            var lower = original.First(v => v.name.StartsWith("Tile " + revealed + " •"));
            Assert.IsTrue(lower.IsRevealing);
            Tap(revealed); Assert.Contains(revealed, app.Run.Selection);
            double combo = app.Run.ComboRemaining;
            yield return new WaitForSecondsRealtime(.18f);
            Assert.Less(app.Run.ComboRemaining, combo); // No artificial timer pause for presentation.
            Assert.IsTrue(clearing.All(v => v.GetComponent<CanvasGroup>().alpha < 1));
            yield return new WaitForSecondsRealtime(.42f);
            Assert.IsTrue(clearing.All(v => v == null));
            Assert.IsNotNull(lower); Assert.IsFalse(lower.IsRevealing);
            Assert.IsFalse(app.GetComponentsInChildren<Image>().Any(i => i.name.StartsWith("Pixel sparkle")));
            Assert.AreEqual(30, app.Run.Board.Remaining); Assert.AreEqual(25, app.Run.Score);
            app.StartRun(); yield return null;
            Assert.AreEqual(36, app.GetComponentsInChildren<TileVisual>().Length);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MintAtlasAndSpatialRaycastsMatchVisibleUpperTile()
        {
            Begin(); yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsNotNull(JewelTheme.Font);
            Assert.IsNotNull(Resources.Load<UnityEngine.U2D.SpriteAtlas>("Atlas/CasualMatte/CasualMatte"));
            foreach (int value in new[] { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, BoardModel.Double })
                Assert.IsNotNull(JewelTheme.Tile(value));
            Assert.IsFalse(app.GetComponentsInChildren<Text>().Any(t => t.text == "FREE TILES" || t.text == "ALWAYS AVAILABLE"));
            Assert.IsFalse(app.GetComponentsInChildren<Image>().Any(i => i.name == "Free tray"));
            var well = app.GetComponentsInChildren<Image>().First(i => i.name == "Board well");
            Assert.Greater(well.rectTransform.rect.height, 600);
            var upper = app.Run.Board.Tiles.First(t => app.Run.Board.IsAvailable(t.Id)
                && app.Run.Board.Tiles.Any(l => BoardModel.Covers(t, l)));
            var lower = app.Run.Board.Tiles.First(l => BoardModel.Covers(upper, l));
            Assert.AreEqual(SelectionResult.Ignored, app.Run.Select(lower.Id));
            var face = app.GetComponentsInChildren<Button>().First(b => b.transform.parent.name.StartsWith("Tile " + upper.Id + " •"));
            Assert.IsNotNull(face.GetComponent<Image>().sprite);
            var boardRoot = face.transform.parent.parent;
            float x = (Math.Max(upper.X, lower.X) + Math.Min(upper.X, lower.X) + BoardModel.TileWidth) / 2;
            float y = (Math.Max(upper.Y, lower.Y) + Math.Min(upper.Y, lower.Y) + BoardModel.TileHeight) / 2;
            var screen = RectTransformUtility.WorldToScreenPoint(null, boardRoot.TransformPoint(new Vector3(x, -y, 0)));
            var pointer = new PointerEventData(EventSystem.current) { position = screen };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits); Assert.AreSame(face.gameObject, hits[0].gameObject);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ComboBadgeChangesTierPulsesAndResetsWithoutBlockingInput()
        {
            Begin(); app.SendMessage("OnApplicationFocus", true);
            var path = app.Run.Board.GuaranteedPath;
            var feedback = app.GetComponentInChildren<ComboFeedback>();
            var badge = (RectTransform)feedback.transform;
            var face = feedback.GetComponentInChildren<RoundedBadgeGraphic>();
            Assert.IsNotNull(face.GetComponent<CanvasRenderer>());
            var caption = feedback.GetComponentInChildren<Text>();
            var score = app.GetComponentsInChildren<Text>().Single(t => t.name == "Run score");
            var bar = app.GetComponentsInChildren<Image>().Single(i => i.name == "Combo remaining");
            var track = app.GetComponentsInChildren<Image>().Single(i => i.name == "Combo track");
            Assert.Greater(badge.GetSiblingIndex(), track.transform.GetSiblingIndex());
            Assert.Greater(badge.GetSiblingIndex(), bar.transform.GetSiblingIndex());
            float trackCenter = track.rectTransform.anchoredPosition.y - track.rectTransform.rect.height / 2;
            Assert.AreEqual(trackCenter, badge.anchoredPosition.y, .01f);
            Assert.AreEqual(trackCenter, bar.rectTransform.anchoredPosition.y, .01f);
            var timeTrack = app.GetComponentsInChildren<Image>().Single(i => i.name == "Time track");
            float timeBottom = timeTrack.rectTransform.anchoredPosition.y - timeTrack.rectTransform.rect.height;
            Assert.Less(badge.anchoredPosition.y + badge.rect.height * 1.16f / 2, timeBottom,
                "The badge must clear the time bar even at peak scale.");
            var factors = new[] { 1d, 1.5, 2, 2.5, 3, 4, 5 };
            var expected = new[] { new Color32(245, 238, 205, 255), new Color32(245, 238, 205, 255),
                new Color32(119, 198, 104, 255), new Color32(119, 198, 104, 255),
                new Color32(112, 184, 255, 255), new Color32(246, 209, 95, 255), new Color32(238, 101, 86, 255) };
            for (int i = 0; i < factors.Length; i++)
            {
                foreach (int id in path[i]) Tap(id);
                Assert.AreEqual(factors[i], app.Run.ComboMultiplier);
                Assert.AreEqual(expected[i], (Color32)face.color);
                Assert.AreEqual(i == 6 ? "×5!!" : "×" + factors[i].ToString("0.#"), caption.text);
                Assert.IsTrue(feedback.GetComponent<ScalePulse>().IsPlaying);
                Assert.IsTrue(score.GetComponent<ScalePulse>().IsPlaying);
                yield return new WaitForSecondsRealtime(.09f);
                Assert.Greater(face.canvasRenderer.GetMesh().vertexCount, 0, "The rounded face must render, not only its caption.");
                Assert.Greater(badge.localScale.x, 1.04f); Assert.LessOrEqual(badge.localScale.x, 1.161f);
                Assert.Greater(score.rectTransform.localScale.x, 1.03f); Assert.LessOrEqual(score.rectTransform.localScale.x, 1.141f);
            }
            Assert.AreEqual(190, app.Run.Score); Assert.AreEqual("190", score.text);
            Assert.AreEqual(1, app.GetComponentsInChildren<ComboFeedback>().Length);
            Assert.IsFalse(feedback.GetComponent<CanvasGroup>().blocksRaycasts);
            Assert.IsTrue(feedback.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget));
            Assert.AreEqual((Color32)ComboFeedback.TierColor(app.Run.ComboMultiplier), (Color32)bar.color);
            app.Run.Pause(); yield return new WaitForSecondsRealtime(ScalePulse.Duration + .1f);
            Assert.AreEqual(Vector3.one, badge.localScale); Assert.AreEqual(Vector3.one, score.rectTransform.localScale);
            Assert.AreEqual("×5!!", caption.text); Assert.AreEqual(1, feedback.GetComponent<CanvasGroup>().alpha);
            app.Run.Resume(); app.Run.Tick(balance.ComboWindow + .01); app.SendMessage("UpdateHud");
            Assert.AreEqual("×1", caption.text); Assert.AreEqual(expected[0], (Color32)face.color);
            Assert.IsFalse(feedback.GetComponent<ScalePulse>().IsPlaying);
            Assert.IsFalse(score.GetComponent<ScalePulse>().IsPlaying);
            app.StartRun();
            Assert.AreEqual("0", score.text); Assert.AreEqual("×1", caption.text);
            Assert.AreEqual(Vector3.one, score.rectTransform.localScale);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator IntroWaveAndLargeBestScorePreserveSelectionWithoutOrderNumbers()
        {
            Begin(); app.SendMessage("OnApplicationFocus", true);
            var intro = app.GetComponentInChildren<LetterWave>();
            var letters = intro.GetComponentsInChildren<Text>();
            Assert.AreEqual(LetterWave.Message, string.Concat(letters.Select(t => t.text)));
            Assert.AreEqual(new Color32(246, 209, 95, 255), (Color32)letters[0].color);
            Assert.AreEqual(new Color32(241, 116, 104, 255), (Color32)letters[13].color);
            Assert.AreEqual(letters[13].color, letters[14].color);
            Assert.IsTrue(letters.All(t => !t.raycastTarget));
            var initial = letters.Select(t => t.transform.localScale.x).ToArray();
            yield return new WaitForSecondsRealtime(.14f);
            Assert.IsTrue(letters.Select((t, i) => Mathf.Abs(t.transform.localScale.x - initial[i])).Any(change => change > .02f));
            Assert.IsTrue(letters.All(t => t.transform.localScale.x >= 1 && t.transform.localScale.x <= 1.221f));
            Assert.IsFalse(app.GetComponentsInChildren<Text>().Any(t => t.text.Contains("REMOVED")));
            var best = app.GetComponentsInChildren<Text>().Single(t => t.name == "Best score");
            Assert.AreEqual(32, best.fontSize);
            Assert.AreEqual(TextAlignmentOptions.Center, best.alignment);
            var move = app.Run.Board.FindMove();
            ClickTile(move[0]); ClickTile(move[1]);
            CollectionAssert.AreEqual(new[] { move[0], move[1] }, app.Run.Selection);
            foreach (int id in move.Take(2))
            {
                var tile = app.GetComponentsInChildren<TileVisual>().Single(v => v.name.StartsWith("Tile " + id + " •"));
                var labels = tile.GetComponentsInChildren<Text>();
                Assert.AreEqual(1, labels.Length); Assert.AreEqual(app.Run.Board.Tiles[id].Label, labels[0].text);
            }
            ClickTile(move[2]);
            Assert.AreEqual("BEST 10 PTS", best.text);
            Assert.IsEmpty(app.Run.Selection);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator JewelTypographyRendersLatinSymbolsAndKoreanFallback()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-Bold SDF");
            var outline = Resources.Load<Material>("Fonts/NotoSans-Outline");
            var tileOutline = Resources.Load<Material>("Fonts/NotoSans-TileOutline");
            Assert.IsNotNull(font); Assert.IsNotNull(outline); Assert.IsNotNull(tileOutline);
            Assert.AreEqual("Noto Sans", font.faceInfo.familyName);
            Assert.Greater(font.fallbackFontAssetTable[0].material.GetFloat("_WeightNormal"), 0,
                "The regular Korean fallback needs enough face weight to remain visible inside the outline.");
            Assert.Greater(outline.GetFloat("_OutlineWidth"), 0);
            Assert.Greater(tileOutline.GetFloat("_OutlineWidth"), 0);
            Assert.IsTrue(outline.IsKeywordEnabled("OUTLINE_ON"));
            Assert.IsTrue(font.HasCharacters("0123456789×−→", out uint[] missing, true));
            Assert.IsEmpty(app.GetComponentsInChildren<UnityEngine.UI.Text>(true));
            Assert.IsTrue(app.GetComponentsInChildren<Text>().All(t => t.font == JewelTheme.Font && !t.raycastTarget));
            var background = app.GetComponentsInChildren<Image>().Single(i => i.name == "Home ruins");
            Assert.AreEqual("Background", background.sprite.texture.name);
            Assert.AreEqual(FilterMode.Bilinear, background.sprite.texture.filterMode);
            Assert.AreEqual(FilterMode.Bilinear, font.atlasTexture.filterMode);
            Button("Rank button").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var korean = app.GetComponentsInChildren<Text>().Single(t => t.text == "전 세계 랭킹");
            korean.ForceMeshUpdate();
            Assert.IsTrue(korean.textInfo.characterInfo.Take(korean.textInfo.characterCount)
                .Where(c => c.character >= 0xAC00).All(c => c.fontAsset.faceInfo.familyName.Contains("Noto Sans CJK")));
            Assert.Greater(korean.textInfo.characterCount, 0);
            Button("BACK").onClick.Invoke();
            Begin(); yield return null;
            var number = app.GetComponentInChildren<TileVisual>().GetComponentInChildren<Text>();
            Assert.AreSame(JewelTheme.Font.material, number.fontSharedMaterial);
            Assert.AreSame(JewelTheme.Font.material, app.GetComponentsInChildren<Text>().Single(t => t.name == "Run score").fontSharedMaterial);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ReviveIconRowPreservesRunAcrossStoreAndFinishesThroughFlag()
        {
            Begin();
            foreach (int id in app.Run.Board.FindMove()) Tap(id);
            app.Run.Tick(1000); yield return null;
            yield return null; // Update creates the modal; its graphics enter the raycast list on the next frame.
            var run = app.Run; var board = run.Board; int score = run.Score;
            var watch = Button("WATCH REWARDED AD / MOCK");
            var finish = Button("FINISH RUN");
            var pass = Button("REVIVE PASS / MOCK STORE");
            var modal = finish.transform.parent;
            Assert.AreEqual(3, modal.GetComponentsInChildren<Button>().Length);
            var rects = new[] { watch, finish, pass }.Select(b => (RectTransform)b.transform).ToArray();
            foreach (var rect in rects)
            {
                Assert.AreEqual(rect.rect.width, rect.rect.height);
                Assert.GreaterOrEqual(rect.rect.width, 120);
                Assert.AreEqual(rects[0].anchoredPosition.y, rect.anchoredPosition.y);
            }
            Assert.Less(rects[0].anchoredPosition.x + rects[0].rect.width, rects[1].anchoredPosition.x);
            Assert.Less(rects[1].anchoredPosition.x + rects[1].rect.width, rects[2].anchoredPosition.x);
            foreach (string sprite in new[] { "ReviveAd", "ReviveFinish", "ReviveTicket", "RevivePrimaryFrame" })
                Assert.IsNotNull(MintTheme.Sprite(sprite), sprite);
            var labels = modal.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Assert.Contains("WATCH AD", labels); Assert.Contains("FINISH", labels); Assert.Contains("REVIVE PASS", labels);
            Assert.IsFalse(labels.Any(t => t.ToLowerInvariant().Contains("survival clock is paused")));
            Assert.AreEqual(2, labels.Count(t => t == "MOCK"));
            ClickGraphic(pass.GetComponent<Image>()); yield return null;
            Assert.IsNotNull(Button("RESTORE PURCHASE"));
            ClickGraphic(Button("BACK").GetComponent<Image>()); yield return null;
            Assert.AreSame(run, app.Run); Assert.AreSame(board, app.Run.Board);
            Assert.AreEqual(score, app.Run.Score); Assert.AreEqual(RunState.ReviveChoice, app.Run.State);
            Assert.IsFalse(app.Run.ReviveUsed);
            ClickGraphic(Button("FINISH RUN").GetComponent<Image>());
            Assert.AreEqual(RunState.GameOver, app.Run.State); Assert.AreEqual(score, app.Run.Score);
            Assert.IsNotNull(Button("PLAY AGAIN"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RewardedAdRevivesOnceAndSecondDeathEndsRun()
        {
            Begin(); app.Run.Tick(1000); yield return null;
            yield return null;
            Assert.AreEqual(RunState.ReviveChoice, app.Run.State);
            ClickGraphic(Button("WATCH REWARDED AD / MOCK").GetComponent<Image>());
            Assert.AreEqual(RunState.AdPlaying, app.Run.State);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!app.Run.ReviveUsed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(app.Run.ReviveUsed); Assert.AreEqual(RunState.Playing, app.Run.State);
            app.Run.Tick(1000); yield return null;
            Assert.AreEqual(RunState.GameOver, app.Run.State);
            Assert.AreEqual(60, app.Run.TotalSurvivalTime, .0001);
            Assert.IsNotNull(Button("PLAY AGAIN"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CancelledAndUnavailableAdsDoNotRevive()
        {
            Begin(); app.Run.Tick(1000); yield return null;
            foreach (var outcome in new[] { MockAdOutcome.Cancelled, MockAdOutcome.Unavailable })
            {
                balance.AdOutcome = outcome;
                Button("WATCH REWARDED AD / MOCK").onClick.Invoke();
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(RunState.ReviveChoice, app.Run.State); Assert.IsFalse(app.Run.ReviveUsed);
                Assert.AreEqual(30, app.Run.TotalSurvivalTime, .0001);
            }
            Button("FINISH RUN").onClick.Invoke();
            Assert.AreEqual(RunState.GameOver, app.Run.State);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator VerifiedTicketUiConsumesOneAndSkipsAd()
        {
            var purchase = app.Tickets.PurchaseAsync(); yield return new WaitUntil(() => purchase.IsCompleted);
            Assert.IsTrue(purchase.Result); Begin(); app.Run.Tick(1000); yield return null;
            yield return null;
            var ticket = Button("USE REVIVE TICKET  (2)");
            Assert.IsTrue(ticket.transform.parent.GetComponentsInChildren<Text>().Any(t => t.text == "USE TICKET"));
            Assert.IsTrue(ticket.transform.parent.GetComponentsInChildren<Text>().Any(t => t.text == "2 LEFT / MOCK"));
            Assert.IsTrue(ticket.GetComponentsInChildren<Image>().Any(i => i.sprite != null && i.sprite.name.StartsWith("ReviveTicket")));
            ClickGraphic(ticket.GetComponent<Image>());
            yield return null;
            Assert.IsTrue(app.Run.ReviveUsed); Assert.AreEqual(RunState.Playing, app.Run.State);
            Assert.AreEqual(1, app.Tickets.CurrentTicketCount);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SavedBalanceSurvivesManagerRecreationAndRestore()
        {
            var purchase = app.Tickets.PurchaseAsync(); yield return new WaitUntil(() => purchase.IsCompleted);
            var consume = app.Tickets.TryConsumeAsync("restart-test"); yield return new WaitUntil(() => consume.IsCompleted);
            Assert.IsTrue(consume.Result);
            var restarted = new ReviveTicketManager(new MockServerClock(), new PlayerPrefsTicketStorage(prefix + ".Mock.Tickets.v15"), new MockPurchaseGateway(prefix));
            Assert.AreEqual(TicketSyncState.RecheckRequired, restarted.SyncState);
            var restore = restarted.RestoreAsync(); yield return new WaitUntil(() => restore.IsCompleted);
            Assert.IsTrue(restore.Result); Assert.AreEqual(1, restarted.CurrentTicketCount);
            var duplicate = restarted.TryConsumeAsync("restart-test"); yield return new WaitUntil(() => duplicate.IsCompleted);
            Assert.IsFalse(duplicate.Result); Assert.AreEqual(1, restarted.CurrentTicketCount);
        }
    }
}

