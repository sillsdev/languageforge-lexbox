<script lang="ts">
  import { Button, Icon, Menu, MenuItem, Popover, Toggle } from 'svelte-ux';
  import FieldTitle from '../FieldTitle.svelte';
  import { useCurrentView } from '../../services/view-service';
  import { mdiChevronDoubleLeft, mdiArrowLeftBold, mdiArrowRightBold, mdiArrowLeftRightBold, mdiPlus, mdiTrashCanOutline, mdiBookOutline } from '@mdi/js';
  import { mdiDotsVertical } from '@mdi/js';
  import EntryOrSensePicker, { type EntrySenseSelection } from './EntryOrSensePicker.svelte';
  import { headword, randomId } from '../../utils';
  import { createEventDispatcher } from 'svelte';
  import { Link } from 'svelte-routing';
  import type { IComplexFormComponent } from '../../mini-lcm/i-complex-form-component';

  const dispatch = createEventDispatcher<{
    change: { value: IComplexFormComponent[] };
  }>();

  export let id: string;
  export let name: string | undefined = undefined;
  export let value: IComplexFormComponent[];
  export let readonly: boolean;
  export let entryId: string;
  let currentView = useCurrentView();

  $: empty = !value;

  let openPicker = false;

  function addComponent(selection: EntrySenseSelection) {
    const component = {
      id: randomId(),
      complexFormEntryId: entryId,
      componentEntryId: selection.entry.id,
      componentSenseId: selection.sense?.id,
      componentHeadword: headword(selection.entry),
    };
    value = [...value, component];
    dispatch('change', { value });
  }

  function removeComponent(component: IComplexFormComponent): void {
    value = value.filter((c) => c !== component);
    dispatch('change', { value });
  }

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

<div
  class="complex-form-components-field field"
  class:empty
  class:hidden={!$currentView.fields[id].show}
  style:grid-area={id}
>
  <FieldTitle {id} {name} />
  <div class="col-span-2 flex gap-2 flex-wrap">
    {#each value as component, i}
      <div
        class="complex-form-component border shadow-sm_ rounded-md pl-2 pr-0.5 py-0.5 inline-flex items-center gap-1 group whitespace-nowrap"
      >
        <Link class="hover:underline" to="?entryId={component.componentEntryId}&search={component.componentHeadword}">
          {component.componentHeadword}
        </Link>
        <Toggle let:on={open} let:toggle let:toggleOff on:toggleOn={() => newIndex = currIndex = i}>
          <Button on:click={toggle} variant={open ? 'fill-light' : 'default'} color="accent" icon={mdiDotsVertical} size="sm" class="px-1">
            <Menu {open} let:close={closeMenu} on:close={toggleOff} placement="bottom-end" disableTransition explicitClose>
              <MenuItem class="gap-2">
                <Link to="?entryId={component.componentEntryId}&search={component.componentHeadword}">
                  Go to {component.componentHeadword}
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
                                title={item.componentHeadword}
                                size="sm">
                                <span class="justify-self-end">{j + 1}:</span>
                                <span class="max-w-52 overflow-x-clip text-ellipsis">{item.componentHeadword}</span>
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
                        <button on:click={() => { if (count > 2) toggle(); else { swap();  closeMenu(); }}} class="text-info">
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
                  removeComponent(component);
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
        <Button on:click={() => openPicker = true} icon={mdiPlus} variant="fill-light" color="success" size="sm">
          <div class="max-sm:hidden">Add Component</div>
        </Button>
      </div>
    {/if}
  </div>
</div>

<EntryOrSensePicker title="Add component to complex form" bind:open={openPicker} on:pick={(e) => addComponent(e.detail)}
  disableEntry={(e) => e.id === entryId || value.some((c) => c.componentEntryId === e.id && !c.componentSenseId)}
  disableSense={(s, e) => value.some((c) => c.componentEntryId === e.id && c.componentSenseId === s.id)} />
