using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTen
{
    public static class TouchStages
    {
        public const int Count = 20;

        public static int TapLimit(int stage)
        {
            if (stage < 1 || stage > Count) throw new ArgumentOutOfRangeException(nameof(stage));
            return 44 - (6 * (stage - 1) + 9) / (Count - 1);
        }

        public static BoardModel Create(int stage, GameBalance balance, out List<int[]> solution)
        {
            TapLimit(stage); // Validate before constructing the board.
            var board = BoardModel.CreateLayout(stage, balance);
            var random = new Random(1091 + stage * 7919);
            solution = new List<int[]>();
            // Each group is exposed together when all higher layers have been cleared.
            foreach (var layer in board.Tiles.GroupBy(t => t.Layer).OrderByDescending(g => g.Key))
            {
                var ids = layer.OrderBy(t => random.Next()).Select(t => t.Id).ToArray();
                for (int start = 0; start < ids.Length;)
                {
                    int left = ids.Length - start;
                    int size;
                    if (left <= 4) size = left;
                    else if (start == 0 && left >= 6) size = 4;
                    else
                    {
                        var sizes = Enumerable.Range(2, Math.Min(5, left) - 1)
                            .Where(n => left - n != 1).ToArray();
                        size = sizes[random.Next(sizes.Length)];
                    }
                    solution.Add(ids.Skip(start).Take(size).ToArray());
                    start += size;
                }
            }

            var specials = new int?[solution.Count];
            if (stage >= 3)
            {
                // Reserve the required specials before sampling extra occurrences.
                var eligible = solution.Select((ids, i) => ids.Length >= 3 ? i : -1)
                    .Where(i => i >= 0).OrderBy(i => random.Next()).ToArray();
                specials[eligible[0]] = -1;
                if (stage >= 5)
                {
                    var free = Enumerable.Range(0, specials.Length).Where(i => !specials[i].HasValue).ToArray();
                    specials[free[random.Next(free.Length)]] = BoardModel.Double;
                }
            }
            for (int i = 0; i < solution.Count; i++)
            {
                var ids = solution[i];
                int? special = specials[i];
                if (!special.HasValue && stage >= 3)
                {
                    double roll = random.NextDouble();
                    double minus = stage >= 5 ? balance.AdvancedNegativeWeight : balance.EarlyNegativeWeight;
                    if (ids.Length >= 3 && roll < minus) special = -1;
                    else if (stage >= 5 && roll >= minus && roll < minus + balance.AdvancedDoubleWeight)
                        special = BoardModel.Double;
                }
                AssignValues(board, ids, special, random);
            }
            return board;
        }

        static void AssignValues(BoardModel board, int[] ids, int? special, Random random)
        {
            // Place -1 first or ×2 last so no prefix of the intended move clears early.
            int start = 0, end = ids.Length, remaining = 10;
            if (special == BoardModel.Double)
            {
                board.Tiles[ids[--end]].Value = BoardModel.Double;
                remaining = 5;
            }
            else if (special.HasValue)
            {
                board.Tiles[ids[start++]].Value = special.Value;
                remaining -= special.Value;
            }
            for (int i = start; i < end; i++)
            {
                int after = end - i - 1;
                int min = Math.Max(1, remaining - 9 * after);
                int max = Math.Min(9, remaining - after);
                int value = random.Next(min, max + 1);
                board.Tiles[ids[i]].Value = value;
                remaining -= value;
            }
        }
    }
}
