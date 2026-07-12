<script lang="ts">
  import type {Snippet} from 'svelte';
  import {ResizableHandle, ResizablePane, ResizablePaneGroup} from '$lib/components/ui/resizable';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import {cn} from '$lib/utils';

  // The app's responsive list-detail shell (Browse, Activity): resizable split panes on desktop; on
  // mobile a full-width list that the detail replaces full-screen while `detailVisible`. The caller owns
  // selection/open state (and its URL params) and passes the combined "should the detail be showing"
  // flag — this component only decides which panes exist. The list stays mounted once shown (IfOnce),
  // so its scroll position survives mobile detail round-trips.
  let {
    detailVisible,
    list,
    detail,
    defaultLayout = [30, 70],
    listMinSize = 15,
    detailMinSize = 15,
    listClass,
    detailClass,
  }: {
    detailVisible: boolean;
    list: Snippet;
    detail: Snippet;
    defaultLayout?: readonly [number, number];
    listMinSize?: number;
    detailMinSize?: number;
    listClass?: string;
    detailClass?: string;
  } = $props();

  let leftPane: ResizablePane | undefined = $state();
  let rightPane: ResizablePane | undefined = $state();
</script>

<ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 overflow-visible!">
  <IfOnce show={!IsMobile.value || !detailVisible}>
    <ResizablePane
      bind:this={leftPane}
      defaultSize={defaultLayout[0]}
      collapsible
      collapsedSize={0}
      minSize={listMinSize}
      class={cn('min-h-0 flex flex-col relative', listClass)}>
      {@render list()}
    </ResizablePane>
  </IfOnce>
  {#if !IsMobile.value}
    <ResizableHandle class="my-4" {leftPane} {rightPane} withHandle resetTo={defaultLayout} />
  {/if}
  {#if !IsMobile.value || detailVisible}
    <ResizablePane
      bind:this={rightPane}
      defaultSize={defaultLayout[1]}
      collapsible
      collapsedSize={0}
      minSize={detailMinSize}
      class={detailClass}>
      {@render detail()}
    </ResizablePane>
  {/if}
</ResizablePaneGroup>
