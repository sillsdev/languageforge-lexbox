<script lang="ts" generics="MutableValue">
  import {Button, XButton} from '$lib/components/ui/button';
  import {Popover, PopoverContent, PopoverTrigger} from '$lib/components/ui/popover';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList} from '../ui/command';
  import {Drawer, DrawerContent, DrawerFooter, DrawerHeader, DrawerTitle, DrawerTrigger} from '../ui/drawer';
  import {Icon} from '../ui/icon';
  import type {ConditionalKeys, Primitive, ReadonlyDeep} from 'type-fest';
  import {cn} from '$lib/utils';
  import {watch} from 'runed';
  import {tick} from 'svelte';
  import {computeCommandScore} from 'bits-ui';
  import CommandSeparator from '../ui/command/command-separator.svelte';
  import {Input} from '../ui/input';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import OptionIndicator from '../ui/select/option-indicator.svelte';
  import {LONG_LIST, OPTION_ROW, tabStaysInFilter, trackActiveDescendant} from '../ui/select/select-list';

  type Value = ReadonlyDeep<MutableValue>;

  const labelledBy = tryUseFieldBody()?.labelId;

  let {
    value = $bindable(),
    ...constProps
  }: {
    value?: Value;
    options: ReadonlyArray<Value>;
    readonly?: boolean;
    clearable?: boolean | 'row' | 'button';
    idSelector: ConditionalKeys<Value, Primitive> | ((value: Value) => Primitive);
    labelSelector: ConditionalKeys<Value, string> | ((value: Value) => string);
    groupSelector?: (value: Value) => string;
    placeholder?: string;
    filterPlaceholder?: string;
    emptyResultsPlaceholder?: string;
    drawerTitle?: string;
    onchange?: (value: Value | undefined) => void;
    class?: string;
  } = $props();

  const {
    options,
    readonly = false,
    clearable = false,
    idSelector,
    labelSelector,
    groupSelector,
    placeholder,
    filterPlaceholder,
    emptyResultsPlaceholder,
    drawerTitle,
    onchange,
    class: className,
  } = $derived(constProps);

  function getId(value: Value): Primitive {
    if (typeof idSelector === 'function') return idSelector(value);
    return value[idSelector] as Primitive;
  }

  function getLabel(value: Value): string {
    if (typeof labelSelector === 'function') return labelSelector(value);
    return value[labelSelector] as string;
  }

  let open = $state(false);
  let filterValue = $state('');
  let triggerRef = $state<HTMLButtonElement | null>(null);
  let commandRef = $state<HTMLElement | null>(null);
  let filterInput = $state<HTMLInputElement | null>(null);

  function clearFilter() {
    filterValue = '';
    filterInput?.focus();
  }
  let listRef = $state<HTMLElement | null>(null);
  $effect(() => {
    if (listRef && !isLong) return trackActiveDescendant(listRef);
  });

  let highlighted = $state('');

  watch(
    () => open,
    () => {
      filterValue = '';
      if (open && value) {
        const current = itemValue(value);
        // After mount: the command list highlights its first item once the items register
        requestAnimationFrame(() => {
          highlighted = current;
          void tick().then(() =>
            commandRef?.querySelector('[data-command-item][data-selected]')?.scrollIntoView({block: 'nearest'}),
          );
        });
      }
    },
  );

  function itemValue(option: Value): string {
    return getLabel(option).toLocaleLowerCase() + String(getId(option));
  }

  const isLong = $derived(options.length >= LONG_LIST);
  const useDrawer = $derived(IsMobile.value && isLong);
  const showNoneRow = $derived(clearable === true || clearable === 'row');

  function dismiss() {
    open = false;
  }

  // Tab closes and moves on from the trigger, since the list is portalled away from the form.
  function onkeydown(e: KeyboardEvent) {
    if (e.key !== 'Tab' || tabStaysInFilter(e)) return;
    e.stopPropagation();
    open = false;
    triggerRef?.focus();
  }

  function selectValue(newValue: Value | undefined) {
    if ((newValue && getId(newValue)) === (value && getId(value))) {
      open = false;
      return;
    }
    value = newValue;
    onchange?.(newValue);
    open = false;
  }

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options
      .map((option) => ({
        option,
        rank: computeCommandScore(getLabel(option).toLocaleLowerCase(), filterValueLower),
      }))
      .filter((result) => result.rank > 0)
      .sort((a, b) => b.rank - a.rank)
      .map((result) => result.option);
  });

  const RENDER_LIMIT = 100;
  // Without a filter, the window shifts so the current value is rendered and can be highlighted.
  const renderStart = $derived.by(() => {
    if (filterValue || !value) return 0;
    const currentId = getId(value);
    const index = filteredOptions.findIndex((option) => getId(option) === currentId);
    return index < RENDER_LIMIT ? 0 : Math.min(index - RENDER_LIMIT / 2, filteredOptions.length - RENDER_LIMIT);
  });
  const renderedOptions = $derived(filteredOptions.slice(renderStart, renderStart + RENDER_LIMIT));
  const renderedGroups = $derived.by(() => {
    if (!groupSelector) return [{heading: undefined, options: renderedOptions}];
    const groups: {heading: string; options: Value[]}[] = [];
    for (const option of renderedOptions) {
      const heading = groupSelector(option);
      const group = groups.find((g) => g.heading === heading);
      if (group) group.options.push(option);
      else groups.push({heading, options: [option]});
    }
    return groups;
  });
</script>

{#snippet trigger({props}: {props: Record<string, unknown>})}
  <!-- Pressed here, not on the Button, so the chevron overlay moves with it. -->
  <div class="relative w-full transition-transform has-[>[role=combobox]:active]:press-wide">
    <Button
      bind:ref={triggerRef}
      variant="outline"
      {...props}
      role="combobox"
      aria-expanded={open}
      class={cn('w-full h-auto min-h-10 px-2 justify-between active:scale-100', className)}
    >
      {#if value}
        <span class="x-ellipsis mr-4">
          {getLabel(value) || $t`Untitled`}
        </span>
      {:else}
        <span class="text-muted-foreground x-ellipsis mr-4">
          {placeholder ?? $t`None`}
          <!-- ensures that baseline alignment works for consumers of this component -->
          &nbsp;
        </span>
      {/if}
    </Button>
    <div class="absolute right-0 z-10 top-1/2 -translate-y-1/2 flex items-center gap-0.5 pointer-events-none">
      {#if clearable === true || clearable === 'button'}
        <XButton
          onclick={() => selectValue(undefined)}
          aria-label={$t`clear`}
          class={cn('pointer-events-auto bg-transparent', value || 'invisible')}
        />
      {/if}
      <Icon icon="i-mdi-chevron-down" class="mr-2 size-5 shrink-0 opacity-50" />
    </div>
  </div>
{/snippet}

{#snippet command()}
  <Command
    shouldFilter={false}
    bind:ref={commandRef}
    bind:value={highlighted}
    class="outline-none"
    onkeydowncapture={onkeydown}
  >
    {#if isLong}
      <CommandInput
        bind:ref={filterInput}
        bind:value={filterValue}
        autofocus
        placeholder={filterPlaceholder ?? $t`Filter...`}
      >
        {#if filterValue}
          <XButton
            onclick={clearFilter}
            onkeydown={(e) => (e.key === 'Enter' || e.key === ' ') && e.stopPropagation()}
            aria-label={$t`Clear filter`}
          />
        {/if}
      </CommandInput>
    {/if}
    <CommandList bind:ref={listRef} tabindex={-1} class={cn(useDrawer ? 'h-75' : 'max-h-72', 'outline-none')}>
      <CommandEmpty>{emptyResultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      {#if showNoneRow && !filterValue}
        <CommandGroup>
          <CommandItem
            onSelect={() => selectValue(undefined)}
            aria-checked={!value}
            class={cn(OPTION_ROW, 'text-muted-foreground')}
          >
            <span class="min-w-0 flex-1">{placeholder ?? $t`None`}</span>
            <OptionIndicator state={!value} />
          </CommandItem>
        </CommandGroup>
        <CommandSeparator />
      {/if}
      {#if renderStart > 0}
        <div class="text-muted-foreground text-sm px-2 py-1.5">
          {$t`Refine your filter to see more...`}
        </div>
      {/if}
      {#each renderedGroups as group, g (group.heading ?? '')}
        {#if g > 0}
          <CommandSeparator forceMount />
        {/if}
        <CommandGroup heading={group.heading}>
          {#each group.options as option (getId(option))}
            {@const label = getLabel(option)}
            {@const selected = !!value && getId(value) === getId(option)}
            <CommandItem
              keywords={[label.toLocaleLowerCase()]}
              value={itemValue(option)}
              onSelect={() => selectValue(option)}
              class={cn(OPTION_ROW, label || 'text-muted-foreground')}
              aria-label={label}
              aria-checked={selected}
            >
              <span class="min-w-0 flex-1 break-words">{label || $t`Untitled`}</span>
              <OptionIndicator state={selected} />
            </CommandItem>
          {/each}
          {#if g === renderedGroups.length - 1 && renderStart + renderedOptions.length < filteredOptions.length}
            <div class="text-muted-foreground text-sm px-2 py-1.5">
              {$t`Refine your filter to see more...`}
            </div>
          {/if}
        </CommandGroup>
      {/each}
    </CommandList>
  </Command>
{/snippet}

{#if readonly}
  <Input
    readonly
    value={value ? getLabel(value) || $t`Untitled` : ''}
    placeholder={placeholder ?? $t`None`}
    aria-labelledby={labelledBy}
    class={className}
  />
{:else if useDrawer}
  <Drawer bind:open>
    <DrawerTrigger child={trigger} />
    <DrawerContent handle={true} class="overflow-hidden">
      <DrawerHeader class="text-left py-2">
        {#if drawerTitle}
          <DrawerTitle class="mb-2">{drawerTitle}</DrawerTitle>
        {/if}
      </DrawerHeader>
      {@render command()}
      <DrawerFooter>
        <Button variant="secondary" onclick={dismiss}>
          {$t`Cancel`}
        </Button>
      </DrawerFooter>
    </DrawerContent>
  </Drawer>
{:else}
  <Popover bind:open>
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
