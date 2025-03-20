<script lang="ts">
  import type { Snippet } from 'svelte';
  import Icon from '../ui/icon/icon.svelte';

  let {
    resistance = 0.3,
    onRefresh = () => Promise.resolve(),
    children,
    ...restProps
  }: { resistance?: number; onRefresh?: () => Promise<unknown>; children?: Snippet } = $props();

  let startY = 0;
  let currentY = 0;
  let pulling = $state(false);
  let rotateDeg = $state(0);
  let shouldRefresh = $state(false);
  let translateY = $state(0);

  function touchStart(event: TouchEvent) {
    startY = event.touches[0].clientY;
  }

  function touchMove(event: TouchEvent) {
    currentY = event.touches[0].clientY;

    if (currentY - startY > 20) {
      pulling = true;
      rotateDeg = (currentY - startY) * 1;
      translateY = (currentY - startY) * resistance;

      if (rotateDeg > 180) {
        shouldRefresh = true;
      } else {
        shouldRefresh = false;
      }
    } else {
      pulling = false;
    }
  }

  function touchEnd() {
    if (shouldRefresh) {
      rotateDeg = 0;
      void refresh();

      translateY = 60;
    } else {
      translateY = 0;
      pulling = false;
      shouldRefresh = false;
    }
  }

  async function refresh() {
    await onRefresh();

    translateY = 0;
    pulling = false;
    shouldRefresh = false;
  }
</script>

<div ontouchstart={touchStart} ontouchmove={touchMove} ontouchend={touchEnd} class="relative" {...restProps}>
  {#if pulling}
    <div class="fixed left-0 right-0 top-30px">
      {#if shouldRefresh}
        <Icon icon="i-mdi-refresh" class="animate-spin" />
      {:else}
        <Icon icon="i-mdi-refresh" />
      {/if}
    </div>
  {/if}

  <div class="transition-transform" style="transform: translateY({translateY}px)">
    {@render children?.()}
  </div>
</div>
