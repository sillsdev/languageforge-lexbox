<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {buttonVariants} from '$lib/components/ui/button/button.svelte';
  import {useResponsiveMenuTrigger} from './responsive-menu.svelte';
  import {ContextMenuTrigger} from '../ui/context-menu';
  import {DropdownMenuTrigger} from '../ui/dropdown-menu';
  import {DrawerTrigger} from '../ui/drawer';
  import type {Snippet} from 'svelte';
  import type {ContextMenuTriggerProps, DropdownMenuTriggerProps} from 'bits-ui';
  import type {DrawerTriggerProps} from 'vaul-svelte';
  import type {VariantProps} from 'tailwind-variants';
  import {cn} from '$lib/utils';

  type Props = {
    children?: Snippet;
    size?: VariantProps<typeof buttonVariants>['size'];
  } & ContextMenuTriggerProps &
    DropdownMenuTriggerProps &
    DrawerTriggerProps;

  let {children, class: className, size = 'icon', ref = $bindable(null), ...rest}: Props = $props();

  const triggerVariant = $derived(buttonVariants({variant: 'ghost', size}));
  const state = useResponsiveMenuTrigger();
</script>

{#snippet standardTriggerContent()}
  <Icon icon="i-mdi-dots-vertical" />
{/snippet}

{#snippet triggerContent()}
  {@render (children ?? standardTriggerContent)()}
{/snippet}

{#if state.contextMenu}
  <ContextMenuTrigger class={className} {...rest} bind:ref>
    {@render triggerContent()}
  </ContextMenuTrigger>
{:else if !IsMobile.value}
  <DropdownMenuTrigger class={cn(rest.child || triggerVariant, className)} {...rest} bind:ref>
    {@render triggerContent()}
  </DropdownMenuTrigger>
{:else}
  <DrawerTrigger class={cn(rest.child || triggerVariant, className)} {...rest} bind:ref>
    {@render triggerContent()}
  </DrawerTrigger>
{/if}
