using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Core.Rules;

public static class SafetyValidator
{
    public static (bool IsValid, string Reason) ValidateOperation(OperationItem item)
    {
        if (item == null)
            return (false, "Operation item cannot be null.");

        if (string.IsNullOrWhiteSpace(item.Target))
            return (false, "Operation target cannot be empty.");

        switch (item.OperationType)
        {
            case OperationType.PackageRemoval:
                if (ProtectedPackages.IsProtected(item.Target))
                {
                    return (false, $"Package '{item.Target}' is a protected Windows system component and cannot be removed.");
                }
                break;

            case OperationType.Service:
                if (CoreWindowsServices.IsProtectedService(item.Target))
                {
                    return (false, $"Service '{item.Target}' is a core Windows infrastructure component and cannot be stopped or disabled.");
                }
                break;

            case OperationType.FileCleanup:
                var trimmed = item.Target.Trim().TrimEnd('\\');
                if (trimmed.Length <= 3 && trimmed.Contains(':'))
                {
                    return (false, $"Drive root '{item.Target}' cannot be targeted for cleanup.");
                }

                var forbiddenPaths = new[]
                {
                    "Windows\\System32",
                    "Windows\\WinSxS",
                    "Windows\\Installer",
                    "Windows\\Prefetch",
                    "Program Files",
                    "Program Files (x86)",
                    "Documents",
                    "Desktop",
                    "Downloads",
                    "Pictures",
                    "Music",
                    "Videos"
                };

                foreach (var forbidden in forbiddenPaths)
                {
                    if (item.Target.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, $"Target path '{item.Target}' contains protected user or system directory and is excluded from automatic cleanup.");
                    }
                }
                break;

            case OperationType.Registry:
                if (string.IsNullOrWhiteSpace(item.Target))
                    return (false, "Registry target cannot be empty.");

                var forbiddenRegPaths = new[]
                {
                    "SAM",
                    "SECURITY",
                    "BCD00000000",
                    "CurrentControlSet\\Control\\SafeBoot",
                    "Session Manager\\KnownDLLs"
                };

                foreach (var forbidden in forbiddenRegPaths)
                {
                    if (item.Target.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, $"Target registry path '{item.Target}' touches critical system security/boot structures and is protected.");
                    }
                }
                break;
        }

        return (true, "Valid");
    }

    public static (bool IsValid, List<string> Violations) ValidatePlan(OperationPlan plan)
    {
        var violations = new List<string>();
        if (plan == null)
        {
            violations.Add("Plan cannot be null.");
            return (false, violations);
        }

        foreach (var item in plan.Items)
        {
            var (isValid, reason) = ValidateOperation(item);
            if (!isValid)
            {
                violations.Add($"[{item.Name}]: {reason}");
            }
        }

        return (violations.Count == 0, violations);
    }
}

