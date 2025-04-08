<script lang="ts" generics="Value">
  import { Badge } from '$lib/components/ui/badge';
  import { Button } from '$lib/components/ui/button';
  import { Popover, PopoverContent, PopoverTrigger } from '$lib/components/ui/popover';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { tick } from 'svelte';
  import { t } from 'svelte-i18n-lingui';
  import { Checkbox } from '../ui/checkbox';
  import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '../ui/command';
  import { Drawer, DrawerContent, DrawerTrigger } from '../ui/drawer';
  import { Icon } from '../ui/icon';
  import type {ConditionalKeys, Primitive} from 'type-fest';
  import {cn} from '$lib/utils';
  import DrawerFooter from '../ui/drawer/drawer-footer.svelte';
  import {slide} from 'svelte/transition';
  import {watch} from 'runed';

  let {
    values = $bindable(),
    ...constProps
  }: {
    values: Value[];
    options: Value[];
    /* eslint-disable @typescript-eslint/no-redundant-type-constituents */
    idSelector: ConditionalKeys<Value, Primitive> | ((value: Value) => Primitive);
    labelSelector: ConditionalKeys<Value, string> | ((value: Value) => string);
    /* eslint-enable @typescript-eslint/no-redundant-type-constituents */
    placeholder?: string;
    filterPlaceholder?: string;
    emptyResultsPlaceholder?: string;
  } = $props();

  const { options, idSelector, labelSelector, placeholder, filterPlaceholder, emptyResultsPlaceholder } = $derived(constProps);

  const getId = $derived.by(() => {
    if (typeof idSelector === 'function') return idSelector;
    return (value: Value) => value[idSelector] as Primitive;
  });

  const getLabel = $derived.by(() => {
    if (typeof labelSelector === 'function') return labelSelector;
    return (value: Value) => value[labelSelector] as string;
  });

  let open = $state(false);
  let dirty = $state(false);
  let pendingValues = $state<Value[]>([]);
  let displayValues = $derived(dirty ? pendingValues : values);
  let triggerRef = $state<HTMLButtonElement | null>(null);

  watch(() => open, () => {
    dirty = false;
    if (open) {
      pendingValues = [...values];
    }
  });

  function dismiss() {
    open = false;
  }

  function submit() {
    open = false;
    values = [...pendingValues];
    void tick().then(() => {
      triggerRef?.focus();
    });
  }

  function onSelect(value: Value, wasSelected: boolean, triggerSubmit: boolean = true) {
    if (!wasSelected) pendingValues = [...pendingValues, value];
    else {
      const id = getId(value);
      const index = pendingValues.findIndex((v) => getId(v) === id);
      if (index !== -1) pendingValues.splice(index, 1);
    }
    if (triggerSubmit) submit();
    else dirty = true;
  }

  let filterValue = $state('');

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options.filter((option) => {
      const label = getLabel(option).toLocaleLowerCase();
      return label.includes(filterValueLower);
    });
  });
</script>

{#snippet trigger({ props }: { props: Record<string, unknown> })}
  <Button bind:ref={triggerRef} variant="outline" {...props} role="combobox" aria-expanded={open} class="w-full h-auto">
    <div class="flex flex-wrap justify-start gap-2">
      {#each displayValues as value (getId(value))}
        <Badge>
          {getLabel(value)}
        </Badge>
      {:else}
        <span class="text-muted-foreground">
          {placeholder}
          &nbsp; <!-- ensures baseline alignment works for consumers of this component -->
        </span>
      {/each}
    </div>
    <div class="grow"></div>
    <Icon icon="i-mdi-chevron-down" class="mr-2 size-5 shrink-0 opacity-50" />
  </Button>
{/snippet}

{#snippet command()}
  <Command shouldFilter={false}>
    <CommandInput bind:value={filterValue} autofocus placeholder={filterPlaceholder ?? $t`Filter...`}>
      <div class="flex items-center gap-2 flex-nowrap">
        {#if IsMobile.value}
          {#if filterValue}
            <Button variant="ghost" size="xs-icon" onclick={() => (filterValue = '')} aria-label={$t`clear`}>
              <Icon icon="i-mdi-close" />
            </Button>
          {/if}
        {:else if !dirty}
          <Button variant="ghost" size="xs" onclick={dismiss} aria-label={$t`Close`}>
              <Icon icon="i-mdi-close" />
          </Button>
        {/if}
      </div>
    </CommandInput>
    {#if !IsMobile.value && dirty}
      <div class="flex gap-3 p-3 items-center flex-nowrap" transition:slide={{ duration: 200 }}>
        <Button class="basis-1/4" variant="secondary" onclick={dismiss} aria-label={$t`Close`}>
            {$t`Cancel`}
        </Button>
        <Button class="basis-3/4" onclick={submit}>
          {$t`Submit`}
        </Button>
      </div>
    {/if}
    <CommandList class="h-[300px] md:max-h-[50vh]">
      <CommandEmpty>{emptyResultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      <CommandGroup>
        {#each filteredOptions as value}
          {@const label = getLabel(value)}
          {@const selected = pendingValues.includes(value)}
          <CommandItem
            keywords={[label.toLocaleLowerCase()]}
            value={label.toLocaleLowerCase()}
            onSelect={() => onSelect(value, selected, !dirty && !IsMobile.value)}
            class="group max-md:h-12"
            aria-label={label}
          >
            <Icon icon="i-mdi-check" class={cn('md:hidden', selected || 'invisible')} />
            <Checkbox
              checked={selected}
              class={cn(
                'max-md:hidden mr-2 border-muted-foreground !text-foreground !bg-transparent group-[&:not(:hover)]:border-transparent',
                selected || '[&:not(:hover)]:opacity-50')}
              onclick={(e) => e.stopPropagation()}
              onCheckedChange={() => onSelect(value, selected, false)}
            />
            {label}
          </CommandItem>
        {/each}
      </CommandGroup>
    </CommandList>
  </Command>
{/snippet}

{#if IsMobile.value}
  <Drawer bind:open dismissible={!dirty}>
    <DrawerTrigger child={trigger} />
    <DrawerContent handle={true} class="overflow-hidden">
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
  <Popover bind:open={() => open, (newOpen) => {
      // discard changes (i.e. prevent passive dismissing) if dirty
      if (!dirty) open = newOpen;
    }}>
    <PopoverTrigger child={trigger} />
    <PopoverContent class="p-0" align="start" sticky="always" side="bottom" avoidCollisions={false}>
      {@render command()}
    </PopoverContent>
  </Popover>
{/if}
