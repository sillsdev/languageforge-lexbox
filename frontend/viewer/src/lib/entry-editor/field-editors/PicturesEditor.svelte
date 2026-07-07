<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
  import {Button} from '$lib/components/ui/button';
  import PictureCarousel from './PictureCarousel.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {randomId} from '$lib/utils';

  type Props = {
    value: IPicture[] | undefined;
    entryId: string;
    senseId: string;
    readonly?: boolean;
  };
  const {value, entryId, senseId, readonly = false}: Props = $props();

  const api = useLexboxApi();
  const dialogsService = useDialogsService();

  // `pictures` is typed as required, but older/legacy sense data may omit it entirely,
  // so guard against undefined rather than trusting the type at runtime.
  const pictures = $derived(value ?? []);

  // The carousel reports which picture is currently shown; Replace/Delete act on it.
  let selectedIndex = $state(0);
  const currentPicture = $derived(pictures[selectedIndex]);

  // Formats the browser accepts and that the server supports for pictures.
  const ACCEPTED_TYPES = 'image/jpeg,image/png';

  let fileInputElement = $state<HTMLInputElement>();
  // Which operation is in flight (drives per-button spinners + disabling the whole group).
  let busyAction = $state<'add' | 'replace' | 'delete' | null>(null);
  // Whether the file picker that's about to open is for adding or replacing.
  let pendingFileAction: 'add' | 'replace' = 'add';

  function pickFile(action: 'add' | 'replace') {
    pendingFileAction = action;
    fileInputElement?.click();
  }

  function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset the input so selecting the same file again re-triggers `change`.
    target.value = '';
    if (!file) return;
    if (pendingFileAction === 'replace') void replacePicture(file);
    else void addPicture(file);
  }

  // Uploads the chosen file and returns its mediaUri, or null if the upload was rejected
  // (a notification is shown for the rejection). saveFile reports outcome via `result`, not
  // exceptions, so we branch on it. We intentionally do NOT pre-check the file size: the size
  // limit lives on the server and may change, so we let the server decide and handle `TooBig`.
  async function uploadFile(file: File): Promise<string | null> {
    const response = await api.saveFile(file, {filename: file.name, mimeType: file.type});
    switch (response.result) {
      case UploadFileResult.SavedLocally:
      case UploadFileResult.SavedToLexbox:
        break;
      case UploadFileResult.TooBig:
        AppNotification.display(tooBigMessage(file), {type: 'error', timeout: 'long'});
        return null;
      case UploadFileResult.NotSupported:
        AppNotification.display($t`Uploading pictures is not supported here`, {type: 'error'});
        return null;
      case UploadFileResult.AlreadyExists:
        AppNotification.display($t`That picture has already been uploaded`, {type: 'error'});
        return null;
      case UploadFileResult.Error:
        AppNotification.display(response.errorMessage ?? $t`Unable to upload the picture`, {type: 'error'});
        return null;
    }
    if (!response.mediaUri) throw new Error('saveFile succeeded but returned no mediaUri');
    return response.mediaUri;
  }

  async function addPicture(file: File): Promise<void> {
    busyAction = 'add';
    try {
      const mediaUri = await uploadFile(file);
      if (!mediaUri) return;
      const picture: IPicture = {id: randomId(), order: pictures.length, mediaUri, caption: {}};
      await api.createPicture(entryId, senseId, picture);
      // The change surfaces via the entry-changed event, which reloads the entry.
    } finally {
      busyAction = null;
    }
  }

  async function replacePicture(file: File): Promise<void> {
    const before = currentPicture ? $state.snapshot(currentPicture) : undefined;
    if (!before) return;
    busyAction = 'replace';
    try {
      const mediaUri = await uploadFile(file);
      if (!mediaUri) return;
      // Keep the same picture (id, order, caption); only swap the image it points at.
      const after: IPicture = {...before, mediaUri};
      await api.updatePicture(entryId, senseId, before, after);
    } finally {
      busyAction = null;
    }
  }

  async function deletePicture(): Promise<void> {
    const target = currentPicture ? $state.snapshot(currentPicture) : undefined;
    if (!target) return;
    if (!(await dialogsService.promptDelete($t`Picture`))) return;
    busyAction = 'delete';
    try {
      await api.deletePicture(entryId, senseId, target.id);
    } finally {
      busyAction = null;
    }
  }

  // The server rejects files above its size limit. JPEGs can usually be shrunk by lowering
  // the export quality, whereas PNGs (lossless) need a smaller resolution instead.
  function tooBigMessage(file: File): string {
    const isPng = file.type === 'image/png' || /\.png$/i.test(file.name);
    return isPng
      ? $t`This picture is too large to upload. Try reducing the image resolution and uploading again.`
      : $t`This picture is too large to upload. Try saving it at a lower JPEG quality and uploading again.`;
  }
</script>

<div class="flex flex-col gap-2">
  {#if pictures.length > 0}
    <PictureCarousel {pictures} bind:selectedIndex />
  {:else if readonly}
    <div class="text-muted-foreground p-1">
      {$t`No pictures`}
    </div>
  {/if}

  {#if !readonly}
    <!-- Buttons right-aligned to match the "+ Component" button style. -->
    <div class="flex flex-wrap justify-end gap-2">
      <Button icon="i-mdi-plus" size="xs" loading={busyAction === 'add'} disabled={busyAction !== null} onclick={() => pickFile('add')}>
        {$t`Picture`}
      </Button>
      {#if pictures.length > 0}
        <Button
          icon="i-mdi-image-refresh"
          size="xs"
          variant="secondary"
          loading={busyAction === 'replace'}
          disabled={busyAction !== null}
          onclick={() => pickFile('replace')}
        >
          {$t`Replace Picture`}
        </Button>
        <Button
          icon="i-mdi-delete"
          size="xs"
          variant="destructive"
          loading={busyAction === 'delete'}
          disabled={busyAction !== null}
          onclick={() => deletePicture()}
        >
          {$t`Delete Picture`}
        </Button>
      {/if}
    </div>
    <!-- Hidden input drives the OS file picker (shared by add + replace); only JPG/PNG offered. -->
    <input
      bind:this={fileInputElement}
      type="file"
      accept={ACCEPTED_TYPES}
      onchange={onFileSelected}
      class="hidden"
    />
  {/if}
</div>
