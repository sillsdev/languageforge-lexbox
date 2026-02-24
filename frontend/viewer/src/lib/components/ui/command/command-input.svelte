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

<div class="flex items-center border-b px-2" data-command-input-wrapper="">
  <Icon icon="i-mdi-magnify" class="mr-2 size-4 shrink-0 opacity-50" />
  <CommandPrimitive.Input
    class={cn(
      'placeholder:text-muted-foreground flex h-11 w-full rounded-md bg-transparent py-3 text-base outline-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
      className,
    )}
    bind:ref
    {...restProps}
    bind:value
  />
  {@render children?.()}
</div>
