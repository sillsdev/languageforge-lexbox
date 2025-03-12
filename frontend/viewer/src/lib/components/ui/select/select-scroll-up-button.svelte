<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Select as SelectPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import {Icon} from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    ...restProps
  }: WithoutChildrenOrChild<SelectPrimitive.ScrollUpButtonProps> = $props();
</script>

<SelectPrimitive.ScrollUpButton
  bind:ref
  class={cn('flex cursor-default items-center justify-center py-1', className)}
  {...restProps}
>
  <Icon icon="i-mdi-chevron-up" class="size-4" />
</SelectPrimitive.ScrollUpButton>
