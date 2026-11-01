using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace TheTen
{
    public enum TicketSyncState { RecheckRequired, Ready }
    [Serializable]
    public sealed class TicketData
    {
        public bool HasAdFreeRevivePurchase;
        public int CurrentTicketCount;
        public string LastResetDate = "";
        public string LastConsumedRun = "";
        public TicketData Copy() => (TicketData)MemberwiseClone();
    }
    public interface ITicketStorage { TicketData Load(); void Save(TicketData data); }
    // Production implementation must obtain authenticated, fresh backend time, never device time.
    public interface ITrustedServerClock { Task<DateTimeOffset?> GetUtcNowAsync(); }
    // Production gateway validates receipts on the backend and binds entitlement to the account.
    // Only a verified result may set Owned; cancellation/network errors are not revocations.
    public readonly struct PurchaseVerification
    {
        public readonly bool Verified, Owned;
        public PurchaseVerification(bool verified, bool owned) { Verified = verified; Owned = owned; }
    }
    public interface IPurchaseGateway
    {
        Task<PurchaseVerification> PurchaseAndVerifyAsync();
        Task<PurchaseVerification> RestoreAndVerifyAsync();
    }

    public sealed class ReviveTicketManager
    {
        public const int DailyTicketAmount = 2;
        public const int MaxTicketCount = 2;
        public const string ResetTimezone = "Asia/Seoul";
        public const int ReferencePriceKrw = 9000;
        readonly ITrustedServerClock clock;
        readonly ITicketStorage storage;
        readonly IPurchaseGateway purchase;
        readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        TicketData data;
        public bool HasAdFreeRevivePurchase => data.HasAdFreeRevivePurchase;
        public int CurrentTicketCount => data.CurrentTicketCount;
        public string LastResetDate => data.LastResetDate;
        public TicketSyncState SyncState { get; private set; } = TicketSyncState.RecheckRequired;

        public ReviveTicketManager(ITrustedServerClock clock, ITicketStorage storage, IPurchaseGateway purchase)
        {
            this.clock = clock; this.storage = storage; this.purchase = purchase;
            try { data = storage.Load() ?? new TicketData(); } catch { data = new TicketData(); }
            data.CurrentTicketCount = Math.Max(0, Math.Min(MaxTicketCount, data.CurrentTicketCount));
            if (!data.HasAdFreeRevivePurchase) data.CurrentTicketCount = 0;
        }
        // Seoul uses UTC+09:00 without DST. Explicit offset also works on mobile devices
        // that do not ship the IANA timezone database. No DateTime.Now/UtcNow is consulted.
        public static string SeoulDate(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(9)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        async Task<bool> RefreshLockedAsync()
        {
            SyncState = TicketSyncState.RecheckRequired;
            var now = await clock.GetUtcNowAsync();
            if (!now.HasValue) return false;
            string today = SeoulDate(now.Value);
            if (!string.IsNullOrEmpty(data.LastResetDate))
            {
                if (!DateTime.TryParseExact(data.LastResetDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
                // Reject stale responses / backward jumps rather than issuing a second grant.
                if (string.CompareOrdinal(today, data.LastResetDate) < 0) return false;
            }
            if (data.HasAdFreeRevivePurchase && today != data.LastResetDate)
            {
                var next = data.Copy(); next.CurrentTicketCount = DailyTicketAmount; next.LastResetDate = today;
                Commit(next);
            }
            SyncState = TicketSyncState.Ready; return true;
        }
        void Commit(TicketData next) { storage.Save(next); data = next; }
        public async Task<bool> RefreshAsync()
        {
            await gate.WaitAsync();
            try { return await RefreshLockedAsync(); }
            catch { SyncState = TicketSyncState.RecheckRequired; return false; }
            finally { gate.Release(); }
        }
        public async Task<bool> PurchaseAsync() => await VerifyAsync(false);
        public async Task<bool> RestoreAsync() => await VerifyAsync(true);
        async Task<bool> VerifyAsync(bool restore)
        {
            await gate.WaitAsync();
            try
            {
                var result = restore ? await purchase.RestoreAndVerifyAsync() : await purchase.PurchaseAndVerifyAsync();
                if (!result.Verified) { SyncState = TicketSyncState.RecheckRequired; return false; }
                var next = data.Copy(); next.HasAdFreeRevivePurchase = result.Owned;
                if (!result.Owned) next.CurrentTicketCount = 0;
                Commit(next);
                return await RefreshLockedAsync();
            }
            catch { SyncState = TicketSyncState.RecheckRequired; return false; }
            finally { gate.Release(); }
        }
        public async Task<bool> TryConsumeAsync(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false;
            await gate.WaitAsync();
            try
            {
                if (!await RefreshLockedAsync() || !data.HasAdFreeRevivePurchase || data.CurrentTicketCount <= 0 || data.LastConsumedRun == runId) return false;
                var next = data.Copy(); next.CurrentTicketCount--; next.LastConsumedRun = runId;
                Commit(next); return true;
            }
            catch { SyncState = TicketSyncState.RecheckRequired; return false; }
            finally { gate.Release(); }
        }
    }
}
