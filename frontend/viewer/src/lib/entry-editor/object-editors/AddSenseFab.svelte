<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {fieldName} from '$lib/i18n';
  import {cn} from '$lib/utils';
  import {useCurrentView} from '$lib/views/view-service';
  import throttle from 'just-throttle';
  import {onMount} from 'svelte';

  const {
    onclick,
  } = $props<{ onclick: () => void }>();

  const currentView = useCurrentView();

  let shrinkFab = $state(false);
  let atBottom = $state(false);

  const handleScroll = throttle((element: Element | null) => {
    if (!element) return;
    if (element.scrollTop == 0) {
      shrinkFab = false;
      return;
    }
    const atEndOfScroll = element.clientHeight + element.scrollTop >= element.scrollHeight - 10;
    atBottom = atEndOfScroll;
    shrinkFab = !atBottom;
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

<Button {onclick} icon="i-mdi-plus" size="fab"
  class={cn('rounded-full gap-0 transition-all drop-shadow-md leading-none ease-in-out duration-300',
    shrinkFab || 'px-4')}>
  <div class="overflow-hidden max-w-max transition-all ease-in-out duration-300" class:!max-w-0={shrinkFab}>
    {fieldName({id: 'sense'}, $currentView.i18nKey)}
  </div>
</Button>
