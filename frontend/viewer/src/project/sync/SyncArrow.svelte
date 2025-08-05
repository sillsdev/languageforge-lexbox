<script lang="ts">
  import {cn} from '$lib/utils';

  const { dir, length = '2em', class: className }: {dir: 'up' | 'down' | 'left' | 'right', length: string, class?: string} = $props();

</script>
<div class={cn(
    'block',
    dir === 'right' && 'rotate-0',
    dir === 'down' && 'rotate-90',
    dir === 'left' && 'rotate-180',
    dir === 'up' && '-rotate-90',
    className,
)} style="--arrow-length: {length};">
  <span class={cn(
    'sync-arrow text-sm',
  )}></span>
</div>
<style>
  .sync-arrow {
    position: relative;
    display: block;
    padding: 1em;
    height: 1em;
    width: 1em;
    transform: translateX(
      calc(var(--arrow-length) * 0.5)
    );
  }

  .sync-arrow:after, .sync-arrow:before {
    content: "";
    display: block;
    position: absolute;
    transition: all 0.5s ease-in-out;
  }

  /* Arrow Head */
  .sync-arrow:before {
    top: 50%;
    right: 1em;
    width: 1em;
    height: 1em;
    transform-origin: 50% 50%;
    transform: translate(0, -0.5em) rotate(45deg);
    box-shadow: inset -0.2em 0.2em 0 0 currentColor;
    border-radius: 0 0.15em 0 0;
  }

  /* Arrow Tail */
  .sync-arrow:after {
    top: 50%;
    right: 1em;
    /*pointing right, so this is the length of the tail*/
    width: var(--arrow-length);
    height: 0.2em;
    transform: translate(0, -0.1em);
    background-color: currentColor;
  }

</style>
