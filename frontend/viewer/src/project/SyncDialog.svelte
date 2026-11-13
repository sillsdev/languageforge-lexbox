<script lang="ts" module>
  export const SYNC_DIALOG_QUERY_PARAM = 'syncDialogOpen';
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {watch} from 'runed';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {Button} from '$lib/components/ui/button';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import FwLiteSyncView from './sync/FwLiteSyncView.svelte';
  import FwClassicSyncView from './sync/FwClassicSyncView.svelte';
  import FolderSyncSection from './sync/FolderSyncSection.svelte';
  import SyncMethodList from './sync/SyncMethodList.svelte';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import type {SyncMethod, SyncTarget} from './sync/sync-target';
  import {featureFlags, hasFlag} from '$lib/feature-flags/feature-flags.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {flushSync} from 'svelte';

  const {
    syncStatus = SyncStatus.Success
  }: { syncStatus?: SyncStatus } = $props();

  const projectContext = useProjectContext();
  const service = useSyncStatusService();
  const features = useFeatures();
  let server = $derived(projectContext.server);
  const showClassic = $derived(!!server && !!features.fieldWorksSync);
  const platformFeatures = usePlatformFeaturesService();
  const showFolder = $derived(platformFeatures.features.supportsFolderSync && hasFlag('folder-sync'));
  const openQueryParam = new QueryParamStateBool(
    { key: SYNC_DIALOG_QUERY_PARAM, replaceOnDefaultValue: true, allowBack: true },
    false,
  );
  // undefined = the list of sync methods
  let method = $state<SyncMethod>();
  // focused when going back to the list
  let lastMethod = $state<SyncMethod>();
  // With only one method there's nothing to choose
  const hasChoice = $derived(showClassic || showFolder || featureFlags.isDev);
  const shown = $derived<SyncMethod | undefined>(hasChoice ? method : 'lite');
  // System and browser back go from a method's view to the list before they close the dialog
  useBackHandler({addToStack: () => openQueryParam.current && !!method, onBack: () => method = undefined, key: 'sync-method'});
  let syncing = $state<SyncTarget>();
  // Any sync (from this dialog or in the background) changes every view's status, so they reload when this changes
  let syncCount = $state(0);
  useProjectEventBus().onSync(e => {
    if (e.status === SyncStatus.Success) syncCount++;
  });

  const methodTitles = $derived<Record<SyncMethod, string>>({
    lite: $t`Other FieldWorks Lite users`,
    classic: $t`FieldWorks Send/Receive (on Lexbox)`,
    folder: $t`USB Drive / Folder`,
    nearby: 'Nearby devices',
  });

  // The dialog's content unmounts on close, so each view loads its own status when it's first shown
  watch(() => openQueryParam.current, (open, wasOpen) => {
    if (!open || wasOpen) return;
    method = lastMethod = undefined;
    void service.getCurrentServer().then(s => server = s);
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  function close() {
    // The method view's history entry sits above the dialog's, so it has to be popped first
    method = undefined;
    flushSync();
    openQueryParam.current = false;
  }

  async function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    if (status !== 'logged-in') return;
    const result = await service.triggerCrdtSync();
    // The sync event fires before the sync date is saved, so reload again now that the sync is done
    if (result.isSynced) syncCount++;
  }
</script>

<ResponsiveDialog bind:open={() => openQueryParam.current, (open) => open ? openQueryParam.current = true : close()} disableBackHandler title={$t`Sync Changes`} hideTitleOnMobile hideTitle={!!shown && hasChoice}
  contentProps={{ class: 'sm:min-w-140 grid-rows-[auto_1fr] items-center' }}>
  <div>
    {#if !shown}
      <SyncMethodList {syncStatus} {showClassic} {showFolder} {syncCount} titles={methodTitles} focus={lastMethod}
        onChoose={m => method = lastMethod = m} />
    {:else if hasChoice}
      <div class="flex items-center gap-1 mb-4 -ms-2 -mt-2 pe-10">
        <Button variant="ghost" size="icon" icon="i-mdi-arrow-left" iconProps={{class: 'rtl:rotate-180'}}
          title={$t`Back`} autofocus onclick={() => method = undefined} />
        <h3 class="text-lg leading-tight font-semibold">{methodTitles[shown]}</h3>
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
        showHeading={!hasChoice}
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
          onSyncedAndUpToDate={close} />
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
