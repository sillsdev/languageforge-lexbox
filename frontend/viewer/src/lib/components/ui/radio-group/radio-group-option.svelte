<script lang="ts">
  import {RadioGroup as RadioGroupPrimitive} from 'bits-ui';
  import Label from '../label/label.svelte';
  import Item from './radio-group-item.svelte';
  import {cn} from '$lib/utils.js';

  let {ref = $bindable(null), class: className, children, ...restProps}: RadioGroupPrimitive.ItemProps = $props();
</script>

<Label class={cn('flex items-center gap-4 md:gap-2 max-md:py-3', restProps.disabled || 'cursor-pointer')}>
  <Item bind:ref {...restProps} />
  <span>{@render children?.()}</span>
</Label>
