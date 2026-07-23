<script lang="ts">
  import {watch} from 'runed';
  import {tick} from 'svelte';
  import {Drawer as DrawerPrimitive} from 'vaul-svelte';

  let {
    shouldScaleBackground = true,
    open = $bindable(false),
    activeSnapPoint = $bindable(null),
    snapPoints,
    ...restProps
  }: DrawerPrimitive.RootProps = $props();

  // Same Svelte 5 activeSnapPoint workaround as drawer.svelte
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

<DrawerPrimitive.NestedRoot {shouldScaleBackground} {snapPoints} bind:open bind:activeSnapPoint {...restProps} />
