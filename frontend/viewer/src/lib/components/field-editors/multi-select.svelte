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

  let {
    values = $bindable(),
    ...constProps
  }: {
    values: Value[];
    options: Value[];
    toLabel: (value: Value) => string;
    placeholder?: string;
    filterPlaceholder?: string;
    resultsPlaceholder?: string;
  } = $props();

  const { options, toLabel, placeholder, filterPlaceholder, resultsPlaceholder } = constProps;

  let open = $state(false);
  let triggerRef = $state<HTMLButtonElement | null>(null);

  function closeAndFocusTrigger() {
    open = false;
    void tick().then(() => {
      triggerRef?.focus();
    });
  }

  function onSelect(value: Value, wasSelected: boolean, close: boolean = true) {
    if (!wasSelected) values = [...values, value];
    else values = values.filter((v) => v !== value);
    if (close) closeAndFocusTrigger();
  }

  let filterValue = $state('');

  const filteredOptions = $derived.by(() => {
    const filterValueLower = filterValue.toLocaleLowerCase();
    return options.filter((option) => {
      const label = toLabel(option).toLocaleLowerCase();
      return label.includes(filterValueLower);
    });
  });
</script>

{#snippet trigger({ props }: { props: Record<string, unknown> })}
  <Button bind:ref={triggerRef} variant="outline" {...props} role="combobox" aria-expanded={open} class="w-full h-auto">
    <div class="flex flex-wrap justify-start gap-2">
      {#each values as value}
        <Badge>
          {toLabel(value)}
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
    <CommandInput bind:value={filterValue} autofocus placeholder={filterPlaceholder ?? 'Filter...'}>
      <Button variant="ghost" size="sm-icon" onclick={() => (open = false)} aria-label={$t`Close`}>
        <Icon icon="i-mdi-close" />
      </Button>
    </CommandInput>
    <CommandList class="h-[300px] md:max-h-[50vh]">
      <CommandEmpty>{resultsPlaceholder ?? $t`No items found`}</CommandEmpty>
      <CommandGroup>
        {#each filteredOptions as value}
          {@const label = toLabel(value)}
          {@const selected = values.includes(value)}
          <CommandItem
            keywords={[label.toLocaleLowerCase()]}
            value={label.toLocaleLowerCase()}
            onSelect={() => onSelect(value, selected)}
            class="group max-md:h-12"
            aria-label={label}
          >
            <Checkbox
              checked={selected}
              class="mr-2 border-muted-foreground !bg-transparent md:group-[&:not(:hover)]:border-transparent max-md:scale-150 !text-foreground"
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
  <Drawer bind:open noBodyStyles modal={undefined} shouldScaleBackground setBackgroundColorOnScale dismissible>
    <DrawerTrigger child={trigger} />
    <DrawerContent handle={false} class="overflow-hidden">
      {@render command()}
    </DrawerContent>
  </Drawer>
{:else}
  <Popover bind:open>
    <PopoverTrigger child={trigger} />
    <PopoverContent class="p-0" align="start" sticky="always" side="bottom" avoidCollisions={false}>
      {@render command()}
    </PopoverContent>
  </Popover>
{/if}
