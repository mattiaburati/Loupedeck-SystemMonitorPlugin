namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using System.Timers;
    using Loupedeck.PCMonitorPlugin.Services;

    // This command displays GPU usage percentage from MSI Afterburner

    public class GPUUsageCommand : PluginDynamicCommand
    {
        private readonly MSIAfterburnerReader _reader;
        private readonly Timer _updateTimer;
        private Single _currentUsage = 0;
        private Boolean _isAvailable = false;

        public GPUUsageCommand()
            : base(displayName: "GPU Usage", description: "Shows GPU usage from MSI Afterburner", groupName: "Individual Metrics")
        {
            this._reader = new MSIAfterburnerReader();

            this._updateTimer = new Timer(500); // Update every 500ms
            this._updateTimer.Elapsed += this.OnUpdateTimer;
            this._updateTimer.AutoReset = true;
            this._updateTimer.Start();

            PluginLog.Info("GPU Usage Command initialized");
        }

        private void OnUpdateTimer(Object sender, ElapsedEventArgs e)
        {
            try
            {
                if (this._reader.TryGetGPUUsage(out var usage))
                {
                    if (Math.Abs(this._currentUsage - usage) > 0.5f || !this._isAvailable)
                    {
                        this._currentUsage = usage;
                        this._isAvailable = true;
                        this.ActionImageChanged();
                    }
                }
                else
                {
                    if (this._isAvailable)
                    {
                        this._isAvailable = false;
                        this._currentUsage = 0;
                        this.ActionImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error updating GPU usage: {ex.Message}");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            // Optional: Log current value when pressed
            PluginLog.Info($"GPU Usage: {this._currentUsage}%");
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._isAvailable)
            {
                return $"GPU{Environment.NewLine}Usage{Environment.NewLine}N/A";
            }

            return $"GPU{Environment.NewLine}Usage{Environment.NewLine}{this._currentUsage:F0}%";
        }
    }
}
