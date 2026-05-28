<script lang="ts">
  import {Select as SelectPrimitive} from 'bits-ui';
  import {cn, type WithoutChildrenOrChild} from '$lib/utils.js';
  import {Icon} from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    ...restProps
  }: WithoutChildrenOrChild<SelectPrimitive.ScrollUpButtonProps> = $props();
</script>

<SelectPrimitive.ScrollUpButton
  bind:ref
  data-slot="select-scroll-up-button"
  class={cn('flex cursor-default items-center justify-center py-1', className)}
  {...restProps}
>
  <Icon icon="i-mdi-chevron-up" class="size-4" />
</SelectPrimitive.ScrollUpButton>
