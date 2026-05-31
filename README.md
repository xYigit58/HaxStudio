# HaxStudio

[![Discord](https://img.shields.io/badge/Discord-Join%20Community-5865F2?logo=discord&logoColor=white)](https://discord.gg/3aDwUt88td)
[![Latest Release](https://img.shields.io/github/v/release/xYigit58/HaxStudio?label=latest%20release)](https://github.com/xYigit58/HaxStudio/releases/latest)

**HaxStudio** is a modern native Windows desktop editor for creating and editing HaxBall `.hbs` stadium files.

Built with **WPF / C#**, HaxStudio focuses on visual stadium editing, advanced map workflows, multi-renderer viewport performance, decorative segment generation, text-to-segment creation, image/SVG-to-segment importing, validation, cleanup tools, safer saving, and a professional workflow for HaxBall stadium creators.

> Status: Public release / v1.5.0  
> Current focus: Stable v1.6.0 release

---

## Download

Download the latest installer from GitHub Releases:

**HaxStudio v1.5.0**  
https://github.com/xYigit58/HaxStudio/releases/tag/v1.5.0

Installer asset:

```text
HaxStudio_Setup_v1.5.0.exe
```

If Windows SmartScreen appears, choose **More info > Run anyway**.

---

## Community

Join the official HaxStudio Discord community to get support, report bugs, suggest features, share maps, and follow development updates.

https://discord.gg/3aDwUt88td

---

## Preview

![HaxStudio Splash Screen](assets/screenshots/splash-screen.png)

---

## Overview

HaxStudio is designed to make HaxBall stadium editing easier, safer, faster, and more visual.

Instead of editing `.hbs` JSON files manually, users can open a stadium file, inspect objects, edit the map visually, generate decorative content, validate the stadium, and save/export the result back as a valid `.hbs` file.

HaxStudio is especially useful for advanced maps that are difficult to manage in browser-based editors, including maps with many joints, discs, curves, moving segment systems, decorative logos, dense object layouts, generated text, and imported image/SVG outlines.

---

## Screenshots

### Main Editor

![Main Editor](assets/screenshots/main-editor.png)

### JSON Editor

![JSON Editor](assets/screenshots/json-editor.png)

### Settings

![Settings](assets/screenshots/settings.png)

### Layers Panel

![Layers Panel](assets/screenshots/layers-panel.png)

### Validator

![Validator](assets/screenshots/validator.png)

---

## What's New in v1.5.0

HaxStudio v1.5.0 is a major workflow, renderer, localization, image/SVG import, and release-polish update.

### Command Palette

A VS Code-style **Command Palette** was added for quickly running editor commands.

Shortcut:

| Shortcut | Action |
|---|---|
| Ctrl + Shift + P | Open Command Palette |

Example commands:

- Open Stadium
- Save Stadium
- Preferences
- Renderer Benchmark
- Validate Stadium
- Cleanup tools
- Text to Segments
- Smart Trace Image as Segments
- Viewport toggles
- Standard stadium templates

### Renderer Benchmark

HaxStudio now includes a **Renderer Benchmark** panel for measuring active viewport performance.

It shows:

- Current renderer
- Average render time
- Minimum / maximum render time
- Estimated FPS
- Snapshot build time
- Object counts
- Selected object count
- Viewport information

Useful for comparing Canvas, Direct2D, OpenGL, and Vulkan performance on your system.

### Turkish Language Support

HaxStudio now includes initial **Türkçe** language support.

Language support:

- English
- Türkçe

Many menus, panels, dialogs, popups, tooltips, validation messages, splash/loading messages, release notes, and runtime-created UI messages are translated.

Technical terms such as **Vertex**, **Segment**, **Disc**, **Plane**, **Joint**, **Spawn**, **Renderer**, **Canvas**, **Direct2D**, **OpenGL**, **Vulkan**, **JSON**, **cMask**, **cGroup**, and **trait** are intentionally kept unchanged.

### What's New Popup

A new **What's New in v1.5.0** popup was added.

- Appears automatically only once
- Can be opened again from the Help menu
- Includes release highlights
- Includes Discord access
- Supports English and Türkçe

### About HaxStudio Polish

The About section was improved with clearer release information:

- HaxStudio version
- Current focus
- Renderer support
- Language support
- Community / Discord information

### Discord Support

Discord access was added inside HaxStudio.

- Help > Join Discord
- About HaxStudio > Join Discord
- What's New popup Discord button

---

## Text to Segments

Type something with your keyboard and let HaxStudio turn it into clean HaxBall segments for you.

Text to Segments features:

- Font support
- Remembers latest used settings
- Adjustable text size
- Adjustable segment color
- Font quality options
- Optional no-collision decorative output
- Click-and-drag placement inside the viewport
- Automatically selects generated text after placement
- Segment-based background drawing behind generated text

This makes it easier to add names, titles, labels, server branding, decorative text, and map details directly into `.hbs` stadiums.

---

## Image to Segments / SVG Logo Import

Upload a logo and let HaxStudio bring it to life inside HaxBall.

HaxStudio includes a Smart Trace Image to Segments workflow for converting images and SVG logos into editable HaxBall segments.

Supported formats:

- SVG
- PNG
- JPG / JPEG
- BMP

Image to Segments features:

- Image tracing presets
- Remembered settings
- Preview variants
- Output color control
- No-collision decorative mode
- Trace quality settings
- Edge style settings
- HaxBall vertex/segment limit protection
- Click-and-drag placement inside the viewport

SVG import features:

- SVG path import
- Polygon / polyline import
- Rect / circle / ellipse / line import
- SVG text handling when real `<text>` elements exist
- SVG logo-focused cleanup options
- SVG Logo Simplifier v2

SVG Logo Simplifier v2 includes:

- SVG Detection Mode
- Keep Details mode
- Ignore tiny holes
- Ignore tiny inner paths
- Smooth curved contours
- Prefer clean rings/circles
- More aggressive point merge for SVG

This is especially useful for decorative logos, club crests, icons, symbols, and map branding.

Complex logos may still require manual cleanup after import.

---

## Multi-Renderer Viewport System

HaxStudio includes a complete multi-renderer viewport system for better performance and compatibility.

Available renderers:

- Canvas fallback renderer
- Direct2D renderer
- OpenGL renderer
- Vulkan renderer

Renderer features:

- Renderer selection in Preferences
- Automatic renderer setting save support
- Viewport renderer badge
- Real-time render ms updates
- Renderer Benchmark panel
- Warning when switching renderer while a stadium is open
- Improved consistency across Canvas, Direct2D, OpenGL, and Vulkan

---

## Editor Tools

HaxStudio includes a growing set of visual editing and workflow tools.

### Core Tools

- Select Tool
- Move Tool
- Add Vertex
- Add Segment
- Add Disc
- Add Goal
- Add Plane
- Add Red Spawn
- Add Blue Spawn
- Add Joint
- Measure Tool
- Mirror placement modes
- Rotation handle

### Viewport Tools

- Zoom and pan
- Reset viewport
- Grid display
- Snap to Grid
- Custom snap size menu
- Adjustable vertex size
- Optional invisible object display
- Optional plane display
- Optional background stripe display
- Viewport mini toolbar
- Renderer badge

### Generated Content

- Text to Segments
- Insert Shape
- Smart Trace Image as Segments
- SVG Logo import
- Segment-based text backgrounds
- Decorative no-collision output by default

---

## Layers Panel

The Layers panel helps manage dense maps and generated objects.

Features include:

- Object list grouped by type
- Search / filtering
- Selected object highlighting
- Hide / show support
- Lock / unlock support
- Copy coordinates
- Copy JSON
- Improved full JSON copy behavior for selected objects
- Better workflow for generated logos, text, and dense decorative maps

---

## Validation and Cleanup

HaxStudio includes practical validation and cleanup tools for real HaxBall maps.

Validation features:

- HaxBall compatibility status
- Critical / warning / info severity
- Object issue list
- Copy validation messages
- Focus/select issue targets where possible
- Safer save/export validation warnings

Cleanup tools:

- Cleanup Safe Issues
- Remove Unused Vertexes
- Remove Unused Traits
- Capacity reporting
- Safer cleanup for invalid or unnecessary data

The validator is designed to be practical for real community maps: critical issues are reserved for problems that are likely to block HaxBall loading, while mapper-quality notes are shown as warnings or info.

---

## File Management

- Open `.hbs` stadium files
- Save existing stadium files
- Save As support
- Export Stadium workflow
- New stadium creation
- Recent Files menu
- JSON Apply / Refresh workflow
- Manual JSON edits are applied before saving
- Invalid JSON prevents unsafe save
- Optional save/export validation flow
- Safe export compatibility report
- Preserves important existing stadium data from real maps

---

## Supported Stadium Objects

- Vertexes
- Segments
- Discs
- Goals
- Planes
- Red spawn points
- Blue spawn points
- Joints

---

## Preserved HaxBall Data

HaxStudio is designed to keep important HaxBall stadium fields intact when loading and saving existing maps.

Preserved data includes:

- `traits`
- `trait`
- `cMask`
- `cGroup`
- `playerPhysics`
- `ballPhysics`
- `canBeStored`
- Curve data
- Joint data
- Object extension data
- Unknown / extra JSON fields where supported

---

## Selection System

- Click selection
- Ctrl + click multi-selection
- Ctrl + A select all
- Left drag selection for objects fully inside the rectangle
- Right drag selection for objects touched by the rectangle
- Multi-delete
- Drag selected objects
- Dedicated Move tool
- Drag multiple selected objects together
- Rotate selected objects from the viewport
- Copy selected object groups
- Paste copied object groups into the same map or another map
- Paste capacity checks
- Clear selection with Escape
- Selected object highlight in viewport
- Selected object highlight in Layers panel
- Selection statistics and bounds information

---

## Inspector

The Inspector panel shows and edits properties for the selected object.

Supported inspector sections include:

- Vertex properties
- Segment properties
- Disc properties
- Goal properties
- Plane properties
- Spawn point properties
- Joint information
- Multi-select common properties
- Selection information panel
- Selection statistics and paste cost information

Editable values include position, radius, color, curve, team, collision values, physics values, joint values, spawn coordinates, and more.

---

## JSON Editor

HaxStudio includes an integrated JSON editor for advanced users.

- View current `.hbs` JSON
- Apply manual JSON edits back into the visual editor
- Refresh JSON from current visual data
- JSON syntax highlighting
- JSON search
- Safer save/export preparation
- Preserves important HaxBall data where supported

---

## AutoSave / Recovery

HaxStudio includes AutoSave support to reduce data-loss risk.

- Optional AutoSave
- Configurable interval
- Custom AutoSave folder support
- Backup file generation
- Preferences integration

---

## Anonymous Usage Analytics

HaxStudio includes optional anonymous usage analytics.

The setting can be turned off anytime in Preferences.

HaxStudio only sends minimal anonymous app usage data:

- App start activity
- App version
- Selected renderer
- Anonymous install ID

HaxStudio does **not** send:

- Map files
- Map names
- File paths
- Usernames
- Emails
- Locations
- Discord information
- Personal content

---

## System Requirements

- Windows 10 / Windows 11
- x64 system recommended
- .NET Desktop Runtime if using a framework-dependent build
- GPU driver recommended for accelerated renderers

Renderer notes:

- Canvas is the safest fallback renderer.
- Direct2D is recommended for most users.
- OpenGL and Vulkan availability depends on system drivers.

---

## Installation

1. Download `HaxStudio_Setup_v1.5.0.exe` from Releases.
2. Run the installer.
3. Launch HaxStudio.
4. Choose your preferred renderer in Preferences.
5. Open or create a `.hbs` stadium.

---

## Known Notes

- Complex Image to Segment / SVG logo imports may still require manual cleanup.
- Very dense maps can still hit HaxBall object limits.
- Some renderer behavior may depend on GPU driver support.
- Turkish language support is actively improving; some technical terms intentionally remain English.

---

## Roadmap / Current Focus

Current focus for the next major work cycle:

**Stable v1.6.0 release**

Likely future focus areas:

- More Image to Segment and SVG logo quality improvements
- Better localization coverage
- More release polish and bug fixes
- Mapper workflow improvements
- Community feedback from Discord and Reddit

---

## License

HaxStudio is currently distributed as a compiled public release.  
All rights reserved unless a separate license file states otherwise.

---

## Links

- Latest Release: https://github.com/xYigit58/HaxStudio/releases/tag/v1.5.0
- Discord: https://discord.gg/3aDwUt88td
