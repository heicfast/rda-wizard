using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.Settings;
using RdaWizard.Core.ViewModels;
using Xunit;

namespace RdaWizard.Core.Tests.ViewModels;

/// <summary>F-C-4 space gate: needed-bytes + continue-anyway (soft), the og
/// "Insufficient Disk Space" semantics — never a silent hard refusal.</summary>
public sealed class RecoverySpaceGateTests
{
    private sealed class FakeRecovery : IRecoveryService
    {
        public Task<IReadOnlyList<string>> PreFlightAsync(RecoveryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<RecoverySummary> RecoverAsync(RecoveryRequest request, IProgress<RecoveryProgress>? progress, CancellationToken cancellationToken)
            => Task.FromResult(new RecoverySummary
            {
                Recovered = 0, Refused = 0, Errors = 0, HashMismatch = 0,
                Outcome = RecoveryOutcome.Success, Elapsed = TimeSpan.Zero,
            });

        public Task<string> ExportReportAsync(RecoverySummary summary, RecoveryRequest request, string destinationFile, CancellationToken cancellationToken = default)
            => Task.FromResult("report");
    }

    private sealed class FakeSessions : ISessionService
    {
        public Task<SessionLoadResult> LoadAsync(string sessionPath, CancellationToken cancellationToken = default)
            => Task.FromResult(new SessionLoadResult { Success = false, Session = null, Diagnostic = null });

        public Task<IReadOnlyList<SessionInfo>> ListRecentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SessionInfo>>([]);

        public string SessionsDirectory => "/tmp/sessions";
    }

    private sealed class FakeDialogs : IDialogService
    {
        public Task<DialogResult> ConfirmAsync(string title, string message, string confirmLabel, bool destructive = false)
            => Task.FromResult(DialogResult.Confirmed);

        public Task AlertAsync(string title, string message) => Task.CompletedTask;

        public Task<string?> PickFolderAsync(string title) => Task.FromResult((string?)null);

        public Task<string?> PickOpenFileAsync(string title, string filter) => Task.FromResult((string?)null);
    }

    private static RecoveryViewModel Build()
    {
        var settings = new SettingsService(NullLogger<SettingsService>.Instance, Path.Combine(Path.GetTempPath(), "sg-" + Guid.NewGuid().ToString("N")));
        return new RecoveryViewModel(
            NullLogger<RecoveryViewModel>.Instance,
            new FakeRecovery(), new FakeSessions(), new FakeDialogs(), settings);
    }

    [Fact]
    public void SpaceGate_NoDestination_IsNeutral()
    {
        var vm = Build();
        vm.RefreshSpaceGate();
        Assert.False(vm.InsufficientSpace);
        Assert.Null(vm.DestinationFreeBytes);
    }

    [Fact]
    public void SpaceGate_NothingNeeded_NeverInsufficient()
    {
        var vm = Build();
        vm.DestinationDirectory = Path.GetTempPath();
        vm.NeededBytes = 0;
        vm.RefreshSpaceGate();
        Assert.False(vm.InsufficientSpace);
        Assert.NotNull(vm.DestinationFreeBytes);
    }

    [Fact]
    public void SpaceGate_ImpossibleNeed_FlagsInsufficient()
    {
        var vm = Build();
        vm.DestinationDirectory = Path.GetTempPath();
        vm.NeededBytes = long.MaxValue / 4; // no host has this free
        vm.RefreshSpaceGate();
        Assert.True(vm.InsufficientSpace);
    }
}
