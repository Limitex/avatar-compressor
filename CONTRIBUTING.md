# Contributing

Thank you for your interest in contributing to Avatar Compressor!
(Issues and Pull Requests in Japanese are also welcome!)

## Requirements

- **Unity 2022.3.22f1** (VRChat specified version)

## Architecture

### Project Structure

```text
Runtime/
├── Components/          # User-facing MonoBehaviours
└── Models/              # Runtime data models

Editor/
├── <Feature>/
│   ├── Plugin/          # NDMF integration
│   ├── Core/            # Processing logic
│   ├── Analysis/        # Analysis algorithms
│   └── UI/              # Inspector components
└── Common/              # Shared utilities
```

### Namespaces

| Namespace                                           | Usage                       |
| --------------------------------------------------- | --------------------------- |
| `dev.limitex.avatar.compressor`                     | Runtime components & models |
| `dev.limitex.avatar.compressor.editor`              | Common editor base classes  |
| `dev.limitex.avatar.compressor.editor.ui`           | Common editor UI utilities  |
| `dev.limitex.avatar.compressor.editor.<feature>`    | Feature editor classes      |
| `dev.limitex.avatar.compressor.editor.<feature>.ui` | Feature UI components       |
| `dev.limitex.avatar.compressor.tests`               | Test classes                |

Example: For the `texture` feature, namespaces are `dev.limitex.avatar.compressor.editor.texture`, `dev.limitex.avatar.compressor.editor.texture.ui`, etc.

## Coding Style

- Use C# naming conventions (PascalCase for public, camelCase for private)
- Add XML documentation for public APIs
- Use interfaces for testability

### Code Formatting

This project uses [CSharpier](https://csharpier.com/) for code formatting. Format your code before committing:

```bash
dotnet csharpier format .
```

CI will automatically check formatting with `dotnet csharpier check .`.

## Key Conventions

- **NDMF tracking:** Always call `ObjectRegistry.RegisterReplacedObject()` when replacing assets
- **NDMF extensions:** Use `WithRequiredExtensions` to declare required extension contexts (e.g., `AnimatorServicesContext`)

## Localization

Editor UI strings live in `Editor/Common/Localization/`. Edit source text in `en-US.po` and translate other locales on Crowdin.

- **UI strings:** Use `AvatarCompressorLocalization` and shared localized UI controls.
- **Keys:** Follow `Scope:kind:name`, appending `:tooltip` for tooltips. Keep keys stable unless their meaning changes.
- **Translations:** Preserve placeholders such as `{0}` and leave untranslated entries empty.
- **New locales:** Add a locale-named `.po` file (e.g., `zh-Hans.po`) and its `.meta`, then update [crowdin.yml](crowdin.yml).

The [translation workflow](.github/workflows/translations.yml) uploads source changes and opens translation PRs. Run it from `main`.

## Running Tests

Window > General > Test Runner > EditMode > Run All

Tests are located in `Tests/Editor/`. All tests must pass in CI (GameCI) before merging.

## Commit Messages

Format: `type: description` — Follows [Conventional Commits](https://www.conventionalcommits.org/).

| Type       | Usage                    |
| ---------- | ------------------------ |
| `feat`     | New feature              |
| `fix`      | Bug fix                  |
| `refactor` | Code refactoring         |
| `test`     | Adding or updating tests |
| `docs`     | Documentation changes    |
| `chore`    | Maintenance tasks        |

Examples:

- `feat: add perceptual complexity strategy`
- `fix: preserve alpha channel in BC7 compression`

## Pull Requests

1. Fork & create a feature branch
2. Make changes
3. **Run `dotnet csharpier format .`** to format code
4. **Add tests** for new functionality
5. **Update `CHANGELOG.md`** under `[Unreleased]` section following [Keep a Changelog](https://keepachangelog.com/) format
6. Submit PR

PRs changing only files under `Editor/Common/Localization/` do not require a CHANGELOG entry.
