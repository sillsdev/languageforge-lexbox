<script lang="ts">
  import {type Checkbox as CheckboxPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import Label from '../label/label.svelte';
  import {cn} from '$lib/utils.js';
  import Checkbox from './checkbox.svelte';

  type Props = {
    label?: string;
  } & WithoutChildrenOrChild<CheckboxPrimitive.RootProps>;

  let {ref = $bindable(null), class: className, label, checked = $bindable(false), indeterminate = $bindable(false), ...restProps}: Props = $props();
</script>

{#snippet control()}
  <Checkbox
    bind:ref
    bind:checked
    bind:indeterminate
    class={className}
    {...restProps}
  />
{/snippet}

{#if label}
  <Label class={cn('flex items-center gap-4 md:gap-2 max-md:py-3', restProps.disabled || 'cursor-pointer')}>
    {@render control()}
    <span class="min-w-0 flex-1 text-sm truncate">{label}</span>
  </Label>
{:else}
  {@render control()}
{/if}
