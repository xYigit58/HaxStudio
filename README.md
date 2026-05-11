# HaxStudio

**HaxStudio** is a modern Windows desktop editor for creating and editing HaxBall `.hbs` stadium files.

Built with **WPF / C#**, HaxStudio focuses on a clean dark UI, visual map editing, safe JSON editing, validation, and a professional workflow for stadium creators.

> Status: Private source / v1.0 preparation

---

## Overview

HaxStudio is designed to make HaxBall stadium editing easier, safer, and more visual.

Instead of editing `.hbs` JSON files manually, users can open a stadium file, inspect objects, edit the map visually, adjust properties, validate the stadium, and save the result back as a valid `.hbs` file.

The project aims to provide a modern editor experience while preserving important HaxBall stadium data from real maps.

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

---

### Supported Stadium Objects

HaxStudio currently supports editing and visualizing the main HaxBall stadium object types:

- Vertexes
- Segments
- Discs
- Goals
- Planes
- Red spawn points
- Blue spawn points
- Joints

---

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

This is important because real `.hbs` maps often contain advanced physics, collision, trait, and storage data that should not be lost during editing.

---

## Viewport Editor

The viewport is the main visual editing area of HaxStudio.

### Viewport Features

- Modern visual stadium viewport
- HaxPuck-inspired grass background
- Zoom support
- Pan support
- Reset viewport
- Grid display option
- Snap to Grid
- Adjustable grid size
- Adjustable vertex size
- Optional invisible object display
- Optional plane display
- Optional background stripe display
- Right panel resize support

### Segment Rendering

- Segment line rendering
- Segment curve rendering
- HaxBall-style arc visualization
- Curve handle editing
- Overlapping segment selection by repeated clicking

---

## Selection System

HaxStudio includes a visual selection system designed for practical map editing.

### Selection Features

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

The Inspector panel shows and edits properties for the currently selected object.

### Smart Inspector

The Inspector changes based on the selected object type.

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

### Object Properties

Depending on the selected object, HaxStudio can edit values such as:

- Position
- Radius
- Color
- Curve
- Team
- Plane normal / distance
- Collision group
- Collision mask
- Trait
- Visibility
- Bounce coefficient
- Inverse mass
- Damping
- Speed
- Gravity

---

## Layers Panel

The Layers panel helps users manage objects in larger stadiums.

### Layers Features

- Search box
- Type filter
- Selected object highlight
- Double click object to focus in viewport
- Right click context menu
- Select object
- Focus object in viewport
- Duplicate object
- Delete object
- Hide / Show object
- Lock / Unlock object
- Show all hidden objects
- Unlock all locked objects

---

## Validator

HaxStudio includes a stadium validator to help prevent broken stadium files.

### Validator Features

- Stadium validation panel
- Validate button
- Error / warning result list
- Optional save warning before saving invalid stadiums
- Double click validation result to focus the related object in the viewport

### Validation Checks

The validator can detect issues such as:

- Invalid segment vertex indexes
- Invalid joint disc indexes
- Invalid disc radius values
- Invalid vertex coordinates
- Invalid disc coordinates
- Invalid spawn point coordinates
- Invalid goal points
- Invalid plane normal / distance values
- Invalid color values
- Invalid or unusual collision groups
- Invalid or unusual collision masks
- Invalid stadium width / height
- Invalid background width / height

---

## JSON Editor

HaxStudio includes a built-in JSON editor for advanced editing.

### JSON Features

- View current stadium JSON
- Edit JSON manually
- Apply JSON changes to the editor
- Refresh JSON from editor data
- JSON edits update the viewport
- JSON edits are applied before saving
- Invalid JSON prevents unsafe saving

This allows both visual editing and manual low-level `.hbs` editing in the same application.

---

## Workflow Tools

HaxStudio includes common editing tools for a smoother workflow.

- Undo
- Redo
- Copy
- Paste
- Duplicate
- Delete
- Snap to Grid
- Resizable right panel
- Custom dark title bar
- Dark modal settings window
- Tool selection highlight
- Tools menu check marks
- Custom application icon support

---

## Settings / Preferences

HaxStudio includes an in-app Settings window.

### Settings Categories

- Hotkeys
- Preferences
- Themes
- Language
- Check for Updates
- About HaxStudio

### Preferences

Preferences include viewport and editor behavior options such as:

- Show Grid
- Show Vertexes
- Show Planes
- Show Background Stripes
- Show Invisible Objects in Editor
- Vertex Size
- Save validation warning toggle

### Themes

The current UI uses a modern dark theme with blue accent colors.

Future versions may include additional theme presets.

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

## Planned v1.0 Polish

Before the first stable release, the following polish items are planned:

- GitHub update check support
- Loading / splash screen
- Release packaging
- Portable build
- Better installer support
- More UI polish
- More validation rules
- More documentation

---

## Technology

- Language: C#
- UI Framework: WPF
- Platform: Windows
- File Type: HaxBall `.hbs` stadium JSON
- IDE: Visual Studio

---

## Version

Current development target:

```text
v1.0.0
