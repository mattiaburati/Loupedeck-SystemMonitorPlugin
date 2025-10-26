namespace Loupedeck.PCMonitorPlugin.Services
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    // This class reads hardware monitoring data from MSI Afterburner / RivaTuner Statistics Server
    // through shared memory (MAHM - MSI Afterburner Hardware Monitor)

    public class MSIAfterburnerReader
    {
        private const String MAHM_SHARED_MEMORY_NAME = "MAHMSharedMemory";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenFileMapping(UInt32 dwDesiredAccess, Boolean bInheritHandle, String lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, UInt32 dwDesiredAccess, UInt32 dwFileOffsetHigh, UInt32 dwFileOffsetlow, UInt32 dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Boolean UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Boolean CloseHandle(IntPtr hObject);

        private const UInt32 FILE_MAP_READ = 0x0004;

        // Structure definitions for MAHM shared memory
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MAHM_SHARED_MEMORY_HEADER
        {
            public UInt32 dwSignature;          // 'MAHM'
            public UInt32 dwVersion;            // Version number
            public UInt32 dwHeaderSize;         // Header size
            public UInt32 dwNumEntries;         // Number of monitoring entries
            public UInt32 dwEntrySize;          // Size of each entry
            public Int32 time;                  // Timestamp
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct MAHM_SHARED_MEMORY_ENTRY
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String szSrcName;            // Data source name

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String szSrcUnits;           // Data source units

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String szLocalizedSrcName;   // Localized data source name

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String szLocalizedSrcUnits;  // Localized data source units

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String szFormat;             // printf-style format string

            public Single data;                 // Current data value
            public Single minLimit;             // Minimum limit
            public Single maxLimit;             // Maximum limit
            public UInt32 dwFlags;              // Flags
        }

        // Generic method to read any value from MSI Afterburner shared memory by searching for entry names
        public Boolean TryGetValue(String[] searchNames, out Single value, out String unit)
        {
            value = 0;
            unit = String.Empty;
            IntPtr hMapFile = IntPtr.Zero;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                // Open the shared memory
                hMapFile = OpenFileMapping(FILE_MAP_READ, false, MAHM_SHARED_MEMORY_NAME);
                if (hMapFile == IntPtr.Zero)
                {
                    return false; // MSI Afterburner not running or shared memory not available
                }

                // Map the view of the file
                pBuffer = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
                if (pBuffer == IntPtr.Zero)
                {
                    return false;
                }

                // Read the header
                var header = Marshal.PtrToStructure<MAHM_SHARED_MEMORY_HEADER>(pBuffer);

                // Verify signature (accept both MAHM and MHAM)
                if (header.dwSignature != 0x4D48414D && header.dwSignature != 0x4D41484D)
                {
                    return false;
                }

                // Search through entries
                var entryOffset = (Int32)header.dwHeaderSize;

                for (var i = 0; i < header.dwNumEntries; i++)
                {
                    var entryPtr = IntPtr.Add(pBuffer, entryOffset);
                    var entry = Marshal.PtrToStructure<MAHM_SHARED_MEMORY_ENTRY>(entryPtr);

                    // Check if this entry matches any of the search names
                    if (entry.szSrcName != null)
                    {
                        foreach (var searchName in searchNames)
                        {
                            if (entry.szSrcName.Contains(searchName, StringComparison.OrdinalIgnoreCase))
                            {
                                value = entry.data;
                                unit = entry.szSrcUnits ?? String.Empty;
                                return true;
                            }
                        }
                    }

                    entryOffset += (Int32)header.dwEntrySize;
                }

                return false; // Entry not found
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error reading MSI Afterburner data: {ex.Message}");
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

        // Tries to read GPU usage percentage
        public Boolean TryGetGPUUsage(out Single usage)
        {
            // Based on actual MSI Afterburner entry names
            var result = this.TryGetValue(new[] { "GPU usage" }, out usage, out _);
            return result;
        }

        // Tries to read GPU power consumption
        public Boolean TryGetGPUPower(out Single power, out String unit)
        {
            // Based on actual MSI Afterburner entry names
            var result = this.TryGetValue(new[] { "Power" }, out power, out unit);
            return result;
        }

        // Tries to read GPU core clock
        public Boolean TryGetGPUClock(out Single clock, out String unit)
        {
            // Based on actual MSI Afterburner entry names
            var result = this.TryGetValue(new[] { "Core clock" }, out clock, out unit);
            return result;
        }

        // Tries to read GPU temperature
        public Boolean TryGetGPUTemperature(out Single temp, out String unit)
        {
            var result = this.TryGetValue(new[] { "GPU temperature" }, out temp, out unit);
            return result;
        }

        // Tries to read GPU memory usage
        public Boolean TryGetGPUMemoryUsage(out Single memUsage, out String unit)
        {
            var result = this.TryGetValue(new[] { "Memory usage" }, out memUsage, out unit);
            return result;
        }

        // Tries to read CPU usage percentage
        public Boolean TryGetCPUUsage(out Single usage)
        {
            var result = this.TryGetValue(new[] { "CPU usage" }, out usage, out _);
            return result;
        }

        // Tries to read CPU temperature
        public Boolean TryGetCPUTemperature(out Single temp, out String unit)
        {
            var result = this.TryGetValue(new[] { "CPU temperature" }, out temp, out unit);
            return result;
        }

        // Tries to read CPU power consumption
        public Boolean TryGetCPUPower(out Single power, out String unit)
        {
            var result = this.TryGetValue(new[] { "CPU power" }, out power, out unit);
            return result;
        }

        // Tries to read RAM usage
        public Boolean TryGetRAMUsage(out Single ramUsage, out String unit)
        {
            var result = this.TryGetValue(new[] { "RAM usage" }, out ramUsage, out unit);
            return result;
        }

        // Gets all available monitoring entries (useful for debugging)
        public String[] GetAvailableEntries()
        {
            IntPtr hMapFile = IntPtr.Zero;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                PluginLog.Info($"Attempting to open shared memory: {MAHM_SHARED_MEMORY_NAME}");

                hMapFile = OpenFileMapping(FILE_MAP_READ, false, MAHM_SHARED_MEMORY_NAME);
                if (hMapFile == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    PluginLog.Warning($"Failed to open shared memory. Error code: {error}");
                    PluginLog.Info("Trying to detect if MSI Afterburner is running...");
                    return Array.Empty<String>();
                }

                PluginLog.Info("Shared memory opened successfully!");

                pBuffer = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
                if (pBuffer == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    PluginLog.Warning($"Failed to map view of file. Error code: {error}");
                    return Array.Empty<String>();
                }

                PluginLog.Info("Memory view mapped successfully!");

                var header = Marshal.PtrToStructure<MAHM_SHARED_MEMORY_HEADER>(pBuffer);

                PluginLog.Info($"Header signature: 0x{header.dwSignature:X8}");
                PluginLog.Info($"Header version: {header.dwVersion}");
                PluginLog.Info($"Header size: {header.dwHeaderSize}");
                PluginLog.Info($"Number of entries: {header.dwNumEntries}");
                PluginLog.Info($"Entry size: {header.dwEntrySize}");

                // Accept both possible signatures (MAHM or MHAM)
                if (header.dwSignature != 0x4D48414D && header.dwSignature != 0x4D41484D)
                {
                    PluginLog.Warning($"Invalid signature! Got 0x{header.dwSignature:X8}");
                    return Array.Empty<String>();
                }

                var entries = new String[header.dwNumEntries];
                var entryOffset = (Int32)header.dwHeaderSize;

                for (var i = 0; i < header.dwNumEntries; i++)
                {
                    var entryPtr = IntPtr.Add(pBuffer, entryOffset);
                    var entry = Marshal.PtrToStructure<MAHM_SHARED_MEMORY_ENTRY>(entryPtr);
                    entries[i] = $"{entry.szSrcName}: {entry.data} {entry.szSrcUnits}";
                    entryOffset += (Int32)header.dwEntrySize;
                }

                return entries;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception in GetAvailableEntries: {ex.Message}");
                PluginLog.Error($"Stack trace: {ex.StackTrace}");
                return Array.Empty<String>();
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
