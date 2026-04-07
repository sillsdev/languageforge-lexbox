<script lang="ts">
  import {RadioGroup as RadioGroupPrimitive} from 'bits-ui';
  import {Icon} from '../icon';
  import Label from '../label/label.svelte';
  import {cn, type WithoutChildrenOrChild} from '$lib/utils.js';

  type Props = {
    label?: string;
  } & WithoutChildrenOrChild<RadioGroupPrimitive.ItemProps>;

  let {ref = $bindable(null), class: className, label, ...restProps}: Props = $props();
</script>

{#snippet control()}
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
{/snippet}

{#if label}
  <Label class={cn('flex items-center gap-4 md:gap-2 max-md:py-3', restProps.disabled || 'cursor-pointer')}>
    {@render control()}
    <span class="min-w-0 flex-1 text-sm truncate">{label}</span>
  </Label>
{:else}
  {@render control()}
{/if}
