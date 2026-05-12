# HaxStudio

**HaxStudio** is a modern native Windows desktop editor for creating and editing HaxBall `.hbs` stadium files.

Built with **WPF / C#**, HaxStudio focuses on visual stadium editing, advanced joint-heavy map workflows, moving segment map support, safer saving, validation, crash recovery, and a professional workflow for HaxBall stadium creators.

> Status: Public release / v1.2.0

---

## Preview

![HaxStudio Splash Screen](assets/screenshots/splash-screen.png)

---

## Overview

HaxStudio is designed to make HaxBall stadium editing easier, safer, and more visual.

Instead of editing `.hbs` JSON files manually, users can open a stadium file, inspect objects, edit the map visually, adjust properties, validate the stadium, and save the result back as a valid `.hbs` file.

The project aims to provide a modern editor experience while preserving important HaxBall stadium data from real maps. HaxStudio is especially useful for advanced maps that are difficult to manage in browser-based editors, including maps with many joints, discs, curves, and moving segment systems.

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

## Key Features in v1.2.0

### Advanced Joint and Moving Segment Map Editing

HaxStudio is designed to support complex HaxBall stadiums with many discs, joints, curves, and segment structures.

- Better workflow for maps with many joints
- Joint editing support for advanced HaxBall stadium mechanics
- Easier editing for moving segment based maps
- Better support for complex disc / joint / segment structures
- Useful for advanced mechanical maps that are hard to edit manually
- Helpful for stadium types that browser-based editors do not handle comfortably

### Recent Files

HaxStudio now remembers recently opened and saved stadium files.

- Added `File > Recent Files`
- Keeps the last 10 recently opened or saved stadium files
- Added `Clear Recent Files`
- Missing files are automatically removed from the recent list
- Recent files are saved between program launches

### Crash Recovery

HaxStudio includes a crash recovery workflow to help protect unsaved stadium work.

- AutoSave recovery state tracking
- Startup recovery dialog when recoverable work exists
- Recovery options: `Recover`, `Open Folder`, and `Discard`
- Recovered maps are marked as unsaved until manually saved
- Successful Save / Save As clears recovery state

### Save / Export Safety

Saving and exporting is safer, especially for maps with color formatting issues.

- Optional validation before save/export
- Short or invalid color detection
- `Auto-fix & Save`
- `Save Original`
- `Cancel`
- Mappers are not forced to auto-fix their files

### Stadium Validator

The validator helps prevent broken stadium files before export or release.

- Segment vertex index validation
- Goal format validation
- Joint disc index validation
- Short / invalid color detection
- NaN / Infinity detection
- `ballPhysics` reference checks
- Trait reference checks

### Large Map Performance

HaxStudio includes performance-focused improvements for large and complex stadiums.

- Render throttling
- Viewport culling
- Lazy JSON preview updates
- Lazy Layers panel updates
- Optimized segment creation refresh flow
- Performance profiler support
- Better editing experience for large joint-heavy maps

---

## File Management

- Open `.hbs` stadium files
- Save existing stadium files
- Save As support
- New stadium creation
- Recent Files menu
- JSON Apply / Refresh workflow
- Manual JSON edits are applied before saving
- Invalid JSON prevents unsafe save
- Optional save/export validation flow
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
- Object extension data
- Unknown / extra JSON fields where supported

---

## Editor Tools

- Modern visual stadium viewport
- HaxPuck-inspired grass background
- Zoom and pan support
- Reset viewport
- Grid display option
- Snap to Grid
- Adjustable grid size
- Adjustable vertex size
- Optional invisible object display
- Optional plane display
- Optional background stripe display
- Segment line and curve rendering
- HaxBall-style arc visualization
- Curve handle editing
- Overlapping segment selection by repeated clicking
- Viewport mini toolbar
- Mirror selected horizontally / vertically
- Auto Mirror placement mode

---

## Selection System

- Click selection
- Ctrl + click multi-selection
- Left drag selection for objects fully inside the rectangle
- Right drag selection for objects touched by the rectangle
- Multi-delete
- Drag selected objects
- Drag multiple selected objects together
- Clear selection with Escape
- Selected object highlight in viewport
- Selected object highlight in Layers panel

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

Editable values include position, radius, color, curve, team, plane normal / distance, collision group, collision mask, trait, visibility, bounce coefficient, inverse mass, damping, speed, gravity, and joint values.

---

## Layers Panel

- Search box
- Type filter
- Selected object highlight
- Double click object to focus in viewport
- Right click context menu
- Duplicate object
- Delete object
- Hide / Show object
- Lock / Unlock object
- Show all hidden objects
- Unlock all locked objects
- Lazy update support for large maps

---

## JSON Editor

HaxStudio includes a built-in JSON editor for advanced editing.

- View current stadium JSON
- Edit JSON manually
- Apply JSON changes to the editor
- Refresh JSON from editor data
- JSON edits update the viewport
- JSON edits are applied before saving
- Invalid JSON prevents unsafe saving
- Lazy preview update support for large maps
- JSON search support
- Syntax highlighting

---

## Settings / Preferences

HaxStudio includes an in-app Settings window.

Settings categories:

- Hotkeys
- Preferences
- Themes
- Language
- Check for Updates
- About HaxStudio

---

## Update System

HaxStudio includes an update checker that reads the latest release manifest from the update repository.

- Current version display
- Latest version check
- Release notes display
- Release page / installer download support

---

## Hotkeys

### Tools

| Shortcut | Action |
|---|---|
| Ctrl + L | Select Tool |
| Ctrl + E | Add Vertex |
| Ctrl + T | Add Segment |
| Ctrl + I | Add Disc |
| Ctrl + G | Add Goal |
| Ctrl + P | Add Plane |
| Ctrl + R | Add Red Spawn |
| Ctrl + B | Add Blue Spawn |

### Editing

| Shortcut | Action |
|---|---|
| Ctrl + Z | Undo |
| Ctrl + Y | Redo |
| Ctrl + C | Copy |
| Ctrl + V | Paste |
| Ctrl + D | Duplicate |
| Delete / Backspace | Delete selected object(s) |
| Escape | Clear selection |

### Viewport

| Shortcut | Action |
|---|---|
| Mouse Wheel | Zoom in / out |
| Middle Mouse Drag | Pan viewport |
| Space + Left Drag | Pan viewport |
| F / Home | Reset viewport |
| Ctrl + Click | Multi-select |
| Left Drag | Select fully contained objects |
| Right Drag | Select touched objects |

---

## Download

Download the latest Windows installer from the GitHub Releases page.

Latest release:

```text
HaxStudio v1.2.0
```

Installer asset:

```text
HaxStudio_Setup_v1.2.0
```

---

## Platform

- Windows x64
- Native WPF desktop application
- HaxBall `.hbs` stadium JSON files

---

## Technology

- Language: C#
- UI Framework: WPF
- Runtime: .NET 8 Windows
- Platform: Windows
- File Type: HaxBall `.hbs` stadium JSON
- IDE: Visual Studio

---

## Version History

### v1.2.0

- Advanced joint-heavy map editing workflow
- Moving segment map editing support
- Recent Files system
- Crash Recovery system
- Safer save/export validation flow
- Improved Stadium Validator checks
- Large map performance improvements
- Updated screenshots and README

### v1.1.0

- Config system
- Installer support
- Persistent user settings
- Improved update workflow
- AutoSave backup system
- Improved panel layout workflow

### v1.0.0

- Initial public release
- Native Windows HaxBall stadium editor
- Visual viewport editing
- JSON editing
- Inspector, Layers, and Validator panels

---

## Notes

HaxStudio is recommended for HaxBall mappers working with advanced stadiums, large maps, joint-heavy mechanics, moving segment systems, or long editing sessions.
