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

  // Formats the browser accepts and that the server supports for pictures.
  const ACCEPTED_TYPES = 'image/jpeg,image/png,image/tiff,image/bmp';

  let fileInputElement = $state<HTMLInputElement>();
  // Which operation is in flight (drives the add-button spinner + disabling the edit affordances).
  let busyAction = $state<'add' | 'replace' | 'delete' | null>(null);
  // The picture to replace once a file is chosen; undefined means the picker is adding a new one.
  let pendingReplace = $state<IPicture | undefined>(undefined);

  function pickAddFile() {
    pendingReplace = undefined;
    fileInputElement?.click();
  }

  function requestReplace(picture: IPicture) {
    pendingReplace = $state.snapshot(picture);
    fileInputElement?.click();
  }

  function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset the input so selecting the same file again re-triggers `change`.
    target.value = '';
    const replaceTarget = pendingReplace;
    pendingReplace = undefined;
    if (!file) return;
    if (replaceTarget) void replacePicture(replaceTarget, file);
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
      case UploadFileResult.AlreadyExists:
        // AlreadyExists is not an error here: one image file (mediaUri) can back many Picture
        // objects across different senses/entries. The server returns the existing file's
        // mediaUri, which we reuse to create a new Picture pointing at that same image.
        break;
      case UploadFileResult.TooBig:
        AppNotification.display(tooBigMessage(file), {type: 'error', timeout: 'long'});
        return null;
      case UploadFileResult.NotSupported:
        AppNotification.display($t`Uploading pictures is not supported here`, {type: 'error'});
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

  async function replacePicture(before: IPicture, file: File): Promise<void> {
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

  async function deletePicture(picture: IPicture): Promise<void> {
    const target = $state.snapshot(picture);
    if (!(await dialogsService.promptDelete($t`Picture`))) return;
    busyAction = 'delete';
    try {
      await api.deletePicture(entryId, senseId, target.id);
    } finally {
      busyAction = null;
    }
  }

  // The server rejects files above its size limit. JPEGs can usually be shrunk by lowering
  // the export quality, whereas lossless formats (PNG, BMP, TIFF) need a smaller resolution.
  function tooBigMessage(file: File): string {
    const isLossless = /^image\/(png|bmp|tiff)$/.test(file.type) || /\.(png|bmp|tiff?)$/i.test(file.name);
    return isLossless
      ? $t`This picture is too large to upload. Try reducing the image resolution and uploading again.`
      : $t`This picture is too large to upload. Try saving it at a lower JPEG quality and uploading again.`;
  }
</script>

<div class="flex flex-col gap-2">
  {#if pictures.length > 0}
    <!-- Replace/Delete live on the picture itself (tap the image to replace, trash-can in the
         corner to delete), so they work on touch screens without hover. -->
    <PictureCarousel
      {pictures}
      {readonly}
      busy={busyAction !== null}
      onReplacePicture={requestReplace}
      onDeletePicture={deletePicture}
    />
  {:else if readonly}
    <div class="text-muted-foreground p-1">
      {$t`No pictures`}
    </div>
  {/if}

  {#if !readonly}
    <!-- Right-aligned to match the "+ Component" button style. -->
    <div class="flex flex-wrap justify-end gap-2">
      <Button icon="i-mdi-plus" size="xs" loading={busyAction === 'add'} disabled={busyAction !== null} onclick={pickAddFile}>
        {$t`Picture`}
      </Button>
    </div>
    <!-- Hidden input drives the OS file picker (shared by add + replace); only JPG/PNG/TIFF/BMP offered. -->
    <input
      bind:this={fileInputElement}
      type="file"
      accept={ACCEPTED_TYPES}
      onchange={onFileSelected}
      class="hidden"
    />
  {/if}
</div>
