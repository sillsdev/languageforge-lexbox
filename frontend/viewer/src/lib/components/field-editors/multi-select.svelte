<script lang="ts" generics="MutableValue">
  import { Badge } from '$lib/components/ui/badge';
  import { Button, XButton } from '$lib/components/ui/button';
  import { Popover, PopoverContent, PopoverTrigger } from '$lib/components/ui/popover';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { tick } from 'svelte';
  import { t } from 'svelte-i18n-lingui';
  import { Checkbox } from '../ui/checkbox';
  import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '../ui/command';
  import { Drawer, DrawerContent, DrawerDescription, DrawerHeader, DrawerTitle, DrawerTrigger } from '../ui/drawer';
  import { Icon } from '../ui/icon';
  import type {ConditionalKeys, Primitive, ReadonlyDeep} from 'type-fest';
  import {cn} from '$lib/utils';
  import DrawerFooter from '../ui/drawer/drawer-footer.svelte';
  import {slide} from 'svelte/transition';
  import {watch} from 'runed';
  import {computeCommandScore} from 'bits-ui';

  type Value = ReadonlyDeep<MutableValue>;

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
  let dirty = $state(false);
  let filterValue = $state('');
  let decoratedValues = $derived(decorateAndSort([...values]));
  let pendingValues = $state<DecoratedValue[]>([]);
  let displayValues = $derived(dirty ? pendingValues : decoratedValues);
  let triggerRef = $state<HTMLButtonElement | null>(null);
  let commandRef = $state<HTMLElement | null>(null);

  watch(() => open, () => {
    dirty = false;
    filterValue = '';
  });

  watch([() => open, () => decoratedValues], () => {
    if (open && !dirty) {
      pendingValues = [...decoratedValues];
    }
  });

  function decorateAndSort(values: Value[]): DecoratedValue[] {
    var decoratedValues = values.map(decorateValue);
    sortValues(decoratedValues);
    return decoratedValues;
  }

  function decorateValue(value: Value): DecoratedValue {
    const id = getId(value);
    const label = getLabel(value);
    const optionIndex = options.findIndex((option) => getId(option) === id);
    return { value, optionIndex, id, label };
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

  function dismiss() {
    open = false;
  }

  function submit() {
    open = false;
    values = pendingValues.map(p => p.value);
    onchange?.(values);
    void tick().then(() => {
      triggerRef?.focus();
    });
  }

  function toggleSelected(value: Value, triggerSubmit: boolean = true) {
    const id = getId(value);
    const isSelected = pendingValues.some((v) => v.id === id);

    if (!isSelected) { // add
      pendingValues = [...pendingValues, decorateValue(value)];
      sortValues(pendingValues);
    } else { // remove
      const index = pendingValues.findIndex((v) => v.id === id);
      if (index !== -1) pendingValues.splice(index, 1);
    }

    if (triggerSubmit) submit();
    else dirty = true;
  }

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options.map(option => ({option, rank: computeCommandScore(getLabel(option).toLocaleLowerCase(), filterValueLower)}))
      .filter(result => result.rank > 0)
      .sort((a, b) => b.rank - a.rank)
      .map(result => result.option);
  });

  const RENDER_LIMIT = 100;
  const renderedOptions = $derived(filteredOptions.slice(0, RENDER_LIMIT));

  function onkeydown(e: KeyboardEvent) {
    if (e.key === 'Enter' && dirty && (e.metaKey || e.ctrlKey)) {
      submit();
      e.stopPropagation();
      e.preventDefault();
    } else if (e.key === ' ' && (e.metaKey || e.ctrlKey)) {
      tryToggleHighlightedValue();
      e.stopPropagation();
      e.preventDefault();
    }
  }

  function tryToggleHighlightedValue() {
      const value = getHighlightedValue();
      if (value) {
        toggleSelected(value, false);
      }
  }

  function getHighlightedValue(): Value | undefined {
    const selectedItem = commandRef?.querySelector('[data-command-item][data-selected]');
    const index = selectedItem?.getAttribute('data-value-index');
    if (index) return filteredOptions[Number(index)];
  }
</script>

{#snippet displayBadges()}
  <div class="flex flex-wrap justify-start gap-2 overflow-hidden">
    {#each displayValues as value (value.id)}
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
        <Badge class="max-w-0 invisible">
          &nbsp;
        </Badge>
      </span>
    {/each}
  </div>
{/snippet}

{#snippet trigger({ props }: { props: Record<string, unknown> })}
  <Button disabled={readonly} bind:ref={triggerRef} variant="outline" {...props} role="combobox" aria-expanded={open}
    class="w-full h-auto min-h-10 px-2 justify-between disabled:opacity-100 disabled:border-transparent">
    {@render displayBadges()}
    {#if !readonly}
      <Icon icon="i-mdi-chevron-down" class="mr-2 size-5 shrink-0 opacity-50" />
    {/if}
  </Button>
{/snippet}

{#snippet command()}
  <Command shouldFilter={false} bind:ref={commandRef}>
    <CommandInput bind:value={filterValue} autofocus placeholder={filterPlaceholder ?? $t`Filter...`} {onkeydown}>
      <div class="flex items-center gap-2 flex-nowrap">
        {#if IsMobile.value}
          {#if filterValue}
            <XButton onclick={() => (filterValue = '')} aria-label={$t`clear`} />
          {/if}
        {:else}
          {#if dirty}
            <div in:slide={{axis: 'x', duration: 200}}>
              <Button variant="default" size="xs" onclick={submit} aria-label={$t`Submit`}>
                {$t`Submit`}
              </Button>
            </div>
          {/if}
          <XButton variant={dirty ? 'secondary' : 'ghost'} onclick={dismiss} aria-label={$t`Close`} />
        {/if}
      </div>
    </CommandInput>
    <CommandList class="max-md:h-[300px] md:max-h-[40vh]">
      <CommandEmpty>{emptyResultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      <CommandGroup>
        {#each renderedOptions as value, i (getId(value))}
          {@const label = getLabel(value)}
          {@const id = getId(value)}
          {@const selected = pendingValues.some(v => v.id === id)}
          <CommandItem
            keywords={[label.toLocaleLowerCase()]}
            value={label.toLocaleLowerCase() + String(id)}
            onSelect={() => toggleSelected(value, !dirty && !IsMobile.value)}
            class={cn('group max-md:h-12', label || 'text-muted-foreground')}
            data-value-index={i}
            aria-label={label}
          >
            <Icon icon="i-mdi-check" class={cn('md:hidden', selected || 'invisible')} />
            <Checkbox
              checked={selected}
              tabindex={-1}
              class={cn(
                'max-md:hidden mr-2 border-muted-foreground !text-foreground !bg-transparent group-[&:not([data-selected])]:border-transparent',
                selected || '[&:not(:hover)]:opacity-50')}
              onclick={(e) => {
                // prevents command item selection
                e.stopPropagation();
              }}
              onpointerdown={(e) => {
                // prevents moving focus to the checkbox when clicking on it
                e.preventDefault();
              }}
              onCheckedChange={() => toggleSelected(value, false)}
            />
            {label || $t`Untitled`}
          </CommandItem>
        {/each}
        {#if renderedOptions.length < filteredOptions.length}
          <div class="text-muted-foreground text-sm px-2 py-1">
            {$t`Refine your filter to see more...`}
          </div>
        {/if}
      </CommandGroup>
    </CommandList>
  </Command>
{/snippet}

{#if IsMobile.value}
  <Drawer bind:open dismissible={!dirty}>
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
        <div class="flex gap-4 items-center flex-nowrap">
          <Button variant="secondary" class={cn('basis-1/4 transition-all', dirty || 'basis-3/4')} onclick={dismiss}>
            {$t`Cancel`}
          </Button>
          <Button disabled={!dirty} class={cn('basis-3/4 transition-all', dirty || 'basis-1/4')} onclick={submit}>
            {$t`Submit`}
          </Button>
        </div>
      </DrawerFooter>
    </DrawerContent>
  </Drawer>
{:else}
  <Popover bind:open>
    <PopoverTrigger child={trigger} />
    <PopoverContent class="p-0 w-96 max-w-[50vw]" align="start" sticky="always" side="bottom" avoidCollisions interactOutsideBehavior={dirty ? 'ignore' : 'close'}>
      {@render command()}
    </PopoverContent>
  </Popover>
{/if}
