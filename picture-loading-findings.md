# Picture loading: local-vs-remote findings

Investigation into detecting whether a sense picture is available **locally** vs **only
remotely**, and why the feature was ultimately implemented as a cache-based "click to load"
instead of a true locality check.

## Question

Can the viewer frontend tell, at render time, whether a picture's image is already available
locally (so it can auto-display it) versus only available remotely (so it should defer loading)?

## Finding: locality is backend runtime state, not derivable on the frontend

- A picture references its image via `mediaUri`, a `sil-media://{authority}/{fileId}` URI
  (`backend/FwLite/MiniLcm/Media/MediaUri.cs`). The URI encodes a `fileId` + `authority`; it does
  **not** encode whether the file is cached on disk. Locality is dynamic — a remote file becomes
  local after it is downloaded — so the same `mediaUri` is used before and after caching.
- The frontend loads images through `IMiniLcmJsInvokable.getFileStream(mediaUri)`
  (`frontend/viewer/src/lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable.ts`).
  There is **no** parameter or companion method exposing "is this local?" and no "local-only" mode.
- On the backend, `LcmCrdt/MediaServer/LcmMediaService.GetFileStream(fileId)` is where locality is
  actually known:
  - `resourceService.GetLocalResource(fileId)` returns the local file if cached.
  - If it is **not** cached, the method **downloads it** (or returns `ReadFileResult.Offline` when
    offline). There is no way to ask "would you have to download this?" without triggering the
    download.
- `ReadFileResult` (`backend/FwLite/MiniLcm/Media/ReadFileResult.cs`) has `Success`, `NotFound`,
  `Offline`, `NotSupported`, `Error` — nothing for "available remotely but not downloaded".
- FwData (FieldWorks desktop) projects store pictures as local files on disk
  (`FwDataMiniLcmBridge/Api/FwDataMiniLcmApi.GetFileStream`), so they have no remote/download concept
  at all — they are always local.

### What a true locality feature required (subsequently done)

A cross-boundary change to the shared MiniLcm media API — implemented as `downloadIfMissing`:

1. `MiniLcm/IMiniLcmReadApi.GetFileStream(MediaUri, bool downloadIfMissing = true)` — the flag.
2. `LcmCrdt/CrdtMiniLcmApi` + `LcmCrdt/MediaServer/LcmMediaService` — when not cached locally and
   `downloadIfMissing == false`, return `ReadFileResult.NotFound` instead of downloading (reusing
   `NotFound` rather than adding a new enum value).
3. `FwDataMiniLcmBridge/Api/FwDataMiniLcmApi` — flag is a no-op (files are always local).
4. `FwLiteShared/Services/MiniLcmJsInvokable` — threads the flag; the regenerated TypeScript
   `getFileStream(mediaUri, downloadIfMissing)` is what the viewer consumes.
5. Frontend + the in-memory demo API (which simulates a remote media service for its pre-seeded
   pictures) exercise it.

## Decision (final)

An interim cache-based "click to load" (no backend change) shipped first, then the backend gained a
`downloadIfMissing` flag on `GetFileStream`, which let us implement the original locality-based
design:

- `GetFileStream(mediaUri, downloadIfMissing)` — when `false`, a file that isn't cached locally
  returns `ReadFileResult.NotFound` **without** downloading (no new enum value was needed; `NotFound`
  from a local-only probe is treated as "not downloaded yet"). When `true`, it downloads as before.
- The viewer's `ImageService` probes each picture with `downloadIfMissing: false` on render
  (`ensureLocal`): a locally-available picture displays immediately; one that would have to be
  fetched from the remote media service stays `not-downloaded` and shows a "Click to load" /
  "Tap to load" placeholder.
- Clicking the placeholder calls `download()` (`downloadIfMissing: true`) to fetch and display it.
- The cache is project-scoped, so once a `mediaUri` is loaded, every picture sharing it — and the
  edit dialog / fullscreen viewer, and revisits after navigating entries — display it immediately.

FwData pictures are always local, so they always auto-load.
