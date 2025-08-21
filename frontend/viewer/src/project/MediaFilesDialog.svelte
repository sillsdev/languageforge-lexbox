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
  async function downloadAll() {
    try {
      const downloadPromise = service.downloadAllResources();
      const count = pendingDownloadCount; // Break reactivity before we set pending count to 0
      AppNotification.promise(downloadPromise, {
        loading: $t`Downloading files from remote...`,
        success: $t`${count} files downloaded.`,
        error: (error) => $t`Failed to download files.` + '\n' + (error as Error).message,
      });
    } finally {
      pendingDownloadCount = 0;
      loadingDownload = false;
    }
  }
  let loadingUpload = $state(false);
  async function uploadAll() {
    try {
      const uploadPromise = service.uploadAllResources();
      const count = pendingUploadCount; // Break reactivity before we set pending count to 0
      AppNotification.promise(uploadPromise, {
        loading: $t`Uploading files to remote...`,
        success: $t`${count} files uploaded.`,
        error: (error) => $t`Failed to upload files.` + '\n' + (error as Error).message,
      });
    } finally {
      pendingUploadCount = 0;
      loadingUpload = false;
    }
  }

  function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    if (status === 'logged-in') {
      onOpen();
    }
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
        class="grid grid-rows-[auto] grid-cols-[1fr_7fr_1fr] gap-y-6 gap-x-8"
      >
      <!-- TODO: Make icon(s) pulse while downloading, perhaps show progress in notification... -->
      <!-- TODO: Detect not-logged-in status and provide login button similar to sync dialog -->
        <div class="col-span-1 text-center">
          <Icon icon="i-mdi-folder" class="size-10" />
        </div>
        <div class="text-center content-center">
          {pendingDownloadCount ?? '?'} files to download
        </div>
        <div class="content-center text-center">
          <Button onclick={downloadAll}>DL</Button>
        </div>
        <div class="col-span-1 text-center">
          <Icon icon="i-mdi-folder" class="size-10" />
        </div>
        <div class="text-center content-center">
          {pendingUploadCount ?? '?'} files to upload
        </div>
        <div class="content-center text-center">
          <Button onclick={uploadAll}>UL</Button>
        </div>
      </div>
    {/if}
  </DialogContent>
</Dialog>
