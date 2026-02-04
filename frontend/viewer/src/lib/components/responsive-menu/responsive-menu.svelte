<script lang="ts" module>
  import {Context} from 'runed';

  type ResponsiveMenuRootStateProps = {
    contextMenu?: boolean;
  };

  const responsiveMenuRootContext = new Context<ResponsiveMenuRootStateProps>('ResponsiveMenu.Root');

  export function useResponsiveMenuRoot(props: ResponsiveMenuRootStateProps): ResponsiveMenuRootStateProps {
    return responsiveMenuRootContext.set(props);
  }

  type ResponsiveMenuContentStateProps = ResponsiveMenuRootStateProps;

  export function useResponsiveMenuContent(): ResponsiveMenuContentStateProps {
    return responsiveMenuRootContext.get();
  }

  type ResponsiveMenuItemStateProps = ResponsiveMenuRootStateProps;

  export function useResponsiveMenuItemList(): ResponsiveMenuItemStateProps {
    return responsiveMenuRootContext.get();
  }

  type ResponsiveMenuTriggerStateProps = ResponsiveMenuRootStateProps;

  export function useResponsiveMenuTrigger(): ResponsiveMenuTriggerStateProps {
    return responsiveMenuRootContext.get();
  }
</script>

<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Drawer from '$lib/components/ui/drawer';
  import * as ContextMenu from '$lib/components/ui/context-menu';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import type {ContextMenuRootProps} from 'bits-ui';
  import type {DrawerRootProps} from 'vaul-svelte';

  type Props = {
    contextMenu?: boolean;
  } & ContextMenuRootProps &
    DrawerRootProps;

  let {open = $bindable(false), contextMenu = false, children, ...rest}: Props = $props();

  useResponsiveMenuRoot({
    get contextMenu() {
      return contextMenu;
    },
  });
</script>

{#if contextMenu}
  <ContextMenu.Root bind:open {...rest}>
    {@render children?.()}
  </ContextMenu.Root>
{:else if !IsMobile.value}
  <DropdownMenu.Root bind:open {...rest}>
    {@render children?.()}
  </DropdownMenu.Root>
{:else}
  <Drawer.Root bind:open {...rest}>
    {@render children?.()}
  </Drawer.Root>
{/if}
