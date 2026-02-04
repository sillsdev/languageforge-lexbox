<script lang="ts">
  import {mergeProps} from 'bits-ui';
  import type {IconProps} from './icon.svelte';
  import Icon, {iconVariants} from './icon.svelte';
  import {cn} from '$lib/utils';
  import type {IconClass} from '$lib/icon-class';

  type Props = IconProps & {
    ping?: boolean;
    icon: IconClass;
  };

  const {ping = true, icon, ...restProps}: Props = $props();
</script>

<span {...mergeProps({class: iconVariants()}, {class: 'relative inline-flex items-center justify-center'}, restProps)}>
  <Icon {icon} class="relative size-full" />
  {#if ping}
    <Icon {icon} class={cn('absolute size-[85%] animate-ping')} />
  {/if}
</span>
