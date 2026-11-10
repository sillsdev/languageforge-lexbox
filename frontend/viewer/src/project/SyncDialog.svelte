<script lang="ts" module>
  export const SYNC_DIALOG_QUERY_PARAM = 'syncDialogOpen';
</script>

<script lang="ts">
  import {t, T} from 'svelte-i18n-lingui';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {resource, watch} from 'runed';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import FwLiteSyncView from './sync/FwLiteSyncView.svelte';
  import FwClassicSyncView from './sync/FwClassicSyncView.svelte';
  import FolderSyncSection from './sync/FolderSyncSection.svelte';
  import SyncMethodButton from './sync/SyncMethodButton.svelte';
  import NearbyDevicesIcon from './sync/NearbyDevicesIcon.svelte';
  import {shortFolderPath} from './sync/folder-sync-messages';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import type {SyncTarget} from './sync/sync-target';
  import {featureFlags, hasFlag} from '$lib/feature-flags/feature-flags.svelte';

  type SyncMethod = SyncTarget | 'nearby';

  const {
    syncStatus = SyncStatus.Success
  }: { syncStatus?: SyncStatus } = $props();

  const projectContext = useProjectContext();
  const service = useSyncStatusService();
  const features = useFeatures();
  let server = $derived(projectContext.server);
  const showClassic = $derived(!!server && features.fieldWorksSync);
  const platformFeatures = usePlatformFeaturesService();
  const showFolder = $derived(platformFeatures.features.supportsFolderSync && hasFlag('folder-sync'));
  const openQueryParam = new QueryParamStateBool(
    { key: SYNC_DIALOG_QUERY_PARAM, replaceOnDefaultValue: true, allowBack: true },
    false,
  );
  // undefined = the list of sync methods
  let method = $state<SyncMethod>();
  // With only one method there's nothing to choose
  const shown = $derived<SyncMethod | undefined>(showClassic || showFolder || featureFlags.isDev ? method : 'lite');
  let syncing = $state<SyncTarget>();
  // Any sync (from this dialog or in the background) changes every view's status, so they reload when this changes
  let syncCount = $state(0);
  useProjectEventBus().onSync(e => {
    if (e.status === SyncStatus.Success) syncCount++;
  });
  const cloudIcon = $derived(syncStatus === SyncStatus.Offline ? 'i-mdi-cloud-off-outline' : 'i-mdi-cloud-outline');

  const lastSync = resource(() => syncCount, () => service.getLatestSyncedCommitDate());
  const lastSyncDate = $derived(lastSync.current ? new Date(lastSync.current) : undefined);
  const rememberedFolder = resource(() => showFolder, async (show) => show ? service.getRememberedSyncFolder() : undefined);

  const methodTitles = $derived<Record<SyncMethod, string>>({
    lite: $t`Other FieldWorks Lite users`,
    classic: $t`FieldWorks Send/Receive (on Lexbox)`,
    folder: $t`USB Drive / Folder`,
    nearby: 'Nearby devices',
  });

  // The dialog's content unmounts on close, so each view loads its own status when it's first shown
  watch(() => openQueryParam.current, (open) => {
    if (!open) return;
    method = undefined;
    void service.getCurrentServer().then(s => server = s);
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  async function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    if (status !== 'logged-in') return;
    const result = await service.triggerCrdtSync();
    // The sync event fires before the sync date is saved, so reload again now that the sync is done
    if (result.isSynced) syncCount++;
  }
</script>

<ResponsiveDialog bind:open={openQueryParam.current} disableBackHandler title={$t`Sync Changes`} hideTitleOnMobile
  contentProps={{ class: 'sm:min-w-140 grid-rows-[auto_1fr] items-center' }}>
  <div>
    {#if !shown}
      <div class="flex flex-col gap-4 max-md:mt-8">
        <SyncMethodButton featured title={methodTitles.lite} onclick={() => method = 'lite'}>
          {#snippet icon()}<Icon icon={cloudIcon} class="size-8" />{/snippet}
          {#snippet description()}
            <span class="flex items-center gap-1.5">
              <span class="size-2 shrink-0 rounded-full"
                class:bg-green-500={syncStatus === SyncStatus.Success}
                class:bg-red-500={syncStatus !== SyncStatus.Success}></span>
              {#if syncStatus === SyncStatus.Success}
                <span>
                  {$t`Auto syncing`} ·
                  <T msg="Last sync: #">
                    {#if !lastSyncDate}
                      <span>{$t`Never`}</span>
                    {:else}
                      <FormatRelativeDate date={lastSyncDate} live />
                    {/if}
                  </T>
                </span>
              {:else if syncStatus === SyncStatus.Offline}
                {$t`Offline. Your changes are saved on this device.`}
              {:else if syncStatus === SyncStatus.NotLoggedIn}
                {$t`Not logged in`}
              {:else if syncStatus === SyncStatus.NoServer}
                {$t`No server configured`}
              {:else}
                {$t`Error getting sync status`}
              {/if}
            </span>
          {/snippet}
        </SyncMethodButton>
        <div class="flex flex-col divide-y rounded-lg border overflow-hidden empty:hidden">
          {#if showClassic}
            <SyncMethodButton title={methodTitles.classic} onclick={() => method = 'classic'}>
              {#snippet icon()}
                <span class="relative">
                  <img src={flexLogo} alt="" class="size-7" />
                  <Icon icon="i-mdi-cloud" class="absolute -bottom-1 -right-1.5 size-4 text-muted-foreground drop-shadow-[0_0_1px_var(--background)]" />
                </span>
              {/snippet}
              {#snippet description()}{$t`Runs on Lexbox, not with FieldWorks on this computer`}{/snippet}
            </SyncMethodButton>
          {/if}
          {#if showFolder}
            <SyncMethodButton title={methodTitles.folder} onclick={() => method = 'folder'}>
              {#snippet icon()}<Icon icon="i-mdi-usb-flash-drive-outline" class="size-7" />{/snippet}
              {#snippet description()}
                {#if rememberedFolder.current}
                  <span title={rememberedFolder.current}>{$t`Sync with ${shortFolderPath(rememberedFolder.current)}`}</span>
                {:else}
                  {$t`Sync with a USB drive or another folder`}
                {/if}
              {/snippet}
            </SyncMethodButton>
          {/if}
          <DevContent>
            <SyncMethodButton title={methodTitles.nearby} onclick={() => method = 'nearby'}>
              {#snippet icon()}<NearbyDevicesIcon class="size-7" />{/snippet}
              {#snippet description()}Phones and computers on the same Wi-Fi{/snippet}
            </SyncMethodButton>
          </DevContent>
        </div>
      </div>
    {:else if method}
      <div class="flex items-center gap-2 mb-4 pb-2 border-b max-md:pe-10">
        <Button variant="ghost" size="icon" icon="i-mdi-arrow-left" iconProps={{class: 'rtl:rotate-180'}}
          title={$t`Back`} onclick={() => method = undefined} />
        <h3 class="font-medium">{methodTitles[method]}</h3>
      </div>
    {/if}
    <!-- Views stay mounted after they're first shown, so going back and forth doesn't reload their status -->
    <IfOnce show={shown === 'lite'}>
      <FwLiteSyncView
        {syncStatus}
        {server}
        serverId={projectContext.projectData?.serverId}
        projectCode={projectContext.projectData?.code}
        bind:syncing
        {syncCount}
        explainFieldWorksSync={!!server && !features.fieldWorksSync}
        {onLoginStatusChange} />
    </IfOnce>
    {#if showClassic}
      <IfOnce show={shown === 'classic'}>
        <FwClassicSyncView
          {syncStatus}
          {server}
          bind:syncing
          {syncCount}
          canSync={features?.write}
          {onLoginStatusChange}
          onSyncedAndUpToDate={() => openQueryParam.current = false} />
      </IfOnce>
    {/if}
    {#if showFolder}
      <IfOnce show={shown === 'folder'}>
        <FolderSyncSection bind:syncing onSynced={() => syncCount++} />
      </IfOnce>
    {/if}
    {#if shown === 'nearby'}
      <p class="py-6 text-center text-sm text-muted-foreground">Coming soon</p>
    {/if}
  </div>
</ResponsiveDialog>
