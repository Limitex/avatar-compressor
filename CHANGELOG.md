# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
