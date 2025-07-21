<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useDialogsService} from '$lib/services/dialogs-service.js';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';
  import {delay} from '$lib/utils/time';
  import AudioProvider from './audio-provider.svelte';
  import AudioEditor from './audio-editor.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
  import {AppNotification} from '$lib/notifications/notifications';

  let open = $state(false);
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'audio-dialog'});
  const dialogsService = useDialogsService();
  dialogsService.invokeAudioDialog = getAudio;
  const lexboxApi = useLexboxApi();

  let submitting = $state(false);
  let selectedFile = $state<File>();
  let audio = $state<Blob>();
  const tooBig = $derived((audio?.size ?? 0) > 10 * 1034 * 1024);

  let requester: {
    resolve: (mediaUri: string | undefined) => void
  } | undefined;


  async function getAudio() {
    reset();
    return new Promise<string | undefined>((resolve) => {
      requester = {resolve};
      open = true;
    });
  }

  watch(() => open, () => {
    if (!open) reset();
  });

  function close() {
    open = false;
    reset();
  }

  function reset() {
    requester?.resolve(undefined);
    requester = undefined;
    clearAudio();
  }

  function clearAudio() {
    audio = selectedFile = undefined;
    submitting = false;
  }

  async function submitAudio() {
    if (!audio) throw new Error('No audio to upload');
    if (!requester) throw new Error('No requester');

    submitting = true;
    try {
      const audioId = await uploadAudio();
      requester.resolve(audioId);
      close();
    } finally {
      submitting = false;
    }
  }

  async function uploadAudio() {
    if (!audio || !selectedFile) throw new Error($t`No file selected`);
    const response = await lexboxApi.saveFile(audio, {filename: selectedFile.name, mimeType: audio.type});
    switch (response.result) {
      case UploadFileResult.SavedLocally:
        AppNotification.display($t`Audio saved locally`, 'success');
        break;
      case UploadFileResult.SavedToLexbox:
        AppNotification.display($t`Audio saved and uploaded to Lexbox`, 'success');
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

    return response.mediaUri;
  }

  async function onFileSelected(file: File) {
    selectedFile = file;
    audio = await processAudio(file);
  }

  async function onRecordingComplete(blob: Blob) {
    let fileExt = mimeTypeToFileExtension(blob.type);
    selectedFile = new File([blob], `recording-${Date.now()}.${fileExt}`, {type: blob.type});
    if (!open) return;
    audio = await processAudio(blob);
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
    audio = undefined;
    selectedFile = undefined;
  }

  let loading = $state(false);
  async function processAudio(blob: Blob): Promise<Blob> {
    loading = true;
    await delay(1000); // Simulate processing delay
    loading = false;
    return blob;
  }
</script>


<Dialog.Root bind:open>
  <Dialog.DialogContent class="grid-rows-[auto_1fr_auto]">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Add audio`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if !audio || !selectedFile}
      {#if loading}
        <Loading class="self-center justify-self-center size-16"/>
      {:else}
        <AudioProvider {onFileSelected} {onRecordingComplete}/>
      {/if}
    {:else}
      <AudioEditor {audio} name={selectedFile.name} onDiscard={onDiscard}/>
      {#if tooBig}
        <p class="text-destructive text-lg text-end">{$t`File too big`}</p>
      {/if}
      <Dialog.DialogFooter>
        <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
        <Button onclick={() => submitAudio()} disabled={tooBig} loading={submitting}>
          {$t`Save audio`}
        </Button>
      </Dialog.DialogFooter>
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>

