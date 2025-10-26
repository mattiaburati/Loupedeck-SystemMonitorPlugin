namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Timers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This command displays GPU core clock frequency from MSI Afterburner

    public class GPUClockCommand : PluginDynamicCommand
    {
        private readonly MSIAfterburnerReader _reader;
        private readonly Timer _updateTimer;
        private Single _currentClock = 0;
        private String _unit = "MHz";
        private Boolean _isAvailable = false;

        public GPUClockCommand()
            : base(displayName: "GPU Clock", description: "Shows GPU core clock from MSI Afterburner", groupName: "Individual Metrics")
        {
            this._reader = new MSIAfterburnerReader();

            this._updateTimer = new Timer(500); // Update every 500ms
            this._updateTimer.Elapsed += this.OnUpdateTimer;
            this._updateTimer.AutoReset = true;
            this._updateTimer.Start();

            PluginLog.Info("GPU Clock Command initialized");
        }

        private void OnUpdateTimer(Object sender, ElapsedEventArgs e)
        {
            try
            {
                if (this._reader.TryGetGPUClock(out var clock, out var unit))
                {
                    if (Math.Abs(this._currentClock - clock) > 1f || !this._isAvailable)
                    {
                        this._currentClock = clock;
                        this._unit = String.IsNullOrEmpty(unit) ? "MHz" : unit;
                        this._isAvailable = true;
                        this.ActionImageChanged();
                    }
                }
                else
                {
                    if (this._isAvailable)
                    {
                        this._isAvailable = false;
                        this._currentClock = 0;
                        this.ActionImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error updating GPU clock: {ex.Message}");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            // Optional: Log current value when pressed
            PluginLog.Info($"GPU Clock: {this._currentClock} {this._unit}");
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._isAvailable)
            {
                return $"GPU{Environment.NewLine}Clock{Environment.NewLine}N/A";
            }

            return $"GPU{Environment.NewLine}Clock{Environment.NewLine}{this._currentClock:F0}{this._unit}";
        }
    }
}
