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

### What a true locality feature would have required (not done)

A 🔴 cross-boundary change to the shared MiniLcm media API:

1. `MiniLcm/Media/ReadFileResult.cs` — a new value, e.g. `RemoteNotLoaded`.
2. `MiniLcm/IMiniLcmReadApi.GetFileStream(MediaUri, bool download = true)` — a new flag.
3. `LcmCrdt/CrdtMiniLcmApi` + `LcmCrdt/MediaServer/LcmMediaService` — when not cached locally and
   `download == false`, return `RemoteNotLoaded` instead of downloading.
4. `FwDataMiniLcmBridge/Api/FwDataMiniLcmApi` — flag is a no-op (files are always local).
5. `FwLiteShared/Services/MiniLcmJsInvokable` — thread the flag, then rebuild `FwLiteShared` to
   regenerate the TypeScript types (`getFileStream`, `ReadFileResult`).
6. Frontend + the in-memory demo API to exercise it.

## Decision

Skip the backend/locality work. Instead, gate auto-display purely on the **entry-view image cache**
(`frontend/viewer/src/lib/entry-editor/field-editors/image-service.svelte.ts`):

- A picture whose `mediaUri` is **not in the cache** shows a "Click to load" / "Tap to load"
  placeholder (styled like the error placeholder) and does not fetch anything up front.
- Clicking/tapping the placeholder loads the image into the cache, replacing the placeholder.
- Because the cache is shared for the whole entry view, once a `mediaUri` is loaded, every picture
  that shares it — and the edit dialog / fullscreen viewer — display it immediately from the cache.

No backend API change.
