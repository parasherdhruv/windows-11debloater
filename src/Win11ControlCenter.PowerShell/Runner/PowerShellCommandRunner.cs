using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Win11ControlCenter.PowerShell.Runner;

public class PowerShellCommandRunner
{
    private static readonly Regex SafePackageNameRegex = new(@"^[a-zA-Z0-9\.\-_~]+$", RegexOptions.Compiled);

    public static async Task<(bool Success, string Output, string Error)> RunSafeCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode == 0, output.Trim(), error.Trim());
    }

    public static bool ValidatePackageName(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName)) return false;
        return SafePackageNameRegex.IsMatch(packageName);
    }
}

