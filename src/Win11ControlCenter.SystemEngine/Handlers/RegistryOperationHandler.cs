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
            var normalizedVal = NormalizeRegistryValue(item.RecommendedValue, kind);
            key.SetValue(valueName, normalizedVal!, kind);
            return (true, $"Set '{valueName}' to '{normalizedVal}' in {item.Target}");
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
            var normalizedVal = NormalizeRegistryValue(item.OriginalValue, kind);
            key.SetValue(valueName, normalizedVal!, kind);
            return (true, $"Rolled back '{valueName}' to original value: '{normalizedVal}'");
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

    private static RegistryValueKind ParseValueKind(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return RegistryValueKind.DWord;

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

    public static object? NormalizeRegistryValue(object? val, RegistryValueKind kind)
    {
        if (val is null) return null;

        if (val is System.Text.Json.JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Number:
                    if (kind == RegistryValueKind.QWord && elem.TryGetInt64(out var l))
                        return l;
                    if (elem.TryGetInt32(out var i))
                        return i;
                    if (elem.TryGetInt64(out var i64))
                        return i64;
                    return elem.GetRawText();

                case System.Text.Json.JsonValueKind.True:
                    return kind switch
                    {
                        RegistryValueKind.DWord => 1,
                        RegistryValueKind.QWord => 1L,
                        _ => "1"
                    };

                case System.Text.Json.JsonValueKind.False:
                    return kind switch
                    {
                        RegistryValueKind.DWord => 0,
                        RegistryValueKind.QWord => 0L,
                        _ => "0"
                    };

                case System.Text.Json.JsonValueKind.String:
                    var str = elem.GetString() ?? string.Empty;
                    if (kind == RegistryValueKind.DWord && int.TryParse(str, out var parsedInt))
                        return parsedInt;
                    if (kind == RegistryValueKind.QWord && long.TryParse(str, out var parsedLong))
                        return parsedLong;
                    return str;

                default:
                    return elem.ToString();
            }
        }

        if (val is string s)
        {
            if (kind == RegistryValueKind.DWord && int.TryParse(s, out var intVal))
                return intVal;
            if (kind == RegistryValueKind.QWord && long.TryParse(s, out var longVal))
                return longVal;
        }

        return val;
    }
}

