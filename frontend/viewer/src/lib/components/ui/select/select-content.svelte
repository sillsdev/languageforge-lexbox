<script lang="ts">
  import {Select as SelectPrimitive} from 'bits-ui';
  import SelectPortal from './select-portal.svelte';
  import {cn, type WithoutChild} from '$lib/utils.js';
  import type {ComponentProps} from 'svelte';
  import type {WithoutChildrenOrChild} from '$lib/utils.js';

  let {
    ref = $bindable(null),
    class: className,
    sideOffset = 4,
    portalProps,
    children,
    preventScroll = true,
    ...restProps
  }: WithoutChild<SelectPrimitive.ContentProps> & {
    portalProps?: WithoutChildrenOrChild<ComponentProps<typeof SelectPortal>>;
  } = $props();
</script>

<!-- No ScrollUp/DownButtons: the viewport is the only scroller and shows a normal scrollbar.
 18rem cuts a row in half, hinting there's more below. -->
<SelectPortal {...portalProps}>
  <SelectPrimitive.Content
    bind:ref
    {sideOffset}
    {preventScroll}
    data-slot="select-content"
    class={cn(
      'bg-popover text-popover-foreground data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-end-2 data-[side=right]:slide-in-from-start-2 data-[side=top]:slide-in-from-bottom-2 relative z-50 max-h-[min(var(--bits-select-content-available-height),18rem)] min-w-32 origin-(--bits-select-content-transform-origin) overflow-hidden rounded-md border shadow-md data-[side=bottom]:translate-y-1 data-[side=left]:-translate-x-1 data-[side=right]:translate-x-1 data-[side=top]:-translate-y-1',
      className,
    )}
    {...restProps}
  >
    <SelectPrimitive.Viewport
      class={cn('h-(--bits-select-anchor-height) w-full min-w-(--bits-select-anchor-width) scroll-my-1 p-1')}
    >
      {@render children?.()}
    </SelectPrimitive.Viewport>
  </SelectPrimitive.Content>
</SelectPortal>

<style>
  /* Undo bits-ui's global !important rule that hides the viewport scrollbar.
   A standard scrollbar-width also makes Chrome ignore its ::-webkit-scrollbar { display: none }. */
  :global([data-slot='select-content'] [data-select-viewport]) {
    scrollbar-width: thin !important;
  }
</style>
