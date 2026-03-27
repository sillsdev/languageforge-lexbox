<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Command as CommandPrimitive} from 'bits-ui';

  let {ref = $bindable(null), class: className, ...restProps}: CommandPrimitive.LinkItemProps = $props();
</script>

<CommandPrimitive.LinkItem
  bind:ref
  data-slot="command-item"
  class={cn(
    "aria-selected:bg-accent aria-selected:text-accent-foreground [&_[class*='i-mdi-']:not([class*='text-'])]:text-muted-foreground relative flex cursor-default items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-hidden select-none data-[disabled=true]:pointer-events-none data-[disabled=true]:opacity-50 **:[[class*='i-mdi-']]:pointer-events-none *:[[class*='i-mdi-']]:shrink-0 [&_[class*='i-mdi-']:not([class*='size-'])]:size-4",
    className,
  )}
  {...restProps}
/>
