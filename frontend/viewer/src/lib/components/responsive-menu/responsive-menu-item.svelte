<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {type IconClass} from '$lib/icon-class';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as ContextMenu from '$lib/components/ui/context-menu';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {buttonVariants} from '$lib/components/ui/button/button.svelte';
  import type {Snippet} from 'svelte';
  import {useResponsiveMenuItemList} from './responsive-menu.svelte';
  import type {ContextMenuItemProps} from 'bits-ui';
  import type {DrawerCloseProps} from 'vaul-svelte';
  import {cn} from '$lib/utils';
  import {DrawerClose} from '../ui/drawer';
  import type {HTMLAnchorAttributes} from 'svelte/elements';
  import Anchor from '../ui/anchor/anchor.svelte';

  type Props = {
    children?: Snippet;
    icon?: IconClass;
    onSelect?: () => void;
    href?: string;
    target?: HTMLAnchorAttributes['target'];
  } & Omit<ContextMenuItemProps & DrawerCloseProps, 'onclick'>;

  let {icon, onSelect, children, ref = $bindable(null), class: className, ...rest}: Props = $props();

  const state = useResponsiveMenuItemList();
</script>

{#snippet content()}
  {#if icon}
    <Icon {icon} />
  {/if}
  {@render children?.()}
{/snippet}

{#snippet anchorChild({props}: {props: Record<string, unknown>})}
  <Anchor {...props}>
    {@render content()}
  </Anchor>
{/snippet}

{#if state.contextMenu}
  <ContextMenu.Item
    class={cn('gap-2 w-full', className)}
    {onSelect}
    bind:ref
    child={rest.href ? anchorChild : undefined}
    {...rest}
  >
    {@render content()}
  </ContextMenu.Item>
{:else if !IsMobile.value}
  <DropdownMenu.Item
    class={cn('gap-2 w-full', className)}
    {onSelect}
    bind:ref
    child={rest.href ? anchorChild : undefined}
    {...rest}
  >
    {@render content()}
  </DropdownMenu.Item>
{:else}
  <DrawerClose
    class={cn(buttonVariants({variant: 'ghost', class: 'w-full justify-start gap-2'}), className)}
    onclick={() => /* wait for drawer to close/start closing */ setTimeout(() => onSelect?.())}
    child={rest.href ? anchorChild : undefined}
    {...rest}
    bind:ref
  >
    {@render content()}
  </DrawerClose>
{/if}
