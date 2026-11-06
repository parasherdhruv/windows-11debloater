namespace Win11ControlCenter.Core.Rules;

public static class ProtectedPackages
{
    private static readonly HashSet<string> ExactMatches = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.Windows.ShellExperienceHost",
        "Microsoft.Windows.StartMenuExperienceHost",
        "Microsoft.Windows.Search",
        "Microsoft.Windows.SecHealthUI",
        "Microsoft.WindowsStore",
        "Microsoft.DesktopAppInstaller",
        "Microsoft.StorePurchaseApp",
        "Microsoft.XboxIdentityProvider",
        "Microsoft.AAD.BrokerPlugin",
        "Microsoft.AccountsControl",
        "Microsoft.BioEnrollment",
        "Microsoft.CredDialogHost",
        "Microsoft.ECApp",
        "Microsoft.LockApp",
        "Microsoft.Win32WebViewHost",
        "Microsoft.EdgeWebView2",
        "Microsoft.Windows.CloudExperienceHost",
        "Microsoft.Windows.Apprep.ChxApp",
        "Microsoft.Windows.ContentDeliveryManager",
        "Microsoft.Windows.OOBENetworkCaptivePortal",
        "Microsoft.Windows.OOBENetworkConnectionFlow",
        "Microsoft.Windows.ParentalControls",
        "Microsoft.Windows.PeopleExperienceHost",
        "Microsoft.Windows.PinningConfirmationDialog",
        "Microsoft.Windows.SecureAssessmentBrowser",
        "Microsoft.Windows.XGpuEjectDialog"
    };

    private static readonly string[] SubstringPrefixes =
    {
        "Microsoft.VCLibs",
        "Microsoft.NET.Native",
        "Microsoft.UI.Xaml",
        "Microsoft.DirectX",
        "Microsoft.Services.Store.Engagement",
        "Microsoft.Advertising.Xaml"
    };

    public static bool IsProtected(string packageNameOrFamily)
    {
        if (string.IsNullOrWhiteSpace(packageNameOrFamily))
            return true;

        if (ExactMatches.Contains(packageNameOrFamily))
            return true;

        foreach (var prefix in SubstringPrefixes)
        {
            if (packageNameOrFamily.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Additional safeguard: If it contains core windows security or shell components
        if (packageNameOrFamily.Contains("SecHealthUI", StringComparison.OrdinalIgnoreCase) ||
            packageNameOrFamily.Contains("ShellExperienceHost", StringComparison.OrdinalIgnoreCase) ||
            packageNameOrFamily.Contains("StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase) ||
            packageNameOrFamily.Contains("WindowsStore", StringComparison.OrdinalIgnoreCase) ||
            packageNameOrFamily.Contains("DesktopAppInstaller", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

