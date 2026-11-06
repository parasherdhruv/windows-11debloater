using Microsoft.Win32;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.SystemEngine.Handlers;

public static class RegistryOperationHandler
{
    public static (bool Success, string Message) Apply(OperationItem item)
    {
        try
        {
            var (baseKey, subKeyPath) = ParseRegistryPath(item.Target);
            using var key = baseKey.CreateSubKey(subKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (key == null)
            {
                return (false, $"Unable to open or create registry key: {item.Target}");
            }

            var valueName = item.Metadata.TryGetValue("ValueName", out var vn) ? vn : string.Empty;

            if (item.RecommendedValue == null)
            {
                // Delete value
                if (key.GetValue(valueName) != null)
                {
                    key.DeleteValue(valueName, false);
                }
                return (true, $"Deleted value '{valueName}' from {item.Target}");
            }

            var kind = ParseValueKind(item.ValueType);
            key.SetValue(valueName, item.RecommendedValue, kind);
            return (true, $"Set '{valueName}' to '{item.RecommendedValue}' in {item.Target}");
        }
        catch (Exception ex)
        {
            return (false, $"Registry operation error: {ex.Message}");
        }
    }

    public static (bool Success, string Message) Rollback(OperationItem item)
    {
        try
        {
            var (baseKey, subKeyPath) = ParseRegistryPath(item.Target);
            var valueName = item.Metadata.TryGetValue("ValueName", out var vn) ? vn : string.Empty;

            using var key = baseKey.OpenSubKey(subKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (key == null)
            {
                return (true, "Target registry key does not exist; nothing to rollback.");
            }

            if (item.OriginalValue == null)
            {
                // Key value originally didn't exist, so delete it
                if (key.GetValue(valueName) != null)
                {
                    key.DeleteValue(valueName, false);
                }
                return (true, $"Rolled back '{valueName}' (deleted value as it did not exist originally)");
            }

            var kind = ParseValueKind(item.ValueType);
            key.SetValue(valueName, item.OriginalValue, kind);
            return (true, $"Rolled back '{valueName}' to original value: '{item.OriginalValue}'");
        }
        catch (Exception ex)
        {
            return (false, $"Registry rollback error: {ex.Message}");
        }
    }

    public static object? ReadCurrentValue(string targetKeyPath, string valueName)
    {
        try
        {
            var (baseKey, subKeyPath) = ParseRegistryPath(targetKeyPath);
            using var key = baseKey.OpenSubKey(subKeyPath, false);
            return key?.GetValue(valueName);
        }
        catch
        {
            return null;
        }
    }

    private static (RegistryKey BaseKey, string SubKeyPath) ParseRegistryPath(string fullPath)
    {
        var parts = fullPath.Split('\\', 2, StringSplitOptions.RemoveEmptyEntries);
        var hive = parts[0].ToUpperInvariant();
        var subKey = parts.Length > 1 ? parts[1] : string.Empty;

        var baseKey = hive switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKU" or "HKEY_USERS" => Registry.Users,
            _ => Registry.CurrentUser
        };

        return (baseKey, subKey);
    }

    private static RegistryValueKind ParseValueKind(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "dword" => RegistryValueKind.DWord,
            "qword" => RegistryValueKind.QWord,
            "string" or "sz" => RegistryValueKind.String,
            "binary" => RegistryValueKind.Binary,
            "multistring" => RegistryValueKind.MultiString,
            _ => RegistryValueKind.DWord
        };
    }
}

