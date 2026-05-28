<script lang="ts">
  import {Button} from '$lib/components/ui/button/index.js';
  import {cn} from '$lib/utils.js';
  import type {ComponentProps} from 'svelte';
  import {Icon} from '../icon';
  import {useSidebar} from './context.svelte.js';
  import type {IconClass} from '$lib/icon-class';
  import type {IconProps} from '../icon/icon.svelte';

  let {
    ref = $bindable(null),
    class: className,
    icon = 'i-mdi-dock-left',
    onclick,
    iconProps = undefined,
    ...restProps
  }: ComponentProps<typeof Button> & {
    onclick?: (e: MouseEvent) => void;
    icon?: IconClass;
    iconProps?: Partial<IconProps>;
  } = $props();

  const sidebar = useSidebar();
</script>

<Button
  data-sidebar="trigger"
  data-slot="sidebar-trigger"
  variant="ghost"
  size="icon"
  class={cn('size-7', className)}
  type="button"
  onclick={(e) => {
    onclick?.(e);
    sidebar.toggle();
  }}
  {...restProps}
>
  <Icon {icon} {...iconProps} />
  <span class="sr-only">Toggle Sidebar</span>
</Button>
