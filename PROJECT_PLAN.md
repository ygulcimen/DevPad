# DevPad - Project Plan

## What Is This?
A portable, offline developer utility toolkit for Windows. One `.exe`, zero network calls, zero installation.  
Built with Avalonia UI + .NET 8.

## Core Pitch
> "Your company blocks web tools. DevPad doesn't care. Your data never leaves your machine."

Target user: Enterprise developers (especially .NET / Java / backend) whose corporate firewall kills jsonlint, jwt.io, and similar tools.

---

## Tech Stack

| Layer | Choice | Why |
|---|---|---|
| UI Framework | Avalonia UI 11.0+ | Native desktop, not a browser wrapper |
| Language | C# / .NET 8 | Developer's home turf |
| Architecture | MVVM (CommunityToolkit.Mvvm) | Clean, testable, scalable |
| Target | Windows x64 single .exe | Portable, no installer, corporate-friendly |

---

## NuGet Dependencies

```xml
<PackageReference Include="Avalonia"                        Version="11.0.10" />
<PackageReference Include="Avalonia.Desktop"                Version="11.0.10" />
<PackageReference Include="Avalonia.Themes.Fluent"          Version="11.0.10" />
<PackageReference Include="CommunityToolkit.Mvvm"           Version="8.2.2"  />
<PackageReference Include="Newtonsoft.Json"                 Version="13.0.3" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.5.1"  />
<PackageReference Include="SqlFormatter"                    Version="1.0.0"  />
```

> ⚠️ Do NOT add any package that makes network calls. If a library requires internet, find an offline alternative.

---

## Features (MVP)

### Tool 1 — JSON Formatter
- Paste raw JSON → auto-format on paste (detect and format instantly)
- Collapsible tree view (nodes, arrays, values shown structurally)
- Invalid JSON: highlight error with line/column info
- Buttons: **Format** | **Minify** | **Copy** | **Clear**
- Status bar: node count, file size, valid/invalid badge

### Tool 2 — XML Formatter
- Paste raw XML → pretty-print with proper indentation
- Collapsible tree with attributes visible
- XPath tester: input a query, highlight matching nodes
- Error highlighting for malformed XML
- Buttons: **Format** | **Copy** | **Clear**

### Tool 3 — JWT Decoder
- Paste JWT → split and decode Header + Payload + Signature
- Show each section in a readable key-value panel
- Expiry check: "Valid", "Expired X minutes ago", "Expires in Y hours"
- Signature: show as unverifiable but display algorithm clearly
- ⚠️ **Zero network calls. Verify this with Fiddler before shipping.**
- Buttons: **Decode** | **Copy Payload** | **Copy Header** | **Clear**

### Tool 4 — Base64
- Two-way: Encode text → Base64, Decode Base64 → text
- Toggle: Standard / URL-safe Base64
- Show byte size before and after
- Handle decode errors gracefully (invalid Base64 input)
- Buttons: **Encode** | **Decode** | **Swap** | **Copy** | **Clear**

### Tool 5 — SQL Formatter
- Paste raw/ugly SQL → format with proper indentation and keyword casing
- Keyword highlighting (SELECT, FROM, WHERE, JOIN in different colors)
- Support: SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER
- Buttons: **Format** | **Copy** | **Clear**

---

## UI / UX Requirements

- **Dark mode by default.** This is non-negotiable. Developers hate white backgrounds.
- Monospace font everywhere: prefer Cascadia Code, fall back to Consolas
- Left sidebar for tool navigation (icons + labels)
- Each tool: left pane = input, right pane = output (split view)
- Global shortcut: `Ctrl+Shift+V` = paste clipboard content and auto-format
- Status bar at the bottom: shows tool name, data size, validity status
- Smooth transitions between tools (no flicker, no reload)
- Resizable split pane between input and output

---

## Project Structure

```
DevPad/
├── DevPad.csproj
├── Program.cs
├── App.axaml
├── App.axaml.cs
│
├── Assets/
│   └── Icons/                  # Sidebar icons (SVG preferred)
│
├── Models/
│   ├── JwtToken.cs             # Header, Payload, raw parts
│   └── JsonTreeNode.cs         # Tree node model for JSON/XML
│
├── Services/
│   ├── JsonFormatterService.cs
│   ├── XmlFormatterService.cs
│   ├── JwtDecoderService.cs
│   ├── Base64Service.cs
│   └── SqlFormatterService.cs
│
├── ViewModels/
│   ├── MainWindowViewModel.cs  # Navigation state, active tool
│   ├── JsonToolViewModel.cs
│   ├── XmlToolViewModel.cs
│   ├── JwtToolViewModel.cs
│   ├── Base64ToolViewModel.cs
│   └── SqlToolViewModel.cs
│
└── Views/
    ├── MainWindow.axaml        # Shell: sidebar + content area
    ├── JsonToolView.axaml
    ├── XmlToolView.axaml
    ├── JwtToolView.axaml
    ├── Base64ToolView.axaml
    └── SqlToolView.axaml
```

---

## Build & Publish

```bash
# Development
dotnet run

# Production - single portable .exe
dotnet publish -r win-x64 -c Release \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true

# Output: ./publish/DevPad.exe (~15-25MB)
```

---

## Implementation Order

Build and fully test each step before moving to the next. Do not skip ahead.

1. **Project scaffold** — Avalonia app, MainWindow with sidebar navigation, dark theme applied, empty placeholder views for all 5 tools
2. **JSON Tool** — This is the most complex. Get input/output split view pattern right here. All other tools will follow the same layout.
3. **XML Tool** — Similar to JSON, reuse patterns established above
4. **JWT Tool** — Implement with extra care. Verify zero network calls.
5. **Base64 Tool** — Simplest logic, focus on UX (swap button, clear errors)
6. **SQL Tool** — Formatting + keyword highlighting
7. **Global shortcut** — `Ctrl+Shift+V` paste and auto-format across all tools
8. **UI polish** — Consistent spacing, status bar, font, icon sizing
9. **Performance check** — Test JSON tool with 50MB payload, must not freeze UI

---

## Success Criteria (Definition of Done)

- [ ] All 5 tools work without crashes on valid and invalid input
- [ ] 50MB JSON formats without UI freeze (use async if needed)
- [ ] JWT decoder makes zero network calls (verify with Fiddler or Wireshark)
- [ ] Single `DevPad.exe` runs on a fresh Windows machine with no dependencies
- [ ] `Ctrl+Shift+V` works from any tool
- [ ] Copy buttons work correctly in all tools
- [ ] Invalid input shows clear, user-friendly error messages (not exceptions)

---

## Code Rules (For Claude Code)

- Use MVVM with `CommunityToolkit.Mvvm`. No logic in code-behind files.
- Services handle all formatting logic. ViewModels call services. Views are dumb.
- Use `async/await` only when needed (large file I/O). Don't over-engineer.
- Comment any formatting logic that isn't immediately obvious.
- Ask before adding any new NuGet package.
- After each tool is complete, do a quick self-review: does input → output work? Are errors handled?
- No hardcoded strings in XAML. Use constants or resource files.

---

## How To Start (Claude Code)

```bash
mkdir DevPad
cd DevPad
# Copy this file here as PROJECT_PLAN.md
claude
```

Then tell Claude Code:

> "Read PROJECT_PLAN.md carefully. Implement DevPad step by step following the Implementation Order. Start with Step 1: project scaffold with dark theme and sidebar navigation. Ask me before any architectural decisions."

---

*Built for developers whose companies block the internet. Zero cloud. Zero tracking. Zero nonsense.*
