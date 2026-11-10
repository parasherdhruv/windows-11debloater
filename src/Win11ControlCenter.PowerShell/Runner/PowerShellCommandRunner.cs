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

        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            // 60-second safety timeout for automated command execution
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            var output = await outputTask;
            var error = await errorTask;

            return (process.ExitCode == 0, output.Trim(), error.Trim());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
            return (false, string.Empty, "Command execution timed out or was cancelled.");
        }
        catch (Exception ex)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
            return (false, string.Empty, $"Execution exception: {ex.Message}");
        }
    }

    public static bool ValidatePackageName(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName)) return false;
        return SafePackageNameRegex.IsMatch(packageName);
    }
}

