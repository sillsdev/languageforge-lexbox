<script lang="ts" generics="MutableValue">
  import {Badge} from '$lib/components/ui/badge';
  import {Button, XButton} from '$lib/components/ui/button';
  import {Popover, PopoverContent, PopoverTrigger} from '$lib/components/ui/popover';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
    CommandSeparator,
  } from '../ui/command';
  import {Drawer, DrawerContent, DrawerDescription, DrawerHeader, DrawerTitle, DrawerTrigger} from '../ui/drawer';
  import {Icon} from '../ui/icon';
  import type {ConditionalKeys, Primitive, ReadonlyDeep} from 'type-fest';
  import {cn} from '$lib/utils';
  import DrawerFooter from '../ui/drawer/drawer-footer.svelte';
  import {watch} from 'runed';
  import {computeCommandScore} from 'bits-ui';
  import {inputVariants} from '../ui/input/input.svelte';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import OptionIndicator from '../ui/select/option-indicator.svelte';
  import {LONG_LIST, OPTION_ROW, trackActiveDescendant} from '../ui/select/select-list';
  import {onDestroy, tick} from 'svelte';
  import {SvelteMap, SvelteSet} from 'svelte/reactivity';
  import {on} from 'svelte/events';

  type Value = ReadonlyDeep<MutableValue>;

  const labelledBy = tryUseFieldBody()?.labelId;

  let {
    values = $bindable(),
    ...constProps
  }: {
    values: Value[];
    options: ReadonlyArray<Value>;
    readonly?: boolean;
    idSelector: ConditionalKeys<Value, Primitive> | ((value: Value) => Primitive);
    labelSelector: ConditionalKeys<Value, string> | ((value: Value) => string);
    placeholder?: string;
    filterPlaceholder?: string;
    emptyResultsPlaceholder?: string;
    drawerTitle?: string;
    sortValuesBy?: 'selectionOrder' | 'optionOrder' | NonNullable<Parameters<Array<Value>['sort']>[0]>;
    showSelectedGroup?: boolean;
    onchange?: (value: Value[]) => void;
  } = $props();

  const {
    options,
    readonly = false,
    idSelector,
    labelSelector,
    placeholder,
    filterPlaceholder,
    emptyResultsPlaceholder,
    drawerTitle,
    sortValuesBy = 'selectionOrder',
    showSelectedGroup = true,
    onchange,
  } = $derived(constProps);

  function getId(value: Value): Primitive {
    if (typeof idSelector === 'function') return idSelector(value);
    return value[idSelector] as Primitive;
  }

  function getLabel(value: Value): string {
    if (typeof labelSelector === 'function') return labelSelector(value);
    return value[labelSelector] as string;
  }

  // A wrapper for caching calculated values
  type DecoratedValue = {
    value: Value;
    optionIndex: number;
    id: Primitive;
    label: string;
  };

  let open = $state(false);
  let filterValue = $state('');
  let decoratedValues = $derived(decorateAndSort([...values]));
  // Frozen at open so newly selected items stay in place until the picker is reopened.
  let selectedGroupSnapshot = $state<DecoratedValue[]>([]);
  // The user's toggles collect here and save as one change when the picker closes, however it closes.
  // Only the toggles are applied, on top of the current values, so changes synced in meanwhile survive.
  let added = new SvelteMap<Primitive, Value>();
  let removed = new SvelteSet<Primitive>();
  let pendingValues = $derived(
    added.size || removed.size
      ? decorateAndSort([...values.filter((v) => !removed.has(getId(v)) && !added.has(getId(v))), ...added.values()])
      : decoratedValues,
  );
  let triggerRef = $state<HTMLButtonElement | null>(null);
  const hintId = $props.id();
  let commandRef = $state<HTMLElement | null>(null);
  let listRef = $state<HTMLElement | null>(null);
  $effect(() => {
    if (listRef && !isLong) return trackActiveDescendant(listRef);
  });

  watch(
    () => open,
    () => {
      filterValue = '';
      if (open) selectedGroupSnapshot = decorateAndSort([...values]);
    },
  );

  watch(
    () => readonly,
    () => {
      if (readonly) setOpen(false);
    },
  );

  onDestroy(() => {
    if (open) save();
  });

  // bits-ui closes a popover 10ms after an outside pointerdown (on touch, after the click), so a click
  // that also submits a form (e.g. "Add Meaning") would read the value before the close saves it.
  $effect(() => {
    if (!open) return;
    return on(
      document,
      'pointerdown',
      (e) => {
        const target = e.target as Node;
        const content = commandRef?.closest('[data-slot="popover-content"], [data-slot="drawer-content"]');
        if (content?.contains(target) || triggerRef?.contains(target)) return;
        save();
      },
      {capture: true},
    );
  });

  function save() {
    if (!added.size && !removed.size) return;
    values = pendingValues.map((v) => v.value);
    added.clear();
    removed.clear();
    onchange?.(values);
  }

  function decorateAndSort(values: Value[]): DecoratedValue[] {
    var decoratedValues = values.map(decorateValue);
    sortValues(decoratedValues);
    return decoratedValues;
  }

  function decorateValue(value: Value): DecoratedValue {
    const id = getId(value);
    const label = getLabel(value);
    const optionIndex = options.findIndex((option) => getId(option) === id);
    return {value, optionIndex, id, label};
  }

  function sortValues(values: DecoratedValue[]): void {
    if (sortValuesBy === 'selectionOrder') {
      // happens automatically
    } else if (sortValuesBy === 'optionOrder') {
      values.sort((a, b) => a.optionIndex - b.optionIndex);
    } else {
      values.sort((a, b) => sortValuesBy(a.value, b.value));
    }
  }

  function setOpen(value: boolean) {
    if (value === open) return;
    open = value;
    if (!value) save();
  }

  function dismiss() {
    setOpen(false);
    void tick().then(() => triggerRef?.focus());
  }

  function toggleSelected(value: Value) {
    const id = getId(value);
    const select = !pendingValues.some((v) => v.id === id);
    const inValues = values.some((v) => getId(v) === id);
    added.delete(id);
    removed.delete(id);
    if (select && !inValues) added.set(id, value);
    else if (!select && inValues) removed.add(id);
  }

  function itemValue(value: Value): string {
    return getLabel(value).toLocaleLowerCase() + String(getId(value));
  }

  // Space toggles like Enter unless there's a filter box to type into; Ctrl+Enter toggles, saves and closes.
  function onkeydown(e: KeyboardEvent) {
    const saveAndClose = e.key === 'Enter' && (e.ctrlKey || e.metaKey);
    if (!saveAndClose && !(e.key === ' ' && !isLong)) return;
    const highlighted = commandRef?.querySelector('[data-command-item][data-selected]')?.getAttribute('data-value');
    const value = renderedOptions.find((option) => itemValue(option) === highlighted);
    if (!value) return;
    e.preventDefault();
    e.stopPropagation();
    toggleSelected(value);
    if (saveAndClose) dismiss();
  }

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options
      .map((option) => ({option, rank: computeCommandScore(getLabel(option).toLocaleLowerCase(), filterValueLower)}))
      .filter((result) => result.rank > 0)
      .sort((a, b) => b.rank - a.rank)
      .map((result) => result.option);
  });

  const isLong = $derived(options.length >= LONG_LIST);
  const useDrawer = $derived(IsMobile.value && isLong);
  const pinSelectedAtTop = $derived(isLong && showSelectedGroup && !filterValue && selectedGroupSnapshot.length > 0);

  const orderedOptions = $derived.by(() => {
    if (!pinSelectedAtTop) return filteredOptions;
    const pinnedIds = new Set(selectedGroupSnapshot.map((s) => s.id));
    const pinned = selectedGroupSnapshot.map((s) => s.value);
    const rest = filteredOptions.filter((option) => !pinnedIds.has(getId(option)));
    return [...pinned, ...rest];
  });

  const pinnedCount = $derived(pinSelectedAtTop ? selectedGroupSnapshot.length : 0);

  const RENDER_LIMIT = 100;
  const renderedOptions = $derived(orderedOptions.slice(0, RENDER_LIMIT));
  const showSelectedDivider = $derived(pinSelectedAtTop && pinnedCount > 0 && pinnedCount < renderedOptions.length);
</script>

{#snippet displayBadges()}
  <div class="flex flex-wrap justify-start gap-2 overflow-hidden">
    {#each pendingValues as value (value.id)}
      <Badge>
        {value.label || $t`Untitled`}
      </Badge>
    {:else}
      <span class="text-muted-foreground x-ellipsis">
        {placeholder ?? $t`None`}
        <!-- ensures that:
          1) baseline alignment works for consumers of this component
          2) list height doesn't shrink when empty
          -->
        <Badge class="max-w-0 invisible">&nbsp;</Badge>
      </span>
    {/each}
  </div>
{/snippet}

{#snippet trigger({props}: {props: Record<string, unknown>})}
  <Button
    bind:ref={triggerRef}
    variant="outline"
    {...props}
    role="combobox"
    aria-expanded={open}
    class="w-full h-auto min-h-10 px-2 justify-between active:press-wide"
  >
    {@render displayBadges()}
    <Icon icon="i-mdi-chevron-down" class="size-5 shrink-0 opacity-50" />
  </Button>
{/snippet}

{#snippet command()}
  <Command shouldFilter={false} bind:ref={commandRef} class="outline-none" onkeydowncapture={onkeydown}>
    {#if isLong}
      <CommandInput
        bind:value={filterValue}
        autofocus
        aria-describedby={hintId}
        placeholder={filterPlaceholder ?? $t`Filter...`}
      >
        <div class="flex items-center gap-2 flex-nowrap">
          {#if IsMobile.value}
            {#if filterValue}
              <XButton onclick={() => (filterValue = '')} aria-label={$t`clear`} />
            {/if}
          {:else}
            <XButton
              onclick={dismiss}
              onkeydown={(e) => (e.key === 'Enter' || e.key === ' ') && e.stopPropagation()}
              aria-label={$t`Close`}
            />
          {/if}
        </div>
      </CommandInput>
    {/if}
    <span id={hintId} class="sr-only"
      >{$t`Ctrl+Enter or Cmd+Enter toggles the highlighted item and closes the list`}</span
    >
    <CommandList
      bind:ref={listRef}
      tabindex={-1}
      class={cn(useDrawer ? 'h-75' : 'max-h-72', 'outline-none')}
      aria-multiselectable="true"
      aria-describedby={hintId}
    >
      <CommandEmpty>{emptyResultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      {#if pinnedCount > 0}
        <CommandGroup heading={$t`Selected`}>
          {#each renderedOptions.slice(0, pinnedCount) as value (getId(value))}
            {@render optionItem(value)}
          {/each}
        </CommandGroup>
      {/if}
      {#if showSelectedDivider}
        <CommandSeparator />
      {/if}
      <CommandGroup heading={showSelectedDivider ? $t`All` : undefined}>
        {#each renderedOptions.slice(pinnedCount) as value (getId(value))}
          {@render optionItem(value)}
        {/each}
        {#if renderedOptions.length < orderedOptions.length}
          <div class="text-muted-foreground text-sm px-2 py-1.5">
            {$t`Refine your filter to see more...`}
          </div>
        {/if}
      </CommandGroup>
    </CommandList>
  </Command>
{/snippet}

{#snippet optionItem(value: Value)}
  {@const label = getLabel(value)}
  {@const id = getId(value)}
  {@const selected = pendingValues.some((v) => v.id === id)}
  <CommandItem
    keywords={[label.toLocaleLowerCase()]}
    value={itemValue(value)}
    onSelect={() => toggleSelected(value)}
    class={cn(OPTION_ROW, label || 'text-muted-foreground')}
    aria-label={label}
    aria-checked={selected}
  >
    <span class="min-w-0 flex-1 break-words">{label || $t`Untitled`}</span>
    <OptionIndicator state={selected} />
  </CommandItem>
{/snippet}

{#if readonly}
  <div
    role="textbox"
    aria-readonly="true"
    aria-labelledby={labelledBy}
    tabindex="0"
    class={cn(inputVariants(), 'h-auto min-h-10 px-2')}
  >
    {@render displayBadges()}
  </div>
{:else if useDrawer}
  <Drawer bind:open={() => open, setOpen}>
    <DrawerTrigger child={trigger} />
    <DrawerContent handle={true} class="overflow-hidden">
      <DrawerHeader class="text-left py-2">
        {#if drawerTitle}
          <DrawerTitle class="mb-2">{drawerTitle}</DrawerTitle>
        {/if}
        <DrawerDescription>
          {@render displayBadges()}
        </DrawerDescription>
      </DrawerHeader>
      {@render command()}
      <DrawerFooter>
        <Button onclick={dismiss}>{$t`Done`}</Button>
      </DrawerFooter>
    </DrawerContent>
  </Drawer>
{:else}
  <Popover bind:open={() => open, setOpen}>
    <PopoverTrigger child={trigger} />
    <PopoverContent
      class="p-0 w-(--bits-popover-anchor-width) min-w-64 max-w-[min(28rem,calc(100vw-1rem))]"
      align="start"
      sticky="always"
      side="bottom"
      avoidCollisions
      onOpenAutoFocus={(e) => {
        if (isLong) return;
        e.preventDefault();
        listRef?.focus();
      }}
    >
      {@render command()}
    </PopoverContent>
  </Popover>
{/if}
