<script lang="ts" module>
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLButtonAttributes} from 'svelte/elements';

  export type ListItemProps = WithElementRef<HTMLButtonAttributes>;
</script>

<script lang="ts">
  let {class: className, ref = $bindable(null), children, ...restProps}: ListItemProps = $props();
</script>

<button class={cn('w-full px-4 py-3 dark:bg-muted/50 bg-muted/80 hover:bg-primary/10 hover:dark:bg-primary/10 justify-between items-center text-left max-w-full overflow-hidden aria-selected:ring-2 ring-primary ring-offset-background rounded',
  className)}
  role="row"
  bind:this={ref}
  {...restProps}
  >
  {@render children?.()}
</button>
