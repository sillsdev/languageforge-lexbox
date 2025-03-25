<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  let {
    ref = $bindable(null),
    class: className,
    children,
    ...restProps
  }: Props = $props();
</script>

<div
  bind:this={ref}
  class={cn(
    'border-input bg-background ring-offset-background placeholder:text-muted-foreground has-[:focus-visible]:ring-ring flex h-10 w-full rounded-md border text-base has-[:focus-visible]:outline-none has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-offset-2 has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50 md:text-sm',
    'flex gap-2 items-center justify-between',
    className,
  )}
  {...restProps}
>
  {@render children?.()}
</div>
