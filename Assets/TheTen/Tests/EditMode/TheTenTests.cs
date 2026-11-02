using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace TheTen.Tests
{
    public sealed class TheTenTests
    {
        GameBalance balance;
        [SetUp] public void Setup() { balance = ScriptableObject.CreateInstance<GameBalance>(); }
        [TearDown] public void Teardown() { UnityEngine.Object.DestroyImmediate(balance); }

        [TestCase(2, 3, 5, true)]
        [TestCase(9, 2, -1, true)]
        [TestCase(9, 1, 0, true)]
        [TestCase(3, 100, 4, true)]
        [TestCase(2, 3, 100, true)]
        [TestCase(100, 2, 3, false)]
        [TestCase(5, 100, 100, false)]
        [TestCase(1, 2, 3, false)]
        public void OrderedArithmetic(int a, int b, int c, bool expected) => Assert.AreEqual(expected, TenRules.IsTen(a, b, c));

        [TestCase(1, 3)] [TestCase(2, 3)] [TestCase(3, 3)] [TestCase(4, 3)] [TestCase(5, 3)] [TestCase(9, 3)] [TestCase(10, 4)] [TestCase(20, 5)] [TestCase(40, 7)] [TestCase(100, 7)]
        public void GeneratedBoardsHaveThirtySixTilesAndCompleteLegalPath(int chapter, int height)
        {
            for (int seed = 0; seed < 15; seed++)
            {
                var board = BoardModel.Create(chapter, balance, new Random(seed));
                Assert.AreEqual(36, board.Tiles.Count);
                Assert.AreEqual(36, BoardModel.StackedTileCount);
                Assert.IsTrue(board.Tiles.All(t => t.Stack < board.StackCount));
                Assert.IsTrue(board.Tiles.All(t => t.X >= 30 && t.X + BoardModel.TileWidth <= 690
                    && t.Y >= BoardModel.BoardTop && t.Y + BoardModel.TileHeight <= BoardModel.BoardBottom));
                Assert.AreEqual(height, board.Height);
                Assert.IsTrue(BoardSolver.ValidatePath(board, board.GuaranteedPath), "chapter " + chapter + " seed " + seed);
                foreach (var tile in board.Tiles)
                    Assert.AreEqual(!board.Tiles.Any(u => BoardModel.Covers(u, tile)), board.IsAvailable(tile.Id));
                Assert.IsTrue(board.Tiles.Any(t => board.Tiles.Count(lower => BoardModel.Covers(t, lower)) >= 2));
                if (chapter < 3) Assert.IsTrue(board.Tiles.All(t => t.Value >= 1 && t.Value <= 9));
                if (chapter < 5) Assert.IsFalse(board.Tiles.Any(t => t.IsDouble));
                else Assert.IsTrue(board.Tiles.Any(t => t.IsDouble));
                if (chapter >= 3) { Assert.IsTrue(board.Tiles.Any(t => t.Value == 0)); Assert.IsTrue(board.Tiles.Any(t => t.Value == -1)); }
            }
        }
        [TestCase(3)] [TestCase(5)]
        public void SpecialTilesAppearFromTheirUnlockChapter(int chapter)
        {
            var values = Enumerable.Range(0, 30).SelectMany(seed => BoardModel.Create(chapter, balance, new Random(seed)).Tiles).Select(t => t.Value).ToArray();
            Assert.Contains(0, values); Assert.Contains(-1, values);
            if (chapter >= 5) Assert.Contains(BoardModel.Double, values);
            else Assert.IsFalse(values.Contains(BoardModel.Double));
        }
        [TestCase(5)] [TestCase(6)] [TestCase(10)] [TestCase(20)] [TestCase(100)]
        public void NewBoardsGuaranteeDoubleEvenWhenRandomWeightIsZero(int chapter)
        {
            balance.AdvancedDoubleWeight = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var board = BoardModel.Create(chapter, balance, new Random(seed));
                Assert.AreEqual(36, board.Remaining);
                Assert.AreEqual(1, board.Tiles.Count(t => t.IsDouble));
                Assert.IsTrue(BoardSolver.ValidatePath(board, board.GuaranteedPath), "chapter " + chapter + " seed " + seed);
            }
        }
        [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(10)] [TestCase(100)]
        public void AllUnlockedSpecialsAreGuaranteedWithZeroRandomWeights(int chapter)
        {
            balance.EarlyZeroWeight = balance.EarlyNegativeWeight = 0;
            balance.AdvancedZeroWeight = balance.AdvancedNegativeWeight = balance.AdvancedDoubleWeight = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var board = BoardModel.Create(chapter, balance, new Random(seed));
                Assert.AreEqual(1, board.Tiles.Count(t => t.Value == 0));
                Assert.AreEqual(1, board.Tiles.Count(t => t.Value == -1));
                Assert.AreEqual(chapter >= 5 ? 1 : 0, board.Tiles.Count(t => t.IsDouble));
                Assert.AreEqual(36, board.Remaining);
                Assert.IsTrue(BoardSolver.ValidatePath(board, board.GuaranteedPath));
            }
        }
        [Test] public void AllSpatialBlockersMustBeRemovedBeforeLowerTileOpens()
        {
            var b = BoardModel.Create(1, balance, new Random(42));
            var lower = b.Tiles.First(t => b.Tiles.Count(u => BoardModel.Covers(u, t)) >= 2);
            var blockers = b.Tiles.Where(t => BoardModel.Covers(t, lower)).ToArray();
            var position = (lower.X, lower.Y);
            Assert.IsFalse(b.IsAvailable(lower.Id));
            foreach (var blocker in blockers.Take(blockers.Length - 1)) blocker.Removed = true;
            Assert.IsFalse(b.IsAvailable(lower.Id)); blockers.Last().Removed = true;
            Assert.IsTrue(b.IsAvailable(lower.Id)); Assert.AreEqual(position, (lower.X, lower.Y));
            Assert.IsTrue(b.Tiles.All(t => b.Tiles.Any(other => BoardModel.Covers(other, t) || BoardModel.Covers(t, other))));
        }
        [Test] public void SelectionOnlyEvaluatesOnThirdTapAndCanCancel()
        {
            var run = new RunModel(balance, 1); var move = run.Board.FindMove();
            Assert.AreEqual(SelectionResult.Selected, run.Select(move[0]));
            Assert.AreEqual(SelectionResult.Deselected, run.Select(move[0]));
            Assert.AreEqual(0, run.Selection.Count);
            run.Select(move[0]); run.Select(move[1]);
            Assert.AreEqual(36, run.Board.Remaining); Assert.AreEqual(30, run.RemainingTime);
            Assert.AreEqual(SelectionResult.Ten, run.Select(move[2]));
            Assert.AreEqual(33, run.Board.Remaining); Assert.AreEqual(33, run.RemainingTime); Assert.AreEqual(1, run.SuccessCount);
        }
        [Test] public void FailedTripleDeductsThreeSecondsOnceAndClearsSelection()
        {
            var run = new RunModel(balance, 1); var a = run.Board.Available().Take(3).ToArray();
            foreach (var t in a) t.Value = 1;
            run.Select(a[0].Id); run.Select(a[1].Id);
            Assert.AreEqual(30, run.RemainingTime); Assert.AreEqual(2, run.Selection.Count);
            Assert.AreEqual(SelectionResult.Miss, run.Select(a[2].Id));
            Assert.AreEqual(36, run.Board.Remaining); Assert.AreEqual(27, run.RemainingTime); Assert.IsEmpty(run.Selection);
            Assert.AreEqual(0, run.Score); Assert.AreEqual(0, run.SuccessCount); Assert.AreEqual(0, run.TotalSurvivalTime);
            Assert.IsFalse(run.CancelSelection()); Assert.AreEqual(27, run.RemainingTime);
            foreach (var t in a) run.Select(t.Id);
            Assert.AreEqual(24, run.RemainingTime); Assert.IsEmpty(run.Selection);
        }
        [TestCase(2, false)] [TestCase(3, false)] [TestCase(3.5, false)]
        [TestCase(2, true)] [TestCase(3, true)] [TestCase(3.5, true)]
        public void FailurePenaltyHandlesLifeBoundaryWithoutAddingSurvivalTime(double remaining, bool revived)
        {
            balance.InitialTime = balance.ReviveTime = 1;
            var run = new RunModel(balance, 1);
            if (revived) { run.Tick(1); Assert.IsTrue(run.Revive()); }
            Match(run, run.Board.GuaranteedPath[0]);
            run.Tick(run.RemainingTime - remaining);
            double first = run.FirstSurvivalTime, second = run.SecondSurvivalTime, combo = run.ComboRemaining;
            Assert.Greater(combo, 0);
            var miss = run.Board.Available().Take(3).ToArray();
            foreach (var tile in miss) tile.Value = 1;
            Match(run, miss.Select(t => t.Id).ToArray());
            Assert.AreEqual(Math.Max(0, remaining - 3), run.RemainingTime);
            Assert.AreEqual(first, run.FirstSurvivalTime); Assert.AreEqual(second, run.SecondSurvivalTime);
            Assert.AreEqual(10, run.Score); Assert.AreEqual(1, run.SuccessCount); Assert.AreEqual(33, run.Board.Remaining);
            Assert.IsEmpty(run.Selection);
            Assert.AreEqual(remaining > 3 ? RunState.Playing : revived ? RunState.GameOver : RunState.ReviveChoice, run.State);
            Assert.AreEqual(remaining > 3 ? combo : 0, run.ComboRemaining);
            if (remaining <= 3)
            {
                Assert.AreEqual(SelectionResult.Ignored, run.Select(miss[0].Id));
                Assert.AreEqual(0, run.RemainingTime);
            }
        }
        [TestCase(1)] [TestCase(2)]
        public void CancelAllSelectionsPreservesTimeScoreComboAndBoard(int count)
        {
            var run = new RunModel(balance, 1);
            var path = run.Board.GuaranteedPath;
            Match(run, path[0]); run.Tick(.5);
            foreach (int id in path[1].Take(count)) run.Select(id);
            double time = run.RemainingTime, combo = run.ComboRemaining, survival = run.TotalSurvivalTime;
            Assert.IsTrue(run.CancelSelection()); Assert.IsEmpty(run.Selection);
            Assert.AreEqual(time, run.RemainingTime); Assert.AreEqual(combo, run.ComboRemaining);
            Assert.AreEqual(survival, run.TotalSurvivalTime); Assert.AreEqual(10, run.Score);
            Assert.AreEqual(33, run.Board.Remaining); Assert.AreEqual(RunState.Playing, run.State);
            Assert.IsFalse(run.CancelSelection());
        }
        [Test] public void RetappingOnlyCancelsThatTileUnlessItWouldLeaveAnOperatorFirst()
        {
            var run = new RunModel(balance, 1); var move = run.Board.GuaranteedPath[0];
            run.Select(move[0]); run.Select(move[1]);
            Assert.AreEqual(SelectionResult.Deselected, run.Select(move[0]));
            CollectionAssert.AreEqual(new[] { move[1] }, run.Selection);
            run.CancelSelection(); run.Board.Tiles[move[1]].Value = BoardModel.Double;
            run.Select(move[0]); run.Select(move[1]); run.Select(move[0]);
            Assert.IsEmpty(run.Selection); Assert.AreEqual(30, run.RemainingTime);
        }
        [Test] public void CoveredTileAndFirstOperatorAreIgnored()
        {
            var run = new RunModel(balance, 1);
            Assert.AreEqual(SelectionResult.Ignored, run.Select(0));
            var a = run.Board.Available().First(); a.Value = BoardModel.Double;
            Assert.AreEqual(SelectionResult.Ignored, run.Select(a.Id));
            Assert.AreEqual(30, run.RemainingTime); Assert.IsEmpty(run.Selection);
        }
        [Test] public void BoardClearAndChapterPreserveLifeTimeAndCounters()
        {
            balance.MaxRemainingTime = 0;
            var run = new RunModel(balance, 3); var path = run.Board.GuaranteedPath.ToList();
            run.Tick(4);
            foreach (var move in path.Take(path.Count - 1)) foreach (int id in move) run.Select(id);
            Assert.AreEqual(3, run.Board.Remaining); Assert.AreEqual(1, run.Chapter);
            Assert.AreEqual(RunState.Playing, run.State);
            Assert.IsFalse(run.CompleteTransition(out _));
            foreach (int id in path.Last()) run.Select(id);
            Assert.AreEqual(RunState.BoardTransition, run.State);
            Assert.AreEqual(0, run.Board.Remaining);
            Assert.AreEqual(1, run.Chapter); Assert.AreEqual(36, run.TotalRemovedTiles); Assert.AreEqual(12, run.SuccessCount);
            double time = run.RemainingTime; run.Tick(30);
            Assert.AreEqual(time, run.RemainingTime); Assert.AreEqual(4, run.TotalSurvivalTime);
            Assert.IsTrue(run.CompleteTransition(out _)); Assert.AreEqual(36, run.Board.Remaining);
            Assert.AreEqual(time, run.RemainingTime); Assert.AreEqual(2, run.Chapter);
            Assert.IsFalse(run.CompleteTransition(out _)); Assert.AreEqual(time, run.RemainingTime);
            Assert.AreEqual(2, run.Chapter);
            foreach (var move in run.Board.GuaranteedPath.ToList()) foreach (int id in move) run.Select(id);
            Assert.AreEqual(RunState.BoardTransition, run.State); Assert.AreEqual(2, run.Chapter);
            Assert.IsTrue(run.CompleteTransition(out _)); Assert.AreEqual(3, run.Chapter);
        }
        [Test] public void TimerExcludesPauseChoiceAdAndGameOverAndClampsDeathFrame()
        {
            var run = new RunModel(balance, 1);
            run.Tick(10); run.Pause(); run.Tick(100); Assert.AreEqual(10, run.TotalSurvivalTime);
            run.Resume(); run.Tick(1000); Assert.AreEqual(30, run.TotalSurvivalTime);
            run.Tick(100); Assert.AreEqual(30, run.TotalSurvivalTime);
            Assert.IsTrue(run.BeginAd()); run.Tick(500); Assert.AreEqual(30, run.TotalSurvivalTime);
            var board = run.Board; Assert.IsTrue(run.Revive()); Assert.AreSame(board, run.Board);
            Assert.IsFalse(run.Revive()); run.Tick(1000);
            Assert.AreEqual(RunState.GameOver, run.State); Assert.AreEqual(60, run.TotalSurvivalTime);
            Assert.AreEqual(30, run.SecondSurvivalTime); Assert.IsFalse(run.BeginAd());
            run.Tick(100); Assert.AreEqual(60, run.TotalSurvivalTime);
        }
        [Test] public void CancelledAdReturnsToChoiceWithoutReviving()
        {
            var run = new RunModel(balance, 1); run.Tick(60); run.BeginAd(); run.CancelAd();
            Assert.AreEqual(RunState.ReviveChoice, run.State); Assert.IsFalse(run.ReviveUsed);
        }
        [Test] public void ShuffleRepairsImpossibleValuesAndPreservesGeometry()
        {
            var b = BoardModel.Create(20, balance, new Random(1));
            var geometry = b.Tiles.Select(t => (t.Id, t.X, t.Y, t.Layer)).ToArray();
            foreach (var t in b.Tiles) t.Value = 9;
            Assert.IsNull(b.FindMove());
            Assert.IsTrue(b.Shuffle(20, balance, new Random(2), out bool repaired));
            Assert.IsTrue(repaired); Assert.IsTrue(BoardSolver.ValidatePath(b, b.GuaranteedPath));
            CollectionAssert.AreEqual(geometry, b.Tiles.Select(t => (t.Id, t.X, t.Y, t.Layer)).ToArray());
        }
        [Test] public void ImpossibleGeometryRearrangesAndPreservesRemainingTiles()
        {
            var b = BoardModel.Create(1, balance, new Random(1));
            var top = b.Tiles.First(t => t.Layer == 2);
            var middle = b.Tiles.First(t => t.Layer == 1 && BoardModel.Covers(top, t));
            var bottom = b.Tiles.First(t => t.Layer == 0 && BoardModel.Covers(middle, t));
            foreach (var t in b.Tiles) t.Removed = t != top && t != middle && t != bottom;
            var ids = b.Tiles.Where(t => !t.Removed).Select(t => t.Id).ToArray();
            top.Value = 2; middle.Value = 3; bottom.Value = 5;
            Assert.IsTrue(b.Shuffle(1, balance, new Random(2), out bool repaired));
            Assert.IsTrue(repaired); Assert.AreEqual(3, b.Remaining);
            CollectionAssert.AreEqual(ids, b.Tiles.Where(t => !t.Removed).Select(t => t.Id));
            CollectionAssert.AreEquivalent(new[] {2, 3, 5}, b.Tiles.Where(t => !t.Removed).Select(t => t.Value));
            Assert.IsTrue(BoardSolver.ValidatePath(b, b.GuaranteedPath));
        }
        [TestCase(3)] [TestCase(6)] [TestCase(9)] [TestCase(18)] [TestCase(33)] [TestCase(36)]
        public void SpatialRecoveryHandlesEveryBoardSize(int count)
        {
            var b = BoardModel.Create(7, balance, new Random(count));
            foreach (var tile in b.Tiles)
            {
                tile.Removed = tile.Id >= count;
                tile.X = 300; tile.Y = 650; tile.Layer = tile.Id / 2; tile.Value = 9;
            }
            Assert.Less(b.Available().Count, 3);
            Assert.IsTrue(b.Shuffle(7, balance, new Random(1), out _));
            Assert.AreEqual(count, b.Remaining);
            Assert.IsTrue(BoardSolver.ValidatePath(b, b.GuaranteedPath));
            Assert.IsTrue(b.Tiles.Where(t => !t.Removed).All(t => t.X >= 30 && t.X + BoardModel.TileWidth <= 690
                && t.Y >= BoardModel.BoardTop && t.Y + BoardModel.TileHeight <= BoardModel.BoardBottom));
        }
        [Test] public void ShuffleBonusOccursOnceAndTimerStopsDuringShuffle()
        {
            var run = new RunModel(balance, 8); var move = run.Board.FindMove();
            foreach (var t in run.Board.Tiles.Where(t => !move.Contains(t.Id))) t.Value = 9;
            foreach (int id in move) run.Select(id);
            Assert.AreEqual(RunState.Shuffling, run.State);
            run.Tick(100); Assert.AreEqual(33, run.RemainingTime);
            Assert.IsTrue(run.CompleteTransition(out _)); Assert.AreEqual(41, run.RemainingTime);
            Assert.IsFalse(run.CompleteTransition(out _)); Assert.AreEqual(41, run.RemainingTime);
        }

        [TestCase(0, 1, 9, 2)] [TestCase(-1, 2, 9, 2)]
        [TestCase(2, 3, 100, 4)] [TestCase(0, 5, 100, 4)] [TestCase(2, 3, 5, 1)]
        public void SpecialBonusMultipliesComboScoreButNotTime(int a, int b, int c, int bonus)
        {
            var run = new RunModel(balance, 12);
            var path = run.Board.GuaranteedPath.ToList();
            Match(run, path[0]);
            var move = path[1];
            run.Board.Tiles[move[0]].Value = a; run.Board.Tiles[move[1]].Value = b; run.Board.Tiles[move[2]].Value = c;
            double time = run.RemainingTime;
            Match(run, move);
            Assert.AreEqual(bonus, run.LastMatchBonusMultiplier);
            Assert.AreEqual(15 * bonus, run.LastMatchScore); Assert.AreEqual(10 + 15 * bonus, run.Score);
            Assert.AreEqual(time + 3, run.RemainingTime);
        }
        [Test] public void FailedSpecialMatchDoesNotAwardBonus()
        {
            var run = new RunModel(balance, 1); var move = run.Board.GuaranteedPath[0];
            run.Board.Tiles[move[0]].Value = 0; run.Board.Tiles[move[1]].Value = 1; run.Board.Tiles[move[2]].Value = 100;
            Match(run, move);
            Assert.AreEqual(0, run.Score); Assert.AreEqual(36, run.Board.Remaining); Assert.AreEqual(27, run.RemainingTime);
            Assert.IsEmpty(run.Selection);
        }
        static void Match(RunModel run, int[] move)
        {
            foreach (int id in move) run.Select(id);
        }
        [Test] public void ComboAwardsExactStepsAndCapsAtFiveTimes()
        {
            var run = new RunModel(balance, 12);
            var path = run.Board.GuaranteedPath;
            int[] points = { 10, 15, 20, 25, 30, 40, 50, 50 };
            int total = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0) run.Tick(2.9);
                Match(run, path[i]); total += points[i];
                Assert.AreEqual(points[i], run.LastMatchScore);
                Assert.AreEqual(total, run.Score);
                Assert.AreEqual(3, run.ComboRemaining);
            }
            Assert.AreEqual(5, run.ComboMultiplier);
        }
        [TestCase(2.999, 15)] [TestCase(3, 10)] [TestCase(3.001, 10)]
        public void ComboWindowExpiresAtThreeSeconds(double delay, int points)
        {
            var run = new RunModel(balance, 2); var path = run.Board.GuaranteedPath;
            Match(run, path[0]); run.Tick(delay); Match(run, path[1]);
            Assert.AreEqual(points, run.LastMatchScore); Assert.AreEqual(10 + points, run.Score);
        }
        [Test] public void PauseAndBoardTransitionFreezeComboAndPreserveScore()
        {
            var run = new RunModel(balance, 2); var path = run.Board.GuaranteedPath;
            Match(run, path[0]); run.Tick(1); run.Pause(); run.Tick(20);
            Assert.AreEqual(2, run.ComboRemaining); Assert.AreEqual(10, run.Score);
            run.Resume();
            foreach (var move in path.Skip(1)) Match(run, move);
            int score = run.Score;
            Assert.AreEqual(RunState.BoardTransition, run.State);
            run.Tick(100); Assert.AreEqual(3, run.ComboRemaining);
            run.CompleteTransition(out _); Assert.AreEqual(score, run.Score);
            Match(run, run.Board.GuaranteedPath[0]); Assert.AreEqual(score + 50, run.Score);
        }
        [Test] public void DeathResetsComboButRevivePreservesPoints()
        {
            var run = new RunModel(balance, 9); Match(run, run.Board.GuaranteedPath[0]);
            run.Tick(1000); Assert.AreEqual(0, run.ComboRemaining); Assert.AreEqual(1, run.ComboMultiplier);
            Assert.IsTrue(run.Revive()); Assert.AreEqual(10, run.Score);
            Match(run, run.Board.GuaranteedPath[1]); Assert.AreEqual(20, run.Score);
        }
        [Test] public void MissDoesNotAwardPointsOrRefreshCombo()
        {
            var run = new RunModel(balance, 9); Match(run, run.Board.GuaranteedPath[0]); run.Tick(1);
            var tiles = run.Board.Available().Take(3).ToArray();
            foreach (var tile in tiles) tile.Value = 1;
            double time = run.RemainingTime, survival = run.TotalSurvivalTime;
            Match(run, tiles.Select(t => t.Id).ToArray());
            Assert.AreEqual(10, run.Score); Assert.AreEqual(2, run.ComboRemaining);
            Assert.AreEqual(time - 3, run.RemainingTime); Assert.AreEqual(survival, run.TotalSurvivalTime);
        }
        [Test] public void RankingPersistsPointsAndIgnoresLegacySeconds()
        {
            string prefix = "TheTen.RankingTest." + Guid.NewGuid().ToString("N");
            try
            {
                PlayerPrefs.SetFloat(prefix + ".Prototype.BestSeconds", 9999);
                var ranking = new LocalRanking(prefix); Assert.AreEqual(0, ranking.BestScore);
                ranking.Submit(125); ranking.Submit(10);
                Assert.AreEqual(125, new LocalRanking(prefix).BestScore);
                ranking.Submit(250); Assert.AreEqual(250, ranking.BestScore);
            }
            finally
            {
                PlayerPrefs.DeleteKey(prefix + ".Prototype.BestSeconds");
                PlayerPrefs.DeleteKey(prefix + ".Prototype.BestScore.v1"); PlayerPrefs.Save();
            }
        }

        sealed class MemoryStore : ITicketStorage
        {
            public TicketData Data = new TicketData();
            public bool Fail;
            public TicketData Load() => Data.Copy();
            public void Save(TicketData data) { if (Fail) throw new Exception("disk failure"); Data = data.Copy(); }
        }
        sealed class Clock : ITrustedServerClock
        {
            public DateTimeOffset? Now = new DateTimeOffset(2026, 10, 31, 14, 59, 59, TimeSpan.Zero);
            public Task<DateTimeOffset?> GetUtcNowAsync() => Task.FromResult(Now);
        }
        sealed class Store : IPurchaseGateway
        {
            public bool Valid = true, Owned = true;
            public Task<PurchaseVerification> PurchaseAndVerifyAsync() => Task.FromResult(new PurchaseVerification(Valid, Owned));
            public Task<PurchaseVerification> RestoreAndVerifyAsync() => PurchaseAndVerifyAsync();
        }
        [Test] public async Task MidnightResetsToTwoRatherThanAddingAndRestartDoesNotRegrant()
        {
            var clock = new Clock(); var storage = new MemoryStore(); var store = new Store();
            var m = new ReviveTicketManager(clock, storage, store);
            Assert.IsTrue(await m.PurchaseAsync()); Assert.AreEqual(2, m.CurrentTicketCount);
            Assert.AreEqual("2026-10-31", m.LastResetDate);
            Assert.IsTrue(await m.TryConsumeAsync("run1"));
            m = new ReviveTicketManager(clock, storage, store);
            Assert.AreEqual(TicketSyncState.RecheckRequired, m.SyncState);
            await m.RestoreAsync(); Assert.AreEqual(1, m.CurrentTicketCount);
            clock.Now = clock.Now.Value.AddSeconds(1);
            await m.RefreshAsync(); Assert.AreEqual("2026-11-01", m.LastResetDate); Assert.AreEqual(2, m.CurrentTicketCount);
            await m.RefreshAsync(); Assert.AreEqual(2, m.CurrentTicketCount);
            Assert.IsFalse(await m.TryConsumeAsync("run1"));
        }
        [Test] public async Task DaysMissedDoNotAccumulateAndBackwardsClockCannotRegrant()
        {
            var c = new Clock(); var m = new ReviveTicketManager(c, new MemoryStore(), new Store());
            await m.PurchaseAsync(); await m.TryConsumeAsync("a"); await m.TryConsumeAsync("b");
            c.Now = c.Now.Value.AddDays(10); await m.RefreshAsync(); Assert.AreEqual(2, m.CurrentTicketCount);
            await m.TryConsumeAsync("c"); c.Now = c.Now.Value.AddDays(-5);
            Assert.IsFalse(await m.RefreshAsync()); Assert.AreEqual(1, m.CurrentTicketCount);
            Assert.AreEqual(TicketSyncState.RecheckRequired, m.SyncState);
        }
        [Test] public async Task OfflinePurchaseAndRestartNeverGrantUntilServerConfirms()
        {
            var c = new Clock { Now = null }; var storage = new MemoryStore(); var store = new Store();
            var m = new ReviveTicketManager(c, storage, store);
            Assert.IsFalse(await m.PurchaseAsync()); Assert.IsTrue(m.HasAdFreeRevivePurchase); Assert.AreEqual(0, m.CurrentTicketCount);
            m = new ReviveTicketManager(c, storage, store);
            Assert.IsFalse(await m.TryConsumeAsync("run")); Assert.AreEqual(TicketSyncState.RecheckRequired, m.SyncState);
            c.Now = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
            Assert.IsTrue(await m.RefreshAsync()); Assert.AreEqual(2, m.CurrentTicketCount);
        }
        [Test] public async Task FailedVerificationCannotGrantAndRestoreIsIdempotent()
        {
            var c = new Clock(); var s = new Store { Valid = false }; var m = new ReviveTicketManager(c, new MemoryStore(), s);
            Assert.IsFalse(await m.PurchaseAsync()); Assert.IsFalse(m.HasAdFreeRevivePurchase); Assert.AreEqual(0, m.CurrentTicketCount);
            s.Valid = true; await m.RestoreAsync(); await m.TryConsumeAsync("a"); await m.RestoreAsync();
            Assert.AreEqual(1, m.CurrentTicketCount);
            s.Owned = false; await m.RestoreAsync(); Assert.IsFalse(m.HasAdFreeRevivePurchase); Assert.AreEqual(0, m.CurrentTicketCount);
        }
        [Test] public async Task ConcurrentDuplicateConsumesUseOnlyOneTicket()
        {
            var m = new ReviveTicketManager(new Clock(), new MemoryStore(), new Store()); await m.PurchaseAsync();
            var results = await Task.WhenAll(m.TryConsumeAsync("a"), m.TryConsumeAsync("a"));
            Assert.AreEqual(1, results.Count(r => r)); Assert.AreEqual(1, m.CurrentTicketCount);
            Assert.IsTrue(await m.TryConsumeAsync("b")); Assert.IsFalse(await m.TryConsumeAsync("c")); Assert.AreEqual(0, m.CurrentTicketCount);
        }
        [Test] public async Task FailedPersistenceDoesNotConsumeOrGrant()
        {
            var storage = new MemoryStore(); var m = new ReviveTicketManager(new Clock(), storage, new Store());
            await m.PurchaseAsync(); storage.Fail = true;
            Assert.IsFalse(await m.TryConsumeAsync("a")); Assert.AreEqual(2, m.CurrentTicketCount);
        }
        [Test] public void SeoulDateIgnoresInputOffset()
        {
            Assert.AreEqual("2026-11-01", ReviveTicketManager.SeoulDate(new DateTimeOffset(2026, 10, 31, 15, 0, 0, TimeSpan.Zero)));
            Assert.AreEqual("2026-11-01", ReviveTicketManager.SeoulDate(new DateTimeOffset(2026, 10, 31, 10, 0, 0, TimeSpan.FromHours(-5))));
        }
    }
}

