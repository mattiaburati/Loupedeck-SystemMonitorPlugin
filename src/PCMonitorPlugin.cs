namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class PCMonitorPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public PCMonitorPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            this.CheckRequiredSoftware();
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
        }

        private void CheckRequiredSoftware()
        {
            var afterburnerRunning = this.IsProcessRunning("MSIAfterburner");
            var rtssRunning = this.IsProcessRunning("RTSS");

            if (!afterburnerRunning && !rtssRunning)
            {
                // Both missing - critical error
                var afterburnerInstalled = this.CheckAfterburnerInstalled();
                var rtssInstalled = this.CheckRTSSInstalled();

                if (!afterburnerInstalled && !rtssInstalled)
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Error,
                        "MSI Afterburner and RivaTuner are not installed",
                        "https://www.msi.com/Landing/afterburner",
                        "Download MSI Afterburner"
                    );
                }
                else if (!afterburnerInstalled)
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Error,
                        "MSI Afterburner is not installed",
                        "https://www.msi.com/Landing/afterburner",
                        "Download MSI Afterburner"
                    );
                }
                else if (!rtssInstalled)
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Error,
                        "RivaTuner Statistics Server is not installed",
                        "https://www.msi.com/Landing/afterburner",
                        "Download from MSI Afterburner package"
                    );
                }
                else
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Error,
                        "MSI Afterburner and RivaTuner are not running. Please start them.",
                        null,
                        null
                    );
                }
            }
            else if (!afterburnerRunning)
            {
                var afterburnerInstalled = this.CheckAfterburnerInstalled();
                if (!afterburnerInstalled)
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Error,
                        "MSI Afterburner is not installed",
                        "https://www.msi.com/Landing/afterburner",
                        "Download MSI Afterburner"
                    );
                }
                else
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Warning,
                        "MSI Afterburner is not running. System monitoring will not work.",
                        null,
                        null
                    );
                }
            }
            else if (!rtssRunning)
            {
                var rtssInstalled = this.CheckRTSSInstalled();
                if (!rtssInstalled)
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Error,
                        "RivaTuner Statistics Server is not installed",
                        "https://www.msi.com/Landing/afterburner",
                        "Download from MSI Afterburner package"
                    );
                }
                else
                {
                    this.OnPluginStatusChanged(
                        Loupedeck.PluginStatus.Warning,
                        "RivaTuner Statistics Server is not running. FPS monitoring will not work.",
                        null,
                        null
                    );
                }
            }
            else
            {
                // Both running - clear status
                this.OnPluginStatusChanged(
                    Loupedeck.PluginStatus.Normal,
                    null,
                    null,
                    null
                );
                PluginLog.Info("✓ MSI Afterburner and RivaTuner are running");
            }
        }

        private Boolean IsProcessRunning(String processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Any();
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Could not check {processName} status: {ex.Message}");
                return false;
            }
        }

        private Boolean CheckAfterburnerInstalled()
        {
            // Common installation paths for MSI Afterburner
            var commonPaths = new[]
            {
                @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe",
                @"C:\Program Files\MSI Afterburner\MSIAfterburner.exe"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return true;
                }
            }

            return false;
        }

        private Boolean CheckRTSSInstalled()
        {
            // Common installation paths for RivaTuner Statistics Server
            var commonPaths = new[]
            {
                @"C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe",
                @"C:\Program Files\RivaTuner Statistics Server\RTSS.exe"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
