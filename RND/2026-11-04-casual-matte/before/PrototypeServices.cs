using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace TheTen
{
    // These adapters are deliberately labelled MOCK. They are not production trust boundaries.
    public sealed class MockServerClock : ITrustedServerClock
    {
        DateTimeOffset anchor = new DateTimeOffset(2026, 10, 31, 14, 59, 0, TimeSpan.Zero);
        readonly Stopwatch elapsed = Stopwatch.StartNew();
        public bool Online = true;
        public void AdvanceDay() { anchor = anchor.AddDays(1); }
        public Task<DateTimeOffset?> GetUtcNowAsync() => Task.FromResult<DateTimeOffset?>(Online ? anchor.Add(elapsed.Elapsed) : (DateTimeOffset?)null);
    }
    public sealed class UnavailableServerClock : ITrustedServerClock
    { public Task<DateTimeOffset?> GetUtcNowAsync() => Task.FromResult<DateTimeOffset?>(null); }
    public sealed class UnavailablePurchaseGateway : IPurchaseGateway
    {
        public Task<PurchaseVerification> PurchaseAndVerifyAsync() => Task.FromResult(new PurchaseVerification(false, false));
        public Task<PurchaseVerification> RestoreAndVerifyAsync() => Task.FromResult(new PurchaseVerification(false, false));
    }
    public sealed class MockPurchaseGateway : IPurchaseGateway
    {
        readonly string EntitlementKey;
        public MockPurchaseGateway(string prefix = "TheTen") { EntitlementKey = prefix + ".MockStore.NonConsumable"; }
        public bool RejectVerification;
        public Task<PurchaseVerification> PurchaseAndVerifyAsync()
        {
            if (RejectVerification) return Task.FromResult(new PurchaseVerification(false, false));
            PlayerPrefs.SetInt(EntitlementKey, 1); PlayerPrefs.Save();
            return Task.FromResult(new PurchaseVerification(true, true));
        }
        public Task<PurchaseVerification> RestoreAndVerifyAsync() => Task.FromResult(new PurchaseVerification(!RejectVerification, PlayerPrefs.GetInt(EntitlementKey) == 1));
    }
    public sealed class PlayerPrefsTicketStorage : ITicketStorage
    {
        readonly string key;
        public PlayerPrefsTicketStorage(string key) { this.key = key; }
        public TicketData Load() => JsonUtility.FromJson<TicketData>(PlayerPrefs.GetString(key, "{}"));
        public void Save(TicketData data) { PlayerPrefs.SetString(key, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
    }
    public interface IRewardedAdService { Task<MockAdOutcome> ShowAsync(); }
    public sealed class MockRewardedAdService : IRewardedAdService
    {
        readonly GameBalance balance;
        public MockRewardedAdService(GameBalance balance) { this.balance = balance; }
        public async Task<MockAdOutcome> ShowAsync()
        {
            var outcome = balance.AdOutcome;
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, balance.MockAdSeconds)));
            return outcome;
        }
    }
    public sealed class UnavailableAdService : IRewardedAdService
    { public Task<MockAdOutcome> ShowAsync() => Task.FromResult(MockAdOutcome.Unavailable); }

    public sealed class LocalRanking
    {
        readonly string key;
        // Separate key: old survival seconds must never be interpreted as points.
        public LocalRanking(string prefix = "TheTen") { key = prefix + ".Prototype.BestScore.v1"; }
        public int BestScore => PlayerPrefs.GetInt(key);
        public void Submit(int score)
        {
            if (score <= BestScore) return;
            PlayerPrefs.SetInt(key, score); PlayerPrefs.Save();
        }
    }
}
