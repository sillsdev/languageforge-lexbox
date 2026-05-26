<script lang="ts">
  import {type RadioGroup as RadioGroupPrimitive} from 'bits-ui';
  import Label from '../label/label.svelte';
  import Item from './radio-group-item.svelte';
  import {cn, type WithoutChildrenOrChild} from '$lib/utils.js';
  import {type Snippet} from 'svelte';

  type Props = WithoutChildrenOrChild<RadioGroupPrimitive.ItemProps> & {
    children?: Snippet;
  };

  let {ref = $bindable(null), class: className, children, ...restProps}: Props = $props();
</script>

<Label class={cn('flex items-center gap-4 md:gap-2 max-md:py-3', restProps.disabled || 'cursor-pointer')}>
  <Item bind:ref {...restProps} />
  {@render children?.()}
</Label>
