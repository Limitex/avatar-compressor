# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Custom Preset Asset** - ScriptableObject-based preset system for reusable custom settings
  - Create presets via Assets menu or "+" button in Inspector
  - Save current settings to linked preset with sync indicator (Modified/Synced)
  - Discard local changes and reload from preset
  - Unlink preset while keeping current settings
  - Auto-apply preset settings when switching to Custom mode with linked asset
  - Presets can be shared across multiple avatars and projects
  - Unit tests for preset operations (ApplyTo, CopyFrom, MatchesSettings)
- **Custom Preset Selection Menu** - Dropdown menu for selecting presets by `MenuPath`
  - Hierarchical menu structure (e.g., "PC/High Detail", "Quest/Optimized")
  - Checkmark indicator for currently selected preset
  - Presets without `MenuPath` are hidden from menu but can be assigned via object field
- **Use Only Mode** - Read-only preset viewing when a preset is selected
  - Settings summary shows current configuration at a glance
  - Description field displayed when set on preset
  - Edit button to switch to edit mode
- **Preset Editing Restrictions** - Protection for non-editable presets
  - `Lock` field to prevent accidental modifications to user presets
  - Built-in preset detection (presets bundled with this package)
  - External package preset detection (presets from other packages)
  - Lock icon indicates restricted presets with tooltip explaining reason
  - Unlink confirmation dialog when attempting to edit restricted presets
- **LRU Cache Utility** - Generic `LruCache<TKey, TValue>` class for editor state management
  - Configurable capacity with automatic eviction of oldest entries
  - Used for button rect caching and edit mode state tracking
  - Comprehensive unit tests for cache operations
- **Custom preset UI tests** - Unit tests for new custom preset components
  - `PresetEditorStateTests` - Edit mode state management
  - `PresetEditTransitionTests` - Mode transition logic
  - `PresetScannerTests` - Preset discovery and menu building
  - `PresetLocationResolverTests` - Preset location and restriction detection
- **Menu ordering for custom presets** - `MenuOrder` field to control display order
  - Lower values appear first in the menu
  - Default value is 1000, allowing built-in presets to appear before user presets
- **Built-in custom presets** - 5 presets to fill gaps between existing compression presets
  - `High Quality+` (MenuOrder: 100) - Between High Quality and Quality
  - `Quality+` (MenuOrder: 200) - Between Quality and Balanced
  - `Balanced+` (MenuOrder: 300) - Between Balanced and Aggressive
  - `Aggressive+` (MenuOrder: 400) - Between Aggressive and Maximum
  - `Maximum+` (MenuOrder: 500) - Slightly better than Maximum

### Changed

- **Settings UI simplified** - Removed section headers from custom settings for cleaner layout
- **Namespace restructured** - Reorganized namespaces for consistency (**Breaking**)
  - Editor: `common` → `editor`
  - Editor: `texture` → `editor.texture`
  - Editor: `texture.editor` → `editor.texture`
  - Editor: `texture.ui` → `editor.texture.ui`
  - Runtime: `texture` → (root namespace)
- **Editor UI refactored** - Modularized Inspector code for maintainability
- **Test structure reorganized** - Grouped tests by module (Analysis, Core, UI, etc.)
- **Custom preset class renamed** - `CustomCompressorPreset` → `CustomTextureCompressorPreset` (**Breaking**)
  - Clearer naming to indicate texture-specific functionality
  - Added `Lock` and `Description` fields for preset management
- **Custom preset UI restructured** - Extracted into dedicated `UI/Custom/` directory
  - `CustomSection` - Main entry point for custom preset UI
  - `PresetEditorState` - Manages edit mode vs use-only mode state
  - `PresetRestriction` - Enum for editing restriction levels
  - `PresetLocationResolver` - Determines preset editability based on location
  - `PresetEditTransition` - Handles mode transitions with confirmation dialogs
  - `PresetScanner` - Scans and caches presets with `MenuPath` for menu building

### Fixed

- **Preset button layout** - Fixed uneven spacing of preset selection buttons in Editor UI
  - Buttons now evenly distributed across available width
  - Consistent spacing between buttons using calculated widths
- **Emission map quality boost** - Fixed calculation that was incorrectly reducing quality instead of boosting it
- **Custom preset edit mode stability** - Fixed issues with edit mode state management
  - NullReferenceException no longer occurs after unlinking a preset
  - Edit mode now auto-exits when preset becomes locked externally
  - Locked presets can no longer be edited even when edit mode flag is set

## [v0.4.0] - 2026-01-16

### Added

- **Search functionality** - Filter frozen textures and preview list by texture name
  - Search box at the top of the frozen textures and preview sections
  - Supports both exact substring matching and fuzzy matching (Bitap algorithm)
  - Toggle button to switch between exact and fuzzy search modes
  - Shows filtered count in section headers (e.g., "Frozen Textures (2/5)")
  - Displays "X hidden by search" indicator when items are filtered out
- **Clickable texture thumbnails** - Click thumbnails to highlight assets in Project window
  - Applies to both frozen textures list and compression preview
  - Cursor changes to link pointer on hover for visual feedback
  - Works even when the row is disabled (e.g., skipped textures)
- **GitHub repository link** - Added repository link section at bottom of Inspector
  - Direct link to project homepage for easy access to documentation and issues
- **Runtime-generated texture skipping** - Textures without asset paths are now automatically skipped
  - Prevents corruption of data textures dynamically created during build
  - These textures may use RGB values for non-visual data (depth, deformation vectors)
  - New `SkipReason.RuntimeGenerated` enum value for UI display
- **Path-based texture exclusion** - Exclude textures by asset path prefix
  - `ExcludedPaths` list in TextureCompressor component for user-defined exclusions
  - Textures with paths starting with listed prefixes are skipped from compression
  - Built-in presets for common packages (e.g., VRCFury Temp)
  - Collapsible "Path Exclusions" section in Editor UI with preset buttons
  - New `SkipReason.ExcludedPath` enum value for UI display
- **Animation-referenced material support** - Materials referenced by animations are now included in compression
  - Uses NDMF's `AnimatorServicesContext` to detect materials in animation clips (MaterialSwap, etc.)
  - MaterialCloner can now clone additional materials beyond renderer-attached ones
  - TextureCollector collects textures from animation-referenced materials
  - Animation curves are automatically updated to reference compressed textures
- **Component-referenced material support** - Materials referenced by components (MA MaterialSetter, etc.) are now detected
  - Scans all components' serialized properties for Material references
  - Respects `EditorOnly` tag (excluded from collection as they are stripped from build)
- **Non-NDMF usage warning** - Added runtime warning when `ICompressor.Compress()` is called outside NDMF build context
  - Warns users that Renderer material references will be changed (though original .mat files are NOT modified)
  - Recommends using the NDMF plugin for non-destructive workflow

### Changed

- **NDMF plugin configuration** - Added `WithRequiredExtensions` for `AnimatorServicesContext`
  - Ensures animation reference updates work correctly by requiring the animator services context
- **Frozen texture identification** - Changed from asset path to GUID-based identification (**Breaking**)
  - `FrozenTextureSettings.TexturePath` renamed to `TextureGuid`
  - `TextureCompressor` API methods now accept GUID instead of asset path:
    - `IsFrozen(string guid)`
    - `GetFrozenSettings(string guid)`
    - `SetFrozenSettings(string guid, FrozenTextureSettings settings)`
    - `UnfreezeTexture(string guid)`
  - Prevents broken references when texture files are moved or renamed
  - Legacy path-based settings are automatically migrated via `[FormerlySerializedAs]`
  - Migration UI in Inspector to convert legacy path entries to GUID
- **TextureProcessor responsibility simplified** - Now handles resizing only, compression moved to TextureFormatSelector
  - Clearer separation of concerns between resizing and compression
  - TextureCompressorService now coordinates both operations
- **TextureCompressorPass refactored** - Extracted pass logic into dedicated class
  - Better separation of concerns between plugin registration and execution
  - Improved error handling with try-catch and warning messages
- **MaterialCloner moved** - Relocated from `Common/Services` to `TextureCompressor/Core/Services`
  - Now part of the TextureCompressor module for better cohesion
  - Enhanced to support cloning additional materials beyond renderer-attached ones
- **Streaming mipmaps warning** - Now only displays once per build instead of per texture
- **Unit tests expanded** - Added comprehensive tests for new features
  - MaterialCollector tests (animation/component material detection, EditorOnly filtering)
  - MaterialReference tests (equality, cloning, source tracking)
  - ComponentUtils tests (IsEditorOnly hierarchy traversal)
  - TextureCollector tests (EditorOnly tagged object skipping, RuntimeGenerated skip, ExcludedPath skip)
  - TextureProcessor tests for resize functionality and settings preservation

### Fixed

- **Normal map alpha channel preservation** - Normal maps with alpha now use BC7 instead of BC5
  - BC5 format only stores 2 channels (RG), losing alpha data
  - Ensures alpha information is preserved for special normal map workflows
- **Preview format consistency** - Preview now correctly shows preserved format for already-compressed textures
  - Matches the actual compression behavior where original compressed formats are maintained
  - Prevents misleading format predictions in the preview UI
- **DXT/BC texture dimension compatibility** - `EnsureMultipleOf4` now rounds up instead of down
  - Ensures textures meet the 4x4 block size requirement for DXT/BC compression formats
  - Prevents potential texture corruption from undersized dimensions
- **Frozen texture skip handling** - Skipped frozen textures now display correctly in preview
- **Modular Avatar compatibility** - Added `AfterPlugin("nadena.dev.modular-avatar")` to ensure proper execution order
  - Fixes potential issues with materials added/modified by Modular Avatar not being processed correctly
  - Ensures animation-referenced materials from MA are properly detected and compressed
- **Preview memory calculation accuracy** - Fixed memory estimation to account for mipmap levels ([@KosmicAnomaly](https://github.com/KosmicAnomaly))
  - Previously used `Profiler.GetRuntimeMemorySizeLong` which returns runtime overhead, not VRAM usage
  - Now calculates compressed memory based on format bits-per-pixel and all mipmap levels
  - Each mipmap level adds 1/4 of the previous level's memory (geometric series)
  - Provides more accurate before/after memory comparison in the preview UI

## [v0.3.4] - 2026-01-06

### Fixed

- **Mipmap streaming** - Fixed NDMF warning about mipmap streaming not being enabled on generated textures
  - Newly compressed textures now have `m_StreamingMipmaps` enabled via SerializedObject
  - Follows the same approach as TexTransTool for proper streaming mipmap support
  - Added comprehensive unit tests for mipmap streaming behavior

## [v0.3.3] - 2026-01-06

### Fixed

- **Mipmap preservation** - Fixed mipmap generation being lost during texture processing ([@dtupper](https://github.com/dtupper))
  - `ResizeTo` and `Copy` methods now preserve source texture's mipmap setting
  - Improves performance, visual quality, and VRAM optimization for processed textures
  - Added comprehensive unit tests for mipmap preservation behavior

## [v0.3.2] - 2026-01-06

### Added

- **Non-destructive tests** - Comprehensive test suite verifying compression process doesn't modify original assets
  - Material non-destructive tests (texture reference, shader, color, name, render queue)
  - Texture non-destructive tests (pixels, dimensions, name, format)
  - Multiple and shared texture tests across materials
  - Hierarchy tests including inactive GameObjects
  - Post-compression state verification tests
  - Frozen texture settings compatibility tests
  - Mixed renderer type tests (MeshRenderer, SkinnedMeshRenderer)
  - Preset variation tests for all compression presets
  - Edge case tests (null materials, empty arrays, size boundary conditions)

### Fixed

- **TexTransTool compatibility** - Fixed conflicts with TexTransTool's AtlasTexture feature
  - Added `ObjectRegistry.RegisterReplacedObject` for materials and textures to enable proper reference tracking across NDMF plugins
  - Plugin now runs before TexTransTool in the build pipeline

## [v0.3.1] 2026-01-05

### Fixed

- **CHANGELOG** - Fixed version header not updated for v0.3.0 release

## [v0.3.0] 2026-01-05

### Added

- **Frozen Textures** - Manual override for individual texture compression settings
  - Freeze specific textures to control their compression independently
  - Configurable divisor (1, 2, 4, 8, 16) per frozen texture
  - Format override (Auto, DXT1, DXT5, BC5, BC7, ASTC_4x4, ASTC_6x6, ASTC_8x8)
  - Skip compression option to exclude textures entirely
  - Validation for divisor values with automatic correction to nearest valid value
  - Warning display for missing texture assets in frozen list
  - Freeze/Unfreeze buttons in preview and dedicated Frozen Textures section
- **Unit tests** for FrozenTextureSettings, TextureCollector, and TextureCompressor config management

### Changed

- **Runtime directory restructured** - Organized into `Components/` and `Models/` subdirectories

## [v0.2.0] 2026-01-04

### Added

- **Component placement warning** - Displays warning when TextureCompressor is not on avatar root
  - Editor: HelpBox warning in Inspector
  - Build: Warning logged to Unity console (does not fail build)
- **Platform-specific compression formats** - Automatic format selection based on build target
  - Desktop (PC): DXT1, DXT5, BC5 (normal maps), BC7 (high complexity)
  - Mobile (Quest/Android): ASTC 4x4, 6x6, 8x8 based on complexity and alpha
- **High-quality format option** for high complexity textures (BC7/ASTC_4x4)
- **Memory estimation display** in compression preview showing estimated VRAM usage
- **Predicted compression format display** in texture list preview
- **Unit tests** for TextureProcessor and TextureFormatSelector

### Changed

- **Renamed** `TextureResizer` to `TextureProcessor` for better clarity
- **Renamed** `HighQualityComplexityThreshold` to `HighComplexityThreshold` for consistency
- **Refactored** texture format selection into dedicated `TextureFormatSelector` class

### Fixed

- **Texture settings preservation** during resizing - wrapMode, filterMode, and anisoLevel are now copied from source texture
- **Build phase** changed from Transforming to Optimizing for proper NDMF pipeline integration
- **Mobile format selection** now properly incorporates alpha channel support
- **ASTC format** alpha support clarified in mobile format selection
- **Unnecessary compression** now skipped for already formatted textures
- **Pixel reading failures** now logged for better debugging
- **ExecuteAlways attribute removed** from TextureCompressor to prevent unintended execution in edit mode
- **Auto-referencing disabled** in asmdef files to avoid unnecessary dependencies

## [0.1.0] - 2026-01-03

### Added

- Initial release of Avatar Compressor (LAC - Limitex Avatar Compressor)
- **Texture Compressor** with complexity-based compression
  - Multiple analysis strategies: Fast, HighAccuracy, Perceptual, and Combined
  - 5 built-in presets: HighQuality, Quality, Balanced, Aggressive, Maximum
  - Custom configuration mode for fine-tuned control
  - Texture type awareness with specialized handling for normal maps and emission maps
  - Shared texture optimization (textures used by multiple materials are processed once)
- Editor UI with real-time compression preview
- NDMF integration for non-destructive avatar builds
- Runs before Avatar Optimizer in the build pipeline for optimal results
