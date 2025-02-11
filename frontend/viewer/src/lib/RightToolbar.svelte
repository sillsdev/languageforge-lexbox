<script lang="ts">
  import {type IEntry} from '$lib/dotnet-types';
  import {mdiArrowCollapseRight, mdiArrowExpandLeft, mdiEyeSettingsOutline, mdiOpenInNew} from '@mdi/js';
  import OpenInFieldWorksButton from '$lib/OpenInFieldWorksButton.svelte';
  import {Button} from 'svelte-ux';
  import Toc from '$lib/layout/Toc.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import {useProjectViewState} from './views/project-view-state-service';
  import {useFeatures} from '$lib/services/feature-service';
  import {useCurrentView} from './views/view-service';
  import {asScottyPortal} from '$lib/layout/Scotty.svelte';
  import {useMultiWindowService} from '$lib/services/service-provider';

  const dispatch = createEventDispatcher<{
    showOptionsDialog: void;
  }>();
  export let selectedEntry: IEntry | undefined;
  export let expandList: boolean;
  let projectName = getContext<string>('project-name');
  const state = useProjectViewState();
  const features = useFeatures();
  const currentView = useCurrentView();
  const multiWindowService = useMultiWindowService();
</script>
<div class="side-scroller pl-6 border-l-2 gap-4 flex-col col-start-3 hidden"
     class:border-l-2={selectedEntry && !expandList} class:lg-view:flex={!expandList}>
  {#if selectedEntry}
    <div class="sm-form:hidden" class:sm:hidden={expandList}>
      <Button icon={$state.rightToolbarCollapsed ? mdiArrowExpandLeft : mdiArrowCollapseRight}
              class="text-field-sibling-button" iconOnly rounded variant="outline"
              title={$state.rightToolbarCollapsed ? 'Expand toolbar' : 'Collapse toolbar'}
              on:click={() => $state.rightToolbarCollapsed = !$state.rightToolbarCollapsed}/>
    </div>
  {/if}
  <div class="w-[15vw] collapsible-col sm-form:w-min" class:self-center={$state.rightToolbarCollapsed}
       class:lg-view:collapse-col={expandList} class:!w-min={$state.rightToolbarCollapsed}>
    {#if selectedEntry}
      {@const entryId = selectedEntry.id}
      <div class="sm-form:flex flex-col" class:lg-view:hidden={expandList}
           class:lg-view:flex={$state.rightToolbarCollapsed}>
        <div class="h-full flex flex-col gap-4 justify-stretch sm-form:icon-button-group-container"
          class:icon-button-group-container={$state.rightToolbarCollapsed}>
          <div class="grid gap-4 auto-rows-fr sm-form:gap-2"
               class:!gap-2={$state.rightToolbarCollapsed}
               >
            <div class="contents" use:asScottyPortal={'right-toolbar'}></div>
            <div class="contents">
              <OpenInFieldWorksButton {entryId} {projectName} show={$features.openWithFlex}/>
            </div>
          </div>
          <div class="contents sm-form:hidden" class:hidden={$state.rightToolbarCollapsed}>
            <Toc entry={selectedEntry}/>
          </div>
          {#if multiWindowService}
            <Button icon={mdiOpenInNew} zize="md" title="Open in new Window" iconOnly on:click={() => multiWindowService.openEntryInNewWindow(entryId)} size="sm">
              <div class="sm-form:hidden" class:hidden={$state.rightToolbarCollapsed}>
                Open in new Window
              </div>
            </Button>
          {/if}
        </div>
        <span
          class="text-surface-content whitespace-nowrap bg-surface-100/75 !pt-2 text-sm lg-form:absolute -bottom-4 -right-4 inline-flex gap-2 text-end items-center"
          class:lg-form:!static={$state.rightToolbarCollapsed} class:lg-form:p-2={!$state.rightToolbarCollapsed}>
                <span class="contents sm-form:hidden" class:hidden={$state.rightToolbarCollapsed}>
                  {$currentView.label}
                </span>
                <Button
                  on:click={() => (dispatch('showOptionsDialog'))}
                  size="md"
                  variant="default"
                  title="Configure view"
                  iconOnly
                  icon={mdiEyeSettingsOutline}/>
              </span>
      </div>
    {/if}
  </div>
</div>
