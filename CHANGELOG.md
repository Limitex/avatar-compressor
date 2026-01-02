# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-01-02

### Added

- Initial release of Avatar Compressor (LAC - Limitex Avatar Compressor)
- **Texture Compressor** with intelligent complexity-based compression
  - Multiple analysis strategies: Fast, HighAccuracy, Perceptual, and Combined
  - 5 built-in presets: HighQuality, Quality, Balanced, Aggressive, Maximum
  - Custom configuration mode for fine-tuned control
  - Texture type awareness with specialized handling for normal maps and emission maps
  - Shared texture optimization (textures used by multiple materials are processed once)
- Editor UI with real-time compression preview
- NDMF integration for non-destructive avatar builds
- Runs before Avatar Optimizer in the build pipeline for optimal results
