<script lang="ts">
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {Drawer as DrawerPrimitive} from 'vaul-svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  let {
    shouldScaleBackground = true,
    open = $bindable(false),
    activeSnapPoint = $bindable(null),
    disableBackHandler = false,
    ...restProps
  }: DrawerPrimitive.RootProps & {
    disableBackHandler?: boolean;
  } = $props();

  useBackHandler({addToStack: () => open && IsMobile.value && !disableBackHandler, onBack: () => open = false, key: 'drawer'});
</script>

<DrawerPrimitive.Root {shouldScaleBackground} bind:open bind:activeSnapPoint {...restProps} />
