# Native AOT Deployment Guide

AutoMappic is the first .NET object mapper specifically designed for **Native AOT** (Ahead-Of-Time) compilation. In this tutorial, we will walk you through deploying an AOT-ready project with AutoMappic.

## 1. Why Native AOT?

Native AOT compiles your .NET application directly into a single, standalone machine-code binary.

- **Fastest Cold Starts**: Your app starts in milliseconds, making it ideal for Serverless and high-density containers.
- **Lower Memory Footprint**: No JIT-compiler overhead or reflection-based metadata storage.
- **Improved Security**: The binary is smaller and has a reduced attack surface.

## 2. Using AutoMappic in an AOT Project

Standard object mappers fail in Native AOT because they rely on runtime reflection or IL generation. AutoMappic solves this by using **Source Generators**.

### Step 1: Configure your Project

Ensure your `.csproj` file is set up for Native AOT:

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

### Step 2: Define your Mappings

Simply use the `Profile` system as usual. AutoMappic will generate all the mapping code at build time.

```csharp
public class UserProfile : Profile
{
    public UserProfile() => CreateMap<User, UserDto>();
}
```

### Step 3: Use Static Registration

In an AOT environment, full-assembly scanning via reflection is not possible. AutoMappic provides a **Zero-Reflection Registration** system:

```csharp
// Program.cs
builder.Services.AddAutoMappic(); // Uses generated static code
```

## 3. Detecting AOT Incompatibilities

AutoMappic's **Diagnostic Suite** (AM0001-AM0013) ensures that your mappings are safe for AOT before you even attempt to publish.

If the generator cannot satisfy a mapping statically, it will report a build-time error, preventing runtime failures in your production AOT binary.

## 4. Publishing your AOT Binary

To publish your AOT-enabled application, use the standard `dotnet publish` command:

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

Because AutoMappic is reflection-free, the resulting binary will be significantly smaller and faster to start than one using traditional mappers.

---

AutoMappic takes the "guesswork" out of AOT mapping, providing you with a type-safe, statically-verified pipeline for your most demanding services.
