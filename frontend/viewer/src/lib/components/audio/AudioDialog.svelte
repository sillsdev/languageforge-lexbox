<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';
  import AudioProvider from './audio-provider.svelte';
  import AudioEditor from './audio-editor.svelte';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
  import {AppNotification} from '$lib/notifications/notifications';
  import type {Snippet} from 'svelte';
  import {cn} from '$lib/utils';

  let {
    open = $bindable(false),
    title = undefined,
    onSubmit = () => {},
    children = undefined,
  }: {
    open: boolean;
    title?: string;
    onSubmit?: (audioId: string) => void;
    children?: Snippet;
  } = $props();
  useBackHandler({addToStack: () => open, onBack: () => (open = false), key: 'audio-dialog'});
  const lexboxApi = useLexboxApi();

  let submitting = $state(false);
  let selectedFile = $state<File>();
  let finalAudio = $state<File>();
  const tooBig = $derived((finalAudio?.size ?? 0) > 10 * 1024 * 1024);

  watch(
    () => open,
    () => {
      if (!open) reset();
    },
  );

  watch(
    () => selectedFile,
    () => {
      if (!selectedFile) finalAudio = undefined;
    },
  );

  function close() {
    open = false;
    reset();
  }

  function reset() {
    clearAudio();
  }

  function clearAudio() {
    selectedFile = undefined;
    submitting = false;
  }

  async function submitAudio() {
    if (!selectedFile) throw new Error('No audio to upload');

    submitting = true;
    try {
      const audioId = await uploadAudio();
      onSubmit(audioId);
      close();
    } finally {
      submitting = false;
    }
  }

  async function uploadAudio() {
    if (!finalAudio) throw new Error($t`No file to upload`);
    const response = await lexboxApi.saveFile(finalAudio, {filename: finalAudio.name, mimeType: finalAudio.type});
    switch (response.result) {
      case UploadFileResult.SavedLocally:
        AppNotification.display($t`Audio saved locally`, {type: 'success', timeout: 'short'});
        break;
      case UploadFileResult.SavedToLexbox:
        AppNotification.display($t`Audio saved and uploaded to Lexbox`, {type: 'success', timeout: 'short'});
        break;
      case UploadFileResult.TooBig:
        throw new Error($t`File too big`);
      case UploadFileResult.NotSupported:
        throw new Error($t`File saving not supported`);
      case UploadFileResult.AlreadyExists:
        throw new Error($t`File already exists`);
      case UploadFileResult.Error:
        throw new Error(response.errorMessage ?? $t`Unknown error`);
    }
    if (!response.mediaUri) throw new Error('No mediaUri returned');

    return response.mediaUri;
  }

  function onFileSelected(file: File) {
    selectedFile = file;
  }

  function onRecordingComplete(blob: Blob) {
    let fileExt = mimeTypeToFileExtension(blob.type);
    selectedFile = new File([blob], `recording-${Date.now()}.${fileExt}`, {type: blob.type});
  }

  function mimeTypeToFileExtension(mimeType: string) {
    if (mimeType.startsWith('audio/')) {
      const baseType = mimeType.split(';')[0];
      switch (baseType) {
        case 'audio/mpeg':
        case 'audio/mp3':
          return 'mp3';
        case 'audio/wav':
        case 'audio/wave':
        case 'audio/x-wav':
          return 'wav';
        case 'audio/ogg':
          return 'ogg';
        case 'audio/webm':
          return 'webm';
        case 'audio/aac':
          return 'aac';
        case 'audio/m4a':
          return 'm4a';
        default:
          return 'audio';
      }
    }
    return 'bin';
  }

  function onDiscard() {
    selectedFile = undefined;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent
    onOpenAutoFocus={(e) => e.preventDefault()}
    class={cn('sm:min-h-[min(calc(100%-16px),30rem)]', children ? 'grid-rows-[auto_auto_1fr]' : 'grid-rows-[auto_1fr]')}
  >
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{title || $t`Add audio`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if children}
      <!-- Ensure children only occupy 1 grid row -->
      <div>{@render children?.()}</div>
    {/if}
    {#if !selectedFile}
      <AudioProvider {onFileSelected} {onRecordingComplete} />
    {:else}
      <AudioEditor audio={selectedFile} bind:finalAudio {onDiscard} />
      {#if tooBig}
        <p class="text-destructive text-lg text-end">{$t`File too big`}</p>
      {/if}
      <Dialog.DialogFooter>
        <Button onclick={() => (open = false)} variant="secondary">{$t`Cancel`}</Button>
        <Button onclick={() => submitAudio()} disabled={tooBig || !finalAudio} loading={submitting}>
          {$t`Save audio`}
        </Button>
      </Dialog.DialogFooter>
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>
