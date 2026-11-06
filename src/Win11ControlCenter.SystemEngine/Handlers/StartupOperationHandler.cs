using Microsoft.Win32;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.SystemEngine.Handlers;

public static class StartupOperationHandler
{
    private const string CurrentUserRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string CurrentUserStartupApproved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static (bool Success, string Message) Apply(OperationItem item)
    {
        try
        {
            var enable = Convert.ToBoolean(item.RecommendedValue ?? false);
            var entryName = item.Target;

            using var approvedKey = Registry.CurrentUser.CreateSubKey(CurrentUserStartupApproved, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (approvedKey != null)
            {
                // Windows Task Manager uses a 12-byte binary value. Byte 0 is 0x02 for Enabled, 0x03 for Disabled.
                byte[] binaryData = enable
                    ? new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
                    : new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                approvedKey.SetValue(entryName, binaryData, RegistryValueKind.Binary);
                return (true, $"Startup entry '{entryName}' {(enable ? "enabled" : "disabled")}.");
            }

            return (false, "Could not open StartupApproved registry key.");
        }
        catch (Exception ex)
        {
            return (false, $"Startup error: {ex.Message}");
        }
    }

    public static (bool Success, string Message) Rollback(OperationItem item)
    {
        try
        {
            var enable = Convert.ToBoolean(item.OriginalValue ?? true);
            var entryName = item.Target;

            using var approvedKey = Registry.CurrentUser.OpenSubKey(CurrentUserStartupApproved, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (approvedKey != null)
            {
                byte[] binaryData = enable
                    ? new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
                    : new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                approvedKey.SetValue(entryName, binaryData, RegistryValueKind.Binary);
                return (true, $"Rolled back startup entry '{entryName}' to {(enable ? "enabled" : "disabled")}.");
            }

            return (true, "Key not present.");
        }
        catch (Exception ex)
        {
            return (false, $"Startup rollback error: {ex.Message}");
        }
    }
}

