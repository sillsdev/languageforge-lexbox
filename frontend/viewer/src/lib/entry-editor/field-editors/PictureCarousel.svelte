<script lang="ts">
  import * as Carousel from '$lib/components/ui/carousel';
  import type {CarouselAPI} from '$lib/components/ui/carousel/context';
  import type {IPicture} from '$lib/dotnet-types';
  import PictureImage from './PictureImage.svelte';
  import {Button} from '$lib/components/ui/button';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';

  /** Each picture is shown for 10 seconds before the carousel advances. */
  const AUTOPLAY_DELAY_MS = 10_000;

  type Props = {
    pictures: IPicture[];
    /** Index of the picture currently shown; bindable so parents can observe the active picture. */
    selectedIndex?: number;
    readonly?: boolean;
    /** Disables the per-picture edit affordances while an operation is in flight. */
    busy?: boolean;
    /** Called with the picture the user tapped, to replace it. */
    onReplacePicture?: (picture: IPicture) => void;
    /** Called with the picture whose trash button was clicked, to delete it. */
    onDeletePicture?: (picture: IPicture) => void;
  };
  let {
    pictures,
    selectedIndex = $bindable(0),
    readonly = false,
    busy = false,
    onReplacePicture,
    onDeletePicture,
  }: Props = $props();

  // The Embla api, captured from <Carousel.Root setApi>. We drive our own controls (dots +
  // prev/next) through it rather than using <Carousel.Previous/Next>, which are positioned
  // outside the frame with English-only labels and offer no dot indicator.
  let api = $state<CarouselAPI>();

  const hasMultiple = $derived(pictures.length > 1);

  // Keep the active-dot indicator (and the parent's `selectedIndex`) in sync with the carousel.
  $effect(() => {
    if (!api) return;
    const embla = api;
    function onSelect() {
      selectedIndex = embla.selectedScrollSnap();
    }
    onSelect();
    embla.on('select', onSelect);
    embla.on('reInit', onSelect);
    return () => {
      embla.off('select', onSelect);
      embla.off('reInit', onSelect);
    };
  });

  // Auto-advance every 10 seconds when there is more than one picture. Delete/replace act on
  // the specific picture the user targets, so autoplay can't cause them to hit the wrong one.
  $effect(() => {
    if (!api || !hasMultiple) return;
    const embla = api;
    const interval = setInterval(() => embla.scrollNext(), AUTOPLAY_DELAY_MS);
    return () => clearInterval(interval);
  });
</script>

<!-- With multiple pictures we bound the carousel to a fixed width and center each picture in
     it, so the centered controls below sit under the picture. (Embla lays slides out in a flex
     row, so a shrink-to-fit width would span the sum of all slides.) A single picture keeps its
     natural, left-justified size since it has no controls to align. -->
<div class={cn('flex flex-col gap-2', hasMultiple && 'max-w-md')}>
  <Carousel.Root setApi={(a) => (api = a)} opts={{loop: true}}>
    <Carousel.Content>
      {#each pictures as picture (picture.id)}
        <Carousel.Item class={cn(hasMultiple && 'flex justify-center')}>
          <PictureImage
            {picture}
            {busy}
            onReplace={readonly ? undefined : () => onReplacePicture?.(picture)}
            onDelete={readonly ? undefined : () => onDeletePicture?.(picture)}
          />
        </Carousel.Item>
      {/each}
    </Carousel.Content>
  </Carousel.Root>

  {#if hasMultiple}
    <div class="flex items-center justify-center gap-2">
      <Button
        variant="ghost"
        size="icon-xs"
        icon="i-mdi-chevron-left"
        aria-label={$t`Previous picture`}
        onclick={() => api?.scrollPrev()}
      />
      <div class="flex items-center gap-1.5">
        {#each pictures as picture, i (picture.id)}
          <button
            type="button"
            aria-label={$t`Go to picture ${i + 1}`}
            class={cn('size-2 rounded-full transition-colors', i === selectedIndex ? 'bg-primary' : 'bg-muted-foreground/40')}
            onclick={() => api?.scrollTo(i)}
          ></button>
        {/each}
      </div>
      <Button
        variant="ghost"
        size="icon-xs"
        icon="i-mdi-chevron-right"
        aria-label={$t`Next picture`}
        onclick={() => api?.scrollNext()}
      />
    </div>
  {/if}
</div>
