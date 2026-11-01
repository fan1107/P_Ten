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
            var upper = app.Run.Board.Tiles.First(t => !t.IsFree && app.Run.Board.IsAvailable(t.Id)
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
        [UnityTest] public IEnumerator RewardedAdRevivesOnceAndSecondDeathEndsRun()
        {
            Begin(); app.Run.Tick(1000); yield return null;
            Assert.AreEqual(RunState.ReviveChoice, app.Run.State);
            Button("WATCH REWARDED AD / MOCK").onClick.Invoke();
            Assert.AreEqual(RunState.AdPlaying, app.Run.State);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.IsTrue(app.Run.ReviveUsed); Assert.AreEqual(RunState.Playing, app.Run.State);
            app.Run.Tick(1000); yield return null;
            Assert.AreEqual(RunState.GameOver, app.Run.State);
            Assert.AreEqual(70, app.Run.TotalSurvivalTime, .0001);
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
                Assert.AreEqual(40, app.Run.TotalSurvivalTime, .0001);
            }
            Button("FINISH RUN").onClick.Invoke();
            Assert.AreEqual(RunState.GameOver, app.Run.State);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator VerifiedTicketUiConsumesOneAndSkipsAd()
        {
            var purchase = app.Tickets.PurchaseAsync(); yield return new WaitUntil(() => purchase.IsCompleted);
            Assert.IsTrue(purchase.Result); Begin(); app.Run.Tick(1000); yield return null;
            Button("USE REVIVE TICKET  (2)").onClick.Invoke();
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

