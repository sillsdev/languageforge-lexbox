<script lang="ts">
  import {RadioGroup as RadioGroupPrimitive} from 'bits-ui';
  import {Icon} from '../icon';
  import {cn} from '$lib/utils.js';

  let {ref = $bindable(null), class: className, ...restProps}: RadioGroupPrimitive.ItemProps = $props();
</script>

<RadioGroupPrimitive.Item
  bind:ref
  data-slot="radio-group-item"
  class={cn(
    'border-input text-primary focus-visible:border-ring focus-visible:ring-ring/50 aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive dark:bg-input/30 aspect-square size-4 shrink-0 rounded-full border shadow-xs transition-[color,box-shadow] outline-none focus-visible:ring-[3px] disabled:cursor-not-allowed disabled:opacity-50',
    className,
  )}
  {...restProps}
>
  {#snippet children({checked})}
    <div data-slot="radio-group-indicator" class="relative flex items-center justify-center">
      {#if checked}
        <Icon icon="i-mdi-circle" class="size-2.5 fill-current text-current" />
      {/if}
    </div>
  {/snippet}
</RadioGroupPrimitive.Item>
