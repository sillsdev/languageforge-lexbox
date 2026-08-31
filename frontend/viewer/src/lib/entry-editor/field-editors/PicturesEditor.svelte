<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
  import {Button} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import EditPictureDialog from './EditPictureDialog.svelte';
  import PictureViewerDialog from './PictureViewerDialog.svelte';
  import {ACCEPTED_PICTURE_TYPES, isLosslessImage, isSupportedImageType} from './picture-formats';
  import {downloadPictureFile} from './picture-actions';
  import {useImageService} from './image-service.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {randomId} from '$lib/utils';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';

  type Props = {
    pictures: IPicture[];
    entryId: string;
    senseId: string;
    readonly?: boolean;
  };
  let {pictures = $bindable(), entryId, senseId, readonly = false}: Props = $props();

  const api = useLexboxApi();
  const dialogsService = useDialogsService();
  const platformFeatures = usePlatformFeaturesService();
  const imageService = useImageService();

  let fileInputElement = $state<HTMLInputElement>();
  let busyAction = $state<'add' | 'edit' | null>(null);

  let editingPictureId = $state<string>();
  const editingPicture = $derived(editingPictureId ? pictures.find((p) => p.id === editingPictureId) : undefined);
  // A not-yet-uploaded draft picture (in-memory preview) shown in the dialog in "add" mode.
  let draftPicture = $state<IPicture>();
  let draftFile = $state<File>();
  let editDialogOpen = $state(false);
  const dialogPicture = $derived(draftPicture ?? editingPicture);
  // Retain the last picture the dialog showed so it stays mounted for its close animation even after
  // that picture is removed from `pictures` (e.g. deleted from within the dialog). The draft is
  // intentionally NOT cleared on close (only overwritten on the next open) so dialogPicture/isNew
  // stay stable through the fade-out.
  let lastDialogPicture = $state<IPicture>();
  $effect(() => {
    if (dialogPicture) lastDialogPicture = dialogPicture;
  });

  let viewerPictureId = $state<string>();
  let viewerOpen = $state(false);

  // Revokes the current draft's preview blob url (if any). Called whenever a draft is superseded or
  // discarded so previews are freed promptly rather than piling up in the cache until dispose().
  function releaseDraftPreview() {
    if (draftPicture) imageService.releaseLocalPreview(draftPicture.mediaUri);
  }

  function openEditor(picture: IPicture) {
    releaseDraftPreview();
    draftPicture = undefined;
    draftFile = undefined;
    editingPictureId = picture.id;
    editDialogOpen = true;
  }

  // Opens the dialog in "add" mode on an in-memory draft (nothing uploaded/created until Submit).
  function openCreate(file: File) {
    releaseDraftPreview();
    const mediaUri = imageService.registerLocalPreview(file);
    editingPictureId = undefined;
    draftFile = file;
    draftPicture = {id: randomId(), order: pictures.length, mediaUri, caption: {}};
    editDialogOpen = true;
  }

  function openViewer(picture: IPicture) {
    viewerPictureId = picture.id;
    viewerOpen = true;
  }

  async function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset the input so selecting the same file again re-triggers `change`.
    target.value = '';
    if (!file) return;
    busyAction = 'add';
    try {
      openCreate(await convertPicture(file));
    } catch {
      // convertPicture already shows a notification on failure
    } finally {
      busyAction = null;
    }
  }

  async function uploadFile(file: File): Promise<string | null> {
    const response = await api.saveFile(file, {filename: file.name, mimeType: file.type, extraFields: {}});
    switch (response.result) {
      case UploadFileResult.SavedLocally:
      case UploadFileResult.SavedToLexbox:
      case UploadFileResult.AlreadyExists:
        // AlreadyExists is not an error here: multiple Picture objects might share one mediaUri
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

  // In-memory replace for a draft: swap the buffered File + preview, no upload yet.
  function replaceDraftFile(file: File): string {
    // The old preview is superseded by this one; free it before registering the replacement.
    releaseDraftPreview();
    const mediaUri = imageService.registerLocalPreview(file);
    draftFile = file;
    if (draftPicture) draftPicture = {...draftPicture, mediaUri};
    return mediaUri;
  }

  // Upload + create only now. Returns false (dialog stays open) if the upload was rejected.
  async function submitNewPicture(after: IPicture): Promise<boolean> {
    const file = draftFile; // capture before any await; draft state may change later
    if (!file) return false;
    busyAction = 'add';
    try {
      const mediaUri = await uploadFile(file);
      if (!mediaUri) return false; // uploadFile already showed the error notification
      const created = await api.createPicture(entryId, senseId, {...after, mediaUri});
      pictures = [...pictures, created];
      return true;
    } finally {
      busyAction = null;
    }
  }

  // --- Edit dialog operations (act on the picture currently open in the dialog) ---

  // Uploads replacement file and returns its mediaUri, WITHOUT touching the model until dialog is submitted
  async function uploadReplacement(file: File): Promise<string | null> {
    busyAction = 'edit';
    try {
      return await uploadFile(file);
    } finally {
      busyAction = null;
    }
  }

  async function submitEdits(after: IPicture): Promise<void> {
    const before = editingPicture ? $state.snapshot(editingPicture) : undefined;
    if (!before) return;
    busyAction = 'edit';
    try {
      const newPicture = await api.updatePicture(entryId, senseId, before, after);
      pictures = pictures.map((p) => (p.id === after.id ? newPicture : p));
    } finally {
      busyAction = null;
    }
  }

  async function deletePicture(pictureId: string): Promise<void> {
    if (!(await dialogsService.promptDelete($t`Picture`))) return;
    busyAction = 'edit';
    try {
      // Close the edit dialog (if open on this picture) *before* deleting so its close animation
      // has time to play; harmless when the delete came from the field/menu instead.
      editDialogOpen = false;
      await api.deletePicture(entryId, senseId, pictureId);
      pictures = pictures.filter((p) => p.id !== pictureId);
    } finally {
      busyAction = null;
    }
  }

  function deleteEditingPicture(): Promise<void> {
    return editingPicture ? deletePicture(editingPicture.id) : Promise.resolve();
  }

  async function downloadPicture(picture: IPicture): Promise<void> {
    const result = await downloadPictureFile(api, picture.mediaUri);
    if (!result.success) {
      AppNotification.display(result.errorMessage ?? $t`Unable to download the picture`, {type: 'error'});
    }
  }

  // The server rejects files above its size limit; the advice differs by format.
  function tooBigMessage(file: File): string {
    return isLosslessImage(file)
      ? $t`This picture is too large to upload. Try reducing the resolution and uploading again.`
      : $t`This picture is too large to upload. Try saving it at a lower JPEG quality and uploading again.`;
  }

  async function takePicture() {
    busyAction = 'add';
    try {
      let result = await platformFeatures.service.captureImage();
      if (result == null) {
        return;
      }
      let file = await convertPicture(
        new File([await result.image.arrayBuffer()], result.fileName, {type: result.contentType}),
      );
      openCreate(file);
    } catch {
      // convertPicture already shows a notification on failure
    } finally {
      busyAction = null;
    }
  }

  async function convertPicture(file: File): Promise<File> {
    if (isSupportedImageType(file)) {
      return file;
    }
    console.debug('Captured image is not a supported type, converting to JPG...', file);
    try {
      const bitmap = await createImageBitmap(file);
      const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
      const ctx = canvas.getContext('2d');
      if (!ctx) throw new Error('Unable to get canvas context');
      ctx.drawImage(bitmap, 0, 0);
      bitmap.close();
      const blob = await canvas.convertToBlob({type: 'image/jpeg', quality: 0.9});
      const newFile = new File([blob], file.name.replace(/\.[^.]+$/, '.jpg'), {type: 'image/jpeg'});
      console.debug('Converted captured image to JPG:', newFile);
      return newFile;
    } catch (err) {
      console.error('Error converting captured image to JPG:', err);
      AppNotification.display($t`Unable to convert captured image from ${file.type || file.name || 'unknown'} to a jpeg`, {type: 'error'});
      throw err;
    }
  }
</script>

<div class="flex flex-col gap-2">
  {#if pictures.length > 0}
    <!-- Pictures flow left-to-right and wrap; on a narrow (mobile) screen they stack vertically -->
    <div class="flex flex-wrap gap-4">
      {#each pictures as picture (picture.id)}
        <PictureImage
          {picture}
          {readonly}
          busy={busyAction !== null}
          onView={() => openViewer(picture)}
          onEdit={() => openEditor(picture)}
          onDownload={() => void downloadPicture(picture)}
          onDelete={() => void deletePicture(picture.id)}
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
      <Button
        icon="i-mdi-plus"
        size="xs"
        loading={busyAction === 'add'}
        disabled={busyAction !== null}
        onclick={() => fileInputElement?.click()}
      >
        {$t`Picture`}
      </Button>
      {#if platformFeatures.features.supportsImageCapture}
        <Button
          icon="i-mdi-camera"
          size="xs"
          loading={busyAction === 'add'}
          disabled={busyAction !== null}
          onclick={() => takePicture()}
        >
          {$t`Camera`}
        </Button>
      {/if}
    </div>
    <!-- Hidden input drives the OS file picker for adding a picture -->
    <input
      bind:this={fileInputElement}
      type="file"
      accept={ACCEPTED_PICTURE_TYPES}
      onchange={onFileSelected}
      class="hidden"
    />
  {/if}
</div>

{#if lastDialogPicture}
  <EditPictureDialog
    bind:open={editDialogOpen}
    picture={dialogPicture ?? lastDialogPicture}
    isNew={!!draftPicture}
    onUploadReplacement={(file) => (draftPicture ? Promise.resolve(replaceDraftFile(file)) : uploadReplacement(file))}
    onSubmit={(after) => (draftPicture ? submitNewPicture(after) : (void submitEdits(after), true))}
    onDelete={() => (draftPicture ? Promise.resolve() : deleteEditingPicture())}
  />
{/if}

<PictureViewerDialog
  bind:open={viewerOpen}
  bind:pictureId={viewerPictureId}
  {pictures}
  {readonly}
  busy={busyAction !== null}
  onEdit={(picture) => {
    viewerOpen = false;
    openEditor(picture);
  }}
  onDownload={(picture) => void downloadPicture(picture)}
  onDelete={(picture) => void deletePicture(picture.id)}
/>
