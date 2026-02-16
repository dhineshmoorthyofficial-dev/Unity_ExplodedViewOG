# Changelog

All notable changes to this project will be documented in this file.

## [1.2.0] - 2026-02-16
### Added
- **Motion Quality Controls**: Per-part animation curves and delay settings for fine-tuned motion control.
- **Global Motion Curve**: Apply a master easing curve to all parts in a manager for consistent animation feel.
- **Hierarchical Debug Synchronization**: "Apply Globally" toggle propagates debug and annotation settings down the entire hierarchy.
- **Local Debug Overlays**: Individual sub-managers can now toggle Distance Heatmap and Path Length independently.
- **Consolidated Visual UI**: Grouped all visualization settings into a single "Visual Fidelity & Debugging" block for cleaner inspector layout.
- **Persistent SerializedObject Cache**: Fixed curve selection issues in nested sub-manager UI by implementing editor-side caching.

### Changed
- **Debug UI Organization**: Moved debug lines, heatmap, and path length controls into a unified section with global override support.
- **Annotation Propagation**: Annotation settings now respect the hierarchical override system when "Apply Globally" is enabled.

### Removed
- **Collision Warning Feature**: Removed collision detection overlay as it was not providing expected results.

### Fixed
- **Curve Selection Bug**: Animation curves in sub-manager lists are now fully editable without losing focus.
- **Duplicate Field Definitions**: Cleaned up duplicate `drawDebugLines` and `debugLineColor` declarations.
- **Heatmap Rendering**: Fixed internal logic to correctly use propagated settings instead of local manager settings.

## [1.1.0] - 2026-01-30
### Added
- **Curved Explosion Mode**: New mode allowing parts to move along custom Bézier paths.
- **Advanced Spline Control**: Per-part manual control points for infinite path customization.
- **Control Point Hull Visualization**: Dotted line visualization in the Scene view for intuitive curve editing.
- **Per-Manager Debug Toggles**: Ability to enable/disable debug lines for individual sub-managers to reduce visual clutter.
- **Auto-Create Targets Toggle**: Added a setting to prevent unexpected target generation during scene loading.

### Fixed
- **Setup Initialization**: Ensured control points are correctly initialized during the "Reset & Setup" process.
- **Recursive Debug Drawing**: Fixed a compilation error in `DrawDebugLinesRecursive` and improved performance.
- **Editor UI**: Contextual hiding of control points when in linear `Target` mode.

## [1.0.0] - 2026-01-30
### Added
- **Spherical Explosion Mode**: Uniformly explodes parts outwards from a central point.
- **Target Explosion Mode**: Allows manual placement of parts for precise exploded views using target objects.
- **Recursive Management**: Supports complex nested hierarchies with independent explosion controls.
- **Auto-Grouping**: Automatically detects and manages child groups for easier setup.
- **Editor Tooling**: Custom inspector with "Danger Zone" for cleanup and deep removal of scripts/targets.
- **Debug Visualization**: Option to draw lines showing part movement paths in the Scene view.
