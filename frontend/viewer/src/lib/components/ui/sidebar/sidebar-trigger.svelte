<script lang="ts">
  import {Button} from '$lib/components/ui/button/index.js';
  import {cn} from '$lib/utils.js';
  import type {ComponentProps} from 'svelte';
  import {Icon} from '../icon';
  import {useSidebar} from './context.svelte.js';
  import type {IconClass} from '$lib/icon-class';

  let {
    ref = $bindable(null),
    class: className,
    icon = 'i-mdi-dock-left',
    onclick,
    ...restProps
  }: ComponentProps<typeof Button> & {
    onclick?: (e: MouseEvent) => void;
    icon?: IconClass
  } = $props();

  const sidebar = useSidebar();
</script>

<Button
  type="button"
  onclick={(e) => {
    onclick?.(e);
    sidebar.toggle();
  }}
  data-sidebar="trigger"
  variant="ghost"
  size="icon"
  class={cn('h-7 w-7', className)}
  {...restProps}
>
  <Icon {icon} />
  <span class="sr-only">Toggle Sidebar</span>
</Button>
