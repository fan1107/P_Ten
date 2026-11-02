using UnityEngine;
using UnityEngine.Serialization;

namespace TheTen
{
    [CreateAssetMenu(menuName = "THE TEN/Game balance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Min(1)] public float InitialTime = 30;
        [Min(1)] public int MatchScore = 10;
        [Min(.1f)] public float ComboWindow = 3;
        [Min(0)] public float MatchBonus = 3;
        [Min(0)] public float MissPenalty = 3;
        [Min(0)] public float ShuffleBonus = 8;
        [Min(1)] public float ReviveTime = 30;
        [Tooltip("Provisional; <= 0 means unlimited.")] public float MaxRemainingTime = 90;
        [Tooltip("Provisional, not a finalized rule.")] public float BoardClearBonus = 0;
        [Range(3, 12)] public int MaxStackHeight = 7;
        [Min(100)] public int SolverNodeBudget = 12000;
        [Range(1, 20)] public int ShuffleAttempts = 6;
        [Min(0)] public float TransitionSeconds = .8f;
        [Min(0)] public float MockAdSeconds = 2;
        public MockAdOutcome AdOutcome = MockAdOutcome.Rewarded;
        public AdUnavailablePolicy UnavailablePolicy = AdUnavailablePolicy.StayOnReviveScreen;
        public bool EnableMockCommerce = true;
        [Header("Chapters 3–4")]
        [FormerlySerializedAs("Chapter10ZeroWeight")] public float EarlyZeroWeight = .08f;
        [FormerlySerializedAs("Chapter10NegativeWeight")] public float EarlyNegativeWeight = .08f;
        [Header("Chapter 5 onwards")]
        [FormerlySerializedAs("Chapter20ZeroWeight")] public float AdvancedZeroWeight = .05f;
        [FormerlySerializedAs("Chapter20NegativeWeight")] public float AdvancedNegativeWeight = .05f;
        [FormerlySerializedAs("Chapter20DoubleWeight")] public float AdvancedDoubleWeight = .05f;
    }
    public enum MockAdOutcome { Rewarded, Cancelled, Unavailable }
    public enum AdUnavailablePolicy { StayOnReviveScreen, EndRun }
}
