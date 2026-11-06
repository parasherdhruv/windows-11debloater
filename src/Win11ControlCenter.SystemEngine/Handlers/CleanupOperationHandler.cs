using System.Runtime.InteropServices;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;

namespace Win11ControlCenter.SystemEngine.Handlers;

public static class CleanupOperationHandler
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI   = 0x00000002;
    private const uint SHERB_NOSOUND        = 0x00000004;

    public static (bool Success, string Message, long BytesDeleted, int FilesDeleted) Apply(OperationItem item)
    {
        var targetPath = item.Target;

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);
        if (!isValid)
        {
            return (false, $"Safety rejection: {reason}", 0, 0);
        }

        // Special Case: Recycle Bin
        if (targetPath.Equals("RecycleBin", StringComparison.OrdinalIgnoreCase))
        {
            return EmptyRecycleBin();
        }

        if (!Directory.Exists(targetPath))
        {
            return (true, $"Target directory '{targetPath}' does not exist or was already empty.", 0, 0);
        }

        long bytesDeleted = 0;
        int filesDeleted = 0;
        int errors = 0;

        try
        {
            var searchPattern = item.Metadata.TryGetValue("SearchPattern", out var pattern) && !string.IsNullOrWhiteSpace(pattern) 
                ? pattern 
                : "*";

            var dirInfo = new DirectoryInfo(targetPath);
            foreach (var file in dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories))
            {
                try
                {
                    // Don't delete files locked or modified within the last 2 hours (active processes)
                    if (DateTime.UtcNow - file.LastWriteTimeUtc < TimeSpan.FromHours(2))
                        continue;

                    var size = file.Length;
                    file.Delete();
                    bytesDeleted += size;
                    filesDeleted++;
                }
                catch
                {
                    errors++;
                }
            }

            // Cleanup empty subdirectories if we did a full wipe (pattern == "*")
            if (searchPattern == "*")
            {
                foreach (var dir in dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories).OrderByDescending(d => d.FullName.Length))
                {
                    try
                    {
                        if (dir.EnumerateFileSystemInfos().Any()) continue;
                        dir.Delete();
                    }
                    catch
                    {
                        // Ignore locked directory
                    }
                }
            }

            var mb = bytesDeleted / (1024.0 * 1024.0);
            return (true, $"Cleaned {filesDeleted} files ({mb:F1} MB reclaimed). Skipped {errors} locked files.", bytesDeleted, filesDeleted);
        }
        catch (Exception ex)
        {
            return (false, $"Cleanup error on '{targetPath}': {ex.Message}", bytesDeleted, filesDeleted);
        }
    }

    public static (long TotalBytes, int TotalItems) QueryRecycleBin()
    {
        try
        {
            var info = new SHQUERYRBINFO();
            info.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
            int res = SHQueryRecycleBin(null, ref info);
            if (res == 0)
            {
                return (info.i64Size, (int)info.i64NumItems);
            }
        }
        catch
        {
            // Fallback if P/Invoke fails
        }
        return (0, 0);
    }

    private static (bool Success, string Message, long BytesDeleted, int FilesDeleted) EmptyRecycleBin()
    {
        try
        {
            var (bytesBefore, itemsBefore) = QueryRecycleBin();
            int res = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            if (res == 0)
            {
                var mb = bytesBefore / (1024.0 * 1024.0);
                return (true, $"Recycle Bin emptied ({itemsBefore} items, {mb:F1} MB reclaimed).", bytesBefore, itemsBefore);
            }
            return (false, $"Failed to empty Recycle Bin (HRESULT: 0x{res:X8})", 0, 0);
        }
        catch (Exception ex)
        {
            return (false, $"Error emptying Recycle Bin: {ex.Message}", 0, 0);
        }
    }
}
