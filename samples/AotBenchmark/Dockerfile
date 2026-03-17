# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /source

# Install dependencies for Native AOT
RUN apk add --no-cache clang build-base zlib-dev

# Copy necessary files
COPY AutoMappic.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY NuGet.Config .
COPY src/ src/
COPY samples/AotBenchmark/ samples/AotBenchmark/
COPY samples/Directory.Build.props samples/

# Publish as Native AOT
WORKDIR /source/samples/AotBenchmark
RUN dotnet publish -c Release -r linux-musl-x64 --self-contained true -o /app

# Final Stage
FROM alpine:latest
WORKDIR /app
COPY --from=build /app/AotBenchmark .

# Run the benchmark
ENTRYPOINT ["./AotBenchmark"]
