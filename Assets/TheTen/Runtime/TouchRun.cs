using System.Collections.Generic;
using System.Linq;

namespace TheTen
{
    public enum TouchStageState { Playing, Cleared, OutOfTaps }

    public sealed class TouchRun
    {
        readonly List<int> selection = new List<int>();
        readonly int matchScore;
        public int Stage { get; }
        public BoardModel Board { get; }
        public int TapsLeft { get; private set; }
        public int Total { get; private set; }
        public int Score { get; private set; }
        public IReadOnlyList<int> Selection => selection;
        public TouchStageState State { get; private set; } = TouchStageState.Playing;

        public TouchRun(int stage, GameBalance balance)
        {
            Stage = stage;
            TapsLeft = TouchStages.TapLimit(stage);
            Board = TouchStages.Create(stage, balance, out _);
            matchScore = balance.MatchScore;
        }

        public void CancelSelection()
        {
            if (State != TouchStageState.Playing) return;
            selection.Clear();
            Total = 0;
        }

        public SelectionResult Select(int id) => Select(id, out _);

        public SelectionResult Select(int id, out bool rearranged) => Select(id, out rearranged, out _);

        internal SelectionResult Select(int id, out bool rearranged, out int[] matchedIds)
        {
            rearranged = false;
            matchedIds = null;
            if (State != TouchStageState.Playing || id < 0 || id >= Board.Tiles.Count || !Board.IsAvailable(id))
                return SelectionResult.Ignored;

            var tile = Board.Tiles[id];
            bool undo = selection.Remove(id);
            if (undo)
            {
                if (selection.Count > 0 && Board.Tiles[selection[0]].IsDouble) selection.Clear();
            }
            else
            {
                if (tile.IsDouble && (selection.Count == 0 || selection.Any(i => Board.Tiles[i].IsDouble)))
                    return SelectionResult.Ignored;
                selection.Add(id);
                TapsLeft--;
            }

            Total = 0;
            foreach (int selected in selection)
            {
                var item = Board.Tiles[selected];
                Total = item.IsDouble ? Total * 2 : Total + item.Value;
            }
            if (Total == 10)
            {
                matchedIds = selection.ToArray();
                Score += matchScore * TenRules.SpecialScoreMultiplier(selection.Select(i => Board.Tiles[i]));
                foreach (int selected in selection) Board.Tiles[selected].Removed = true;
                CancelSelection();
                if (Board.Remaining == 0) State = TouchStageState.Cleared;
                else if (TapsLeft == 0) State = TouchStageState.OutOfTaps;
                else if (TouchRedeal.FindTen(Board.Available()) == null)
                {
                    Board.ShuffleTouchValues(new System.Random(Stage * 7919 + Board.Remaining));
                    rearranged = true;
                }
                return SelectionResult.Ten;
            }
            if (TapsLeft == 0) State = TouchStageState.OutOfTaps;
            return undo ? SelectionResult.Deselected : SelectionResult.Selected;
        }

    }
}
