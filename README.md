# ClipMaster - Windows Clipboard Manager

A powerful, modern clipboard manager for Windows 10/11 that emulates and enhances the classic ClipMate functionality.

![ClipMaster Screenshot](docs/screenshot.png)

## Features

### Core Functionality
- **Automatic Clipboard Monitoring** - Captures all copied text without stealing focus
- **Persistent History** - Stores clipboard history in SQLite database (survives restarts)
- **Search** - Quickly find past clips with real-time search
- **Source Tracking** - Shows which application the text was copied from

### Text Transformations
| Transform | Description |
|-----------|-------------|
| UPPERCASE | Convert all text to uppercase |
| lowercase | Convert all text to lowercase |
| Title Case | Capitalize first letter of each word |
| Sentence case | Capitalize first letter of each sentence |
| Remove EOL | Join all lines into a single line |
| Trim Spaces | Remove extra whitespace |
| Sort Lines | Alphabetically sort all lines |
| Reverse | Reverse the entire text |
| Line Numbers | Add numbered prefixes to each line |
| Remove Duplicates | Remove duplicate lines |
| JSON Format | Pretty-print JSON data |

### Extraction Tools
- **Extract URLs** - Pull all web links from text
- **Extract Emails** - Find all email addresses
- **Extract Numbers** - Extract all numeric values
- **Remove Formatting** - Strip HTML tags

### User Experience
- **Sound Notifications** - Audio feedback when text is captured
- **Always on Top** - Optional stay-on-top window mode
- **Global Hotkeys** - Ctrl+Shift+V to show/hide
- **No Focus Stealing** - Window appears but doesn't interrupt your workflow
- **Export History** - Save all clips to a text file
- **Statistics** - Character, word, and line counts

## System Requirements

- Windows 10 (version 1809+) or Windows 11
- .NET 8.0 Runtime
- ~50MB disk space

## Installation

### Option 1: Build from Source

1. **Prerequisites**
   - Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Install [Visual Studio 2022](https://visualstudio.microsoft.com/) (optional, for IDE)

2. **Clone and Build**
   ```bash
   git clone https://github.com/yourusername/ClipMaster.git
   cd ClipMaster
   dotnet build --configuration Release
   ```

3. **Run**
   ```bash
   dotnet run --project ClipMaster.csproj
   ```

### Option 2: Download Release
Download the latest release from the [Releases page](https://github.com/yourusername/ClipMaster/releases).

## Usage

### Basic Operations

1. **Start the Application**
   - Launch ClipMaster.exe
   - The window will appear and begin monitoring the clipboard

2. **Copy Text**
   - Copy text from any application as usual (Ctrl+C)
   - ClipMaster will capture it and play a sound notification
   - The window shows without stealing focus

3. **Select a Clip**
   - Click any item in the left panel history list
   - The full text appears in the editor on the right

4. **Transform Text**
   - Click any transformation button to modify the text
   - Use Undo to revert changes

5. **Use the Transformed Text**
   - Click "Copy to Clipboard" to copy the modified text
   - Click "Paste & Send" to paste directly to the previous application

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Shift+V | Show/Hide ClipMaster window |
| Ctrl+Shift+C | Quick paste from history |
| Ctrl+Z | Undo (in text editor) |

### Settings

- **Sound** - Toggle audio notifications on/off
- **Always on Top** - Keep window above other applications

## File Locations

| File | Location |
|------|----------|
| Database | `%AppData%\ClipMaster\cliphistory.db` |
| Settings | `%AppData%\ClipMaster\settings.json` |
| Logs | `%AppData%\ClipMaster\logs\` |

## Customization

### Custom Sound Files
Place custom `.wav` files in the `Sounds` folder:
- `clip.wav` - Played when text is captured
- `error.wav` - Played on errors

### Startup with Windows
1. Open Settings
2. Enable "Start with Windows"
3. Optionally enable "Start Minimized"

## Troubleshooting

### Clipboard not being captured
- Ensure ClipMaster is running (check system tray)
- Some applications use private clipboard formats not captured by text monitoring
- Restart ClipMaster

### Hotkeys not working
- Another application may have registered the same hotkey
- Run ClipMaster as Administrator
- Check Windows keyboard settings

### High memory usage
- Reduce "Max History Items" in settings
- Use "Clear All" to remove old clips
- Restart the application

## Architecture

```
ClipMaster/
├── App.xaml(.cs)           # Application entry point
├── MainWindow.xaml(.cs)    # Main UI window
├── Models/
│   ├── ClipItem.cs         # Clipboard item model
│   └── AppSettings.cs      # Application settings
├── Services/
│   ├── ClipboardService.cs # Windows clipboard monitoring
│   ├── DatabaseService.cs  # SQLite persistence
│   ├── SoundService.cs     # Audio notifications
│   └── HotkeyService.cs    # Global hotkey handling
├── Helpers/
│   └── TextTransformHelper.cs # Text manipulation utilities
└── Resources/
    └── Sounds/             # Audio files
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by the classic [ClipMate](http://www.thornsoft.com/) application
- Built with [WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/) and [.NET 8](https://dotnet.microsoft.com/)
- Uses [SQLite](https://www.sqlite.org/) for data persistence

## Changelog

### v1.0.0 (Initial Release)
- Core clipboard monitoring
- History storage with SQLite
- Text transformations
- Sound notifications
- Global hotkeys
- Export functionality
