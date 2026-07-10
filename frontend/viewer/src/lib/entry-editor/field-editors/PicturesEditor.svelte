<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
  import {Button} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import EditPictureDialog from './EditPictureDialog.svelte';
  import {ACCEPTED_PICTURE_TYPES, isLosslessImage} from './picture-formats';
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

  let fileInputElement = $state<HTMLInputElement>();
  // Which operation is in flight: 'add' drives the add-button spinner; 'edit' covers the
  // replace/delete/caption operations invoked from the edit dialog.
  let busyAction = $state<'add' | 'edit' | null>(null);

  // The picture currently open in the edit dialog, tracked by id so it stays in sync with the
  // reloaded entry after each change (e.g. its image updates in the dialog after a replace).
  let editingPictureId = $state<string>();
  const editingPicture = $derived(editingPictureId ? pictures.find((p) => p.id === editingPictureId) : undefined);
  let editDialogOpen = $state(false);

  function openEditor(picture: IPicture) {
    editingPictureId = picture.id;
    editDialogOpen = true;
  }

  function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset the input so selecting the same file again re-triggers `change`.
    target.value = '';
    if (file) void addPicture(file);
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
        // mediaUri, which we reuse to point a Picture at that same image.
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

  // --- Edit dialog operations (act on the picture currently open in the dialog) ---

  // Uploads a replacement file and returns its mediaUri, WITHOUT touching the model — the dialog
  // previews it and only commits on Submit (via submitEdits).
  async function uploadReplacement(file: File): Promise<string | null> {
    busyAction = 'edit';
    try {
      return await uploadFile(file);
    } finally {
      busyAction = null;
    }
  }

  // Applies the dialog's buffered edits (new caption and/or replaced image) in one update.
  async function submitEdits(after: IPicture): Promise<void> {
    const before = editingPicture ? $state.snapshot(editingPicture) : undefined;
    if (!before) return;
    busyAction = 'edit';
    try {
      await api.updatePicture(entryId, senseId, before, after);
    } finally {
      busyAction = null;
    }
  }

  async function deleteEditingPicture(): Promise<void> {
    const target = editingPicture ? $state.snapshot(editingPicture) : undefined;
    if (!target) return;
    if (!(await dialogsService.promptDelete($t`Picture`))) return;
    busyAction = 'edit';
    try {
      await api.deletePicture(entryId, senseId, target.id);
      editDialogOpen = false;
    } finally {
      busyAction = null;
    }
  }

  // The server rejects files above its size limit; the advice differs by format.
  function tooBigMessage(file: File): string {
    return isLosslessImage(file)
      ? $t`This picture is too large to upload. Try reducing the image resolution and uploading again.`
      : $t`This picture is too large to upload. Try saving it at a lower JPEG quality and uploading again.`;
  }
</script>

<div class="flex flex-col gap-2">
  {#if pictures.length > 0}
    <!-- Pictures flow left-to-right and wrap; on a narrow (mobile) screen they stack vertically
         with no CSS change. Each picture + its caption is one flex item. Clicking a picture (a
         pencil hints at this) opens the edit dialog. -->
    <div class="flex flex-wrap gap-4">
      {#each pictures as picture (picture.id)}
        <PictureImage
          {picture}
          busy={busyAction !== null}
          onEdit={readonly ? undefined : () => openEditor(picture)}
        />
      {/each}
    </div>
  {:else if readonly}
    <div class="text-muted-foreground p-1">
      {$t`No pictures`}
    </div>
  {/if}

  {#if !readonly}
    <!-- Right-aligned to match the "+ Component" button style. -->
    <div class="flex flex-wrap justify-end gap-2">
      <Button icon="i-mdi-plus" size="xs" loading={busyAction === 'add'} disabled={busyAction !== null} onclick={() => fileInputElement?.click()}>
        {$t`Picture`}
      </Button>
    </div>
    <!-- Hidden input drives the OS file picker for adding a picture; only JPG/PNG/TIFF/BMP offered. -->
    <input
      bind:this={fileInputElement}
      type="file"
      accept={ACCEPTED_PICTURE_TYPES}
      onchange={onFileSelected}
      class="hidden"
    />
  {/if}
</div>

{#if editingPicture}
  <EditPictureDialog
    bind:open={editDialogOpen}
    picture={editingPicture}
    onUploadReplacement={(file) => uploadReplacement(file)}
    onSubmit={(after) => void submitEdits(after)}
    onDelete={() => deleteEditingPicture()}
  />
{/if}
