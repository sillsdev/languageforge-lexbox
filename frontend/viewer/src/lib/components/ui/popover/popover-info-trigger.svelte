<script lang="ts">
  import type {ComponentProps, Snippet} from 'svelte';
  import {Icon} from '../icon';
  import * as Popover from '../popover';
  import {cn} from '$lib/utils';

  type Props = ComponentProps<typeof Popover.Trigger> & {
    children?: Snippet;
  }

  const {
    children,
    class: className,
    ...restProps
  }: Props = $props();

</script>

<Popover.Trigger class={cn('group', className)} {...restProps}>
  {@render children?.()}
  <Icon icon="i-mdi-information-outline" class="size-4 text-muted-foreground group-hover:text-foreground" />
</Popover.Trigger>
