<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import { type IconClass } from '$lib/icon-class';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as ContextMenu from '$lib/components/ui/context-menu';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import Button, {buttonVariants} from '$lib/components/ui/button/button.svelte';
  import type {Snippet} from 'svelte';
  import {useResponsiveMenuItemList} from './responsive-menu.svelte';
  import type {ButtonProps} from 'node_modules/bits-ui/dist/bits/toolbar/exports';
  import {mergeProps, type ContextMenuItemProps} from 'bits-ui';
  import {cn} from '$lib/utils';

  type Props = {
    children?: Snippet;
    icon?: IconClass;
    label?: string;
    onSelect?: () => void;
    href?: string;
    child?: Snippet;
  } & ContextMenuItemProps & ButtonProps;

  let {
    icon,
    label,
    onSelect,
    children,
    ref = $bindable(null),
    class: className,
    ...rest
  }: Props = $props();

  const state = useResponsiveMenuItemList();

  const buttonProps = $derived({
    variant: 'ghost',
    class: cn(buttonVariants({ variant: 'ghost', class: 'w-full justify-start gap-2' }), className),
    onclick: onSelect,
  } as const);

  const mergedProps = $derived(mergeProps(buttonProps, rest));
</script>

{#snippet content()}
  {#if icon}
    <Icon {icon} />
  {/if}
  {@render children?.()}
  {label}
{/snippet}

{#if state.contextMenu}
  <ContextMenu.Item class={cn('cursor-pointer gap-2', className)} onclick={onSelect} bind:ref {...rest}>
    {@render content()}
  </ContextMenu.Item>
{:else if !IsMobile.value}
  <DropdownMenu.Item class={cn('cursor-pointer gap-2', className)} {onSelect} bind:ref {...rest}>
    {@render content()}
  </DropdownMenu.Item>
{:else if rest.child}
  {@render rest.child({ props: mergedProps})}
{:else}
  <Button
    {...buttonProps}
    bind:ref>
    {@render content()}
  </Button>
{/if}
