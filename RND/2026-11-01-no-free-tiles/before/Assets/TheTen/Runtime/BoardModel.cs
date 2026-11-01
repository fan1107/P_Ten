using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTen
{
    public sealed class Tile
    {
        public int Id, Stack, Layer, Value;
        public float X, Y;
        public bool IsFree;
        public bool Removed;
        public bool IsDouble => Value == BoardModel.Double;
        public string Label => IsDouble ? "×2" : Value.ToString();
    }

    public static class TenRules
    {
        public static bool TryEvaluate(int a, int b, int c, out int result)
        {
            result = 0;
            if (a == BoardModel.Double || (b == BoardModel.Double && c == BoardModel.Double)) return false;
            result = b == BoardModel.Double ? a * 2 + c : c == BoardModel.Double ? (a + b) * 2 : a + b + c;
            return true;
        }
        public static bool IsTen(int a, int b, int c) => TryEvaluate(a, b, c, out var value) && value == 10;
        public static int[] OrderedTen(Tile a, Tile b, Tile c)
        {
            var t = new[] { a, b, c };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (i != j && IsTen(t[i].Value, t[j].Value, t[3 - i - j].Value))
                        return new[] { t[i].Id, t[j].Id, t[3 - i - j].Id };
            return null;
        }
    }

    public sealed class BoardModel
    {
        public const int Double = 100;
        public const int TileCount = 36;
        public const int FreeTileCount = 6;
        public const int StackedTileCount = TileCount - FreeTileCount;
        public const float TileWidth = 92, TileHeight = 116;
        public readonly List<Tile> Tiles = new List<Tile>();
        public List<int[]> GuaranteedPath { get; private set; }
        public int Remaining => Tiles.Count(t => !t.Removed);
        public int StackCount { get; private set; }
        public int Height { get; private set; }
        public static bool Covers(Tile upper, Tile lower) => !upper.IsFree && !lower.IsFree && upper.Layer > lower.Layer
            && upper.X < lower.X + TileWidth && upper.X + TileWidth > lower.X
            && upper.Y < lower.Y + TileHeight && upper.Y + TileHeight > lower.Y;
        public bool IsAvailable(int id) => !Tiles[id].Removed && !Tiles.Any(t => !t.Removed && Covers(t, Tiles[id]));
        public List<Tile> Available() => Tiles.Where(t => IsAvailable(t.Id)).ToList();

        public static BoardModel Create(int chapter, GameBalance balance, Random random)
        {
            var b = new BoardModel();
            b.Height = Math.Max(3, Math.Min(10, Math.Min(balance.MaxStackHeight, 3 + chapter / 10)));
            b.StackCount = StackedTileCount; // Compatibility ID boundary; blockers are spatial, not per-stack.
            int[] counts = b.Height == 3 ? new[] { 15, 9, 6 } : b.Height == 4 ? new[] { 12, 9, 6, 3 } :
                Enumerable.Range(0, b.Height).Select(l => 3 * (10 / b.Height + (l < 10 % b.Height ? 1 : 0))).ToArray();
            for (int layer = 0; layer < b.Height; layer++)
            {
                int columns = counts[layer] == 15 ? 5 : counts[layer] == 12 ? 4 : 3;
                int rows = counts[layer] / columns;
                float offsetX = layer == 0 ? 0 : layer % 2 == 1 ? -26 : 26;
                float left = (720 - (columns - 1) * 104 - TileWidth) / 2 + offsetX;
                float top = 622 - ((rows - 1) * 116 + TileHeight) / 2 + (layer % 2 == 1 ? 22 : 0) - layer * 9;
                for (int i = 0; i < counts[layer]; i++)
                    b.Tiles.Add(new Tile { Id = b.Tiles.Count, Stack = b.Tiles.Count, Layer = layer,
                        X = left + i % columns * 104, Y = top + i / columns * 116 });
            }
            for (int i = 0; i < FreeTileCount; i++) b.Tiles.Add(new Tile { Id = b.Tiles.Count, Stack = b.StackCount + i,
                Layer = 0, IsFree = true, X = 54 + i * 102, Y = 934 });
            b.Reconstruct(chapter, balance, random);
            return b;
        }

        // Construct a spatially legal removal path. Keep free tiles in reserve and
        // remove upper terraces first; each initial terrace contains a multiple of three.
        // Values are sampled with chapter weights, conditioned on a valid triple.
        public void Reconstruct(int chapter, GameBalance balance, Random random)
        {
            var pending = new HashSet<int>(Tiles.Where(t => !t.Removed).Select(t => t.Id));
            GuaranteedPath = new List<int[]>();
            while (pending.Count > 0)
            {
                var available = Tiles.Where(t => pending.Contains(t.Id) && !Tiles.Any(u => pending.Contains(u.Id) && Covers(u, t)))
                    .OrderBy(t => t.IsFree).ThenByDescending(t => t.Layer).ThenBy(t => random.Next()).Take(3).ToArray();
                if (available.Length < 3)
                {
                    // Spatially locked leftovers cannot form any legal triple. Recovery removes
                    // no tiles and keeps geometry: caller ends the run with an explicit error.
                    throw new InvalidOperationException("Remaining geometry has fewer than three exposed tiles.");
                }
                var triple = available;
                int a = 2, b = 3, c = 5;
                for (int attempt = 0; attempt < 1000; attempt++)
                {
                    int x = Sample(chapter, balance, random), y = Sample(chapter, balance, random), z = Sample(chapter, balance, random);
                    if (!TenRules.IsTen(x, y, z)) continue;
                    a = x; b = y; c = z; break;
                }
                triple[0].Value = a; triple[1].Value = b; triple[2].Value = c;
                GuaranteedPath.Add(triple.Select(t => t.Id).ToArray());
                foreach (var t in triple) pending.Remove(t.Id);
            }
        }

        static int Sample(int chapter, GameBalance b, Random r)
        {
            double roll = r.NextDouble();
            double zero = chapter >= 20 ? b.Chapter20ZeroWeight : chapter >= 10 ? b.Chapter10ZeroWeight : 0;
            double negative = chapter >= 20 ? b.Chapter20NegativeWeight : chapter >= 10 ? b.Chapter10NegativeWeight : 0;
            double twice = chapter >= 20 ? b.Chapter20DoubleWeight : 0;
            if (roll < zero) return 0;
            if (roll < zero + negative) return -1;
            if (roll < zero + negative + twice) return Double;
            return r.Next(1, 10);
        }

        public int[] FindMove()
        {
            var a = Available();
            for (int i = 0; i < a.Count - 2; i++)
                for (int j = i + 1; j < a.Count - 1; j++)
                    for (int k = j + 1; k < a.Count; k++)
                    {
                        var move = TenRules.OrderedTen(a[i], a[j], a[k]);
                        if (move != null) return move;
                    }
            return null;
        }

        public bool Shuffle(int chapter, GameBalance balance, Random random, out bool reconstructed)
        {
            reconstructed = false;
            var remaining = Tiles.Where(t => !t.Removed).ToList();
            var original = remaining.Select(t => t.Value).ToArray();
            for (int attempt = 0; attempt < balance.ShuffleAttempts; attempt++)
            {
                for (int i = remaining.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1), value = remaining[i].Value;
                    remaining[i].Value = remaining[j].Value; remaining[j].Value = value;
                }
                if (BoardSolver.TrySolve(this, balance.SolverNodeBudget, out var path))
                { GuaranteedPath = path; return true; }
            }
            // Bounded fallback changes values only, never positions or removal counters.
            // If geometry cannot be solved, restore the original labels and fail explicitly.
            if (balance.BoardRecovery == BoardRecoveryPolicy.EndRunIfSearchFails)
            {
                for (int i = 0; i < remaining.Count; i++) remaining[i].Value = original[i];
                return false;
            }
            try { Reconstruct(chapter, balance, random); reconstructed = true; return true; }
            catch (InvalidOperationException)
            {
                for (int i = 0; i < remaining.Count; i++) remaining[i].Value = original[i];
                return false;
            }
        }
    }

    public static class BoardSolver
    {
        public static bool ValidatePath(BoardModel board, IEnumerable<int[]> path)
        {
            ulong mask = 0;
            foreach (var t in board.Tiles) if (!t.Removed) mask |= 1UL << t.Id;
            var blockers = Blockers(board);
            foreach (var move in path)
            {
                if (move.Length != 3 || move.Distinct().Count() != 3) return false;
                foreach (int id in move)
                    if (id < 0 || id >= board.Tiles.Count || (mask & (1UL << id)) == 0 || (mask & blockers[id]) != 0) return false;
                if (!TenRules.IsTen(board.Tiles[move[0]].Value, board.Tiles[move[1]].Value, board.Tiles[move[2]].Value)) return false;
                foreach (int id in move) mask &= ~(1UL << id);
            }
            return mask == 0;
        }

        public static bool TrySolve(BoardModel board, int budget, out List<int[]> path)
        {
            if (board.GuaranteedPath != null && ValidatePath(board, board.GuaranteedPath))
            { path = new List<int[]>(board.GuaranteedPath); return true; }
            var output = new List<int[]>();
            var seen = new HashSet<ulong>();
            ulong mask = 0;
            foreach (var t in board.Tiles) if (!t.Removed) mask |= 1UL << t.Id;
            var blockers = Blockers(board);
            bool Search(ulong state)
            {
                if (state == 0) return true;
                if (--budget < 0 || !seen.Add(state)) return false;
                var a = board.Tiles.Where(t => (state & (1UL << t.Id)) != 0 && (state & blockers[t.Id]) == 0).ToArray();
                for (int i = 0; i < a.Length - 2; i++)
                    for (int j = i + 1; j < a.Length - 1; j++)
                        for (int k = j + 1; k < a.Length; k++)
                        {
                            if (budget < 0) return false;
                            var move = TenRules.OrderedTen(a[i], a[j], a[k]);
                            if (move == null) continue;
                            ulong next = state;
                            foreach (int id in move) next &= ~(1UL << id);
                            output.Add(move);
                            if (Search(next)) return true;
                            output.RemoveAt(output.Count - 1);
                        }
                return false;
            }
            bool solved = Search(mask); path = solved ? output : null; return solved;
        }

        static ulong[] Blockers(BoardModel board)
        {
            var result = new ulong[board.Tiles.Count];
            foreach (var t in board.Tiles)
                foreach (var other in board.Tiles)
                    if (BoardModel.Covers(other, t)) result[t.Id] |= 1UL << other.Id;
            return result;
        }
    }
}
