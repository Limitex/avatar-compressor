# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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
