<script lang="ts">
  import type {IPicture, IRichMultiString} from '$lib/dotnet-types';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Editor from '$lib/components/editor';
  import {RichMultiWsInput} from '$lib/components/field-editors';
  import {Button} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import {ACCEPTED_PICTURE_TYPES} from './picture-formats';
  import {t} from 'svelte-i18n-lingui';
  import {useWritingSystemService} from '$project/data';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';

  type Props = {
    open: boolean;
    picture: IPicture;
    /** Disables Replace/Delete while an operation is in flight. */
    busy?: boolean;
    onReplace: (file: File) => void;
    onDelete: () => void;
    onCaptionChange: (caption: IRichMultiString) => void;
  };
  let {open = $bindable(false), picture, busy = false, onReplace, onDelete, onCaptionChange}: Props = $props();

  useBackHandler({addToStack: () => open, onBack: () => (open = false), key: 'edit-picture-dialog'});
  const writingSystemService = useWritingSystemService();

  // A local, editable copy of the caption. Editing round-trips through the API, which reloads the
  // entry; keeping the input's value local means that reload can't reset it mid-edit. Re-seeded
  // each time the dialog opens so it reflects the picture being edited.
  let caption = $state<IRichMultiString>({});
  watch(
    () => open,
    () => {
      if (open) caption = structuredClone($state.snapshot(picture.caption ?? {}));
    },
  );

  let fileInputElement = $state<HTMLInputElement>();
  function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset so picking the same file again re-triggers `change`.
    target.value = '';
    if (file) onReplace(file);
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent onOpenAutoFocus={(e) => e.preventDefault()}>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Edit Picture`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>

    <!-- Picture at the top, centered in the dialog. -->
    <div class="flex justify-center">
      <PictureImage {picture} showCaption={false} />
    </div>

    <!-- Caption editor. Wrapped in the editor grid so RichMultiWsInput's subgrid rows lay out. -->
    <Editor.Root>
      <Editor.Grid>
        <Editor.Field.Root>
          <Editor.Field.Title name={$t`Caption`} />
          <Editor.Field.Body subGrid>
            <RichMultiWsInput
              bind:value={caption}
              writingSystems={writingSystemService.allWritingSystems()}
              onchange={() => onCaptionChange($state.snapshot(caption))}
            />
          </Editor.Field.Body>
        </Editor.Field.Root>
      </Editor.Grid>
    </Editor.Root>

    <Dialog.DialogFooter>
      <Button icon="i-mdi-image-refresh" variant="secondary" disabled={busy} onclick={() => fileInputElement?.click()}>
        {$t`Replace Picture`}
      </Button>
      <Button icon="i-mdi-delete" variant="destructive" disabled={busy} onclick={() => onDelete()}>
        {$t`Delete Picture`}
      </Button>
    </Dialog.DialogFooter>

    <input bind:this={fileInputElement} type="file" accept={ACCEPTED_PICTURE_TYPES} onchange={onFileSelected} class="hidden" />
  </Dialog.DialogContent>
</Dialog.Root>
