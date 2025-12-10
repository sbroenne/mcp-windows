# Feature Specification: Screenshot LLM Optimization

**Feature Branch**: `011-screenshot-llm-optimization`  
**Created**: 2025-12-10  
**Status**: Draft  
**Input**: User description: "Add LLM-optimized screenshot defaults: JPEG format (quality 85), auto-scaling to 1568px width, configurable imageFormat/quality/maxWidth/maxHeight parameters, and file output mode as alternative to base64"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - LLM-Optimized Default Format (Priority: P1)

As an LLM agent frequently analyzing screenshots, I need screenshots returned in JPEG format by default so I can minimize token usage, API costs, and processing latency without any configuration.

**Why this priority**: Screenshots are one of the most frequently used MCP tools. The current PNG default produces files 3-10x larger than necessary for LLM vision analysis. For a 1080p screen, PNG is ~2MB while JPEG at quality 85 is ~300KB. Changing the default delivers immediate value to all users.

**Independent Test**: Request a screenshot with no parameters, verify returned image is JPEG format (not PNG) and file size is under 500KB for a typical 1080p display.

**Acceptance Scenarios**:

1. **Given** the agent requests a capture with no format specified, **When** the capture completes, **Then** the image is returned as JPEG with quality 85
2. **Given** the agent requests a capture with `imageFormat: "png"`, **When** the capture completes, **Then** the image is returned as lossless PNG
3. **Given** the agent specifies an invalid format (e.g., "gif", "webp"), **Then** an error is returned listing valid formats: jpeg, png
5. **Given** the agent requests JPEG with explicit `quality: 70`, **When** the capture completes, **Then** the image is compressed at quality 70
6. **Given** the agent specifies quality outside 1-100 range, **Then** an appropriate error is returned
7. **Given** the agent requests PNG format with a quality parameter, **When** the capture completes, **Then** the quality parameter is ignored (PNG is lossless)

---

### User Story 2 - Auto-Scaling for LLM Vision Models (Priority: P1)

As an LLM agent working with any display resolution, I need screenshots automatically scaled to match vision model native limits so I get optimal file sizes without manual configuration.

**Why this priority**: All major LLM vision models (Claude, GPT-4V, Gemini) internally downsample images to ~1568px or less. Sending 4K screenshots (3840px) wastes 97% of bandwidth since the model discards the extra resolution. Auto-scaling by default ensures optimal performance.

**Independent Test**: On a 4K display (3840×2160), request a screenshot with default parameters, verify returned image width is 1568px (not 3840px) and file size is under 300KB.

**Acceptance Scenarios**:

1. **Given** the agent requests a capture with no scaling parameters on a 4K display, **When** the capture completes, **Then** the image is automatically scaled to 1568px width with proportional height
2. **Given** the agent requests a capture on a 1080p display (1920×1080), **When** the capture completes, **Then** the image is scaled to 1568px width (still applies)
3. **Given** the agent requests a capture on a display smaller than 1568px, **When** the capture completes, **Then** the image is NOT upscaled (original size preserved)
4. **Given** the agent requests a capture with `maxWidth: 0` (disable scaling), **When** the capture completes, **Then** the full resolution image is returned
5. **Given** the agent requests a capture with explicit `maxWidth: 1920`, **When** the capture completes, **Then** the image is scaled to 1920px width
6. **Given** the agent requests a capture with `maxHeight: 1080`, **When** the capture completes, **Then** the image is scaled to fit within 1080px height with proportional width
7. **Given** both `maxWidth: 1920` and `maxHeight: 1080` are specified, **When** the capture completes, **Then** the image fits within both constraints while preserving aspect ratio
8. **Given** scaling is applied, **When** the capture completes, **Then** the response metadata includes both actual dimensions and original dimensions

---

### User Story 3 - File Output Mode (Priority: P2)

As an LLM agent or automation client, I need the option to receive screenshots as file paths instead of inline base64 so I can avoid JSON payload overhead and handle images more efficiently in file-based workflows.

**Why this priority**: While inline base64 is convenient for most use cases, file output eliminates the 33% encoding overhead entirely and enables direct file operations (archiving, external tool integration, large batch processing).

**Independent Test**: Request a screenshot with `outputMode: "file"`, verify response contains a valid file path under 1KB payload, and confirm the file exists with correct image content.

**Acceptance Scenarios**:

1. **Given** the agent requests a capture with `outputMode: "inline"` (or no outputMode), **When** the capture completes, **Then** the image is returned as base64-encoded data in the response
2. **Given** the agent requests a capture with `outputMode: "file"`, **When** the capture completes, **Then** the image is saved to a temp file and the response contains the file path
3. **Given** file output mode is used, **When** the capture completes, **Then** the response includes: file path, dimensions, format, and file size in bytes
4. **Given** file output is requested with custom `outputPath: "C:\\Screenshots\\capture.jpg"`, **When** the capture completes, **Then** the image is saved to the specified path
5. **Given** the specified output path is invalid or not writable, **Then** an appropriate error is returned
6. **Given** file output is used without outputPath, **When** the capture completes, **Then** the file is saved to system temp directory with unique filename pattern `screenshot_{timestamp}_{guid}.{ext}`

---

### User Story 4 - Combined Optimizations (Priority: P2)

As an LLM agent, I need format, scaling, and output mode to work together seamlessly so I can achieve maximum optimization with minimal configuration.

**Why this priority**: The real value comes from combining all optimizations. A 4K PNG (8MB) becomes a 1568px JPEG file path (~200KB file, <1KB response payload) — a 99.99% reduction in transmitted data.

**Independent Test**: On a 4K display, request a screenshot with defaults (no parameters), verify the response is JPEG format, 1568px width, quality 85, and under 300KB.

**Acceptance Scenarios**:

1. **Given** default parameters on a 4K display, **When** the capture completes, **Then** the result is JPEG q85 at 1568px width (all defaults applied)
2. **Given** `outputMode: "file"` with default format/scaling, **When** the capture completes, **Then** the file is JPEG q85 at 1568px width saved to temp
3. **Given** `imageFormat: "png"` with `maxWidth: 1920`, **When** the capture completes, **Then** PNG format is used with 1920px scaling
4. **Given** `maxWidth: 0` (disable scaling) with JPEG format, **When** the capture completes, **Then** full resolution JPEG is returned

---

### Edge Cases

- What happens with very small captures (< 100px)? → No scaling applied, return original size
- What happens if temp directory is full or read-only? → Return error with specific message about disk space/permissions
- What if file already exists at outputPath? → Overwrite (standard behavior for explicit paths)
- How are window captures handled with scaling? → Scale after capture, same as screen captures
- What happens with region captures smaller than maxWidth? → No upscaling, return original region size

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST default to JPEG format (quality 85) for all screenshot captures
- **FR-002**: System MUST support `imageFormat` parameter with values: "jpeg" (default), "png"
- **FR-003**: System MUST support `quality` parameter (1-100) for JPEG format; default 85
- **FR-004**: System MUST ignore `quality` parameter when format is "png" (lossless)
- **FR-005**: System MUST return error for invalid imageFormat values, listing valid options
- **FR-006**: System MUST return error for quality values outside 1-100 range
- **FR-007**: System MUST default to `maxWidth: 1568` for all captures (LLM vision model native limit)
- **FR-008**: System MUST support `maxWidth` parameter to override default scaling
- **FR-009**: System MUST support `maxHeight` parameter for height-constrained scaling
- **FR-010**: System MUST preserve aspect ratio when scaling
- **FR-011**: System MUST NOT upscale images when source is smaller than max dimensions
- **FR-012**: System MUST allow disabling auto-scaling via `maxWidth: 0`
- **FR-013**: System MUST use high-quality interpolation (bicubic or better) when scaling
- **FR-014**: System MUST include original dimensions in response metadata when scaling is applied
- **FR-015**: System MUST support `outputMode` parameter with values: "inline" (default), "file"
- **FR-016**: System MUST save file output to system temp directory by default with unique filename
- **FR-017**: System MUST support `outputPath` parameter to specify custom file save location
- **FR-018**: System MUST return file path, dimensions, format, and file size in bytes for file output
- **FR-019**: System MUST use appropriate file extension based on imageFormat (.jpg, .png)
- **FR-020**: System MUST apply all optimizations in order: capture → scale → encode → output

### Key Entities

- **ImageFormat**: Output encoding format - "jpeg" (lossy, small, default), "png" (lossless, large)
- **ImageQuality**: Compression quality for lossy formats (1-100, where 100 is highest quality/least compression)
- **ScalingConstraints**: Optional maxWidth (default: 1568) and maxHeight limits for downscaling
- **OutputMode**: How the image is delivered - "inline" (base64 in JSON) or "file" (saved to disk, path returned)
- **ScreenshotMetadata**: Response data including dimensions (actual and original), format, file size, and optionally file path

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Default captures (JPEG q85, maxWidth=1568) are at least 90% smaller than current PNG captures at native resolution
- **SC-002**: 4K display captures produce files under 300KB with default settings
- **SC-003**: 1080p display captures produce files under 200KB with default settings
- **SC-004**: Scaling operations add less than 50ms to capture time
- **SC-005**: File output mode response payloads are under 1KB (path + metadata only)
- **SC-006**: Scaled images maintain readable text and UI elements (validated by LLM vision analysis during integration testing)
- **SC-007**: All new parameters are documented in tool description for LLM discoverability
- **SC-008**: Existing screenshot functionality continues to work with explicit `imageFormat: "png"` and `maxWidth: 0`

## Assumptions

1. **JPEG Default**: JPEG at quality 85 provides optimal balance of file size and visual fidelity for LLM vision analysis
2. **1568px Threshold**: This matches Claude's high-res vision mode native limit; works well for GPT-4V and Gemini too
3. **No Upscaling**: Upscaling adds file size without information; always preserve original if smaller than limits
4. **Bicubic Interpolation**: Provides good balance of quality and performance for scaling
5. **Client Cleanup Responsibility**: When using file output mode, the client is responsible for deleting temp files
6. **Temp File Naming**: Temp files use format `screenshot_{timestamp}.{ext}` for uniqueness (e.g., `screenshot_20251210_143052_789.jpg` using `yyyyMMdd_HHmmss_fff`)

## Out of Scope

- Automatic temp file cleanup/TTL (client responsibility per clarification)
- Video recording or streaming
- Image analysis or OCR
- Multiple format outputs in single request
- Lossy-to-lossless conversion warnings
- WebP format support (deferred; requires native library dependency)

## Dependencies

- **Feature 005 (Screenshot Capture)**: This feature extends the existing screenshot implementation
- **.NET System.Drawing**: For image encoding (JPEG, PNG) and scaling operations
