# AutoMappic.Generator.CodeFixes

The **AutoMappic.Generator.CodeFixes** project provides the interactive "Lightbulb" IDE support for the AutoMappic v0.5.0 stable release and beyond.

## Features

- **Smart-Match Suggestions (AM0015)**: When an unmapped property is detected with a similar name in the source, this provider suggests the correct mapping and automatically applies the `[MapProperty(...)]` attribute to your `Profile` class.
- **Diagnostics Resolution**: Provides automated fixes for common configuration issues that would otherwise break the build.
- **Convention Refactoring**: Helps you discover and apply naming conventions correctly as you type.

Integrates seamlessly into **Visual Studio 2022**, **VS Code** (via C# Dev Kit), and **JetBrains Rider**.
