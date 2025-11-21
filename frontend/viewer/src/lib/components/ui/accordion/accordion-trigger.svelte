<script lang="ts">
  import { Accordion as AccordionPrimitive, type WithoutChild } from 'bits-ui';
  import { cn } from '$lib/utils.js';
  import { Icon } from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    level = 3,
    children,
    ...restProps
  }: WithoutChild<AccordionPrimitive.TriggerProps> & {
    level?: AccordionPrimitive.HeaderProps['level'];
  } = $props();
</script>

<AccordionPrimitive.Header {level} class="flex">
  <AccordionPrimitive.Trigger
    bind:ref
    class={cn(
      'flex flex-1 items-center justify-between py-4 font-medium transition-all hover:underline [&[data-state=open]>.i-mdi-chevron-down]:rotate-180',
      className,
    )}
    {...restProps}
  >
    {@render children?.()}
    <Icon icon="i-mdi-chevron-down" class="size-4 shrink-0 transition-transform duration-200" />
  </AccordionPrimitive.Trigger>
</AccordionPrimitive.Header>
