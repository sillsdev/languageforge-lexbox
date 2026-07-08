<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {FormatDate} from '$lib/components/ui/format';
  import type {IHarmonyResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/IHarmonyResource';
  import type {ILcmFileMetadata} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ILcmFileMetadata';
  import type {IMediaFilesServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMediaFilesServiceJsInvokable';
  import type {IReadFileResponseJs} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IReadFileResponseJs';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import {AppNotification} from '$lib/notifications/notifications';
  import AudioInput, {AUDIO_LOADER_HANDLED} from '$lib/components/field-editors/audio-input.svelte';
  import {cn} from '$lib/utils';
  import {resource} from 'runed';
  import {t} from 'svelte-i18n-lingui';

  type LocationStatus = 'local' | 'remote' | 'both';

  let {
    file,
    mediaFilesService,
    locationStatus: status,
    loadingFile = false,
    onLoadFile: onLoadFile,
    class: className,
  }: {
    file: IHarmonyResource;
    mediaFilesService?: IMediaFilesServiceJsInvokable;
    locationStatus: LocationStatus;
    loadingFile?: boolean;
    onLoadFile?: (fileId: string) => Promise<void>;
    class?: string;
  } = $props();

  let savingAs = $state(false);

  const locationLabels: Record<LocationStatus, () => string> = {
    local: () => $t`Local only`,
    remote: () => $t`Remote only`,
    both: () => $t`Local and remote`,
  };

  function displayNameFromPath(localPath?: string, fallback?: string): string {
    if (localPath) {
      return localPath.split(/[/\\]/).pop() ?? fallback ?? '';
    }
    return fallback ?? '';
  }

  function guessMimeType(filename: string): string {
    const ext = filename.slice(filename.lastIndexOf('.')).toLowerCase();
    const mimeByExt: Record<string, string> = {
      '.mp3': 'audio/mpeg',
      '.wav': 'audio/wav',
      '.ogg': 'audio/ogg',
      '.webm': 'audio/webm',
      '.m4a': 'audio/mp4',
      '.jpg': 'image/jpeg',
      '.jpeg': 'image/jpeg',
      '.png': 'image/png',
      '.gif': 'image/gif',
      '.webp': 'image/webp',
      '.svg': 'image/svg+xml',
      '.mp4': 'video/mp4',
      '.pdf': 'application/pdf',
    };
    return mimeByExt[ext] ?? 'application/octet-stream';
  }

  function formatFileSize(bytes?: number): string | undefined {
    if (bytes == null) return undefined;
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;
    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }
    return `${size.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
  }

  const metadata = resource(
    () => [file.id, file.remote, file.localPath, file.metadata, mediaFilesService] as const,
    async ([fileId, remote, localPath, metadata, service]): Promise<ILcmFileMetadata | undefined> => {
      if (metadata) {
        return metadata;
      }
      if (remote && service) {
        try {
          return await service.getFileMetadata(fileId);
        } catch {
          // fall through to local fallback when available
        }
      }
      if (localPath) {
        const filename = displayNameFromPath(localPath, fileId);
        return {
          filename,
          mimeType: guessMimeType(filename),
        };
      }
      return undefined;
    },
  );

  async function loadFileBlob(
    service: IMediaFilesServiceJsInvokable,
    fileId: string,
    mimeTypeHint?: string,
    filenameHint?: string,
  ): Promise<{kind: 'success'; blob: Blob; mimeType: string} | {kind: 'error'; response: IReadFileResponseJs}> {
    const response = await service.getFileStream(fileId);
    if (!response.stream) {
      return {kind: 'error', response};
    }

    const stream = await response.stream.stream();
    const blob = await new Response(stream).blob();
    return {
      kind: 'success',
      blob,
      mimeType: mimeTypeHint || blob.type || guessMimeType(filenameHint ?? response.fileName ?? ''),
    };
  }

  const isAudioPreview = $derived(file.local && (metadata.current?.mimeType.startsWith('audio/') ?? false));

  async function mediaFileAudioLoader(fileId: string) {
    if (!mediaFilesService) throw new Error('No media files service');
    const response = await mediaFilesService.getFileStream(fileId);
    if (!response.stream) {
      AppNotification.error($t`Unable to load audio`, readFileErrorMessage(response.result, response.errorMessage));
      return AUDIO_LOADER_HANDLED;
    }
    return {
      stream: await response.stream.stream(),
      filename: response.fileName ?? metadata.current?.filename ?? fileId,
    };
  }

  const preview = resource(
    () => [file.id, file.local, mediaFilesService, metadata.current] as const,
    async ([fileId, local, service, meta]) => {
      if (!service || !local || !meta) return undefined;
      if (meta.mimeType.startsWith('audio/')) return undefined;

      const result = await loadFileBlob(service, fileId, meta.mimeType, meta.filename);
      if (result.kind === 'error') {
        return {kind: 'error' as const, response: result.response};
      }

      const url = URL.createObjectURL(result.blob);
      return {
        kind: 'success' as const,
        url,
        blob: result.blob,
        mimeType: result.mimeType,
      };
    },
  );

  $effect(() => {
    const url = preview.current?.kind === 'success' ? preview.current.url : undefined;
    return () => {
      if (url) URL.revokeObjectURL(url);
    };
  });

  const effectiveMimeType = $derived(
    metadata.current?.mimeType ?? (preview.current?.kind === 'success' ? preview.current.mimeType : undefined),
  );

  const previewKind = $derived.by(() => {
    const mime = effectiveMimeType ?? '';
    if (mime.startsWith('image/')) return 'image';
    if (mime.startsWith('audio/')) return 'audio';
    if (mime.startsWith('video/')) return 'video';
    return 'other';
  });

  const title = $derived(metadata.current?.filename ?? displayNameFromPath(file.localPath, file.id));

  const canSaveAs = $derived(file.local);

  function readFileErrorMessage(result: ReadFileResult, errorMessage?: string): string {
    switch (result) {
      case ReadFileResult.NotFound:
        return $t`File not found`;
      case ReadFileResult.Offline:
        return $t`Offline, unable to open file`;
      case ReadFileResult.NotSupported:
        return $t`Preview not supported`;
      default:
        return errorMessage ?? $t`Unable to open file`;
    }
  }

  async function loadBlobForSave(): Promise<Blob | undefined> {
    if (preview.current?.kind === 'success') {
      return preview.current.blob;
    }
    if (!file.local || !mediaFilesService) return undefined;

    const result = await loadFileBlob(
      mediaFilesService,
      file.id,
      metadata.current?.mimeType,
      metadata.current?.filename,
    );
    if (result.kind === 'error') {
      AppNotification.error(
        $t`Unable to export file`,
        readFileErrorMessage(result.response.result, result.response.errorMessage),
      );
      return undefined;
    }

    return result.blob;
  }

  async function saveAs() {
    if (!canSaveAs || savingAs) return;

    savingAs = true;
    try {
      const blob = await loadBlobForSave();
      if (!blob) return;

      const filename = title;
      const savePicker = (
        window as Window & {
          showSaveFilePicker?: (options?: {suggestedName?: string}) => Promise<FileSystemFileHandle>;
        }
      ).showSaveFilePicker;
      if (savePicker) {
        try {
          const handle = await savePicker({suggestedName: filename});
          const writable = await handle.createWritable();
          await writable.write(blob);
          await writable.close();
          return;
        } catch (error) {
          if (error instanceof DOMException && error.name === 'AbortError') return;
        }
      }

      const url = URL.createObjectURL(blob);
      try {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = filename;
        anchor.click();
      } finally {
        URL.revokeObjectURL(url);
      }
    } catch (error) {
      AppNotification.error($t`Failed to save file`, error instanceof Error ? error.message : String(error));
    } finally {
      savingAs = false;
    }
  }
</script>

<div class={cn('flex h-full min-h-0 flex-col', className)}>
  <div class="shrink-0 border-b px-4 py-3">
    <div class="flex items-start justify-between gap-3">
      <div class="min-w-0">
        <h2 class="break-all text-lg font-semibold leading-snug">{title}</h2>
        <p class="mt-1 text-sm text-muted-foreground">{locationLabels[status]()}</p>
      </div>
      {#if canSaveAs}
        <Button
          variant="outline"
          icon="i-mdi-content-save"
          class="shrink-0"
          loading={savingAs}
          disabled={savingAs || (!isAudioPreview && preview.loading)}
          onclick={() => void saveAs()}
        >
          {$t`Save as`}
        </Button>
      {/if}
    </div>
  </div>

  <div class="min-h-0 flex-1 space-y-4 overflow-y-auto p-4">
    {#if metadata.loading && !metadata.current}
      <p class="text-sm text-muted-foreground">{$t`Loading metadata...`}</p>
    {:else if metadata.error}
      <p class="text-sm text-destructive">{$t`Failed to load metadata`}</p>
    {:else if metadata.current}
      <dl class="grid gap-3 text-sm sm:grid-cols-[auto_1fr]">
        <dt class="text-muted-foreground">{$t`Filename`}</dt>
        <dd class="break-all font-medium">{metadata.current.filename}</dd>

        <dt class="text-muted-foreground">{$t`Type`}</dt>
        <dd class="break-all">{metadata.current.mimeType}</dd>

        {#if metadata.current.author}
          <dt class="text-muted-foreground">{$t`Author`}</dt>
          <dd>{metadata.current.author}</dd>
        {/if}

        {#if metadata.current.uploadDate}
          <dt class="text-muted-foreground">{$t`Uploaded`}</dt>
          <dd>
            <FormatDate date={metadata.current.uploadDate} options={{dateStyle: 'medium', timeStyle: 'short'}} />
          </dd>
        {/if}

        {#if metadata.current.sizeInBytes != null}
          <dt class="text-muted-foreground">{$t`Size`}</dt>
          <dd>{formatFileSize(metadata.current.sizeInBytes)}</dd>
        {/if}

        <dt class="text-muted-foreground">{$t`ID`}</dt>
        <dd class="break-all font-mono text-xs">{file.id}</dd>
      </dl>
    {/if}

    <section class="space-y-2">
      <h3 class="text-sm font-medium">{$t`Preview`}</h3>

      {#if file.remote && !file.local}
        <div class="rounded-md border border-dashed px-4 py-6 text-center text-sm text-muted-foreground">
          <Icon icon="i-mdi-cloud-download-outline" class="mx-auto mb-2 size-8" />
          <p>{$t`This file is only available remotely.`}</p>
          <p class="mt-1">{$t`Load it to preview or export.`}</p>
          <Button
            variant="outline"
            class="mt-3"
            icon="i-mdi-download"
            loading={loadingFile}
            disabled={loadingFile || !onLoadFile}
            onclick={() => void onLoadFile?.(file.id)}
          >
            {$t`Load`}
          </Button>
        </div>
      {:else if isAudioPreview}
        <AudioInput audioId={file.id} readonly loader={mediaFileAudioLoader} />
      {:else if preview.loading}
        <div class="flex items-center gap-2 text-sm text-muted-foreground">
          <Icon icon="i-mdi-loading" class="size-4 animate-spin" />
          {$t`Loading preview...`}
        </div>
      {:else if preview.current?.kind === 'error'}
        <div class="rounded-md border border-dashed px-4 py-6 text-center text-sm text-muted-foreground">
          <Icon icon="i-mdi-file-alert-outline" class="mx-auto mb-2 size-8" />
          <p>{readFileErrorMessage(preview.current.response.result, preview.current.response.errorMessage)}</p>
        </div>
      {:else if preview.current?.kind === 'success'}
        {#if previewKind === 'image'}
          <img
            src={preview.current.url}
            alt={title}
            class="max-h-80 w-full rounded-md border bg-background object-contain"
          />
        {:else if previewKind === 'video'}
          <!-- svelte-ignore a11y_media_has_caption -->
          <video controls src={preview.current.url} class="max-h-80 w-full rounded-md border bg-background"></video>
        {:else}
          <div class="rounded-md border border-dashed px-4 py-6 text-center text-sm text-muted-foreground">
            <Icon icon="i-mdi-file-outline" class="mx-auto mb-2 size-8" />
            <p>{$t`Preview not available for this file type.`}</p>
          </div>
        {/if}
      {/if}
    </section>
  </div>
</div>
