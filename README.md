# Avatar Compressor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub release](https://img.shields.io/github/v/release/limitex/avatar-compressor)](https://github.com/limitex/avatar-compressor/releases/latest)
[![GitHub Downloads (latest)](https://img.shields.io/github/downloads/limitex/avatar-compressor/latest/total?label=downloads%40latest)](https://github.com/limitex/avatar-compressor/releases/latest)
[![GitHub Downloads (total)](https://img.shields.io/github/downloads/limitex/avatar-compressor/total?label=downloads%40total)](https://github.com/limitex/avatar-compressor/releases)
[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/limitex/avatar-compressor/gameci.yml?label=tests)](https://github.com/limitex/avatar-compressor/actions)

A non-destructive avatar optimization toolkit for VRChat. This VPM package provides various tools to reduce avatar size.

## Features

### Texture Compressor

Intelligently analyzes and compresses avatar textures based on complexity analysis.

- **Intelligent complexity analysis** - Textures are analyzed to determine optimal compression levels
- **Multiple analysis strategies** - Fast, HighAccuracy, Perceptual, and Combined modes
- **Preset configurations** - Quick setup with 5 built-in presets
- **Texture type awareness** - Specialized handling for normal maps, emission maps, and more
- **Shared texture optimization** - Shared textures are processed once and reused

## Requirements

- Unity 2022.3.22f1 (VRChat specified version)
- VRChat SDK Avatars 3.10.0 or later
- NDMF 1.10.0 or later

## Installation

### Via ALCOM (Recommended)

1. Open [ALCOM](https://vrc-get.anatawa12.com/alcom/)
2. Add the repository: `https://vpm.limitex.dev/`
3. Add **Avatar Compressor** to your project

### Via VRChat Creator Companion

1. Open VRChat Creator Companion
2. Add the repository: `https://vpm.limitex.dev/`
3. Add **Avatar Compressor** to your project

### Manual Installation

Download the latest release from [GitHub Releases](https://github.com/limitex/avatar-compressor/releases) and import the `.zip` file into your Unity project.

## Usage

Add optimization components to your avatar's root GameObject, configure settings, and build your avatar. All optimizations are applied automatically at build time via NDMF.

## Recommended

For best results, we recommend using this tool together with [Avatar Optimizer (AAO)](https://vpm.anatawa12.com/avatar-optimizer/). Avatar Optimizer provides additional optimization features such as mesh merging, bone reduction, and more. LAC runs before Avatar Optimizer in the build pipeline, ensuring optimal texture compression before other optimizations are applied.

## License

[MIT License](LICENSE)

## Author

[Limitex](https://github.com/limitex)
