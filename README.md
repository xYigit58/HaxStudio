# HaxStudio

**HaxStudio** is a modern Windows desktop editor for creating and editing HaxBall `.hbs` stadium files.

Built with **WPF / C#**, HaxStudio focuses on a clean dark UI, visual map editing, safe JSON editing, validation, and a professional workflow for stadium creators.

> Status: Public release / v1.0.0

---

## Preview

![HaxStudio Splash Screen](assets/screenshots/splash-screen.png)

---

## Overview

HaxStudio is designed to make HaxBall stadium editing easier, safer, and more visual.

Instead of editing `.hbs` JSON files manually, users can open a stadium file, inspect objects, edit the map visually, adjust properties, validate the stadium, and save the result back as a valid `.hbs` file.

The project aims to provide a modern editor experience while preserving important HaxBall stadium data from real maps.

---

## Screenshots

### Splash Screen

![Splash Screen](assets/screenshots/splash-screen.png)

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

## Features

### File Management

- Open `.hbs` stadium files
- Save existing stadium files
- Save As support
- New stadium creation
- JSON Apply / Refresh workflow
- Manual JSON edits are applied before saving
- Invalid JSON prevents unsafe save
- Preserves important existing stadium data from real maps

### Supported Stadium Objects

- Vertexes
- Segments
- Discs
- Goals
- Planes
- Red spawn points
- Blue spawn points
- Joints

### Preserved HaxBall Data

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

Editable values include position, radius, color, curve, team, plane normal / distance, collision group, collision mask, trait, visibility, bounce coefficient, inverse mass, damping, speed, and gravity.

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

---

## Validator

HaxStudio includes a stadium validator to help prevent broken stadium files.

The validator can detect invalid segment vertex indexes, invalid joint disc indexes, invalid disc radius values, invalid coordinates, invalid goal points, invalid plane normal / distance values, invalid color values, unusual collision groups, unusual collision masks, and invalid stadium or background dimensions.

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

## Technology

- Language: C#
- UI Framework: WPF
- Platform: Windows
- File Type: HaxBall `.hbs` stadium JSON
- IDE: Visual Studio

---

## Roadmap

### v1.1.0

- Config system
- Installer support
- Persistent user settings
- Improved update workflow

---

## Version

```text
v1.0.0
```