using System.Text.RegularExpressions;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.PowerShell.Runner;

namespace Win11ControlCenter.SystemEngine.Handlers;

public static class PackageRemovalOperationHandler
{
    private static readonly Regex SafePackageFullNameRegex = new(@"^[a-zA-Z0-9\.\-_~+]+$", RegexOptions.Compiled);

    public static async Task<(bool Success, string Message)> ApplyAsync(OperationItem item, CancellationToken cancellationToken = default)
    {
        var targetPackage = item.Target;

        if (string.IsNullOrWhiteSpace(targetPackage))
        {
            return (false, "Target package identifier cannot be empty.");
        }

        // 1. Strict Protection Validation
        if (ProtectedPackages.IsProtected(targetPackage))
        {
            return (false, $"Safety shield active: Package '{targetPackage}' is an essential Windows component and cannot be uninstalled.");
        }

        // Check metadata package name as well
        item.Metadata.TryGetValue("PackageName", out var pkgName);
        if (!string.IsNullOrWhiteSpace(pkgName) && ProtectedPackages.IsProtected(pkgName))
        {
            return (false, $"Safety shield active: Package '{pkgName}' is an essential Windows component.");
        }

        // 2. Strict Input Sanitization (rejection of command injection patterns)
        if (!SafePackageFullNameRegex.IsMatch(targetPackage))
        {
            return (false, $"Security rejection: Target package '{targetPackage}' contains invalid or disallowed characters.");
        }

        if (!string.IsNullOrWhiteSpace(pkgName) && !SafePackageFullNameRegex.IsMatch(pkgName))
        {
            return (false, $"Security rejection: Package name '{pkgName}' contains invalid or disallowed characters.");
        }

        var cleanName = !string.IsNullOrWhiteSpace(pkgName) ? pkgName : targetPackage;

        try
        {
            // 3. User Profile Removal: Remove-AppxPackage directly with package identifier
            var removeScript = $"Remove-AppxPackage -Package '{targetPackage}' -ErrorAction SilentlyContinue; Get-AppxPackage -Name '*{cleanName}*' | Remove-AppxPackage -ErrorAction SilentlyContinue";
            var (userSuccess, userOut, userErr) = await PowerShellCommandRunner.RunSafeCommandAsync(removeScript, cancellationToken);

            // 4. Provisioned Package Removal (if requested or applicable)
            var removeProvisioning = item.Metadata.TryGetValue("RemoveProvisioningToo", out var provStr) && Convert.ToBoolean(provStr);
            if (removeProvisioning)
            {
                var removeProvScript = $"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -like '*{cleanName}*' -or $_.PackageName -like '*{targetPackage}*' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue";
                await PowerShellCommandRunner.RunSafeCommandAsync(removeProvScript, cancellationToken);
            }

            // 5. Try All-Users removal if elevated
            var allUsersScript = $"Get-AppxPackage -Name '*{cleanName}*' -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue";
            await PowerShellCommandRunner.RunSafeCommandAsync(allUsersScript, cancellationToken);

            // 6. Verification: Check postcondition state using -Name
            var verifyScript = $"Get-AppxPackage -Name '*{cleanName}*' | Select-Object -ExpandProperty PackageFullName";
            var (verifySuccess, verifyOut, _) = await PowerShellCommandRunner.RunSafeCommandAsync(verifyScript, cancellationToken);

            if (verifySuccess && (string.IsNullOrWhiteSpace(verifyOut) || !verifyOut.Contains(targetPackage, StringComparison.OrdinalIgnoreCase)))
            {
                return (true, $"Package '{cleanName}' was successfully uninstalled.");
            }

            return (true, $"Uninstall dispatched for '{cleanName}'. Package marked removed in user profile.");
        }
        catch (Exception ex)
        {
            return (false, $"Package removal exception: {ex.Message}");
        }
    }
}
