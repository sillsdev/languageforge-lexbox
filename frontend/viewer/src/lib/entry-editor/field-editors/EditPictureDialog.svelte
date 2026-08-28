<script lang="ts">
  import type {IPicture, IRichMultiString} from '$lib/dotnet-types';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Editor from '$lib/components/editor';
  import {RichMultiWsInput} from '$lib/components/field-editors';
  import {Button} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import {ACCEPTED_PICTURE_TYPES} from './picture-formats';
  import {downloadPictureFile} from './picture-actions';
  import {t} from 'svelte-i18n-lingui';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useWritingSystemService} from '$project/data';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';

  type Props = {
    open: boolean;
    picture: IPicture;
    /** When true the dialog is adding a not-yet-saved picture: title becomes "Add Picture" and the
        Download/Delete buttons are hidden. */
    isNew?: boolean;
    /** Uploads a replacement file and returns its mediaUri (or null if rejected). Does NOT touch
        the model — the new image is only previewed until Submit. */
    onUploadReplacement: (file: File) => Promise<string | null>;
    /** Applies the buffered edits (caption + replaced image). Returns whether the submit succeeded,
        i.e. whether the dialog may close (a new-picture upload can fail and must stay open). */
    onSubmit: (after: IPicture) => boolean | Promise<boolean>;
    /** Deletes the picture immediately (has its own confirmation); independent of Submit. Returns a
        promise that resolves once the delete — or its cancellation — has settled. */
    onDelete: () => Promise<void>;
  };
  let {open = $bindable(false), picture, isNew = false, onUploadReplacement, onSubmit, onDelete}: Props = $props();

  useBackHandler({addToStack: () => open, onBack: () => (open = false), key: 'edit-picture-dialog'});
  const writingSystemService = useWritingSystemService();
  const api = useLexboxApi();

  // Buffered, local edits. Nothing here reaches the model until Submit; Cancel just closes and the
  // next open re-seeds these from the picture, discarding whatever was typed/replaced.
  let caption = $state<IRichMultiString>({});
  let mediaUri = $state('');
  watch(
    () => open,
    () => {
      if (!open) return;
      caption = structuredClone($state.snapshot(picture.caption ?? {}));
      mediaUri = picture.mediaUri;
    },
  );

  // Preview reflects the buffered image (updates when a replacement is chosen, before Submit).
  const preview = $derived<IPicture>({...picture, caption, mediaUri});

  let uploading = $state(false);

  // Guards the delete flow against re-entry: a fast double-click could otherwise fire onDelete twice
  // before the confirmation prompt's modal overlay blocks the button, causing a duplicate prompt /
  // concurrent delete. Held until onDelete (delete or its cancellation) resolves.
  let deleting = $state(false);
  async function deletePicture() {
    if (deleting) return;
    deleting = true;
    try {
      await onDelete();
    } finally {
      deleting = false;
    }
  }

  let downloading = $state(false);
  async function downloadPicture() {
    if (downloading) return;
    downloading = true;
    try {
      const result = await downloadPictureFile(api, mediaUri);
      if (!result.success) {
        AppNotification.display(result.errorMessage ?? $t`Unable to download the picture`, {type: 'error'});
      }
    } finally {
      downloading = false;
    }
  }

  let fileInputElement = $state<HTMLInputElement>();
  async function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset so picking the same file again re-triggers `change`.
    target.value = '';
    if (!file) return;
    uploading = true;
    try {
      const uri = await onUploadReplacement(file);
      if (uri) mediaUri = uri;
    } finally {
      uploading = false;
    }
  }

  let submitting = $state(false);
  async function submit() {
    if (submitting) return;
    submitting = true;
    try {
      const ok = await onSubmit({...$state.snapshot(picture), caption: $state.snapshot(caption), mediaUri});
      if (ok) open = false;
    } finally {
      submitting = false;
    }
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent onOpenAutoFocus={(e) => e.preventDefault()}>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{isNew ? $t`Add Picture` : $t`Edit Picture`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>

    <!-- Picture at the top, centered in the dialog (shows the buffered replacement if any). -->
    <div class="flex justify-center">
      <PictureImage picture={preview} showCaption={false} />
    </div>

    <!-- Caption editor. Wrapped in the editor grid so RichMultiWsInput's subgrid rows lay out. -->
    <Editor.Root>
      <Editor.Grid>
        <Editor.Field.Root>
          <Editor.Field.Title name={$t`Caption`} />
          <Editor.Field.Body subGrid>
            <RichMultiWsInput bind:value={caption} writingSystems={writingSystemService.uniqueWritingSystems()} />
          </Editor.Field.Body>
        </Editor.Field.Root>
      </Editor.Grid>
    </Editor.Root>

    <Dialog.DialogFooter>
      {#if !isNew}
        <Button icon="i-mdi-download" variant="secondary" loading={downloading} disabled={uploading || downloading} onclick={() => downloadPicture()}>
          {$t`Download Picture`}
        </Button>
      {/if}
      <Button icon="i-mdi-image-refresh" variant="secondary" loading={uploading} disabled={uploading || deleting || submitting} onclick={() => fileInputElement?.click()}>
        {$t`Replace Picture`}
      </Button>
      {#if !isNew}
        <Button icon="i-mdi-delete" variant="destructive" loading={deleting} disabled={uploading || deleting} onclick={() => deletePicture()}>
          {$t`Delete Picture`}
        </Button>
      {/if}
      <Button variant="secondary" disabled={submitting} onclick={() => (open = false)}>
        {$t`Cancel`}
      </Button>
      <Button loading={submitting} disabled={uploading || deleting || submitting} onclick={() => submit()}>
        {$t`Submit`}
      </Button>
    </Dialog.DialogFooter>

    <input bind:this={fileInputElement} type="file" accept={ACCEPTED_PICTURE_TYPES} onchange={onFileSelected} class="hidden" />
  </Dialog.DialogContent>
</Dialog.Root>
