<script lang="ts">
  import {Upload} from 'tus-js-client'
  import Button from '$lib/forms/Button.svelte';
  import {env} from '$env/dynamic/public';
  import FormField from '$lib/forms/FormField.svelte';

  enum UploadStatus {
    NO_FILE,
    READY,
    UPLOADING,
    PAUSED,
    COMPLETE,
    ERROR
  }

  let status = UploadStatus.NO_FILE;
  let percent = 0;
  let error: string = '';
  let upload: Upload | undefined;
  const maxUploadChunkSizeMb = parseInt(env.PUBLIC_TUS_CHUNK_SIZE_MB);

  async function fileSelected(e: Event) {
    let inputElement = e.target as HTMLInputElement;
    if (!inputElement.files) return;
    let file = inputElement.files[0];
    upload = new Upload(file, {
      chunkSize: maxUploadChunkSizeMb * 1024 * 1024,
      endpoint: '/api/tus-test',
      uploadDataDuringCreation: true,
      onProgress: (bytesUploaded, bytesTotal) => {
        percent = bytesUploaded / bytesTotal * 100;
      },
      onSuccess: () => {
        status = UploadStatus.COMPLETE;
        percent = 100;
      },
      onError: (err) => {
        status = UploadStatus.ERROR;
        error = err.message;
      }
    });
    percent = 0;
    status = UploadStatus.READY;
  }

  async function startUpload() {
    if (!upload) return;
    const previousUploads = await upload.findPreviousUploads();
    if (previousUploads.length > 0) {
      upload.resumeFromPreviousUpload(previousUploads[0]);
    }
    upload.start();
    status = UploadStatus.UPLOADING;
  }
</script>
<FormField label="Upload test file" id="test-upload">
  <input id="test-upload" type="file" class="file-input" on:change={fileSelected}/>
</FormField>
<p>Status: {UploadStatus[status]}</p>
{#if error}
  <p>Error: {error}</p>
{/if}
<progress class="progress progress-success" value={percent} max="100"></progress>

{#if status < UploadStatus.UPLOADING}
    <Button disabled={!upload} on:click={() => startUpload()}>Start</Button>
{/if}
