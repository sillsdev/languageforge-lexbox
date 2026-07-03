<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
  import {Button} from '$lib/components/ui/button';
  import PictureCarousel from './PictureCarousel.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useLexboxApi} from '$lib/services/service-provider';
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

  // `pictures` is typed as required, but older/legacy sense data may omit it entirely,
  // so guard against undefined rather than trusting the type at runtime.
  const pictures = $derived(value ?? []);

  // Formats the browser accepts and that the server supports for pictures.
  const ACCEPTED_TYPES = 'image/jpeg,image/png';

  let fileInputElement = $state<HTMLInputElement>();
  let uploading = $state(false);

  function selectFile() {
    fileInputElement?.click();
  }

  function onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    // Reset the input so selecting the same file again re-triggers `change`.
    target.value = '';
    if (file) void uploadPicture(file);
  }

  async function uploadPicture(file: File): Promise<void> {
    uploading = true;
    try {
      // saveFile reports the outcome via `result` (not exceptions), so we branch on it.
      // We intentionally do NOT pre-check the file size: the size limit lives on the
      // server and may change, so we let the server decide and handle a `TooBig` result.
      const response = await api.saveFile(file, {filename: file.name, mimeType: file.type});
      switch (response.result) {
        case UploadFileResult.SavedLocally:
        case UploadFileResult.SavedToLexbox:
          break;
        case UploadFileResult.TooBig:
          AppNotification.display(tooBigMessage(file), {type: 'error', timeout: 'long'});
          return;
        case UploadFileResult.NotSupported:
          AppNotification.display($t`Uploading pictures is not supported here`, {type: 'error'});
          return;
        case UploadFileResult.AlreadyExists:
          AppNotification.display($t`That picture has already been uploaded`, {type: 'error'});
          return;
        case UploadFileResult.Error:
          AppNotification.display(response.errorMessage ?? $t`Unable to upload the picture`, {type: 'error'});
          return;
      }
      if (!response.mediaUri) throw new Error('saveFile succeeded but returned no mediaUri');

      const picture: IPicture = {
        id: randomId(),
        order: pictures.length,
        mediaUri: response.mediaUri,
        caption: {},
      };
      await api.createPicture(entryId, senseId, picture);
      // The created picture surfaces via the entry-changed event, which reloads the entry.
    } finally {
      uploading = false;
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

{#if pictures.length > 0}
  <PictureCarousel {pictures} />
{:else if !readonly}
  <Button icon="i-mdi-plus" size="xs" loading={uploading} onclick={selectFile}>
    {$t`Picture`}
  </Button>
  <!-- Hidden input drives the OS file picker; only JPG/PNG are offered. -->
  <input
    bind:this={fileInputElement}
    type="file"
    accept={ACCEPTED_TYPES}
    onchange={onFileSelected}
    class="hidden"
  />
{:else}
  <div class="text-muted-foreground p-1">
    {$t`No pictures`}
  </div>
{/if}
