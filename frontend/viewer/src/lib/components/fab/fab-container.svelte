<script lang="ts">
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLElement> & {
    children?: Snippet;
  };

  const {children, class: className}: Props = $props();
</script>

<!--
  pb-wide so the FAB sits above the Android gesture-nav tappable region.
  `.app` no longer pays the chrome inset on the bottom (scroll content extends
  behind the gesture nav by design), so the FAB pays the full wide value here.
  pr-wide-extra still adds only the differential because `.app` keeps its
  right chrome padding. No-op on desktop / iOS where wide == chrome.
-->
<div
  class={cn(
    'absolute bottom-4 right-4 md:bottom-6 md:right-6 pb-wide pr-wide-extra flex flex-col items-end z-10 gap-4',
    className,
  )}
>
  {@render children?.()}
</div>
