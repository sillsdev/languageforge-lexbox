<script lang="ts">
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';
  import {tick} from 'svelte';
  import {Drawer as DrawerPrimitive} from 'vaul-svelte';

  let {
    shouldScaleBackground = true,
    open = $bindable(false),
    activeSnapPoint = $bindable(null),
    snapPoints,
    disableBackHandler = false,
    ...restProps
  }: DrawerPrimitive.RootProps & {
    disableBackHandler?: boolean;
  } = $props();

  useBackHandler({
    addToStack: () => open && IsMobile.value && !disableBackHandler,
    onBack: () => (open = false),
    key: 'drawer',
  });

  // Workaround for vaul-svelte + Svelte 5: bind:activeSnapPoint is ignored on open
  // (drawer lands on snapPoints[0] until interaction). Re-apply after mount/tick.
  // https://github.com/huntabyte/vaul-svelte/issues/129
  watch(
    () => open,
    (isOpen) => {
      if (!isOpen || !snapPoints?.length || activeSnapPoint == null) return;
      const target = activeSnapPoint;
      void tick().then(async () => {
        activeSnapPoint = null;
        await tick();
        if (open) activeSnapPoint = target;
      });
    },
  );
</script>

<DrawerPrimitive.Root {shouldScaleBackground} {snapPoints} bind:open bind:activeSnapPoint {...restProps} />
