using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.SystemEngine.Services;

public class SystemDiscoveryService : ISystemDiscoveryService
{
    private readonly ILoggingService _logger;

    public SystemDiscoveryService(ILoggingService logger)
    {
        _logger = logger;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var info = new SystemInfo();

            try
            {
                // 1. Operating System Information from Registry
                ReadOsInformation(info);

                // 2. Hardware Information
                ReadCpuModel(info);
                ReadGpuModel(info);
                ReadMemoryInfo(info);
                ReadStorageInfo(info);

                // 3. Security & System Status
                info.IsElevated = IsRunningAsAdministrator();
                info.IsPendingRestart = IsPendingReboot();
                info.ActivePowerPlan = GetActivePowerPlan();
                info.WindowsActivationStatus = CheckActivationStatus();
                info.IsSystemRestoreEnabled = CheckSystemRestoreStatus();

                // 4. Quick Inventory Counts
                ReadQuickInventoryCounts(info);

                info.ScannedAt = DateTime.UtcNow;
                _logger.LogInfo($"System scan completed: {info.OsName} {info.Edition} (Build {info.OsBuild}), CPU: {info.CpuModel}", "Discovery");
            }
            catch (Exception ex)
            {
                _logger.LogError("System information discovery encounter an issue", ex, "Discovery");
            }

            return info;
        }, cancellationToken);
    }

    public Task<(double CpuPercent, double AvailableRamGb)> GetLiveResourceMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            double availableRamGb = 0;
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                availableRamGb = Math.Round(memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0), 2);
            }

            double cpuPercent = 0;
            try
            {
                using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                cpuCounter.NextValue();
                Thread.Sleep(100);
                cpuPercent = Math.Round(cpuCounter.NextValue(), 1);
            }
            catch
            {
                // Fallback if performance counter is inaccessible
                cpuPercent = 0;
            }

            return (cpuPercent, availableRamGb);
        }, cancellationToken);
    }

    public bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public bool IsPendingReboot()
    {
        try
        {
            // 1. Component Based Servicing (CBS)
            using var cbsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending", false);
            if (cbsKey != null) return true;

            // 2. Windows Update Auto Update
            using var wuKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired", false);
            if (wuKey != null) return true;

            // 3. Pending File Rename Operations
            using var smKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager", false);
            var pendingFiles = smKey?.GetValue("PendingFileRenameOperations");
            if (pendingFiles != null)
            {
                if (pendingFiles is string[] array && array.Length > 0) return true;
                if (pendingFiles is string str && !string.IsNullOrWhiteSpace(str)) return true;
            }
        }
        catch
        {
            // Suppress registry access errors
        }

        return false;
    }

    private static void ReadOsInformation(SystemInfo info)
    {
        using var cvKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
        if (cvKey != null)
        {
            var buildStr = cvKey.GetValue("CurrentBuild")?.ToString() ?? cvKey.GetValue("CurrentBuildNumber")?.ToString() ?? "0";
            var ubr = cvKey.GetValue("UBR")?.ToString() ?? "0";
            var displayVersion = cvKey.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
            var editionId = cvKey.GetValue("EditionID")?.ToString() ?? "Pro";

            if (int.TryParse(buildStr, out int buildNumber))
            {
                info.OsName = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
            }
            else
            {
                info.OsName = "Windows 11";
            }

            info.OsVersion = !string.IsNullOrEmpty(displayVersion) ? displayVersion : "24H2";
            info.OsBuild = $"{buildStr}.{ubr}";

            info.Edition = editionId switch
            {
                "Core" => "Windows 11 Home",
                "CoreSingleLanguage" => "Windows 11 Home Single Language",
                "Professional" => "Windows 11 Pro",
                "Enterprise" => "Windows 11 Enterprise",
                "Education" => "Windows 11 Education",
                _ => $"Windows 11 {editionId}"
            };
        }

        info.Architecture = Environment.Is64BitOperatingSystem ? "64-bit (x64)" : "32-bit";
    }

    private static void ReadCpuModel(SystemInfo info)
    {
        try
        {
            using var cpuKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", false);
            var cpuName = cpuKey?.GetValue("ProcessorNameString")?.ToString();
            if (!string.IsNullOrWhiteSpace(cpuName))
            {
                info.CpuModel = cpuName.Trim();
                return;
            }
        }
        catch { }

        info.CpuModel = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 Processor";
    }

    private static void ReadGpuModel(SystemInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name) && !name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                {
                    info.GpuModel = name.Trim();
                    return;
                }
            }
        }
        catch { }

        info.GpuModel = "Integrated / Discrete GPU";
    }

    private static void ReadMemoryInfo(SystemInfo info)
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            info.TotalRamGigabytes = Math.Round(memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0), 1);
            info.AvailableRamGigabytes = Math.Round(memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0), 1);
        }
        else
        {
            var gcBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            info.TotalRamGigabytes = Math.Round(gcBytes / (1024.0 * 1024.0 * 1024.0), 1);
            info.AvailableRamGigabytes = info.TotalRamGigabytes;
        }
    }

    private static void ReadStorageInfo(SystemInfo info)
    {
        try
        {
            var systemDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.RootDirectory.FullName.StartsWith("C", StringComparison.OrdinalIgnoreCase));
            if (systemDrive != null)
            {
                info.SystemDriveTotalGigabytes = Math.Round(systemDrive.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                info.SystemDriveFreeGigabytes = Math.Round(systemDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
            }
        }
        catch { }
    }

    private static string GetActivePowerPlan()
    {
        try
        {
            var psi = new ProcessStartInfo("powercfg.exe", "/getactivescheme")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);

                if (!string.IsNullOrWhiteSpace(output))
                {
                    // Format: Power Scheme GUID: ...  (Balanced)
                    var parenStart = output.IndexOf('(');
                    var parenEnd = output.IndexOf(')');
                    if (parenStart >= 0 && parenEnd > parenStart)
                    {
                        return output.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                    }
                }
            }
        }
        catch { }

        return "Balanced";
    }

    private static string CheckActivationStatus()
    {
        // Non-destructive read from SoftwareLicensingProduct if accessible
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL");
            foreach (ManagementObject obj in searcher.Get())
            {
                var status = Convert.ToInt32(obj["LicenseStatus"]);
                if (status == 1) return "Activated (Genuine)";
            }
        }
        catch { }

        return "Licensed";
    }

    private static bool CheckSystemRestoreStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", false);
            var disableVal = key?.GetValue("RPSessionInterval");
            return disableVal != null;
        }
        catch
        {
            return false;
        }
    }

    private static void ReadQuickInventoryCounts(SystemInfo info)
    {
        try
        {
            // Startup items in HKCU & HKLM Run
            int startupCount = 0;
            using (var k1 = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                startupCount += k1?.ValueCount ?? 0;
            using (var k2 = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                startupCount += k2?.ValueCount ?? 0;
            info.StartupAppCount = startupCount;

            // Running vs disabled services count
            int runningServices = 0;
            int disabledServices = 0;
            using (var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", false))
            {
                if (servicesKey != null)
                {
                    foreach (var subKeyName in servicesKey.GetSubKeyNames())
                    {
                        using var sKey = servicesKey.OpenSubKey(subKeyName, false);
                        var startVal = sKey?.GetValue("Start");
                        if (startVal is int val && val == 4)
                        {
                            disabledServices++;
                        }
                        else
                        {
                            runningServices++;
                        }
                    }
                }
            }
            info.RunningServiceCount = runningServices;
            info.DisabledServiceCount = disabledServices;

            // Quick App count in AppxAllUserStore
            using var appxStore = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications", false);
            info.ProvisionedAppCount = appxStore?.SubKeyCount ?? 0;
        }
        catch { }
    }
}

