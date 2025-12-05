# SnapCaption

Real-time audio/speech translation tool based on Windows LiveCaptions.

## Description

SnapCaption integrates translation APIs with Windows Live Captions, enabling real-time speech translation on Windows 11. It combines Windows' built-in LiveCaptions with various translation services including LLMs and traditional translation APIs.

## Features

- Seamless integration with Windows LiveCaptions
- Modern Fluent UI with light/dark theme support
- Multiple translation services:
  - LLM-based: Ollama, OpenAI Compatible API, OpenRouter
  - Traditional: Google Translate, DeepL, Youdao, Baidu, MTranServer, LibreTranslate
- Overlay window for subtitle display
- Translation history with CSV export
- Flexible controls (always-on-top, pause/resume, copy text)

## Requirements

- Windows 11 (22H2+) with LiveCaptions support
- .NET 8.0+ runtime (or use self-contained version)

## Installation

1. Download the latest release from the releases page
2. Extract and run `SnapCaption.exe`

## Building from Source

### Prerequisites

- .NET 8.0 SDK or higher
- Windows 11

### Build Commands

```bash
# Build (Release)
make build

# Build (Debug)
make build-debug

# Clean build artifacts
make clean

# Publish self-contained executables
make publish

# Restore NuGet packages
make restore

# Run the application
make run
```

Or using .NET CLI directly:

```bash
dotnet build -c Release
dotnet run
```

## Configuration

Before first run:

1. Enable Windows LiveCaptions (Win + Ctrl + L)
2. Set LiveCaptions position to "Overlaid on screen" in settings
3. Configure your preferred translation API in SnapCaption settings

## License

Apache License 2.0 - See [LICENSE](LICENSE) for details.

## Credits

Based on the original [LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator) project by SakiRinn.