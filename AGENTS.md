## Project Overview

VS-LiveMap-Revival is a Vintage Story game mod providing a real-time, Google Maps-like web interface for viewing the game world. It consists of a C# backend (game mod) and TypeScript/Leaflet.js frontend (web map).

## Build System

### Building the Project
```bash
# Windows
./build.ps1
# Linux/Mac
./build.sh
```
This command:
1. Compiles the C# mod (target: .NET 8.0)
2. Runs `npm install` in the `web/` directory
3. Runs `npm run build` to bundle frontend assets via Webpack
4. Copies web assets to `bin/assets/livemap/config/`
5. Creates final mod package at `bin/livemap.zip`
6. Copies the mod package to the `Release` directory with versioning appended to the name

### Frontend Development
```bash
cd web
npm run build         # Production build
npm run server        # Development server with hot reload
npm run lint          # Run ESLint
npm run lint:fix      # Fix linting issues
npm run type-check    # TypeScript type checking
npm test              # Run Vitest tests
npm run test:watch    # Run tests in watch mode
```

### C# Testing
```bash
dotnet test           # Run all C# unit tests
```

### Documentation Generation
```powershell
.\build-docs.ps1      # Generate Doxygen documentation (requires Doxygen installed)
```
Output: `docs/html/`

## Architecture Overview

### High-Level Structure

**Dual-Component Architecture:**
- **C# Backend** (`src/`): Vintage Story mod running server-side
  - Hooks into game events (chunk loading, block changes)
  - Manages rendering pipeline, web server, and data layers
  - Provides extensibility API for other mods
- **TypeScript Frontend** (`web/src/`): Browser-based map interface
  - Leaflet.js for map rendering
  - Polls JSON endpoints for real-time marker updates
  - Loads pre-rendered map tiles

### Core Backend Components

**Entry Point:** `LiveMapMod.cs` → initializes `LiveMap.cs` singleton

**Key Subsystems:**
- `EventCoordinator.cs` - Subscribes to game events, triggers rendering
- `RenderTaskManager.cs` - Triple-queue system (buffer, high-priority, low-priority)
- `AsyncTaskManager.cs` - Background tasks for marker/settings JSON updates
- `WebServer.cs` - GenHTTP-based web server with LRU file caching
- `NetworkHandler.cs` - Client-server protocol for colormap generation

**Registries (Extension Points):**
- `LayerRegistry.cs` - Register custom marker layers (players, traders, custom)
- `RendererRegistry.cs` - Register custom rendering styles (basic, sepia, custom)

### Rendering Pipeline

1. **Event Detection:** Game events → `EventCoordinator` queues regions for rendering
2. **Queue Management:** `RenderTaskManager` prioritizes render tasks
3. **Block Scanning:** `RenderTask` loads chunks from SQLite, scans 512x512 block columns
4. **Tile Generation:** Registered renderers process blocks → generate zoom levels → save WebP/PNG tiles
5. **Client Loading:** Frontend requests tiles via HTTP, Leaflet.js displays them

**Chunk Loading:** Direct SQLite read-only access with three-tier LRU cache (chunks, map chunks, regions)

**Tile Structure:** `tiles/{renderer}/{zoom}/{regionX}_{regionZ}.webp`

### Real-Time Data Flow

**Static Data (Map Tiles):**
- Game world changes → rendering pipeline → static tile files
- Served via WebServer LRU cache

**Dynamic Data (Markers):**
- `AsyncTaskManager` polls layers every N seconds
- Writes JSON to `data/markers/{layer}.json` or `data/json/{layer}.json`
- Frontend polls `data/markers.json` index, fetches updated layer files

### Extensibility API

**Adding Custom Layers:**
```csharp
public class MyLayer : Layer {
    public MyLayer() : base("my-layer", "My Layer") {
        Interval = 5; // Update every 5 seconds
    }

    public override async Task WriteToDisk(CancellationToken ct) {
        Markers.Add(new Icon("id", "icon", position));
        await base.WriteToDisk(ct);
    }
}

LiveMap.Api.LayerRegistry.Register(new MyLayer());
```

**Adding Custom Renderers:**
```csharp
public class MyRenderer : Renderer {
    public MyRenderer() : base("my-style") { }

    public override void ProcessBlockData(int regionX, int regionZ, BlockData blockData) {
        // Custom rendering logic
    }
}

LiveMap.Api.RendererRegistry.Register(new MyRenderer());
```

## Key Technical Details

### Colormap System
- Season-based block coloring using Harmony patches
- 12 monthly colormap files (`colormap-{1-12}.json`)
- Generated client-side, transferred via chunked network protocol
- Stored as: `{ "block-code": [color1, color2, ...] }`

### Performance Optimizations
- **Shadow Calculation:** Neighbor caching reduces redundant `ProcessBlock` calls
- **Incremental Saves:** Only saves changed zoom levels
- **Unsafe Pointers:** Direct bitmap manipulation for speed
- **LRU Caches:** File cache (100MB, 500 files), chunk cache (configurable)

### Configuration
- Main config: JSON file in Vintage Story data directory
- Editable via in-game `/livemap config` commands
- Settings include: web server port, rendering styles, layer intervals, cache sizes

## Common Development Patterns

### C# Code Style
- **Nullable Reference Types:** Enabled (`<Nullable>enable</Nullable>`)
- **Warnings as Errors:** Enabled (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **Unsafe Code:** Allowed for performance-critical bitmap operations
- **Internals Visible To:** Tests project has access to internal members

### Frontend Code Style
- **TypeScript:** Strict mode enabled
- **Linting:** ESLint with @nodecraft/eslint-config
- **Testing:** Vitest with @testing-library/dom

### Commit Convention
All commits use conventional commits format (fully lowercase unless proper nouns/acronyms).

## Project-Specific Notes

### Web Asset Pipeline
- Source: `web/src/` (TypeScript, SCSS)
- Build: `web/dist/` (Webpack bundles)
- Deployment: `bin/assets/livemap/config/` (copied during build)
- Static files: `web/public/` (copied as-is, excluding tiles/data)

### VINTAGE_STORY Environment Variable
The `.csproj` references Vintage Story DLLs via `$(VINTAGE_STORY)` environment variable. Set this to your Vintage Story installation path for local development.

### Final Package Structure
```
livemap.zip
├── livemap.dll (compiled mod)
├── modinfo.json (version/description injected at build time)
├── assets/livemap/config/
│   ├── index.html (web frontend entry point)
│   └── dist/ (bundled JS/CSS)
└── resources/ (LICENSE, README.md)
```

### Built-in Layers
- **players** - Real-time player positions (configurable filters: sneaking, underground, spectator)
- **spawn** - World spawn point
- **traders** - Discovered trader locations
- **translocators** - Discovered translocator network
- **vscartographer** - Integration with VSCartographer mod waypoints

### Available Marker Types
Located in `src/layer/marker/`:
- `Icon` - Point markers with custom icons
- `Circle`, `Ellipse`, `Rectangle` - Shapes
- `Polygon`, `Polyline` - Custom paths/areas

### Working Files
- `runData` is the directory that contains a running server (ran with `the runServer.ps1/sh` script)
- `runDataClient` is the directory that contains a running client (ran with `the runClient.ps1/sh` script)
