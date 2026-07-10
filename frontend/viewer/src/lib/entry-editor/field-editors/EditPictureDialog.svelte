<script lang="ts">
  import type {IPicture, IRichMultiString} from '$lib/dotnet-types';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Editor from '$lib/components/editor';
  import {RichMultiWsInput} from '$lib/components/field-editors';
  import {Button} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import {ACCEPTED_PICTURE_TYPES} from './picture-formats';
  import {t} from 'svelte-i18n-lingui';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useWritingSystemService} from '$project/data';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';

  type Props = {
    open: boolean;
    picture: IPicture;
    /** Uploads a replacement file and returns its mediaUri (or null if rejected). Does NOT touch
        the model — the new image is only previewed until Submit. */
    onUploadReplacement: (file: File) => Promise<string | null>;
    /** Applies the buffered edits (caption + replaced image) to the model. */
    onSubmit: (after: IPicture) => void;
    /** Deletes the picture immediately (has its own confirmation); independent of Submit. Returns a
        promise that resolves once the delete — or its cancellation — has settled. */
    onDelete: () => Promise<void>;
  };
  let {open = $bindable(false), picture, onUploadReplacement, onSubmit, onDelete}: Props = $props();

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

  // Downloads the currently-shown image, saved under the filename the media server reports for it.
  let downloading = $state(false);
  async function downloadPicture() {
    if (downloading) return;
    downloading = true;
    try {
      const file = await api.getFileStream(mediaUri);
      if (!file.stream) {
        AppNotification.display(file.errorMessage ?? $t`Unable to download the picture`, {type: 'error'});
        return;
      }
      const blob = await new Response(await file.stream.stream()).blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = file.fileName ?? 'picture';
      anchor.click();
      // Release the object URL on the next tick, once the browser has captured the blob.
      setTimeout(() => URL.revokeObjectURL(url), 0);
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

  function submit() {
    onSubmit({...$state.snapshot(picture), caption: $state.snapshot(caption), mediaUri});
    open = false;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent onOpenAutoFocus={(e) => e.preventDefault()}>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Edit Picture`}</Dialog.DialogTitle>
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
            <RichMultiWsInput bind:value={caption} writingSystems={writingSystemService.allWritingSystems()} />
          </Editor.Field.Body>
        </Editor.Field.Root>
      </Editor.Grid>
    </Editor.Root>

    <Dialog.DialogFooter>
      <Button icon="i-mdi-download" variant="secondary" loading={downloading} disabled={uploading || downloading} onclick={() => downloadPicture()}>
        {$t`Download Picture`}
      </Button>
      <Button icon="i-mdi-image-refresh" variant="secondary" loading={uploading} disabled={uploading || deleting} onclick={() => fileInputElement?.click()}>
        {$t`Replace Picture`}
      </Button>
      <Button icon="i-mdi-delete" variant="destructive" loading={deleting} disabled={uploading || deleting} onclick={() => deletePicture()}>
        {$t`Delete Picture`}
      </Button>
      <Button variant="secondary" onclick={() => (open = false)}>
        {$t`Cancel`}
      </Button>
      <Button disabled={uploading || deleting} onclick={() => submit()}>
        {$t`Submit`}
      </Button>
    </Dialog.DialogFooter>

    <input bind:this={fileInputElement} type="file" accept={ACCEPTED_PICTURE_TYPES} onchange={onFileSelected} class="hidden" />
  </Dialog.DialogContent>
</Dialog.Root>
