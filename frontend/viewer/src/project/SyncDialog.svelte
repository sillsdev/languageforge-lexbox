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
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import * as Tabs from '$lib/components/ui/tabs';
  import {Icon} from '$lib/components/ui/icon';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import FwLiteSyncView from './sync/FwLiteSyncView.svelte';
  import FwClassicSyncView from './sync/FwClassicSyncView.svelte';
  import type {SyncTarget} from './sync/sync-target';

  const {
    syncStatus = SyncStatus.Success
  }: { syncStatus?: SyncStatus } = $props();

  const projectContext = useProjectContext();
  const service = useSyncStatusService();
  const features = useFeatures();
  let server = $derived(projectContext.server);
  const openQueryParam = new QueryParamStateBool(
    { key: SYNC_DIALOG_QUERY_PARAM, replaceOnDefaultValue: true, allowBack: true },
    false,
  );
  let tab = $state<SyncTarget>('lite');
  let syncing = $state<SyncTarget>();
  const cloudIcon = $derived(syncStatus === SyncStatus.Offline ? 'i-mdi-cloud-off-outline' : 'i-mdi-cloud-outline');

  // The dialog's content unmounts on close, so each view loads its own status when it's first shown
  watch(() => openQueryParam.current, (open) => {
    if (!open) return;
    tab = 'lite';
    void service.getCurrentServer().then(s => server = s);
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    // A successful sync publishes a sync event, which the views refresh on
    if (status === 'logged-in') void service.triggerCrdtSync();
  }
</script>

<ResponsiveDialog bind:open={openQueryParam.current} disableBackHandler title={$t`Sync Changes`}
  contentProps={{ class: 'sm:min-w-140 grid-rows-[auto_1fr] items-center' }}>
  <Tabs.Root bind:value={tab} class="flex md:flex-col flex-col-reverse">
    {#if server}
      <Tabs.List class="w-full md:mb-2 max-md:mt-4 max-md:sticky bottom-0 z-1">
        <Tabs.Trigger class="flex-1" value="lite">{$t`FieldWorks Lite`}</Tabs.Trigger>
        <Tabs.Trigger class="flex-1 gap-2" value="classic"><Icon icon={cloudIcon} class="-my-1" /> {$t`Lexbox`}</Tabs.Trigger>
      </Tabs.List>
    {/if}
    <Tabs.Content value="lite">
      <IfOnce show={tab === 'lite'}>
        <FwLiteSyncView
          {syncStatus}
          {server}
          serverId={projectContext.projectData?.serverId}
          projectCode={projectContext.projectData?.code}
          bind:syncing
          {onLoginStatusChange} />
      </IfOnce>
    </Tabs.Content>
    {#if server}
      <Tabs.Content value="classic">
        <IfOnce show={tab === 'classic'}>
          <FwClassicSyncView
            {syncStatus}
            {server}
            bind:syncing
            canSync={features?.write}
            {onLoginStatusChange}
            onSyncedAndUpToDate={() => openQueryParam.current = false} />
        </IfOnce>
      </Tabs.Content>
    {/if}
  </Tabs.Root>
</ResponsiveDialog>
