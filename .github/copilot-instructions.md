# GitHub Copilot Instructions for VS-LiveMap-Revival

## Project Context

This is a **Vintage Story game mod** providing a real-time web-based map. It combines:
- **C# Backend** (.NET 8.0): Game mod running server-side with strict null checking enabled
- **TypeScript Frontend**: Leaflet.js-based web map interface

## Code Review Guidelines

### Do NOT Flag These Patterns

#### 1. Unsafe Code Blocks
**Unsafe code is intentional and necessary** for performance in bitmap manipulation:
```csharp
unsafe {
    uint* ptr = (uint*)scan0;
    // Direct pointer manipulation for speed
}
```
This is acceptable in `TileImage.cs`, `BasicRenderer.cs`, `SepiaRenderer.cs`, and other rendering code.

#### 2. Singleton Pattern
The `LiveMap.Api` singleton is **intentional architecture**:
```csharp
public static LiveMap? Api { get; private set; }
```
This provides a single access point for mod subsystems. Do not suggest dependency injection here.

#### 3. Nullable Reference Type Patterns
With `<Nullable>enable</Nullable>`, null checks like these are **appropriate**:
```csharp
if (TileImage?.CalculateShadows() ?? false) { }
BlocksToIgnore?.Contains(block.Top) ?? false
```
These null-conditional operators are correct given the nullable context.

#### 4. Collection Expressions
Modern C# collection expressions are used:
```csharp
LayerRegistry = [];
RendererRegistry = [];
```
Do not suggest replacing with `new List<>()`.

#### 5. Primary Constructors
Primary constructors are used throughout:
```csharp
public abstract class Renderer(string id) : Keyed { }
```
This is modern C# syntax, not a mistake.

#### 6. Direct SQLite Access
Direct database queries are **intentional** for performance:
```csharp
using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
```
We bypass Vintage Story's chunk loading for speed. LRU caching is implemented separately.

#### 7. AggressiveInlining
Performance-critical methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]`:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public virtual (int, int) ProcessBlock(BlockData.Data? block, int defY = 0)
```
This is intentional optimization, not premature.

### Architecture Patterns to Follow

#### Registry-Based Extensibility
Layers and renderers use registry pattern:
```csharp
LiveMap.Api.LayerRegistry.Register(new MyLayer());
LiveMap.Api.RendererRegistry.Register(new MyRenderer());
```
**Suggest registry pattern** for new extensibility points.

#### Async Task Management
Background tasks use `AsyncTaskManager` with cancellation tokens:
```csharp
public override async Task WriteToDisk(CancellationToken ct)
```
Always check `ct.IsCancellationRequested` in long-running operations.

#### Event-Driven Architecture
Game events flow through `EventCoordinator`:
```csharp
Sapi.Event.ChunkDirty += OnChunkDirty;
```
New event handlers should follow this pattern.

#### LRU Caching Strategy
Multiple caches exist for performance:
- `ChunkLoader`: Chunks, MapChunks, Regions
- `WebServer`: File cache (100MB, 500 files)

**Respect cache invalidation patterns** when modifying data flow.

### Code Style Conventions

#### Commit Messages
All commits must follow **conventional commits** (lowercase unless proper nouns/acronyms):
```
feat: add custom renderer support
fix: resolve shadow calculation bug
docs: update api documentation
perf: optimize chunk loading cache
```

#### Namespace Organization
```csharp
namespace livemap.render;     // lowercase namespaces
namespace livemap.task;
namespace livemap.layer;
```

#### Warnings as Errors
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is enabled.
**All warnings must be fixed**, not suppressed (except external dependencies in NoWarn).

### Frontend-Specific Guidelines

#### TypeScript Strict Mode
Strict mode is enabled. Proper typing is required:
```typescript
interface MarkerData {
    id: string;
    position: [number, number];
}
```

#### Leaflet.js Integration
Map markers follow Leaflet conventions:
```typescript
L.marker([x, z], { icon: customIcon }).addTo(map);
```

#### Polling Pattern
Frontend polls JSON endpoints (do not suggest WebSockets):
```typescript
setInterval(() => fetchMarkers(), 1000);
```
This is intentional for simplicity and compatibility.

### Performance Considerations

#### When to Optimize
- **Rendering pipeline**: Shadow calculation, tile generation (hot path)
- **Chunk loading**: Database queries (occurs frequently)
- **Marker updates**: JSON serialization (happens every second)

#### When NOT to Optimize
- Configuration loading (once at startup)
- Colormap generation (rare, client-initiated)
- Command handling (user-triggered)

### Security Considerations

#### DO Flag
- SQL injection risks (though we use parameterized queries)
- Path traversal in file operations
- XSS risks in HTML/JS generation
- Unvalidated user input in commands

#### DO NOT Flag
- CORS being enabled (required for web map functionality)
- HTTP server binding to 0.0.0.0 (configurable, documented)
- Direct file system access (necessary for tile/data serving)

### Testing Expectations

#### C# Tests
- Use Vitest for unit tests
- Mock Vintage Story APIs (ICoreServerAPI, etc.)
- Focus on logic, not integration

#### Frontend Tests
- Use Vitest with @testing-library/dom
- Test marker rendering, layer management
- Mock Leaflet.js when necessary

### Common False Positives

**Do NOT suggest:**
1. Replacing `async Task` with `async Task<Unit>` or similar
2. Adding try-catch to every async method (cancellation is expected)
3. Making static classes into singletons (LiveMap.Api pattern is intentional)
4. Removing `#pragma warning disable` for external Vintage Story APIs
5. Replacing LINQ with loops "for performance" unless profiled
6. Adding XML documentation to private methods (public API only)
7. Extracting constants for magic numbers in rendering (often algorithm-specific)

### What TO Suggest

**DO suggest:**
1. Missing null checks when nullable warnings appear
2. Potential race conditions in multi-threaded rendering
3. Unhandled exceptions in async tasks
4. Memory leaks (undisposed resources, event handler leaks)
5. Missing cancellation token checks in long loops
6. Inefficient LINQ in hot paths (if profiling supports it)
7. Breaking changes to public API without version bump

## Project-Specific Knowledge

### Vintage Story Context
- `ICoreServerAPI`: Main server API
- `IServerPlayer`: Player representation
- `IWorldAccessor`: World data access
- `ChunkPos`: Chunk position (16x16x16 blocks)
- Region: 512x512 blocks (32x32 chunks)

### Colormap System
Colormaps are generated **client-side** using Harmony patches to modify game calendar. Server requests, client generates, server receives via chunked network protocol.

### Web Server
GenHTTP v10 library. Do not suggest alternatives (Express, Kestrel, etc.) without understanding integration constraints.

### Tile Format
- Default: WebP at 80% quality
- Fallback: PNG for compatibility
- Zoom levels: 0 (base) to configurable max
- Hierarchical downsampling for performance

## Review Priority

**High Priority Issues:**
1. Memory leaks (especially in rendering pipeline)
2. Thread safety violations
3. Breaking API changes
4. Security vulnerabilities

**Medium Priority Issues:**
1. Missing error handling
2. Code duplication
3. Inconsistent naming

**Low Priority Issues:**
1. Minor style inconsistencies
2. Optional optimizations
3. Documentation improvements
