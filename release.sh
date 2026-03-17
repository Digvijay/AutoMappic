#!/bin/bash
set -e

# AutoMappic Release Script
# Follows the Sannr pattern: tag-based NuGet publishing.

echo "🚀 Preparing AutoMappic Release..."

# 1. Extract version from Directory.Build.props.
# Compatible with MacOS and Linux sed
VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Directory.Build.props | head -n 1 | tr -d '[:space:]')

if [ -z "$VERSION" ]; then
  echo "❌ Error: Could not find version in Directory.Build.props"
  exit 1
fi

TAG="v$VERSION"
echo "📦 Target Version: $VERSION"
echo "🏷️ Tag: $TAG"

# 2. Check for uncommitted changes
if [[ -n $(git status -s) ]]; then
  echo "⚠️ Warning: You have uncommitted changes. Please commit or stash them before releasing."
  exit 1
fi

# 3. Confirm
read -p "⚠️ Create and push tag $TAG? (y/N): " confirm
if [[ $confirm != "y" ]]; then
  echo "❌ Aborted."
  exit 1
fi

# 4. Create and push tag
echo "🏷️ Creating tag $TAG..."
git tag -a "$TAG" -m "Release $TAG"

echo "📤 Pushing tag to origin..."
git push origin "$TAG"

echo "✅ Done! GitHub Actions will now build and publish the NuGet package."
