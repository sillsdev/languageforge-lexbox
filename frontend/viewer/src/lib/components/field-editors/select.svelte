<script lang="ts" generics="Value">
  import { Badge } from '$lib/components/ui/badge';
  import { Button } from '$lib/components/ui/button';
  import { Popover, PopoverContent, PopoverTrigger } from '$lib/components/ui/popover';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { t } from 'svelte-i18n-lingui';
  import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '../ui/command';
  import { Drawer, DrawerContent, DrawerFooter, DrawerHeader, DrawerTitle, DrawerTrigger } from '../ui/drawer';
  import { Icon } from '../ui/icon';
  import type {ConditionalKeys, Primitive} from 'type-fest';
  import {cn} from '$lib/utils';
  import {watch} from 'runed';

  let {
    value = $bindable(),
    ...constProps
  }: {
    value?: Value;
    options: Readonly<Readonly<Value>[]>;
    readonly?: boolean;
    /* eslint-disable @typescript-eslint/no-redundant-type-constituents */
    idSelector: ConditionalKeys<Value, Primitive> | ((value: Value) => Primitive);
    labelSelector: ConditionalKeys<Value, string> | ((value: Value) => string);
    /* eslint-enable @typescript-eslint/no-redundant-type-constituents */
    placeholder?: string;
    filterPlaceholder?: string;
    emptyResultsPlaceholder?: string;
    drawerTitle?: string;
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
  } = $derived(constProps);

  const getId = $derived.by(() => {
    if (typeof idSelector === 'function') return idSelector;
    return (value: Value) => value[idSelector] as Primitive;
  });

  const getLabel = $derived.by(() => {
    if (typeof labelSelector === 'function') return labelSelector;
    return (value: Value) => value[labelSelector] as string;
  });

  let open = $state(false);
  let filterValue = $state('');
  let triggerRef = $state<HTMLButtonElement | null>(null);
  let commandRef = $state<HTMLElement | null>(null);

  watch(() => open, () => {
    filterValue = '';
  });

  function dismiss() {
    open = false;
  }

  function selectValue(newValue: Value) {
    value = newValue;
    open = false;
  }

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options.filter((option) => {
      const label = getLabel(option).toLocaleLowerCase();
      return label.includes(filterValueLower);
    });
  });
</script>

{#snippet trigger({ props }: { props: Record<string, unknown> })}
  <Button disabled={readonly} bind:ref={triggerRef} variant="outline" {...props} role="combobox" aria-expanded={open}
    class="w-full h-auto px-2 justify-between disabled:opacity-100 disabled:border-transparent">
    {#if value}
      <span>
        {getLabel(value)}
      </span>
    {:else}
      <span class="text-muted-foreground">
        {placeholder ?? $t`None`}
        <!-- ensures that baseline alignment works for consumers of this component -->
        &nbsp;
      </span>
    {/if}
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
            <Button variant="ghost" size="xs-icon" onclick={() => (filterValue = '')} aria-label={$t`clear`}>
              <Icon icon="i-mdi-close" />
            </Button>
          {/if}
        {:else}
          <Button variant="ghost" size="xs-icon" onclick={dismiss} aria-label={$t`Close`}>
              <Icon icon="i-mdi-close" />
          </Button>
        {/if}
      </div>
    </CommandInput>
    <CommandList class="max-md:h-[300px] md:max-h-[50vh]">
      <CommandEmpty>{emptyResultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      <CommandGroup>
        {#each filteredOptions as option, i (getId(option))}
          {@const label = getLabel(option)}
          {@const id = getId(option)}
          {@const selected = value && getId(value) === id}
          <CommandItem
            keywords={[label.toLocaleLowerCase()]}
            value={label.toLocaleLowerCase()}
            onSelect={() => selectValue(option)}
            class="group max-md:h-12"
            data-value-index={i}
            aria-label={label}
          >
            <Icon icon="i-mdi-check" class={cn('md:hidden', selected || 'invisible')} />
            {label}
          </CommandItem>
        {/each}
      </CommandGroup>
    </CommandList>
  </Command>
{/snippet}

{#if IsMobile.value}
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
    <PopoverContent class="p-0 w-96 max-w-[50vw]" align="start" sticky="always" side="bottom" avoidCollisions>
      {@render command()}
    </PopoverContent>
  </Popover>
{/if}
