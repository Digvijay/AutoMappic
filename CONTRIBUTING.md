# Contributing to AutoMappic

Thank you for your interest in contributing to AutoMappic! We welcome contributions from the community to help make this the standard for AOT-first mapping in .NET.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [How to Contribute](#how-to-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Pull Requests](#pull-requests)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)

---

## Code of Conduct
This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## How to Contribute

### Reporting Bugs
Bugs are tracked as GitHub issues. When filing an issue, please include:
1.  **Version**: The version of AutoMappic you are using.
2.  **Reproduction**: A code snippet or minimal reproduction repository.
3.  **Expected vs Actual**: What you expected to happen vs what actually happened.

### Suggesting Enhancements
We love new ideas! If you want to suggest a new convention (e.g., `Reverse Flattening`), please open a **Feature Request** issue first to discuss the API design before writing code.

### Pull Requests
1.  **Fork** the repository.
2. **Branch** off `main` (e.g., `feature/add-new-convention` or `fix/flattening-bug`).
3. **Commit** your changes. Please use clear, imperative commit messages.
4. **Test** your changes. Run `dotnet run --project tests/AutoMappic.Tests` to ensure all standard and generator tests pass.
5.  **Push** to your fork and submit a Pull Request.

---

## Development Workflow

AutoMappic is a **Source Generator** project heavily leveraging Roslyn Interceptors.

1.  **Open the Solution**: `AutoMappic.sln`.
2.  **Build**: `dotnet build`.
3. **Test**: The `tests/AutoMappic.Tests` project verifies the generated code.
    * *Note:* Visual Studio/Rider sometimes requires a restart to pick up changes to the source generator logic itself.
    * We recommend running `dotnet run --project tests/AutoMappic.Tests` from the CLI for the most reliable results during generator development.

## Coding Standards

* **Style**: We follow standard C# coding conventions (PascalCase for public members, `_camelCase` for private fields).
* **Performance**: This is a high-performance AOT compiler plugin.
    * Avoid LINQ in hot paths within the `IIncrementalGenerator` provider.
    * Ensure symbols map to an `IEquatable` array before rendering.
* **Tests**: All new features must be accompanied by unit tests in `AutoMappic.Tests`.

## License
By contributing, you agree that your contributions will be licensed under its MIT License.
