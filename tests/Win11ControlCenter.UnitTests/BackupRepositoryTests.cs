using System.IO;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class BackupRepositoryTests
{
    [Fact]
    public void BackupRepository_CreatesSessionAndFiles()
    {
        var repo = new BackupRepository();
        var plan = new OperationPlan
        {
            Title = "Unit Test Plan",
            ProfileName = "TestProfile",
            Items = new List<OperationItem>
            {
                new()
                {
                    Name = "Test Registry Tweak",
                    OperationType = OperationType.Registry,
                    Target = @"HKCU\Software\TestKey",
                    OriginalValue = 1,
                    RecommendedValue = 0,
                    Metadata = new() { { "ValueName", "TestValue" } }
                }
            }
        };

        var sessionDir = repo.CreateBackupSession(plan, "26300");

        Assert.True(Directory.Exists(sessionDir));
        Assert.True(File.Exists(Path.Combine(sessionDir, "metadata.json")));
        Assert.True(File.Exists(Path.Combine(sessionDir, "operations.json")));
        Assert.True(File.Exists(Path.Combine(sessionDir, "registry", "pre_apply_rollback.reg")));

        var regContent = File.ReadAllText(Path.Combine(sessionDir, "registry", "pre_apply_rollback.reg"));
        Assert.Contains("Windows Registry Editor Version 5.00", regContent);
        Assert.Contains("[HKCU\\Software\\TestKey]", regContent);
        Assert.Contains("\"TestValue\"=dword:00000001", regContent);

        // Cleanup test backup
        try { Directory.Delete(sessionDir, true); } catch { }
    }
}

