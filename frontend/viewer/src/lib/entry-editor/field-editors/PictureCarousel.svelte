<script lang="ts">
  import emblaCarouselSvelte from 'embla-carousel-svelte';
  import type {IPicture} from '$lib/dotnet-types';
  import PictureImage from './PictureImage.svelte';
  import {Button} from '$lib/components/ui/button';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';

  /** Each picture is shown for 10 seconds before the carousel advances. */
  const AUTOPLAY_DELAY_MS = 10_000;

  // Minimal structural type for the bits of the Embla api we use. Declared locally rather
  // than imported from `embla-carousel` (a transitive dep that pnpm does not hoist), so the
  // only carousel import is the directly-declared `embla-carousel-svelte` action.
  type EmblaApi = {
    scrollNext: () => void;
    scrollPrev: () => void;
    scrollTo: (index: number) => void;
    selectedScrollSnap: () => number;
    on: (event: string, callback: () => void) => void;
    off: (event: string, callback: () => void) => void;
  };

  type Props = {
    pictures: IPicture[];
  };
  const {pictures}: Props = $props();

  let emblaApi = $state<EmblaApi>();
  let selectedIndex = $state(0);

  const hasMultiple = $derived(pictures.length > 1);

  // embla-carousel-svelte dispatches an `emblaInit` CustomEvent carrying the api in `detail`.
  // It fires synchronously while the `use:` action sets up (during mount), which is *before*
  // $effects run — so the listener must be bound in the template (the `on:` directive form,
  // as documented by embla-carousel-svelte) to avoid missing it.
  function onEmblaInit(event: Event) {
    emblaApi = (event as CustomEvent<EmblaApi>).detail;
  }

  // Keep the active-dot indicator in sync with the carousel.
  $effect(() => {
    const api = emblaApi;
    if (!api) return;
    const onSelect = () => (selectedIndex = api.selectedScrollSnap());
    onSelect();
    api.on('select', onSelect);
    api.on('reInit', onSelect);
    return () => {
      api.off('select', onSelect);
      api.off('reInit', onSelect);
    };
  });

  // Auto-advance every 10 seconds when there is more than one picture.
  $effect(() => {
    const api = emblaApi;
    if (!api || !hasMultiple) return;
    const interval = setInterval(() => api.scrollNext(), AUTOPLAY_DELAY_MS);
    return () => clearInterval(interval);
  });
</script>

<div class="flex flex-col gap-2">
  <div class="overflow-hidden" use:emblaCarouselSvelte={{options: {loop: true}}} on:emblaInit={onEmblaInit}>
    <div class="flex">
      {#each pictures as picture (picture.id)}
        <div class="min-w-0 flex-[0_0_100%] px-2">
          <PictureImage {picture} />
        </div>
      {/each}
    </div>
  </div>

  {#if hasMultiple}
    <div class="flex items-center justify-center gap-2">
      <Button
        variant="ghost"
        size="icon-xs"
        icon="i-mdi-chevron-left"
        aria-label={$t`Previous picture`}
        onclick={() => emblaApi?.scrollPrev()}
      />
      <div class="flex items-center gap-1.5">
        {#each pictures as picture, i (picture.id)}
          <button
            type="button"
            aria-label={$t`Go to picture ${i + 1}`}
            class={cn('size-2 rounded-full transition-colors', i === selectedIndex ? 'bg-primary' : 'bg-muted-foreground/40')}
            onclick={() => emblaApi?.scrollTo(i)}
          ></button>
        {/each}
      </div>
      <Button
        variant="ghost"
        size="icon-xs"
        icon="i-mdi-chevron-right"
        aria-label={$t`Next picture`}
        onclick={() => emblaApi?.scrollNext()}
      />
    </div>
  {/if}
</div>
