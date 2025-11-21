<script lang="ts">
  import { t } from 'svelte-i18n-lingui';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import type {IEntry} from '$lib/dotnet-types';
  import type {Snippet} from 'svelte';
  import {useWritingSystemService} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import {useFeatures} from '$lib/services/feature-service';
  import HistoryView from '$lib/history/HistoryView.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {useAppLauncherService} from '$lib/services/app-launcher-service';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import OpenInFieldWorksButton from '$lib/components/OpenInFieldWorksButton.svelte';

  const multiWindowService = useMultiWindowService();
  const dialogsService = useDialogsService();
  const projectEventBus = useProjectEventBus();
  const writingSystemService = useWritingSystemService();
  const miniLcmApi = useMiniLcmApi();
  const currentView = useCurrentView();

  let { entry, contextMenu = false, children = undefined } = $props<{
    entry: IEntry;
    contextMenu?: boolean;
    children?: Snippet
  }>();

  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);

  let open = $state(false);

  async function onDelete() {
    if (!await dialogsService.promptDelete(pt($t`Entry`, $t`Word`, $currentView), headword)) return;
    await miniLcmApi.deleteEntry(entry.id);
    projectEventBus.notifyEntryDeleted(entry.id);
  }

  const features = useFeatures();
  let showHistoryView = $state(false);
  const appLauncher = useAppLauncherService();
</script>
{#if features.history}
  <HistoryView bind:open={showHistoryView} id={entry.id}/>
{/if}

<ResponsiveMenu.Root {contextMenu} bind:open>
  <ResponsiveMenu.Trigger {children} />
  <ResponsiveMenu.Content>
    {#if features.write}
      <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={onDelete}>
        {pt($t`Delete Entry`, $t`Delete Word`, $currentView)}
      </ResponsiveMenu.Item>
    {/if}
    {#if features.history}
      <ResponsiveMenu.Item icon="i-mdi-history" onSelect={() => showHistoryView = true}>
        {$t`History`}
      </ResponsiveMenu.Item>
    {/if}
    {#if multiWindowService}
      <ResponsiveMenu.Item icon="i-mdi-open-in-new" onSelect={() => void multiWindowService.openEntryInNewWindow(entry.id)}>
        {$t`Open in new Window`}
      </ResponsiveMenu.Item>
    {/if}
    {#if features.openWithFlex && (appLauncher || !IsMobile.value)}
      <ResponsiveMenu.Item>
        {#snippet child({props})}
          <OpenInFieldWorksButton {entry} {...props} />
        {/snippet}
      </ResponsiveMenu.Item>
    {/if}
  </ResponsiveMenu.Content>
</ResponsiveMenu.Root>
