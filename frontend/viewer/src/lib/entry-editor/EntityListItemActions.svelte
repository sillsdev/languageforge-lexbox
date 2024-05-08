<script lang="ts">
  import { mdiArrowDownBold, mdiArrowUpBold, mdiArrowUpDownBold, mdiChevronDoubleLeft, mdiPlaylistPlus, mdiTrashCanOutline } from "@mdi/js";
  import { createEventDispatcher } from "svelte";
  import { Button, ButtonGroup, Icon, Popover, Toggle } from "svelte-ux";
  import HistoryView from '../history/HistoryView.svelte';

  const dispatch = createEventDispatcher<{
    move: number;
    delete: void;
  }>();

  export let i: number;
  export let items: string[];
  export let id: string | undefined = undefined;

  $: displayItems = items;
  $: count = items.length;
  $: first = i === 0;
  $: last = i === count - 1;
  $: only = count === 1;

  $: newIndex = i;
  $: {
    const newDisplayItems = [...items];
    newDisplayItems.splice(i, 1);
    newDisplayItems.splice(newIndex, 0, items[i]);
    displayItems = newDisplayItems;
  }
</script>

<ButtonGroup color="primary">
  {#if !only}
    <div>
      <Toggle let:on={open} let:toggle let:toggleOff>
        <Popover {open} let:close on:close={toggleOff} placement={last ? 'top' : 'bottom'} offset={4}>
          <menu class="grid gap-2 p-2 rounded-2xl bg-surface-100 border shadow-lg max-h-[50vh] overflow-auto"
            style="grid-template-columns: max-content 1fr max-content;"
            on:mouseleave={() => newIndex = i}>
            {#each displayItems as item, j}
              <button class="contents"
                on:mouseover={() => newIndex = j}
                on:focus={() => newIndex = j}
                on:mouseup={() => {
                  if (j !== i) dispatch('move', j);
                  close();
                }}>
                <Button
                  variant={j === i ? 'outline' : j === newIndex ? 'fill-outline' : 'fill-light'}
                  class="border grid grid-cols-subgrid col-span-full justify-items-start items-center"
                  color='info'
                  rounded="full"
                  title={item}
                  size="sm">
                  <span class="justify-self-end">{j + 1}:</span>
                  <span class="max-w-52 overflow-x-clip text-ellipsis">{item}</span>
                  {#if j === newIndex}
                    <div class="h-4 flex items-start">
                      <Icon data={mdiChevronDoubleLeft} />
                    </div>
                  {/if}
                </Button>
              </button>
            {/each}
          </menu>
        </Popover>
        {#if first}
          <Button on:click={() => {count > 2 ? toggle() : dispatch('move', 1)}} variant="fill-light" color="info" rounded icon={mdiArrowDownBold} size="sm"></Button>
        {:else if last}
          <Button on:click={() => {count > 2 ? toggle() : dispatch('move', 0)}} variant="fill-light" color="info" rounded icon={mdiArrowUpBold} size="sm"></Button>
        {:else}
          <Button on:click={toggle} variant="fill-light" color="info" rounded icon={mdiArrowUpDownBold} size="sm"></Button>
        {/if}
      </Toggle>
    </div>
  {/if}
  {#if id}
    <HistoryView small {id}/>
  {/if}
  <Button on:click={() => dispatch('delete')} variant="fill-light" rounded icon={mdiTrashCanOutline} color="danger" size="sm"></Button>
</ButtonGroup>
