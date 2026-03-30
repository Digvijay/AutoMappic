# AutoMappic CLI

The **AutoMappic CLI** is a standalone diagnostic and visualization tool for .NET projects using the AutoMappic source-generated mapper. It allows developers to validate their mapping configurations and visualize complex object relationships without running the application or relying solely on IDE feedback.

## Features

- **Project Validation (`validate`)**: Performs full build-time analysis of `Profile` classes and `CreateMap` declarations. It reports all **AM0001-AM0017** diagnostics, making it ideal for CI/CD gates.
- **Mapping Visualization (`visualize`)**: Generates [Mermaid.js](https://mermaid.js.org/) graph definitions of your mapping architecture, showing both type-level links and detailed property-level transitions (e.g., flattened paths).

## Installation

Install the AutoMappic CLI as a global .NET tool:

```bash
dotnet tool install -g AutoMappic.Cli
```

## Usage

### 1. Validating a Project
Ensure all your mapping configurations are structurally sound:

```bash
# Standard output
automappic validate MyProject.csproj

# Machine-readable JSON output for CI/CD
automappic validate MyProject.csproj --format json
```

### 2. Visualizing Mapping Graphs
Generate a Mermaid graph of your mapping surface:

```bash
automappic visualize MyProject.csproj --format mermaid
```

You can then paste the output into any Mermaid-compatible viewer (e.g., GitHub, VS Code Mermaid preview, or [Mermaid Live Editor](https://mermaid.live/)).

## Industrial-Grade Readiness
The CLI follows the same "Zero-Reflection" philosophy as the core library, utilizing the **Roslyn Workspace API** to analyze code purely via its semantic model. This ensures that validation is fast, accurate, and 100% consistent with the build-time source generator output.
