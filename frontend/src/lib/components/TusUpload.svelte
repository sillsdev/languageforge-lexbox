<script lang="ts">
    import {Upload} from 'tus-js-client'
    import Button from '$lib/forms/Button.svelte';
    import {env} from '$env/dynamic/public';
    import FormField from '$lib/forms/FormField.svelte';
    import {createEventDispatcher} from 'svelte';
  import FormError from '$lib/forms/FormError.svelte';

    enum UploadStatus {
        NoFile,
        Ready,
        Error,
        Uploading,
        Paused,
        Complete
    }

    export let endpoint: string;
    export let accept: string;
    const dispatch = createEventDispatcher<{
        uploadComplete: { upload: Upload }
    }>();

    let status = UploadStatus.NoFile;
    let percent = 0;
    let error: string | undefined = undefined;
    let fileError: string | undefined = undefined;
    let upload: Upload | undefined;
    const maxUploadChunkSizeMb = parseInt(env.PUBLIC_TUS_CHUNK_SIZE_MB);

    function fileSelected(e: Event): void {
      error = fileError = upload = undefined;
      status = UploadStatus.NoFile;
        let inputElement = e.target as HTMLInputElement;
        if (!inputElement.files?.length) return;
        let file = inputElement.files[0];
        console.log(file);
        if (!file.name.endsWith('.zip')) {
          fileError = `Only .zip files are allowed`;
          return;
        }
        upload = new Upload(file, {
            chunkSize: maxUploadChunkSizeMb * 1024 * 1024,
            endpoint,
            metadata: {
                filetype: file.type
            },
            uploadDataDuringCreation: true,
            onProgress: (bytesUploaded, bytesTotal) => {
                percent = bytesTotal > 0 ? bytesUploaded / bytesTotal * 100 : 0;
            },
            onSuccess: () => {
                status = UploadStatus.Complete;
                percent = 100;
                if (upload)
                    dispatch('uploadComplete', {upload});
            },
            onError: (err) => {
                status = UploadStatus.Error;
                error = err.message;
            }
        });
        percent = 0;
        status = UploadStatus.Ready;
    }

    async function startUpload(): Promise<void> {
        if (!upload) {
          fileError = 'Please choose a file';
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
    <FormField label="Project zip file" id="test-upload" error={fileError}>
        <input id="test-upload"
              type="file"
              {accept}
              class="file-input file-input-bordered file-input-primary"
              on:cancel|stopPropagation
              on:change={fileSelected} />
    </FormField>
    <FormError {error} />
  </form>
  <p>Status: {UploadStatus[status]}</p>
  <progress class="progress progress-success" class:progress-error={error} value={percent} max="100"></progress>
</div>

<div class="mt-6">
  <Button style="btn-success" disabled={status > UploadStatus.Ready} on:click={startUpload}>Upload project</Button>
</div>
