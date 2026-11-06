using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class CleanupAndGamingTests
{
    private readonly LoggingService _logger;
    private readonly BackupRepository _backupRepo;
    private readonly OperationExecutor _opExecutor;
    private readonly CleanupService _cleanupService;
    private readonly TweakEngineService _tweakService;

    public CleanupAndGamingTests()
    {
        _logger = new LoggingService();
        _backupRepo = new BackupRepository();
        _opExecutor = new OperationExecutor(_logger, _backupRepo);
        _cleanupService = new CleanupService(_logger, _opExecutor);
        _tweakService = new TweakEngineService(_logger, _opExecutor);
    }

    [Fact]
    public void CleanupService_Catalog_ContainsEssentialTargets()
    {
        var targets = CleanupService.GetCatalogTargets();

        Assert.NotEmpty(targets);
        Assert.True(targets.Count >= 5);
        Assert.Contains(targets, t => t.Id == "user-temp");
        Assert.Contains(targets, t => t.Id == "windows-temp");
        Assert.Contains(targets, t => t.Id == "recycle-bin");
        Assert.Contains(targets, t => t.Id == "explorer-thumbnails");
        Assert.Contains(targets, t => t.Id == "crash-dumps");
    }

    [Fact]
    public async Task CleanupService_ScanCleanupTargets_PopulatesSizes()
    {
        var targets = await _cleanupService.ScanCleanupTargetsAsync();

        Assert.NotEmpty(targets);
        foreach (var target in targets)
        {
            Assert.True(target.TotalSizeBytes >= 0);
            Assert.True(target.FileCount >= 0);
            Assert.False(string.IsNullOrWhiteSpace(target.SizeFormatted));
        }
    }

    [Fact]
    public async Task CleanupService_CreateCleanupPlan_GeneratesValidPlan()
    {
        var targets = CleanupService.GetCatalogTargets();
        var plan = await _cleanupService.CreateCleanupPlanAsync(targets.Take(2), isDryRun: true);

        Assert.NotNull(plan);
        Assert.Equal(2, plan.Items.Count);
        Assert.True(plan.IsDryRun);
        Assert.All(plan.Items, item =>
        {
            Assert.Equal(OperationType.FileCleanup, item.OperationType);
            Assert.Equal(ReversibilityTier.Irreversible, item.Reversibility);
            Assert.False(item.RollbackSupported);
        });
    }

    [Fact]
    public async Task TweakEngine_ReturnsGamingTweaks()
    {
        var tweaks = await _tweakService.GetGamingTweaksAsync();

        Assert.NotEmpty(tweaks);
        Assert.Contains(tweaks, t => t.Id == "enable-game-mode");
        Assert.Contains(tweaks, t => t.Id == "disable-game-dvr-background-recording");
        Assert.Contains(tweaks, t => t.Id == "enable-hags");
        Assert.Contains(tweaks, t => t.Id == "optimize-window-animations");
    }

    [Fact]
    public void GamingTweaks_ValuesMatchWindowsDocumentedStandards()
    {
        var tweaks = TweakEngineService.GetSupportedGamingCatalog();

        var gameMode = tweaks.First(t => t.Id == "enable-game-mode");
        Assert.Equal(1, (int)gameMode.RecommendedValue!);

        var gameDvr = tweaks.First(t => t.Id == "disable-game-dvr-background-recording");
        Assert.Equal(0, (int)gameDvr.RecommendedValue!);

        var hags = tweaks.First(t => t.Id == "enable-hags");
        Assert.Equal(2, (int)hags.RecommendedValue!);
        Assert.True(hags.RequiresRestart);
    }
}

