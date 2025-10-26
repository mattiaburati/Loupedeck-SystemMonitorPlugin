namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Timers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This command displays RAM memory usage monitoring

    public class MEMMonitorCommand : PluginDynamicCommand
    {
        private const Int32 TITLE_FONT_SIZE = 11;
        private const Int32 VALUE_FONT_SIZE = 16;

        private readonly MSIAfterburnerReader _reader;
        private readonly Timer _updateTimer;

        private Single _ramUsage = 0;
        private String _unit = "MB";
        private Boolean _isAvailable = false;

        public MEMMonitorCommand()
            : base(displayName: "RAM Monitor", description: "Shows RAM Memory Usage", groupName: "System Monitor")
        {
            this.IsWidget = true; // Use only bitmap image, no text overlay

            this._reader = new MSIAfterburnerReader();

            this._updateTimer = new Timer(1000); // Update every 1 second
            this._updateTimer.Elapsed += this.OnUpdateTimer;
            this._updateTimer.AutoReset = true;
            this._updateTimer.Start();

            PluginLog.Info("MEM Monitor Command initialized");
        }

        private void OnUpdateTimer(Object sender, ElapsedEventArgs e)
        {
            try
            {
                if (this._reader.TryGetRAMUsage(out var ramUsage, out var unit))
                {
                    // Only update if value changed significantly (avoid unnecessary redraws)
                    if (Math.Abs(this._ramUsage - ramUsage) > 10f || !this._isAvailable)
                    {
                        this._ramUsage = ramUsage;
                        this._unit = String.IsNullOrEmpty(unit) ? "MB" : unit;
                        this._isAvailable = true;
                        this.ActionImageChanged();
                    }
                }
                else
                {
                    if (this._isAvailable)
                    {
                        this._isAvailable = false;
                        this._ramUsage = 0;
                        this.ActionImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error updating MEM monitor: {ex.Message}");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Info($"MEM Monitor - Usage: {this._ramUsage}{this._unit}");
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
                builder.DrawText("RAM", 0, 0, 90, 15, titleColor, TITLE_FONT_SIZE);

                if (!this._isAvailable)
                {
                    builder.DrawText("N/A", 0, 32, 90, 30, new BitmapColor(100, 100, 100), 14);
                    return builder.ToImage();
                }

                // Value
                String valueText;
                if (this._ramUsage >= 1024 && this._unit == "MB")
                {
                    var ramGB = this._ramUsage / 1024f;
                    valueText = $"{ramGB:F1} GB";
                }
                else
                {
                    valueText = $"{this._ramUsage:F0} {this._unit}";
                }

                builder.DrawText(valueText, 0, 30, 90, 30, valueColor, VALUE_FONT_SIZE);

                return builder.ToImage();
            }
        }
    }
}
