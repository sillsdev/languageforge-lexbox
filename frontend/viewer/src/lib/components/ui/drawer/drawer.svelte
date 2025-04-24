<script lang="ts">
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {watch} from 'runed';
  import {onDestroy, onMount} from 'svelte';
  import {Drawer as DrawerPrimitive} from 'vaul-svelte';

  let {
    shouldScaleBackground = true,
    open = $bindable(false),
    activeSnapPoint = $bindable(null),
    ...restProps
  }: DrawerPrimitive.RootProps = $props();

  watch(() => open, () => {
    if (open) {
      handleOpen();
    } else {
      handleClose();
    }
  });

  let addedHistory = $state(false);

  function handleOpen() {
    if (IsMobile.value) {
      history.pushState({ drawer: true }, '');
      addedHistory = true;
    }
  }

  function handleClose() {
    if (addedHistory) {
      addedHistory = false;
      history.back(); // we need to manually pop our added history
    }
  }

  const abortController = new AbortController();

  onMount(() => {
    window.addEventListener('popstate', () => {
      if (open && addedHistory) {
        addedHistory = false; // the user popped our added history
        open = false;
      }
    }, { signal: abortController.signal });
  });

  onDestroy(() => {
    if (addedHistory) history.back();
    abortController.abort();
  });
</script>

<DrawerPrimitive.Root {shouldScaleBackground} bind:open bind:activeSnapPoint {...restProps} />
