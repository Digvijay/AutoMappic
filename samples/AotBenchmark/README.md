# AutoMappic AOT Benchmark

This sample demonstrates **AutoMappic's total compatibility with Native AOT**.

## Why this matters
Traditional mappers (like AutoMapper) use runtime reflection and IL generation. Native AOT requires all code to be available at compile time. Since AutoMappic is a Source Generator, it produces standard C# code that the AOT compiler can optimize perfectly.

## Building the Image

### Method 1: Dockerfile (Recommended for cross-platform)
This is the most reliable way to build a Linux-based AOT image from a Mac or Windows machine, as it uses a Linux SDK container to perform the AOT compilation.

Run from the repository root:
```bash
docker build -t automappic-aot -f samples/AotBenchmark/Dockerfile .
```

### Method 2: .NET SDK Container Support (OCI)
If you are already on a Linux machine (or have the cross-compilation toolchain installed), you can build the image directly using the .NET SDK without a Dockerfile:

```bash
dotnet publish -c Release /t:PublishContainer
```

## Running
```bash
docker run --rm automappic-aot
```
