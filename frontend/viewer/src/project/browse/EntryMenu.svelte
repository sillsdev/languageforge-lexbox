<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import type {IEntry} from '$lib/dotnet-types';
  import type {Snippet} from 'svelte';
  import {useWritingSystemService} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import HistoryView from '$lib/history/HistoryView.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {useAppLauncherService} from '$lib/services/app-launcher-service';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import OpenInFieldWorksButton from '$lib/components/OpenInFieldWorksButton.svelte';
  import CommentDialog from '$lib/entry-editor/CommentDialog.svelte';
  import {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';

  const multiWindowService = useMultiWindowService();
  const dialogsService = useDialogsService();
  const projectEventBus = useProjectEventBus();
  const writingSystemService = useWritingSystemService();
  const miniLcmApi = useMiniLcmApi();
  const viewService = useViewService();

  let { entry, contextMenu = false, children = undefined } = $props<{
    entry: IEntry;
    contextMenu?: boolean;
    children?: Snippet
  }>();

  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);

  let open = $state(false);

  async function onDelete() {
    if (!await dialogsService.promptDelete(pt($t`Entry`, $t`Word`, viewService.currentView), headword)) return;
    await miniLcmApi.deleteEntry(entry.id);
    projectEventBus.notifyEntryDeleted(entry.id);
  }

  const features = useFeatures();
  let showHistoryView = $state(false);
  let showCommentDialog = $state(false);
  const appLauncher = useAppLauncherService();
</script>
{#if features.history}
  <HistoryView bind:open={showHistoryView} id={entry.id}/>
{/if}
{#if features.comments}
  <CommentDialog
    bind:open={showCommentDialog}
    subjectType={SubjectType.Entry}
    subjectId={entry.id}
    subjectName={headword}
  />
{/if}

<ResponsiveMenu.Root {contextMenu} bind:open>
  <ResponsiveMenu.Trigger {children} />
  <ResponsiveMenu.Content>
    {#if features.comments}
      <ResponsiveMenu.Item icon="i-mdi-comment-text-outline" onSelect={() => showCommentDialog = true}>
        {$t`Comments`}
      </ResponsiveMenu.Item>
    {/if}
    {#if features.write}
      <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={onDelete}>
        {pt($t`Delete Entry`, $t`Delete Word`, viewService.currentView)}
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
