using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TheTen.Tests
{
    public sealed class TouchStageTests
    {
        GameBalance balance;

        [SetUp] public void Setup() { balance = ScriptableObject.CreateInstance<GameBalance>(); }
        [TearDown] public void Teardown() { Object.DestroyImmediate(balance); }

        [Test]
        public void EveryStageClearsWithItsFinalTapThroughNormalSelection()
        {
            int previousLimit = 44;
            for (int stage = 1; stage <= TouchStages.Count; stage++)
            {
                var run = new TouchRun(stage, balance);
                TouchStages.Create(stage, balance, out var moves);
                var values = run.Board.Tiles.Select(t => t.Value).ToArray();
                Assert.AreEqual(36, values.Length);
                Assert.IsFalse(values.Contains(0), $"Stage {stage} contains a zero tile");
                Assert.AreEqual(stage >= 3, values.Contains(-1));
                Assert.AreEqual(stage >= 5, values.Contains(BoardModel.Double));
                Assert.IsTrue(moves.Any(m => m.Length > 3));
                Assert.LessOrEqual(run.TapsLeft, previousLimit);
                previousLimit = run.TapsLeft;
                if (stage == 1) Assert.AreEqual(44, run.TapsLeft);
                if (stage == 20) Assert.AreEqual(38, run.TapsLeft);

                while (run.TapsLeft > 36)
                {
                    Assert.AreEqual(SelectionResult.Selected, run.Select(moves[0][0]));
                    run.CancelSelection();
                }
                foreach (var move in moves)
                {
                    for (int i = 0; i < move.Length; i++)
                        Assert.AreEqual(i + 1 == move.Length ? SelectionResult.Ten : SelectionResult.Selected,
                            run.Select(move[i]), $"Stage {stage}, tile {move[i]}");
                }
                Assert.AreEqual(0, run.TapsLeft);
                Assert.AreEqual(0, run.Board.Remaining);
                Assert.AreEqual(TouchStageState.Cleared, run.State);
            }
        }

        TouchRun WithValues(params int[] values)
        {
            var run = new TouchRun(5, balance);
            foreach (var tile in run.Board.Tiles)
            {
                tile.Removed = tile.Id >= values.Length;
                tile.Layer = 0;
                if (!tile.Removed) tile.Value = values[tile.Id];
            }
            return run;
        }

        [Test]
        public void OvershootCanReturnToTenAndKeepsSpecialScore()
        {
            var run = WithValues(8, 3, -1, 1, 1, 1, 2, 2, 3);
            run.Select(0); run.Select(1);
            Assert.AreEqual(11, run.Total);
            Assert.AreEqual(SelectionResult.Ten, run.Select(2));
            Assert.AreEqual(20, run.Score);
            for (int id = 3; id < 8; id++) Assert.AreEqual(SelectionResult.Selected, run.Select(id));
            Assert.AreEqual(SelectionResult.Ten, run.Select(8));
            Assert.AreEqual(30, run.Score);
            Assert.AreEqual(TouchStageState.Cleared, run.State);
        }

        [Test]
        public void UndoReevaluatesInOrderAndOnlyNewLegalPicksCostTaps()
        {
            var run = WithValues(2, 1, BoardModel.Double, BoardModel.Double, 6);
            int limit = run.TapsLeft;
            Assert.AreEqual(SelectionResult.Ignored, run.Select(35));
            Assert.AreEqual(SelectionResult.Ignored, run.Select(2));
            run.Select(0); run.Select(1); run.Select(2);
            Assert.AreEqual(SelectionResult.Ignored, run.Select(3));
            Assert.AreEqual(6, run.Total);
            run.Select(1);
            Assert.AreEqual(4, run.Total);
            run.Select(0); // Leading operator cancels the selection.
            Assert.IsEmpty(run.Selection);
            Assert.AreEqual(limit - 3, run.TapsLeft);
            run.Select(0); run.CancelSelection();
            Assert.AreEqual(limit - 4, run.TapsLeft);
            run.Select(0); run.Select(2); run.Select(4);
            Assert.AreEqual(40, run.Score);
            Assert.AreEqual(TouchStageState.Playing, run.State);
            Assert.AreEqual(2, run.Board.Remaining);
            Assert.AreEqual(limit - 7, run.TapsLeft);
            Assert.AreEqual(BoardModel.Double, run.Board.Tiles[3].Value);
            run.Select(1); run.Select(3);
            Assert.AreEqual(TouchStageState.Cleared, run.State);
        }

        [TestCase(1), TestCase(4)]
        public void DeadEndKeepsEverySlotAndHasACompletePlayableSolution(int columns)
        {
            for (int count = 1; count <= 34; count++)
            {
                var run = new TouchRun(5, balance);
                var tiles = run.Board.Tiles;
                tiles[0].Value = 7; tiles[1].Value = 3;
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].Removed = i >= count + 2;
                    tiles[i].X = i < 2 ? i * 115 : 250 + (i - 2) % columns * 115;
                    tiles[i].Y = 650;
                    tiles[i].Layer = i < 2 ? 0 : i;
                }
                foreach (var exposed in run.Board.Available().Where(t => t.Id >= 2)) exposed.Value = 9;
                var slots = tiles.Select(t => (t.Id, t.Stack, t.X, t.Y, t.Layer)).ToArray();
                int height = run.Board.Height;
                int budget = run.TapsLeft;
                run.Select(0); run.Select(1, out bool changed);
                Assert.IsTrue(changed, $"Expected a redeal for {count} remaining");
                Assert.IsFalse(tiles.Any(t => !t.Removed && t.Value == 0));
                Assert.AreEqual(10, run.Score);
                Assert.AreEqual(budget - 2, run.TapsLeft);
                Assert.AreEqual(0, run.Total);
                Assert.IsEmpty(run.Selection);
                Assert.AreEqual(height, run.Board.Height);
                CollectionAssert.AreEqual(slots, tiles.Select(t => (t.Id, t.Stack, t.X, t.Y, t.Layer)));
                CollectionAssert.AreEquivalent(Enumerable.Range(2, count), tiles.Where(t => !t.Removed).Select(t => t.Id));
                var path = run.Board.GuaranteedPath;
                CollectionAssert.AreEquivalent(Enumerable.Range(2, count), path.SelectMany(group => group));
                foreach (var group in path)
                {
                    for (int i = 0; i < group.Length; i++)
                    {
                        var result = run.Select(group[i], out bool redealtAgain);
                        Assert.AreEqual(i == group.Length - 1 ? SelectionResult.Ten : SelectionResult.Selected, result);
                        Assert.IsFalse(redealtAgain, $"Broken solution for {count} remaining");
                    }
                }
                Assert.AreEqual(TouchStageState.Cleared, run.State);
                Assert.AreEqual(budget - 2 - count, run.TapsLeft);
            }
        }

        [Test]
        public void BuriedValuesSwapIntoExposedSlotsWhenTheyCanSupplyACompleteSolution()
        {
            var run = WithValues(6, 4, 5, 8, 2, BoardModel.Double);
            foreach (var tile in run.Board.Tiles.Skip(2).Where(t => !t.Removed))
            {
                tile.X = 250 + tile.Id % 2 * 115; tile.Y = 750; tile.Layer = (tile.Id - 2) / 2;
            }
            var values = run.Board.Tiles.Skip(2).Where(t => !t.Removed).Select(t => t.Value).ToArray();
            run.Select(0); run.Select(1, out bool changed);
            Assert.IsTrue(changed);
            CollectionAssert.AreEquivalent(values, run.Board.Tiles.Where(t => !t.Removed).Select(t => t.Value));
            CollectionAssert.AreEquivalent(new[] {4, 5}, run.Board.Available().Select(t => t.Id));
            Assert.AreEqual(10, run.Score);
            foreach (var group in run.Board.GuaranteedPath)
                foreach (int id in group) run.Select(id);
            Assert.AreEqual(TouchStageState.Cleared, run.State);
        }

        [Test]
        public void InsufficientBudgetStillRedealsAndOnlyFailsWhenTheLastTapIsSpent()
        {
            var run = WithValues(6, 4, 9, -1, -1);
            while (run.TapsLeft > 3) { run.Select(0); run.CancelSelection(); }
            run.Select(0); run.Select(1, out bool changed);
            Assert.IsTrue(changed);
            Assert.AreEqual(TouchStageState.Playing, run.State);
            Assert.AreEqual(1, run.TapsLeft);
            Assert.AreEqual(3, run.Board.Remaining);
            CollectionAssert.AreEquivalent(new[] {9, 2, -1}, run.Board.Tiles.Where(t => !t.Removed).Select(t => t.Value));
            run.Select(run.Board.GuaranteedPath[0][0]);
            Assert.AreEqual(TouchStageState.OutOfTaps, run.State);
            Assert.Zero(run.TapsLeft);
        }

        [Test]
        public void LastTapStopsInputOnAnUnfinishedBoard()
        {
            var run = WithValues(2, 8, 3, 7);
            while (run.TapsLeft > 2) { run.Select(0); run.CancelSelection(); }
            run.Select(0); run.Select(1);
            Assert.AreEqual(TouchStageState.OutOfTaps, run.State);
            Assert.AreEqual(2, run.Board.Remaining);
            Assert.AreEqual(SelectionResult.Ignored, run.Select(2));
            Assert.AreEqual(0, run.TapsLeft);
        }
    }
}
