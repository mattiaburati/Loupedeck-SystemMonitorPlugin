namespace Loupedeck.PCMonitorPlugin
{
    using System;
    using Loupedeck.PCMonitorPlugin.Services;

    // Debug command to show all available MSI Afterburner/RivaTuner entries

    public class DebugMonitorCommand : PluginDynamicCommand
    {
        private readonly MSIAfterburnerReader _reader;
        private Int32 _pressCount = 0;

        public DebugMonitorCommand()
            : base(displayName: "Debug Monitor", description: "Shows all available monitoring entries in log", groupName: "Individual Metrics")
        {
            this._reader = new MSIAfterburnerReader();
            PluginLog.Info("Debug Monitor Command initialized");
        }

        protected override void RunCommand(String actionParameter)
        {
            this._pressCount++;
            this.ActionImageChanged();

            try
            {
                PluginLog.Info("========================================");
                PluginLog.Info("MSI Afterburner / RivaTuner Available Entries:");
                PluginLog.Info("========================================");

                var entries = this._reader.GetAvailableEntries();

                if (entries.Length == 0)
                {
                    PluginLog.Warning("No entries found! Make sure MSI Afterburner is running with RivaTuner Statistics Server.");
                }
                else
                {
                    PluginLog.Info($"Found {entries.Length} entries:");
                    for (var i = 0; i < entries.Length; i++)
                    {
                        PluginLog.Info($"[{i}] {entries[i]}");
                    }
                }

                PluginLog.Info("========================================");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error getting entries: {ex.Message}");
                PluginLog.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return $"Debug{Environment.NewLine}Monitor{Environment.NewLine}({this._pressCount})";
        }
    }
}
