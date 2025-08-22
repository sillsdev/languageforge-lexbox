<script lang="ts" module>
  export const MEDIA_FILES_DIALOG_QUERY_PARAM = 'mediaFilesDialogOpen';
</script>

<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon, PingingIcon } from '$lib/components/ui/icon';
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { t, plural } from 'svelte-i18n-lingui';
  import { AppNotification } from '$lib/notifications/notifications';
  import { QueryParamStateBool } from '$lib/utils/url.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import { formatDate } from '$lib/components/ui/format';
  import { watch } from 'runed';
  import { fade } from 'svelte/transition';
  import { delay } from '$lib/utils/time';
  import { cn } from '$lib/utils';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {useProjectContext} from '$lib/project-context.svelte';
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import {ProjectSyncStatusErrorCode} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusErrorCode';
  import {useMediaFilesService} from '$lib/services/media-files-service';
  import type {IRemoteResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/IRemoteResource';
  import type {ILocalResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/ILocalResource';
  import {Checkbox, CheckboxGroup} from '$lib/components/ui/checkbox';

  const {
    syncStatus = SyncStatus.Success
  }: {syncStatus?: SyncStatus} = $props();

  const projectContext = useProjectContext();
  const service = useMediaFilesService();
  const features = useFeatures();
  let remoteFiles = $state<IRemoteResource[]>([]);
  let localFiles = $state<ILocalResource[]>([]);
  let pendingUploadCount = $derived(localFiles?.length ?? 0);
  let pendingDownloadCount = $derived(remoteFiles?.length ?? 0);
  let server = $derived(projectContext.server);
  let loading = $state(false);
  const openQueryParam = new QueryParamStateBool(
    { key: MEDIA_FILES_DIALOG_QUERY_PARAM, replaceOnDefaultValue: true, allowBack: true },
    false,
  );

  let selectedFilesToDownload = $state<string[]>([]);
  let selectedFilesToUpload = $state<string[]>([]);

  const serverName = $derived(server?.displayName ?? projectContext.projectData?.serverId ?? 'unknown');

  watch(() => openQueryParam.current, (newValue) => {
    if (newValue) void onOpen();
    else setTimeout(onClose, 500); // don't clear contents until close animation is done
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  async function onOpen(): Promise<void> {
    loading = true;
    try {
      let remotePromise = service.resourcesPendingDownload();
      let localPromise = service.resourcesPendingUpload();
      [localFiles, remoteFiles] = await Promise.all([
        localPromise,
        remotePromise,
      ]);
    } finally {
      loading = false;
    }
  }

  function onClose(): void {
    localFiles = [];
    remoteFiles = [];
  }

  let loadingDownload = $state(false);
  function downloadAll() {
    try {
      const downloadPromise = service.downloadAllResources().then(onOpen);
      const count = pendingDownloadCount; // Break reactivity before we set pending count to 0
      AppNotification.promise(downloadPromise, {
        loading: $t`Downloading files from remote...`,
        success: $t`${count} files downloaded.`,
        error: (error) => $t`Failed to download files.` + '\n' + (error as Error).message,
      });
    } finally {
      loadingDownload = false;
    }
  }
  let loadingUpload = $state(false);
  function uploadAll() {
    try {
      const uploadPromise = service.uploadAllResources().then(onOpen);
      const count = pendingUploadCount; // Break reactivity before we set pending count to 0
      AppNotification.promise(uploadPromise, {
        loading: $t`Uploading files to remote...`,
        success: $t`${count} files uploaded.`,
        error: (error) => $t`Failed to upload files.` + '\n' + (error as Error).message,
      });
    } finally {
      loadingUpload = false;
    }
  }
  function downloadSelected() {
    try {
      const downloadPromise = service.downloadResources(selectedFilesToDownload).then(onOpen);
      const count = selectedFilesToDownload.length; // Break reactivity before we set selected count to 0
      AppNotification.promise(downloadPromise, {
        loading: $t`Downloading files from remote...`,
        success: $t`${count} files downloaded.`,
        error: (error) => $t`Failed to download files.` + '\n' + (error as Error).message,
      });
    } finally {
      selectedFilesToDownload = [];
      loadingDownload = false;
    }
  }
  function uploadSelected() {
    try {
      const uploadPromise = service.uploadResources(selectedFilesToUpload).then(onOpen);
      const count = selectedFilesToUpload.length; // Break reactivity before we set selected count to 0
      AppNotification.promise(uploadPromise, {
        loading: $t`Uploading files to remote...`,
        success: $t`${count} files uploaded.`,
        error: (error) => $t`Failed to upload files.` + '\n' + (error as Error).message,
      });
    } finally {
      selectedFilesToUpload = [];
      loadingUpload = false;
    }
  }

  function fileTypeIcon(mimeType: string)
  {
    if (!mimeType) return 'i-mdi-file';
    if (mimeType.startsWith('audio')) return 'i-mdi-file-music';
    if (mimeType.startsWith('video')) return 'i-mdi-file-video';
    if (mimeType.startsWith('image')) return 'i-mdi-file-image';
    return 'i-mdi-file';
  }
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-80 sm:min-w-96 grid-rows-[auto_1fr] items-center">
    <DialogHeader>
      <DialogTitle>{$t`Download Files`}</DialogTitle>
    </DialogHeader>
    {#if loadingDownload || loadingUpload}
      <Loading class="place-self-center size-10" />
    {:else}
      <div in:fade
        class="grid grid-rows-[auto] grid-cols-[1fr_7fr_120px] gap-y-6 gap-x-8"
      >
      <!-- TODO: Make icon(s) pulse while downloading, perhaps show progress in notification... -->
      <!-- TODO: Detect not-logged-in status and provide login button similar to sync dialog -->
        <div class="col-span-1 text-center">
          <Icon icon="i-mdi-folder-download" class="size-10" />
        </div>
        <div class="text-center content-center">
          {pendingDownloadCount ?? '?'} files to download
        </div>
        <div class="content-center text-center">
          <Button onclick={downloadAll}>Download All</Button>
        </div>
        <div class="col-span-full text-left">
          <ul>
            <CheckboxGroup bind:value={selectedFilesToDownload} >
            {#each remoteFiles as file, idx (idx)}
            <li>
            {#await service.getFileMetadata(file.id)}
              ...
            {:then metadata}
              <Checkbox value={file.id} />
              <Icon icon={fileTypeIcon(metadata.mimeType)} /> {metadata.filename}
            {/await}
            </li>
            {/each}
            </CheckboxGroup>
          </ul>
        </div>
        {#if selectedFilesToDownload?.length}
        <div class="col-span-full text-center">
          <Button onclick={downloadSelected}>Download {selectedFilesToDownload.length} selected files</Button>
        </div>
        {/if}
        <div class="col-span-1 text-center">
          <Icon icon="i-mdi-folder-upload" class="size-10" />
        </div>
        <div class="text-center content-center">
          {pendingUploadCount ?? '?'} files to upload
        </div>
        <div class="content-center text-center">
          <Button onclick={uploadAll}>Upload all</Button>
        </div>
        <div class="col-span-full text-left">
          <ul>
            <CheckboxGroup bind:value={selectedFilesToUpload} >
            {#each localFiles as file, idx (idx)}
            <li>
            {#await service.getFileMetadata(file.id)}
              ...
            {:then metadata}
              <Checkbox value={file.id} />
              <Icon icon={fileTypeIcon(metadata.mimeType)} /> {metadata.filename}
            {/await}
            </li>
            {/each}
            </CheckboxGroup>
          </ul>
        </div>
        {#if selectedFilesToUpload?.length}
        <div class="col-span-full text-center">
          <Button onclick={uploadSelected}>Upload {selectedFilesToUpload.length} selected files</Button>
        </div>
        {/if}
      </div>
    {/if}
  </DialogContent>
</Dialog>
