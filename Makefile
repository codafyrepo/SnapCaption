# Makefile for SnapCaption
# .NET WPF Application Build System

# Variables
PROJECT = SnapCaption.csproj
SOLUTION = SnapCaption.sln
APP_NAME = SnapCaption
VERSION = v1.1.0
CONFIGURATION = Release
CONFIGURATION_DEBUG = Debug
RUNTIME_X64 = win-x64
RUNTIME_ARM64 = win-arm64
OUTPUT_DIR = bin
OBJ_DIR = obj
PUBLISH_DIR = publish

# .NET CLI
DOTNET = dotnet

# Phony targets
.PHONY: all build build-debug clean restore run publish publish-x64 publish-arm64 publish-all release release-single dist deploy deploy-x64 deploy-arm64 help

# Default target
all: build

# Build project in Release mode
build:
	@echo "Building SnapCaption - Release mode..."
	$(DOTNET) build $(PROJECT) -c $(CONFIGURATION)
	@echo "Build completed successfully!"

# Build project in Debug mode
build-debug:
	@echo "Building SnapCaption - Debug mode..."
	$(DOTNET) build $(PROJECT) -c $(CONFIGURATION_DEBUG)
	@echo "Debug build completed successfully!"

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	@rm -rf $(OUTPUT_DIR)
	@rm -rf $(OBJ_DIR)
	@rm -rf $(PUBLISH_DIR)
	@echo "Clean completed!"

# Restore NuGet packages
restore:
	@echo "Restoring NuGet packages..."
	$(DOTNET) restore $(PROJECT)
	@echo "Restore completed!"

# Run the application (Debug mode)
run:
	@echo "Running SnapCaption..."
	$(DOTNET) run --project $(PROJECT)

# Publish self-contained executable for x64
publish-x64:
	@echo "Publishing self-contained x64 executable..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_X64) --self-contained true -o $(PUBLISH_DIR)/$(RUNTIME_X64)
	@echo "x64 publish completed!"

# Publish self-contained executable for ARM64
publish-arm64:
	@echo "Publishing self-contained ARM64 executable..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_ARM64) --self-contained true -o $(PUBLISH_DIR)/$(RUNTIME_ARM64)
	@echo "ARM64 publish completed!"

# Publish framework-dependent executable for x64
publish-x64-fd:
	@echo "Publishing framework-dependent x64 executable..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_X64) --self-contained false -o $(PUBLISH_DIR)/$(RUNTIME_X64)-fd
	@echo "x64 framework-dependent publish completed!"

# Publish framework-dependent executable for ARM64
publish-arm64-fd:
	@echo "Publishing framework-dependent ARM64 executable..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_ARM64) --self-contained false -o $(PUBLISH_DIR)/$(RUNTIME_ARM64)-fd
	@echo "ARM64 framework-dependent publish completed!"

# Publish all variants
publish-all: publish-x64 publish-arm64 publish-x64-fd publish-arm64-fd
	@echo "All publish targets completed!"

# Shorthand for publish-all
publish: publish-all

# Release build - Self-contained x64 executable with everything included
release:
	@echo "Creating self-contained release (x64)..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_X64) --self-contained true \
		-p:PublishTrimmed=false \
		-p:DebugType=None \
		-p:DebugSymbols=false \
		-o $(PUBLISH_DIR)/release
	@echo ""
	@echo "Release build completed!"
	@echo "Executable location: $(PUBLISH_DIR)/release/SnapCaption.exe"

# Release build - Single file self-contained executable
release-single:
	@echo "Creating single-file self-contained release (x64)..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_X64) --self-contained true \
		-p:PublishSingleFile=true \
		-p:PublishTrimmed=false \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:DebugType=None \
		-p:DebugSymbols=false \
		-o $(PUBLISH_DIR)/release-single
	@echo ""
	@echo "Single-file release completed!"
	@echo "Executable location: $(PUBLISH_DIR)/release-single/SnapCaption.exe"

# Distribution package - Ready to distribute
dist: release-single
	@echo "Creating distribution package..."
	@mkdir -p $(PUBLISH_DIR)/dist
	@cp $(PUBLISH_DIR)/release-single/SnapCaption.exe $(PUBLISH_DIR)/dist/
	@cp setting.json $(PUBLISH_DIR)/dist/
	@cp README.md $(PUBLISH_DIR)/dist/
	@cp LICENSE $(PUBLISH_DIR)/dist/
	@echo ""
	@echo "Distribution package created!"
	@echo "Location: $(PUBLISH_DIR)/dist/"
	@echo ""
	@echo "Contents:"
	@echo "  - SnapCaption.exe (self-contained)"
	@echo "  - setting.json (default configuration)"
	@echo "  - README.md"
	@echo "  - LICENSE"
	@echo ""
	@echo "Ready to distribute! Users can run SnapCaption.exe directly."

# Deploy single-file executables with versioned filenames
deploy: deploy-x64 deploy-arm64
	@echo "Deploy targets completed."

deploy-x64:
	@echo "Building deployable single-file for $(RUNTIME_X64)..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_X64) --self-contained true \
		-p:PublishSingleFile=true \
		-p:PublishTrimmed=false \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:DebugType=None \
		-p:DebugSymbols=false \
		-o $(PUBLISH_DIR)/deploy/$(RUNTIME_X64)
	@mv $(PUBLISH_DIR)/deploy/$(RUNTIME_X64)/SnapCaption.exe \
		$(PUBLISH_DIR)/deploy/$(RUNTIME_X64)/$(APP_NAME)-$(VERSION)-$(RUNTIME_X64).exe
	@echo "Deployed: $(PUBLISH_DIR)/deploy/$(RUNTIME_X64)/$(APP_NAME)-$(VERSION)-$(RUNTIME_X64).exe"

deploy-arm64:
	@echo "Building deployable single-file for $(RUNTIME_ARM64)..."
	$(DOTNET) publish $(PROJECT) -c $(CONFIGURATION) -r $(RUNTIME_ARM64) --self-contained true \
		-p:PublishSingleFile=true \
		-p:PublishTrimmed=false \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:DebugType=None \
		-p:DebugSymbols=false \
		-o $(PUBLISH_DIR)/deploy/$(RUNTIME_ARM64)
	@mv $(PUBLISH_DIR)/deploy/$(RUNTIME_ARM64)/SnapCaption.exe \
		$(PUBLISH_DIR)/deploy/$(RUNTIME_ARM64)/$(APP_NAME)-$(VERSION)-$(RUNTIME_ARM64).exe
	@echo "Deployed: $(PUBLISH_DIR)/deploy/$(RUNTIME_ARM64)/$(APP_NAME)-$(VERSION)-$(RUNTIME_ARM64).exe"

# Help target
help:
	@echo "SnapCaption Build System"
	@echo ""
	@echo "Quick Start:"
	@echo "  make dist               - Create distribution package (RECOMMENDED)"
	@echo "  make release-single     - Create single-file release executable"
	@echo "  make release            - Create self-contained release"
	@echo ""
	@echo "Build Targets:"
	@echo "  make build              - Build project in Release mode (requires .NET runtime)"
	@echo "  make build-debug        - Build project in Debug mode"
	@echo "  make clean              - Remove all build artifacts"
	@echo "  make restore            - Restore NuGet packages"
	@echo "  make run                - Run the application"
	@echo ""
	@echo "Advanced Publishing:"
	@echo "  make publish            - Publish all variants (x64, ARM64, framework-dependent)"
	@echo "  make publish-x64        - Publish self-contained x64 executable"
	@echo "  make publish-arm64      - Publish self-contained ARM64 executable"
	@echo "  make publish-x64-fd     - Publish framework-dependent x64 executable"
	@echo "  make publish-arm64-fd   - Publish framework-dependent ARM64 executable"
	@echo ""
	@echo "  make help               - Show this help message"
