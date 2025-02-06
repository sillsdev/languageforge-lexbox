<script lang="ts">
  import {DotnetService, type IEntry} from '$lib/dotnet-types';
  import {mdiArrowCollapseRight, mdiArrowExpandLeft, mdiEyeSettingsOutline, mdiOpenInNew} from '@mdi/js';
  import OpenInFieldWorksButton from '$lib/OpenInFieldWorksButton.svelte';
  import {Button} from 'svelte-ux';
  import Toc from '$lib/layout/Toc.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import {useProjectViewState} from '$lib/services/project-view-state-service';
  import {useFeatures} from '$lib/services/feature-service';
  import {useCurrentView} from '$lib/services/view-service';
  import {asScottyPortal} from '$lib/layout/Scotty.svelte';
  import {tryUseService} from '$lib/services/service-provider';

  const dispatch = createEventDispatcher<{
    showOptionsDialog: void;
  }>();
  export let selectedEntry: IEntry | undefined;
  export let expandList: boolean;
  let projectName = getContext<string>('project-name');
  const state = useProjectViewState();
  const features = useFeatures();
  const currentView = useCurrentView();
  const multiWindowService = tryUseService(DotnetService.MultiWindowService);
</script>
<div class="side-scroller pl-6 border-l-2 gap-4 flex-col col-start-3 hidden"
     class:border-l-2={selectedEntry && !expandList} class:lg-view:flex={!expandList}>
  {#if selectedEntry}
    <div class="sm-form:hidden" class:sm:hidden={expandList}>
      <Button icon={$state.rightToolbarCollapsed ? mdiArrowExpandLeft : mdiArrowCollapseRight}
              class="text-field-sibling-button" iconOnly rounded variant="outline"
              on:click={() => $state.rightToolbarCollapsed = !$state.rightToolbarCollapsed}/>
    </div>
  {/if}
  <div class="w-[15vw] collapsible-col sm-form:w-min" class:self-center={$state.rightToolbarCollapsed}
       class:lg-view:collapse-col={expandList} class:!w-min={$state.rightToolbarCollapsed}>
    {#if selectedEntry}
      <div class="sm-form:flex flex-col" class:lg-view:hidden={expandList}
           class:lg-view:flex={$state.rightToolbarCollapsed}>
        <div class="h-full flex flex-col gap-4 justify-stretch">
          <div class="grid gap-4 auto-rows-fr sm-form:gap-2 sm-form:icon-button-group-container"
               class:!gap-2={$state.rightToolbarCollapsed}
               class:icon-button-group-container={$state.rightToolbarCollapsed}>
            <div class="contents" use:asScottyPortal={'right-toolbar'}></div>
            <div class="contents">
              <OpenInFieldWorksButton entryId={selectedEntry.id} {projectName} show={$features.openWithFlex}/>
            </div>
          </div>
          <div class="contents sm-form:hidden" class:hidden={$state.rightToolbarCollapsed}>
            <Toc entry={selectedEntry}/>
          </div>
          {#if multiWindowService}
            <div class="contents sm-form:hidden" class:hidden={$state.rightToolbarCollapsed}>
              <Button icon={mdiOpenInNew} on:click={() => multiWindowService.openNewWindow(location.pathname + location.search + location.hash)}>Open new Window</Button>
            </div>
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
                  iconOnly
                  icon={mdiEyeSettingsOutline}/>
              </span>
      </div>
    {/if}
  </div>
</div>
