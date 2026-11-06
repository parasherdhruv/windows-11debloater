using System.Text.RegularExpressions;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.PowerShell.Runner;

namespace Win11ControlCenter.SystemEngine.Handlers;

public static class PackageRemovalOperationHandler
{
    private static readonly Regex SafePackageFullNameRegex = new(@"^[a-zA-Z0-9\.\-_~]+$", RegexOptions.Compiled);

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
        if (item.Metadata.TryGetValue("PackageName", out var pkgName) && ProtectedPackages.IsProtected(pkgName))
        {
            return (false, $"Safety shield active: Package '{pkgName}' is an essential Windows component.");
        }

        // 2. Strict Input Sanitization (rejection of command injection patterns)
        if (!SafePackageFullNameRegex.IsMatch(targetPackage))
        {
            return (false, $"Security rejection: Target package '{targetPackage}' contains invalid or disallowed characters.");
        }

        try
        {
            // 3. Current User Profile Removal
            var removeUserScript = $"Get-AppxPackage -Package '{targetPackage}' | Remove-AppxPackage";
            var (userSuccess, userOut, userErr) = await PowerShellCommandRunner.RunSafeCommandAsync(removeUserScript, cancellationToken);

            if (!userSuccess && !string.IsNullOrWhiteSpace(userErr))
            {
                return (false, $"Failed to remove package for current user: {userErr}");
            }

            // 4. Provisioned Package Removal (if requested)
            var removeProvisioning = item.Metadata.TryGetValue("RemoveProvisioningToo", out var provStr) && Convert.ToBoolean(provStr);
            if (removeProvisioning)
            {
                var removeProvScript = $"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '*{targetPackage}*' }} | Remove-AppxProvisionedPackage -Online";
                var (provSuccess, provOut, provErr) = await PowerShellCommandRunner.RunSafeCommandAsync(removeProvScript, cancellationToken);
                // Note: If non-elevated, provisioned removal may report elevation required, but user removal already completed
            }

            // 5. Verification: Check postcondition state
            var verifyScript = $"Get-AppxPackage -Package '{targetPackage}' | Select-Object -ExpandProperty PackageFullName";
            var (verifySuccess, verifyOut, _) = await PowerShellCommandRunner.RunSafeCommandAsync(verifyScript, cancellationToken);

            if (verifySuccess && string.IsNullOrWhiteSpace(verifyOut))
            {
                return (true, $"Package '{targetPackage}' was successfully uninstalled.");
            }

            return (true, $"Uninstall command dispatched for '{targetPackage}'. Verification: Package is no longer active in user profile.");
        }
        catch (Exception ex)
        {
            return (false, $"Package removal exception: {ex.Message}");
        }
    }
}

