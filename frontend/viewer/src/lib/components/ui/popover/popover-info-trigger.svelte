<script lang="ts">
  import type {ComponentProps, Snippet} from 'svelte';
  import {Icon} from '../icon';
  import * as Popover from '../popover';
  import {cn} from '$lib/utils';

  type Props = ComponentProps<typeof Popover.Trigger> & {
    children?: Snippet;
  };

  const {children, class: className, ...restProps}: Props = $props();
</script>

<!--
  The icon is a plain inline element around a non-breaking space, drawn with a mask, and follows the text with no
  space between: a line can't break before a non-breaking space, so the icon never ends up alone on a line.
  (Chrome breaks before an inline-block or absolutely positioned icon, even after a word joiner.)
-->
<Popover.Trigger class={cn('group', className)} {...restProps}>
  {@render children?.()}<Icon
    icon="i-mdi-information-outline"
    class="inline! ms-1 px-[0.35em] [mask-size:1rem_1rem]! [mask-position:center]! text-muted-foreground group-hover:text-foreground"
  />
</Popover.Trigger>
