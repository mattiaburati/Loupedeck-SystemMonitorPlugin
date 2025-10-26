namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Timers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This command displays GPU power consumption from MSI Afterburner

    public class GPUPowerCommand : PluginDynamicCommand
    {
        private readonly MSIAfterburnerReader _reader;
        private readonly Timer _updateTimer;
        private Single _currentPower = 0;
        private String _unit = "W";
        private Boolean _isAvailable = false;

        public GPUPowerCommand()
            : base(displayName: "GPU Power", description: "Shows GPU power consumption from MSI Afterburner", groupName: "Individual Metrics")
        {
            this._reader = new MSIAfterburnerReader();

            this._updateTimer = new Timer(500); // Update every 500ms
            this._updateTimer.Elapsed += this.OnUpdateTimer;
            this._updateTimer.AutoReset = true;
            this._updateTimer.Start();

            PluginLog.Info("GPU Power Command initialized");
        }

        private void OnUpdateTimer(Object sender, ElapsedEventArgs e)
        {
            try
            {
                if (this._reader.TryGetGPUPower(out var power, out var unit))
                {
                    if (Math.Abs(this._currentPower - power) > 0.5f || !this._isAvailable)
                    {
                        this._currentPower = power;
                        this._unit = String.IsNullOrEmpty(unit) ? "W" : unit;
                        this._isAvailable = true;
                        this.ActionImageChanged();
                    }
                }
                else
                {
                    if (this._isAvailable)
                    {
                        this._isAvailable = false;
                        this._currentPower = 0;
                        this.ActionImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error updating GPU power: {ex.Message}");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            // Optional: Log current value when pressed
            PluginLog.Info($"GPU Power: {this._currentPower} {this._unit}");
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._isAvailable)
            {
                return $"GPU{Environment.NewLine}Power{Environment.NewLine}N/A";
            }

            return $"GPU{Environment.NewLine}Power{Environment.NewLine}{this._currentPower:F0}{this._unit}";
        }
    }
}
