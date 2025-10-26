# PC Monitor Plugin for Loupedeck

A comprehensive system monitoring plugin for Loupedeck devices that displays real-time PC performance metrics including FPS, CPU, GPU, and RAM usage.

![Loupedeck](https://img.shields.io/badge/Loupedeck-Plugin-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

### 🎮 FPS Monitoring
- Real-time FPS display from RivaTuner Statistics Server (RTSS)
- Automatic detection of active games
- Manual app selection for multi-process games (e.g., League of Legends)
- Displays application icon with current FPS

### 💻 System Monitoring
- **CPU Monitor**: Load, Temperature, and Power consumption
- **GPU Monitor**: Load, Temperature, and Power consumption
- **RAM Monitor**: Memory usage in GB

### 🎨 Clean UI Design
- Widget-style displays with no text overlay
- Custom bitmap rendering inspired by LibreHardwareMonitor
- Color-coded labels for easy identification
- Professional dark theme

### 🔧 Smart Detection
- Automatic detection of MSI Afterburner and RivaTuner installation
- User notifications for missing dependencies
- Process filtering to exclude monitoring tools

## Prerequisites

This plugin requires the following software to be installed and running:

1. **MSI Afterburner** - For CPU/GPU/RAM monitoring
   - Download: [MSI Afterburner Official Site](https://www.msi.com/Landing/afterburner)

2. **RivaTuner Statistics Server (RTSS)** - For FPS monitoring
   - Included with MSI Afterburner installation
   - Must be running for FPS monitoring to work

3. **Loupedeck Software** - Version 6.0 or higher
   - Download: [Loupedeck Official Site](https://loupedeck.com/)

## Installation

### Option 1: Install from Release
1. Download the latest release from the [Releases](../../releases) page
2. Double click on the lplug4 and install it

### Option 2: Build from Source
1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/PCMonitorPlugin.git
   cd PCMonitorPlugin
   ```

2. Build the project:
   ```bash
   dotnet build src/PCMonitorPlugin.csproj
   ```

3. The plugin will be automatically deployed to your Loupedeck plugins folder

## Usage

### Available Actions

#### System Monitors
Add these widgets to your Loupedeck layout for continuous monitoring:

- **CPU Monitor** - Shows CPU load (%), temperature (°C), and power (W)
- **GPU Monitor** - Shows GPU load (%), temperature (°C), and power (W)
- **RAM Monitor** - Shows RAM usage in GB
- **FPS Monitor** - Shows current FPS from the active game

#### FPS App Selector
Use this folder to manually select which application to monitor for FPS:

1. Add the "Select FPS App" folder to your layout
2. Press to open and see all running applications detected by RTSS
3. Select the game you want to monitor
4. The FPS Monitor will now track only that application
5. Select "Auto Mode" to return to automatic detection

### Configuration

The plugin works out of the box with default settings. Make sure:

1. **MSI Afterburner** is running with hardware monitoring enabled
2. **RivaTuner Statistics Server** is running
3. In-game overlay is enabled in RTSS for FPS detection

### Troubleshooting

**No data displayed:**
- Check that MSI Afterburner and RTSS are running
- Verify that hardware monitoring is enabled in MSI Afterburner
- Check the Loupedeck logs for detailed error messages

**FPS shows wrong application:**
- Use the "Select FPS App" folder to manually choose the correct process
- Some games launch through clients (e.g., League of Legends) - select the actual game process

**Metrics not updating:**
- Restart MSI Afterburner
- Restart the Loupedeck service
- Check Windows Task Manager to verify processes are running

## Project Structure

```
PCMonitorPlugin/
├── src/
│   ├── Actions/              # Plugin commands and widgets
│   │   ├── CPUMonitorCommand.cs
│   │   ├── GPUMonitorCommand.cs
│   │   ├── MEMMonitorCommand.cs
│   │   ├── FPSDisplayCommand.cs
│   │   └── FPSAppSelectorFolder.cs
│   ├── Services/             # Data readers
│   │   ├── MSIAfterburnerReader.cs
│   │   └── RTSSReader.cs
│   ├── Helpers/              # Utility classes
│   │   └── IconHelper.cs
│   └── PCMonitorPlugin.cs    # Main plugin class
├── package/                  # Plugin metadata
└── README.md
```

## Technical Details

### Data Sources

**MSI Afterburner Shared Memory (MAHM)**
- Memory-mapped file: `MAHMSharedMemory`
- Provides: CPU/GPU temperature, load, power, and RAM usage
- Signature: `0x4D48414D` or `0x4D41484D`

**RivaTuner Statistics Server Shared Memory**
- Memory-mapped file: `RTSSSharedMemoryV2`
- Provides: Per-application FPS data
- Signature: `0x52545353`
- FPS calculation: `1,000,000 / frameTime(µs)`

### Technologies Used

- **.NET 8.0** - Target framework
- **Loupedeck Plugin API** - Plugin infrastructure
- **System.Drawing.Common** - Icon extraction and image processing
- **P/Invoke (Win32 API)** - Memory-mapped file access and icon extraction

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

- **Author**: Spumiglio
- **Inspired by**: [LibreHardwareMonitor Loupedeck Plugin](https://github.com/notadoctor99/librehardwaremonitorplugin)
- **MSI Afterburner**: RivaTuner Statistics Server Team
- **Loupedeck**: Logi Plugin Service Team

## Acknowledgments

- Thanks to the MSI Afterburner team for providing shared memory access
- Thanks to the RivaTuner team for FPS monitoring capabilities
- Thanks to the LibreHardwareMonitor plugin for UI design inspiration

## Support

For issues, questions, or suggestions:
- Open an issue on [GitHub Issues](../../issues)
- Check existing issues for solutions

---

**Note**: This is an unofficial plugin and is not affiliated with or endorsed by Loupedeck, MSI, or RivaTuner.
