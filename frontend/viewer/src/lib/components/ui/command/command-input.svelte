<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Command as CommandPrimitive} from 'bits-ui';
  import {Icon} from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    value = $bindable(''),
    children,
    ...restProps
  }: CommandPrimitive.InputProps = $props();
</script>

<div class="flex items-center gap-2 border-b px-2" data-slot="command-input-wrapper">
  <Icon icon="i-mdi-magnify" class="size-4 shrink-0 opacity-50" />
  <CommandPrimitive.Input
    data-slot="command-input"
    class={cn(
      'placeholder:text-muted-foreground flex h-11 w-full rounded-md bg-transparent py-3 text-sm outline-hidden disabled:cursor-not-allowed disabled:opacity-50',
      className,
    )}
    bind:ref
    autocapitalize="off"
    {...restProps}
    bind:value
  />
  {@render children?.()}
</div>
