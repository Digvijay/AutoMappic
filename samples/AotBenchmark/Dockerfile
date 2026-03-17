# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /source

# Install dependencies for Native AOT
RUN apk add --no-cache clang build-base zlib-dev

# Copy necessary files
COPY AutoMappic.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/ src/
COPY samples/samples.sln samples/
COPY samples/AotBenchmark/ samples/AotBenchmark/

# Publish as Native AOT
WORKDIR /source/samples/AotBenchmark
RUN dotnet publish -c Release -r linux-musl-x64 --self-contained true /p:PublishAot=true -o /app

# Final Stage
FROM alpine:latest
WORKDIR /app
COPY --from=build /app/AotBenchmark .

# Run the benchmark
ENTRYPOINT ["./AotBenchmark"]
