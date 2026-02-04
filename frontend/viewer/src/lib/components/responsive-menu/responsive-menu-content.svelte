<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {buttonVariants} from '$lib/components/ui/button/button.svelte';
  import {useResponsiveMenuContent} from './responsive-menu.svelte';
  import {ContextMenuContent} from '../ui/context-menu';
  import {DropdownMenuContent} from '../ui/dropdown-menu';
  import * as Drawer from '../ui/drawer';
  import type {Snippet} from 'svelte';
  import type {ContextMenuContentProps, DropdownMenuContentProps} from 'bits-ui';
  import type {DrawerContentProps} from 'vaul-svelte';

  type Props = {
    children?: Snippet;
  } & ContextMenuContentProps &
    DropdownMenuContentProps &
    DrawerContentProps;

  let {children, ref = $bindable(null), ...rest}: Props = $props();

  const state = useResponsiveMenuContent();
</script>

{#if state.contextMenu}
  <ContextMenuContent {...rest} bind:ref>
    {@render children?.()}
  </ContextMenuContent>
{:else if !IsMobile.value}
  <DropdownMenuContent align="end" {...rest} bind:ref>
    {@render children?.()}
  </DropdownMenuContent>
{:else}
  <Drawer.Content {...rest} bind:ref>
    <div class="mx-auto w-full max-w-sm p-4">
      <Drawer.Close class={buttonVariants({variant: 'ghost', size: 'icon', class: 'absolute top-4 right-4 z-10'})}>
        <Icon icon="i-mdi-close" />
      </Drawer.Close>
      <Drawer.Header class="justify-items-end"></Drawer.Header>
      <div class="flex flex-col gap-2">
        {@render children?.()}
      </div>
    </div>
  </Drawer.Content>
{/if}
