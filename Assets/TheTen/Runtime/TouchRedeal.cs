using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTen
{
    internal static class TouchRedeal
    {
        // Record a witness for every reachable sum. A numeric tile can be skipped,
        // tapped normally, or tapped before a single ×2 so it counts twice.
        internal static List<Tile> FindTen(List<Tile> tiles, int maxTiles = BoardModel.TileCount)
        {
            var twice = tiles.FirstOrDefault(t => t.IsDouble);
            var sums = new Dictionary<(int sum, bool doubled), (ulong before, ulong after, int count)>
                { [(0, false)] = (0, 0, 0) };
            void Remember(int sum, ulong before, ulong after, int count)
            {
                var key = (sum, before != 0);
                if (count <= maxTiles && (!sums.TryGetValue(key, out var old) || count < old.count))
                    sums[key] = (before, after, count);
            }
            foreach (var tile in tiles.Where(t => !t.IsDouble))
            {
                ulong bit = 1UL << tile.Id;
                foreach (var entry in sums.ToArray())
                {
                    var p = entry.Value;
                    Remember(entry.Key.sum + tile.Value, p.before, p.after | bit, p.count + 1);
                    if (twice != null)
                        Remember(entry.Key.sum + 2 * tile.Value, p.before | bit, p.after, p.count + (entry.Key.doubled ? 1 : 2));
                }
                if (!sums.TryGetValue((10, false), out var path) && !sums.TryGetValue((10, true), out path)) continue;
                var move = tiles.Where(t => (path.before & (1UL << t.Id)) != 0).ToList();
                if (move.Count > 0) move.Add(twice);
                move.AddRange(tiles.Where(t => (path.after & (1UL << t.Id)) != 0));
                // Selecting a shorter prefix which reaches ten clears immediately.
                int total = 0;
                int last = move.FindIndex(t => (total = t.IsDouble ? total * 2 : total + t.Value) == 10);
                return move.GetRange(0, last + 1);
            }
            return null;
        }

        internal static List<int[]> ShuffleValues(BoardModel board, Random random)
        {
            // Simulate removals in the original slots. Later swaps only touch the
            // pending tiles, so they cannot invalidate any earlier certified move.
            var pending = board.Tiles.Where(t => !t.Removed).OrderBy(t => random.Next()).ToList();
            foreach (var tile in pending.Where(t => t.Value == 0)) tile.Value = 1;
            var solution = new List<int[]>();
            while (pending.Count > 0)
            {
                var exposed = pending.Where(t => !pending.Any(upper => BoardModel.Covers(upper, t)))
                    .OrderByDescending(t => t.Layer).ToList();
                var move = FindTen(exposed);
                if (move == null && exposed.Count < pending.Count)
                {
                    move = FindTen(pending, exposed.Count);
                    if (move != null) BringValuesToSurface(move, exposed);
                }
                if (move == null)
                {
                    int count = Math.Min(4, exposed.Count);
                    if (count >= 3 && pending.Count - count == 1) count--;
                    move = exposed.GetRange(0, count);
                    CompleteGroup(move);
                }
                solution.Add(move.Select(t => t.Id).ToArray());
                pending.RemoveAll(move.Contains);
            }
            return solution;
        }

        static void BringValuesToSurface(List<Tile> move, List<Tile> exposed)
        {
            var freeSlots = new Queue<Tile>(exposed.Except(move));
            for (int i = 0; i < move.Count; i++)
            {
                var source = move[i];
                if (exposed.Contains(source)) continue;
                var target = freeSlots.Dequeue();
                (source.Value, target.Value) = (target.Value, source.Value);
                move[i] = target;
            }
        }

        static void CompleteGroup(List<Tile> group)
        {
            if (group.Count == 1) { group[0].Value = 10; return; }
            // Use ×2 last. Keep -1 tiles whenever the other slots can reach the target.
            var twice = group.FirstOrDefault(t => t.IsDouble);
            if (twice != null) group.Remove(twice);
            foreach (var extra in group.Where(t => t.IsDouble)) extra.Value = 1;
            int target = twice == null ? 10 : 5;
            int maximum = group.Sum(t => t.Value == -1 ? -1 : 9);
            foreach (var minus in group.Where(t => t.Value == -1))
            {
                if (maximum >= target) break;
                minus.Value = 1;
                maximum += 10; // This slot can now hold 9 instead of -1.
            }
            int difference = target - group.Sum(t => t.Value);
            foreach (var tile in group.Where(t => t.Value > 0))
            {
                if (difference == 0) break;
                int next = Math.Max(1, Math.Min(9, tile.Value + difference));
                difference -= next - tile.Value;
                tile.Value = next;
            }
            // Negative numbers go first so no proper prefix clears ahead of the group.
            group.Sort((a, b) => a.Value.CompareTo(b.Value));
            if (twice != null) group.Add(twice);
        }
    }
}
