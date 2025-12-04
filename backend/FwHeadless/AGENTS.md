# FwHeadless

Headless service for FieldWorks data processing. Handles Mercurial sync, FwData conversion, and SendReceive operations.

## Run

```bash
dotnet run --project FwHeadless.csproj
```

## Purpose

FwHeadless bridges between:
- **LexBox** (Harmony/CRDT sync)
- **Classic FieldWorks** (Mercurial Send/Receive)

It processes FwData (FieldWorks XML format) and syncs changes bidirectionally.

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `Services/` | Core sync and processing services |
| `Mercurial/` | Mercurial repository operations |
| `MercurialExtensions/` | Custom hg extensions |
| `Controllers/` | API endpoints |
| `Routes/` | Route handlers |
| `Media/` | Media file handling |

## Key Concepts

### SendReceive Flow

1. Classic FieldWorks does S/R to Mercurial repo
2. FwHeadless detects changes
3. Converts FwData → CRDT changes
4. Syncs to Harmony (LexBox)
5. Reverse: Harmony → FwData → Mercurial

### FwData

FieldWorks XML format containing:
- Lexical entries
- Senses, definitions, examples
- Writing systems
- Custom fields

## Configuration

- `FwHeadlessConfig.cs` - Service configuration
- `appsettings.json` - Environment settings

## Important Files

- `Program.cs` - Entry point
- `FwHeadlessKernel.cs` - DI registration
- `Services/` - Core business logic
- `LexboxFwDataMediaAdapter.cs` - Media file handling
- `HttpClientAuthHandler.cs` - Auth for LexBox API calls

## Common Issues

- **Stuck hg commits**: Check `Mercurial/` for lock files
- **Sync failures**: Check FwHeadless logs for FwData parsing errors
- **Media sync**: Large files may timeout, see `Media/`
