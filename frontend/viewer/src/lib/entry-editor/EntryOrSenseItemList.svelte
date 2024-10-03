<script lang="ts">
  import { Button, Icon, Menu, MenuItem, Popover, Toggle } from 'svelte-ux';
  import { mdiChevronDoubleLeft, mdiArrowLeftBold, mdiArrowRightBold, mdiArrowLeftRightBold, mdiTrashCanOutline, mdiBookOutline } from '@mdi/js';
  import { mdiDotsVertical } from '@mdi/js';
  import { createEventDispatcher } from 'svelte';
  import { Link } from 'svelte-routing';

  type T = $$Generic;

  const dispatch = createEventDispatcher<{
    change: { value: T[] };
  }>();

  export let value: T[];
  export let readonly: boolean;
  export let getEntryId: (item: T) => string;
  export let getHeadword: (item: T) => string | undefined;

  $: displayItems = value;
  $: count = value.length;

  let currIndex = -1;
  let newIndex = -1;
  $: if (currIndex >= 0) {
    const newDisplayItems = [...value];
    newDisplayItems.splice(currIndex, 1);
    newDisplayItems.splice(newIndex, 0, value[currIndex]);
    displayItems = newDisplayItems;
  }

  function remove(item: T): void {
    value = value.filter((v) => v !== item);
    dispatch('change', { value });
  }

  function move(): void {
    value = displayItems;
    dispatch('change', { value });
  }

  function swap(): void {
    if (value.length > 2) throw new Error(`Swap does not support more than 2 items (${value.length})`)
    if (value.length < 2) return;
    value = [value[1], value[0]];
    dispatch('change', { value });
  }
</script>

<div class="col-span-2 flex gap-2 flex-wrap">
  {#each value as item, i}
    {@const entryId = getEntryId(item)}
    {@const headword = getHeadword(item) || '–'}
    <div
      class="border rounded-md pl-2 pr-0.5 py-0.5 inline-flex items-center gap-1 group whitespace-nowrap"
    >
      <Link class="hover:underline" to="?entryId={entryId}&search={headword}">
        {headword}
      </Link>
      <Toggle let:on={open} let:toggle let:toggleOff on:toggleOn={() => newIndex = currIndex = i}>
        <Button on:click={toggle} variant={open ? 'fill-light' : 'default'} color="accent" icon={mdiDotsVertical} size="sm" class="px-1">
          <Menu {open} let:close={closeMenu} on:close={toggleOff} placement="bottom-end" disableTransition explicitClose>
            <MenuItem class="gap-2">
              <Link to="?entryId={entryId}&search={headword}">
                Go to {headword}
                <Icon data={mdiBookOutline} />
              </Link>
            </MenuItem>
            {#if !readonly}
              {@const first = i === 0}
              {@const last = i === count - 1}
              {@const only = count === 1}
              {#if !only}
                <MenuItem>
                  <Toggle let:on={open} let:toggle let:toggleOff>
                    <Popover {open} let:close on:close={toggleOff} placement="right-start" offset={4}>
                      <menu class="grid gap-2 p-2 rounded-lg bg-surface-100 border shadow-lg max-h-[50vh] overflow-auto"
                        style="grid-template-columns: max-content 1fr max-content;"
                        on:mouseleave={() => newIndex = currIndex}>
                        {#each displayItems as item, j}
                          {@const reorderHeadword = getHeadword(item) || '–'}
                          <button class="contents"
                            on:mouseover={() => newIndex = j}
                            on:focus={() => newIndex = j}
                            on:mouseup={() => {
                              if (j !== i) move();
                              close();
                              closeMenu();
                            }}>
                            <Button
                              variant={j === i ? 'outline' : j === newIndex ? 'fill-outline' : 'fill-light'}
                              class="border grid grid-cols-subgrid col-span-full justify-items-start items-center"
                              color='info'
                              rounded
                              title={reorderHeadword}
                              size="sm">
                              <span class="justify-self-end">{j + 1}:</span>
                              <span class="max-w-52 overflow-x-clip text-ellipsis">{reorderHeadword}</span>
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
                    {#if first || last}
                      <button on:click={() => { if (count > 2) toggle(); else { swap(); closeMenu(); }}} class="text-info">
                        Move
                        <Icon data={first ? mdiArrowRightBold : mdiArrowLeftBold} />
                      </button>
                    {:else}
                      <button on:click={toggle} class="text-info">
                        Move
                        <Icon data={mdiArrowLeftRightBold} />
                      </button>
                    {/if}
                  </Toggle>
                </MenuItem>
              {/if}
              <MenuItem class="text-danger gap-2" on:click={() => {
                remove(item);
                closeMenu();
              }}>
                Remove
                <Icon data={mdiTrashCanOutline} />
              </MenuItem>
              {/if}
          </Menu>
        </Button>
      </Toggle>
    </div>
  {/each}
  {#if !readonly}
    <div class="grow text-right">
      <slot name="actions" />
    </div>
  {/if}
</div>
