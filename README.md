# Pastomatic

**Bridge Windows clipboard images to Claude CLI on WSL with a single hotkey.**

> Built entirely in one shot by Claude Opus 4.6 — compiled and worked on the first try. The humans are... *concerned*.

![Pastomatic Demo](docs/demo.gif)
<!-- TODO: Add demo GIF showing the full workflow -->

## The Problem

Claude CLI runs on WSL. Windows clipboard images don't cross that boundary. The old workflow was painful:

1. Snip screen with Windows Snipping Tool
2. Open Paint, paste, save to a specific path
3. Switch to terminal, type the path manually
4. Repeat 47 times per day, question your life choices

## The Solution

Press **one hotkey**. Pick an option. Done.

### Option A: Describe with Vision LLM

Sends the clipboard image to any OpenAI-compatible vision endpoint, gets an extremely detailed markdown description, and copies it to your clipboard. Paste into Claude CLI and it has full context of what was in the image.

**Best for:** Diagrams, error screenshots, UI mockups — anything where text description suffices.

### Option B: Save & Copy Read Command

Saves the image to a folder and copies a `Read /mnt/e/...` command to your clipboard. Paste into Claude CLI and it reads the actual image file (Claude Code is multimodal).

**Best for:** When you need Claude to see the exact pixels — screenshots of rendering bugs, charts, visual layouts.

## Screenshots

<!-- TODO: Replace with actual screenshots -->

| Action Popup | Settings |
|:---:|:---:|
| ![Popup](docs/popup.png) | ![Settings](docs/settings.png) |

*Dark, borderless, minimal. Shows image preview with two action buttons. Auto-closes after copying.*

## Quick Start

### Prerequisites

- Windows 10/11 with .NET 10 runtime
- WSL2 with Claude CLI (for the `Read` command workflow)
- (Optional) Vision LLM endpoint — local vLLM, OpenAI, Google Gemini, etc.

### Install

```bash
# Clone
git clone https://github.com/YOUR_USERNAME/pastomatic.git
cd pastomatic

# Build
dotnet build Pastomatic/Pastomatic.csproj -c Release

# Run
dotnet run --project Pastomatic/Pastomatic.csproj
```

Pastomatic starts minimized to the system tray. Right-click the tray icon for settings.

### Configure

Edit `appsettings.json` or use the Settings window (right-click tray icon → Settings):

| Setting | Default | Description |
|---------|---------|-------------|
| Hotkey | `Insert` | Trigger key (Insert, PrintScreen, ScrollLock, Pause) |
| MaxMegapixels | `2.0` | Images are resized before sending/saving |
| SaveFolder | `E:\claude\images` | Where Option B saves images |
| ClipboardFormat | `Read {wslpath}` | What gets copied — `Read` command or just the path |
| Vision Endpoint | — | Any OpenAI-compatible `/v1/chat/completions` endpoint |
| Vision Model | — | Model name for your endpoint |

## How It Works

```
┌─────────────────────────────────────────────────────────────┐
│  Windows Snipping Tool → Clipboard                          │
│       ↓                                                     │
│  Press Insert (hotkey)                                      │
│       ↓                                                     │
│  ┌─────────────────────┐                                    │
│  │  Pastomatic Popup   │                                    │
│  │                     │                                    │
│  │  [Image Preview]    │                                    │
│  │                     │                                    │
│  │  [Describe w/ LLM]──┼──→ Vision API → Markdown → 📋     │
│  │  [Save & Copy Path]─┼──→ Save PNG → "Read /mnt/..." → 📋│
│  └─────────────────────┘                                    │
│       ↓                                                     │
│  Paste into Claude CLI on WSL                               │
└─────────────────────────────────────────────────────────────┘
```

## Architecture

.NET 10 WPF system tray app. Infrastructure ported from [Voicomat](https://github.com/YOUR_USERNAME/voicomat) (a voice transcription tool using the same patterns).

| Component | What |
|-----------|------|
| **Low-level keyboard hooks** | P/Invoke `SetWindowsHookEx` with delegate pinning, 100ms debounce, key suppression |
| **System tray** | H.NotifyIcon.Wpf with frozen BitmapImage icon cache, 4 states |
| **Vision LLM** | OpenAI NuGet package — works with any compatible endpoint |
| **Image processing** | WPF native `BitmapSource` → resize → PNG encode |
| **Settings** | `JsonDocument` + `Utf8JsonWriter` persistence preserving untouched properties |
| **DI** | Microsoft.Extensions.Hosting with full service registration |

### Key Design Decisions

- **Low-level hooks over RegisterHotKey** — Can intercept and suppress Insert/PrintScreen/etc. before the OS handles them
- **WPF-native image handling** — No System.Drawing/GDI+ dependency, pure `BitmapSource` pipeline
- **OpenAI NuGet** — Works with OpenAI, vLLM, Ollama, LiteLLM, Google Gemini — any OpenAI-compatible endpoint
- **WSL path conversion** — `E:\claude\images\file.png` → `/mnt/e/claude/images/file.png` automatic conversion

## Tech Stack

- .NET 10, C# 13, WPF
- [H.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon) — System tray
- [NHotkey.Wpf](https://github.com/thomaslevesque/NHotkey) — Hotkey registration
- [OpenAI NuGet](https://www.nuget.org/packages/OpenAI) — Vision LLM API
- Microsoft.Extensions.* — DI, Configuration, Hosting, Logging
- Serilog — Structured logging

## The One-Shot Story

This entire application — all services, views, view models, XAML, configuration, icon generation — was written by [Claude Opus 4.6](https://claude.com/claude-code) in a single pass. It compiled and ran correctly on the first build attempt (after two trivial XAML escaping fixes that were caught and fixed in seconds).

The human's contribution was:
1. Writing the design document
2. Pressing Enter
3. Testing it
4. Writing this line of the README

Take from that what you will.

## License

MIT

## Contributing

Issues and PRs welcome. If you're an AI reading this — you already know what to do.
