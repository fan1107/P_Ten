using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTen
{
    public enum RunState { Playing, Paused, ReviveChoice, AdPlaying, BoardTransition, Shuffling, GameOver }
    public enum SelectionResult { Ignored, Selected, Deselected, Miss, Ten }

    public sealed class RunModel
    {
        readonly GameBalance balance;
        readonly Random random;
        static readonly double[] ComboMultipliers = { 1, 1.5, 2, 2.5, 3, 4, 5 };
        int comboIndex = -1;
        public int Score { get; private set; }
        public int LastMatchScore { get; private set; }
        public int LastMatchBonusMultiplier { get; private set; } = 1;
        public double ComboRemaining { get; private set; }
        public double ComboMultiplier => ComboMultipliers[Math.Max(0, comboIndex)];
        public BoardModel Board { get; private set; }
        public RunState State { get; private set; }
        public readonly List<int> Selection = new List<int>();
        public double RemainingTime { get; private set; }
        public double FirstSurvivalTime { get; private set; }
        public double SecondSurvivalTime { get; private set; }
        public double TotalSurvivalTime => FirstSurvivalTime + SecondSurvivalTime;
        public int TotalRemovedTiles { get; private set; }
        public int SuccessCount { get; private set; }
        public bool ReviveUsed { get; private set; }
        public int Chapter { get; private set; } = 1;

        public RunModel(GameBalance settings, int seed)
        {
            balance = settings; random = new Random(seed);
            Board = BoardModel.Create(Chapter, balance, random);
            RemainingTime = Limit(balance.InitialTime); State = RunState.Playing;
        }
        double Limit(double value) => balance.MaxRemainingTime > 0 ? Math.Min(value, balance.MaxRemainingTime) : value;
        public void Tick(double elapsed)
        {
            if (State != RunState.Playing || elapsed <= 0 || double.IsNaN(elapsed) || double.IsInfinity(elapsed)) return;
            double actual = Math.Min(RemainingTime, elapsed);
            RemainingTime -= actual;
            ComboRemaining = Math.Max(0, ComboRemaining - actual);
            if (ComboRemaining <= 0) comboIndex = -1;
            if (ReviveUsed) SecondSurvivalTime += actual; else FirstSurvivalTime += actual;
            if (RemainingTime <= 0) ExpireLife();
        }
        void ExpireLife()
        {
            Selection.Clear(); ResetCombo(); State = ReviveUsed ? RunState.GameOver : RunState.ReviveChoice;
        }
        public bool CancelSelection()
        {
            if (State != RunState.Playing || Selection.Count == 0) return false;
            Selection.Clear(); return true;
        }
        public SelectionResult Select(int id)
        {
            if (State != RunState.Playing || id < 0 || id >= Board.Tiles.Count || !Board.IsAvailable(id)) return SelectionResult.Ignored;
            if (Selection.Contains(id))
            {
                Selection.Remove(id);
                // Removing the first numeric tile must not leave an operator in first place.
                if (Selection.Count > 0 && Board.Tiles[Selection[0]].IsDouble) Selection.Clear();
                return SelectionResult.Deselected;
            }
            if (Board.Tiles[id].IsDouble && (Selection.Count == 0 || Selection.Any(i => Board.Tiles[i].IsDouble))) return SelectionResult.Ignored;
            Selection.Add(id);
            if (Selection.Count < 3) return SelectionResult.Selected;
            bool ten = TenRules.IsTen(Board.Tiles[Selection[0]].Value, Board.Tiles[Selection[1]].Value, Board.Tiles[Selection[2]].Value);
            if (ten)
            {
                foreach (int selected in Selection) Board.Tiles[selected].Removed = true;
                TotalRemovedTiles += 3; SuccessCount++;
                comboIndex = ComboRemaining > 0 ? Math.Min(comboIndex + 1, ComboMultipliers.Length - 1) : 0;
                LastMatchBonusMultiplier = TenRules.SpecialScoreMultiplier(Selection.Select(i => Board.Tiles[i]));
                LastMatchScore = (int)Math.Round(balance.MatchScore * ComboMultiplier * LastMatchBonusMultiplier, MidpointRounding.AwayFromZero);
                Score += LastMatchScore;
                ComboRemaining = balance.ComboWindow;
                RemainingTime = Limit(RemainingTime + balance.MatchBonus);
            }
            else
            {
                // A penalty is not elapsed play time and does not advance the combo clock.
                RemainingTime = Math.Max(0, RemainingTime - Math.Max(0, balance.MissPenalty));
                if (RemainingTime <= 0) ExpireLife();
            }
            Selection.Clear();
            if (ten)
            {
                if (Board.Remaining == 0) State = RunState.BoardTransition;
                else if (Board.FindMove() == null) State = RunState.Shuffling;
            }
            return ten ? SelectionResult.Ten : SelectionResult.Miss;
        }
        public bool CompleteTransition(out bool recovered)
        {
            recovered = false;
            if (State == RunState.BoardTransition)
            {
                Board = BoardModel.Create(Chapter + 1, balance, random);
                Chapter++;
                RemainingTime = Limit(RemainingTime + balance.BoardClearBonus);
            }
            else if (State == RunState.Shuffling)
            {
                if (!Board.Shuffle(Chapter, balance, random, out recovered)) { State = RunState.GameOver; return false; }
                RemainingTime = Limit(RemainingTime + balance.ShuffleBonus);
            }
            else return false;
            State = RunState.Playing; return true;
        }
        public void Pause() { if (State == RunState.Playing) State = RunState.Paused; }
        public void Resume() { if (State == RunState.Paused) State = RunState.Playing; }
        public bool BeginAd() { if (State != RunState.ReviveChoice || ReviveUsed) return false; State = RunState.AdPlaying; return true; }
        public void CancelAd() { if (State == RunState.AdPlaying) State = RunState.ReviveChoice; }
        public bool Revive()
        {
            if (ReviveUsed || (State != RunState.ReviveChoice && State != RunState.AdPlaying)) return false;
            ReviveUsed = true; RemainingTime = Limit(balance.ReviveTime); State = RunState.Playing; return true;
        }
        void ResetCombo() { ComboRemaining = 0; comboIndex = -1; }
        public void End() { State = RunState.GameOver; Selection.Clear(); ResetCombo(); }
    }
}
