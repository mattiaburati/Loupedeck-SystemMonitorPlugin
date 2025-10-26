namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Loupedeck.PCMonitorPlugin.Helpers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This folder allows users to select which application to monitor for FPS
    public class FPSAppSelectorFolder : PluginDynamicFolder
    {
        private readonly RTSSReader _rtssReader;

        public FPSAppSelectorFolder()
            : base()
        {
            this.DisplayName = "Select FPS App";
            this.Description = "Choose which application to monitor";
            this.GroupName = "PC Monitor";

            this._rtssReader = new RTSSReader();
            PluginLog.Info("FPS App Selector Folder initialized");
        }

        public override IEnumerable<String> GetButtonPressActionNames(DeviceType deviceType)
        {
            var actionNames = new List<String>();

            try
            {
                // Add "Auto" option first
                actionNames.Add(this.CreateCommandName("auto"));

                // Get all active applications from RTSS
                var apps = this._rtssReader.GetActiveApplications();
                PluginLog.Info($"Found {apps.Length} active applications from RTSS");

                foreach (var app in apps)
                {
                    PluginLog.Info($"Adding app: {app.DisplayName} (PID: {app.ProcessID})");
                    actionNames.Add(this.CreateCommandName(app.ProcessID.ToString()));
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error getting applications for folder: {ex.Message}");
            }

            return actionNames;
        }

        public override void RunCommand(String actionParameter)
        {
            try
            {
                if (actionParameter == "auto")
                {
                    // Return to auto mode
                    FPSDisplayCommand.SelectedProcessID = null;
                    FPSDisplayCommand.SelectedProcessName = null;
                    PluginLog.Info("FPS Monitor: Switched to auto mode");
                }
                else if (UInt32.TryParse(actionParameter, out var processID))
                {
                    // User selected a specific application
                    var apps = this._rtssReader.GetActiveApplications();
                    var selectedApp = apps.FirstOrDefault(a => a.ProcessID == processID);

                    if (selectedApp != null)
                    {
                        FPSDisplayCommand.SelectedProcessID = processID;
                        FPSDisplayCommand.SelectedProcessName = selectedApp.DisplayName;
                        PluginLog.Info($"FPS Monitor: Selected {selectedApp.DisplayName} (PID: {processID})");
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error handling button press: {ex.Message}");
            }
        }

        public override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            PluginLog.Info($"GetCommandDisplayName called for: {actionParameter}");

            if (actionParameter == "auto")
            {
                return "Auto Mode";
            }
            else if (UInt32.TryParse(actionParameter, out var processID))
            {
                try
                {
                    var apps = this._rtssReader.GetActiveApplications();
                    var app = apps.FirstOrDefault(a => a.ProcessID == processID);
                    if (app != null)
                    {
                        var selected = FPSDisplayCommand.SelectedProcessID == processID ? " ✓" : "";
                        var fpsText = app.FPS > 0 ? $"\n{app.FPS:F0} FPS" : "";
                        return $"{app.DisplayName}{selected}{fpsText}";
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Error getting display name: {ex.Message}");
                }
            }

            return actionParameter;
        }

        public override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            PluginLog.Info($"GetCommandImage called for: {actionParameter}");

            if (actionParameter == "auto")
            {
                // For auto mode, just show a simple icon
                using (var builder = new BitmapBuilder(imageSize))
                {
                    builder.Clear(BitmapColor.Black);
                    var isSelected = !FPSDisplayCommand.SelectedProcessID.HasValue;
                    var textColor = isSelected ? new BitmapColor(0, 255, 100) : BitmapColor.White;
                    builder.DrawText("AUTO", 0, 30, 90, 30, textColor, 14);
                    return builder.ToImage();
                }
            }
            else if (UInt32.TryParse(actionParameter, out var processID))
            {
                try
                {
                    var apps = this._rtssReader.GetActiveApplications();
                    var app = apps.FirstOrDefault(a => a.ProcessID == processID);
                    if (app != null)
                    {
                        // Simply return the extracted icon
                        var icon = IconHelper.ExtractIconFromExecutable(app.ProcessPath, 50);
                        if (icon != null)
                        {
                            return icon;
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Error extracting icon: {ex.Message}");
                }
            }

            // Fallback
            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(BitmapColor.Black);
                return builder.ToImage();
            }
        }
    }
}
