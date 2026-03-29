# DevPad Redesign — Claude Code Task List
> Avalonia UI (.NET) — Prioritized by impact. Execute phase by phase.

---

## PHASE 1 — Visual Foundation (Biggest Impact, Do First)

### 1.1 Color System Overhaul
- Define a global color resource file (e.g. `Themes/Colors.axaml`)
- Base background: `#0d1117` (deep charcoal, not pure black)
- Secondary surface: `#161b22` (sidebar, panels)
- Border color: `#30363d`
- Text primary: `#e6edf3`
- Text secondary: `#7d8590`
- Define format-specific accent colors:
  - JSON → `#58a6ff` (blue)
  - XML → `#f0883e` (orange)
  - JWT → `#bc8cff` (purple)
  - Base64 → `#3fb950` (green)
  - SQL → `#ffa657` (amber)
- Apply accent color dynamically to selected sidebar item and active tab underline

### 1.2 Typography
- Replace system default font with JetBrains Mono for all code/editor areas
- Use Segoe UI Variable or Inter for UI labels (sidebar, buttons, headers)
- Increase base font size slightly (editor: 13-14px, UI: 12-13px)
- Ensure monospace font is loaded as embedded resource if not system-available

### 1.3 Sidebar Redesign
- Increase sidebar width slightly (from ~240px to 260px)
- Add subtle top gradient or logo mark above "DevPad" title
- Style "DevPad" title: larger, slightly bolder, maybe with a subtle accent color dot or icon prefix
- Sidebar items: increase padding (16px vertical), larger icons (20px), proper spacing
- Selected item: use accent color background with left border indicator (3px solid accent)
- Hover state: `#21262d` background with smooth transition (0.15s ease)
- Add subtle divider between tool groups if needed

### 1.4 Button Hierarchy
- **Primary action** (Copy Output, Format): filled button with accent color background
- **Secondary action** (Copy, Minify): outlined button with border only
- **Danger action** (Clear): subtle styling, red tint on hover (`#da3633`)
- All buttons: 8px border radius, proper padding (8px 16px), consistent height (32px)
- Hover state: slight brightness increase with smooth transition

### 1.5 Panel & Layout Spacing
- Add 8px padding inside input and output panels (currently touching edges)
- Add subtle border between input and output: `#30363d` 1px
- Header area (where buttons live): give it a proper background `#161b22` with bottom border
- Remove any harsh contrasts, unify to the color system

---

## PHASE 2 — Interaction Polish

### 2.1 Copy Button Micro-interaction
- When "Copy" or "Copy Output" is clicked:
  - Button text changes to "✓ Copied!" 
  - Brief green color flash on button
  - After 1.5 seconds, revert back to original text
  - Show character count in tooltip or small label: "Copied 1,240 chars"

### 2.2 Clear Button Confirmation
- Add a simple confirmation step for Clear:
  - First click: button turns red, text changes to "Confirm Clear"
  - Second click or loses focus: executes clear
  - This prevents accidental data loss

### 2.3 Empty State Design
- Replace plain gray text in empty output panel with a proper empty state:
  - Centered icon (tool-specific: `{}` for JSON, `</>` for XML, etc.)
  - Subtitle: "Paste your [format] in the left panel"
  - Use accent color for the icon matching the active tool
  - Subtle opacity (40%) so it doesn't compete with actual content

### 2.4 Tab Switcher (TEXT / TREE)
- Style active tab with accent color underline (2px)
- Inactive tab: muted color, hover state
- Smooth transition between tabs (subtle fade 0.1s)

### 2.5 Format Auto-Detection on Paste
- When user pastes content into input:
  - Detect format heuristically:
    - Starts with `{` or `[` → JSON
    - Starts with `<` → XML
    - Three parts separated by `.` with base64 chars → JWT
    - Contains SELECT/INSERT/UPDATE/CREATE keywords → SQL
    - Otherwise → try Base64 decode
  - If detected format differs from current selection:
    - Show a small non-intrusive toast/banner: "Detected JSON — Switch?" with Yes/Dismiss
  - Do NOT auto-switch without user confirmation (respect user intent)

---

## PHASE 3 — Feature Additions (Wow Factor)

### 3.1 Syntax Highlighting in Output Panel
- Integrate AvaloniaEdit (already a mature Avalonia code editor component)
- Apply syntax highlighting for:
  - JSON output → JSON highlighting
  - XML output → XML highlighting  
  - SQL output → SQL highlighting
  - JWT decoded payload → JSON highlighting
- Line numbers on output panel (optional toggle)
- This is the single biggest "wow" feature upgrade

### 3.2 Input Area Upgrade
- Replace plain TextBox with AvaloniaEdit for input too
- Benefits: line numbers, better performance with large payloads, syntax highlighting on input
- Add drag-and-drop file support:
  - User drops a `.json`, `.xml`, `.sql` file → content loads into input
  - Show visual drop zone overlay when dragging over app

### 3.3 JWT Tool — Visual Upgrade
- JWT decoder currently probably shows raw decoded JSON
- Add visual sections: HEADER / PAYLOAD / SIGNATURE with color-coded panels
- Show expiration time in human-readable format: "Expires in 2 hours 14 minutes"
- If expired: show red "EXPIRED" badge
- This makes JWT tool genuinely useful and differentiated

### 3.4 Base64 Intelligence
- When decoding Base64:
  - Try to detect if result is an image (check for image magic bytes)
  - If image detected: show thumbnail preview below the decoded text
  - If valid JSON: offer "Open in JSON formatter" button
  - If plain text: just show decoded text as now

### 3.5 Input History (Simple)
- Store last 10 inputs per tool type in local app settings
- Small "History" dropdown button in header per tool
- Click to restore a previous input
- Helps when switching between formats and wanting to go back

### 3.6 Resizable Splitter
- Make the divider between input and output panels draggable
- User can give more space to output when reading large formatted results
- Snap to 50/50 on double-click
- Avalonia supports GridSplitter natively

---

## PHASE 4 — Polish & Release Prep

### 4.1 App Icon & Branding
- Design a proper DevPad icon (not default Avalonia icon)
- Simple concept: `< />` or `{ }` with a subtle gradient
- Apply to window title bar and taskbar

### 4.2 Window Chrome
- Consider custom title bar (remove default Windows chrome, draw own)
- Gives full control over the top area
- Avalonia supports `ExtendClientAreaToDecorationsHint`
- Add logo/icon in custom title bar left side

### 4.3 Settings Panel
- Simple settings accessible via gear icon in sidebar bottom:
  - Font size (editor)
  - Toggle line numbers
  - Toggle auto-detection
  - Clear history
  - Light/Dark theme toggle (optional)

### 4.4 Keyboard Shortcuts
- `Ctrl+Shift+F` → Format/Beautify
- `Ctrl+M` → Minify
- `Ctrl+Shift+C` → Copy Output
- `Ctrl+L` → Clear
- `Ctrl+1-5` → Switch tools (JSON, XML, JWT, Base64, SQL)
- Show shortcuts in button tooltips

### 4.5 About / Version Screen
- Simple About page: DevPad logo, version, "Made by Yasin"
- GitHub link when you publish it
- "Free & Open Source" badge

---

## Implementation Notes for Claude Code

**Tech context:**
- Avalonia UI framework (.NET 8)
- MVVM pattern (likely ReactiveUI or CommunityToolkit.Mvvm)
- Existing tools: JSON, XML, JWT, Base64, SQL

**Key packages to add if not present:**
- `AvaloniaEdit` — for syntax highlighted code editor
- `Avalonia.Svg` — if custom SVG icons needed

**Approach:**
- Start with Phase 1 (pure XAML styling changes, no logic changes)
- Phase 1 alone will transform the visual feel dramatically
- Each phase is independently deployable
- Do NOT break existing functionality — wrap changes around it

**File structure to target:**
- `App.axaml` → global styles and color resources
- `MainWindow.axaml` → layout and structure
- `Views/` → individual tool views
- `Themes/` → create this folder for color system

---

## Priority Order Summary

| Priority | Task | Effort | Impact |
|---|---|---|---|
| 🔴 1 | Color system + typography | Medium | Massive |
| 🔴 2 | Button hierarchy | Low | High |
| 🔴 3 | Sidebar redesign | Medium | High |
| 🟡 4 | Copy micro-interaction | Low | Medium |
| 🟡 5 | Empty state design | Low | Medium |
| 🟡 6 | AvaloniaEdit integration | High | Massive |
| 🟡 7 | JWT visual upgrade | Medium | High |
| 🟢 8 | Auto-detection on paste | Medium | Medium |
| 🟢 9 | Resizable splitter | Low | Medium |
| 🟢 10 | Keyboard shortcuts | Low | High |
| ⚪ 11 | Settings panel | Medium | Low |
| ⚪ 12 | Custom title bar | High | Medium |
| ⚪ 13 | Input history | Medium | Low |
