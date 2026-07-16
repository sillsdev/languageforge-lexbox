<script lang="ts">
  import {ResizableHandle, ResizablePane, ResizablePaneGroup} from '$lib/components/ui/resizable';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {type Snippet, untrack} from 'svelte';
  import {watch} from 'runed';
  import {MasterDetailUrlState} from './master-detail-url-state.svelte';

  let {
    idParam,
    openParam,
    selectedId = $bindable(''),
    defaultLayout = [30, 70],
    onClose,
    master,
    detail,
    empty,
  }: {
    idParam: string;
    openParam: string;
    selectedId?: string;
    defaultLayout?: readonly [number, number];
    onClose?: () => void | Promise<void>;
    master: Snippet<[{selectedId: string; select: (id: string) => void}]>;
    detail: Snippet<[{selectedId: string; close: () => void; showClose: boolean}]>;
    empty?: Snippet;
  } = $props();

  // Param keys and history strategy are fixed at mount (same as prior BrowseView behavior).
  const urlState = untrack(() => new MasterDetailUrlState(idParam, openParam));
  // Seed from URL before watches run so we don't clear an existing deep link.
  selectedId = urlState.selectedId.current;

  watch(
    () => urlState.selectedId.current,
    (id) => {
      if (selectedId !== id) selectedId = id;
    },
  );

  // Parent writes (e.g. after creating an item) → sync to URL and open detail.
  watch(
    () => selectedId,
    (id) => {
      if (id !== urlState.selectedId.current) {
        urlState.select(id);
      }
    },
  );

  function select(id: string) {
    urlState.select(id);
  }

  function close() {
    if (!IsMobile.value) return;
    urlState.close();
    void onClose?.();
  }

  let leftPane: ResizablePane | undefined = $state();
  let rightPane: ResizablePane | undefined = $state();
</script>

<ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 overflow-visible!">
  <IfOnce show={urlState.showMaster}>
    <ResizablePane
      bind:this={leftPane}
      defaultSize={defaultLayout[0]}
      collapsible
      collapsedSize={0}
      minSize={15}
      class="min-h-0 flex flex-col relative @container/list"
    >
      {@render master({selectedId: urlState.selectedId.current, select})}
    </ResizablePane>
  </IfOnce>
  {#if !IsMobile.value}
    <ResizableHandle class="my-4" {leftPane} {rightPane} withHandle resetTo={defaultLayout} />
  {/if}
  {#if urlState.showDetail}
    <ResizablePane bind:this={rightPane} defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
      {#if !urlState.selectedId.current}
        {@render empty?.()}
      {:else}
        {@render detail({
          selectedId: urlState.selectedId.current,
          close,
          showClose: IsMobile.value,
        })}
      {/if}
    </ResizablePane>
  {/if}
</ResizablePaneGroup>
