using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
            foreach (int id in move) app.Run.Board.Tiles[id].Value = 1;
            app.SendMessage("RenderBoard"); yield return null;
            app.SendMessage("OnApplicationFocus", true);
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
            Assert.Contains("·   +   ·   +   ·   =   10", labels);
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
            Assert.IsTrue(app.GetComponentsInChildren<Text>().Any(t => t.text == "·   +   ·   +   ·   =   10"));
            ClickTile(move[0]); ClickTile(move[1]);
            ClickScreen(BoardScreenPoint(710, 1100), pointerId: 42);
            Assert.IsEmpty(app.Run.Selection); Assert.AreEqual(time, app.Run.RemainingTime);
            ClickTile(move[0]);
            ClickScreen(BoardScreenPoint(35, 500), button: PointerEventData.InputButton.Right);
            CollectionAssert.AreEqual(new[] { move[0] }, app.Run.Selection);
            ClickGraphic(Button("HOW TO PLAY").GetComponent<Image>());
            Assert.AreEqual(RunState.Paused, app.Run.State); Assert.AreEqual(1, app.Run.Selection.Count);
            yield return null; // Newly created menu graphics join the canvas before the next click.
            ClickScreen(BoardScreenPoint(10, 700));
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
                Assert.AreEqual(TileVisual.TileColor(app.Run.Board.Tiles[id].Value), face.GetComponent<Image>().color);
            }
            foreach (int id in move) Tap(id);
            Assert.AreEqual(40, app.Run.Score);
            var effect = app.GetComponentsInChildren<ChapterFireworks>().Single();
            Assert.AreEqual("Special fireworks", effect.name);
            Assert.IsTrue(effect.GetComponentsInChildren<Image>().All(i => !i.raycastTarget));
            Assert.IsTrue(app.GetComponentInChildren<ComboFeedback>().GetComponentsInChildren<Text>().Any(t => t.text.Contains("BONUS ×4")));
            app.Run.Pause(); yield return new WaitForSecondsRealtime(2.2f);
            Assert.IsTrue(effect == null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RuinsHomeButtonsOpenMenusAndReturn()
        {
            Assert.IsTrue(app.IsHome);
            foreach (string sprite in new[] { "HomeRuins", "HomePlay", "HomeTrophy", "HomeStore", "HomeBook", "HomeGear", "HomeChart" })
                Assert.IsNotNull(MintTheme.Sprite(sprite), sprite);
            Button("Rank button").onClick.Invoke();
            Assert.IsNotNull(Button("Rank tab 한국"));
            Button("BACK").onClick.Invoke();
            Button("REVIVE PASS / MOCK STORE").onClick.Invoke();
            Assert.IsNotNull(Button("RESTORE PURCHASE")); Button("BACK").onClick.Invoke();
            Button("HOW TO PLAY").onClick.Invoke(); Button("GOT IT").onClick.Invoke();
            Button("Home settings").onClick.Invoke();
            Button("MENU MOTION: ON").onClick.Invoke(); Button("BACK").onClick.Invoke();
            Assert.IsEmpty(app.GetComponentsInChildren<HomeTileMotion>());
            Button("Home records").onClick.Invoke();
            Assert.IsTrue(app.GetComponentsInChildren<Text>().Any(t => t.text == "YOUR RECORD"));
            Button("BACK").onClick.Invoke();
            Assert.IsTrue(app.IsHome); Assert.IsNull(app.Run);
            Begin(); yield return null;
            Assert.IsFalse(app.GetComponentsInChildren<Image>().Any(i => i.name == "Home ruins"));
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
            Assert.IsTrue(app.GetComponentsInChildren<Text>().Any(t => t.text == "SCORE  10"));
            var bar = app.GetComponentsInChildren<Image>().First(i => i.name == "Combo remaining");
            var timer = app.GetComponentsInChildren<Image>().First(i => i.name == "Time remaining");
            Assert.Greater(bar.rectTransform.rect.width, 0);
            Assert.Less(bar.rectTransform.anchoredPosition.y, timer.rectTransform.anchoredPosition.y);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ChapterClearWaitsForConfirmationBeforeStartingNextBoard()
        {
            Begin();
            var header = app.GetComponentsInChildren<Text>().Single(t => t.text == "CHAPTER 01");
            Assert.AreEqual(TextAnchor.MiddleCenter, header.alignment);
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
            Assert.IsNotNull(Resources.Load<Font>("Atlas/MintPixel"));
            Assert.IsNotNull(Resources.Load<UnityEngine.U2D.SpriteAtlas>("Atlas/MintCream"));
            Assert.IsNotNull(MintTheme.Sprite("TileSelected"));
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
        [UnityTest] public IEnumerator ComboFeedbackAllowsRapidMatchesAndCleansUp()
        {
            Begin(); var path = app.Run.Board.GuaranteedPath;
            foreach (int id in path[0]) Tap(id);
            var bar = app.GetComponentsInChildren<Image>().First(i => i.name == "Combo remaining");
            Color firstColor = bar.color;
            for (int i = 1; i < 7; i++) foreach (int id in path[i]) Tap(id);
            Assert.AreEqual(190, app.Run.Score);
            Assert.AreEqual(1, app.GetComponentsInChildren<ComboFeedback>().Length);
            var feedback = app.GetComponentInChildren<ComboFeedback>();
            var track = app.GetComponentsInChildren<Image>().First(i => i.name == "Combo track");
            var expectedCenter = track.rectTransform.anchoredPosition + new Vector2(track.rectTransform.rect.width / 2, -track.rectTransform.rect.height / 2);
            Assert.AreEqual(expectedCenter, ((RectTransform)feedback.transform).anchoredPosition);
            Assert.IsTrue(feedback.GetComponentsInChildren<Text>().Any(t => t.text == "+50"));
            Assert.IsTrue(feedback.GetComponentsInChildren<Text>().Any(t => t.text == "×5"));
            Assert.IsTrue(feedback.GetComponentsInChildren<Text>().Any(t => t.text.Contains("MAX COMBO")));
            Assert.IsFalse(feedback.GetComponent<CanvasGroup>().blocksRaycasts);
            Assert.IsTrue(feedback.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget));
            Assert.AreNotEqual(firstColor, bar.color);
            app.Run.Pause();
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual(0, feedback.GetComponent<CanvasGroup>().alpha);
            Assert.AreEqual(190, app.Run.Score);
            Assert.AreEqual(ComboFeedback.TierColor(5), bar.color);
            app.StartRun();
            Assert.AreEqual(0, feedback.GetComponent<CanvasGroup>().alpha);
            Assert.AreEqual(ComboFeedback.TierColor(1), bar.color);
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
            yield return new WaitForSecondsRealtime(.15f);
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

