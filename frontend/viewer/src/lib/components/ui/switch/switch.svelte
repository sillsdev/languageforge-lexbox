<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Switch as SwitchPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import {Label} from '../label';

  let {
    ref = $bindable(null),
    class: className,
    checked = $bindable(false),
    label,
    ...restProps
  }: WithoutChildrenOrChild<SwitchPrimitive.RootProps> & {
    label?: string;
  } = $props();
</script>

{#snippet control()}
  <SwitchPrimitive.Root
    bind:ref
    bind:checked
    class={cn(
      'focus-visible:ring-ring focus-visible:ring-offset-background data-[state=checked]:bg-primary data-[state=unchecked]:bg-input peer inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
      !!label || className,
    )}
    {...restProps}
  >
  <!-- ensures that baseline alignment works for consumers of this component -->
  <span class="max-w-0">&nbsp;</span>
    <SwitchPrimitive.Thumb
      class={cn(
        'bg-background pointer-events-none block size-5 rounded-full shadow-lg ring-0 transition-transform data-[state=checked]:translate-x-5 data-[state=unchecked]:translate-x-0',
      )}
    />
  </SwitchPrimitive.Root>
{/snippet}

{#if label}
  <Label class={cn('cursor-pointer flex items-center gap-4 max-md:w-full max-md:h-10', className)}>
    {@render control()}
    <span>{label}</span>
  </Label>
{:else}
  {@render control()}
{/if}
