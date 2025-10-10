<script lang="ts" generics="MutableValue">
  import { Button, XButton } from '$lib/components/ui/button';
  import { Popover, PopoverContent, PopoverTrigger } from '$lib/components/ui/popover';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { t } from 'svelte-i18n-lingui';
  import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '../ui/command';
  import { Drawer, DrawerContent, DrawerFooter, DrawerHeader, DrawerTitle, DrawerTrigger } from '../ui/drawer';
  import { Icon } from '../ui/icon';
  import type {ConditionalKeys, Primitive, ReadonlyDeep} from 'type-fest';
  import {cn} from '$lib/utils';
  import {watch} from 'runed';
  import {computeCommandScore} from 'bits-ui';

  type Value = ReadonlyDeep<MutableValue>;

  let {
    value = $bindable(),
    ...constProps
  }: {
    value?: Value;
    options: ReadonlyArray<Value>;
    readonly?: boolean;
    idSelector: ConditionalKeys<Value, Primitive> | ((value: Value) => Primitive);
    labelSelector: ConditionalKeys<Value, string> | ((value: Value) => string);
    placeholder?: string;
    filterPlaceholder?: string;
    emptyResultsPlaceholder?: string;
    drawerTitle?: string;
    onchange?: (value: Value | undefined) => void;
    class?: string;
    nullOption?: string;
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
    onchange,
    class: className,
    nullOption,
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

  watch(() => open, () => {
    filterValue = '';
  });

  function dismiss() {
    open = false;
  }

  function selectValue(newValue: Value | undefined) {
    value = newValue;
    onchange?.(newValue);
    open = false;
  }

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options.map(option => ({
      option,
      rank: computeCommandScore(getLabel(option).toLocaleLowerCase(), filterValueLower)
    }))
      .filter(result => result.rank > 0)
      .sort((a, b) => b.rank - a.rank)
      .map(result => result.option);
  });

  const RENDER_LIMIT = 100;
  const renderedOptions = $derived(filteredOptions.slice(0, RENDER_LIMIT));
</script>

{#snippet trigger({ props }: { props: Record<string, unknown> })}
  <Button disabled={readonly} bind:ref={triggerRef} variant="outline" {...props} role="combobox" aria-expanded={open}
    class={cn('w-full h-auto px-2 justify-between disabled:opacity-100 disabled:border-transparent', className)}>
    {#if value}
      <span>
        {getLabel(value)}
      </span>
    {:else}
      <span class="text-muted-foreground">
        {placeholder ?? nullOption ?? $t`None`}
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
    <CommandInput bind:value={filterValue} autofocus placeholder={filterPlaceholder ?? $t`Filter...`}>
      <div class="flex items-center gap-2 flex-nowrap">
        {#if IsMobile.value}
          {#if filterValue}
            <XButton onclick={() => (filterValue = '')} aria-label={$t`clear`} />
          {/if}
        {:else}
          <XButton onclick={dismiss} />
        {/if}
      </div>
    </CommandInput>
    <CommandList class="max-md:h-[300px] md:max-h-[50vh]">
      <CommandEmpty>{emptyResultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      <CommandGroup>
        {#if nullOption}
          <CommandItem
            keywords={[nullOption]}
            onSelect={() => selectValue(undefined)}
            class="group max-md:h-12 text-muted-foreground"
            data-value-index={-1}
            aria-label={nullOption}
          >
            <Icon icon="i-mdi-check" class={cn('md:hidden', value === null || 'invisible')} />
            {nullOption}
          </CommandItem>
        {/if}
        {#each renderedOptions as option, i (getId(option))}
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
