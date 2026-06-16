<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import * as Drawer from '$lib/components/ui/drawer';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import type {IHarmonyResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/IHarmonyResource';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {cn} from '$lib/utils';
  import {useProjectContext} from '$project/project-context.svelte';
  import {resource} from 'runed';
  import {SvelteSet} from 'svelte/reactivity';
  import {t} from 'svelte-i18n-lingui';
  import MediaFileDetail from './MediaFileDetail.svelte';


  const projectContext = useProjectContext();
  const mediaFilesService = $derived(projectContext.mediaFilesService);

  const mediaFiles = resource(
    () => mediaFilesService,
    async (service) => {
      if (!service) return [];
      return await service.allResources();
    },
    {initialValue: []},
  );

  type LocationStatus = 'local' | 'remote' | 'both';

  function displayName(resource: IHarmonyResource): string {
    if (resource.localPath) {
      return resource.localPath.split(/[/\\]/).pop() ?? resource.id;
    }
    return resource.id;
  }

  function locationStatus(resource: IHarmonyResource): LocationStatus {
    if (resource.local && resource.remote) return 'both';
    if (resource.local) return 'local';
    return 'remote';
  }

  const locationLabels: Record<LocationStatus, () => string> = {
    local: () => $t`Local only`,
    remote: () => $t`Remote only`,
    both: () => $t`Local and remote`,
  };

  const downloadableCount = $derived(
    mediaFiles.current.filter((file) => file.remote && !file.local).length,
  );

  let downloading = $state(false);
  let uploading = $state(false);
  let downloadingFileIds = new SvelteSet<string>();
  let selectedFileId = $state<string | undefined>();
  const selectedFile = $derived(
    selectedFileId ? mediaFiles.current.find((file) => file.id === selectedFileId) : undefined,
  );

  function selectFile(file: IHarmonyResource) {
    selectedFileId = file.id;
  }

  async function downloadFileForDetail(fileId: string) {
    await downloadFile(fileId);
  }

  async function downloadFile(fileId: string, event?: MouseEvent) {
    event?.stopPropagation();

    const service = mediaFilesService;
    if (!service || downloadingFileIds.has(fileId)) return;

    downloadingFileIds.add(fileId);
    try {
      await service.downloadResources([fileId]);
      await mediaFiles.refetch();
      AppNotification.display($t`Downloaded file`);
    } catch (error) {
      AppNotification.error($t`Failed to download file`, error instanceof Error ? error.message : String(error));
    } finally {
      downloadingFileIds.delete(fileId);
    }
  }

  async function downloadAll() {
    const service = mediaFilesService;
    if (!service || downloading) return;

    const resourceIds = mediaFiles.current
      .filter((file) => file.remote && !file.local)
      .map((file) => file.id);

    if (resourceIds.length === 0) return;

    downloading = true;
    try {
      await service.downloadResources(resourceIds);
      await mediaFiles.refetch();
      AppNotification.display($t`Downloaded ${resourceIds.length} file(s)`);
    } catch (error) {
      AppNotification.error($t`Failed to download files`, error instanceof Error ? error.message : String(error));
    } finally {
      downloading = false;
    }
  }

  async function uploadPending() {
    const service = mediaFilesService;
    if (!service || uploading) return;

    uploading = true;
    try {
      await service.uploadPendingResources();
      await mediaFiles.refetch();
      AppNotification.display($t`Uploaded pending files`);
    } catch (error) {
      AppNotification.error($t`Failed to upload files`, error instanceof Error ? error.message : String(error));
    } finally {
      uploading = false;
    }
  }
</script>

<div class="flex h-full min-h-0 flex-col gap-4 p-3 sm:p-4">
  <div class="flex shrink-0 flex-col gap-3 sm:flex-row sm:items-center">
    <div class="flex min-w-0 items-center gap-2">
      <SidebarTrigger icon="i-mdi-menu" class="aspect-square shrink-0 p-0" />
      <h1 class="truncate text-lg font-semibold sm:text-xl">{$t`Media Manager`}</h1>
    </div>
    <div class="grid grid-cols-2 gap-2 sm:ms-auto sm:flex sm:flex-wrap sm:items-center">
      <Button
        variant="outline"
        icon="i-mdi-download"
        class="col-span-1 min-h-10 w-full sm:w-auto"
        disabled={downloadableCount === 0 || downloading || mediaFiles.loading}
        loading={downloading}
        onclick={() => void downloadAll()}
      >
        <span class="truncate">{$t`Download all`}</span>
      </Button>
      <Button
        variant="outline"
        icon="i-mdi-cloud-upload-outline"
        class="col-span-1 min-h-10 w-full sm:w-auto"
        disabled={uploading || mediaFiles.loading}
        loading={uploading}
        onclick={() => void uploadPending()}
      >
        <span class="truncate">{$t`Upload pending`}</span>
      </Button>
      <Button
        variant="ghost"
        size="icon"
        icon="i-mdi-refresh"
        class="col-span-2 justify-self-end sm:col-span-1"
        title={$t`Refresh`}
        disabled={mediaFiles.loading}
        onclick={() => void mediaFiles.refetch()}
      />
    </div>
  </div>

  {#if mediaFiles.error}
    <div class="shrink-0 rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-destructive">
      {$t`Failed to load media files`}
    </div>
  {:else if mediaFiles.loading && mediaFiles.current.length === 0}
    <div class="text-muted-foreground px-2">{$t`Loading...`}</div>
  {:else if mediaFiles.current.length === 0}
    <div class="text-muted-foreground px-2">{$t`No media files found`}</div>
  {:else}
    <div class="flex min-h-0 flex-1 flex-col gap-4 overflow-hidden md:grid md:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
      <ul class="min-h-0 space-y-2 overflow-y-auto overscroll-contain pb-4 px-2 pt-2 md:pb-0">
        {#each mediaFiles.current as file (file.id)}
          {@const status = locationStatus(file)}
          <li>
            <div
              role="button"
              tabindex="0"
              onclick={() => selectFile(file)}
              onkeydown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  selectFile(file);
                }
              }}
              class={cn(
                'group flex min-h-16 cursor-pointer items-start gap-3 rounded-lg bg-muted px-3 py-3 shadow-sm transition-colors sm:items-center sm:gap-4 sm:px-4',
                'hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50',
                selectedFile?.id === file.id && 'ring-2 ring-primary bg-primary/10',
              )}
            >
              <div
                class="flex shrink-0 items-center gap-1 pt-0.5 sm:pt-0"
                title={locationLabels[status]()}
                aria-label={locationLabels[status]()}
              >
                {#if status === 'local' || status === 'both'}
                  <Icon icon="i-mdi-laptop" class="size-5 text-blue-600 dark:text-blue-400" />
                {/if}
                {#if status === 'remote' || status === 'both'}
                  <Icon icon="i-mdi-cloud-outline" class="size-5 text-sky-600 dark:text-sky-400" />
                {/if}
              </div>
              <div class="min-w-0 flex-1">
                <p class="break-all text-sm font-medium leading-snug sm:text-base sm:leading-normal">
                  {displayName(file)}
                </p>
                <p class="mt-1 text-xs text-muted-foreground sm:text-sm">
                  {locationLabels[status]()}
                </p>
              </div>
              {#if file.remote && !file.local}
                <Button
                  variant="ghost"
                  size="icon-sm"
                  icon="i-mdi-download"
                  class="hidden shrink-0 md:inline-flex md:opacity-0 md:transition-opacity md:group-hover:opacity-100 md:group-focus-within:opacity-100"
                  title={$t`Download`}
                  loading={downloadingFileIds.has(file.id)}
                  disabled={downloadingFileIds.has(file.id)}
                  onclick={(event) => void downloadFile(file.id, event)}
                />
              {/if}
            </div>
          </li>
        {/each}
      </ul>

      {#if selectedFile && !IsMobile.value}
        <aside class="hidden min-h-0 overflow-hidden rounded-lg border bg-background md:flex md:flex-col">
          <MediaFileDetail
            file={selectedFile}
            {mediaFilesService}
            locationStatus={locationStatus(selectedFile)}
            downloading={downloadingFileIds.has(selectedFile.id)}
            onDownload={downloadFileForDetail}
            class="h-full"
          />
        </aside>
      {/if}
    </div>
  {/if}
</div>

{#if IsMobile.value}
  <Drawer.Root bind:open={() => !!selectedFile, (open) => { if (!open) selectedFileId = undefined; }}>
    <Drawer.Content class="mx-auto max-h-[90vh] w-full max-w-none">
      <div class="mx-auto w-full max-w-4xl overflow-y-auto overscroll-contain">
        <Drawer.Header class="text-left">
          <Drawer.Title class="sr-only">{$t`Media file details`}</Drawer.Title>
        </Drawer.Header>
        {#if selectedFile}
          <MediaFileDetail
            file={selectedFile}
            {mediaFilesService}
            locationStatus={locationStatus(selectedFile)}
            downloading={downloadingFileIds.has(selectedFile.id)}
            onDownload={downloadFileForDetail}
         />
        {/if}
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}