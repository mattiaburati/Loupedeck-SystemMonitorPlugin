namespace Loupedeck.PCMonitorPlugin.Services
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;

    // This class reads FPS data from RivaTuner Statistics Server (RTSS)
    // through its shared memory interface

    public class RTSSReader
    {
        private const String RTSS_SHARED_MEMORY_NAME = "RTSSSharedMemoryV2";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenFileMapping(UInt32 dwDesiredAccess, Boolean bInheritHandle, String lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, UInt32 dwDesiredAccess, UInt32 dwFileOffsetHigh, UInt32 dwFileOffsetlow, UInt32 dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Boolean UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Boolean CloseHandle(IntPtr hObject);

        private const UInt32 FILE_MAP_READ = 0x0004;

        // RTSS Shared Memory structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RTSS_SHARED_MEMORY
        {
            public UInt32 dwSignature;          // 'RTSS'
            public UInt32 dwVersion;            // Version
            public UInt32 dwAppEntrySize;       // Size of application entry
            public UInt32 dwAppArrOffset;       // Offset to application array
            public UInt32 dwAppArrSize;         // Size of application array
            public UInt32 dwOSDEntrySize;       // OSD entry size
            public UInt32 dwOSDArrOffset;       // OSD array offset
            public UInt32 dwOSDArrSize;         // OSD array size
            public UInt32 dwOSDFrame;           // Current OSD frame
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct RTSS_SHARED_MEMORY_APP_ENTRY
        {
            public UInt32 dwProcessID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
            public Byte[] szName;
            public UInt32 dwFlags;
            public UInt32 dwTime0;
            public UInt32 dwTime1;
            public UInt32 dwFrames;
            public UInt32 dwFrameTime;
            public Single fFramerate;
            public UInt32 dwStatFlags;
            public UInt32 dwStatTime0;
            public UInt32 dwStatTime1;
            public UInt32 dwStatFrames;
            public UInt32 dwStatCount;
            public Single fStatFramerateMin;    // Changed to Single
            public Single fStatFramerateAvg;    // Changed to Single - This might be more reliable!
            public Single fStatFramerateMax;    // Changed to Single
        }

        // Helper method to check if a process is still running
        private Boolean IsProcessRunning(UInt32 processId)
        {
            try
            {
                var process = Process.GetProcessById((Int32)processId);
                return !process.HasExited;
            }
            catch
            {
                // Process not found or access denied
                return false;
            }
        }

        // Helper method to check if a process should be excluded from FPS monitoring
        private Boolean IsExcludedProcess(String processPath)
        {
            if (String.IsNullOrEmpty(processPath))
            {
                return true;
            }

            var processPathLower = processPath.ToLower();

            // Exclude RivaTuner itself and other monitoring/overlay tools
            var excludedProcesses = new[]
            {
                "rtss.exe",
                "rivatuner",
                "nvidia broadcast",
                "nvbroadcast",
                "obs",
                "streamlabs",
                "xsplit",
                "afterburner.exe",
                "hwinfo",
                "msiafterburner"
            };

            foreach (var excluded in excludedProcesses)
            {
                if (processPathLower.Contains(excluded))
                {
                    return true;
                }
            }

            return false;
        }

        // Class to hold app information
        public class RTSSAppInfo
        {
            public UInt32 ProcessID { get; set; }
            public String ProcessName { get; set; }
            public String DisplayName { get; set; }
            public String ProcessPath { get; set; }
            public Single FPS { get; set; }
        }

        // Get all active monitored applications
        public RTSSAppInfo[] GetActiveApplications()
        {
            var apps = new System.Collections.Generic.List<RTSSAppInfo>();
            IntPtr hMapFile = IntPtr.Zero;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                hMapFile = OpenFileMapping(FILE_MAP_READ, false, RTSS_SHARED_MEMORY_NAME);
                if (hMapFile == IntPtr.Zero)
                {
                    return apps.ToArray();
                }

                pBuffer = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
                if (pBuffer == IntPtr.Zero)
                {
                    return apps.ToArray();
                }

                var header = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(pBuffer);

                if (header.dwSignature != 0x52545353 || header.dwAppArrSize == 0)
                {
                    return apps.ToArray();
                }

                var maxEntries = header.dwAppArrSize;

                for (var i = 0; i < maxEntries; i++)
                {
                    var appEntryPtr = IntPtr.Add(pBuffer, (Int32)(header.dwAppArrOffset + (i * header.dwAppEntrySize)));
                    var appEntry = Marshal.PtrToStructure<RTSS_SHARED_MEMORY_APP_ENTRY>(appEntryPtr);

                    if (appEntry.dwProcessID != 0 && this.IsProcessRunning(appEntry.dwProcessID))
                    {
                        var appPath = Encoding.ASCII.GetString(appEntry.szName).TrimEnd('\0');

                        if (this.IsExcludedProcess(appPath))
                        {
                            continue;
                        }

                        var fps = 0f;
                        if (appEntry.fFramerate > 0)
                        {
                            fps = appEntry.fFramerate;
                        }
                        else if (appEntry.fStatFramerateAvg > 0)
                        {
                            fps = appEntry.fStatFramerateAvg;
                        }
                        else if (appEntry.dwFrameTime > 0)
                        {
                            fps = 1000000.0f / appEntry.dwFrameTime;
                        }

                        var processName = System.IO.Path.GetFileNameWithoutExtension(appPath);
                        var displayName = String.IsNullOrEmpty(processName) ? $"Process {appEntry.dwProcessID}" : processName;

                        apps.Add(new RTSSAppInfo
                        {
                            ProcessID = appEntry.dwProcessID,
                            ProcessName = processName,
                            DisplayName = displayName,
                            ProcessPath = appPath,
                            FPS = fps
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error getting active applications: {ex.Message}");
            }
            finally
            {
                if (pBuffer != IntPtr.Zero)
                {
                    UnmapViewOfFile(pBuffer);
                }
                if (hMapFile != IntPtr.Zero)
                {
                    CloseHandle(hMapFile);
                }
            }

            return apps.ToArray();
        }

        // Tries to read the framerate from RTSS shared memory for a specific process
        public Boolean TryGetFramerate(out Single fps, UInt32? targetProcessID = null)
        {
            fps = 0;
            IntPtr hMapFile = IntPtr.Zero;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                // Open the shared memory
                hMapFile = OpenFileMapping(FILE_MAP_READ, false, RTSS_SHARED_MEMORY_NAME);
                if (hMapFile == IntPtr.Zero)
                {
                    return false; // RTSS not running
                }

                // Map the view of the file
                pBuffer = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
                if (pBuffer == IntPtr.Zero)
                {
                    return false;
                }

                // Read the header
                var header = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(pBuffer);

                // Verify signature (should be 'RTSS')
                if (header.dwSignature != 0x52545353) // 'RTSS' in ASCII
                {
                    return false;
                }

                // Check if we have any application entries
                if (header.dwAppArrSize == 0)
                {
                    return false; // No applications being monitored
                }

                // dwAppArrSize is the maximum number of entries (not bytes)
                var maxEntries = header.dwAppArrSize;

                // Loop through all application entries to find one with valid FPS
                for (var i = 0; i < maxEntries; i++)
                {
                    var appEntryPtr = IntPtr.Add(pBuffer, (Int32)(header.dwAppArrOffset + (i * header.dwAppEntrySize)));
                    var appEntry = Marshal.PtrToStructure<RTSS_SHARED_MEMORY_APP_ENTRY>(appEntryPtr);

                    // Check if this entry has valid framerate data AND process is still running
                    if (appEntry.dwProcessID != 0 && this.IsProcessRunning(appEntry.dwProcessID))
                    {
                        // If we're looking for a specific process, skip others
                        if (targetProcessID.HasValue && appEntry.dwProcessID != targetProcessID.Value)
                        {
                            continue;
                        }

                        // Get process name and check if it should be excluded
                        var appName = Encoding.ASCII.GetString(appEntry.szName).TrimEnd('\0');

                        // Skip excluded processes (RTSS, monitoring tools, etc.)
                        if (this.IsExcludedProcess(appName))
                        {
                            continue;
                        }

                        // Try instant framerate first
                        if (appEntry.fFramerate > 0)
                        {
                            fps = appEntry.fFramerate;
                            return true;
                        }
                        // Try average framerate from statistics
                        else if (appEntry.fStatFramerateAvg > 0)
                        {
                            fps = appEntry.fStatFramerateAvg;
                            return true;
                        }
                        // Calculate FPS from frame time (most accurate method)
                        else if (appEntry.dwFrameTime > 0)
                        {
                            // Frame time is in microseconds (µs)
                            // FPS = 1,000,000 / frameTime(µs)
                            fps = 1000000.0f / appEntry.dwFrameTime;
                            return true;
                        }
                    }
                }

                return false; // No active game with FPS found
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error reading RTSS data: {ex.Message}");
                return false;
            }
            finally
            {
                // Clean up
                if (pBuffer != IntPtr.Zero)
                {
                    UnmapViewOfFile(pBuffer);
                }
                if (hMapFile != IntPtr.Zero)
                {
                    CloseHandle(hMapFile);
                }
            }
        }

        // Debug method to get information about monitored applications
        public String GetDebugInfo()
        {
            IntPtr hMapFile = IntPtr.Zero;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                hMapFile = OpenFileMapping(FILE_MAP_READ, false, RTSS_SHARED_MEMORY_NAME);
                if (hMapFile == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    return $"Failed to open RTSS shared memory. Error code: {error}";
                }

                pBuffer = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
                if (pBuffer == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    return $"Failed to map RTSS memory view. Error code: {error}";
                }

                var header = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(pBuffer);

                var sb = new StringBuilder();
                sb.AppendLine($"RTSS Signature: 0x{header.dwSignature:X8}");
                sb.AppendLine($"RTSS Version: {header.dwVersion}");
                sb.AppendLine($"App Array Size: {header.dwAppArrSize}");
                sb.AppendLine($"App Entry Size: {header.dwAppEntrySize}");

                if (header.dwSignature != 0x52545353)
                {
                    sb.AppendLine("Invalid RTSS signature!");
                    return sb.ToString();
                }

                if (header.dwAppArrSize > 0)
                {
                    var maxEntries = header.dwAppArrSize;
                    sb.AppendLine($"Max app slots: {maxEntries}");
                    sb.AppendLine("---");

                    var foundApps = 0;
                    for (var i = 0; i < maxEntries; i++)
                    {
                        var appEntryPtr = IntPtr.Add(pBuffer, (Int32)(header.dwAppArrOffset + (i * header.dwAppEntrySize)));
                        var appEntry = Marshal.PtrToStructure<RTSS_SHARED_MEMORY_APP_ENTRY>(appEntryPtr);

                        if (appEntry.dwProcessID != 0)
                        {
                            foundApps++;
                            var appName = Encoding.ASCII.GetString(appEntry.szName).TrimEnd('\0');
                            var isRunning = this.IsProcessRunning(appEntry.dwProcessID);
                            var isExcluded = this.IsExcludedProcess(appName);

                            sb.AppendLine($"[{i}] Process ID: {appEntry.dwProcessID}");
                            sb.AppendLine($"    Name: {appName}");
                            sb.AppendLine($"    Status: {(isRunning ? "RUNNING" : "STOPPED")}");
                            sb.AppendLine($"    Excluded: {(isExcluded ? "YES (monitoring tool)" : "NO")}");
                            sb.AppendLine($"    Instant Framerate: {appEntry.fFramerate} FPS");
                            sb.AppendLine($"    Frame Time: {appEntry.dwFrameTime} µs");
                            sb.AppendLine($"    Frames: {appEntry.dwFrames}");

                            // Calculate FPS from frame time
                            if (appEntry.dwFrameTime > 0)
                            {
                                var calculatedFps = 1000000.0f / appEntry.dwFrameTime;
                                sb.AppendLine($"    Calculated FPS: {calculatedFps:F1}");
                            }

                            sb.AppendLine($"    Stat Flags: {appEntry.dwStatFlags}");
                            sb.AppendLine($"    Stat Frames: {appEntry.dwStatFrames}");
                            sb.AppendLine($"    Stat FPS Min: {appEntry.fStatFramerateMin}");
                            sb.AppendLine($"    Stat FPS Avg: {appEntry.fStatFramerateAvg}");
                            sb.AppendLine($"    Stat FPS Max: {appEntry.fStatFramerateMax}");
                            sb.AppendLine("---");
                        }
                    }

                    if (foundApps == 0)
                    {
                        sb.AppendLine("No active applications found in any slot");
                    }
                    else
                    {
                        sb.AppendLine($"Total active apps: {foundApps}");
                    }
                }
                else
                {
                    sb.AppendLine("No applications currently monitored");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
            finally
            {
                if (pBuffer != IntPtr.Zero)
                {
                    UnmapViewOfFile(pBuffer);
                }
                if (hMapFile != IntPtr.Zero)
                {
                    CloseHandle(hMapFile);
                }
            }
        }
    }
}
