<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Tabs as TabsPrimitive} from 'bits-ui';

  let {ref = $bindable(null), class: className, ...restProps}: TabsPrimitive.TriggerProps = $props();
</script>

<TabsPrimitive.Trigger
  bind:ref
  data-slot="tabs-trigger"
  class={cn(
    "data-[state=active]:bg-background dark:data-[state=active]:text-foreground focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:outline-ring dark:data-[state=active]:border-input dark:data-[state=active]:bg-input/30 text-foreground dark:text-muted-foreground inline-flex h-[calc(100%-1px)] flex-1 items-center justify-center rounded-md border border-transparent px-3 py-1.5 text-sm font-medium whitespace-nowrap transition-[color,box-shadow] focus-visible:ring-[3px] focus-visible:outline-1 disabled:pointer-events-none disabled:opacity-50 data-[state=active]:shadow-sm **:[[class*='i-mdi-']]:pointer-events-none *:[[class*='i-mdi-']]:shrink-0 [&_[class*='i-mdi-']:not([class*='size-'])]:size-4",
    className,
  )}
  {...restProps}
/>
