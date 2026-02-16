# Exploded View Tool - User Manual

## Introduction

Welcome to the **Exploded View Tool**! This Unity tool allows you to easily create impressive exploded view animations and visualizations for your 3D models. Whether you want to show the inner workings of a machine, create a cool breakdown effect, or produce a technical assembly animation, this tool makes it simple and powerful.

### What can this package do?
*   **Spherical Explosion**: Move all parts straight out from a center point (like a "big bang").
*   **Targeted Explosion**: Manually tell each part exactly where to go for precise control.
*   **Curved Explosion**: Move parts along custom Bézier paths for organic, cinematic reveals.
*   **Motion Quality Controls**: Fine-tune animations with per-part curves and delays, plus a global motion curve.
*   **Orchestration**: Sequence complex nested objects (e.g., Unpack the Engine -> Then explode the Pistions).
*   **Sequential Animation**: Automate "Move then Open" effects with a single click.
*   **Nested Groups**: Handle complex objects with "parts inside parts" easily.
*   **Debug Overlays**: Visualize movement with heatmaps, path lengths, and debug lines (global or per-group).
*   **Non-Destructive**: Your original object layout is always safe. You can reset at any time.

---

## Installation

1.  Import the package into your Unity project.
2.  That's it! The tool is ready to use.

---

## Quick Start Guide

1.  **Add the Component**: Select the parent GameObject of your model in the Scene hierarchy.
2.  **Add Script**: Click `Add Component` in the Inspector and search for `Exploded View`.
3.  **Setup**: Click the `Reset & Setup Explosion` button in the script component.
    *   *This will automatically find all the parts of your model.*
4.  **Explode!**: Drag the **Explosion Factor** slider from `0` to `1` to see your model explode.

---

## Core Modes

### 1. Spherical Mode (Default)
Best for: Simple objects, debris, or quick visualizations.
*   **How it works**: Parts move away from a central point.
*   **Controls**:
    *   `Sensitivity`: Controls how far parts fly out.
    *   `Center`: (Optional) Drag a Transform here to change the center of the explosion.

### 2. Target Mode
Best for: Technical diagrams, assembly instructions, or precise animations.
*   **How it works**: You define a specific "End Position" for each part.
*   **Usage**:
    1.  Switch **Explosion Mode** to `Target`.
    2.  Click **Create Targets** (if they don't appear automatically).
    3.  You will see new "Ghost" objects appear (Target_ObjectName).
    4.  **Move the Ghost objects** to where you want the parts to end up.
    5.  Now, the **Explosion Factor** slider will animate parts smoothly in a straight line to the target.

### 3. Curved Mode
Best for: Organic movements, cinematic reveals, or complex fly-throughs.
*   **How it works**: Parts move along a multi-point Bézier spline.
*   **Usage**:
    1.  Switch **Explosion Mode** to `Curved`.
    2.  Expand the **Control Points** list in the Inspector (under the Structure list).
    3.  Move any Control Point transform in the scene to reshape the curve.
    4.  The **Explosion Factor** will now animate the part along this custom path.

---

## Mode Decision Table

| Mode | Best For... | Movement Complexity | Performance |
| :--- | :--- | :--- | :--- |
| **Spherical** | Quick previews, debris, uniform expansion. | Low (Linear) | Ultra High |
| **Target** | Technical diagrams, cabling, specific landing spots. | Medium (Linear) | High |
| **Curved** | Organic reveals, cinematics, avoiding obstacles. | High (Interpolated) | Medium |
| **Orchestrated** | Multi-stage assemblies (e.g., Engines, Gearboxes). | Very High (Sequenced) | High |
| **Sequential** | "Unpack then Deploy" logic (Case opens, then parts move). | High (Automated) | High |

---

## Orchestration (New!)

For complex mechanical objects, simply moving everything at once isn't enough. You need specific things to happen in a specific order. The Orchestration system allows you to control this sequence.

### 1. Independent Control
By default, an object has two sliders:
*   **Explosion Factor**: Controls the *local* parts of this object (the "Leaves").
*   **Orchestration Factor**: Controls the sequence of *sub-managers* (the "Branches").
    *   *Example*: Use Explosion Factor to open the case, then use Orchestration Factor to push out the internal components one by one.

### 2. Linked Control (Master Slider)
If you want one slider to rule them all:
1.  Check **Link to Main Explosion**.
2.  Now, the **Explosion Factor** becomes the Master Control.
3.  It will drive both the local parts and the sub-manager sequence simultaneously.

### 3. Sequential Mode ("Move then Open")
Often, you want a sub-assembly to move into position *before* it starts exploding itself.
1.  Enable **Sequential Mode** (available when Linked on the Main object, or per-object in the Sub-Manager list).
2.  **How it works**:
    *   **0% - 50%**: The object moves to its target position.
    *   **50% - 100%**: The object starts its own internal explosion sequence.
3.  This creates a beautiful "Unpack -> Deploy" animation automatically.

### 4. Reordering
*   Use the **Reorderable List** in the Inspector to drag-and-drop sub-managers or parts.
*   Items at the **Top** explode first (at factor 0.0).
*   Items at the **Bottom** explode last (at factor 1.0).

---

## Motion Quality Controls

Fine-tune the animation feel of your explosions with per-part and global motion controls.

### Per-Part Controls
Each part has its own motion settings accessible in the Parts list:
*   **Motion Curve**: Custom animation curve for easing (e.g., ease-in, bounce, overshoot).
*   **Delay**: Start this part's movement later in the sequence (0.0 = immediate, 0.5 = halfway through).

### Global Motion Curve
Apply a master easing curve to all parts in a manager:
1.  Expand the **Global Motion** section in the Inspector.
2.  Edit the **Global Motion Curve** to apply consistent easing to all parts.
3.  This curve is applied *after* the per-part curve, allowing for layered control.

---

## Debug Overlays

Visualize and debug your explosion setup with built-in Scene view overlays.

### Available Overlays
*   **Draw Path Lines**: Shows the movement path for each part (yellow lines by default).
*   **Distance Heatmap**: Color-codes parts based on how far they move (blue = short, red = long).
*   **Path Lengths**: Displays numeric distance labels for each part.

### Global vs. Local Control
*   **Apply Globally**: Enable this on the root manager to push debug settings to all sub-managers.
*   **Local Control**: When global mode is off, each sub-manager can toggle its own debug overlays independently.

---

## Advanced Features

*   **Sub-Managers (Nested Groups)**: If you have a car engine, you can have one ExplodedView on the "Engine" and another on the "Piston". Exploding the engine will move the whole piston, but you can *also* explode the piston itself separately!
*   **Deep Recursion**: Driving the root Orchestrator will drive the entire tree recursively. You can animate a massively complex assembly with a single slider.
*   **Auto-Grouping**: Enable `Auto Group Children` to automatically detect sub-assemblies and add scripts to them.

---

## FAQ

**Q: My parts aren't moving!**
A: Make sure you clicked "Reset & Setup Explosion". The tool needs to scan your object first.

**Q: Can I undo the explosion?**
A: Yes! Just set **Explosion Factor** back to `0`. Your object returns to its original state.

**Q: How do I remove the tool?**
A: **Do not just delete the script.** Scroll down to the "Danger Zone" in the inspector and click **Deep Remove**. This ensures all helper scripts and temporary target objects are cleaned up properly.

**Q: Why is my slider greyed out?**
A: If a slider is disabled, it means that object is being **controlled by a parent Orchestrator**. To manually control it, select the parent and disable its Orchestration, or uncheck "Link to Main Explosion" if applicable.
