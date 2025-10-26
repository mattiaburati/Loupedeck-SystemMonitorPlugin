namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Timers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This command displays comprehensive GPU monitoring data

    public class GPUMonitorCommand : PluginDynamicCommand
    {
        private const Int32 TITLE_FONT_SIZE = 11;
        private const Int32 LABEL_FONT_SIZE = 12;
        private const Int32 VALUE_FONT_SIZE = 14;

        private readonly MSIAfterburnerReader _reader;
        private readonly Timer _updateTimer;

        private Single _gpuLoad = 0;
        private Single _gpuTemp = 0;
        private Single _gpuPower = 0;
        private String _tempUnit = "°C";
        private String _powerUnit = "W";
        private Boolean _isAvailable = false;

        public GPUMonitorCommand()
            : base(displayName: "GPU Monitor", description: "Shows GPU Load, Temperature and Power", groupName: "System Monitor")
        {
            this.IsWidget = true; // Use only bitmap image, no text overlay

            this._reader = new MSIAfterburnerReader();

            this._updateTimer = new Timer(1000); // Update every 1 second
            this._updateTimer.Elapsed += this.OnUpdateTimer;
            this._updateTimer.AutoReset = true;
            this._updateTimer.Start();

            PluginLog.Info("GPU Monitor Command initialized");
        }

        private void OnUpdateTimer(Object sender, ElapsedEventArgs e)
        {
            try
            {
                var hasLoad = this._reader.TryGetGPUUsage(out var load);
                var hasTemp = this._reader.TryGetGPUTemperature(out var temp, out var tempUnit);
                var hasPower = this._reader.TryGetGPUPower(out var power, out var powerUnit);

                if (hasLoad || hasTemp || hasPower)
                {
                    var changed = false;

                    if (hasLoad && Math.Abs(this._gpuLoad - load) > 0.5f)
                    {
                        this._gpuLoad = load;
                        changed = true;
                    }

                    if (hasTemp && Math.Abs(this._gpuTemp - temp) > 0.5f)
                    {
                        this._gpuTemp = temp;
                        this._tempUnit = String.IsNullOrEmpty(tempUnit) ? "°C" : tempUnit;
                        changed = true;
                    }

                    if (hasPower && Math.Abs(this._gpuPower - power) > 0.5f)
                    {
                        this._gpuPower = power;
                        this._powerUnit = String.IsNullOrEmpty(powerUnit) ? "W" : powerUnit;
                        changed = true;
                    }

                    if (changed || !this._isAvailable)
                    {
                        this._isAvailable = true;
                        this.ActionImageChanged();
                    }
                }
                else
                {
                    if (this._isAvailable)
                    {
                        this._isAvailable = false;
                        this.ActionImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error updating GPU monitor: {ex.Message}");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Info($"GPU Monitor - Load: {this._gpuLoad}%, Temp: {this._gpuTemp}{this._tempUnit}, Power: {this._gpuPower}{this._powerUnit}");
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) => null;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(BitmapColor.Black);

                var titleColor = BitmapColor.White;
                var labelColor = new BitmapColor(120, 255, 180);
                var valueColor = BitmapColor.White;

                // Title
                builder.DrawText("GPU", 0, 0, 90, 15, titleColor, TITLE_FONT_SIZE);

                if (!this._isAvailable)
                {
                    builder.DrawText("N/A", 0, 35, 90, 30, new BitmapColor(100, 100, 100), 12);
                    return builder.ToImage();
                }

                // Load
                builder.DrawText("L", 5, 18, 20, 22, labelColor, LABEL_FONT_SIZE);
                builder.DrawText($"{this._gpuLoad:F1}%", 22, 18, 68, 22, valueColor, VALUE_FONT_SIZE);

                // Temperature
                builder.DrawText("T", 5, 40, 20, 22, labelColor, LABEL_FONT_SIZE);
                builder.DrawText($"{this._gpuTemp:F1}°", 22, 40, 68, 22, valueColor, VALUE_FONT_SIZE);

                // Power
                builder.DrawText("P", 5, 62, 20, 22, labelColor, LABEL_FONT_SIZE);
                builder.DrawText($"{this._gpuPower:F1}W", 22, 62, 68, 22, valueColor, VALUE_FONT_SIZE);

                return builder.ToImage();
            }
        }
    }
}
