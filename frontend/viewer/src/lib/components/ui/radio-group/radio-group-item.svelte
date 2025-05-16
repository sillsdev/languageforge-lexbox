<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {RadioGroup as RadioGroupPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import {Icon} from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    ...restProps
  }: WithoutChildrenOrChild<RadioGroupPrimitive.ItemProps> = $props();
</script>

<RadioGroupPrimitive.Item
  bind:ref
  class={cn(
    'border-primary text-primary ring-offset-background focus-visible:ring-ring aspect-square size-4 rounded-full border focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
    className,
  )}
  {...restProps}
>
  {#snippet children({ checked })}
    <!--
    The circle icon is not well centered depending on zoom level.
    There are multiple potential fixes worth trying in: https://github.com/shadcn-ui/ui/issues/3621
    -->
    <div class="flex items-center justify-center">
      {#if checked}
        <Icon icon="i-mdi-circle" class="size-3 text-current" />
      {/if}
    </div>
  {/snippet}
</RadioGroupPrimitive.Item>
