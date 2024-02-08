<script context="module" lang="ts">
  export enum UploadStatus {
    NoFile = 'NoFile',
    InvalidFile = 'InvalidFile',
    Ready = 'Ready',
    Error = 'Error',
    Uploading = 'Uploading',
    Complete = 'Complete',
  }
</script>

<script lang="ts">
  import { Upload, type DetailedError } from 'tus-js-client';
  import { Button, FormError, FormField } from '$lib/forms';
  import { env } from '$env/dynamic/public';
  import { createEventDispatcher, onDestroy, onMount } from 'svelte';
  import t from '$lib/i18n';
  import IconButton from './IconButton.svelte';

  export let endpoint: string;
  export let accept: string;
  export let inputLabel: string = $t('tus.select_file');
  export let inputDescription: string | undefined = undefined;
  export let internalButton = false;
  const dispatch = createEventDispatcher<{
    uploadComplete: { upload: Upload };
    status: UploadStatus;
  }>();

  let status = UploadStatus.NoFile;
  $: dispatch('status', status);

  let percent = 0;
  let fileError: string | undefined = undefined;
  let uploadError: string | undefined = undefined;
  let upload: Upload | undefined;
  let fileInput: HTMLInputElement | undefined;
  const maxUploadChunkSizeMb = parseInt(env.PUBLIC_TUS_CHUNK_SIZE_MB);

  function fileChanged(): void {
    uploadError = fileError = upload = undefined;
    status = UploadStatus.NoFile;
    const file = fileInput?.files?.[0];
    if (!file) return;
    console.log(file);
    if (accept === 'application/zip' && !file.name.endsWith('.zip')) {
      status = UploadStatus.InvalidFile;
      fileError = $t('tus.zip_only');
      return;
    }
    upload = new Upload(file, {
      chunkSize: maxUploadChunkSizeMb * 1024 * 1024,
      endpoint,
      metadata: {
        filetype: file.type,
      },
      uploadDataDuringCreation: false,
      onProgress: (bytesUploaded, bytesTotal) => {
        percent = bytesTotal > 0 ? (bytesUploaded / bytesTotal) * 100 : 0;
      },
      onSuccess: () => {
        status = UploadStatus.Complete;
        percent = 100;
        if (upload) dispatch('uploadComplete', { upload });
      },
      onError: (err) => {
        status = UploadStatus.Error;
        const errorCode = getErrorCode(err);
        if (errorCode !== 'unknown') {
          uploadError = $t('tus.server_error_codes.' + errorCode);
          return;
        }
        uploadError = err.message;
      },
      onShouldRetry: (err) => {
        //probably won't do anything as this is what the library does already, more to make ts happy and to avoid breaking if the library changes it's implementation
        if (!('originalResponse' in err)) {
          return false;
        }
        const errorCode = getErrorCode(err);
        if (errorCode != 'unknown') return false;

        const status = err.originalResponse ? err.originalResponse.getStatus() : 0;
        return navigator.onLine && (status === 409 || status == 423 || status < 400 || 499 < status);
      },
    });
    percent = 0;
    status = UploadStatus.Ready;
  }

  function clearFile(): void {
    fileInput!.value = '';
    fileChanged();
  }

  function getErrorCode(err: DetailedError | Error): string {
    if (!('originalResponse' in err)) {
      return 'unknown';
    }
    if (err.originalResponse?.getStatus() != 500) {
      return 'unknown';
    }
    const jsonString = err.originalResponse.getBody();
    try {
      const json = JSON.parse(jsonString);
      return json?.['app-error-code'] || 'unknown';
    } catch {
      return 'unknown';
    }
  }

  onMount(() => {
    // make sure listeners are ready
    dispatch('status', status);
  });

  //svelte on on mount
  onDestroy(() => {
    if (upload) {
      void upload.abort(false);
    }
  });

  export async function startUpload(): Promise<void> {
    if (fileError) return;
    if (!upload) {
      fileError = $t('tus.no_file_selected');
      return;
    }
    const previousUploads = await upload.findPreviousUploads();
    if (previousUploads.length > 0) {
      upload.resumeFromPreviousUpload(previousUploads[0]);
    }
    upload.start();
    status = UploadStatus.Uploading;
  }
</script>

<div class="space-y-4">
  <form>
    <FormField label={inputLabel} id="tus-upload" error={fileError} description={inputDescription}>
      <div class="flex gap-2">
        <input
          id="tus-upload"
          type="file"
          {accept}
          class="file-input file-input-bordered file-input-primary grow"
          bind:this={fileInput}
          on:cancel|stopPropagation
          on:change={fileChanged}
        />
        <IconButton icon="i-mdi-close"
          on:click={clearFile}
          disabled={status !== UploadStatus.Ready && status !== UploadStatus.InvalidFile} />
      </div>
    </FormField>
    <FormError error={uploadError} />
  </form>
</div>

<div class="mt-6 flex items-center gap-6">
  {#if internalButton}
    <Button style="btn-success" disabled={status > UploadStatus.Ready} on:click={startUpload}>{$t('tus.upload')}</Button>
  {/if}
  <div class="flex-1">
    <p class="label label-text py-0">{$t('tus.upload_progress')}</p>
    <progress class="progress progress-success" class:progress-error={uploadError} value={percent} max="100" />
  </div>
</div>
