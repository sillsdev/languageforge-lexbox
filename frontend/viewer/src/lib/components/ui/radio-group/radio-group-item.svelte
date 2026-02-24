<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {RadioGroup as RadioGroupPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import {Icon} from '../icon';
  import Label from '../label/label.svelte';

  type Props = {
    label?: string;
  } & WithoutChildrenOrChild<RadioGroupPrimitive.ItemProps>;

  let {ref = $bindable(null), class: className, label, ...restProps}: Props = $props();
</script>

{#snippet control()}
  <RadioGroupPrimitive.Item
    bind:ref
    class={cn(
      'border-primary text-primary ring-offset-background focus-visible:ring-ring aspect-square size-4 rounded-full border focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
      className,
    )}
    {...restProps}
  >
    {#snippet children({checked})}
      {#if checked}
        <Icon icon="i-mdi-circle" class="text-current size-full scale-75 origin-center" />
      {/if}
    {/snippet}
  </RadioGroupPrimitive.Item>
{/snippet}

{#if label}
  <Label class="cursor-pointer flex items-center gap-4 md:gap-2 max-md:py-3">
    {@render control()}
    <span>{label}</span>
  </Label>
{:else}
  {@render control()}
{/if}
