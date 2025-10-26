namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Timers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This command displays the current framerate from RivaTuner Statistics Server

    public class FPSDisplayCommand : PluginDynamicCommand
    {
        private const Int32 TITLE_FONT_SIZE = 11;
        private const Int32 VALUE_FONT_SIZE = 18;

        private readonly RTSSReader _rtssReader;
        private readonly Timer _updateTimer;
        private Single _currentFps = 0;
        private Boolean _isAvailable = false;

        // Static shared state for app selection
        public static UInt32? SelectedProcessID = null;
        public static String SelectedProcessName = null;

        // Initializes the command class
        public FPSDisplayCommand()
            : base(displayName: "FPS Monitor", description: "Shows current FPS from RivaTuner", groupName: "PC Monitor")
        {
            this.IsWidget = true; // Use only bitmap image, no text overlay

            this._rtssReader = new RTSSReader();

            // Set up a timer to update the FPS value periodically
            this._updateTimer = new Timer(500); // Update every 500ms
            this._updateTimer.Elapsed += this.OnUpdateTimer;
            this._updateTimer.AutoReset = true;
            this._updateTimer.Start();

            PluginLog.Info("FPS Display Command initialized");
        }

        // Timer callback to update FPS value
        private void OnUpdateTimer(Object sender, ElapsedEventArgs e)
        {
            try
            {
                // If a process is manually selected, check if it's still running
                if (SelectedProcessID.HasValue)
                {
                    var isStillRunning = false;
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById((Int32)SelectedProcessID.Value);
                        isStillRunning = !process.HasExited;
                    }
                    catch
                    {
                        // Process no longer exists
                    }

                    if (!isStillRunning)
                    {
                        // Selected process closed, return to auto mode
                        PluginLog.Info($"Selected process {SelectedProcessName} closed, returning to auto mode");
                        SelectedProcessID = null;
                        SelectedProcessName = null;
                    }
                }

                if (this._rtssReader.TryGetFramerate(out var fps, SelectedProcessID))
                {
                    // Only update if the value has changed significantly (avoid unnecessary redraws)
                    if (Math.Abs(this._currentFps - fps) > 0.5f || !this._isAvailable)
                    {
                        this._currentFps = fps;
                        this._isAvailable = true;
                        this.ActionImageChanged(); // Notify Loupedeck that the display needs to be updated
                    }
                }
                else
                {
                    // RivaTuner not available or selected process has no FPS data
                    if (this._isAvailable)
                    {
                        this._isAvailable = false;
                        this._currentFps = 0;
                        this.ActionImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error updating FPS: {ex.Message}");
            }
        }

        // This method is called when the user presses the button - just log debug info
        protected override void RunCommand(String actionParameter)
        {
            try
            {
                var debugInfo = this._rtssReader.GetDebugInfo();
                PluginLog.Info("========================================");
                PluginLog.Info("RTSS Debug Information:");
                PluginLog.Info(debugInfo);
                PluginLog.Info("========================================");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error logging RTSS info: {ex.Message}");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) => null;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(BitmapColor.Black);

                var titleColor = BitmapColor.White;
                var valueColor = BitmapColor.White;

                // Title
                builder.DrawText("FPS", 0, 0, 90, 15, titleColor, TITLE_FONT_SIZE);

                if (!this._isAvailable)
                {
                    builder.DrawText("N/A", 0, 32, 90, 30, new BitmapColor(100, 100, 100), 14);
                    return builder.ToImage();
                }

                // FPS Value - centered
                builder.DrawText($"{this._currentFps:F0}", 0, 30, 90, 30, valueColor, VALUE_FONT_SIZE);

                return builder.ToImage();
            }
        }
    }
}
