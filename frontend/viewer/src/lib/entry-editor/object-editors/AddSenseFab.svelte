<script lang="ts">
  import {fieldName} from '$lib/i18n';
  import {useCurrentView} from '$lib/services/view-service';
  import {mdiPlus} from '@mdi/js';
  import throttle from 'just-throttle';
  import {onMount} from 'svelte';
  import {Button} from 'svelte-ux';

  const currentView = useCurrentView();

  let shrinkFab = false;

  const handleScroll = throttle((element: Element | null) => {
    if (!element) return;
    if (element.scrollTop == 0) {
      shrinkFab = false;
      return;
    }
    const atEndOfScroll = element.clientHeight + element.scrollTop >= element.scrollHeight - 10;
    shrinkFab = !atEndOfScroll;
  }, 20, {leading: false});

  onMount(() => {
    const abortController = new AbortController();
    window.addEventListener('scroll', (e) => {
      const target = (e.target as Document)?.scrollingElement;
      handleScroll(target);
    }, abortController);
    return () => {
      abortController.abort();
    };
  });
</script>

<Button on:click icon={mdiPlus} variant="fill" color="success" size="md" iconOnly rounded="full"
  class="gap-0 transition-all drop-shadow-md {shrinkFab ? '' : 'px-4'}">
  <div class="overflow-hidden ease-linear max-w-max transition-all" class:!max-w-0={shrinkFab}>
    {fieldName({id: 'sense'}, $currentView.i18nKey)}
  </div>
</Button>
